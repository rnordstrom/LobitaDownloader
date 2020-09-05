using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LobitaDownloader
{
    public enum AutoMode
    {
        AUTO,
        MANUAL
    }

    public struct ImageInfo
    {
        public string FileExt { get; }
        public byte[] Bytes { get; }
    }

    public delegate List<ImageInfo> SourceQuery(List<string> qParams);

    class LobitaDownloader
    {
        static void Main(string[] args)
        {
            string workingDir = new FileInfo(Assembly.GetExecutingAssembly().Location).FullName;
            string[] cmdHandles = new string[] {
                "lysithea",
                "holo",
                "fenrir",
                "myuri",
                "ryouko",
                "nagatoro"};

            // Change implementations here
            IDownloader downloader = 
                new BooruDownloader(new FolderManager(workingDir), new XmlManager(workingDir));
            SourceQuery query = ApiQuery;

            downloader.Download(cmdHandles, query);
        }

        private static List<ImageInfo> ApiQuery(List<string> parameters)
        {
            return null;
        }
    }
}
