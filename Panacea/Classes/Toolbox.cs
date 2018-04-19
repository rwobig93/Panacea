using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static Panacea.MainWindow;

namespace Panacea.Classes
{
    public static class Toolbox
    {
        public static StringBuilder debugLog = new StringBuilder();
        public static Settings settings = new Settings();
        private static string logDir = string.Format(@"{0}\Logs\", Directory.GetCurrentDirectory());
        private static string exDir = string.Format(@"{0}\Logs\Exceptions\", Directory.GetCurrentDirectory());
        private static Random random = new Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        
        public static void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            var timeStamp = string.Format("{0}--{1}", DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), DateTime.Now.ToLocalTime().ToLongTimeString());
            string exString = string.Format("Timestamp: {0}{1}Caller: {2} at line {3}{1}ExType: {4}{1}HR: {5}{1}Message: {6}{1}StackTrace:{1} {7}{1}Path: {8}{1}", timeStamp, Environment.NewLine, caller, lineNum, ex.GetType().Name, ex.HResult, ex.Message, ex.StackTrace, path);
            using (StreamWriter sw = File.AppendText(string.Format(@"{0}\Exception_{1}.log", exDir, DateTime.Now.ToLocalTime().ToString("MM-dd-yy"))))
                sw.WriteLine(exString);
        }

        public static void LogException(Exception ex, int lineNum, string caller, string path, string dupe)
        {
            var timeStamp = string.Format("{0}--{1}", DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), DateTime.Now.ToLocalTime().ToLongTimeString());
            string exString = string.Format("Timestamp: {0}{1}Caller: {2} at line {3}{1}ExType: {4}{1}HR: {5}{1}Message: {6}{1}StackTrace:{1} {7}{1}Path: {8}{1}", timeStamp, Environment.NewLine, caller, lineNum, ex.GetType().Name, ex.HResult, ex.Message, ex.StackTrace, path);
            using (StreamWriter sw = File.AppendText(string.Format(@"{0}\Exception_{1}_{2}.log", exDir, DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), dupe)))
                sw.WriteLine(exString);
        }

        public static void AnimateGrid(Grid grd, System.Windows.Thickness toThickness)
        {
            ThicknessAnimation animation = new ThicknessAnimation()
            {
                AccelerationRatio = .9,
                Duration = TimeSpan.FromSeconds(.3),
                To = toThickness
            };
            grd.BeginAnimation(Grid.MarginProperty, animation);
        }

        public static void AnimateListBox(ListBox lb, System.Windows.Thickness toThickness)
        {
            ThicknessAnimation animation = new ThicknessAnimation()
            {
                AccelerationRatio = .9,
                Duration = TimeSpan.FromSeconds(.3),
                To = toThickness
            };
            lb.BeginAnimation(ListBox.MarginProperty, animation);
        }

        public static int GenerateRandomNumber()
        {
            return random.Next();
        }

        public static int GenerateRandomNumber(int max)
        {
            return random.Next(max);
        }

        public static int GenerateRandomNumber(int min, int max)
        {
            return random.Next(min, max);
        }

        public static void uAddDebugLog(string _log, DebugType _type = DebugType.INFO)
        {
            try
            {
                debugLog.Append($"{DateTime.Now.ToLocalTime().ToString("MM-dd-yy")}_{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {_type.ToString()}: {_log}{Environment.NewLine}");
#if DEBUG
                Events.AddDebugStatus(_log, _type); 
#endif
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
    }
}
