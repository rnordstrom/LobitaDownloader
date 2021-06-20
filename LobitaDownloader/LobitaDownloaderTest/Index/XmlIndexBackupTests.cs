using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace LobitaDownloader.Tests
{
    [TestClass()]
    public class XmlIndexBackupTests
    {
        private XmlIndexBackup backup;
        private Dictionary<string, List<string>> tagLinks = new Dictionary<string, List<string>>();
        private Dictionary<string, HashSet<string>> seriesTags = new Dictionary<string, HashSet<string>>();
        string tag1 = "gawr_gura";
        string tag2 = "ninomae_ina'nis";
        string series = "hololive";

        [TestInitialize]
        public void Setup()
        {
            IConfigManager configManager = new XmlConfigManager(Resources.TestDirectory, Resources.ConfigFile);
            backup = new XmlIndexBackup(configManager);

            tagLinks.Add(tag1, new List<string>() { "1.png", "2.png", "3.jpg" });
            tagLinks.Add(tag2, new List<string>() { "4.png", "5.png" });
            seriesTags.Add(series, new HashSet<string>() { tag1, tag2 });
        }

        [TestMethod]
        public void BackupNamesAndReadTest()
        {
            List<string> tagNames = new List<string>();
            List<string> seriesNames = new List<string>();

            foreach (string s in tagLinks.Keys)
            {
                tagNames.Add(s);
            }

            foreach (string s in seriesTags.Keys)
            {
                seriesNames.Add(s);
            }

            backup.BackupTagNames(tagNames);
            backup.BackupSeriesNames(seriesNames);

            Dictionary<string, List<string>> readTagLinks
                = (Dictionary<string, List<string>>)backup.GetTagIndex(ModificationStatus.UNMODIFIED);
            Dictionary<string, HashSet<string>> readSeriesTags
                = (Dictionary<string, HashSet<string>>)backup.GetSeriesIndex();

            CollectionAssert.AreEqual(tagNames, readTagLinks.Keys);
            CollectionAssert.AreEqual(seriesNames, readSeriesTags.Keys);
            Assert.IsTrue(readTagLinks.Values.Count > 0);
            Assert.IsTrue(readSeriesTags.Values.Count > 0);
        }

        [TestMethod]
        public void BackupAndReadTest()
        {
            Backup();

            Dictionary<string, List<string>> readTagLinks
                = (Dictionary<string, List<string>>)backup.GetTagIndex(ModificationStatus.DONE);
            Dictionary<string, HashSet<string>> readSeriesTags
                = (Dictionary<string, HashSet<string>>)backup.GetSeriesIndex();

            CollectionAssert.AreEqual(tagLinks.Keys, readTagLinks.Keys);
            CollectionAssert.AreEqual(seriesTags.Keys, readSeriesTags.Keys);
        }

        [TestMethod()]
        public void IsConnectedTest()
        {
            Assert.IsTrue(backup.IsConnected());
        }

        [TestMethod]
        public void MarkForUpdateTest()
        {
            Backup();

            Dictionary<string, List<string>> readTagLinks
                = (Dictionary<string, List<string>>)backup.GetTagIndex(ModificationStatus.DONE);

            CollectionAssert.AreEqual(tagLinks.Keys, readTagLinks.Keys);

            List<string> tagNames = new List<string>();

            foreach (string s in tagLinks.Keys)
            {
                tagNames.Add(s);
            }

            backup.MarkForUpdate(tagNames);

            readTagLinks = (Dictionary<string, List<string>>)backup.GetTagIndex(ModificationStatus.UNMODIFIED);

            CollectionAssert.AreEqual(tagLinks.Keys, readTagLinks.Keys);
        }

        private void Backup()
        {
            List<string> tagNames = new List<string>();
            List<string> seriesNames = new List<string>();

            foreach (string s in tagLinks.Keys)
            {
                tagNames.Add(s);
            }

            foreach (string s in seriesTags.Keys)
            {
                seriesNames.Add(s);
            }

            backup.BackupTagNames(tagNames);
            backup.BackupSeriesNames(seriesNames);

            backup.BackupTagLinks(tagLinks);
            backup.BackupSeriesTags(seriesTags);
        }
    }
}