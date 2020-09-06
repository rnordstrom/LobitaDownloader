using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;

namespace LobitaDownloader
{
    public class FolderManager : IPersistenceManager
    {
        private const string dataDir = "data";

        public DirectoryInfo DataDirectory { get; }

        public FolderManager()
        {
            DataDirectory = Directory.CreateDirectory(Path.Join(Constants.WorkingDirectory, dataDir));
        }

        public DateTime CheckLastUpdate(string cmdHandle)
        {
            string cmdDir = Path.Join(DataDirectory.FullName, cmdHandle);

            if (Directory.Exists(cmdDir))
            {
                return Directory.GetLastWriteTime(cmdDir);
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        public void Persist(string cmdHandle, List<ImageInfo> imageInfos)
        {
            DirectoryInfo di = Directory.CreateDirectory(Path.Join(DataDirectory.FullName, cmdHandle));
            int counter = 1;
            FileStream fs;
            
            // Names all files for a given command 1 - n, where n equals the number of files
            foreach (var info in imageInfos)
            {
                fs = File.Create(Path.Join(di.FullName, (counter++).ToString() + info.FileExt));
                fs.Write(info.Bytes, 0, info.Bytes.Length);
                fs.Close();
            }
        }
    }
}
