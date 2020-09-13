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

    public struct ImageInfo
    {
        public string FileExt { get; set; }
        public Bitmap Image { get; set; }
    }

    public static class Constants
    {
        public static string WorkingDirectory { get; } 
            = Directory.GetCurrentDirectory();
        public static string[] CmdHandles = new string[] {
            "lysithea",
            "holo",
            "fenrir",
            "myuri",
            "ryouko",
            "nagatoro",
            "velvet"};
    }

    public delegate List<ImageInfo> SourceQuery(string qParam);
    public delegate string CmdToParam(string cmdHandle);

    public class LobitaDownloader
    {
        static void Main(string[] args)
        {
            // Change implementations here
            IDownloader downloader = 
                new BooruDownloader(new FolderManager(), new XmlManager());

            try
            {
                // Make sure that the number of log files does not exceed the limit
                Logger.CleanDirectory();

                downloader.Download(Constants.CmdHandles);
            }
            catch(Exception e)
            {
                Logger.Log(e.Message);
            }

            Logger.Log("Program terminated successfully.");
        }
    }
}
