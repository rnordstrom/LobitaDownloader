using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net;

namespace LobitaDownloader
{
    class VideoThemeDownloader : Downloader, IDownloader
    {
        private const string MoeUrl = "https://openings.moe/";
        private const string FileExt = "mp4";
        private const int VideosToFetch = 5;

        public VideoThemeDownloader(IPersistenceManager pm, IConfigManager cm) : base(pm, cm) { }

        public void Download(string[] cmdHandles)
        {
            base.Download(cmdHandles, HtmlQuery, ToParam);
        }

        private List<FileData> HtmlQuery(string themeType)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc;
            HtmlNode videoNode;
            List<HtmlNode> nodeList = new List<HtmlNode>();

            for (int i = 0; i < VideosToFetch; i++)
            {
                doc = web.Load(MoeUrl);

                videoNode = doc.GetElementbyId("bgvid")
                    .SelectSingleNode($"//source[@type = 'video/{FileExt}']");

                nodeList.Add(videoNode);
            }

            string videoSrc;
            string fileName;
            string fileExt = "." + FileExt;
            byte[] data;
            List<FileData> videoData = new List<FileData>();

            using (WebClient webClient = new WebClient())
            {
                foreach (HtmlNode node in nodeList)
                {
                    videoSrc = node.GetAttributeValue("src", null);
                    fileName = videoSrc.Split("/")[1].Split(".")[0];
                    data = webClient.DownloadData(MoeUrl + videoSrc);

                    videoData.Add(new VideoData(fileExt, fileName, data));
                }
            }

            return videoData;
        }

        private string ToParam(string handle) => handle;
    }
}
