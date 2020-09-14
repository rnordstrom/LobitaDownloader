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
        protected string fileExt;

        public FileData(string ext)
        {
            fileExt = ext;
        }
    }

    public class ImageData : FileData
    {
        public string FileExt { get; set; }
        public Bitmap Image { get; set; }

        public ImageData(string ext, Bitmap img) : base(ext)
        {
            FileExt = ext;
            Image = img;
        }
    }

    public static class Constants
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
            "velvet"
        };
        public static string[] VideoCmdHandles = new string[]
        {
            "OP",
            "ED"
        };
    }

    public delegate List<FileData> SourceQuery(string qParam);
    public delegate string CmdToParam(string cmdHandle);

    public class LobitaDownloader
    {
        static void Main(string[] args)
        {
            // Change implementations here
            IDownloader downloader = 
                new BooruDownloader(new FolderImageManager(), new XmlManager());

            try
            {
                // Make sure that the number of log files does not exceed the limit
                Logger.CleanDirectory();

                downloader.Download(Constants.ImageCmdHandles);
            }
            catch(Exception e)
            {
                Logger.Log(e);
            }

            Logger.Log("Program terminated successfully.");
        }
    }
}
