using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace LobitaDownloader.Tests
{
    [TestClass()]
    public class XmlIndexPersistenceTests
    {
        private XmlIndexPersistence persistence;
        private Dictionary<string, List<string>> tagLinks = new Dictionary<string, List<string>>();
        private Dictionary<string, HashSet<string>> seriesTags = new Dictionary<string, HashSet<string>>();
        string tag1 = "gawr_gura";
        string tag2 = "ninomae_ina'nis";
        string series = "hololive";

        [TestInitialize]
        public void Setup()
        {
            IConfigManager configManager = new XmlConfigManager(Resources.TestDirectory, Resources.ConfigFile);
            persistence = new XmlIndexPersistence(configManager);

            tagLinks.Add(tag1, new List<string>() { "1.png", "2.png", "3.jpg" });
            tagLinks.Add(tag2, new List<string>() { "4.png", "5.png" });
            seriesTags.Add(series, new HashSet<string>() { tag1, tag2 });
        }

        [TestMethod]
        public void PersistTest()
        {
            persistence.CleanTagLinks();
            persistence.PersistTagLinks(tagLinks);

            persistence.CleanSeries();
            persistence.PersistSeriesTags(seriesTags);

            Assert.IsTrue(File.Exists(persistence.TagsFileName));
            Assert.IsTrue(File.Exists(persistence.SeriesFileName));
        }

        [TestMethod]
        public void ReadTest()
        {
            Dictionary<string, List<string>> readTagLinks
                = (Dictionary<string, List<string>>)persistence.GetTagIndex();
            Dictionary<string, HashSet<string>> readSeriesTags
                = (Dictionary<string, HashSet<string>>)persistence.GetSeriesIndex();

            CollectionAssert.AreEqual(tagLinks.Keys, readTagLinks.Keys);
            CollectionAssert.AreEqual(seriesTags.Keys, readSeriesTags.Keys);

            foreach (string tagName in tagLinks.Keys)
            {
                CollectionAssert.AreEqual(tagLinks[tagName], readTagLinks[tagName]);
            }

            foreach (string seriesName in seriesTags.Keys)
            {
                Assert.AreEqual(seriesTags[seriesName].Count, readSeriesTags[seriesName].Count);

                foreach (string s in seriesTags[seriesName])
                {
                    Assert.IsTrue(readSeriesTags[series].Contains(s));
                }
            }
        }

        [TestMethod()]
        public void IsConnectedTest()
        {
            Assert.IsTrue(persistence.IsConnected());
        }
    }
}