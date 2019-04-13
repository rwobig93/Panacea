using Microsoft.Win32;
using Panacea.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Panacea.Classes
{
    public static class Actions
    {
        private static void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            Toolbox.LogException(ex, lineNum, caller, path);
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

        public static void ShowChangelog()
        {
            try
            {
                Changelog changelog = new Changelog();
                changelog.ShowDialog();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static void CopyToClipboard(string clip, string optionalMessage = null)
        {
            try
            {
                Clipboard.SetText(clip);
                if (optionalMessage == null)
                    Director.Main.ShowNotification($"Set Clipboard: {clip}");
                else
                    Director.Main.ShowNotification(optionalMessage);
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Error occured when setting clipboard: {clip} | {ex.Message}", DebugType.FAILURE);
                Director.Main.ShowNotification($"Error occured when setting clipboard: {clip}");
            }
        }

        public static void ResetConfigToDefault()
        {
            try
            {
                uDebugLogAdd("User clicked the Default Config button");
                var response = Prompt.YesNo("Are you sure you want to default all of this applications configuration?");
                uDebugLogAdd($"When prompted, the user hit: {response.ToString()}");
                if (response == Prompt.PromptResponse.Yes)
                {
                    uDebugLogAdd("Resetting config to default");
                    AddToWindowsStartup(false);
                    Toolbox.settings = new Settings();
                    Director.Main.SaveSettings();
                    Director.Main.UpdateAllSettingsUI();
                    Director.Main.ShowNotification("Config reset to Default");
                }
                else
                {
                    uDebugLogAdd("We ended up not resetting all of our config.... maybe next time", DebugType.FAILURE);
                    Director.Main.ShowNotification("An error occured when trying to default the config and didn't finish");
                    Director.Main.SaveSettings();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static void StartProcess(string path, string args = null)
        {
            try
            {
                Process proc = new Process() { StartInfo = new ProcessStartInfo() { FileName = $@"{path}" } };
                if (string.IsNullOrWhiteSpace(args))
                    proc.StartInfo.Arguments = $@"{args}";
                proc.Start();
                ProcessWatcher(proc);
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Unable to start process [path] {path} | [args] {args} | [msg] {ex.Message}", DebugType.FAILURE);
            }
        }

        private static void ProcessWatcher(Process proc)
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    bool movedWindow = false;
                    uDebugLogAdd($"Started process watcher for launched process [id] {proc.Id} [path] {proc.StartInfo.FileName}");
                    DateTime timeToStop = DateTime.Now.AddMinutes(5);
                    while (string.IsNullOrWhiteSpace(proc.ProcessName))
                    {
                        Thread.Sleep(1000);
                        if (DateTime.Now > timeToStop)
                        {
                            uDebugLogAdd($"Time is up, stopping proc watch [path] {proc.StartInfo.FileName}");
                            return;
                        }
                    }
                    while (!movedWindow && DateTime.Now < timeToStop)
                    {
                        var existingWindow = Toolbox.settings.ActiveWindowList.Find(x => x.WindowInfo.Name.ToLower() == proc.ProcessName.ToLower());
                        if (existingWindow != null)
                        {
                            uDebugLogAdd($"Found matching window item for process, initiating process window move [name] {proc.ProcessName} [path] {proc.StartInfo.FileName}");
                            movedWindow = MoveProcessHandle(existingWindow, proc);
                        }
                        Thread.Sleep(1000);
                    }
                    if (!movedWindow)
                    {
                        uDebugLogAdd($"Wasn't able to find a matching process window within time limit, canceling | [id] {proc.Id} [path] {proc.StartInfo.FileName}");
                    }
                    else
                    {
                        uDebugLogAdd($"Successfully moved proc window | [id] {proc.Id} [path] {proc.StartInfo.FileName}");
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        public static bool MoveProcessHandle(WindowItem selectedWindow, Process process = null)
        {
            bool moveAll = false;
            bool windowMoved = false;
            try
            {
                /// Title is a wildcard, lets move ALL THE WINDOWS!!!
                if (selectedWindow.WindowInfo.Title == "*")
                {
                    moveAll = true;
                    uDebugLogAdd($"WindowInfo title for {selectedWindow.WindowInfo.Name} is {selectedWindow.WindowInfo.Title} so we will be MOVING ALL THE WINDOWS!!!!");
                }
                /// Title isn't a wildcard, lets only move the windows we want
                else
                    uDebugLogAdd($"WindowInfo title for {selectedWindow.WindowInfo.Name} is {selectedWindow.WindowInfo.Title} so I can only move matching handles... :(");

                List<DetailedProcess> foundList = new List<DetailedProcess>();
                if (process == null)
                {
                    foreach (var proc in Process.GetProcessesByName(selectedWindow.WindowInfo.Name))
                    {
                        foreach (var handle in WinAPIWrapper.EnumerateProcessWindowHandles(proc.Id))
                        {
                            try
                            {
                                var detProc = DetailedProcess.Create(proc, handle);
                                foundList.Add(detProc);
                                uDebugLogAdd($"Added to list | [{detProc.Handle}]{detProc.Name} :: {detProc.Title}");
                            }
                            catch (Exception ex)
                            {
                                uDebugLogAdd($"Unable to add handle to the list | [{handle}]{proc.ProcessName}: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    foreach (var handle in WinAPIWrapper.EnumerateProcessWindowHandles(process.Id))
                    {
                        try
                        {
                            var detProc = DetailedProcess.Create(process, handle);
                            foundList.Add(detProc);
                            uDebugLogAdd($"Added to list | [{detProc.Handle}]{detProc.Name} :: {detProc.Title}");
                        }
                        catch (Exception ex)
                        {
                            uDebugLogAdd($"Unable to add handle to the list | [{handle}]{process.ProcessName}: {ex.Message}");
                        }
                    }
                }
                if (moveAll)
                    foreach (var detProc in foundList)
                    {
                        try
                        {
                            if (Toolbox.settings.ActiveWindowList.Find(x =>
                            x.WindowInfo.Name == detProc.Name &&
                            x.WindowInfo.Title == detProc.Title
                            ) == null)
                            {
                                uDebugLogAdd($"Moving handle | [{detProc.Handle}]{detProc.Name} :: {detProc.Title}");
                                WinAPIWrapper.MoveWindow(detProc.Handle, selectedWindow.WindowInfo.XValue, selectedWindow.WindowInfo.YValue, selectedWindow.WindowInfo.Width, selectedWindow.WindowInfo.Height, true);
                                windowMoved = true;
                            }
                            else
                            {
                                uDebugLogAdd($"Skipping handle window as it has another place to be | [{detProc.Handle}]{detProc.Name} {detProc.Title}");
                            }
                        }
                        catch (Exception ex)
                        {
                            uDebugLogAdd($"Unable to move handle window | [{detProc.Handle}]{detProc.Name} {detProc.Title}: {ex.Message}");
                        }
                    }
                else
                {
                    foreach (var detProc in foundList)
                    {
                        try
                        {
                            if (detProc.Name == selectedWindow.WindowInfo.Name &&
                                            detProc.Title == selectedWindow.WindowInfo.Title)
                            {
                                uDebugLogAdd($"Matched window & title, moving: [{detProc.Handle}]{detProc.Name} | {detProc.Title}");
                                WinAPIWrapper.MoveWindow(detProc.Handle, selectedWindow.WindowInfo.XValue, selectedWindow.WindowInfo.YValue, selectedWindow.WindowInfo.Width, selectedWindow.WindowInfo.Height, true);
                                windowMoved = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            uDebugLogAdd($"Unable to move handle window | [{detProc.Handle}]{detProc.Name} {detProc.Title}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            return windowMoved;
        }

        public static void GetWindowItemLocation(WindowItem windowItem)
        {
            try
            {
                Process foundProc = null;
                if (windowItem.WindowInfo.Title == "*")
                    foreach (var proc in Process.GetProcesses())
                    {
                        if (
                            proc.ProcessName == windowItem.WindowInfo.Name &&
                            proc.MainModule.ModuleName == windowItem.WindowInfo.ModName &&
                            proc.MainModule.FileName == windowItem.WindowInfo.FileName
                           )
                            foundProc = proc;
                    }
                else
                    foreach (var proc in Process.GetProcesses())
                    {
                        if (
                            proc.ProcessName == windowItem.WindowInfo.Name &&
                            proc.MainModule.ModuleName == windowItem.WindowInfo.ModName &&
                            proc.MainWindowTitle == windowItem.WindowInfo.Title &&
                            proc.MainModule.FileName == windowItem.WindowInfo.FileName
                           )
                            foundProc = proc;
                    }
                if (foundProc == null)
                    uDebugLogAdd("Running process matching windowItem wasn't found");
                else
                {
                    Toolbox.settings.UpdateWindowLocation(windowItem, foundProc);
                    uDebugLogAdd($"Updated {windowItem.WindowName} windowItem");
                    Director.Main.uStatusUpdate($"Updated location for {foundProc.ProcessName}");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static void TriggerProcessProfileStart(StartProfile profile)
        {
            try
            {
                uDebugLogAdd("Starting Start profile trigger");
                List<StartProcessItem> chosenStartList = null;
                string startProfileName = null;
                switch (profile)
                {
                    case StartProfile.Start1:
                        chosenStartList = Toolbox.settings.StartProfile1;
                        startProfileName = Toolbox.settings.StartProfileName1;
                        break;
                    case StartProfile.Start2:
                        chosenStartList = Toolbox.settings.StartProfile2;
                        startProfileName = Toolbox.settings.StartProfileName2;
                        break;
                    case StartProfile.Start3:
                        chosenStartList = Toolbox.settings.StartProfile3;
                        startProfileName = Toolbox.settings.StartProfileName3;
                        break;
                    case StartProfile.Start4:
                        chosenStartList = Toolbox.settings.StartProfile4;
                        startProfileName = Toolbox.settings.StartProfileName4;
                        break;
                }
                uDebugLogAdd($"Start process count: {chosenStartList.Count} for {profile.ToString()}");
                foreach (var item in chosenStartList)
                {
                    StartProcess(item.Path, item.Args);
                }
                uDebugLogAdd("Finished start profile trigger");
                Director.Main.ShowNotification($"Started {chosenStartList.Count} Processes ({startProfileName})");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static void MoveActiveProfileWindows()
        {
            try
            {
                uDebugLogAdd("Starting All Window Move");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                var workerCount = 0;
                foreach (var window in Toolbox.settings.ActiveWindowList.ToList())
                {
                    workerCount++;
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (sender, e) =>
                    {
                        try
                        {
                            MoveProcessHandle(window);
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                        finally
                        {
                            workerCount--;
                        }
                    };
                    worker.RunWorkerAsync();
                }
                BackgroundWorker verifyier = new BackgroundWorker() { WorkerReportsProgress = true };
                verifyier.DoWork += (ws, we) =>
                {
                    while (workerCount != 0)
                    {
                        Thread.Sleep(100);
                    }
                    stopwatch.Stop();
                    uDebugLogAdd($"Finished All Window Move after {stopwatch.Elapsed.Seconds}s {stopwatch.Elapsed.Milliseconds}ms");
                    Director.Main.ShowNotification("Moved all windows");
                };
                verifyier.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static void MoveProfileWindows(WindowProfile profile)
        {
            try
            {
                uDebugLogAdd("Starting All Window Move");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                var workerCount = 0;
                List<WindowItem> chosenWindowList = null;
                string chosenProfileName = string.Empty;
                switch (profile)
                {
                    case WindowProfile.Profile1:
                        chosenWindowList = Toolbox.settings.WindowProfile1;
                        chosenProfileName = Toolbox.settings.WindowProfileName1;
                        break;
                    case WindowProfile.Profile2:
                        chosenWindowList = Toolbox.settings.WindowProfile2;
                        chosenProfileName = Toolbox.settings.WindowProfileName2;
                        break;
                    case WindowProfile.Profile3:
                        chosenWindowList = Toolbox.settings.WindowProfile3;
                        chosenProfileName = Toolbox.settings.WindowProfileName3;
                        break;
                    case WindowProfile.Profile4:
                        chosenWindowList = Toolbox.settings.WindowProfile4;
                        chosenProfileName = Toolbox.settings.WindowProfileName4;
                        break;
                    default:
                        chosenWindowList = Toolbox.settings.WindowProfile1;
                        chosenProfileName = Toolbox.settings.WindowProfileName1;
                        break;
                }
                foreach (var window in chosenWindowList.ToList())
                {
                    workerCount++;
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (sender, e) =>
                    {
                        try
                        {
                            MoveProcessHandle(window);
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                        finally
                        {
                            workerCount--;
                        }
                    };
                    worker.RunWorkerAsync();
                }
                BackgroundWorker verifyier = new BackgroundWorker() { WorkerReportsProgress = true };
                verifyier.DoWork += (ws, we) =>
                {
                    while (workerCount != 0)
                    {
                        Thread.Sleep(100);
                    }
                    stopwatch.Stop();
                    uDebugLogAdd($"Finished All Window Move after {stopwatch.Elapsed.Seconds}s {stopwatch.Elapsed.Milliseconds}ms");
                    Director.Main.ShowNotification($"Moved all {chosenProfileName} windows");
                };
                verifyier.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
    }
}
