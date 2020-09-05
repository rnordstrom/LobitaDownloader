using System;
using System.Collections.Generic;

namespace LobitaDownloader
{
    class BooruDownloader : IDownloader
    {
        private IPersistenceManager persistence;
        private IConfigManager config;
        private Dictionary<string, string> tagsDict;

        public BooruDownloader(IPersistenceManager pm, IConfigManager cm)
        {
            persistence = pm;
            config = cm;
            tagsDict = new Dictionary<string, string>() 
            {
                {"lysithea", "lysithea_von_ordelia"},
                {"holo", "holo"},
                {"fenrir", "fenrir_(shingeki_no_bahamut)"},
                {"myuri", "myuri_(spice_and_wolf)"},
                {"ryouko", "ookami_ryouko"},
                {"nagatoro", "nagatoro"}
            };
        }

        public void Download(string[] cmdHandles, SourceQuery apiQuery)
        {
            List<string> qParams = new List<string>();

            foreach (string handle in cmdHandles)
            {
                if(config.CheckAutoMode(handle) == AutoMode.AUTO 
                    && persistence.CheckLastUpdate(handle) < DateTime.Now)
                {
                    qParams.Add(ConvertToTag(handle));
                    persistence.Persist(handle, apiQuery(qParams));
                    qParams.Clear();
                }
            }
        }

        private string ConvertToTag(string cmdHandle) => tagsDict[cmdHandle];
    }
}
