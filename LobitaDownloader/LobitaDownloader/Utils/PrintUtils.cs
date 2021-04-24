using System;

namespace LobitaDownloader
{
    public static class PrintUtils
    {
        public static void PrintRow(string text, int row, int col)
        {
            try
            {
                int windowRemainder = Console.WindowWidth - text.Length;

                if (windowRemainder < 0)
                {
                    windowRemainder = 0;
                }

                Console.SetCursorPosition(row, col);
                Console.Write(text + new string(' ', windowRemainder));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        public static void Report(Exception e)
        {
            if (Resources.SystemLogger != null)
            {
                Resources.SystemLogger.Log(e.Message + Environment.NewLine + e.StackTrace);
            }

            Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
        }
    }
}
