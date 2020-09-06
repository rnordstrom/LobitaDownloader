using LobitaDownloader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
        private List<ImageInfo> TestQuery(List<string> qParams)
        {
            string fileExt = ".dat";
            byte[] bytes = new byte[5] { 1, 1, 1, 1, 1 };
            ImageInfo info1 = new ImageInfo { FileExt = fileExt, Bytes = bytes };
            ImageInfo info2 = new ImageInfo { FileExt = fileExt, Bytes = bytes };

            return new List<ImageInfo>() { info1, info2 };
        }
    }
}
