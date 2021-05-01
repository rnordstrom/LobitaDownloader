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
        private IIndexPersistence _backup;
        private IConfigManager _config;
        private HttpXmlClient client;
        private const string TestBooruUrl = "https://testbooru.donmai.us/";
        private const string DanBooruUrl = "https://danbooru.donmai.us/";
        private string urlToUse;
        private int numThreads = 0;
        private const int TagsLimit = 1000;
        private const int PostsLimit = 1000;
        private const int SeriesLimit = 1;
        private const int BackoffLimitSeconds = 320;
        private IDictionary<string, List<string>> tagLinks = new ConcurrentDictionary<string, List<string>>();
        private IDictionary<string, HashSet<string>> seriesTags = new ConcurrentDictionary<string, HashSet<string>>();

        public IndexBuilder(IIndexPersistence persistence, IIndexPersistence backup, IConfigManager config)
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
        }

        public void BuildIndex()
        {
            int lastId = 0;
            int postCount;
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
                tagRoot = client.GetPosts(urlToUse + $"tags.xml?search[category]=4&limit={TagsLimit}&page=a{lastId}&only=name,id,post_count").Result;
                tagNodes = tagRoot.SelectNodes("tag");

                for (int i = 0; i < tagNodes.Count; i++)
                {
                    tagName = tagNodes[i].SelectSingleNode("name").InnerText;

                    if (!tagName.Contains("#"))
                    {
                        postCount = int.Parse(tagNodes[i].SelectSingleNode("post-count").InnerText);
                        tagLinks.TryAdd(tagName, new List<string>(postCount));

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
            //while (j < 1000);

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

            j = 0;

            foreach (var l in limits)
            {
                threads[j] = new Thread(() => GetLinksForTag(l.Item1, l.Item2));
                threads[j].Name = j.ToString();
                threads[j].Start();

                j++;
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            Console.Clear();
            Console.WriteLine("Backing up data...");

            _backup.CleanTagLinks();
            _backup.PersistTagLinks(tagLinks);

            _backup.CleanSeries();
            _backup.PersistSeriesTags(seriesTags);

            Console.Clear();
            Console.WriteLine("Writing to database...");

            _persistence.CleanTagLinks();
            _persistence.PersistTagLinks(tagLinks);
            
            _persistence.CleanSeries();
            _persistence.PersistSeriesTags(seriesTags);

            _persistence.CountTagLinks();
            _persistence.CountSeriesLinks();

            SwitchDatabase();

            watch.Stop();

            TimeSpan timespan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
            string timeString = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);

            Resources.SystemLogger.Log($"Downloaded {tagLinks.Keys.Count} tags in {timeString} using {numThreads} thread(s).");
        }

        public void BackupRestore()
        {
            Console.Clear();
            Console.WriteLine("Restoring from backup...");

            tagLinks = _backup.GetTagIndex();

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

        public void Count()
        {
            Console.Clear();

            _persistence.CountTagLinks();
            _persistence.CountSeriesLinks();
        }

        private void GetLinksForTag(int start, int end)
        {
            int lastId = 0;
            int j = 1;
            int l = 1;
            int backoffSeconds;
            string tagName;
            string output;
            string path;
            bool noIdsLeft = false;
            XmlElement postRoot;
            XmlNodeList postNodes = null;
            XmlNode fileNode;
            XmlNode idNode;
            XmlNode seriesNode;
            IDictionary<string, int> tagOccurrences = new Dictionary<string, int>();
            List<string> topSeries;

            for (int i = start; i <= end; i++)
            {
                tagName = tagLinks.Keys.ElementAt(i);
                
                do
                {
                    try
                    {
                        backoffSeconds = 10;
                        output = $"Thread {int.Parse(Thread.CurrentThread.Name)}, processing tag '{tagName}' ({i - start + 1} / {end - start + 1}; page #{j}).";

                        PrintUtils.PrintRow(output, 0, int.Parse(Thread.CurrentThread.Name));

                        path = urlToUse + $"posts.xml?tags={tagName} rating:safe&limit={PostsLimit}&page=a{lastId}&only=id,file_url,tag_string_copyright";
                        postRoot = client.GetPosts(path).Result;

                        // Keep trying to fetch posts if the first request fails. Wait for a doubling backoff-period.
                        while (postRoot == null && backoffSeconds <= BackoffLimitSeconds)
                        {
                            output = $"Thread {int.Parse(Thread.CurrentThread.Name)} (Stalled; backoff: {backoffSeconds}), processing tag '{tagName}' ({i - start + 1} / {end - start + 1}; page #{j}).";

                            PrintUtils.PrintRow(output, 0, int.Parse(Thread.CurrentThread.Name));
                            Thread.Sleep(backoffSeconds * 1000);

                            postRoot = client.GetPosts(path).Result;
                            backoffSeconds *= 2;
                        }

                        postNodes = postRoot.SelectNodes("post");

                        for (int k = 0; k < postNodes.Count; k++)
                        {
                            fileNode = postNodes[k].SelectSingleNode("file-url");
                            seriesNode = postNodes[k].SelectSingleNode("tag-string-copyright");

                            // If there is no file url, simply skip the post
                            if (fileNode != null)
                            {
                                tagLinks[tagName].Add(fileNode.InnerText);
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

                            // If there is no post ID, keep searching until one is found on the page or move on to the next tag
                            if (k == 0)
                            {
                                idNode = postNodes[k].SelectSingleNode("id");

                                while (idNode == null && l < postNodes.Count)
                                {
                                    idNode = postNodes[l].SelectSingleNode("id");

                                    l++;
                                }

                                if (idNode != null)
                                {
                                    lastId = int.Parse(idNode.InnerText);
                                }
                                else
                                {
                                    noIdsLeft = true;
                                }

                                l = 1;
                            }
                        }

                        j++;

                        if (noIdsLeft)
                        {
                            break;
                        }
                    }
                    catch (NullReferenceException e)
                    {
                        Resources.SystemLogger.Log($"Failed to retrieve page {j + 1} posts for tag {tagName}." + Environment.NewLine + e.StackTrace);
                    }
                }
                while (postNodes.Count != 0);

                topSeries = IndexUtils.GetTopSeries(ref tagOccurrences, SeriesLimit);

                foreach (string series in topSeries)
                {
                    seriesTags[series].Add(tagName);
                }

                j = 1;
                lastId = 0;
                noIdsLeft = false;
                tagOccurrences.Clear();

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
