using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Panacea.Windows;
using static Panacea.MainWindow;

namespace Panacea.Classes
{
    public static class Toolbox
    {
        private static string ExceptionDirectory { get; } = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Logs\Exceptions\";
        public static StringBuilder debugLog = new StringBuilder();
        public static Settings settings = new Settings();
        public static List<ChangeLogItem> changeLogs = new List<ChangeLogItem>();
        private static Random random = new Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        
        public static void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            var timeStamp = string.Format("{0}--{1}", DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), DateTime.Now.ToLocalTime().ToLongTimeString());
            string exString = string.Format("Timestamp: {0}{1}Caller: {2} at line {3}{1}ExType: {4}{1}HR: {5}{1}Message: {6}{1}StackTrace:{1} {7}{1}Path: {8}{1}", timeStamp, Environment.NewLine, caller, lineNum, ex.GetType().Name, ex.HResult, ex.Message, ex.StackTrace, path);
            using (StreamWriter sw = File.AppendText(string.Format(@"{0}\Exception_{1}.log", ExceptionDirectory, DateTime.Now.ToLocalTime().ToString("MM-dd-yy"))))
                sw.WriteLine(exString);
        }

        public static void LogException(Exception ex, int lineNum, string caller, string path, string dupe)
        {
            var timeStamp = string.Format("{0}--{1}", DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), DateTime.Now.ToLocalTime().ToLongTimeString());
            string exString = string.Format("Timestamp: {0}{1}Caller: {2} at line {3}{1}ExType: {4}{1}HR: {5}{1}Message: {6}{1}StackTrace:{1} {7}{1}Path: {8}{1}", timeStamp, Environment.NewLine, caller, lineNum, ex.GetType().Name, ex.HResult, ex.Message, ex.StackTrace, path);
            using (StreamWriter sw = File.AppendText(string.Format(@"{0}\Exception_{1}_{2}.log", ExceptionDirectory, DateTime.Now.ToLocalTime().ToString("MM-dd-yy"), dupe)))
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

        public static void AnimateScrollviewer(ScrollViewer scroll, System.Windows.Thickness toThickness)
        {
            ThicknessAnimation animation = new ThicknessAnimation()
            {
                AccelerationRatio = .9,
                Duration = TimeSpan.FromSeconds(.3),
                To = toThickness
            };
            scroll.BeginAnimation(ScrollViewer.MarginProperty, animation);
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

        public static void AnimateRectangle(Rectangle rect, System.Windows.Thickness toThickness)
        {
            ThicknessAnimation animation = new ThicknessAnimation()
            {
                AccelerationRatio = .9,
                Duration = TimeSpan.FromSeconds(.3),
                To = toThickness
            };
            rect.BeginAnimation(Rectangle.MarginProperty, animation);
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

        public static void uAddDebugLog(string _log, DebugType _type = DebugType.INFO, string caller = "")
        {
            try
            {
                debugLog.Append($"{DateTime.Now.ToLocalTime().ToString("MM-dd-yy")}_{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {caller.ToUpper()} :: {_type.ToString()}: {_log}{Environment.NewLine}");
                if (Toolbox.debugLog.Length > 10000)
                {
                    Toolbox.debugLog.Append($"{DateTime.Now.ToLocalTime().ToString("MM-dd-yy")}_{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {caller.ToUpper()} :: {_type.ToString()}: Dumping Debug Logs...{Environment.NewLine}");
                    Director.Main.DumpDebugLog();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static Version GetVersionNumber(string assemblyPath)
        {
            return Assembly.LoadFile(assemblyPath).GetName().Version;
        }

        private static string FormatResourceName(Assembly assembly, string resourceName)
        {
            return assembly.GetName().Name + "." + resourceName.Replace(" ", "_")
                                                               .Replace("\\", ".")
                                                               .Replace("/", ".");
        }

        public static Stream GetEmbeddedResource(string resourceName, Assembly assembly)
        {
            Stream desiredStream;
            resourceName = FormatResourceName(assembly, resourceName);
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    return null;

                desiredStream = resourceStream;
            }
            return desiredStream;
        }

        public static Color ColorFromHex(string colorHex)
        {
            return (Color)ColorConverter.ConvertFromString(colorHex);
        }

        public static SolidColorBrush SolidBrushFromHex(string colorHex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }

        public static ComboBoxItem FindComboBoxItemByString(ComboBox combo, string v)
        {
            ComboBoxItem foundItem = null;
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content.ToString().ToLower() == v.ToLower())
                    foundItem = item;
            }
            return foundItem;
        }

        public static string GetMD5Hash(string input)
        {
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            using (MD5 md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
            }

            return sBuilder.ToString();
        }

        public static string GetMD5Hash(string[] input)
        {
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            using (MD5 md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input.ToString()));

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
            }

            return sBuilder.ToString();
        }

        public static bool VerifyMD5Hash(string input, string hash) // Need to add in an MD5 hash arg in the constructor instead of using the default
        {
            // Hash the input.
            string hashOfInput = GetMD5Hash(input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
