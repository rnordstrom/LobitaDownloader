﻿using System.Collections.Generic;

namespace LobitaDownloader
{
    interface IIndexBackup
    {
        public void BackupTagLinksFull(IDictionary<string, List<string>> index);
        public void BackupSeriesTagsFull(IDictionary<string, HashSet<string>> index);
        public void BackupSingleTagLinks(string tagName, List<string> links);
        public void BackupSingleSeriesTags(string seriesName, string tagName);
        public void BackupTagNames(List<string> tagNames);
        public void BackupSeriesNames(List<string> seriesNames);
        public IDictionary<string, List<string>> GetTagIndex(ModificationStatus status);
        public IDictionary<string, HashSet<string>> GetSeriesIndex();
        public bool IsConnected();
    }
}