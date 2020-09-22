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
        private Dictionary<string, string> tagsDict;
        private static Logger logger = new Logger("images_logs");
        private const string BooruUrl = "https://safebooru.org/";
        private const int HardLimit = 100;
        private const string BaseParams = "index.php?page=dapi&s=post&q=index";
        private const int ImgsToFetch = 20;
        private const long SizeOfMB = 1024 * 1024;
        private const long MaxImgSize = 7 * SizeOfMB;

        public BooruDownloader(IPersistenceManager pm, IConfigManager cm) : base(pm, cm)
        {
            client.BaseAddress = new Uri(BooruUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/xml"));

            tagsDict = new Dictionary<string, string>() 
            {
                { Constants.ImageCmdHandles[0], "lysithea_von_ordelia" },
                { Constants.ImageCmdHandles[1], "holo" },
                { Constants.ImageCmdHandles[2], "fenrir_(shingeki_no_bahamut)" },
                { Constants.ImageCmdHandles[3], "myuri_(spice_and_wolf)" },
                { Constants.ImageCmdHandles[4], "ookami_ryouko" },
                { Constants.ImageCmdHandles[5], "nagatoro" },
                { Constants.ImageCmdHandles[6], "velvet_crowe" }
            };
        }

        public void Download(string[] cmdHandles)
        {
            base.Download(cmdHandles, ApiQuery, ConvertToTag);

            logger.Log("Image downloads completed.");
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
            int dataSize = 0;

            using (WebClient webClient = new WebClient())
            {
                foreach (XmlElement element in selected)
                {
                    do
                    {
                        tempElement = element;

                        if (tried == true)
                        {
                            logger.Log($"Image of size greater than {MaxImgSize} encountered for tags {tags}. Actual image size = {dataSize}.");

                            random = RandomIndex(ref chosenRands, count);
                            tempElement = allElements[random];
                        }

                        fileUrl = tempElement.GetAttribute("file_url");
                        fileExt = "." + fileUrl.Split('.').Last();

                        data = webClient.DownloadData(fileUrl);
                        dataSize = data.Length;
                        tried = true;
                    }
                    while (dataSize > MaxImgSize); // Implement a fairly wide margin

                    stream = new MemoryStream(data);
                    image = new Bitmap(stream);

                    fileData.Add(new ImageData(fileExt, image));

                    tried = false;
                }
            }

            logger.Log($"Downloaded {selected.Count}/{ImgsToFetch} images for '{tags}'. Total number of images = {count}.");

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
