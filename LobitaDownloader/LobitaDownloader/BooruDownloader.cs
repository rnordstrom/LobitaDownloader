using System.Collections.Generic;

namespace LobitaDownloader
{
    public class BooruDownloader : Downloader, IDownloader
    {
        private Dictionary<string, string> tagsDict;

        public BooruDownloader(IPersistenceManager pm, IConfigManager cm) : base(pm, cm)
        {
            tagsDict = new Dictionary<string, string>() 
            {
                {Constants.CmdHandles[0], "lysithea_von_ordelia"},
                {Constants.CmdHandles[1], "holo"},
                {Constants.CmdHandles[2], "fenrir_(shingeki_no_bahamut)"},
                {Constants.CmdHandles[3], "myuri_(spice_and_wolf)"},
                {Constants.CmdHandles[4], "ookami_ryouko"},
                {Constants.CmdHandles[5], "nagatoro"}
            };
        }

        public void Download(string[] cmdHandles)
        {
            base.Download(cmdHandles, ApiQuery, ConvertToTag);
        }

        private static List<ImageInfo> ApiQuery(string qParam)
        {
            return null;
        }

        private string ConvertToTag(string cmdHandle) => tagsDict[cmdHandle];
    }
}
