using LobitaDownloader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;

namespace LobitaDownloaderTest
{
    [TestClass]
    public class DownloaderTests
    {
        [TestMethod]
        public void TestDownload()
        {
            IPersistenceManager pm = new FolderManager();
            IConfigManager cm = new XmlManager();
            TestDownloader testDL = new TestDownloader(pm, cm);

            testDL.Download(Constants.CmdHandles);
        }
    }

    public class TestDownloader : Downloader, IDownloader
    {
        public TestDownloader(IPersistenceManager pm, IConfigManager cm) : base(pm, cm) { }

        public void Download(string[] cmdHandles)
        {
            base.Download(cmdHandles, TestQuery, TestConvert);
        }

        private string TestConvert(string cmdHandle)
        {
            return cmdHandle;
        }

        // Mock API-call
        private List<ImageInfo> TestQuery(string qParam)
        {
            string fileExt = ".png";
            Bitmap image = new Bitmap(10, 10); 
            ImageInfo info1 = new ImageInfo { FileExt = fileExt, Image = image };
            ImageInfo info2 = new ImageInfo { FileExt = fileExt, Image = image };

            return new List<ImageInfo>() { info1, info2 };
        }
    }
}
