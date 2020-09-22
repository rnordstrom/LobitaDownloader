using System;
using System.IO;

namespace LobitaDownloader
{
    public class Logger
    {
        private static readonly int BackLogDays = 30;

        public DirectoryInfo LogDirectory { get; }
        public static string FileExt { get; } = ".txt";

        public Logger(string dirName)
        {
            LogDirectory = new DirectoryInfo(Path.Join(Constants.WorkingDirectory, dirName));
            LogDirectory.Create();

            Log($"[{DateTime.Now}]");
            CleanDirectory();
        }

        public void Log(string msg)
        {
            using (StreamWriter fs = GetLogFileStream())
            {
                fs.WriteLine(msg);
                fs.WriteLine();
            }
        }

        public void Log(Exception e)
        {
            Log(e.Message + Environment.NewLine + e.StackTrace);
        }

        // Creates a log file named after today's date (sans time!) or returns it if it exists
        private StreamWriter GetLogFileStream()
        {
            string filePath = Path.Join(LogDirectory.FullName, DateTime.Today.Date.ToShortDateString() + FileExt);
            FileInfo logFile = new FileInfo(filePath);
            StreamWriter logStream;
            
            logStream = logFile.AppendText();

            return logStream;
        }

        // Removes the oldest log files if the total number of files is greater than intended
        public void CleanDirectory()
        {
            FileInfo[] files = LogDirectory.GetFiles();

            if(files.Length > BackLogDays)
            {
                int difference = files.Length - BackLogDays;
                int extLength = FileExt.Length;

                Array.Sort(files, 
                    (x, y) => DateTime.Parse(x.Name.Substring(0, x.Name.Length - extLength))
                        .CompareTo(DateTime.Parse(y.Name.Substring(0, y.Name.Length - extLength))));

                for (int i = 0; i < difference; i++)
                {
                    files[i].Delete();
                }
            }
        }
    }
}
