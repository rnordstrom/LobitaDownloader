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
        private int batchQueryLimit;
        private int timeOut;

        public DbIndexPersistence(IConfigManager config)
        {
            string dbName = config.GetItemByName("NextDatabase");

            connStr = 
                $"server={Environment.GetEnvironmentVariable("DB_HOST")};" +
                $"user={Environment.GetEnvironmentVariable("DB_USER")};" +
                $"database={dbName};port=3306;" +
                $"password={Environment.GetEnvironmentVariable("DB_PWD")}";
            conn = new MySqlConnection(connStr);

            batchQueryLimit = int.Parse(config.GetItemByName("BatchQueryLimit"));
            timeOut = int.Parse(config.GetItemByName("TimeOut"));
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
                string deleteBatch = $"DELETE FROM {tableName} LIMIT {batchQueryLimit}";
                string resetInc = $"ALTER TABLE {tableName} AUTO_INCREMENT = 1";
                string output;

                MySqlCommand existsCmd = new MySqlCommand(checkExists, conn);
                MySqlCommand deleteCmd = new MySqlCommand(deleteBatch, conn);
                rdr = existsCmd.ExecuteReader();

                deleteCmd.CommandTimeout = timeOut;

                while (rdr.Read())
                {
                    rdr.Close();
                    deleteCmd.ExecuteNonQuery();

                    currentId += batchQueryLimit;
                    deleted += batchQueryLimit;
                    checkExists = $"SELECT {idColumn} FROM {tableName} WHERE {idColumn} = {currentId}";
                    existsCmd = new MySqlCommand(checkExists, conn);

                    rdr = existsCmd.ExecuteReader();
                    output = $"Deleted {deleted} rows.";

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
                Report(e);
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

                    if (j >= batchQueryLimit || k == index.Keys.Count - 1)
                    {
                        insertTagLinks.Remove(insertTagLinks.Length - 1, 1);
                        insertTagLinks.Append(";");

                        cmd = new MySqlCommand(insertTagLinks.ToString(), conn);

                        cmd.CommandTimeout = timeOut;
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
                Report(e);

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

                    if (j >= batchQueryLimit || k == index.Keys.Count - 1)
                    {
                        insertSeriesTags.Remove(insertSeriesTags.Length - 1, 1);
                        insertSeriesTags.Append(";");

                        cmd = new MySqlCommand(insertSeriesTags.ToString(), conn);

                        cmd.CommandTimeout = timeOut;
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
                Report(e);

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

                    if (values.Count == 1 || (j > 0 && (j % batchQueryLimit == 0 || j == values.Count - 1)))
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
                Report(e);
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

        public IDictionary<string, List<string>> GetTagIndex()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, HashSet<string>> GetSeriesIndex()
        {
            throw new NotImplementedException();
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
                Report(e);
            }

            conn.Close();

            return isOpen;
        }

        private void Report(Exception e)
        {
            if (Resources.SystemLogger != null)
            {
                Resources.SystemLogger.Log(e.Message + Environment.NewLine + e.StackTrace);
            }

            Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
        }
    }
}
