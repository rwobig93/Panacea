using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Upstaller
{
    public static class Toolbox
    {
        public static StringBuilder debugLog = new StringBuilder();
        private static string logDir = string.Format(@"{0}\Logs\", Directory.GetCurrentDirectory());
        private static string exDir = string.Format(@"{0}\Logs\Exceptions\", Directory.GetCurrentDirectory());
        private static Random random = new Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));

        public static void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            var timeStamp = string.Format("{0}--{1}", DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), DateTime.Now.ToLocalTime().ToLongTimeString());
            string exString = string.Format("Timestamp: {0}{1}Caller: {2} at line {3}{1}ExType: {4}{1}HR: {5}{1}Message: {6}{1}StackTrace:{1} {7}{1}Path: {8}{1}", timeStamp, Environment.NewLine, caller, lineNum, ex.GetType().Name, ex.HResult, ex.Message, ex.StackTrace, path);
            using (StreamWriter sw = File.AppendText(string.Format(@"{0}\Upstaller_Exception_{1}.log", exDir, DateTime.Now.ToLocalTime().ToString("MM-dd-yy"))))
                sw.WriteLine(exString);
        }

        public static void LogException(Exception ex, int lineNum, string caller, string path, string dupe)
        {
            var timeStamp = string.Format("{0}--{1}", DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), DateTime.Now.ToLocalTime().ToLongTimeString());
            string exString = string.Format("Timestamp: {0}{1}Caller: {2} at line {3}{1}ExType: {4}{1}HR: {5}{1}Message: {6}{1}StackTrace:{1} {7}{1}Path: {8}{1}", timeStamp, Environment.NewLine, caller, lineNum, ex.GetType().Name, ex.HResult, ex.Message, ex.StackTrace, path);
            using (StreamWriter sw = File.AppendText(string.Format(@"{0}\Upstaller_Exception_{1}_{2}.log", exDir, DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), dupe)))
                sw.WriteLine(exString);
        }

        public static void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, string caller = "")
        {
            try
            {
                debugLog.Append($"{DateTime.Now.ToLocalTime().ToString("MM-dd-yy")}_{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {caller.ToUpper()} :: {_type.ToString()}: {_log}{Environment.NewLine}");
                if (debugLog.Length > 2500)
                    DumpDebugLog();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static void DumpDebugLog()
        {
            string _dateNow = DateTime.Now.ToLocalTime().ToString("MM-dd-yy");
            string _debugLocation = ($@"{Directory.GetCurrentDirectory()}\Logs\Upstaller_DebugLog_{_dateNow}.log");
            try
            {
                if (!File.Exists(_debugLocation))
                    using (StreamWriter _sw = new StreamWriter(_debugLocation))
                        _sw.WriteLine(debugLog.ToString());
                else
                    using (StreamWriter _sw = File.AppendText(_debugLocation))
                        _sw.WriteLine(debugLog.ToString());
            }
            catch (IOException io) { SaveFileRetry(_debugLocation, debugLog.ToString(), io.Message); return; }
            catch (Exception ex)
            {
                LogFullException(ex);
            }
            finally
            {
                debugLog.Clear();
            }
        }
        
        private static void LogFullException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            try
            {
                Toolbox.LogException(ex, lineNum, caller, path);
                Toolbox.uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
                Toolbox.uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
            }
        }

        public static void SaveFileRetry(string filePath, string writeString, string message)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    Toolbox.uDebugLogAdd($"Starting file save retry, reason: {message}");
                    string newPath = $@"{filePath.Replace(".", "rt.")}";
                    int tryAttempts = 10;
                    for (int t = 1; t <= tryAttempts; t++)
                    {
                        try
                        {
                            using (StreamWriter sw = File.AppendText(filePath))
                                sw.WriteLine(writeString);
                            Toolbox.uDebugLogAdd($"Successfully saved to file after {t} attempts: {filePath}");
                        }
                        catch (IOException)
                        {
                            if (t == tryAttempts)
                            {
                                try
                                {
                                    Toolbox.uDebugLogAdd($"Max attempts reached saving to \"{filePath}\", now saving to {newPath}");
                                    using (StreamWriter sw = File.AppendText(newPath))
                                        sw.WriteLine(writeString);
                                    return;
                                }
                                catch (IOException io)
                                {
                                    Toolbox.uDebugLogAdd($"Saving to new file also failed, starting new retry method: {newPath}");
                                    SaveFileRetry(newPath, writeString, io.Message);
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogFullException(ex);
                }
            };
            worker.RunWorkerAsync();
        }
    }
    
    public enum DebugType
    {
        EXCEPTION,
        STATUS,
        INFO,
        FAILURE
    }
}
