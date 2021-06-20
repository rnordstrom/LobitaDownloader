using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexBackup
    {
        public void MarkForUpdate(List<string> tagNames);
        public void BackupTagLinks(IDictionary<string, List<string>> index);
        public void BackupSeriesTags(IDictionary<string, HashSet<string>> index);
        public void BackupTagNames(List<string> tagNames);
        public void BackupSeriesNames(List<string> seriesNames);
        public IDictionary<string, List<string>> GetTagIndex(ModificationStatus status, int batchSize = -1);
        public IDictionary<string, HashSet<string>> GetSeriesIndex();
        public bool IsConnected();
    }
}
