using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexPersistence
    {
        public void PersistTagLinks(IDictionary<string, List<string>> index);
        public void PersistSeriesTags(IDictionary<string, HashSet<string>> index);
        public void CleanTagLinks();
        public void CleanSeriesTags();
        IDictionary<string, List<string>> GetTagIndex();
        IDictionary<string, HashSet<string>> GetSeriesIndex();
    }
}
