using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;

namespace LobitaDownloader
{
    public class FolderImageManager : FolderManager, IPersistenceManager
    {
        private const string imageDir = "images";

        public FolderImageManager() : base(imageDir) { }

        public void Persist(string cmdHandle, List<FileData> imageData)
        {
            Console.WriteLine($"Storing images for {cmdHandle}...");

            DirectoryInfo di = Directory.CreateDirectory(Path.Join(DataDirectory.FullName, cmdHandle));
            int counter = 1;
            string fileName;
            ImageFormat imgFormat;

            // Delete all existing files before new writes
            CleanUp(di);

            // Names all files for a given command 1 - n, where n equals the number of files
            foreach (ImageData image in imageData)
            {
                fileName = Path.Join(di.FullName, (counter++).ToString() + image.FileExt);

                if (image.FileExt == ".jpg")
                {
                    imgFormat = ImageFormat.Jpeg;
                }
                else if (image.FileExt == ".png")
                {
                    imgFormat = ImageFormat.Png;
                }

                image.Image.Save(fileName); 
            }
        }
    }
}
