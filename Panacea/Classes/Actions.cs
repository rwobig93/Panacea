using Microsoft.Win32;
using Panacea.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Classes
{
    public static class Actions
    {
        private static void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            Director.Main.LogException(ex, lineNum, caller, path);
        }

        private static void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            Director.Main.uDebugLogAdd(_log, _type, caller);
        }

        public static void SendDiagnostics()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    uDebugLogAdd("Attempting to send diagnostic info...");
                    var diagDir = $@"{Director.Main.CurrentDirectory}\Diag";
                    if (!Directory.Exists(diagDir))
                    {
                        uDebugLogAdd($"Diag directory not found, creating: {diagDir}");
                        Directory.CreateDirectory(diagDir);
                        uDebugLogAdd("Created diagDir");
                    }
                    var zipName = $"Panacea_Diag_{ DateTime.Today.ToString("MM_dd_yy")}";
                    var zipPath = $@"{diagDir}\{zipName}.zip";
                    uDebugLogAdd($"zipPath: {zipPath}");
                    if (File.Exists(zipPath))
                    {
                        zipPath = $@"{diagDir}\{zipName}{Toolbox.GenerateRandomNumber(0, 1000)}.zip";
                        uDebugLogAdd($"Diag zip file for today already exists, added random string: {zipPath}");
                    }
                    Director.Main.DumpDebugLog();
                    ZipFile.CreateFromDirectory(Director.Main.LogDirectory, zipPath);
                    using (SmtpClient smtp = new SmtpClient("mail.wobigtech.net"))
                    {
                        uDebugLogAdd("Generating mail");
                        MailMessage message = new MailMessage();
                        MailAddress from = new MailAddress("PanaceaLogs@WobigTech.net");
                        smtp.UseDefaultCredentials = true;
                        smtp.Timeout = TimeSpan.FromMinutes(5.0).Seconds;

                        message.From = from;
                        message.Subject = $"Panacea Diag {DateTime.Today.ToString("MM-dd-yy_hh:mm_tt")}";
                        message.IsBodyHtml = false;
                        message.Body = "Panacea Diag Attached";
                        message.To.Add("rick@wobigtech.net");
                        message.Attachments.Add(new Attachment(zipPath) { Name = $"{zipName}.zip" });
                        uDebugLogAdd("Attempting to send mail");
                        smtp.Send(message);
                        uDebugLogAdd("Mail sent");
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        public static void AddToWindowsStartup(bool startup = true)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                bool keyExists = CheckWinStartupRegKeyExistance();
                if (startup)
                {
                    if (keyExists)
                    {
                        uDebugLogAdd($"Panacea windows startup registry keyExists is {keyExists}, skipping key add");
                    }
                    else
                    {
                        uDebugLogAdd($"Add to windows startup is {startup} and keyExists is {keyExists}, adding registry key");
                        key.SetValue("Panacea", $@"{Director.Main.CurrentDirectory}\Panacea.exe");
                        Toolbox.settings.WindowsStartup = true;
                        uDebugLogAdd("Successfully added to windows startup");
                    }
                    Director.Main.ShowNotification("Panacea set to launch on Windows startup");
                }
                else
                {
                    if (keyExists)
                    {
                        uDebugLogAdd($"Add to windows startup is {startup} and keyExists is {keyExists}, removing registry key");
                        key.DeleteValue("Panacea", false);
                        Toolbox.settings.WindowsStartup = false;
                        uDebugLogAdd("Successfully removed from windows startup");
                    }
                    else
                    {
                        uDebugLogAdd($"Panacea windows startup registry keyExists is {keyExists}, skipping key removal");
                    }
                    Director.Main.ShowNotification("Panacea set to NOT launch on Windows startup");
                }
            }
            catch (ArgumentNullException ane) { uDebugLogAdd($"Argument was null when writing regkey for startup: [{ane.ParamName}] {ane.Message}", DebugType.FAILURE); Director.Main.ShowNotification("Unable to add open on windows startup, an error occured"); }
            catch (ObjectDisposedException ode) { uDebugLogAdd($"Object was disposed when writing regkey for startup: [{ode.ObjectName}] {ode.Message}"); Director.Main.ShowNotification("Unable to add open on windows startup, an error occured"); }
            catch (System.Security.SecurityException se) { uDebugLogAdd($"Security Exception occured when writing regkey for startup: [{se.PermissionType}][{se.PermissionState}][{se.Method}] {se.Message}"); Director.Main.ShowNotification("Unable to add open on windows startup, an error occured"); }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static bool CheckWinStartupRegKeyExistance()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return key.GetValue("Panacea") == null ? false : true;
        }
    }
}
