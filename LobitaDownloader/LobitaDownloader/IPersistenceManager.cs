using System;
using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IPersistenceManager
    {
        public void Persist(List<string> images);
        public DateTime CheckLastUpdate(string cmdHandle);
    }
}
