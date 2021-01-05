using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexPersistence
    {
        public void PersistTagLinks(IDictionary<string, List<string>> index);
        public void PersistSeriesTags(IDictionary<string, HashSet<string>> index);
        public void CleanTagLinks();
        public void CleanSeriesTags();
        public IDictionary<string, List<string>> GetTagIndex();
        public IDictionary<string, HashSet<string>> GetSeriesIndex();
        public bool IsConnected();

    }
}
