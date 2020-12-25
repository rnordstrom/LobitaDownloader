using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LobitaDownloader
{
    public class BooruDownloader : Downloader, IDownloader
    {
        private static HttpClient client = new HttpClient();
        private static List<string> banFilter;
        private Dictionary<string, string> tagsDict;
        private const string BooruUrl = "https://safebooru.org/";
        private const string TestBooruUrl = "https://testbooru.donmai.us/";
        private const string DanBooruUrl = "https://danbooru.donmai.us/";
        private const int HardLimit = 100;
        private const string BaseParams = "index.php?page=dapi&s=post&q=index";
        private const int ImgsToFetch = 20;
        private const int NumThreads = 16;
        private const int TagsLimit = 1000;
        private const int PostsLimit = 1000;

        public BooruDownloader(IPersistenceManager pm, IConfigManager cm) : base(pm, cm)
        {
            client.BaseAddress = new Uri(BooruUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/xml"));

            tagsDict = new Dictionary<string, string>() 
            {
                { Resources.ImageCmdHandles[0], "lysithea_von_ordelia" },
                { Resources.ImageCmdHandles[1], "holo" },
                { Resources.ImageCmdHandles[2], "fenrir_(shingeki_no_bahamut)" },
                { Resources.ImageCmdHandles[3], "myuri_(spice_and_wolf)" },
                { Resources.ImageCmdHandles[4], "ookami_ryouko" },
                { Resources.ImageCmdHandles[5], "nagatoro" },
                { Resources.ImageCmdHandles[6], "velvet_crowe" },
                { Resources.ImageCmdHandles[7], "hololive" }
            };

            banFilter = new List<string>();
            banFilter.Add("1880445");
        }

        public void Download(string[] cmdHandles)
        {
            base.Download(cmdHandles, ApiQuery, ConvertToTag);

            Resources.SystemLogger.Log("Image downloads completed.");
        }

        private static List<FileData> ApiQuery(string tags)
        {
            // XML element results are fetched from the API

            XmlElement posts = GetPosts(BaseParams + $"&tags={tags}").Result;
            XmlNodeList postList;
            List<XmlElement> allElements = new List<XmlElement>();

            int count = int.Parse(posts.GetAttribute("count"));
            int numPages = count / HardLimit;

            for (int pageNum = 0; pageNum <= numPages; pageNum++)
            {
                posts = GetPosts(BaseParams + $"&tags={tags}&pid={pageNum}").Result;
                postList = posts.SelectNodes("post");

                foreach (XmlElement post in postList)
                {
                    allElements.Add(post);
                }
            }

            // Images are selected, based on the number of desired images
            
            List<XmlElement> selected = new List<XmlElement>();
            int random;
            List<int> chosenRands = new List<int>();

            if(count < ImgsToFetch)
            {
                foreach (XmlElement element in allElements)
                {
                    selected.Add(element);
                }
            }
            else
            {
                for (int i = 0; i < ImgsToFetch; i++)
                {
                    random = RandomIndex(ref chosenRands, count);

                    selected.Add(allElements[random]);
                }
            }

            // Images are downloaded. Images must be smaller than 8MB.

            string fileUrl;
            string fileExt;
            List<FileData> fileData = new List<FileData>();
            byte[] data;
            Stream stream;
            Bitmap image;
            bool tried = false;
            XmlElement tempElement;
            string id = " ";

            using (WebClient webClient = new WebClient())
            {
                foreach (XmlElement element in selected)
                {
                    tempElement = element;

                    do
                    {
                        if (tried == true)
                        {
                            Resources.ImageLogger.Log($"Banned image encountered for tags '{tags}'. ID = {id}.");

                            random = RandomIndex(ref chosenRands, count);
                            tempElement = allElements[random];
                        }

                        id = tempElement.GetAttribute("id");
                        tried = true;
                    }
                    while (banFilter.Contains(id));

                    fileUrl = tempElement.GetAttribute("file_url");
                    fileExt = "." + fileUrl.Split('.').Last();
                    data = webClient.DownloadData(fileUrl);
                    stream = new MemoryStream(data);
                    image = new Bitmap(stream);

                    fileData.Add(new ImageData(fileExt, image, id));

                    tried = false;
                }
            }

            Resources.ImageLogger.Log($"Downloaded {selected.Count}/{ImgsToFetch} images for '{tags}'. Total number of images = {count}.");

            return fileData;
        }

        public void BuildIndex()
        {
            int lastId = 0;
            string tagName;
            ConcurrentDictionary<string, List<string>> index = new ConcurrentDictionary<string, List<string>>();
            XmlElement tagRoot;
            XmlNodeList tagNodes;

            Console.WriteLine("Building index...");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            do
            {
                tagRoot = GetPosts(DanBooruUrl + $"tags.xml?search[category]=4&limit={TagsLimit}&page=a{lastId}").Result;
                tagNodes = tagRoot.SelectNodes("tag");

                for (int i = 0; i < tagNodes.Count; i++)
                {
                    tagName = tagNodes[i].SelectSingleNode("name").InnerText;

                    if (!tagName.Contains("#"))
                    {
                        index.TryAdd(tagName, new List<string>());
                        Console.WriteLine($"Adding tag {tagName}.");
                    }

                    if (i == 0)
                    {
                        lastId = int.Parse(tagNodes[i].SelectSingleNode("id").InnerText);
                    }
                }
            }
            while (tagNodes.Count != 0);
            //while (index.Keys.Count < TagsLimit);

            int partitionSize = (int) Math.Round((double) index.Keys.Count / NumThreads);
            Thread[] threads = new Thread[NumThreads];
            Tuple<int, int>[] limits = new Tuple<int, int>[NumThreads];

            for (int i = 0; i < NumThreads; i++)
            {
                if (i == NumThreads - 1)
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
            Console.WriteLine("Writing data to document...");

            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement tagsElement = doc.CreateElement(string.Empty, "tags", string.Empty);
            doc.AppendChild(tagsElement);

            XmlElement tagElement;
            XmlElement linkElement;
            XmlText linkText;

            foreach (var t in index.Keys)
            {
                tagElement = doc.CreateElement(string.Empty, "tag", string.Empty);
                tagElement.SetAttribute("name", t);

                foreach (var l in index[t])
                {
                    linkElement = doc.CreateElement(string.Empty, "link", string.Empty);
                    linkText = doc.CreateTextNode(l);

                    linkElement.AppendChild(linkText);
                    tagElement.AppendChild(linkElement);
                }

                tagsElement.AppendChild(tagElement);
            }

            doc.Save(Path.Join(Resources.WorkingDirectory, "index.xml"));

            watch.Stop();

            TimeSpan timespan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
            string timeString = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);

            Resources.SystemLogger.Log($"Processed {index.Keys.Count} tags in {timeString} using {NumThreads} thread(s).");
        }

        private void GetLinksForTag(ref ConcurrentDictionary<string, List<string>> index, int start, int end)
        {
            int lastId = 0;
            int j = 1;
            int l = 1;
            int nullIdCount = 0;
            int nullFileCount = 0;
            int windowRemainder = 0;
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

                        postRoot = GetPosts(path).Result;

                        // Keep trying to fetch posts if the first request fails
                        while (postRoot == null)
                        {
                            output = $"Thread {int.Parse(Thread.CurrentThread.Name)} (stalled), processing tag '{tagName}' ({i - start + 1} out of {end - start + 1}; page #{j}).";

                            Console.SetCursorPosition(0, int.Parse(Thread.CurrentThread.Name));
                            Console.Write(output + new string(' ', windowRemainder));

                            postRoot = GetPosts(path).Result;
                        }

                        postNodes = postRoot.SelectNodes("post");

                        for (int k = 0; k < postNodes.Count; k++)
                        {
                            fileNode = postNodes[k].SelectSingleNode("file-url");

                            // If there is no file url, simply skip the post
                            if (fileNode != null)
                            {
                                index[tagName].Add(fileNode.InnerText);
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
            for (int i = NumThreads; i < NumThreads + 10; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth));
            }
        }

        private string ConvertToTag(string cmdHandle) => tagsDict[cmdHandle];

        private static async Task<XmlElement> GetPosts(string path)
        {
            XmlElement result = null;
            HttpResponseMessage response = await client.GetAsync(path);

            if(response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadAsAsync<XmlElement>();
            }

            return result;
        }

        // Selects a random index for a list. Indices that have been selected previously may not be selected again.
        private static int RandomIndex(ref List<int> chosenRands, int max)
        {
            Random rand = new Random();
            int random = rand.Next(0, max - 1);

            while (chosenRands.Contains(random))
            {
                random = rand.Next(0, max - 1);
            }

            chosenRands.Add(random);

            return random;
        }
    }
}
