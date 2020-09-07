using Microsoft.VisualStudio.TestTools.UnitTesting;
using LobitaDownloader;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace LobitaDownloaderTest
{
    [TestClass]
    public class PersistenceTests
    {
        private FolderManager fm = new FolderManager();

        [TestMethod]
        public void TestPersist()
        {
            string fileExt = ".png";
            Bitmap image = new Bitmap(10, 10);
            ImageInfo info = new ImageInfo { FileExt = fileExt, Image = image };
            List<ImageInfo> infos = new List<ImageInfo>() { info };
            FileInfo[] files;
            int count = 1;

            foreach (string handle in Constants.CmdHandles)
            {
                fm.Persist(handle, infos);
            }

            DirectoryInfo[] directories = fm.DataDirectory.GetDirectories();
            Assert.AreEqual(Constants.CmdHandles.Length, directories.Length);

            Assert.IsTrue(Directory.Exists(fm.DataDirectory.FullName));
            Directory.SetCurrentDirectory(fm.DataDirectory.FullName);

            foreach (DirectoryInfo d in directories)
            {
                Directory.SetCurrentDirectory(d.FullName);
                files = d.GetFiles();
                Assert.AreEqual(infos.Count, files.Length);

                foreach (FileInfo f in files)
                {
                    Assert.AreEqual(f.Name, count.ToString() + fileExt);
                }
            }
        }
    }
}
