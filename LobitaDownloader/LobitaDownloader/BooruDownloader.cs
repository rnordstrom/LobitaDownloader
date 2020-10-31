using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private const int HardLimit = 100;
        private const string BaseParams = "index.php?page=dapi&s=post&q=index";
        private const int ImgsToFetch = 20;

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
