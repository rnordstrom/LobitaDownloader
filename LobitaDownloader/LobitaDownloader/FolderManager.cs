using System.IO;

namespace LobitaDownloader
{
    public abstract class FolderManager
    {
        public DirectoryInfo DataDirectory { get; }

        public FolderManager(string dirName)
        {
            DataDirectory = Directory.CreateDirectory(Path.Join(Constants.WorkingDirectory, dirName));
        }

        protected void CleanUp(DirectoryInfo d)
        {
            foreach (FileInfo f in d.GetFiles())
            {
                f.Delete();
            }
        }
    }
}
