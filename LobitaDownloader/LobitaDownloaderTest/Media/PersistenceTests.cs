using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace LobitaDownloader.Tests
{
    [TestClass]
    public class PersistenceTests
    {
        private FolderImageManager fim = new FolderImageManager();
        private FolderVideoManager fvm = new FolderVideoManager();

        [TestMethod]
        public void TestImagePersist()
        {
            string fileExt = ".png";
            Bitmap image = new Bitmap(10, 10);
            ImageData imgData = new ImageData(fileExt, image, "1111");
            List<FileData> imgList = new List<FileData>() { imgData };
            FileInfo[] files;
            int count = 1;

            foreach (string handle in Resources.ImageCmdHandles)
            {
                fim.Persist(handle, imgList);
            }

            DirectoryInfo[] directories = fim.DataDirectory.GetDirectories();
            Assert.AreEqual(Resources.ImageCmdHandles.Length, directories.Length);

            Assert.IsTrue(Directory.Exists(fim.DataDirectory.FullName));
            Directory.SetCurrentDirectory(fim.DataDirectory.FullName);

            foreach (DirectoryInfo d in directories)
            {
                Directory.SetCurrentDirectory(d.FullName);
                files = d.GetFiles();
                Assert.AreEqual(imgList.Count, files.Length);

                foreach (FileInfo f in files)
                {
                    Assert.AreEqual(f.Name, count.ToString() + fileExt);
                }
            }
        }

        [TestMethod]
        public void TestVideoPersist()
        {
            string fileExt = ".mp4";
            string fileName = "video";
            byte[] data = new byte[5];
            VideoData videoData = new VideoData(fileExt, fileName, data);
            List<FileData> videoList = new List<FileData>() { videoData };
            FileInfo[] files;

            foreach (string handle in Resources.VideoCmdHandles)
            {
                fvm.Persist(handle, videoList);
            }

            DirectoryInfo[] directories = fvm.DataDirectory.GetDirectories();
            Assert.AreEqual(Resources.VideoCmdHandles.Length, directories.Length);

            Assert.IsTrue(Directory.Exists(fvm.DataDirectory.FullName));
            Directory.SetCurrentDirectory(fvm.DataDirectory.FullName);

            foreach (DirectoryInfo d in directories)
            {
                Directory.SetCurrentDirectory(d.FullName);
                files = d.GetFiles();
                Assert.AreEqual(videoList.Count, files.Length);

                foreach (FileInfo f in files)
                {
                    Assert.AreEqual(f.Name, fileName + fileExt);
                }
            }
        }
    }
}
