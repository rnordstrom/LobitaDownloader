using System;
using System.Collections.Immutable;
using System.Linq;

namespace LobitaDownloader
{
    enum AutoMode
    {
        AUTO,
        MANUAL
    }

    class LobitaDownloader
    {
        static void Main(string[] args)
        {
            ImmutableArray<string> cmdHandles 
                = ImmutableArray.Create<string>(new string[] {
                    "lysithea", 
                    "holo", 
                    "fenrir",
                    "myuri",
                    "ryouko",
                    "nagatoro"});

            // Change implementations here
            IDownloader downloader = 
                new BooruDownloader(new FolderManager(), new XmlManager());

            downloader.Download(cmdHandles.ToArray<string>());
        }
    }
}
