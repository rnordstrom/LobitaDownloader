using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LobitaDownloader
{
    public class DbIndexPersistence : IIndexPersistence
    {
        private string connStr;
        private MySqlConnection conn;
        private readonly int BatchQueryLimit;
        private readonly int TimeOut;

        public DbIndexPersistence(IConfigManager config)
        {
            string dbName = config.GetItemByName("NextDatabase");

            connStr = 
                $"server={Environment.GetEnvironmentVariable("DB_HOST")};" +
                $"user={Environment.GetEnvironmentVariable("DB_USER")};" +
                $"database={dbName};port=3306;" +
                $"password={Environment.GetEnvironmentVariable("DB_PWD")}";
            conn = new MySqlConnection(connStr);

            BatchQueryLimit = int.Parse(config.GetItemByName("BatchQueryLimit"));
            TimeOut = int.Parse(config.GetItemByName("TimeOut"));
        }

        public void CleanTagLinks()
        {
            Clean("tags", "id");
            Clean("links", "id");
        }

        public void CleanSeries()
        {
            Clean("series", "id");
        }

        private void Clean(string tableName, string idColumn)
        {
            try
            {
                string output = "Cleaning database...";

                PrintUtils.PrintRow(output, 0, 0);

                conn.Open();
                
                int currentId = 1;
                int deleted = 0;
                string checkMin = $"SELECT MIN({idColumn}) FROM {tableName}";

                MySqlCommand minCmd = new MySqlCommand(checkMin, conn);
                MySqlDataReader rdr = minCmd.ExecuteReader();

                while (rdr.Read() && rdr[0] != DBNull.Value)
                {
                    currentId = (int)rdr[0];
                }

                rdr.Close();

                string checkExists = $"SELECT {idColumn} FROM {tableName} WHERE {idColumn} = {currentId}";
                string deleteBatch = $"DELETE FROM {tableName} LIMIT {BatchQueryLimit}";
                string resetInc = $"ALTER TABLE {tableName} AUTO_INCREMENT = 1";

                MySqlCommand existsCmd = new MySqlCommand(checkExists, conn);
                MySqlCommand deleteCmd = new MySqlCommand(deleteBatch, conn);
                rdr = existsCmd.ExecuteReader();

                deleteCmd.CommandTimeout = TimeOut;

                while (rdr.Read())
                {
                    rdr.Close();
                    deleteCmd.ExecuteNonQuery();

                    currentId += BatchQueryLimit;
                    deleted += BatchQueryLimit;
                    checkExists = $"SELECT {idColumn} FROM {tableName} WHERE {idColumn} = {currentId}";
                    existsCmd = new MySqlCommand(checkExists, conn);

                    rdr = existsCmd.ExecuteReader();
                    output = $"Deleted {deleted} rows from table '{tableName}'.";

                    PrintUtils.PrintRow(output, 0, 0);
                }

                rdr.Close();

                MySqlCommand resetCmd = new MySqlCommand(resetInc, conn);
                resetCmd.ExecuteNonQuery();

                output = $"Database cleaned.";

                PrintUtils.PrintRow(output, 0, 0);
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);
            }

            conn.Close();
        }

        public void CountTagLinks()
        {
            string getTagLinksCount =
                $"SELECT t.id, COUNT(l.id) " +
                $"FROM tags AS t, tag_links AS tl, links AS l " +
                $"WHERE t.id = tl.tag_id AND l.id = tl.link_id AND t.id IN (%) " +
                $"GROUP BY t.id";

            CountPosts("tags", "id", "post_count", getTagLinksCount);
        }

        public void CountSeriesLinks()
        {
            string getSeriesLinksCount =
                $"SELECT s.id, COUNT(l.id) " +
                $"FROM tags AS t, series_tags AS st, series AS s, links AS l, tag_links AS tl " +
                $"WHERE t.id = tl.tag_id AND l.id = tl.link_id AND t.id = st.tag_id AND st.series_id = s.id AND s.id IN (%) " +
                $"GROUP BY s.id";

            CountPosts("series", "id", "post_count", getSeriesLinksCount);
        }

        private void CountPosts(string tableName, string idColumn, string countColumn, string countQuery)
        {
            try
            {
                string output = "Computing post counts...";
                int tagsOffset = 0;

                PrintUtils.PrintRow(output, 0, 0);
                conn.Open();

                while (true)
                {
                    string getTagIDs = $"SELECT {idColumn} FROM {tableName} LIMIT {BatchQueryLimit} OFFSET {tagsOffset}";
                    StringBuilder sb = new StringBuilder();
                    List<int> ids = new List<int>(); 
                    MySqlCommand cmd = new MySqlCommand(getTagIDs, conn);

                    cmd.CommandTimeout = TimeOut;

                    MySqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        ids.Add((int)rdr[0]);
                        sb.Append((int)rdr[0] + ",");
                    }

                    if (!rdr.HasRows)
                    {
                        break;
                    }

                    rdr.Close();

                    sb.Remove(sb.Length - 1, 1);
                    string countQueryUpdated = countQuery.Replace("%", sb.ToString());

                    cmd = new MySqlCommand(countQueryUpdated, conn);

                    cmd.CommandTimeout = TimeOut;

                    string updateCount;
                    var idCounts = new Dictionary<int, long>();
                    rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        idCounts.Add((int)rdr[0], (long)rdr[1]);
                    }

                    rdr.Close();

                    foreach (int id in ids)
                    {
                        if (!idCounts.ContainsKey(id))
                        {
                            idCounts.Add(id, 0);
                        }
                    }

                    foreach (var pair in idCounts)
                    {
                        output = $"Updating {idColumn} {pair.Key} with {countColumn} = {pair.Value} in table '{tableName}'.";

                        PrintUtils.PrintRow(output, 0, 0);

                        updateCount = $"UPDATE {tableName} SET {countColumn} = {pair.Value} WHERE {idColumn} = {pair.Key}";
                        cmd = new MySqlCommand(updateCount, conn);

                        cmd.ExecuteNonQuery();
                    }

                    tagsOffset += BatchQueryLimit;

                    output = $"Processed {BatchQueryLimit} posts in table '{tableName}' ({tagsOffset} done).";

                    PrintUtils.PrintRow(output, 0, 0);
                }
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);
            }

            conn.Close();
        }

        public void PersistTagLinks(IDictionary<string, List<string>> index)
        {
            int i = 1;
            string output;
            MySqlCommand cmd;
            MySqlDataReader rdr;
            MySqlTransaction transaction = null;

            PersistColumnBatch(index.Keys.ToList(), "tags", "name");
            PersistColumnBatch(ToUniqueSet(index.Values.ToList()), "links", "url");

            Dictionary<string, int> tagDict = new Dictionary<string, int>();
            Dictionary<string, int> linkDict = new Dictionary<string, int>();
            string queryTags = "SELECT name, id FROM tags";
            string queryLinks = "SELECT url, id FROM links";

            output = $"Preparing ID dictionaries.";

            PrintUtils.PrintRow(output, 0, 0);

            try
            {
                conn.Open();

                cmd = new MySqlCommand(queryTags, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    tagDict.Add((string)rdr[0], (int)rdr[1]);
                }

                rdr.Close();

                cmd = new MySqlCommand(queryLinks, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    linkDict.Add((string)rdr[0], (int)rdr[1]);
                }

                rdr.Close();

                StringBuilder insertTagLinks = new StringBuilder("INSERT INTO tag_links(tag_id, link_id) VALUES");
                int j = 0;
                int k = 0;

                transaction = conn.BeginTransaction();

                foreach (string tagName in index.Keys)
                {
                    int tagId = tagDict[tagName];
                    output = $"Writing tag ({i++} / {index.Keys.Count}).";

                    PrintUtils.PrintRow(output, 0, 0);

                    foreach (string link in index[tagName])
                    {
                        int linkId = linkDict[link];

                        insertTagLinks.Append($"('{tagId}', {linkId}),");

                        j++;
                    }

                    if (j >= BatchQueryLimit || k == index.Keys.Count - 1)
                    {
                        insertTagLinks.Remove(insertTagLinks.Length - 1, 1);
                        insertTagLinks.Append(";");

                        cmd = new MySqlCommand(insertTagLinks.ToString(), conn);

                        cmd.CommandTimeout = TimeOut;
                        cmd.ExecuteNonQuery();
                        transaction.Commit();

                        transaction = conn.BeginTransaction();
                        insertTagLinks = new StringBuilder("INSERT INTO tag_links(tag_id, link_id) VALUES");

                        j = 0;
                    }

                    k++;
                }
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }

            conn.Close();
        }

        public void PersistSeriesTags(IDictionary<string, HashSet<string>> index)
        {
            int i = 1;
            string output;
            MySqlCommand cmd;
            MySqlDataReader rdr;
            MySqlTransaction transaction = null;

            PersistColumnBatch(index.Keys.ToList(), "series", "name");

            Dictionary<string, int> seriesDict = new Dictionary<string, int>();
            Dictionary<string, int> tagDict = new Dictionary<string, int>();
            string querySeries = "SELECT name, id FROM series";
            string queryTags = "SELECT name, id FROM tags";

            output = $"Preparing ID dictionaries.";

            PrintUtils.PrintRow(output, 0, 0);

            try
            {
                conn.Open();

                cmd = new MySqlCommand(queryTags, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    tagDict.Add((string)rdr[0], (int)rdr[1]);
                }

                rdr.Close();

                cmd = new MySqlCommand(querySeries, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    seriesDict.Add((string)rdr[0], (int)rdr[1]);
                }

                rdr.Close();

                StringBuilder insertSeriesTags = new StringBuilder("INSERT INTO series_tags(tag_id, series_id) VALUES");
                int j = 0;
                int k = 0;

                transaction = conn.BeginTransaction();

                foreach (string seriesName in index.Keys)
                {
                    int seriesId = seriesDict[seriesName];
                    output = $"Writing series ({i++} / {index.Keys.Count}).";

                    PrintUtils.PrintRow(output, 0, 0);

                    foreach (string tagName in index[seriesName])
                    {
                        int tagId = tagDict[tagName];

                        insertSeriesTags.Append($"({tagId}, {seriesId}),");

                        j++;
                    }

                    if (j >= BatchQueryLimit || k == index.Keys.Count - 1)
                    {
                        insertSeriesTags.Remove(insertSeriesTags.Length - 1, 1);
                        insertSeriesTags.Append(";");

                        cmd = new MySqlCommand(insertSeriesTags.ToString(), conn);

                        cmd.CommandTimeout = TimeOut;
                        cmd.ExecuteNonQuery();
                        transaction.Commit();

                        transaction = conn.BeginTransaction();
                        insertSeriesTags = new StringBuilder("INSERT INTO series_tags(tag_id, series_id) VALUES");

                        j = 0;
                    }

                    k++;
                }
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }

            conn.Close();
        }

        private void PersistColumnBatch(ICollection<string> values, string tableName, string columnName)
        {
            string replacedName;
            string output;
            MySqlCommand cmd;
            StringBuilder insertValues = new StringBuilder($"INSERT INTO {tableName}({columnName}) VALUES");
            int i = 1;
            int j = 0;

            try
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();

                foreach (string s in values)
                {
                    output = $"Writing value to column {tableName}.{columnName} ({i++} / {values.Count}).";

                    PrintUtils.PrintRow(output, 0, 0);

                    replacedName = s.Replace("'", "''");
                    insertValues.Append($"('{replacedName}')");

                    if (values.Count == 1 || (j > 0 && (j % BatchQueryLimit == 0 || j == values.Count - 1)))
                    {
                        insertValues.Append(";");

                        cmd = new MySqlCommand(insertValues.ToString(), conn);

                        cmd.ExecuteNonQuery();
                        transaction.Commit();

                        transaction = conn.BeginTransaction();
                        insertValues = new StringBuilder($"INSERT INTO {tableName}({columnName}) VALUES");
                    }
                    else
                    {
                        insertValues.Append(",");
                    }

                    j++;
                }
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);
            }

            conn.Close();
        }

        private HashSet<string> ToUniqueSet(ICollection<List<string>> values)
        {
            HashSet<string> uniqueSet = new HashSet<string>();

            foreach(var v in values)
            {
                foreach(string s in v)
                {
                    uniqueSet.Add(s);
                }
            }

            return uniqueSet;
        }

        public bool IsConnected()
        {
            bool isOpen = false;

            try
            {
                conn.Open();

                isOpen = conn.State == ConnectionState.Open;
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);
            }

            conn.Close();

            return isOpen;
        }
    }
}
