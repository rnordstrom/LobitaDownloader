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
        private Dictionary<string, string> tagsDict;
        private static HttpClient client = new HttpClient();
        private const string BooruUrl = "https://safebooru.org/";
        private const int HardLimit = 100;
        private const string BaseParams = "index.php?page=dapi&s=post&q=index";
        private const int ImgsToFetch = 20;
        private const long SizeOfMB = 1024 * 1024;

        public BooruDownloader(IPersistenceManager pm, IConfigManager cm) : base(pm, cm)
        {
            client.BaseAddress = new Uri(BooruUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/xml"));

            tagsDict = new Dictionary<string, string>() 
            {
                { Constants.CmdHandles[0], "lysithea_von_ordelia" },
                { Constants.CmdHandles[1], "holo" },
                { Constants.CmdHandles[2], "fenrir_(shingeki_no_bahamut)" },
                { Constants.CmdHandles[3], "myuri_(spice_and_wolf)" },
                { Constants.CmdHandles[4], "ookami_ryouko" },
                { Constants.CmdHandles[5], "nagatoro" },
                { Constants.CmdHandles[6], "velvet_crowe" }
            };
        }

        public void Download(string[] cmdHandles)
        {
            base.Download(cmdHandles, ApiQuery, ConvertToTag);
        }

        private static List<ImageInfo> ApiQuery(string tags)
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
            Random rand = new Random();
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
            List<ImageInfo> infos = new List<ImageInfo>();
            byte[] data;
            Stream stream;
            Bitmap image;
            bool tried = false;
            XmlElement tempElement;

            using (WebClient webClient = new WebClient())
            {
                foreach (XmlElement element in selected)
                {
                    do
                    {
                        tempElement = element;

                        if (tried == true)
                        {
                            random = RandomIndex(ref chosenRands, count);

                            tempElement = allElements[random];
                        }

                        fileUrl = tempElement.GetAttribute("file_url");
                        fileExt = "." + fileUrl.Split('.').Last();

                        data = webClient.DownloadData(fileUrl);
                        tried = true;
                    }
                    while (data.Length > 7 * SizeOfMB); // Implement a fairly wide margin

                    stream = new MemoryStream(data);
                    image = new Bitmap(stream);

                    infos.Add(new ImageInfo { FileExt = fileExt, Image = image });

                    tried = false;
                }
            }

            return infos;
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
