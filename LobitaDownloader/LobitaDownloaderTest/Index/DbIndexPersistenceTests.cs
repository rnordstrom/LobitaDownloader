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
        private DbIndexPersistence database;
        private Dictionary<string, List<string>> tagLinks = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> seriesTags = new Dictionary<string, List<string>>();
        string tag1 = "gawr_gura";
        string tag2 = "ninomae_ina'nis";
        string series = "hololive";

        [TestInitialize]
        public void Setup()
        {
            string dbName = "tagdb_test";
            string connStr = $"server=localhost;user=root;database={dbName};port=3306;password={Environment.GetEnvironmentVariable("PWD")}";

            conn = new MySqlConnection(connStr);
            database = new DbIndexPersistence(dbName);

            tagLinks.Add(tag1, new List<string>() { "1.png", "2.png", "3.jpg" });
            tagLinks.Add(tag2, new List<string>() { "4.png", "5.png" });

            seriesTags.Add(series, new List<string>() { tag1, tag2 });
        }

        [TestMethod()]
        public void DatabaseQueryTest()
        {
            database.Clean();
            database.PersistTagLinks(tagLinks);
            database.PersistSeriesTags(seriesTags);

            string replacedName = tag2.Replace("'", "''");

            string queryLinks1 = 
                $"SELECT COUNT(l.id) " +
                $"FROM tags as t, links as l " +
                $"WHERE t.id = l.tag_id AND t.name = '{tag1}'";
            string queryLinks2 = 
                $"SELECT COUNT(l.id) " +
                $"FROM tags as t, links as l " +
                $"WHERE t.id = l.tag_id AND t.name = '{replacedName}'";
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

            Assert.AreEqual(tagLinks[tag1].Count, countList[0]);
            Assert.AreEqual(tagLinks[tag2].Count, countList[1]);
            Assert.AreEqual(tagLinks.Count, countList[2]);

            conn.Close();
        }
    }
}