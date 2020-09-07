﻿using System;
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
        private const string booruUrl = "https://safebooru.org/";
        private const int hardLimit = 100;
        private const string baseParams = "index.php?page=dapi&s=post&q=index";
        private const int softLimit = 20;

        public BooruDownloader(IPersistenceManager pm, IConfigManager cm) : base(pm, cm)
        {
            client.BaseAddress = new Uri(booruUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/xml"));

            tagsDict = new Dictionary<string, string>() 
            {
                {Constants.CmdHandles[0], "lysithea_von_ordelia"},
                {Constants.CmdHandles[1], "holo"},
                {Constants.CmdHandles[2], "fenrir_(shingeki_no_bahamut)"},
                {Constants.CmdHandles[3], "myuri_(spice_and_wolf)"},
                {Constants.CmdHandles[4], "ookami_ryouko"},
                {Constants.CmdHandles[5], "nagatoro"}
            };
        }

        public void Download(string[] cmdHandles)
        {
            base.Download(cmdHandles, ApiQuery, ConvertToTag);
        }

        private static List<ImageInfo> ApiQuery(string tags)
        {
            XmlElement posts = GetPosts(baseParams + $"&tags={tags}").Result;

            int count = int.Parse(posts.GetAttribute("count"));
            int numPages = count / hardLimit;

            Random rand = new Random();
            int pageNum = rand.Next(0, numPages);

            posts = GetPosts(baseParams + $"&tags={tags}&pid={pageNum}").Result;
            string fileUrl;
            string fileExt;
            List<ImageInfo> infos = new List<ImageInfo>();
            WebClient webClient = new WebClient();
            Stream stream;
            Bitmap image;

            int perPage = posts.ChildNodes.Count;
            int actualLimit = perPage < hardLimit ? perPage : hardLimit;
            int fetchOffset = rand.Next(0, (actualLimit - softLimit) - 1);
            int fetchLimit = fetchOffset + softLimit;
            int i = fetchOffset;

            foreach (XmlElement post in posts.SelectNodes("post"))
            {
                if(i >= fetchOffset && i < fetchLimit)
                {
                    fileUrl = post.GetAttribute("file_url");
                    fileExt = "." + fileUrl.Split('.').Last();

                    stream = webClient.OpenRead(fileUrl);
                    image = new Bitmap(stream);

                    infos.Add(new ImageInfo { FileExt = fileExt, Image = image });

                    stream.Close();
                }
                else if(i >= fetchLimit)
                {
                    break;
                }

                i++;
            }

            webClient.Dispose();

            return infos;
        }

        private string ConvertToTag(string cmdHandle) => tagsDict[cmdHandle];

        private static async Task<XmlElement> GetPosts(string path)
        {
            XmlElement result = null;
            HttpResponseMessage response = await client.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadAsAsync<XmlElement>();
            }

            return result;
        }
    }
}
