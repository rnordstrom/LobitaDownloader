using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LobitaDownloader
{
    public class FolderImageManager : FolderManager, IPersistenceManager
    {
        private const string imageDir = "images";

        public FolderImageManager() : base(imageDir) { }

        public void Persist(string cmdHandle, List<FileData> imageData)
        {
            Console.WriteLine($"Storing images for {cmdHandle}...");

            DirectoryInfo di = InitDirectory(cmdHandle);
            int counter = 1;
            string fileName;
            ImageFormat imgFormat;

            // Names all files for a given command 1 - n, where n equals the number of files
            foreach (ImageData image in imageData)
            {
                fileName = Path.Join(di.FullName, (counter++).ToString() + image.FileExt);

                if(image.FileExt == ".jpg")
                {
                    imgFormat = ImageFormat.Jpeg;
                }
                else if(image.FileExt == ".png")
                {
                    imgFormat = ImageFormat.Png;
                }
                else if(image.FileExt == ".gif")
                {
                    imgFormat = ImageFormat.Gif;
                }
                else
                {
                    continue;
                }

                try
                {
                    image.Image.Save(fileName);
                }
                catch(ExternalException e)
                {
                    Resources.ImageLogger.Log(e);
                    Resources.ImageLogger.Log($"Error encountered while saving image for {cmdHandle}. ID = {image.ID}.");
                }
            }
        }
    }
}
