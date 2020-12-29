using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace LobitaDownloader
{
    class DbImageManager : IPersistenceManager
    {
        private string connStr = $"server=localhost;user=root;database=tagdb;port=3306;password={Environment.GetEnvironmentVariable("PWD")}";
        private MySqlConnection conn;

        public DbImageManager()
        {
            conn = new MySqlConnection(connStr);
        }

        public void Clean()
        {
            conn.Open();

            string deleteTags = "DELETE FROM tags";
            string resetTagsId = "ALTER TABLE tags AUTO_INCREMENT = 1";
            string resetLinksId = "ALTER TABLE links AUTO_INCREMENT = 1";

            MySqlCommand cmd = new MySqlCommand(deleteTags, conn);

            cmd.ExecuteNonQuery();

            cmd = new MySqlCommand(resetTagsId, conn);

            cmd.ExecuteNonQuery();

            cmd = new MySqlCommand(resetLinksId, conn);

            cmd.ExecuteNonQuery();

            conn.Close();
        }

        public void Persist(string name, List<FileData> fileDatas)
        {
            try
            {
                conn.Open();

                name = name.Replace("'", "''");

                string insertTag = $"INSERT INTO tags(name) VALUES('{name}')";
                MySqlCommand cmd = new MySqlCommand(insertTag, conn);

                cmd.ExecuteNonQuery();

                string queryId = $"SELECT id FROM tags WHERE name='{name}'";
                cmd = new MySqlCommand(queryId, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                int id = int.MaxValue;

                while (rdr.Read())
                {
                    id = (int) rdr[0];
                }

                rdr.Close();

                string insertLink;

                foreach (LinkData ld in fileDatas)
                {
                    insertLink = $"INSERT INTO links(url, tag_id) VALUES('{ld.Link}', {id})";
                    cmd = new MySqlCommand(insertLink, conn);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Resources.SystemLogger.Log(e);
            }

            conn.Close();
        }

        public void PersistBatch(IDictionary<string, List<FileData>> fileIndex)
        {
            string replacedName;
            string output;
            int windowRemainder;
            int i = 0;
            MySqlTransaction transaction = null;

            try
            {
                conn.Open();

                foreach (string name in fileIndex.Keys)
                {
                    transaction = conn.BeginTransaction();

                    output = $"Writing tag {i++ + 1} out of {fileIndex.Keys.Count}.";
                    windowRemainder = Console.WindowWidth - output.Length;

                    Console.SetCursorPosition(0, 0);
                    Console.Write(output + new string(' ', windowRemainder));

                    replacedName = name.Replace("'", "''");

                    string insertTag = $"INSERT INTO tags(name) VALUES('{replacedName}')";
                    MySqlCommand cmd = new MySqlCommand(insertTag, conn);

                    cmd.ExecuteNonQuery();

                    string queryId = $"SELECT id FROM tags WHERE name='{replacedName}'";
                    cmd = new MySqlCommand(queryId, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    int id = int.MaxValue;

                    while (rdr.Read())
                    {
                        id = (int)rdr[0];
                    }

                    rdr.Close();

                    StringBuilder insertLinks = new StringBuilder("INSERT INTO links(url, tag_id) VALUES");
                    int j = 0;

                    foreach (LinkData ld in fileIndex[name])
                    {
                        insertLinks.Append($"('{ld.Link}', {id})");

                        if (j < fileIndex[name].Count - 1)
                        {
                            insertLinks.Append(",");
                        }
                        else
                        {
                            insertLinks.Append(";");
                        }

                        j++;
                    }

                    if (j > 0)
                    {
                        cmd = new MySqlCommand(insertLinks.ToString(), conn);

                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                Resources.SystemLogger.Log(e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }

            conn.Close();
        }
    }
}
