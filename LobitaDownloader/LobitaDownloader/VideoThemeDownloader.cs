using System;
using System.Collections.Generic;

namespace LobitaDownloader
{
    class VideoThemeDownloader : Downloader, IDownloader
    {
        private const string MoeUrl = "https://openings.moe/";

        public VideoThemeDownloader(IPersistenceManager pm, IConfigManager cm) : base(pm, cm)
        {

        }

        public void Download(string[] cmdHandles)
        {
            base.Download(cmdHandles, HtmlQuery, ToParam);
        }

        private List<FileData> HtmlQuery(string themeType)
        {
            throw new NotImplementedException();
        }

        private string ToParam(string handle) => handle;
    }
}
