using LobitaDownloader.Index.Models;
using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexPersistence
    {
        public void PersistCharacters(IDictionary<string, Character> characterIndex);
        public void Clean();
        public bool IsConnected();
    }
}
