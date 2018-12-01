using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

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
        public bool argBeta { get; set; } = false;
        public bool verifyInProgress = false;
        public bool downloadInProgress = false;
        public bool updateInProgress = false;
        public bool relaunchUpstaller = false;
        public string currentDir { get; set; }
        private string logDir = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Logs\";
        private string confDir = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Config\";
        private string exDir = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Logs\Exceptions\";
        private Octokit.GitHubClient gitClient = null;
        private Version currentVer = null;
        private Version prodVer = null;
        private string baseURI = $@"https://github.com/rwobig93/Panacea/releases/download";
        private string productionURI = string.Empty;
        private FixMeStatus fmButtonStatus = FixMeStatus.Shrug;
        private InstallStatus installStatus = InstallStatus.NotInstalled;
        private int shruggieCounter = 0;
        private enum FixMeStatus
        {
            NotInstalled,
            NeedUpdate,
            Shrug
        }
        private enum VersionStatus
        {
            UpToDate,
            UpdateAvailable,
            Verifying,
            NotFound
        }
        private enum InstallStatus
        {
            Installed,
            NotInstalled
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

        private void btnStartPanacea_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartPanacea();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void lblTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string verNum = GetVersionNumber().ToString();
                System.Windows.Clipboard.SetText(verNum);
                uStatusUpdate($"Copied version to clipboard: {verNum}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                uStatusUpdate("I'm a test string!");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region I got this

        private void Startup()
        {
#if DEBUG
            btnTest.Visibility = Visibility.Visible;
#endif
            VerifyDirectories();
            uDebugLogAdd(string.Format("{0}###################################### Application Start ######################################{0}", Environment.NewLine));
            InitializeVisualElements();
            if (installStatus == InstallStatus.Installed)
            {
                uDebugLogAdd($"Install status is: {installStatus} | Starting update check");
                ToggleVersionStatus(VersionStatus.Verifying);
                tCheckForUpdates();
            }
            else
            {
                uDebugLogAdd($"Install status is: {installStatus} | Skipping update check");
                ToggleVersionStatus(VersionStatus.NotFound);
            }
            VerifyArgs();
        }

        private void VerifyDirectories()
        {
            try
            {
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                    uDebugLogAdd("Created Missing Logs Directory");
                }
                else { uDebugLogAdd("Logs Directory Already Exists"); }
                uDebugLogAdd(string.Format("Log Directory: {0}", logDir));
                if (!Directory.Exists(exDir))
                {
                    Directory.CreateDirectory(exDir);
                    uDebugLogAdd("Created missing Exception Directory");
                }
                else { uDebugLogAdd("Exception Directory Already Exists"); }
                uDebugLogAdd(string.Format("Exception Directory: {0}", exDir));
                if (!Directory.Exists(confDir))
                {
                    Directory.CreateDirectory(confDir);
                    uDebugLogAdd("Created missing config directory");
                }
                else { uDebugLogAdd($"Config Directory: {confDir}"); }
                FileInfo fi = new FileInfo($@"{Directory.GetCurrentDirectory()}\Relaunch.bat");
                FileInfo fi2 = new FileInfo($@"{Directory.GetCurrentDirectory()}\Relaunch123456789.bat");
                if (fi.Exists)
                {
                    fi.Delete();
                    uDebugLogAdd("Relauncher batch file found and annihilated");
                }
                if (fi2.Exists)
                {
                    fi2.Delete();
                    uDebugLogAdd("Relauncher extended batch file found and annihilated");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyArgs()
        {
            try
            {
                if (argUpdate && txtLabelInstalled.Text == "Installed")
                {
                    uDebugLogAdd($"Update arg is set to: {argUpdate}, updating Panacea");
                    UpdatePanacea();
                }
                else
                    uDebugLogAdd($"Arg is: {argUpdate} & Install status is: {txtLabelInstalled.Text} | Skipping auto-update");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void InitializeVisualElements()
        {
            UpdateDirectory();
            VerifyInstallation();
            ToggleVersionStatus(VersionStatus.Verifying);
            ToggleDynamicLabel("Idle", Colors.Cyan);
        }

        private void PanaceaStartDisable()
        {
            try
            {
                if (txtLabelInstalled.Text != "Installed")
                {
                    btnStartPanacea.IsEnabled = false;
                    uDebugLogAdd("Disabled Panacea Start Button");
                }
                else
                {
                    btnStartPanacea.IsEnabled = true;
                    uDebugLogAdd("Enabled Panacea Start Button");
                }
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
                    ToggleInstallationLabel(InstallStatus.Installed);
                }
                else
                    ToggleInstallationLabel(InstallStatus.NotInstalled);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleInstallationLabel(InstallStatus install)
        {
            try
            {
                installStatus = install;
                switch (install)
                {
                    case InstallStatus.Installed:
                        txtLabelInstalled.Foreground = new SolidColorBrush(Colors.LawnGreen);
                        txtLabelInstalled.Text = "Installed";
                        uStatusUpdate("Found installed Panacea executable");
                        PanaceaReadyToStart();
                        break;
                    case InstallStatus.NotInstalled:
                        txtLabelInstalled.Foreground = new SolidColorBrush(Colors.Red);
                        txtLabelInstalled.Text = "Not Installed";
                        uStatusUpdate("Couldn't find Panacea executable, not installed");
                        ToggleFixMeButton(FixMeStatus.NotInstalled);
                        PanaceaStartDisable();
                        break;
                    default:
                        uDebugLogAdd("Unknown state triggered for install status", DebugType.FAILURE);
                        uStatusUpdate("Unknown state triggered for install status, please let the developer know");
                        break;
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
                        btnFixMe.Foreground = new SolidColorBrush(Colors.Red);
                        btnFixMe.Content = "Install Panacea";
                        btnFixMe.IsEnabled = true;
                        break;
                    case FixMeStatus.NeedUpdate:
                        btnFixMe.Foreground = new SolidColorBrush(Colors.LawnGreen);
                        btnFixMe.Content = "Update Panacea";
                        btnFixMe.IsEnabled = true;
                        break;
                    case FixMeStatus.Shrug:
                        btnFixMe.Foreground = new SolidColorBrush(Colors.Cyan);
                        btnFixMe.Content = @"¯\_(ツ)_/¯";
                        btnFixMe.IsEnabled = true;
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
                currentDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { txtStatus.AppendText($"{Environment.NewLine}{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {update}"); } catch (Exception ex) { LogException(ex); } });
                uDebugLogAdd(update, DebugType.STATUS);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            try
            {
                Toolbox.uDebugLogAdd(_log, _type, caller);
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
                verifyInProgress = true;
                uDebugLogAdd("Checking for updates...");
                InitializeGitClient();
                string executable = "Panacea.exe";
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        var assemblyLoc = $@"{currentDir}\{executable}";
                        uDebugLogAdd($"Assembly location: {assemblyLoc}");
                        currentVer = GetVersionNumber(assemblyLoc);
                        uDebugLogAdd($"Current Version: {currentVer}");
                        Task t = Task.Run(async () => { await GetUpdate(executable); });
                        uDebugLogAdd("Starting verification wait for GetUpdate task to finish");
                        while (!t.IsCompleted)
                        {
                            uDebugLogAdd("GetUpdate task isn't finished, sleeping...");
                            Thread.Sleep(500);
                        }
                        uDebugLogAdd("GetUpdate task finished, Comparing version");
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
                                ToggleVersionStatus(VersionStatus.UpdateAvailable);
                            }
                            else
                            {
                                ToggleVersionStatus(VersionStatus.UpToDate);
                                uDebugLogAdd($"Current version is the same or newer than release: [c]{currentVer} [p]{prodVer}");
                            }
                        }
                        verifyInProgress = false;
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

        private void ToggleVersionStatus(VersionStatus version)
        {
            try
            {
                switch (version)
                {
                    case VersionStatus.NotFound:
                        uStatusUpdate("Please install Panacea");
                        txtLabelUpToDate.Text = "Not Found";
                        txtLabelUpToDate.Foreground = new SolidColorBrush(Colors.Red);
                        ToggleFixMeButton(FixMeStatus.NotInstalled);
                        break;
                    case VersionStatus.UpdateAvailable:
                        uStatusUpdate("Update found and now available!");
                        txtLabelUpToDate.Text = "Update Available";
                        txtLabelUpToDate.Foreground = new SolidColorBrush(Colors.Cyan);
                        ToggleFixMeButton(FixMeStatus.NeedUpdate);
                        break;
                    case VersionStatus.UpToDate:
                        uStatusUpdate("Panacea is at the most up to date version!");
                        txtLabelUpToDate.Text = "Up To Date";
                        txtLabelUpToDate.Foreground = new SolidColorBrush(Colors.LawnGreen);
                        ToggleFixMeButton(FixMeStatus.Shrug);
                        break;
                    case VersionStatus.Verifying:
                        txtLabelUpToDate.Text = "Verifying";
                        txtLabelUpToDate.Foreground = new SolidColorBrush(Colors.Yellow);
                        break;
                    default:
                        txtLabelUpToDate.Text = "?????";
                        txtLabelUpToDate.Foreground = new SolidColorBrush(Colors.OrangeRed);
                        uDebugLogAdd("Unknown state triggered for install status", DebugType.FAILURE);
                        uStatusUpdate("Unknown state triggered for version status, please let the developer know");
                        break;
                }
                uDebugLogAdd($"Version status toggled to: {version.ToString()}");
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
                uDebugLogAdd("Starting GetUpdate task");
                var releases = await gitClient.Repository.Release.GetAll("rwobig93", "Panacea");
                Octokit.Release recentRelease = null;
                if (argBeta)
                    recentRelease = releases.ToList().FindAll(x => x.Prerelease == true && x.Assets[0].Name.Contains("Panacea")).OrderBy(x => x.TagName).Last();
                else
                    recentRelease = releases.ToList().FindAll(x => x.Prerelease == false && x.Assets[0].Name.Contains("Panacea")).OrderBy(x => x.TagName).Last();
                Version prodVersion = new Version(recentRelease.TagName);
                prodVer = prodVersion;
                uDebugLogAdd($"ProdVer: {prodVer}");
                productionURI = $@"{baseURI}/{recentRelease.TagName}/{exe}";
                uDebugLogAdd($"URI: {productionURI}");
                uDebugLogAdd($"Finished getting github recent version");
            }
            catch (InvalidOperationException ioe)
            {
                uDebugLogAdd($"Unable to get updates, reason: {ioe.Message}");
                uStatusUpdate("Unable to check for updates, couldn't find any versions in the repository");
                return;
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
                // this.Close();
                uDebugLogAdd(string.Format("{0}##################################### Application Closing #####################################{0}", Environment.NewLine));
                Toolbox.DumpDebugLog();
                var thisProc = Process.GetCurrentProcess();
                thisProc.Kill();
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
                shruggieCounter++;
                uDebugLogAdd($"Shruggie Counter: {shruggieCounter}");
                if (shruggieCounter < 10)
                    uStatusUpdate(@"¯\_(ツ)_/¯");
                else if (shruggieCounter >= 10 && shruggieCounter < 20)
                    uStatusUpdate(@"¯\_(ツ)_/¯ | ¯\_(ツ)_/¯");
                else
                    uStatusUpdate(@"¯\_(ツ)_/¯ | ¯\_(ツ)_/¯ | ¯\_(ツ)_/¯");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void InstallPanacea()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (ws, we) =>
            {
                VerifyInstallationReqs();
                Task t = Task.Run(async () => { await GetUpdate("Panacea.exe"); });
                uDebugLogAdd("Starting thread sleeper for info update");
                while (!t.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                uDebugLogAdd("Update info has been gathered, let's kick this off!");
                worker.ReportProgress(1);
            };
            worker.ProgressChanged += (ps, pe) =>
            {
                if (pe.ProgressPercentage == 1)
                {
                    try
                    {
                        uDebugLogAdd($@"Starting Panacea installation, prodURI: {productionURI}");
                        uStatusUpdate("Starting Panacea Installation");
                        PrepStagingArea();
                        DownloadWhatWeCameHereFor();
                        PutPanaceaInItsPlace();
                        VerifyInstallation();
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                }
            };
            worker.RunWorkerAsync();
        }

        private void RelaunchUpstaller()
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (sender, e) =>
                {
                    try
                    {
                        uDebugLogAdd("Waiting until we are ready to re-launch...");
                        while (updateInProgress)
                        {
                            Thread.Sleep(100);
                        }
                        uDebugLogAdd($"Relaunch bool is: {relaunchUpstaller} | Re-launching Upstaller");
                        string reLauncherBat = $@"{Directory.GetCurrentDirectory()}\Panacea\Relaunch.bat";
                        string launchString = $"taskkill /im Upstaller.exe{Environment.NewLine}START {Directory.GetCurrentDirectory()}\\Panacea\\Upstaller.exe";
                        if (!File.Exists(reLauncherBat))
                        {
                            using (StreamWriter sw = File.AppendText(reLauncherBat))
                                sw.WriteLine(launchString);
                            uDebugLogAdd("Created relauncher batch file");
                        }
                        else
                        {
                            reLauncherBat = $@"{Directory.GetCurrentDirectory()}\Panacea\Relaunch123456789.bat";
                            using (StreamWriter sw = File.AppendText(reLauncherBat))
                                sw.WriteLine(launchString);
                            uDebugLogAdd("Created relauncher extended batch file");
                        }
                        uDebugLogAdd("Dumping debug log and launching relauncher batch file");
                        Toolbox.DumpDebugLog();
                        Process proc = new Process() { StartInfo = new ProcessStartInfo() { FileName = "cmd.exe", Arguments = "/c" + $"\"{reLauncherBat}\"" } };
                        proc.Start();
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

        private void PutPanaceaInItsPlace()
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    uDebugLogAdd("Starting sleep thread to wait for the SLOOOOOW download to finish");
                    while (downloadInProgress)
                    {
                        Thread.Sleep(100);
                    }
                    uDebugLogAdd("Download FINALLY finished, jeez we need better throughput!");
                    worker.ReportProgress(1);
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    if (pe.ProgressPercentage == 1)
                    {
                        FileInfo fi = new FileInfo($@"{currentDir}\TopSecret\Panacea.exe");
                        uDebugLogAdd($@"Moving Panacea.exe to it's place | Before: {fi.FullName}");
                        fi.MoveTo($@"{currentDir}\Panacea.exe");
                        uDebugLogAdd($@"Moved Panacea.exe to it's place | After: {fi.FullName}");
                        PanaceaReadyToStart();
                        uDebugLogAdd("Finished Panacea update process.");
                        var curDur = Directory.GetCurrentDirectory();
                        uDebugLogAdd($"CurrentDir: {curDur}");
                        var fullPath = Assembly.GetExecutingAssembly().Location;
                        var pathDir = System.IO.Path.GetDirectoryName(fullPath);
                        uDebugLogAdd($"CurrentAssemblyDir: {pathDir}");
                        updateInProgress = false;
                        if (relaunchUpstaller)
                            RelaunchUpstaller();
                    }
                };
                worker.RunWorkerAsync();
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
                    if (!Directory.Exists($@"{currentDir}\Panacea"))
                        Directory.CreateDirectory($@"{currentDir}\Panacea");
                    currentDir = $@"{currentDir}\Panacea";
                    whereTheThingGoes = $@"{currentDir}";
                    uDebugLogAdd($"After: {whereTheThingGoes} | There, that's much better");
                    MoveUpstallerToItsNewHome(currentDir);
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

        private void MoveUpstallerToItsNewHome(string currentDir)
        {
            try
            {
                uDebugLogAdd($"Moving Upstaller.exe from {Directory.GetCurrentDirectory()} to it's new home: {currentDir}");
                FileInfo fi = new FileInfo($@"{Directory.GetCurrentDirectory()}\Upstaller.exe");
                fi.MoveTo($@"{currentDir}\Upstaller.exe");
                DirectoryInfo diLog = new DirectoryInfo($@"{Directory.GetCurrentDirectory()}\Logs");
                DirectoryInfo diConf = new DirectoryInfo($@"{Directory.GetCurrentDirectory()}\Config");
                if (diLog.Exists)
                {
                    diLog.MoveTo($@"{currentDir}\Logs");
                    uDebugLogAdd("Moved Logs directory");
                }
                else
                    uDebugLogAdd("Previous logs directory didn't exist");
                if (diConf.Exists)
                {
                    diConf.MoveTo($@"{currentDir}\Config");
                    uDebugLogAdd("Moved Config directory");
                }
                else
                    uDebugLogAdd("Previuos config directory didn't exist");
                relaunchUpstaller = true;
                uDebugLogAdd("Successfully moved Upstaller.exe to it's new home, where it'll be happy");
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
                downloadInProgress = true;
                uDebugLogAdd($"Starting Panacea download, this is getting exciting!!! URI: {productionURI}");
                ToggleDynamicLabel("Downloading", Colors.Yellow);
                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += (s, e) => { pbDownload.Value = e.ProgressPercentage; uDebugLogAdd($"Download progress updated: {e.ProgressPercentage}%"); };
                webClient.DownloadFileCompleted += (s2, e2) => { DownloadComplete(); };
                uDebugLogAdd("Starting that download thang");
                uStatusUpdate("Starting Panacea download...");
                webClient.DownloadFileAsync(new Uri(productionURI), $@"{currentDir}\TopSecret\Panacea.exe");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleDynamicLabel(string v, Color color)
        {
            try
            {
                txtLabelDynamic.Text = v;
                txtLabelDynamic.Foreground = new SolidColorBrush(color);
                uDebugLogAdd($"Dynamic label changed: [s]{v} [c]{color.ToString()}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DownloadComplete()
        {
            try
            {
                uDebugLogAdd($"Successfully downloaded Panacea to: {$@"{currentDir}\TopSecret\Panacea.exe"}");
                uStatusUpdate("Download finished!");
                ToggleDynamicLabel("Idle", Colors.LawnGreen);
                downloadInProgress = false;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdatePanacea()
        {
            try
            {
                updateInProgress = true;
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        uDebugLogAdd("Starting pre-update verification sleep");
                        while (verifyInProgress)
                        {
                            Thread.Sleep(100);
                        }
                        uDebugLogAdd("Verify no longer in progress, finished sleeping");
                        worker.ReportProgress(1);
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    if (pe.ProgressPercentage == 1)
                    {
                        uDebugLogAdd("Starting Panacea update process.");
                        ClosePanaceaInstances();
                        BackupEverything();
                        PrepStagingArea();
                        DownloadWhatWeCameHereFor();
                        PutPanaceaInItsPlace();
                        VerifyInstallation();
                        FinishUpdate();
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void FinishUpdate()
        {
            try
            {
                if (!argUpdate)
                {
                    uDebugLogAdd($"Argupdate is: {argUpdate} | Skipping update finish method");
                    return;
                }
                uDebugLogAdd("Starting update arg finish");
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    while (updateInProgress)
                    {
                        Thread.Sleep(100);
                    }
                    uDebugLogAdd("Finished waiting for update method to finish");
                    worker.ReportProgress(1);
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    if (pe.ProgressPercentage == 1)
                    {
                        StartPanacea();
                        CloseDisBisch();
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void StartPanacea()
        {
            try
            {
                var panacea = $@"{currentDir}\Panacea.exe";
                if (File.Exists(panacea))
                {
                    uDebugLogAdd($"Found Panacea: {panacea}");
                    var proc = Process.Start(panacea);
                    uDebugLogAdd($"Started Panacea process: {proc.ProcessName} | {proc.Id}");
                }
                else
                    uDebugLogAdd($@"Couldn't find Panacea at {panacea}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void PanaceaReadyToStart()
        {
            try
            {
                btnStartPanacea.IsEnabled = true;
                uDebugLogAdd("Panacea start button enabled");
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
                uDebugLogAdd("Starting backup of everything");
                BackupData();
                BackupOldClient();
                uDebugLogAdd("Finished backing up everything");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BackupData()
        {
            try
            {
                var dataDir = $@"{currentDir}\Config";
                var backupDir = $@"{currentDir}\Backup";
                var backupDataDir = $@"{currentDir}\Backup\Config";
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                    uDebugLogAdd("Backup dir didn't exist, created");
                }
                if (!Directory.Exists(backupDataDir))
                {
                    Directory.CreateDirectory(backupDataDir);
                    uDebugLogAdd("Backup data dir didn't exist, created");
                }
                if (Directory.Exists(dataDir))
                {
                    uDebugLogAdd($@"Found existing data stored at {dataDir}");
                    CleanupOldBackupData();
                    DirectoryInfo di = new DirectoryInfo(dataDir);
                    foreach (var fi in di.EnumerateFiles())
                    {
                        uDebugLogAdd($"Copying file {fi.Name}");
                        fi.CopyTo($@"{backupDataDir}\{fi.Name}", true);
                        uDebugLogAdd($@"Copied data file to backup: {backupDataDir}\{fi.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BackupOldClient()
        {
            try
            {
                uDebugLogAdd("Starting old client backup");
                VerifyBackupDirectory();
                CleanupBackupClient();
                FileInfo fi = new FileInfo($@"{currentDir}\Panacea.exe");
                uDebugLogAdd($@"Moving old client to it's new home, Before: {fi.FullName}");
                fi.MoveTo($@"{currentDir}\Backup\Panacea.exe");
                uDebugLogAdd($@"Finished old client backup, After: {fi.FullName}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyBackupDirectory()
        {
            try
            {
                var backupDir = $@"{currentDir}\Backup";
                if (!Directory.Exists(backupDir))
                {
                    uDebugLogAdd($@"Backup dir didn't exist at {backupDir}, creating dir");
                    Directory.CreateDirectory(backupDir);
                    if (!Directory.Exists(backupDir))
                    {
                        uDebugLogAdd($@"I attempted creating the backup dir at {backupDir} but it never showed up to the party");
                    }
                    else
                        uDebugLogAdd($"Backup dir created at {backupDir}");
                }
                else
                    uDebugLogAdd($@"Backup dir exists at {backupDir}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CleanupBackupClient()
        {
            try
            {
                if (File.Exists($@"{currentDir}\Backup\Panacea.exe"))
                {
                    uDebugLogAdd("Existing old client found, removing that sucka!");
                    FileInfo fi = new FileInfo($@"{currentDir}\Backup\Panacea.exe");
                    fi.Delete();
                    uDebugLogAdd("Deleted old backed up client, it's gone, like for real");
                }
                else
                    uDebugLogAdd("Old backup client not found, skipping...");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CleanupOldBackupData()
        {
            try
            {
                var backupDataDir = $@"{currentDir}\Backup\Config";
                DirectoryInfo di = new DirectoryInfo(backupDataDir);
                foreach (var fi in di.EnumerateFiles())
                {
                    uDebugLogAdd($"Removing old backed up file: {fi.FullName}");
                    fi.Delete();
                    uDebugLogAdd("Deleted file");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ChangeDirectoryAction()
        {
            try
            {
                string chosenDir = currentDir;
                if (!AskPermission($"This will close any open instances of Panacea,{Environment.NewLine}do you want to continue?"))
                    return;
                ClosePanaceaInstances();
                PromptForDirChoosing(out chosenDir);
                MoveOurJunkToNewDir(chosenDir);
                VerifyInstallation();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ClosePanaceaInstances()
        {
            try
            {
                uDebugLogAdd("Starting Panacea process slaughter");
                foreach (var proc in Process.GetProcessesByName("Panacea"))
                {
                    uDebugLogAdd($"Killing process [{proc.Id}]{proc.ProcessName}");
                    proc.Kill();
                    uDebugLogAdd("Killed process");
                }
                uDebugLogAdd("Finished Panacea process slaughter");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private bool AskPermission(string v)
        {
            bool answer = false;
            try
            {
                uDebugLogAdd($"Asking user question: {v}");
                if (Prompt.YesNo(v) == Prompt.PromptResponse.Yes)
                {
                    answer = true;
                    uDebugLogAdd($"User answered yes to: {v}");
                }
                else
                    uDebugLogAdd($"User answered no to: {v}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            return answer;
        }

        private void MoveOurJunkToNewDir(string newDir)
        {
            try
            {
                MoveTheThingsToTheirNewHome(newDir);
                SetupPlanAfterIClose();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SetupPlanAfterIClose()
        {
            throw new NotImplementedException();
        }

        private void MoveTheThingsToTheirNewHome(string newDir)
        {
            throw new NotImplementedException();
        }

        private void PromptForDirChoosing(out string newDir)
        {
            newDir = string.Empty;
            try
            {
                uDebugLogAdd("Prompting user for folder directory");
                using (var dialog = new FolderBrowserDialog())
                {
                    var result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        uDebugLogAdd("Dialog result was ok");
                        newDir = dialog.SelectedPath;
                        uDebugLogAdd($"Path chosen: {newDir}");
                    }
                }
                uDebugLogAdd("Finished prompting user for folder directory");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private Version GetVersionNumber()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        #endregion
    }
}
