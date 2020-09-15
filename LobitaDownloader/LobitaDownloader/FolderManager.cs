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

        protected DirectoryInfo InitDirectory(string cmdHandle)
        {
            DirectoryInfo di = Directory.CreateDirectory(Path.Join(DataDirectory.FullName, cmdHandle));

            CleanUp(di);

            return di;
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
