using System.Collections.Generic;

namespace LobitaDownloader
{
    public interface IPersistenceManager
    {
        public void Persist(string name, List<FileData> fileInfos);
        public void PersistBatch(IDictionary<string, List<FileData>> fileIndex);

        public void Clean();
    }
}
