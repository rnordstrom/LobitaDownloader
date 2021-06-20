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
        private Dictionary<string, List<string>> tagLinks = new Dictionary<string, List<string>>();
        private Dictionary<string, HashSet<string>> seriesTags = new Dictionary<string, HashSet<string>>();
        XmlConfigManager cm;
        private DbIndexPersistence database;
        private string tag1 = "gawr_gura";
        private string tag2 = "ninomae_ina'nis";
        private string tag3 = "hilda_valentine_goneril";
        private string series = "hololive";

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

            tagLinks.Add(tag1, new List<string>() { "1.png", "2.png", "3.jpg" });
            tagLinks.Add(tag2, new List<string>() { "3.jpg", "4.png" });
            tagLinks.Add(tag3, new List<string>() { "6.png" });

            seriesTags.Add(series, new HashSet<string>() { tag1, tag2 });
        }

        [TestMethod()]
        public void DatabasePersistAndQueryTest()
        {
            database.CleanTagLinks(); // Clean full

            Assert.IsTrue(TableIsEmpty("links"));
            Assert.IsTrue(TableIsEmpty("tags"));
            Assert.IsTrue(TableIsEmpty("tag_links"));

            database.PersistTagLinks(tagLinks);

            Assert.IsFalse(TableIsEmpty("links"));
            Assert.IsFalse(TableIsEmpty("tags"));
            Assert.IsFalse(TableIsEmpty("tag_links"));

            List<string> keys = new List<string>();

            foreach (string s in tagLinks.Keys)
            {
                if (s.Contains("'"))
                {
                    keys.Add(s.Replace("'", "''"));
                }
                else
                {
                    keys.Add(s);
                }
            }

            database.CleanTagLinks(keys); // Clean specified

            Assert.IsTrue(TableIsEmpty("links"));
            Assert.IsTrue(TableIsEmpty("tags"));
            Assert.IsTrue(TableIsEmpty("tag_links"));

            database.PersistTagLinks(tagLinks);

            database.CleanSeries();

            Assert.IsTrue(TableIsEmpty("series"));
            Assert.IsTrue(TableIsEmpty("series_tags"));

            database.PersistSeriesTags(seriesTags);

            List<long> countList = GetDatabaseCounts();

            Assert.AreEqual(tagLinks[tag1].Count, countList[0]);
            Assert.AreEqual(tagLinks[tag2].Count, countList[1]);
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
                $"WHERE t.id = st.tag_id AND s.id = st.series_id AND s.name = '{series}'";

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
            database.CountTagLinks();
            database.CountSeriesLinks();

            string getTagLinksCount1 = $"SELECT post_count FROM tags WHERE name = '{tag1}'";
            string getTagLinksCount2 = $"SELECT post_count FROM tags WHERE name = '{tag2.Replace("'", "''")}'";
            string getTagLinksCount3 = $"SELECT post_count FROM tags WHERE name = '{tag3}'";
            string getSeriesLinksCount = $"SELECT post_count FROM series WHERE name = '{series}'";

            try
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(getTagLinksCount1, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Assert.AreEqual(tagLinks[tag1].Count, (int)rdr[0]);
                }

                rdr.Close();

                cmd = new MySqlCommand(getTagLinksCount2, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Assert.AreEqual(tagLinks[tag2].Count, (int)rdr[0]);
                }

                rdr.Close();

                cmd = new MySqlCommand(getTagLinksCount3, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Assert.AreEqual(tagLinks[tag3].Count, (int)rdr[0]);
                }

                rdr.Close();

                cmd = new MySqlCommand(getSeriesLinksCount, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Assert.AreEqual(tagLinks[tag1].Count + tagLinks[tag2].Count, (int)rdr[0]);
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