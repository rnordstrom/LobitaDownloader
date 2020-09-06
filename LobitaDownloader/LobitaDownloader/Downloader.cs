﻿using System;
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

        protected void Download(string[] cmdHandles, SourceQuery apiQuery, CmdToParam toParam)
        {
            foreach (string handle in cmdHandles)
            {
                if (config.CheckAutoMode(handle) == AutoMode.AUTO
                    && persistence.CheckLastUpdate(handle) < DateTime.Now)
                {
                    persistence.Persist(handle, apiQuery(toParam(handle)));
                }
            }
        }
    }
}
