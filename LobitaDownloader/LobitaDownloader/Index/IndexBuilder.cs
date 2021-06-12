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
        private const string TestBooruUrl = "https://testbooru.donmai.us/";
        private const string DanBooruUrl = "https://danbooru.donmai.us/";
        private string urlToUse;
        private int numThreads = 0;
        private const int TagsLimit = 1000;
        private const int PostsLimit = 200;
        private const int SeriesLimit = 1;
        private const int BackoffLimitSeconds = 320;
        private IDictionary<string, List<string>> tagLinks = new ConcurrentDictionary<string, List<string>>();
        private IDictionary<string, HashSet<string>> seriesTags = new ConcurrentDictionary<string, HashSet<string>>();

        public IndexBuilder(IIndexPersistence persistence, IIndexBackup backup, IConfigManager config)
        {
            #if DEBUG
                urlToUse = TestBooruUrl;
            #else
                urlToUse = DanBooruUrl;
            #endif

            _persistence = persistence;
            _backup = backup;
            _config = config;
            client = new HttpXmlClient(urlToUse);

            numThreads = int.Parse(_config.GetItemByName("NumThreads"));

            if (numThreads > 1)
            {
                throw new NotSupportedException("Multi-threading is not supported in this version of LobitaDownloader. Please set NumThreads to 1.");
            }
        }

        public void BuildIndex()
        {
            int lastId = 0;
            int j = 1;
            string output;
            string tagName;
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

                    if (!tagName.Contains("#"))
                    {
                        tagLinks.TryAdd(tagName, new List<string>());

                        output = $"Fetching character tags ({j++}).";
                        PrintUtils.PrintRow(output, 0, 0);
                    }

                    if (i == 0)
                    {
                        lastId = int.Parse(tagNodes[i].SelectSingleNode("id").InnerText);
                    }
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
                    tagName = tagNodes[i].SelectSingleNode("name").InnerText;

                    if (!tagName.Contains("#"))
                    {
                        seriesTags.TryAdd(tagName, new HashSet<string>());

                        output = $"Fetching series tags ({j++}).";
                        PrintUtils.PrintRow(output, 0, 0);
                    }

                    if (i == 0)
                    {
                        lastId = int.Parse(tagNodes[i].SelectSingleNode("id").InnerText);
                    }
                }
            }
            while (tagNodes.Count != 0);

            // Write empty tag and series names to backup
            _backup.BackupTagNames(tagLinks.Keys.ToList());
            _backup.BackupSeriesNames(seriesTags.Keys.ToList());

            // Fetch data from external source and write to backup
            FetchData();

            // Persist to database
            BackupRestore();

            watch.Stop();

            TimeSpan timespan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
            string timeString = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);

            Resources.SystemLogger.Log($"Downloaded {tagLinks.Keys.Count} tags in {timeString} using {numThreads} thread(s).");
        }

        public void Recover()
        {
            Console.Clear();
            Console.WriteLine("Recovering from previous run...");

            tagLinks = _backup.GetTagIndex(ModificationStatus.UNMODIFIED);

            if (tagLinks.Count == 0)
            {
                Console.WriteLine("Index is complete; no recovery necessary.");

                return;
            }

            seriesTags = _backup.GetSeriesIndex();

            FetchData();
            BackupRestore();
        }

        public void BackupRestore()
        {
            Console.Clear();
            Console.WriteLine("Restoring from backup...");

            tagLinks = _backup.GetTagIndex(ModificationStatus.DONE);

            _persistence.CleanTagLinks();
            _persistence.PersistTagLinks(tagLinks);

            seriesTags = _backup.GetSeriesIndex();

            _persistence.CleanSeries();
            _persistence.PersistSeriesTags(seriesTags);

            _persistence.CountTagLinks();
            _persistence.CountSeriesLinks();

            SwitchDatabase();
        }

        public void CleanUp()
        {
            Console.Clear();

            _persistence.CleanTagLinks();
            _persistence.CleanSeries();
        }

        private void FetchData()
        {
            int partitionSize = (int)Math.Round((double)tagLinks.Keys.Count / numThreads);
            Thread[] threads = new Thread[numThreads];
            Tuple<int, int>[] limits = new Tuple<int, int>[numThreads];

            for (int i = 0; i < numThreads; i++)
            {
                if (i == numThreads - 1)
                {
                    limits[i] = new Tuple<int, int>(partitionSize * i, tagLinks.Keys.Count - 1);
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
                threads[j] = new Thread(() => GetLinks(l.Item1, l.Item2));
                threads[j].Name = j.ToString();
                threads[j].Start();

                j++;
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }
        }

        private void GetLinks(int start, int end)
        {
            int j = 1;
            int backoffSeconds;
            string tagName;
            string output;
            string path;
            XmlElement postRoot;
            XmlNodeList postNodes;
            XmlNode fileNode;
            XmlNode seriesNode;
            IDictionary<string, int> tagOccurrences = new Dictionary<string, int>();
            List<string> linksForTag = new List<string>();
            List<string> topSeries;

            for (int i = start; i <= end; i++)
            {
                tagName = tagLinks.Keys.ElementAt(i);
                
                while (true)
                {
                    try
                    {
                        backoffSeconds = 10;
                        output = $"Thread {int.Parse(Thread.CurrentThread.Name)}: processing tag '{tagName}' ({i - start + 1} / {end - start + 1}; page #{j}).";

                        PrintUtils.PrintRow(output, 0, int.Parse(Thread.CurrentThread.Name));

                        path = urlToUse + $"posts.xml?tags={tagName} rating:safe&limit={PostsLimit}&page={j}&only=file_url,tag_string_copyright";
                        postRoot = client.GetPosts(path).Result;

                        // Keep trying to fetch a page of posts if the first request fails. Wait for a doubling backoff-period.
                        while (postRoot == null && backoffSeconds <= BackoffLimitSeconds)
                        {
                            output = $"Thread {int.Parse(Thread.CurrentThread.Name)} (Stalled; backoff: {backoffSeconds}), processing tag '{tagName}' ({i - start + 1} / {end - start + 1}; page #{j}).";

                            PrintUtils.PrintRow(output, 0, int.Parse(Thread.CurrentThread.Name));
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
                            fileNode = postNodes[k].SelectSingleNode("file-url");
                            seriesNode = postNodes[k].SelectSingleNode("tag-string-copyright");

                            // If there is no file url, simply skip the post
                            if (fileNode != null)
                            {
                                linksForTag.Add(fileNode.InnerText);
                            }

                            if (seriesNode != null)
                            {
                                foreach (string seriesName in seriesNode.InnerText.Split(" "))
                                {
                                    if (!string.IsNullOrEmpty(seriesName) && seriesTags.ContainsKey(seriesName))
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
                        Resources.SystemLogger.Log($"Failed to retrieve page {j} for tag {tagName}." + Environment.NewLine + e.StackTrace);
                    }

                    j++;
                }

                topSeries = IndexUtils.GetTopSeries(ref tagOccurrences, SeriesLimit);

                // Backup each tag
                foreach (string series in topSeries)
                {
                    _backup.BackupSingleSeriesTags(series, tagName);
                }

                _backup.BackupSingleTagLinks(tagName, linksForTag);

                j = 1;

                tagOccurrences.Clear();
                linksForTag.Clear();

                ClearBelow();
            }
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
