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
        private FolderImageManager fm = new FolderImageManager();

        [TestMethod]
        public void TestPersist()
        {
            string fileExt = ".png";
            Bitmap image = new Bitmap(10, 10);
            ImageData info = new ImageData(fileExt, image);
            List<FileData> infos = new List<FileData>() { info };
            FileInfo[] files;
            int count = 1;

            foreach (string handle in Constants.ImageCmdHandles)
            {
                fm.Persist(handle, infos);
            }

            DirectoryInfo[] directories = fm.DataDirectory.GetDirectories();
            Assert.AreEqual(Constants.ImageCmdHandles.Length, directories.Length);

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
