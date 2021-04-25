using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace LobitaDownloader
{
    public enum AutoMode
    {
        AUTO,
        MANUAL,
        INDETERMINATE
    }

    public abstract class FileData
    {
        public string FileExt { get; }

        public FileData(string ext)
        {
            FileExt = ext;
        }
    }

    public class ImageData : FileData
    {
        public Bitmap Image { get; }
        public string ID { get; }

        public ImageData(string ext, Bitmap img, string id) : base(ext)
        {
            Image = img;
            ID = id;
        }
    }

    public class VideoData : FileData
    {
        public string FileName { get; }
        public byte[] Video { get; }

        public VideoData(string ext, string fn, byte[] v) : base(ext)
        {
            FileName = fn;
            Video = v;
        }
    }

    public static class Resources
    {
        public static string WorkingDirectory { get; } = Directory.GetCurrentDirectory();
        public static string[] ImageCmdHandles = new string[] 
        {
            "lysithea",
            "holo",
            "fenrir",
            "myuri",
            "ryouko",
            "nagatoro",
            "velvet",
            "hololive"
        };
        public static string[] VideoCmdHandles = new string[]
        {
            "OP",
            "ED"
        };
        public static Logger SystemLogger { get; set; }
        public static Logger ImageLogger { get; set; }
        public static Logger VideoLogger { get; set; }
        public const string ProductionDirectory = "production";
        public const string TestDirectory = "test";
        public const string ConfigFile = "lobitaconfig.xml";
    }

    public delegate List<FileData> SourceQuery(string qParam);
    public delegate string CmdToParam(string cmdHandle);

    public class LobitaDownloader
    {
        static int Main(string[] args)
        {
            string usageString = "Usage: LobitaDownloader index | backup | clean | count>";

            Resources.SystemLogger = new Logger("syslogs");

            XmlConfigManager config = new XmlConfigManager(Resources.ProductionDirectory, Resources.ConfigFile);
            DbIndexPersistence persistence = new DbIndexPersistence(config);
            XmlIndexPersistence backup = new XmlIndexPersistence(config);
            IndexBuilder indexBuilder = new IndexBuilder(persistence, backup, config);

            try
            {
                switch (args[0])
                {
                    case "index":
                        if (CheckConnections(persistence, backup))
                        {
                            indexBuilder.BuildIndex();
                        }
                        else
                        {
                            return -1;
                        }
                        break;
                    case "backup":
                        if (CheckConnections(persistence, backup))
                        {
                            indexBuilder.BackupRestore();
                        }
                        else
                        {
                            return -1;
                        }
                        break;
                    case "clean":
                        if (CheckConnections(persistence, backup))
                        {
                            indexBuilder.CleanUp();
                        }
                        else
                        {
                            return -1;
                        }
                        break;
                    case "count":
                        if (CheckConnections(persistence, backup))
                        {
                            indexBuilder.Count();
                        }
                        else
                        {
                            return -1;
                        }
                        break;
                    default:
                        Console.WriteLine(usageString);
                        return -1;
                }
            }
            catch(Exception e)
            {
                PrintUtils.Report(e);

                return -1;
            }

            Resources.SystemLogger.Log("Application terminated successfully.");

            return 0;
        }

        private static bool CheckConnections(IIndexPersistence persistence, IIndexPersistence backup)
        {
            if (persistence.IsConnected() && backup.IsConnected())
            {
                return true;
            }

            if (!persistence.IsConnected())
            {
                Console.WriteLine("Failed to connect to primary persistence.");
            }
            
            if (!backup.IsConnected())
            {
                Console.WriteLine("Failed to connect to backup storage.");
            }

            return false;
        }
    }
}
