using LobitaDownloader.Index.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LobitaDownloader.Tests
{
    [TestClass()]
    public class XmlIndexBackupTests
    {
        private XmlIndexBackup backup;
        private Dictionary<string, Character> characterIndex = new Dictionary<string, Character>();
        private Dictionary<string, Series> seriesIndex = new Dictionary<string, Series>();
        string tag1 = "gawr_gura";
        string tag2 = "ninomae_ina'nis";
        string seriesName = "hololive";

        [TestInitialize]
        public void Setup()
        {
            IConfigManager configManager = new XmlConfigManager(Resources.TestDirectory, Resources.ConfigFile);
            backup = new XmlIndexBackup(configManager);

            Url url1 = new Url(1, "1.png");
            Url url2 = new Url(2, "2.png");
            Url url3 = new Url(3, "3.png");
            Url url4 = new Url(4, "4.png");
            Url url5 = new Url(5, "5.png");

            List<Url> urlList1 = new List<Url>() { url1, url2, url3 };
            List<Url> urlList2 = new List<Url>() { url4, url5 };

            Series series1 = new Series(1, seriesName, urlList1.Count + urlList2.Count);

            List<Series> seriesList1 = new List<Series>() { series1 };

            Character character1 = new Character(1, tag1, urlList1.Count, seriesList1, urlList1);
            Character character2 = new Character(2, tag2, urlList2.Count, seriesList1, urlList2);

            characterIndex.Add(tag1, character1);
            characterIndex.Add(tag2, character2);


            seriesIndex.Add(seriesName, series1);
        }

        [TestMethod]
        public void BackupNamesAndReadTest()
        {
            backup.IndexCharacters(characterIndex);
            backup.IndexSeries(seriesIndex);

            Dictionary<string, Character> readCharacters
                = (Dictionary<string, Character>)backup.GetCharacterIndex(ModificationStatus.UNMODIFIED);
            Dictionary<string, Series> readSeries
                = (Dictionary<string, Series>)backup.GetSeriesIndex();

            CollectionAssert.AreEqual(characterIndex.Keys, readCharacters.Keys);
            CollectionAssert.AreEqual(seriesIndex.Keys, readSeries.Keys);
            Assert.IsTrue(readCharacters.Values.Count > 0);
            Assert.IsTrue(readSeries.Values.Count > 0);
        }

        [TestMethod]
        public void BackupAndReadTest()
        {
            Backup();

            Dictionary<string, Character> readCharacters
                = (Dictionary<string, Character>)backup.GetCharacterIndex(ModificationStatus.DONE);
            Dictionary<string, Series> readSeries
                = (Dictionary<string, Series>)backup.GetSeriesIndex();

            CollectionAssert.AreEqual(characterIndex.Keys, readCharacters.Keys);
            CollectionAssert.AreEqual(seriesIndex.Keys, readSeries.Keys);

            Assert.AreEqual(characterIndex[tag1].Id, readCharacters[tag1].Id);
            Assert.AreEqual(characterIndex[tag2].Id, readCharacters[tag2].Id);

            Assert.AreEqual(characterIndex[tag1].Name, readCharacters[tag1].Name);
            Assert.AreEqual(characterIndex[tag2].Name, readCharacters[tag2].Name);

            Assert.AreEqual(characterIndex[tag1].PostCount, readCharacters[tag1].PostCount);
            Assert.AreEqual(characterIndex[tag2].PostCount, readCharacters[tag2].PostCount);
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

            Dictionary<string, Character> readCharacters
                = (Dictionary<string, Character>)backup.GetCharacterIndex(ModificationStatus.DONE);

            CollectionAssert.AreEqual(characterIndex.Keys, readCharacters.Keys);

            List<string> tagNames = new List<string>();

            foreach (string s in characterIndex.Keys)
            {
                tagNames.Add(s);
            }

            backup.MarkForUpdate(tagNames);

            readCharacters = (Dictionary<string, Character>)backup.GetCharacterIndex(ModificationStatus.UNMODIFIED);

            CollectionAssert.AreEqual(characterIndex.Keys, readCharacters.Keys);
        }

        private void Backup()
        {
            backup.IndexCharacters(characterIndex);
            backup.IndexSeries(seriesIndex);

            backup.BackupCharacterData(characterIndex);
        }
    }
}