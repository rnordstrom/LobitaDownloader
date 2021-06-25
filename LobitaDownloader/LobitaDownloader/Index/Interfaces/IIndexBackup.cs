using LobitaDownloader.Index.Models;
using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexBackup
    {
        public void MarkForUpdate(List<string> characterNames);
        public void BackupCharacterData(IDictionary<string, Character> index);
        public void IndexCharacters(IDictionary<string, Character> index);
        public void IndexSeries(IDictionary<string, Series> index);
        public IDictionary<string, Character> GetCharacterIndex(ModificationStatus status, int batchSize = -1);
        public IDictionary<string, Series> GetSeriesIndex();
        public bool IsConnected();
    }
}
