﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Upstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Thinking of keeping these

        public MainWindow()
        {
            InitializeComponent();
        }
        public bool argUpdate { get; set; } = false;
        public string currentDir { get; set; }
        private Octokit.GitHubClient gitClient = null;
        private Version currentVer = null;
        private Version prodVer = null;
        private string baseURI = $@"https://github.com/rwobig93/Panacea/releases/download";
        private string productionURI = string.Empty;
        private FixMeStatus fmButtonStatus = FixMeStatus.Shrug;
        private int shruggieCounter = 0;
        private enum FixMeStatus
        {
            NotInstalled,
            NeedUpdate,
            Shrug
        }

        #endregion

        #region Lets do the thing!

        private void mainWin_Loaded(object sender, RoutedEventArgs e)
        {
            Startup();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseDisBisch();
        }

        private void btnFixMe_Click(object sender, RoutedEventArgs e)
        {
            FixMeAction();
        }

        private void btnChangeDirectory_Click(object sender, RoutedEventArgs e)
        {
            ChangeDirectoryAction();
        }

        #endregion

        #region I got this

        private void Startup()
        {
            InitializeVisualElements();
        }

        private void InitializeVisualElements()
        {
            UpdateDirectory();
            VerifyInstallation();
            tCheckForUpdates();
            SetIdleDefault();
        }

        private void SetIdleDefault()
        {
            try
            {
                txtLabelDynamic.Text = "Idle";
                txtLabelDynamic.Foreground = new SolidColorBrush(Colors.LawnGreen);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyInstallation()
        {
            try
            {
                var panacea = $@"{currentDir}\Panacea.exe";
                if (File.Exists(panacea))
                {
                    ToggleInstallationLabel(true);
                }
                else
                    ToggleInstallationLabel(false);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleInstallationLabel(bool v)
        {
            try
            {
                if (v)
                {
                    txtLabelInstalled.Foreground = new SolidColorBrush(Colors.LawnGreen);
                    txtLabelInstalled.Text = "Installed";
                    uStatusUpdate("Found installed Panacea executable");
                }
                else
                {
                    txtLabelInstalled.Foreground = new SolidColorBrush(Colors.Red);
                    txtLabelInstalled.Text = "Not Installed";
                    uStatusUpdate("Couldn't find Panacea executable, not installed");
                    ToggleFixMeButton(FixMeStatus.NotInstalled);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleFixMeButton(FixMeStatus status)
        {
            try
            {
                fmButtonStatus = status;
                switch (status)
                {
                    case FixMeStatus.NotInstalled:
                        btnFixMe.Content = "Install Panacea";
                        btnFixMe.IsEnabled = true;
                        break;
                    case FixMeStatus.NeedUpdate:
                        btnFixMe.Content = "Update Panacea";
                        btnFixMe.IsEnabled = true;
                        break;
                    case FixMeStatus.Shrug:
                        btnFixMe.Content = @"¯\_(ツ)_/¯";
                        btnFixMe.IsEnabled = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateDirectory()
        {
            try
            {
                currentDir = Directory.GetCurrentDirectory();
                txtDirectory.Text = currentDir;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            try
            {
                Toolbox.LogException(ex, lineNum, caller, path);
                Toolbox.uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
                uStatusUpdate("An Exception was logged");
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
                Toolbox.uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
                uStatusUpdate("An Exception was logged");
            }
        }

        private void uStatusUpdate(string update)
        {
            try
            {
                txtStatus.Text += $"{update}{Environment.NewLine}";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO)
        {
            try
            {
                Toolbox.uDebugLogAdd(_log, _type);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private Version GetVersionNumber(string assemblyPath)
        {
            return Assembly.LoadFile(assemblyPath).GetName().Version;
        }

        private void tCheckForUpdates()
        {
            try
            {
                uDebugLogAdd("Checking for updates...");
                InitializeGitClient();
                string executable = "Panacea.exe";
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        currentVer = GetVersionNumber($@"{currentDir}\{executable}");
                        uDebugLogAdd($"Current Version: {currentVer}");
                        Task t = GetUpdate(executable);
                        t.Start();
                        while (!t.IsCompleted)
                        {
                            Thread.Sleep(500);
                        }
                        worker.ReportProgress(1);
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    try
                    {
                        if (pe.ProgressPercentage == 1)
                        {
                            if (currentVer.CompareTo(prodVer) < 0)
                            {
                                uDebugLogAdd($"New version found: [c]{currentVer} [p]{prodVer}");
                                ToggleVersionStatus(true);
                            }
                            else
                            {
                                ToggleVersionStatus(false);
                                uDebugLogAdd($"Current version is the same or newer than release: [c]{currentVer} [p]{prodVer}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleVersionStatus(bool v)
        {
            try
            {
                if (txtLabelInstalled.Text != "Installed")
                {
                    uDebugLogAdd($"Installation status is: {txtLabelInstalled.Text}, skipping update toggle");
                    return;
                }
                if (v)
                {
                    uStatusUpdate("Update found and now available!");
                    txtLabelUpToDate.Text = "Update Available";
                    txtLabelUpToDate.Foreground = new SolidColorBrush(Colors.AliceBlue);
                    ToggleFixMeButton(FixMeStatus.NeedUpdate);
                }
                else
                {
                    uStatusUpdate("Panacea is at the most up to date version!");
                    txtLabelUpToDate.Text = "Up To Date";
                    txtLabelUpToDate.Foreground = new SolidColorBrush(Colors.LawnGreen);
                    ToggleFixMeButton(FixMeStatus.Shrug);
                }
                uDebugLogAdd($"Version status toggled to: {txtLabelUpToDate.Text}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public async Task GetUpdate(string exe)
        {
            try
            {
                var releases = await gitClient.Repository.Release.GetAll("rwobig93", "Panacea");
                var recentRelease = releases.ToList().FindAll(x => x.Prerelease == true).OrderBy(x => x.TagName).First();
                Version prodVersion = new Version(recentRelease.TagName);
                prodVer = prodVersion;
                uDebugLogAdd($"ProdVer: {prodVer}");
                productionURI = $@"{baseURI}/{recentRelease.TagName}/{exe}";
                uDebugLogAdd($"URI: {productionURI}");
                uDebugLogAdd($"Finished getting github recent version");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CloseDisBisch()
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void FixMeAction()
        {
            try
            {
                switch (fmButtonStatus)
                {
                    case FixMeStatus.NeedUpdate:
                        UpdatePanacea();
                        break;
                    case FixMeStatus.NotInstalled:
                        InstallPanacea();
                        break;
                    case FixMeStatus.Shrug:
                        ShruggieClick();
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShruggieClick()
        {
            try
            {
                uStatusUpdate(@"¯\_(ツ)_/¯");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void InstallPanacea()
        {
            try
            {
                uDebugLogAdd($@"Starting Panacea installation, prodURI: {productionURI}");
                VerifyInstallationReqs();
                uDebugLogAdd($@"Finished verification checks, continuing installation w/ prodURI: {productionURI}");
                uStatusUpdate("Starting Panacea Installation");
                PrepStagingArea();
                DownloadWhatWeCameHereFor();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void PrepStagingArea()
        {
            try
            {
                uDebugLogAdd("Starting staging area prep");
                var whereTheThingGoes = $@"{currentDir}";
                if (!currentDir.ToLower().EndsWith("panacea"))
                {
                    uDebugLogAdd($"Directory didn't have Panacea in the name, preposterous. I fixed the glitch, before: {whereTheThingGoes}");
                    whereTheThingGoes = $@"{currentDir}\Panacea";
                    uDebugLogAdd($"After: {whereTheThingGoes} | There, that's much better");
                }
                var stagingArea = $@"{whereTheThingGoes}\TopSecret";
                if (!Directory.Exists(whereTheThingGoes))
                {
                    uDebugLogAdd($"Panacea directory didn't exist, making love and creating a directory, I'll name it: {whereTheThingGoes}");
                    Directory.CreateDirectory(whereTheThingGoes);
                    uDebugLogAdd($"{whereTheThingGoes} is born! Man I feel bad for that kid");
                }
                else
                    uDebugLogAdd($"The holy place already exists, lets move on: {whereTheThingGoes}");
                if (!Directory.Exists(stagingArea))
                {
                    uDebugLogAdd($"The staging area doesn't exist, I need a place to stage stuff damnit! {stagingArea}");
                    Directory.CreateDirectory(stagingArea);
                    uDebugLogAdd($"There, now I can stage all the things! {stagingArea}");
                }
                else
                    uDebugLogAdd("The not so secret folder has been verified to exist and is not classified");
                CleanupStagingArea(stagingArea);
                uDebugLogAdd("Staging area prep is finished... finally!");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CleanupStagingArea(string stagingArea)
        {
            try
            {
                uDebugLogAdd($"Starting staging area cleanup: {stagingArea}");
                var crapCounter = 0;
                foreach (var file in Directory.EnumerateFiles(stagingArea))
                {
                    FileInfo fi = new FileInfo(file);
                    try
                    {
                        uDebugLogAdd($"Deleting old staging junk: {fi.Name}");
                        fi.Delete();
                        uDebugLogAdd($"Success! That crap is outta heyah!");
                        crapCounter++;
                    }
                    catch (Exception ex2)
                    {
                        uDebugLogAdd($"Error occured when trying to delete a file: {Environment.NewLine}{fi.FullName}{Environment.NewLine}Error: {ex2.Message}");
                    }
                }
                uDebugLogAdd($"Finished staging area cleanup, we trashed {crapCounter} thing(s)!");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DownloadWhatWeCameHereFor()
        {
            try
            {
                uDebugLogAdd($"Starting Panacea download, this is getting exciting!!! URI: {productionURI}");
                ToggleIdleLable("Downloading");
                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += (s, e) => { pbDownload.Value = e.ProgressPercentage; uDebugLogAdd($"Download progress updated: {e.ProgressPercentage}%"); };
                webClient.DownloadFileCompleted += (s2, e2) => { DownloadComplete(); };
                uDebugLogAdd("Starting that download thang");
                uStatusUpdate("Starting Panacea download...");
                webClient.DownloadFileAsync(new Uri(productionURI), $@"{currentDir}\Panacea\TopSecret\Panacea.exe");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleIdleLable(string v)
        {
            try
            {
                txtLabelDynamic.Text = v;
                txtLabelDynamic.Foreground = new SolidColorBrush(Colors.AliceBlue);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DownloadComplete()
        {
            uDebugLogAdd($"Successfully downloaded Panacea to: {$@"{currentDir}\Panacea\Panacea.exe"}");
            uStatusUpdate("Download finished!");
            SetIdleDefault();
        }

        private void UpdatePanacea()
        {
            try
            {
                VerifyInstallationReqs();
                BackupEverything();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyInstallationReqs()
        {
            try
            {
                InitializeGitClient();
                if (string.IsNullOrWhiteSpace(productionURI))
                {
                    tCheckForUpdates();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void InitializeGitClient()
        {
            try
            {
                if (gitClient == null)
                {
                    gitClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("Panacea"));
                    uDebugLogAdd("gitClient was null, initialized gitClient");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BackupEverything()
        {
            try
            {
                BackupData();
                BackupOldClient();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BackupOldClient()
        {
            
        }

        private void BackupData()
        {
            throw new NotImplementedException();
        }

        private void ChangeDirectoryAction()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
