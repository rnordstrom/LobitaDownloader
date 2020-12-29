﻿using System;
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
        private IPersistenceManager persistence;
        private HttpXmlClient client;
        private const string TestBooruUrl = "https://testbooru.donmai.us/";
        private const string DanBooruUrl = "https://danbooru.donmai.us/";
        private int numThreads = 0;
        private const int TagsLimit = 1000;
        private const int PostsLimit = 1000;

        public IndexBuilder(IPersistenceManager pm)
        {
            persistence = pm;
            client = new HttpXmlClient(DanBooruUrl);

            numThreads = int.Parse(Environment.GetEnvironmentVariable("NUM_THREADS"));
        }

        public void BuildIndex()
        {
            ConcurrentDictionary<string, List<FileData>> index = new ConcurrentDictionary<string, List<FileData>>();
            int lastId = 0;
            string tagName;
            XmlElement tagRoot;
            XmlNodeList tagNodes;

            Console.WriteLine("Building index...");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            do
            {
                tagRoot = client.GetPosts(DanBooruUrl + $"tags.xml?search[category]=4&limit={TagsLimit}&page=a{lastId}").Result;
                tagNodes = tagRoot.SelectNodes("tag");

                for (int i = 0; i < tagNodes.Count; i++)
                {
                    tagName = tagNodes[i].SelectSingleNode("name").InnerText;

                    if (!tagName.Contains("#"))
                    {
                        index.TryAdd(tagName, new List<FileData>());
                        Console.WriteLine($"Adding tag {tagName}.");
                    }

                    if (i == 0)
                    {
                        lastId = int.Parse(tagNodes[i].SelectSingleNode("id").InnerText);
                    }
                }
            }
            while (tagNodes.Count != 0);

            int partitionSize = (int)Math.Round((double)index.Keys.Count / numThreads);
            Thread[] threads = new Thread[numThreads];
            Tuple<int, int>[] limits = new Tuple<int, int>[numThreads];

            for (int i = 0; i < numThreads; i++)
            {
                if (i == numThreads - 1)
                {
                    limits[i] = new Tuple<int, int>(partitionSize * i, index.Keys.Count - 1);
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
                threads[j] = new Thread(() => GetLinksForTag(ref index, l.Item1, l.Item2));
                threads[j].Name = j.ToString();
                threads[j].Start();

                j++;
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            Console.Clear();
            Console.WriteLine("Writing to database...");

            persistence.Clean();
            persistence.PersistBatch(index);

            watch.Stop();

            TimeSpan timespan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
            string timeString = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);

            Resources.SystemLogger.Log($"Downloaded {index.Keys.Count} tags in {timeString} using {numThreads} thread(s).");
        }

        private void GetLinksForTag(ref ConcurrentDictionary<string, List<FileData>> index, int start, int end)
        {
            int lastId = 0;
            int j = 1;
            int l = 1;
            int nullIdCount = 0;
            int nullFileCount = 0;
            int windowRemainder;
            string tagName;
            string output;
            string path;
            bool noIdsLeft = false;
            XmlElement postRoot;
            XmlNodeList postNodes;
            XmlNode fileNode;
            XmlNode idNode;

            for (int i = start; i <= end; i++)
            {
                tagName = index.Keys.ElementAt(i);

                try
                {
                    do
                    {
                        output = $"Thread {int.Parse(Thread.CurrentThread.Name)}, processing tag '{tagName}' ({i - start + 1} out of {end - start + 1}; page #{j}).";
                        windowRemainder = Console.WindowWidth - output.Length;

                        if (windowRemainder < 0)
                        {
                            windowRemainder = 0;
                        }

                        Console.SetCursorPosition(0, int.Parse(Thread.CurrentThread.Name));
                        Console.Write(output + new string(' ', windowRemainder));

                        path = DanBooruUrl + $"posts.xml?tags={tagName} rating:safe&limit={PostsLimit}&page=a{lastId}";

                        postRoot = client.GetPosts(path).Result;

                        // Keep trying to fetch posts if the first request fails
                        while (postRoot == null)
                        {
                            output = $"Thread {int.Parse(Thread.CurrentThread.Name)} (stalled), processing tag '{tagName}' ({i - start + 1} out of {end - start + 1}; page #{j}).";

                            Console.SetCursorPosition(0, int.Parse(Thread.CurrentThread.Name));
                            Console.Write(output + new string(' ', windowRemainder));

                            postRoot = client.GetPosts(path).Result;
                        }

                        postNodes = postRoot.SelectNodes("post");

                        for (int k = 0; k < postNodes.Count; k++)
                        {
                            fileNode = postNodes[k].SelectSingleNode("file-url");

                            // If there is no file url, simply skip the post
                            if (fileNode != null)
                            {
                                index[tagName].Add(new LinkData(fileNode.InnerText));
                            }
                            else
                            {
                                nullFileCount++;
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
                                    nullIdCount++;

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
                    while (postNodes.Count != 0);
                }
                catch (NullReferenceException e)
                {
                    Resources.SystemLogger.Log($"Failed to retrieve page {j + 1} posts for tag {tagName}.\n" + e.StackTrace);
                }

                if (nullIdCount > 0)
                {
                    Resources.SystemLogger.Log($"Encountered {nullIdCount} instances of null ID for tag {tagName}.");
                }

                if (nullFileCount > 0)
                {
                    Resources.SystemLogger.Log($"Encountered {nullFileCount} instances of null file URL for tag {tagName}.");
                }

                j = 1;
                lastId = 0;
                nullIdCount = 0;
                nullFileCount = 0;
                noIdsLeft = false;


                ClearBelow();
            }
        }

        private void ClearBelow()
        {
            for (int i = numThreads; i < numThreads + 10; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth));
            }
        }
    }
}
