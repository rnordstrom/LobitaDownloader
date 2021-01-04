using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LobitaDownloader.Tests
{
    [TestClass]
    public class LoggerTest
    {
        private Logger logger = new Logger("images_logs");

        [TestMethod]
        public void TestLog()
        {
            DeleteFiles(logger.LogDirectory.GetFiles());
            Assert.IsTrue(logger.LogDirectory.GetFiles().Length == 0);

            try
            {
                throw new InvalidOperationException("You wouldn't download a lobita...");
            }
            catch(Exception e)
            {
                logger.Log(e);
            }

            int extLength = Logger.FileExt.Length;

            FileInfo[] files = logger.LogDirectory.GetFiles();
            Assert.IsTrue(files.Length == 1);
            Assert.AreEqual(DateTime.Parse(files[0].Name.Substring(0, files[0].Name.Length - extLength)), DateTime.Today.Date);

            using (StreamReader reader = new StreamReader(new FileInfo(files[0].FullName).OpenRead()))
            {
                Console.WriteLine(reader.ReadToEnd());
            }
        }

        [TestMethod]
        public void TestCleanDirectory()
        {
            DeleteFiles(logger.LogDirectory.GetFiles());
            Assert.IsTrue(logger.LogDirectory.GetFiles().Length == 0);

            const int NumFiles = 33;
            const int DesiredNumFiles = 30;
            const int difference = NumFiles - DesiredNumFiles;
            FileInfo[] filesBefore = new FileInfo[NumFiles];
            DateTime dt = DateTime.Today.Date;

            for (int i = 0; i < NumFiles; i++)
            {
                filesBefore[i] = new FileInfo(Path.Join(logger.LogDirectory.FullName, dt.AddDays(i).ToShortDateString() + Logger.FileExt));
                using (FileStream fs = filesBefore[i].Create()) { };
                Console.WriteLine(filesBefore[i].Name);
            }

            Console.WriteLine();

            Assert.IsTrue(logger.LogDirectory.GetFiles().Length > DesiredNumFiles);

            logger.CleanDirectory();

            int extLength = Logger.FileExt.Length;
            FileInfo[] filesAfter = logger.LogDirectory.GetFiles();
            Assert.IsTrue(filesAfter.Length == DesiredNumFiles);

            for (int i = 0; i < difference; i++)
            {
                Console.WriteLine(filesAfter[i].Name);
                Assert.IsFalse(filesAfter.Any(
                    x => DateTime.Parse(x.Name.Substring(0, x.Name.Length - extLength))
                        .CompareTo(dt.AddDays(i)) == 0));
            }
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
