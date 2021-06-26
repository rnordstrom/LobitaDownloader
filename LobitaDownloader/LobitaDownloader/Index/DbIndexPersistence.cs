using LobitaDownloader.Index.Interfaces;
using LobitaDownloader.Index.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

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

        public void Clean()
        {
            Clean("tags", "id");
            Clean("links", "id");
            Clean("series", "id");
        }

        private void Clean(string tableName, string idColumn)
        {
            MySqlCommand cmd;
            MySqlDataReader rdr;
            long count = 0;
            string countQuery;
            string deleteBatch;
            int deleted = 0;

            try
            {
                string output = "Cleaning database...";

                PrintUtils.PrintRow(output, 0, 0);

                conn.Open();

                countQuery = $"SELECT COUNT({idColumn}) FROM {tableName}";
                deleteBatch = $"DELETE FROM {tableName} LIMIT {BatchQueryLimit}";

                cmd = new MySqlCommand(countQuery, conn);

                cmd.CommandTimeout = TimeOut;

                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    count = (long)rdr[0];
                }

                rdr.Close();

                cmd = new MySqlCommand(deleteBatch, conn);

                cmd.CommandTimeout = TimeOut;

                while (count > 0 && deleted < count)
                {
                    cmd.ExecuteNonQuery();

                    deleted += BatchQueryLimit;
                    output = $"Deleted {deleted} rows from table '{tableName}'.";

                    PrintUtils.PrintRow(output, 0, 0);
                }

                output = $"Database cleaned.";

                PrintUtils.PrintRow(output, 0, 0);
            }
            catch (Exception e)
            {
                PrintUtils.Report(e);
            }

            conn.Close();
        }

        public void PersistCharacters(IDictionary<string, Character> index)
        {
            PersistColumnBatch(index.Values, "tags", "id", "name");
            PersistColumnBatch(ToUniqueSet(index.Values.SelectMany(c => c.Urls).ToList()), "links", "id", "url");
            PersistColumnBatch(ToUniqueSet(index.Values.SelectMany(c => c.Series).ToList()), "series", "id", "name");

            string output;
            int i = 1;
            int characterId;

            foreach (string characterName in index.Keys)
            {
                output = $"Writing tag ({i++} / {index.Keys.Count}).";

                PrintUtils.PrintRow(output, 0, 0);

                characterId = index[characterName].Id;

                PersistRelations(characterId, "tag_links", "tag_id", "link_id", ToUniqueSet(index[characterName].Urls.Cast<ModelBase>().ToList()));
                PersistRelations(characterId, "series_tags", "tag_id", "series_id", ToUniqueSet(index[characterName].Series.Cast<ModelBase>().ToList()));
            }
        }

        private void PersistRelations(int characterId, string tableName, string charIdCol, string relIdCol, ICollection<ModelBase> objects)
        {
            MySqlTransaction transaction = null;
            MySqlCommand cmd;
            string queryString = $"INSERT INTO {tableName}({charIdCol}, {relIdCol}) VALUES";

            try
            {
                conn.Open();

                StringBuilder insertRelations = new StringBuilder(queryString);
                int j = 0;

                transaction = conn.BeginTransaction();

                foreach (ModelBase o in objects)
                {
                    int objectId = o.Id;

                    insertRelations.Append($"({characterId}, {objectId}),");

                    j++;
                }

                if (j > 0 && (j >= BatchQueryLimit || j == objects.Count))
                {
                    insertRelations.Remove(insertRelations.Length - 1, 1);
                    insertRelations.Append(";");

                    cmd = new MySqlCommand(insertRelations.ToString(), conn);

                    cmd.CommandTimeout = TimeOut;

                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    transaction = conn.BeginTransaction();
                    insertRelations = new StringBuilder(queryString);

                    j = 0;
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

        private void PersistColumnBatch<T>(ICollection<T> objects, string tableName, string idColumn, string nameColumn) where T : ModelBase, Model
        {
            string replacedName;
            string output;
            MySqlCommand cmd;
            string insertQuery;
            StringBuilder insertValues = new StringBuilder();
            int postCount;
            int i = 1;
            int j = 0;

            try
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();

                foreach (T o in objects)
                {
                    output = $"Writing values to columns {tableName}.{idColumn}, {tableName}.{nameColumn} ({i++} / {objects.Count}).";

                    PrintUtils.PrintRow(output, 0, 0);

                    replacedName = o.GetName().Replace("'", "''");

                    try
                    {
                        postCount = o.GetCount();
                        insertQuery = $"INSERT INTO {tableName}({idColumn}, {nameColumn}, post_count) VALUES";

                        if (j == 0)
                        {
                            insertValues.Append(insertQuery);
                        }

                        insertValues.Append($"({o.Id}, '{replacedName}', {postCount})");
                    }
                    catch (Exception)
                    {
                        insertQuery = $"INSERT INTO {tableName}({idColumn}, {nameColumn}) VALUES";

                        if (j == 0)
                        {
                            insertValues.Append(insertQuery);
                        }

                        insertValues.Append($"({o.Id}, '{replacedName}')");
                    }

                    if (objects.Count == 1 || (j > 0 && (j % BatchQueryLimit == 0 || j == objects.Count - 1)))
                    {
                        insertValues.Append(";");

                        cmd = new MySqlCommand(insertValues.ToString(), conn);

                        cmd.CommandTimeout = TimeOut;

                        cmd.ExecuteNonQuery();
                        transaction.Commit();

                        transaction = conn.BeginTransaction();
                        insertValues = new StringBuilder(insertQuery);
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

        private ICollection<T> ToUniqueSet<T>(ICollection<T> objects) where T : ModelBase
        {
            List<T> objectList = new List<T>();
            HashSet<int> objectSet = new HashSet<int>();

            foreach (T o in objects)
            {
                if (!objectSet.Contains(o.Id))
                {
                    objectList.Add(o);
                    objectSet.Add(o.Id);
                }
            }

            return objectList;
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
