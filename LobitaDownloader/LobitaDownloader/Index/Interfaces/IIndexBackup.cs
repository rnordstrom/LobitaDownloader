using LobitaDownloader.Index.Models;
using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexBackup
    {
        public void DeleteDataDocuments();
        public void ResetDocumentStatus();
        public void MarkForUpdate(List<string> characterNames);
        public void MarkAsDone(List<string> characterNames);
        public void MarkAsSaved(List<string> characterNames);
        public void MarkAsUnsaved(List<string> characterNames);
        public void IndexCharacters(IDictionary<string, Character> index);
        public void IndexSeries(IDictionary<string, Series> index);
        public void WriteCharacterData(IDictionary<string, Character> index);
        public bool ReadCharacterData(PersistenceStatus status, int batchSize, out IDictionary<string, Character> data);
        public IDictionary<string, Character> GetCharacterIndex(ModificationStatus status, int batchSize);
        public IDictionary<string, Series> GetSeriesIndex();
        public bool IsConnected();
    }
}
