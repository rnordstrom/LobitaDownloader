using LobitaDownloader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LobitaDownloaderTest
{
    [TestClass]
    public class LoggerTest
    {
        [TestMethod]
        public void TestLog()
        {
            DeleteFiles(Logger.LogDirectory.GetFiles());
            Assert.IsTrue(Logger.LogDirectory.GetFiles().Length == 0);

            Logger.Log("This is LobitaDownloader!");

            int extLength = Logger.FileExt.Length;

            FileInfo[] files = Logger.LogDirectory.GetFiles();
            Assert.IsTrue(files.Length == 1);
            Assert.AreEqual(DateTime.Parse(files[0].Name.Substring(0, files[0].Name.Length - extLength)), DateTime.Today.Date);

            using (StreamReader reader = new StreamReader(new FileInfo(files[0].FullName).OpenRead()))
            {
                Console.WriteLine(reader.ReadToEnd());
            }

            DeleteFiles(files);
        }

        [TestMethod]
        public void TestCleanDirectory()
        {
            DeleteFiles(Logger.LogDirectory.GetFiles());
            Assert.IsTrue(Logger.LogDirectory.GetFiles().Length == 0);

            const int NumFiles = 33;
            const int DesiredNumFiles = 30;
            const int difference = NumFiles - DesiredNumFiles;
            FileInfo[] filesBefore = new FileInfo[NumFiles];
            DateTime dt = DateTime.Today.Date;

            for (int i = 0; i < NumFiles; i++)
            {
                filesBefore[i] = new FileInfo(Path.Join(Logger.LogDirectory.FullName, dt.AddDays(i).ToShortDateString() + Logger.FileExt));
                using (FileStream fs = filesBefore[i].Create()) { };
                Console.WriteLine(filesBefore[i].Name);
            }

            Console.WriteLine();

            Assert.IsTrue(Logger.LogDirectory.GetFiles().Length > DesiredNumFiles);

            Logger.CleanDirectory();

            int extLength = Logger.FileExt.Length;
            FileInfo[] filesAfter = Logger.LogDirectory.GetFiles();
            Assert.IsTrue(filesAfter.Length == DesiredNumFiles);

            for (int i = 0; i < difference; i++)
            {
                Console.WriteLine(filesAfter[i].Name);
                Assert.IsFalse(filesAfter.Any(
                    x => DateTime.Parse(x.Name.Substring(0, x.Name.Length - extLength))
                        .CompareTo(dt.AddDays(i)) == 0));
            }

            DeleteFiles(filesAfter);
        }

        private void DeleteFiles(FileInfo[] files)
        {
            foreach (FileInfo file in files)
            {
                file.Delete();
            }
        }
    }
}
