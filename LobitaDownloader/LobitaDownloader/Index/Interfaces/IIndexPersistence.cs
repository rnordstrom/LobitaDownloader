using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexPersistence
    {
        public void PersistTagLinks(IDictionary<string, List<string>> index);
        public void PersistSeriesTags(IDictionary<string, List<string>> index);
        public void Clean();
    }
}
