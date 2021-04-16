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
            string usageString = "Usage: LobitaDownloader index | backup>";

            Resources.SystemLogger = new Logger("syslogs");

            try
            {
                switch (args[0])
                {
                    /*case "images":
                        Resources.ImageLogger = new Logger("images_logs");
                        IDownloader imageDownloader =
                            new ImageDownloader(new FolderImageManager(), new XmlConfigManager());
                        imageDownloader.Download(Resources.ImageCmdHandles);
                        break;
                    case "videos":
                        Resources.VideoLogger = new Logger("videos_logs");
                        IDownloader videoDownloader =
                            new VideoThemeDownloader(new FolderVideoManager(), new XmlConfigManager("lobitaconfig.xml"));
                        videoDownloader.Download(Resources.VideoCmdHandles);
                        break;*/
                    case "index":
                        XmlConfigManager config = new XmlConfigManager(Resources.ProductionDirectory, Resources.ConfigFile);
                        string dbName = config.GetItemByName("NextDatabase");
                        string backupLocation = config.GetItemByName("BackupLocation");
                        DbIndexPersistence persistence = new DbIndexPersistence(dbName);
                        XmlIndexPersistence backup = new XmlIndexPersistence(backupLocation);
                        IndexBuilder indexBuilder = new IndexBuilder(persistence, backup, config);
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
                        XmlConfigManager config1 = new XmlConfigManager(Resources.ProductionDirectory, Resources.ConfigFile);
                        string dbName1 = config1.GetItemByName("NextDatabase");
                        string backupLocation1 = config1.GetItemByName("BackupLocation");
                        DbIndexPersistence persistence1 = new DbIndexPersistence(dbName1);
                        XmlIndexPersistence backup1 = new XmlIndexPersistence(backupLocation1);
                        IndexBuilder backupIndex = new IndexBuilder(persistence1, backup1, config1);
                        if (CheckConnections(persistence1, backup1))
                        {
                            backupIndex.BackupRestore();
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
                Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
                Resources.SystemLogger.Log(e.Message + Environment.NewLine + e.StackTrace);

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
            else if (!persistence.IsConnected())
            {
                Console.WriteLine("Failed to connect to primary persistence.");
            }
            else if (!backup.IsConnected())
            {
                Console.WriteLine("Failed to connect to backup storage.");
            }
            else
            {
                Console.WriteLine("Failed to connect to primary persistence and backup storage.");
            }

            return false;
        }
    }
}
