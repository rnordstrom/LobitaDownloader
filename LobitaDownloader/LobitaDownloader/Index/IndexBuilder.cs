using LobitaDownloader.Index.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml;

namespace LobitaDownloader
{
    class IndexBuilder
    {
        private IIndexPersistence _persistence;
        private IIndexBackup _backup;
        private IConfigManager _config;
        private HttpXmlClient client;
        private const string TestbooruUrl = "https://testbooru.donmai.us/";
        private const string DanbooruUrl = "https://danbooru.donmai.us/";
        private string urlToUse;
        private int numThreads = 0;
        private const int TagsLimit = 1000;
        private const int PostsLimit = 200;
        private const int SeriesLimit = 1;
        private const int BackoffLimitSeconds = 320;
        private IDictionary<string, Character> characterIndex = new ConcurrentDictionary<string, Character>();
        private IDictionary<string, Series> seriesIndex = new ConcurrentDictionary<string, Series>();

        public IndexBuilder(IIndexPersistence persistence, IIndexBackup backup, IConfigManager config)
        {
            #if DEBUG
                urlToUse = TestbooruUrl;
            #else
                urlToUse = DanbooruUrl;
            #endif

            _persistence = persistence;
            _backup = backup;
            _config = config;
            client = new HttpXmlClient(urlToUse);

            numThreads = int.Parse(_config.GetItemByName("NumThreads"));
        }

        public void Index(int tagCount)
        {
            int id;
            int lastId = 0;
            int j = 1;
            string output;
            string tagName;
            string seriesName;
            XmlElement tagRoot;
            XmlNodeList tagNodes;

            Console.Clear();
            Console.WriteLine("Building index...");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            // Fetch character tags
            do
            {
                tagRoot = client.GetPosts(urlToUse + $"tags.xml?search[category]=4&limit={TagsLimit}&page=a{lastId}&only=name,id").Result;
                tagNodes = tagRoot.SelectNodes("tag");

                for (int i = 0; i < tagNodes.Count; i++)
                {
                    tagName = tagNodes[i].SelectSingleNode("name").InnerText;
                    id = int.Parse(tagNodes[i].SelectSingleNode("id").InnerText);

                    if (!tagName.Contains("#"))
                    {
                        characterIndex.TryAdd(tagName, new Character(id, tagName, 0, new List<Series>(), new List<Url>()));

                        output = $"Fetching character tags ({j++}).";
                        PrintUtils.PrintRow(output, 0, 0);
                    }

                    if (i == 0)
                    {
                        lastId = id;
                    }
                }

                if (tagCount > 0 && j == tagCount)
                {
                    break;
                }
            }
            while (tagNodes.Count != 0);

            // Fetch series tags
            lastId = 0;
            j = 1;

            do
            {
                tagRoot = client.GetPosts(urlToUse + $"tags.xml?search[category]=3&limit={TagsLimit}&page=a{lastId}&only=name,id").Result;
                tagNodes = tagRoot.SelectNodes("tag");

                for (int i = 0; i < tagNodes.Count; i++)
                {
                    seriesName = tagNodes[i].SelectSingleNode("name").InnerText;
                    id = int.Parse(tagNodes[i].SelectSingleNode("id").InnerText);

                    if (!seriesName.Contains("#"))
                    {
                        seriesIndex.TryAdd(seriesName, new Series(id, seriesName, 0));

                        output = $"Fetching series tags ({j++}).";
                        PrintUtils.PrintRow(output, 0, 0);
                    }

                    if (i == 0)
                    {
                        lastId = id;
                    }
                }
            }
            while (tagNodes.Count != 0);

            // Write empty tag and series names to backup
            _backup.IndexCharacters(characterIndex);
            _backup.IndexSeries(seriesIndex);

            // Fetch data from external source and save locally
            Download();

            watch.Stop();

            TimeSpan timespan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
            string timeString = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);

            Resources.SystemLogger.Log($"Downloaded {characterIndex.Keys.Count} tags in {timeString} using {numThreads} thread(s).");
        }

        public void Download()
        {
            Console.Clear();
            Console.WriteLine("Downloading data...");

            int batchSize = int.Parse(_config.GetItemByName("BatchSize"));

            while (true)
            {
                characterIndex = _backup.GetCharacterIndex(ModificationStatus.UNMODIFIED, batchSize);
                ClearTagIndex();

                if (characterIndex.Count == 0)
                {
                    Console.WriteLine("Index is complete.");

                    break;
                }

                seriesIndex = _backup.GetSeriesIndex();

                FetchData();

                Console.Clear();
                Console.WriteLine("Writing to backups...");

                _backup.BackupCharacterData(characterIndex);
            }

            Persist();
        }

        public void Persist()
        {
            Console.Clear();
            Console.WriteLine("Persisting to database from backups...");

            characterIndex = _backup.GetCharacterIndex(ModificationStatus.DONE);

            _persistence.Clean();
            _persistence.PersistCharacters(characterIndex);

            SwitchDatabase();
        }

        public void Update(List<string> tagNames)
        {
            Console.Clear();
            Console.WriteLine($"Updating tags...");

            _backup.MarkForUpdate(tagNames);

            characterIndex = _backup.GetCharacterIndex(ModificationStatus.UNMODIFIED);
            ClearTagIndex();

            if (characterIndex.Count == 0)
            {
                Console.WriteLine("Index is complete.");

                return;
            }

            seriesIndex = _backup.GetSeriesIndex();

            FetchData();

            Console.Clear();
            Console.WriteLine("Writing to backups...");

            _backup.BackupCharacterData(characterIndex);

            Persist();
        }

        public void CleanUp()
        {
            Console.Clear();

            _persistence.Clean();
        }

        private void ClearTagIndex()
        {
            foreach (string key in characterIndex.Keys)
            {
                characterIndex[key].Urls.Clear();
            }
        }

        private void FetchData()
        {
            int tagCount = characterIndex.Keys.Count;

            if (tagCount < numThreads)
            {
                numThreads = tagCount;
            }

            int partitionSize = (int)Math.Round((double)tagCount / numThreads);
            Thread[] threads = new Thread[numThreads];
            Tuple<int, int>[] limits = new Tuple<int, int>[numThreads];

            for (int i = 0; i < numThreads; i++)
            {
                if (i == numThreads - 1)
                {
                    limits[i] = new Tuple<int, int>(partitionSize * i, characterIndex.Keys.Count - 1);
                }
                else
                {
                    limits[i] = new Tuple<int, int>(partitionSize * i, (partitionSize * (i + 1)) - 1);
                }
            }

            Console.Clear();

            int j = 0;

            foreach (var l in limits)
            {
                threads[j] = new Thread(() => GetLinks(characterIndex.Keys.ToList().GetRange(l.Item1, (l.Item2 - l.Item1) + 1)));
                threads[j].Name = j.ToString();
                threads[j].Start();

                j++;
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }
        }

        private void GetLinks(ICollection<string> tagNames)
        {
            int j = 1;
            int backoffSeconds;
            string characterName;
            string output;
            string path;
            XmlElement postRoot;
            XmlNodeList postNodes;
            XmlNode idNode;
            XmlNode fileNode;
            XmlNode seriesNode;
            IDictionary<string, int> tagOccurrences = new Dictionary<string, int>();
            HashSet<string> topSeries;
            int threadId = int.Parse(Thread.CurrentThread.Name);

            for (int i = 0; i < tagNames.Count; i++)
            {
                characterName = tagNames.ElementAt(i);
                
                while (true)
                {
                    try
                    {
                        backoffSeconds = 10;
                        output = $"Thread {threadId}: processing tag '{characterName}' ({i + 1} / {tagNames.Count}; page #{j}).";

                        PrintUtils.PrintRow(output, 0, threadId);

                        path = urlToUse + $"posts.xml?tags={characterName} rating:safe&limit={PostsLimit}&page={j}&only=id,file_url,tag_string_copyright";
                        postRoot = client.GetPosts(path).Result;

                        // Keep trying to fetch a page of posts if the first request fails. Wait for a doubling backoff-period.
                        while (postRoot == null && backoffSeconds <= BackoffLimitSeconds)
                        {
                            output = $"Thread {threadId} (Stalled; backoff: {backoffSeconds}), processing tag '{characterName}' ({i + 1} / {tagNames.Count}; page #{j}).";

                            PrintUtils.PrintRow(output, 0, threadId);
                            Thread.Sleep(backoffSeconds * 1000);

                            postRoot = client.GetPosts(path).Result;
                            backoffSeconds *= 2;
                        }

                        postNodes = postRoot.SelectNodes("post");

                        // If an empty page is reached, move on to the next tag.
                        if (postNodes.Count == 0)
                        {
                            break;
                        }

                        for (int k = 0; k < postNodes.Count; k++)
                        {
                            idNode = postNodes[k].SelectSingleNode("id");
                            fileNode = postNodes[k].SelectSingleNode("file-url");
                            seriesNode = postNodes[k].SelectSingleNode("tag-string-copyright");

                            // If there is no file url, simply skip the post
                            if (fileNode != null)
                            {
                                characterIndex[characterName].Urls.Add(new Url(int.Parse(idNode.InnerText), fileNode.InnerText));
                                characterIndex[characterName].PostCount++;
                            }

                            if (seriesNode != null)
                            {
                                foreach (string seriesName in seriesNode.InnerText.Split(" "))
                                {
                                    if (!string.IsNullOrEmpty(seriesName) && seriesIndex.ContainsKey(seriesName))
                                    {
                                        if (!tagOccurrences.ContainsKey(seriesName))
                                        {
                                            tagOccurrences.Add(seriesName, 1);
                                        }
                                        else
                                        {
                                            tagOccurrences[seriesName]++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (NullReferenceException e) // Skip the page and try fetching the next page
                    {
                        Resources.SystemLogger.Log($"Failed to retrieve page {j} for tag {characterName}." + Environment.NewLine + e.StackTrace);
                    }

                    j++;
                }

                topSeries = IndexUtils.GetTopSeries(ref tagOccurrences, SeriesLimit);

                foreach (string seriesName in topSeries)
                {
                    characterIndex[characterName].Series.Add(seriesIndex[seriesName]);
                    seriesIndex[seriesName].PostCount += characterIndex[characterName].PostCount;
                }

                j = 1;

                tagOccurrences.Clear();
            }

            output = $"Thread {threadId}: done.";
            PrintUtils.PrintRow(output, 0, threadId);
        }

        private void SwitchDatabase()
        {
            string currentName = "CurrentDatabase";
            string nextName = "NextDatabase";

            string currentDatabase = _config.GetItemByName(currentName);
            string nextDatabase = _config.GetItemByName(nextName);
            string temp = currentDatabase;

            _config.ChangeItemByName(currentName, nextDatabase);
            _config.ChangeItemByName(nextName, temp);
        }

        private void ClearBelow()
        {
            int remainder = Console.WindowHeight - numThreads;

            if (remainder < 0)
            {
                remainder = 0;
            }

            for (int i = numThreads; i < numThreads + remainder; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth));
            }
        }
    }
}
