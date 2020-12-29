using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace LobitaDownloader
{
    public enum AutoMode
    {
        AUTO,
        MANUAL
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

    public class LinkData : FileData
    {
        public string Link { get; }

        public LinkData(string link) : base(null)
        {
            Link = link;
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
    }

    public delegate List<FileData> SourceQuery(string qParam);
    public delegate string CmdToParam(string cmdHandle);

    public class LobitaDownloader
    {
        static int Main(string[] args)
        {
            string usageString = "Usage: LobitaDownloader <images|videos>";

            if (args.Length != 1)
            {
                Console.WriteLine(usageString);

                return -1;
            }

            Resources.SystemLogger = new Logger("syslogs");

            try
            {
                switch (args[0])
                {
                    case "images":
                        Resources.ImageLogger = new Logger("images_logs");
                        IDownloader imageDownloader =
                            new ImageDownloader(new FolderImageManager(), new XmlManager());
                        imageDownloader.Download(Resources.ImageCmdHandles);
                        break;
                    case "videos":
                        Resources.VideoLogger = new Logger("videos_logs");
                        IDownloader videoDownloader =
                            new VideoThemeDownloader(new FolderVideoManager(), new XmlManager());
                        videoDownloader.Download(Resources.VideoCmdHandles);
                        break;
                    case "index":
                        IndexBuilder indexBuilder =
                            new IndexBuilder(new DbImageManager());
                        indexBuilder.BuildIndex();
                        break;
                    default:
                        Console.WriteLine(usageString);
                        return -1;
                }
            }
            catch(Exception e)
            {
                Resources.SystemLogger.Log(e);
            }

            Resources.SystemLogger.Log("Application terminated successfully.");

            return 0;
        }
    }
}
