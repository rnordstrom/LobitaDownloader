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

        public ImageData(string ext, Bitmap img) : base(ext)
        {
            Image = img;
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
            IDownloader imageDownloader = 
                new BooruDownloader(new FolderImageManager(), new XmlManager());
            IDownloader videoDownloader =
                new VideoThemeDownloader(new FolderVideoManager(), new XmlManager());

            try
            {
                // Make sure that the number of log files does not exceed the limit
                Logger.CleanDirectory();

                //imageDownloader.Download(Constants.ImageCmdHandles);
                videoDownloader.Download(Constants.VideoCmdHandles);
            }
            catch(Exception e)
            {
                Logger.Log(e);
            }

            Logger.Log("Program terminated successfully.");
        }
    }
}
