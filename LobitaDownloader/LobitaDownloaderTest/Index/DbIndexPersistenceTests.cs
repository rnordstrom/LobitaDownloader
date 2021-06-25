using LobitaDownloader.Index.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace LobitaDownloader.Tests
{
    [TestClass()]
    public class DbIndexPersistenceTests
    {
        private MySqlConnection conn;
        private Dictionary<string, Character> characterIndex = new Dictionary<string, Character>();
        private Dictionary<string, Series> seriesIndex = new Dictionary<string, Series>();
        XmlConfigManager cm;
        private DbIndexPersistence database;
        private string tag1 = "gawr_gura";
        private string tag2 = "ninomae_ina'nis";
        private string tag3 = "hilda_valentine_goneril";
        private string seriesName = "hololive";

        [TestInitialize]
        public void Setup()
        {
            cm = new XmlConfigManager(Resources.TestDirectory, Resources.ConfigFile);
            database = new DbIndexPersistence(cm);
            string dbName = cm.GetItemByName("NextDatabase");
            
            string connStr =
                $"server={Environment.GetEnvironmentVariable("DB_HOST")};" +
                $"user={Environment.GetEnvironmentVariable("DB_USER")};" +
                $"database={dbName};port=3306;" +
                $"password={Environment.GetEnvironmentVariable("DB_PWD")}";

            conn = new MySqlConnection(connStr);

            Url url1 = new Url(1, "1.png");
            Url url2 = new Url(2, "2.png");
            Url url3 = new Url(3, "3.png");
            Url url4 = new Url(4, "4.png");
            Url url6 = new Url(6, "6.png");

            List<Url> urlList1 = new List<Url>() { url1, url2, url3 };
            List<Url> urlList2 = new List<Url>() { url3, url4 };
            List<Url> urlList3 = new List<Url>() { url6 };
            
            Series series1 = new Series(1, seriesName, urlList1.Count + urlList2.Count - 1);

            List<Series> seriesList1 = new List<Series>() { series1 };

            Character character1 = new Character(1, tag1, urlList1.Count, seriesList1, urlList1);
            Character character2 = new Character(2, tag2, urlList2.Count, seriesList1, urlList2);
            Character character3 = new Character(3, tag3, urlList3.Count, new List<Series>(), urlList3);

            characterIndex.Add(tag1, character1);
            characterIndex.Add(tag2, character2);
            characterIndex.Add(tag3, character3);

            seriesIndex.Add(seriesName, series1);
        }

        [TestMethod()]
        public void DatabaseQueryTest()
        {
            database.Clean();

            Assert.IsTrue(TableIsEmpty("links"));
            Assert.IsTrue(TableIsEmpty("tags"));
            Assert.IsTrue(TableIsEmpty("tag_links"));
            Assert.IsTrue(TableIsEmpty("series"));
            Assert.IsTrue(TableIsEmpty("series_tags"));

            database.PersistCharacters(characterIndex);

            List<long> countList = GetDatabaseCounts();

            Assert.AreEqual(characterIndex[tag1].Urls.Count, countList[0]);
            Assert.AreEqual(characterIndex[tag2].Urls.Count, countList[1]);
            Assert.AreEqual(2, countList[2]);

            conn.Close();
        }

        [TestMethod()]
        public void IsConnectedTest()
        {
            DbIndexPersistence database = new DbIndexPersistence(cm);

            Assert.IsTrue(database.IsConnected());
        }

        private List<long> GetDatabaseCounts()
        {
            string replacedName = tag2.Replace("'", "''");

            string queryLinks1 =
                $"SELECT COUNT(l.id) " +
                $"FROM tags as t, tag_links AS tl, links as l " +
                $"WHERE t.id = tl.tag_id AND l.id = tl.link_id AND t.name = '{tag1}'";
            string queryLinks2 =
                $"SELECT COUNT(l.id) " +
                $"FROM tags as t, tag_links AS tl, links as l " +
                $"WHERE t.id = tl.tag_id AND l.id = tl.link_id AND t.name = '{replacedName}'";
            string querySeries = $"SELECT COUNT(t.id) " +
                $"FROM tags AS t, series_tags AS st, series AS s " +
                $"WHERE t.id = st.tag_id AND s.id = st.series_id AND s.name = '{seriesName}'";

            List<string> queryList = new List<string>() { queryLinks1, queryLinks2, querySeries };
            List<long> countList = new List<long>();
            MySqlCommand cmd;
            MySqlDataReader rdr;

            try
            {
                conn.Open();

                foreach (string query in queryList)
                {
                    cmd = new MySqlCommand(query, conn);
                    rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        countList.Add((long)rdr[0]);
                    }

                    rdr.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
            }

            conn.Close();

            return countList;
        }

        [TestMethod]
        public void TestCountPosts()
        {
            string getTagLinksCount1 = $"SELECT post_count FROM tags WHERE name = '{tag1}'";
            string getTagLinksCount2 = $"SELECT post_count FROM tags WHERE name = '{tag2.Replace("'", "''")}'";
            string getTagLinksCount3 = $"SELECT post_count FROM tags WHERE name = '{tag3}'";
            string getSeriesLinksCount = $"SELECT post_count FROM series WHERE name = '{seriesIndex}'";

            try
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(getTagLinksCount1, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Assert.AreEqual(characterIndex[tag1].Urls.Count, (int)rdr[0]);
                }

                rdr.Close();

                cmd = new MySqlCommand(getTagLinksCount2, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Assert.AreEqual(characterIndex[tag2].Urls.Count, (int)rdr[0]);
                }

                rdr.Close();

                cmd = new MySqlCommand(getTagLinksCount3, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Assert.AreEqual(characterIndex[tag3].Urls.Count, (int)rdr[0]);
                }

                rdr.Close();

                cmd = new MySqlCommand(getSeriesLinksCount, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Assert.AreEqual(characterIndex[tag1].Urls.Count + characterIndex[tag2].Urls.Count, (int)rdr[0]);
                }

                rdr.Close();
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);
                Assert.IsTrue(false);
            }

            conn.Close();
        }

        private bool TableIsEmpty(string tableName)
        {
            string countQuery = $"SELECT COUNT(*) FROM {tableName}";
            long count = 1;

            try
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(countQuery, conn); ;
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    count = (long)rdr[0];
                }

                rdr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
            }

            conn.Close();

            return count == 0 ? true : false;
        }
    }
}