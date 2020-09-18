using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace LobitaDownloader
{
    class VideoThemeDownloader : Downloader, IDownloader
    {
        private const string MoeUrl = "https://openings.moe/";
        private const string FileExt = "mp4";
        private const int VideosToFetch = 5;
        private const int WaitInterval = 5000;

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
                Thread.Sleep(WaitInterval);
            }

            string videoSrc;
            string fileName;
            string extractedName;
            string fileExt = "." + FileExt;
            byte[] data;
            List<FileData> videoData = new List<FileData>();

            using (WebClient webClient = new WebClient())
            {
                foreach (HtmlNode node in nodeList)
                {
                    videoSrc = node.GetAttributeValue("src", null);
                    extractedName = videoSrc.Split("/")[1].Split(".")[0];
                    fileName = RemoveSpecialChars(extractedName);
                    data = webClient.DownloadData(MoeUrl + videoSrc);

                    videoData.Add(new VideoData(fileExt, fileName, data));
                }
            }

            return videoData;
        }

        private string ToParam(string handle) => handle;

        private string RemoveSpecialChars(string s)
        {
            string toRemove = "%";

            while (s.Contains(toRemove))
            {
                s = s.Remove(s.IndexOf(toRemove), 3);
            }

            return s;
        }
    }
}
