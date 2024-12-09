using System.Diagnostics;

namespace GenshinFilePlayer
{
    public class Log
    {
        public static void Write(string message)
        {
            Trace.WriteLine(message);
            MainWindow.GetInstance()!.WriteLog(message);
        }
    }
}
