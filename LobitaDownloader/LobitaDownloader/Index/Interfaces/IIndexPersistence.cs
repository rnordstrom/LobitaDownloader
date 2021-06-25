using LobitaDownloader.Index.Models;
using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexPersistence
    {
        public void PersistCharacters(IDictionary<string, Character> index);
        public void PersistSeries(IDictionary<string, Series> index);
        public void CleanCharacters();
        public void CleanSeries();
        public void CountCharacterPosts(IDictionary<string, Character> index);
        public void CountSeriesPosts(IDictionary<string, Series> seriesIndex, IDictionary<string, Character> characterIndex);
        public bool IsConnected();
    }
}
