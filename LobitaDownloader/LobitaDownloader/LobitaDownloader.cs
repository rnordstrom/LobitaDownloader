using System.Collections.Generic;
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
        public byte[] Bytes { get; set; }
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
            "nagatoro"};
    }

    public delegate List<ImageInfo> SourceQuery(List<string> qParams);
    public delegate string CmdToParam(string cmdHandle);

    public class LobitaDownloader
    {
        static void Main(string[] args)
        {
            // Change implementations here
            IDownloader downloader = 
                new BooruDownloader(new FolderManager(), new XmlManager());

            downloader.Download(Constants.CmdHandles);
        }
    }
}
