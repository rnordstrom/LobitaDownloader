using LobitaDownloader.Index.Models;
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

        public void CleanCharacters()
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

        public void CountCharacterPosts(IDictionary<string, Character> index)
        {
            int count;

            foreach (string characterName in index.Keys)
            {
                count = index[characterName].Urls.Count;

                UpdatePostCount(index[characterName].Id, count, "tags", "id", "post_count");
            }
        }

        public void CountSeriesPosts(IDictionary<string, Series> seriesIndex, IDictionary<string, Character> characterIndex)
        {
            int count;

            foreach (string seriesName in seriesIndex.Keys)
            {
                count = 0;

                foreach (Character character in seriesIndex[seriesName].Characters)
                {
                    count += characterIndex[character.Name].Urls.Count;
                }

                UpdatePostCount(seriesIndex[seriesName].Id, count, "series", "id", "post_count");
            }
        }

        private void UpdatePostCount(int id, int count, string tableName, string idColumn, string countColumn)
        {
            try
            {
                string output;
                string updateCount;
                MySqlCommand cmd;

                conn.Open();

                output = $"Updating {idColumn} {id} with {countColumn} = {count} in table '{tableName}'.";

                PrintUtils.PrintRow(output, 0, 0);

                updateCount = $"UPDATE {tableName} SET {countColumn} = {count} WHERE {idColumn} = {id}";
                cmd = new MySqlCommand(updateCount, conn);

                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);
            }

            conn.Close();
        }

        public void PersistCharacters(IDictionary<string, Character> index)
        {
            int i = 1;
            string output;
            MySqlCommand cmd;
            MySqlTransaction transaction = null;
            Dictionary<int, string> characterIds = new Dictionary<int, string>();
            Dictionary<int, string> urlIds = new Dictionary<int, string>();

            foreach (string characterName in index.Keys)
            {
                characterIds.Add(index[characterName].Id, characterName);

                foreach (Url url in index[characterName].Urls)
                {
                    if (!urlIds.ContainsKey(url.Id))
                    {
                        urlIds.Add(url.Id, url.Link);
                    }
                }
            }

            PersistColumnBatch(characterIds, "tags", "id", "name");
            PersistColumnBatch(urlIds, "links", "id", "url");

            try
            {
                conn.Open();

                StringBuilder insertTagLinks = new StringBuilder("INSERT INTO tag_links(tag_id, link_id) VALUES");
                int j = 0;
                int k = 0;

                transaction = conn.BeginTransaction();

                foreach (string characterName in index.Keys)
                {
                    int characterId = index[characterName].Id;
                    output = $"Writing tag ({i++} / {index.Keys.Count}).";

                    PrintUtils.PrintRow(output, 0, 0);

                    foreach (Url url in index[characterName].Urls)
                    {
                        int urlId = url.Id;

                        insertTagLinks.Append($"('{characterId}', {urlId}),");

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

        public void PersistSeries(IDictionary<string, Series> index)
        {
            int i = 1;
            string output;
            MySqlCommand cmd;
            MySqlTransaction transaction = null;
            Dictionary<int, string> seriesIds = new Dictionary<int, string>();

            foreach (string seriesName in index.Keys)
            {
                seriesIds.Add(index[seriesName].Id, seriesName);
            }

            PersistColumnBatch(seriesIds, "series", "id", "name");

            try
            {
                conn.Open();

                StringBuilder insertSeriesTags = new StringBuilder("INSERT INTO series_tags(tag_id, series_id) VALUES");
                int j = 0;
                int k = 0;

                transaction = conn.BeginTransaction();

                foreach (string seriesName in index.Keys)
                {
                    int seriesId = index[seriesName].Id;
                    output = $"Writing series ({i++} / {index.Keys.Count}).";

                    PrintUtils.PrintRow(output, 0, 0);

                    foreach (Character character in index[seriesName].Characters)
                    {
                        int characterId = character.Id;

                        insertSeriesTags.Append($"({characterId}, {seriesId}),");

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

        private void PersistColumnBatch(IDictionary<int, string> values, string tableName, string idColumn, string nameColumn)
        {
            string replacedName;
            string output;
            MySqlCommand cmd;
            StringBuilder insertValues = new StringBuilder($"INSERT INTO {tableName}({idColumn}, {nameColumn}) VALUES");
            int i = 1;
            int j = 0;

            try
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();

                foreach (int id in values.Keys)
                {
                    output = $"Writing values to columns {tableName}.{idColumn}, {tableName}.{nameColumn} ({i++} / {values.Count}).";

                    PrintUtils.PrintRow(output, 0, 0);

                    replacedName = values[id].Replace("'", "''");
                    insertValues.Append($"({id}, '{replacedName}')");

                    if (values.Count == 1 || (j > 0 && (j % BatchQueryLimit == 0 || j == values.Count - 1)))
                    {
                        insertValues.Append(";");

                        cmd = new MySqlCommand(insertValues.ToString(), conn);

                        cmd.ExecuteNonQuery();
                        transaction.Commit();

                        transaction = conn.BeginTransaction();
                        insertValues = new StringBuilder($"INSERT INTO {tableName}({idColumn}, {nameColumn}) VALUES");
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
