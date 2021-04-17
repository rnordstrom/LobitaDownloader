using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;

namespace LobitaDownloader.Tests
{
    [TestClass]
    [Ignore]
    public class DownloaderTests
    {
        [TestMethod]
        public void TestDownload()
        {
            IPersistenceManager pm = new FolderImageManager();
            IConfigManager cm = new XmlConfigManager(Resources.TestDirectory, Resources.ConfigFile);
            TestDownloader testDL = new TestDownloader(pm, cm);

            testDL.Download(Resources.ImageCmdHandles);
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
        private List<FileData> TestQuery(string qParam)
        {
            string fileExt = ".png";
            Bitmap image = new Bitmap(10, 10); 
            ImageData info1 = new ImageData(fileExt, image, "1234");
            ImageData info2 = new ImageData(fileExt, image, "1235");

            return new List<FileData>() { info1, info2 };
        }
    }
}
