using System;

namespace LobitaDownloader
{
    public abstract class Downloader
    {
        private IPersistenceManager persistence;
        private IConfigManager config;

        protected Downloader(IPersistenceManager pm, IConfigManager cm)
        {
            persistence = pm;
            config = cm;
        }

        protected void Download(string[] cmdHandles, SourceQuery query, CmdToParam toParam)
        {
            foreach (string handle in cmdHandles)
            {
                if (config.CheckAutoMode(handle) == AutoMode.AUTO)
                {
                    Console.WriteLine($"Downloading images for {handle}...");

                    persistence.Persist(handle, query(toParam(handle)));
                }
            }
        }
    }
}
