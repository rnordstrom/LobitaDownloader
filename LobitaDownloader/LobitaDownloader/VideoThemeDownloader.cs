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

            Resources.SystemLogger.Log("Video downloads completed.");
        }

        private List<FileData> HtmlQuery(string themeType)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc;
            HtmlNode videoNode;
            
            string videoSrc;
            string extractedName;
            string fileExt = "." + FileExt;
            string fileName;
            byte[] data;
            List<FileData> videoData = new List<FileData>();

            // Collect video links and download corresponding video files

            using (WebClient webClient = new WebClient())
            {
                for (int i = 0; i < VideosToFetch; i++)
                {
                    do
                    {
                        doc = web.Load(MoeUrl);
                        videoNode = doc.GetElementbyId("bgvid")
                            .SelectSingleNode($"//source[@type = 'video/{FileExt}']");
                        videoSrc = videoNode.GetAttributeValue("src", null);
                        extractedName = videoSrc.Split("/")[1].Split(".")[0];

                        Thread.Sleep(WaitInterval);
                    }
                    while (!extractedName.Contains("-" + themeType));

                    fileName = RemoveSpecialChars(extractedName);
                    data = webClient.DownloadData(MoeUrl + videoSrc);

                    videoData.Add(new VideoData(fileExt, fileName, data));
                }
            }

            Resources.VideoLogger.Log($"Downloaded {videoData.Count}/{VideosToFetch} videos for '{themeType}'.");

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
