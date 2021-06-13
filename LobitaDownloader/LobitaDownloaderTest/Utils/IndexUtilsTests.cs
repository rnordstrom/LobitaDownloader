using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace LobitaDownloader.Tests
{
    [TestClass()]
    public class IndexUtilsTests
    {
        [TestMethod]
        public void GetTopSeriesEmptyTest()
        {
            IDictionary<string, int> tagOccurrences = new Dictionary<string, int>();

            List<string> keysBefore = tagOccurrences.Keys.ToList();
            HashSet<string> topSeries = IndexUtils.GetTopSeries(ref tagOccurrences, 10);

            Assert.AreEqual(0, topSeries.Count);
        }

        [TestMethod]
        public void GetTopSeriesBelowLimitTest()
        {
            IDictionary<string, int> tagOccurrences = new Dictionary<string, int>();

            tagOccurrences.Add("hololive", 10);
            tagOccurrences.Add("pokemon", 8);
            tagOccurrences.Add("spice_and_wolf", 5);

            HashSet<string> keysBefore = tagOccurrences.Keys.ToHashSet();
            HashSet<string> topSeries = IndexUtils.GetTopSeries(ref tagOccurrences, 10);

            CollectionAssert.AreEqual(keysBefore.ToList(), topSeries.ToList());
        }

        [TestMethod]
        public void GetTopSeriesBeyondLimitTest()
        {
            IDictionary<string, int> tagOccurrences = new Dictionary<string, int>();

            tagOccurrences.Add("hololive", 100);
            tagOccurrences.Add("pokemon", 80);
            tagOccurrences.Add("spice_and_wolf", 50);
            tagOccurrences.Add("fire_emblem", 40);
            tagOccurrences.Add("the_witcher", 30);
            tagOccurrences.Add("shingeki_no_bahamut", 20);
            tagOccurrences.Add("tales_of_berseria", 15);
            tagOccurrences.Add("nijisanji", 7);
            tagOccurrences.Add("dark_souls", 5);
            tagOccurrences.Add("red_dead_redemption", 4);
            tagOccurrences.Add("grand_theft_auto", 1);

            HashSet<string> topSeries = IndexUtils.GetTopSeries(ref tagOccurrences, 10);

            Assert.AreEqual(10, topSeries.Count);
            Assert.IsFalse(topSeries.Contains("grand_theft_auto"));
        }
    }
}