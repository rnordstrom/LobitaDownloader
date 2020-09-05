using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IDownloader
    {
        public void Download(string[] cmdHandles, SourceQuery query);
    }
}
