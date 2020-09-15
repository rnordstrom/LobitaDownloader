using System;
using System.Collections.Generic;
using System.IO;

namespace LobitaDownloader
{
    class FolderVideoManager : FolderManager, IPersistenceManager
    {
        private const string videoDir = "videos";

        public FolderVideoManager() : base(videoDir) { }

        public void Persist(string cmdHandle, List<FileData> videoData)
        {
            Console.WriteLine($"Storing videos for {cmdHandle}...");

            DirectoryInfo di = InitDirectory(cmdHandle);

            foreach (VideoData video in videoData)
            {
                File.WriteAllBytesAsync(Path.Join(di.FullName, video.FileName + video.FileExt), video.Video);
            }
        }
    }
}
