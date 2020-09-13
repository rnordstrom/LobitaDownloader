using System;
using System.IO;

namespace LobitaDownloader
{
    public static class Logger
    {
        const int BackLogDays = 30;

        public static DirectoryInfo LogDirectory { get; } 
            = new DirectoryInfo(Path.Join(Constants.WorkingDirectory, "logs"));
        public static string FileExt { get; } = ".txt";

        static Logger()
        {
            LogDirectory.Create();
        }

        public static void Log(string msg)
        {
            using (StreamWriter fs = GetLogFileStream())
            {
                fs.WriteLine($"[{DateTime.Now}]");
                fs.WriteLine(msg);
                fs.WriteLine();
            }
        }

        // Creates a log file named after today's date (sans time!) or returns it if it exists
        private static StreamWriter GetLogFileStream()
        {
            string filePath = Path.Join(LogDirectory.FullName, DateTime.Today.Date.ToShortDateString() + FileExt);
            FileInfo logFile = new FileInfo(filePath);
            StreamWriter logStream;
            
            logStream = logFile.AppendText();

            return logStream;
        }

        // Removes the oldest log files if the total number of files is greater than intended
        public static void CleanDirectory()
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
