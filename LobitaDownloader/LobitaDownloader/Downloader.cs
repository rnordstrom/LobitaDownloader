using System;
using System.Collections.Generic;

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

        protected void Download(string[] cmdHandles, SourceQuery apiQuery, CmdToParam ctp)
        {
            List<string> qParams = new List<string>();

            foreach (string handle in cmdHandles)
            {
                if (config.CheckAutoMode(handle) == AutoMode.AUTO
                    && persistence.CheckLastUpdate(handle) < DateTime.Now)
                {
                    qParams.Add(ctp(handle));
                    persistence.Persist(handle, apiQuery(qParams));
                    qParams.Clear();
                }
            }
        }
    }
}
