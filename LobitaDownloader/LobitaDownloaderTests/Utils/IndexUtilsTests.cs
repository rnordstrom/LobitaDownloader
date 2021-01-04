using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace LobitaDownloader.Tests
{
    [TestClass()]
    public class IndexUtilsTests
    {
        [TestMethod()]
        public void GetTopSeriesTest()
        {
            IDictionary<string, int> tagOccurrences = new Dictionary<string, int>();

            tagOccurrences.Add("hololive", 10);
            tagOccurrences.Add("pokemon", 8);
            tagOccurrences.Add("spice_and_wolf", 5);

            string tagName = "gawr_gura";

            List<string> topSeries = IndexUtils.GetTopSeries(ref tagOccurrences, tagName);

            CollectionAssert.AreEqual(tagOccurrences.Keys.ToList(), topSeries);
        }
    }
}