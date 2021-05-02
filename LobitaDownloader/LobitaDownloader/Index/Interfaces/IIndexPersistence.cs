using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexPersistence
    {
        public void PersistTagLinks(IDictionary<string, List<string>> index);
        public void PersistSeriesTags(IDictionary<string, HashSet<string>> index);
        public void CleanTagLinks();
        public void CleanSeries();
        public void CountTagLinks();
        public void CountSeriesLinks();
        public bool IsConnected();
    }
}
