using System;
using System.Collections.Generic;

namespace LobitaDownloader
{
    class FolderVideoManager : FolderManager, IPersistenceManager
    {
        private const string videoDir = "videos";

        public FolderVideoManager() : base(videoDir) { }

        public void Persist(string cmdHandle, List<FileData> imageInfos)
        {
            throw new NotImplementedException();
        }
    }
}
