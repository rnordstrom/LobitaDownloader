using Microsoft.VisualStudio.TestTools.UnitTesting;
using LobitaDownloader;
using System;
using System.Collections.Generic;
using System.IO;

namespace LobitaDownloaderTest
{
    [TestClass]
    public class PersistenceTests
    {
        private FolderManager fm = new FolderManager();

        [TestMethod]
        public void TestCheckLastUpdate()
        {
            foreach (string handle in Constants.CmdHandles)
            {
                if(Directory.Exists(fm.DataDirectory.FullName))
                {
                    Assert.IsTrue(fm.CheckLastUpdate(handle) < DateTime.Now);
                }
                else
                {
                    Assert.AreEqual(fm.CheckLastUpdate(handle), DateTime.MinValue);
                }
            }
        }

        [TestMethod]
        public void TestPersist()
        {
            string fileExt = ".dat";
            byte[] bytes = new byte[5] { 1, 1, 1, 1, 1 };
            ImageInfo info = new ImageInfo { FileExt = fileExt, Bytes = bytes };
            List<ImageInfo> infos = new List<ImageInfo>() { info };

            DirectoryInfo[] directories = fm.DataDirectory.GetDirectories();
            Assert.AreEqual(Constants.CmdHandles.Length, directories.Length);
            
            FileInfo[] files;
            int count = 1;

            // Delete all existing files before writes and asserts
            CleanUp(directories);

            foreach (string handle in Constants.CmdHandles)
            {
                fm.Persist(handle, infos);
            }

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
                    Assert.AreEqual(f.Length, bytes.Length);
                }
            }
        }

        private void CleanUp(DirectoryInfo[] dirs)
        {
            FileInfo[] files;

            foreach (DirectoryInfo d in dirs)
            {
                files = d.GetFiles();

                foreach (FileInfo f in files)
                {
                    f.Delete();
                }
            }
        }
    }
}
