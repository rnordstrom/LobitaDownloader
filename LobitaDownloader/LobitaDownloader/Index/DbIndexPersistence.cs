using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LobitaDownloader
{
    public class DbIndexPersistence : IIndexPersistence
    {
        private string connStr;
        private MySqlConnection conn;
        private const int BatchInsertLimit = 10000;

        public DbIndexPersistence(string dbName)
        {
            connStr = $"server=localhost;user=root;database={dbName};port=3306;password={Environment.GetEnvironmentVariable("PWD")}";
            conn = new MySqlConnection(connStr);
        }

        public void Clean()
        {
            try
            {
                conn.Open();

                List<string> cmds = new List<string>();
                MySqlCommand cmd;

                cmds.Add("DELETE FROM tags");
                cmds.Add("DELETE FROM series");
                cmds.Add("ALTER TABLE tags AUTO_INCREMENT = 1");
                cmds.Add("ALTER TABLE links AUTO_INCREMENT = 1");
                cmds.Add("ALTER TABLE series AUTO_INCREMENT = 1");

                foreach (string s in cmds)
                {
                    cmd = new MySqlCommand(s, conn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                if (Resources.SystemLogger != null)
                {
                    Resources.SystemLogger.Log(e.Message + Environment.NewLine + e.StackTrace);
                }

                Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
            }

            conn.Close();
        }

        public void PersistTagLinks(IDictionary<string, List<string>> index)
        {
            string replacedName;
            string output;
            StringBuilder insertTagLinks;
            int id = 0;
            int i = 0;
            int j;
            MySqlCommand cmd;
            MySqlDataReader rdr;
            MySqlTransaction transaction = null;

            PersistNames(index.Keys.ToList(), "tags");

            try
            {
                conn.Open();

                foreach (string tagName in index.Keys)
                {
                    transaction = conn.BeginTransaction();

                    output = $"Writing tag {i++ + 1} out of {index.Keys.Count}.";

                    PrintUtils.PrintRow(output, 0, 0);

                    replacedName = tagName.Replace("'", "''");

                    string queryId = $"SELECT id FROM tags WHERE name='{replacedName}'";
                    cmd = new MySqlCommand(queryId, conn);
                    rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        id = (int)rdr[0];
                    }

                    rdr.Close();

                    insertTagLinks = new StringBuilder("INSERT INTO links(url, tag_id) VALUES");
                    j = 0;

                    foreach (string link in index[tagName])
                    {
                        insertTagLinks.Append($"('{link}', {id})");

                        if (j < index[tagName].Count - 1)
                        {
                            insertTagLinks.Append(",");
                        }
                        else
                        {
                            insertTagLinks.Append(";");
                        }

                        j++;
                    }

                    if (j > 0)
                    {
                        cmd = new MySqlCommand(insertTagLinks.ToString(), conn);

                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                if (Resources.SystemLogger != null)
                {
                    Resources.SystemLogger.Log(e.Message + Environment.NewLine + e.StackTrace);
                }

                Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);

                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }

            conn.Close();
        }

        public void PersistSeriesTags(IDictionary<string, List<string>> index)
        {
            string replacedName;
            string output;
            string querySeriesId;
            string queryTagId;
            StringBuilder insertSeriesTags;
            int seriesId = 0;
            int tagId = 0;
            int i = 0;
            int j;
            MySqlCommand cmd;
            MySqlDataReader rdr;
            MySqlTransaction transaction = null;

            PersistNames(index.Keys.ToList(), "series");

            try
            {
                conn.Open();

                foreach (string seriesName in index.Keys)
                {
                    transaction = conn.BeginTransaction();
                    output = $"Writing series {i++ + 1} out of {index.Keys.Count}.";

                    PrintUtils.PrintRow(output, 0, 0);

                    replacedName = seriesName.Replace("'", "''");

                    querySeriesId = $"SELECT id FROM series WHERE name='{replacedName}'";
                    cmd = new MySqlCommand(querySeriesId, conn);
                    rdr = cmd.ExecuteReader();
                    
                    while (rdr.Read())
                    {
                        seriesId = (int)rdr[0];
                    }

                    rdr.Close();

                    insertSeriesTags = new StringBuilder("INSERT INTO series_tags(tag_id, series_id) VALUES");
                    j = 0;

                    foreach (string tagName in index[seriesName])
                    {
                        replacedName = tagName.Replace("'", "''");
                        queryTagId = $"SELECT id FROM tags WHERE name='{replacedName}'";

                        cmd = new MySqlCommand(queryTagId, conn);
                        rdr = cmd.ExecuteReader();

                        while (rdr.Read())
                        {
                            tagId = (int)rdr[0];
                        }

                        insertSeriesTags.Append($"('{tagId}', {seriesId})");

                        if (j < index[seriesName].Count - 1)
                        {
                            insertSeriesTags.Append(",");
                        }
                        else
                        {
                            insertSeriesTags.Append(";");
                        }

                        j++;
                        rdr.Close();
                    }

                    if (j > 0)
                    {
                        cmd = new MySqlCommand(insertSeriesTags.ToString(), conn);

                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                if (Resources.SystemLogger != null)
                {
                    Resources.SystemLogger.Log(e.Message + Environment.NewLine + e.StackTrace);
                }

                Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);

                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }

            conn.Close();
        }

        private void PersistNames(List<string> names, string tableName)
        {
            string replacedName;
            MySqlTransaction transaction = null;
            MySqlCommand cmd;
            StringBuilder insertNames = new StringBuilder($"INSERT INTO {tableName}(name) VALUES");
            int j = 0;

            try
            {
                conn.Open();
                transaction = conn.BeginTransaction();

                foreach (string s in names)
                {
                    replacedName = s.Replace("'", "''");
                    insertNames.Append($"('{replacedName}')");

                    if (names.Count == 1 || (j > 0 && (j % BatchInsertLimit == 0 || j == names.Count - 1)))
                    {
                        insertNames.Append(";");

                        cmd = new MySqlCommand(insertNames.ToString(), conn);

                        cmd.ExecuteNonQuery();
                        transaction.Commit();

                        transaction = conn.BeginTransaction();
                        insertNames = new StringBuilder($"INSERT INTO {tableName}(name) VALUES");
                    }
                    else
                    {
                        insertNames.Append(",");
                    }

                    j++;
                }
            }
            catch (Exception e)
            {
                if (Resources.SystemLogger != null)
                {
                    Resources.SystemLogger.Log(e.Message + Environment.NewLine + e.StackTrace);
                }

                Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);

                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }

            conn.Close();
        }
    }
}
