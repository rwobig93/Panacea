using Newtonsoft.Json;
using Octokit;
using Panacea.Classes;
using Panacea.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for Director.xaml
    /// </summary>
    public partial class Director : Window
    {
        public Director()
        {
            InitializeComponent();
            Startup();
            //InitializeMasterThread();
        }

        private void InitializeMasterThread()
        {
            try
            {
                _mainWorker = new BackgroundWorker();
                _mainWorker.ProgressChanged += MasterThreadHandoff;
                _mainWorker.DoWork += MasterThreadWork;
                _mainWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MasterThreadWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (!_stopApplication)
                {

                }
                _mainWorker.ReportProgress(99);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MasterThreadHandoff(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                switch (e.ProgressPercentage)
                {
                    case 99:
                        FullApplicationClose();
                        break;
                    default:
                        uDebugLogAdd($"Master Thread Progress: {e.ProgressPercentage} | {e.UserState}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private BackgroundWorker _mainWorker;
        private bool _stopApplication = false;
        private bool upstallerUpdateInProg = false;
        private bool downloadInProgress = false;
        private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon taskIcon = null;
        private Octokit.GitHubClient gitClient = null;
        public static Director Main;
        public MainWindow DesktopWindow;
        public UtilityBar UtilBar;
        public string LogDirectory { get; } = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Logs\";
        public string ConfigDirectory { get; } = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Config\";
        public string ExceptionDirectory { get; } = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Logs\Exceptions\";
        public string CurrentDirectory { get; } = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private void Startup()
        {
            uDebugLogAdd(string.Format("{0}###################################### Application Start ######################################{0}", Environment.NewLine));
            Main = this;
            SetupAppFiles();
            DeSerializeSettings();
            SubscribeToEvents();
            InitializeBackgroundNotificationIcon();
            InitializeDesktopWindow();
            InitializeUtilBar();
            ShowPreferredWindow();
        }

        private void ShowPreferredWindow()
        {
            try
            {
                UtilBar.Show();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SubscribeToEvents()
        {
            Events.UpdateDebugStatus += Events_UpdateDebugStatus;
        }

        private void Events_UpdateDebugStatus(DebugUpdateArgs args)
        {
            try
            {
                if (Toolbox.debugLog.Length > 10000)
                {
                    Toolbox.debugLog.Append($"{args.DebugType.ToString()} :: {DateTime.Now.ToLocalTime().ToString("MM-dd-yy")}_{DateTime.Now.ToLocalTime().ToLongTimeString()}: Dumping Debug Logs...");
                    DumpDebugLog();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void InitializeUtilBar()
        {
            try
            {
                uDebugLogAdd("Initializing UtilityBar");
                UtilBar = new UtilityBar();
                uDebugLogAdd("Finished Initializing UtilityBar");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void InitializeDesktopWindow()
        {
            try
            {
                uDebugLogAdd("Initializing Desktop Window");
                DesktopWindow = new MainWindow();
                uDebugLogAdd("Finished Initializing Desktop Window");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleApplicationStop()
        {
            try
            {
                uDebugLogAdd("Application stop toggled");
                _stopApplication = true;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void FullApplicationClose()
        {
            TearDownBackgroundNotificationIcon();
            DesktopWindow.Close();
            UtilBar.Close();
            SaveSettings();
            uDebugLogAdd(string.Format("{0}##################################### Application Closing #####################################{0}", Environment.NewLine));
            DumpDebugLog();
            this.Close();
        }

        public void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            try
            {
                Toolbox.uAddDebugLog(_log, _type, caller);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            try
            {
                Toolbox.LogException(ex, lineNum, caller, path);
                uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
                uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
            }
        }

        public void DumpDebugLog()
        {
            int retries = 3;
            int delay = 1000;
            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    using (StreamWriter sw = File.AppendText($@"{LogDirectory}DebugLog_{DateTime.Now.ToString("MM-dd-yy")}.log"))
                        sw.WriteLine(Toolbox.debugLog.ToString());
                    Toolbox.debugLog.Clear();
                    break;
                }
                catch (IOException io)
                {
                    if (i <= retries)
                        Thread.Sleep(delay);
                    else
                    {
                        Toolbox.uAddDebugLog($"Dumping debug w/ retries resulted in ioexception: {io.Message}");
                        using (StreamWriter sw = File.AppendText($@"{LogDirectory}DebugLog_{DateTime.Now.ToString("MM-dd-yy")}_{Toolbox.GenerateRandomNumber()}.log"))
                            sw.WriteLine(Toolbox.debugLog.ToString());
                        Toolbox.debugLog.Clear();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }
        }

        private void SetupAppFiles()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                    uDebugLogAdd("Created Missing Logs Directory");
                }
                else { uDebugLogAdd("Logs Directory Already Exists"); }
                uDebugLogAdd(string.Format("Log Directory: {0}", LogDirectory));
                if (!Directory.Exists(ExceptionDirectory))
                {
                    Directory.CreateDirectory(ExceptionDirectory);
                    uDebugLogAdd("Created missing Exception Directory");
                }
                else { uDebugLogAdd("Exception Directory Already Exists"); }
                uDebugLogAdd(string.Format("Exception Directory: {0}", ExceptionDirectory));
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                    uDebugLogAdd("Created missing config directory");
                }
                else { uDebugLogAdd($"Config Directory: {ConfigDirectory}"); }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SerializeSettings()
        {
            int retries = 3;
            int delay = 1000;
            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    uDebugLogAdd("Starting settings serialization");
                    var settings = new JsonSerializerSettings()
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                        NullValueHandling = NullValueHandling.Ignore,
                    };
                    string serializedObj = JsonConvert.SerializeObject(Toolbox.settings, Formatting.Indented, settings);
                    using (StreamWriter sw = new StreamWriter($@"{ConfigDirectory}Settings.conf"))
                        sw.WriteLine(serializedObj);
                    uDebugLogAdd($"Settings.conf serialized");
                    break;
                }
                catch (Exception ex)
                {
                    if (i <= retries)
                        Thread.Sleep(delay);
                    else
                        LogException(ex);
                }
            }
        }

        private void DeSerializeSettings()
        {
            try
            {
                uDebugLogAdd("Starting settings deserialization");
                if (File.Exists($@"{ConfigDirectory}Settings.conf"))
                {
                    uDebugLogAdd($@"Settings file found: {ConfigDirectory}Settings.conf");
                    var settings = new JsonSerializerSettings()
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                        NullValueHandling = NullValueHandling.Ignore,
                    };
                    using (StreamReader sr = new StreamReader($@"{ConfigDirectory}Settings.conf"))
                    {
                        dynamic tmpSettings = JsonConvert.DeserializeObject<Settings>(sr.ReadToEnd(), settings);
                        if (tmpSettings != null)
                            Toolbox.settings = tmpSettings;
                    }
                    uDebugLogAdd($@"Deserialized file: {ConfigDirectory}Settings.conf");
                }
                else { uDebugLogAdd($@"Setings file not found at: {ConfigDirectory}Settings.conf"); }
                uDebugLogAdd("Finished settings deserialization");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void SaveSettings()
        {
            try
            {
                uDebugLogAdd("Saving settings");
                SerializeSettings();
                uDebugLogAdd("Settings saved");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void InitializeBackgroundNotificationIcon()
        {
            try
            {
                uDebugLogAdd("Initializing Background Notification Icon");
                MenuItem ItemDesktop = new MenuItem() { Name = "IShowDesktopWindow", Header = "Show Panacea" };
                ItemDesktop.Click += ItemDesktop_Click;
                MenuItem ItemUtilBar = new MenuItem() { Name = "IShowUtilBar", Header = "Show UtilityBar" };
                ItemUtilBar.Click += ItemUtilBar_Click;
                Separator ItemSeperator = new Separator();
                MenuItem ItemQuit = new MenuItem() { Name = "IQuit", Header = "Quit" };
                ItemQuit.Click += ItemQuit_Click;
                ContextMenu NotificationMenu = new ContextMenu()
                {
                    Background = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                          {
                              new GradientStop(Color.FromArgb(100,28,28,28), 0),
                              new GradientStop(Color.FromArgb(100,0,0,0), 1)
                          }
                    },
                    Foreground = new SolidColorBrush(Color.FromArgb(100, 141, 141, 141))
                };
                NotificationMenu.Items.Add(ItemDesktop);
                NotificationMenu.Items.Add(ItemUtilBar);
                NotificationMenu.Items.Add(ItemSeperator);
                NotificationMenu.Items.Add(ItemQuit);
                taskIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon { Icon = new System.Drawing.Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Panacea.Dependencies.Panacea_Icon.ico")) };
                taskIcon.LeftClickCommand = new CommandShowPanacea();
                taskIcon.ContextMenu = NotificationMenu;
                uDebugLogAdd("Background Notification Icon successfully initialized");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ItemQuit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var response = Prompt.YesNo("Are you sure you want to Anihilate the Panacea process completely?");
                if (response == Prompt.PromptResponse.Yes)
                    ToggleApplicationStop();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ItemUtilBar_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowUtilityBar();
        }

        private void ShowUtilityBar()
        {
            try
            {
                UtilBar.Show();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ItemDesktop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowDesktopWindow();
        }

        private void ShowDesktopWindow()
        {
            try
            {
                DesktopWindow.Show();
                DesktopWindow.BringWinMainBackFromTheVoid();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TearDownBackgroundNotificationIcon()
        {
            try
            {
                uDebugLogAdd("Tearing down Background Notification Icon");
                taskIcon.Dispose();
                uDebugLogAdd("Background Notification Icon successfully torn down");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void tStartTimedActions()
        {
            try
            {
                uDebugLogAdd("Starting timed actions worker");
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.ProgressChanged += (sender, e) =>
                {
                    try
                    {
                        if (e.ProgressPercentage == 1)
                        {
                            SaveSettings();
                        }
                        if (e.ProgressPercentage == 2)
                        {
                            tCheckForUpdates();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                };
                worker.DoWork += (sender, e) =>
                {
                    try
                    {
                        var updateCounter = 0;
                        while (true)
                        {
                            Thread.Sleep(TimeSpan.FromMinutes(5));
                            uDebugLogAdd("5min passed, now running timed actions");
                            worker.ReportProgress(1);
                            if (updateCounter >= 12)
                            {
                                uDebugLogAdd("60min passed, now running update timed action");
                                worker.ReportProgress(2);
                                updateCounter = -1;
                            }
                            updateCounter++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                };
                worker.RunWorkerAsync();
                uDebugLogAdd("Auto timed actions worker started");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void tCheckForUpdates()
        {
            try
            {
                uDebugLogAdd("Checking for updates...");
                if (gitClient == null)
                {
                    gitClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("Panacea"));
                    uDebugLogAdd("gitClient was null, initialized gitClient");
                }
                if (Toolbox.settings.CurrentVersion == null)
                {
                    Toolbox.settings.CurrentVersion = new Version("0.0.0.0");
                    uDebugLogAdd($"Current version was null, set to: {Toolbox.settings.CurrentVersion}");
                }
                var currVer = GetVersionNumber();
                var prevVer = new Version(Toolbox.settings.CurrentVersion.ToString());
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        // Upstaller update
                        var upstaller = $"{CurrentDirectory}\\Upstaller.exe";
                        if (File.Exists(upstaller))
                        {
                            Toolbox.settings.UpCurrentVersion = Toolbox.GetVersionNumber(upstaller);
                            uDebugLogAdd($"Upstaller Current Version: {Toolbox.settings.UpCurrentVersion}");
                            upstallerUpdateInProg = true;
                            Task u = Task.Run(async () => { await GetUpdate(AppUpdate.Upstaller); });
                            while (!u.IsCompleted)
                            {
                                Thread.Sleep(500);
                            }
                            worker.ReportProgress(2);
                        }
                        else
                            uDebugLogAdd($"Upstaller not found at: {upstaller}");

                        // Panacea update
                        if ((Toolbox.settings.CurrentVersion != new Version("0.0.0.0")) && Toolbox.settings.CurrentVersion != currVer)
                        {
                            uDebugLogAdd($"Current Version [{currVer}] isn't 0.0.0.0 and is newer than the existing version [{Toolbox.settings.CurrentVersion}] | I believe an update may have occured, or I'm insane...");
                            worker.ReportProgress(3);
                        }
                        else
                            uDebugLogAdd($"Current Version [{currVer}] is 0.0.0.0 or the same as it was before: {Toolbox.settings.CurrentVersion} | Skipped notification");
                        Toolbox.settings.CurrentVersion = currVer;
                        uDebugLogAdd($"Current Version: {Toolbox.settings.CurrentVersion}");
                        Task t = Task.Run(async () => { await GetUpdate(AppUpdate.Panacea); });
                        while ((!t.IsCompleted) || upstallerUpdateInProg)
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
                            if (Toolbox.settings.CurrentVersion.CompareTo(Toolbox.settings.ProductionVersion) < 0)
                            {
                                uDebugLogAdd($"New version found: [c]{Toolbox.settings.CurrentVersion} [p]{Toolbox.settings.ProductionVersion}");
                                if (Toolbox.settings.UpdateAvailable != true)
                                    TriggerUpdateAvailable();
                            }
                            else
                            {
                                uDebugLogAdd($"Current version is the same or newer than release: [c]{Toolbox.settings.CurrentVersion} [p]{Toolbox.settings.ProductionVersion}");
                                if (Toolbox.settings.ShowChangelog)
                                {
                                    Toolbox.settings.ShowChangelog = false;
                                    ShowChangelog();
                                }
                            }
                            if (Toolbox.changeLogs.Find(x => x.Version == Toolbox.settings.CurrentVersion.ToString()) != null)
                            {
                                uDebugLogAdd("Found changelog item for current running version, verifying if beta release");
                                ToggleBetaVersion();
                            }
                            SaveSettings();
                        }
                        else if (pe.ProgressPercentage == 2)
                        {
                            if (Toolbox.settings.UpCurrentVersion.CompareTo(Toolbox.settings.UpProductionVersion) < 0)
                            {
                                uDebugLogAdd($"New version of the upstaller found: [c]{Toolbox.settings.UpCurrentVersion} [p]{Toolbox.settings.UpProductionVersion}");
                                UpdateUpstaller();
                            }
                            else
                            {
                                uDebugLogAdd($"Upstaller version is the same or newer than release: [c]{Toolbox.settings.UpCurrentVersion} [p]{Toolbox.settings.UpProductionVersion}");
                                upstallerUpdateInProg = false;
                            }
                        }
                        else if (pe.ProgressPercentage == 3)
                        {
                            ShowNotification($"Updated from v{prevVer} to v{currVer}");
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

        public void ShowNotification(string v)
        {
            try
            {
                DesktopWindow.ShowNotification(v);
                UtilBar.ShowNotification(v);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleBetaVersion()
        {
            throw new NotImplementedException();
        }

        public void ShowChangelog()
        {
            try
            {
                Changelog changelog = new Changelog();
                changelog.ShowDialog();
                //uDebugLogAdd("Showing Changelog Window");
                //Prompt.OK($"Changelog for v{Toolbox.settings.CurrentVersion}:{Environment.NewLine}{Toolbox.settings.LatestChangelog}", TextAlignment.Left);
                //uDebugLogAdd("Closed changelog window");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SendDiagnostics()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    uDebugLogAdd("Attempting to send diagnostic info...");
                    var diagDir = $@"{CurrentDirectory}\Diag";
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
                    DumpDebugLog();
                    ZipFile.CreateFromDirectory(LogDirectory, zipPath);
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
                    ShowNotification("Diagnostic info sent to the developer, Thank You!");
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        private void TriggerUpdateAvailable()
        {
            throw new NotImplementedException();
        }

        public async Task GetUpdate(AppUpdate appUpdate)
        {
            try
            {
                var releases = await gitClient.Repository.Release.GetAll("rwobig93", "Panacea");
                var appName = appUpdate.ToString();
                uDebugLogAdd($"Releases found: {releases.Count}");
                if (appUpdate == AppUpdate.Panacea)
                {
                    uDebugLogAdd("Gathering changelog from each release");
                    Toolbox.changeLogs.Clear();
                    var panaceaReleases = releases.ToList().FindAll(x => x.Assets[0].Name.Contains(appName)).OrderBy(x => x.TagName);
                    foreach (var release in panaceaReleases)
                    {
                        if (!string.IsNullOrWhiteSpace(release.Body))
                        {
                            uDebugLogAdd($"Adding release to changelog list: {release.TagName}");
                            var changeLog = new ChangeLogItem()
                            {
                                Version = $"v{release.TagName}",
                                Body = release.Body,
                                BugFixes = release.Body.Contains("Bug Fixes:") ? Visibility.Visible : Visibility.Hidden,
                                NewFeatures = release.Body.Contains("New Features:") ? Visibility.Visible : Visibility.Hidden,
                                BetaRelease = release.Prerelease ? Visibility.Visible : Visibility.Hidden
                            };
                            Toolbox.changeLogs.Add(changeLog);
                            uDebugLogAdd($"Added release [v]{changeLog.Version} [fix]{changeLog.BugFixes.ToString()} [feat]{changeLog.NewFeatures.ToString()} [beta]{changeLog.BetaRelease.ToString()}");
                        }
                        else
                            uDebugLogAdd($"Release {release.TagName} is a {release.Name} release, skipping changeLog add");
                    }
                    uDebugLogAdd("Finished gathering all changelogs");
                }
                Octokit.Release recentRelease;
                uDebugLogAdd($"Getting update for: {appName}");
                if (Toolbox.settings.BetaUpdate)
                    recentRelease = releases.ToList().FindAll(x => x.Assets[0].Name.Contains(appName)).OrderBy(x => x.TagName).Last();
                else
                    recentRelease = releases.ToList().FindAll(x => x.Prerelease == false && x.Assets[0].Name.Contains(appName)).OrderBy(x => x.TagName).Last();
                Version prodVersion = new Version(recentRelease.TagName);
                switch (appUpdate)
                {
                    case AppUpdate.Panacea:
                        Toolbox.settings.ProductionVersion = prodVersion;
                        uDebugLogAdd($"ProdVer: {Toolbox.settings.ProductionVersion}");
                        Toolbox.settings.ProductionURI = $@"{Defaults.GitUpdateURIBase}/{recentRelease.TagName}/{appName}.exe";
                        uDebugLogAdd($"URI: {Toolbox.settings.ProductionURI}");
                        Toolbox.settings.LatestChangelog = recentRelease.Body;
                        uDebugLogAdd($"Changelog: {Environment.NewLine}{Toolbox.settings.LatestChangelog}");
                        break;
                    case AppUpdate.Upstaller:
                        Toolbox.settings.UpProductionVersion = prodVersion;
                        uDebugLogAdd($"UpProdVer: {Toolbox.settings.UpProductionVersion}");
                        Toolbox.settings.UpProductionURI = $@"{Defaults.GitUpdateURIBase}/{recentRelease.TagName}/{appName}.exe";
                        uDebugLogAdd($"URI: {Toolbox.settings.UpProductionURI}");
                        break;
                    default:
                        break;
                }
                uDebugLogAdd($"Finished getting github recent version");
            }
            catch (HttpRequestException hre) { uDebugLogAdd($"Unable to reach site, error: {hre.Message}"); Toolbox.changeLogs.Add(new ChangeLogItem() { BetaRelease = Visibility.Visible, BugFixes = Visibility.Visible, NewFeatures = Visibility.Visible, Version = @"¯\_(ツ)_/¯", Body = "Github is unreachable, couldn't get the changelogzzzz :(" }); }
            catch (RateLimitExceededException rlee) { uDebugLogAdd($"Unable to get update, API rate limit reached: [L]{rlee.Limit} [R]{rlee.Remaining} [ResetTime]{rlee.Reset} [CurrentTime]{DateTime.Now.ToLocalTime().ToString()} [M]{rlee.Message}"); }
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

        private void uStatusUpdate(string v)
        {
            throw new NotImplementedException();
        }

        #region Install/Update

        public Version GetVersionNumber()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void UpdateApplication()
        {
            try
            {
                uDebugLogAdd("Starting update process");
                SaveSettings();
                var upstaller = $"{CurrentDirectory}\\Upstaller.exe";
                uDebugLogAdd($"Upstaller dir: {upstaller}");
                if (File.Exists(upstaller))
                {
                    uDebugLogAdd("Upstaller exists, starting the proc");
                    var args = "/update";
                    if (Toolbox.settings.BetaUpdate)
                        args = "/update /beta";
                    Toolbox.settings.ShowChangelog = true;
                    Toolbox.settings.UpdateAvailable = false;
                    SaveSettings();
                    Process proc = new Process() { StartInfo = new ProcessStartInfo() { FileName = upstaller, Arguments = args } };
                    proc.Start();
                    uDebugLogAdd($"Started {proc.ProcessName}[{proc.Id}]");
                }
                else
                {
                    uDebugLogAdd("Upstaller doesn't exist in the current running directory, cancelling...");
                    ShowNotification("Updater/Installer not found, please repair the application");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateUpstaller()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    CloseUpstallerInstances();
                    BackupEverything();
                    PrepStagingArea();
                    DownloadWhatWeCameFor();
                    PutUpstallerInItsPlace();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
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
                var dataDir = $@"{CurrentDirectory}\Config";
                var backupDir = $@"{CurrentDirectory}\Backup";
                var backupDataDir = $@"{CurrentDirectory}\Backup\Config";
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
                FileInfo fi = new FileInfo($@"{CurrentDirectory}\Upstaller.exe");
                uDebugLogAdd($@"Moving old client to it's new home, Before: {fi.FullName}");
                fi.MoveTo($@"{CurrentDirectory}\Backup\Upstaller.exe");
                uDebugLogAdd($@"Finished old client backup, After: {fi.FullName}");
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
                var backupDataDir = $@"{CurrentDirectory}\Backup\Config";
                DirectoryInfo di = new DirectoryInfo(backupDataDir);
                if (di.Exists)
                {
                    foreach (var fi in di.EnumerateFiles())
                    {
                        uDebugLogAdd($"Removing old backed up file: {fi.FullName}");
                        fi.Delete();
                        uDebugLogAdd("Deleted file");
                    }
                }
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
                if (File.Exists($@"{CurrentDirectory}\Backup\Upstaller.exe"))
                {
                    uDebugLogAdd("Existing old client found, removing that sucka!");
                    FileInfo fi = new FileInfo($@"{CurrentDirectory}\Backup\Upstaller.exe");
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

        private void VerifyBackupDirectory()
        {
            try
            {
                var backupDir = $@"{CurrentDirectory}\Backup";
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

        private void FinishUpstallerUpdate()
        {
            try
            {
                uDebugLogAdd("Good news everyone! The Upstaller update finished!");
                upstallerUpdateInProg = false;
                uStatusUpdate($"Upstaller updated from v{Toolbox.settings.UpCurrentVersion} to v{Toolbox.settings.UpProductionVersion}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void PutUpstallerInItsPlace()
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        uDebugLogAdd("Starting sleep thread to wait for the SLOOOOOW download to finish");
                        while (downloadInProgress)
                        {
                            Thread.Sleep(100);
                        }
                        uDebugLogAdd("Download FINALLY finished, jeez we need better throughput!");
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
                            FileInfo fi = new FileInfo($@"{CurrentDirectory}\TopSecret\Upstaller.exe");
                            uDebugLogAdd($@"Moving Upstaller.exe to it's place | Before: {fi.FullName}");
                            fi.MoveTo($@"{CurrentDirectory}\Upstaller.exe");
                            uDebugLogAdd($@"Moved Upstaller.exe to it's place | After: {fi.FullName}");
                            uDebugLogAdd("Finished Upstaller update process.");
                            FinishUpstallerUpdate();
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

        private void DownloadWhatWeCameFor()
        {
            try
            {
                downloadInProgress = true;
                uDebugLogAdd($"Starting Upstaller download, this is getting exciting!!! URI: {Toolbox.settings.UpProductionURI}");
                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += (s, e) => { uDebugLogAdd($"Download progress updated: {e.ProgressPercentage}%"); };
                webClient.DownloadFileCompleted += (s2, e2) =>
                {
                    uDebugLogAdd("Finished Upstaller download");
                    uStatusUpdate("Finished Upstaller download!");
                    downloadInProgress = false;
                };
                uDebugLogAdd("Starting that download thang");
                uStatusUpdate("Starting Upstaller download...");
                webClient.DownloadFileAsync(new Uri(Toolbox.settings.UpProductionURI), $@"{CurrentDirectory}\TopSecret\Upstaller.exe");
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
                var stagingArea = $@"{CurrentDirectory}\TopSecret";
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

        private void CloseUpstallerInstances()
        {
            try
            {
                uDebugLogAdd("Starting Upstaller process slaughter");
                foreach (var proc in Process.GetProcessesByName("Upstaller"))
                {
                    uDebugLogAdd($"Killing process [{proc.Id}]{proc.ProcessName}");
                    proc.Kill();
                    uDebugLogAdd("Killed process");
                }
                uDebugLogAdd("Finished Upstaller process slaughter");
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

        #endregion
    }
}
