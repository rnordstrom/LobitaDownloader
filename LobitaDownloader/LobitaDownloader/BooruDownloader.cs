using System;
using System.Collections.Generic;

namespace LobitaDownloader
{
    class BooruDownloader : IDownloader
    {
        private IPersistenceManager persistence;
        private IConfigManager config;
        private Dictionary<string, string> tagsDict;
        private const string rating = "rating:safe";

        public BooruDownloader(IPersistenceManager pm, IConfigManager cm)
        {
            persistence = pm;
            config = cm;
            tagsDict = new Dictionary<string, string>() 
            {
                {"lysithea", "lysithea_von_ordelia " + rating},
                {"holo", "holo " + rating},
                {"fenrir", "fenrir_(shingeki_no_bahamut) " + rating},
                {"myuri", "myuri_(spice_and_wolf) " + rating},
                {"ryouko", "ookami_ryouko " + rating},
                {"nagatoro", "nagatoro " + rating}
            };
        }

        public void Download(string[] cmdHandles)
        {
            List<string> results = new List<string>();

            foreach (string handle in cmdHandles)
            {
                if(config.CheckAutoMode(handle) == AutoMode.AUTO 
                    && persistence.CheckLastUpdate(handle) < DateTime.Now)
                {
                    results.Add(ApiQuery(ConvertToTag(handle)));
                }
            }

            persistence.Persist(results);
        }

        private string ConvertToTag(string cmdHandle) => tagsDict[cmdHandle];

        private string ApiQuery(string tag)
        {
            return null;
        }
    }
}
