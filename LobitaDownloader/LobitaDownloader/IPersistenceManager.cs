﻿using System;
using System.Collections.Generic;

namespace LobitaDownloader
{
    public interface IPersistenceManager
    {
        public void Persist(string cmdHandle, List<ImageInfo> dataInfo);
        public DateTime CheckLastUpdate(string cmdHandle);
    }
}
