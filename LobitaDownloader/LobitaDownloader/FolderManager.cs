using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;

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

        public void Persist(string cmdHandle, List<ImageInfo> imageInfos)
        {
            Console.WriteLine($"Storing images for {cmdHandle}...");

            DirectoryInfo di = Directory.CreateDirectory(Path.Join(DataDirectory.FullName, cmdHandle));
            int counter = 1;
            string fileName;
            ImageFormat imgFormat;

            // Delete all existing files before new writes
            CleanUp(di);
            
            // Names all files for a given command 1 - n, where n equals the number of files
            foreach (var info in imageInfos)
            {
                fileName = Path.Join(di.FullName, (counter++).ToString() + info.FileExt);

                if(info.FileExt == ".jpg")
                {
                    imgFormat = ImageFormat.Jpeg;
                }
                else if(info.FileExt == ".png")
                {
                    imgFormat = ImageFormat.Png;
                }

                info.Image.Save(fileName);
            }
        }

        private void CleanUp(DirectoryInfo d)
        {
            foreach (FileInfo f in d.GetFiles())
            {
                f.Delete();
            }
        }
    }
}
