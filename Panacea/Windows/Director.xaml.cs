using NAudio.CoreAudioApi;
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
using System.Management;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for Director.xaml
    /// </summary>
    public partial class Director : Window
    {
        #region Constructor
        public Director()
        {
            InitializeComponent();
            VerifyDebugMode();
            Startup();
        }
        #endregion

        #region Globals
        // Public Globals
        public string LogDirectory { get; } = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Logs\";
        public string ConfigDirectory { get; } = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Config\";
        public string ExceptionDirectory { get; } = $@"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Logs\Exceptions\";
        public string CurrentDirectory { get; } = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static Director Main;
        public MainWindow DesktopWindow;
        public UtilityBar UtilBar;
        private HandleDisplay WindowHandleDisplay;
        public bool UpdateAvailable { get; private set; } = false;
        public bool DebugMode { get; private set; } = false;
        public CurrentDisplay CurrentDisplay { get; private set; } = new CurrentDisplay();
        public List<Window> PopupWindows { get; set; } = new List<Window>();

        // Private Globals
        private bool upstallerUpdateInProg = false;
        private bool downloadInProgress = false;
        private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon taskIcon = null;
        private Octokit.GitHubClient gitClient = null;
        private ManagementEventWatcher procWatchStart = null;
        private ManagementEventWatcher procWatchStop = null;
        #endregion

        #region Event Handlers
        private void ItemQuit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var response = Prompt.YesNo("Are you sure you want to Anihilate the Panacea process completely?");
                if (response == Prompt.PromptResponse.Yes)
                    FullApplicationClose();
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

        private void ItemDesktop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowDesktopWindow();
        }
        #endregion

        #region Methods
        public void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            try
            {
                Toolbox.uAddDebugLog($"DIRECTR: {_log}", _type, caller);
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

        private void VerifyDebugMode()
        {
#if DEBUG
            DebugMode = true;
#endif
        }

        private void Startup()
        {
            Main = this;
            SubscribeToEvents();
            SetupAppFiles();
            uDebugLogAdd(string.Format("{0}###################################### Application Start ######################################{0}", Environment.NewLine));
            DeSerializeSettings();
            InitializeBackgroundNotificationIcon();
            InitializeDesktopWindow();
            InitializeUtilBar();
            RefreshDisplaySizes();
            ShowPreferredWindow();
            tStartTimedActions();
            CleanupOldFiles();
            tCheckForUpdates();
            this.Hide();
        }

        private void FullApplicationClose()
        {
            UtilBar.TearDownGlobalHotkey();
            //TeardownGlobalProcessWatchers();
            TearDownBackgroundNotificationIcon();
            CloseAllOpenPopupWindows();
            DesktopWindow.Close();
            UtilBar.Close();
            SaveSettings();
            uDebugLogAdd(string.Format("{0}##################################### Application Closing #####################################{0}", Environment.NewLine));
            DumpDebugLog();
            this.Close();
        }

        private void CloseAllOpenPopupWindows()
        {
            try
            {
                bool allWindowsGone = false;
                while (!allWindowsGone)
                {
                    foreach (var popup in PopupWindows.ToList())
                    {
                        try
                        {
                            if (popup != null)
                            {
                                uDebugLogAdd($"Popup wasn't closed, closing now: {popup.Name}");
                                popup.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                    if (PopupWindows.ToList().Count <= 0)
                        allWindowsGone = true;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void ShowPreferredWindow()
        {
            try
            {
                if (Toolbox.settings.PreferredWindow == WindowPreference.NotChosen)
                    AskForPreferredWindowStyle();
                if (Toolbox.settings.InitialStartup)
                {
                    OpenInfoWindow(HelpMenu.NotificationIcon, true);
                }
                if (Toolbox.settings.PreferredWindow == WindowPreference.DesktopWindow)
                    ShowDesktopWindow();
                else
                    ShowUtilityBar();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void AskForPreferredWindowStyle()
        {
            try
            {
                var response = Prompt.WindowPreference();
                if (response == Prompt.PromptResponse.Custom2)
                    Toolbox.settings.PreferredWindow = WindowPreference.DesktopWindow;
                else
                    Toolbox.settings.PreferredWindow = WindowPreference.UtilityBar;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SubscribeToEvents()
        {
            Events.AudioEndpointListUpdated += Events_AudioEndpointListUpdated;
            Events.WinInfoChanged += Events_WinInfoChanged;
            Events.StartProcInfoChanged += Events_StartProcInfoChanged;
            Events.NetConnectivityChanged += Events_NetConnectivityChanged;
            Events.DisplayProfileChanged += Events_DisplayProfileChanged;
            //InitializeGlobalProcessWatchers();
        }

        private void InitializeGlobalProcessWatchers()
        {
            try
            {
                uDebugLogAdd("Starting Global Process Watcher Initializations");
                string watcherScope = @"\\.\root\CIMV2";
                string startQuery = "SELECT TargetInstance FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'";
                string stopQuery = "SELECT TargetInstance FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'";

                procWatchStart = new ManagementEventWatcher(watcherScope, startQuery);
                procWatchStart.EventArrived += ProcWatchStart_EventArrived;
                procWatchStart.Start();
                uDebugLogAdd("Initialized Process Start Watcher");

                procWatchStop = new ManagementEventWatcher(watcherScope, stopQuery);
                procWatchStop.EventArrived += ProcWatchStop_EventArrived;
                procWatchStop.Start();
                uDebugLogAdd("Initialized Process Stop Watcher");

                uDebugLogAdd("Finished Initializing Global Process Watchers");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TeardownGlobalProcessWatchers()
        {
            try
            {
                uDebugLogAdd("Starting Global Process Watcher Teardown");
                if (procWatchStart != null)
                {
                    procWatchStart.Stop();
                    procWatchStart = null;
                    uDebugLogAdd("Tore down Global Process Start Watcher");
                }
                if (procWatchStop != null)
                {
                    procWatchStop.Stop();
                    procWatchStop = null;
                    uDebugLogAdd("Tore down Global Process Stop Watcher");
                }
                uDebugLogAdd("Global Process Watcher Teardown finished");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ProcWatchStop_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                ManagementBaseObject targetInst = ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value);
                string handle = targetInst["Handle"].ToString();
                var foundProc = Process.GetProcessById(WinAPIWrapper.GetProcessId(new IntPtr(Convert.ToInt32(handle))));
                uDebugLogAdd($"Stop Event: {foundProc.ProcessName} | {foundProc.Id} | {handle}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ProcWatchStart_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                ManagementBaseObject targetInst = ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value);
                string handle = targetInst["Handle"].ToString();
                var foundProc = Process.GetProcessById(WinAPIWrapper.GetProcessId(new IntPtr(Convert.ToInt32(handle))));
                uDebugLogAdd($"Start Event: {foundProc.ProcessName} | {foundProc.Id} | {handle}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Events_DisplayProfileChanged()
        {
            try
            {
                // Thrown here in case the event doesn't have a listener yet
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Events_NetConnectivityChanged(NetConnectivityArgs args)
        {
            try
            {
                // Thrown here currently so the event has a listener if the InfoPopup window isn't initialized
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Events_StartProcInfoChanged(StartProcArgs args)
        {
            UpdateAllSettingsUI();
        }

        private void Events_WinInfoChanged(WindowInfoArgs args)
        {
            UpdateAllSettingsUI();
        }

        private void Events_AudioEndpointListUpdated(AudioEndpointListArgs args)
        {
            if (!Interfaces.AudioMain.FirstRefresh)
                ShowNotification("Audio Devices Updated!");
            else
                Interfaces.AudioMain.FirstRefresh = false;
        }

        private void InitializeUtilBar()
        {
            try
            {
                uDebugLogAdd("Initializing UtilityBar");
                UtilBar = new UtilityBar();
                UtilBar.HideUtilBarInBackground();
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
                DesktopWindow.HideWinMainInBackground();
                uDebugLogAdd("Finished Initializing Desktop Window");
            }
            catch (Exception ex)
            {
                LogException(ex);
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
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { DesktopWindow.txtStatus.Clear(); } catch (Exception ex) { LogException(ex); } });
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
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { DesktopWindow.txtStatus.Clear(); } catch (Exception ex) { LogException(ex); } });
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
                        //MissingMemberHandling = MissingMemberHandling.Ignore,
                        //DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                        //NullValueHandling = NullValueHandling.Ignore,
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
                        //MissingMemberHandling = MissingMemberHandling.Ignore,
                        //DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                        //NullValueHandling = NullValueHandling.Ignore,
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
                MenuItem ItemDesktop = new MenuItem() { Name = "IShowDesktopWindow", Header = "Show Desktop Window" };
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
                              new GradientStop(Toolbox.ColorFromHex("#FF1C1C1C"), 0),
                              new GradientStop(Colors.Black, 1)
                          }
                    },
                    Foreground = Toolbox.SolidBrushFromHex("#FF8D8D8D")
                };
                NotificationMenu.Items.Add(ItemDesktop);
                NotificationMenu.Items.Add(ItemUtilBar);
                NotificationMenu.Items.Add(ItemSeperator);
                NotificationMenu.Items.Add(ItemQuit);
                taskIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon { Icon = new System.Drawing.Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Panacea.Dependencies.Panacea_Icon.ico")) };
                taskIcon.ContextMenu = NotificationMenu;
                taskIcon.DoubleClickCommand = new CommandShowChosenWindow();
                uDebugLogAdd("Background Notification Icon successfully initialized");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShowUtilityBar()
        {
            try
            {
                UtilBar.BringUtilBarBackFromTheVoid();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShowDesktopWindow()
        {
            try
            {
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
                        switch (e.ProgressPercentage)
                        {
                            case 1:
                                RefreshDisplaySizes();
                                break;
                            case 2:
                                SaveSettings();
                                break;
                            case 3:
                                tCheckForUpdates();
                                break;
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
                            Thread.Sleep(1000);
                            worker.ReportProgress(1);
                            if (updateCounter > 300 && updateCounter % 300 == 0)
                            {
                                uDebugLogAdd("5min passed, now running timed actions");
                                worker.ReportProgress(2);
                            }
                            if (updateCounter > 3600 && updateCounter % 3600 == 0)
                            {
                                uDebugLogAdd("60min passed, now running update timed action");
                                worker.ReportProgress(3);
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
                                    SaveSettings();
                                    if (DebugMode)
                                        ShowNotification($"Skipping Changelog show as debug: {DebugMode}");
                                    else
                                    {
                                        if (!Toolbox.settings.InitialStartup)
                                            Actions.ShowChangelog();
                                    }
                                }
                            }
                            if (Toolbox.changeLogs.Find(x => x.Version == Toolbox.settings.CurrentVersion.ToString()) != null)
                            {
                                uDebugLogAdd("Found changelog item for current running version, verifying if beta release");
                                if (Toolbox.changeLogs.Find(x => x.Version == Toolbox.settings.CurrentVersion.ToString()).BetaRelease == Visibility.Visible)
                                    TriggerBetaVersion();
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

        private void TriggerBetaVersion()
        {
            uDebugLogAdd("Current version is verified as Beta Release, triggering Beta UI Elements");
            DesktopWindow.TriggerBetaReleaseUI();
            UtilBar.TriggerBetaRelease();
        }

        private void TriggerUpdateAvailable()
        {
            try
            {
                uDebugLogAdd("Triggering update available on windows");
                UpdateAvailable = true;
                DesktopWindow.TriggerUpdateAvailable();
                UtilBar.TriggerUpdateAvailable();
                uDebugLogAdd("Finished update availablility trigger on all windows");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public async Task GetUpdate(AppUpdate appUpdate)
        {
            try
            {
                if (DebugMode)
                {
                    uDebugLogAdd($"Debug mode is {DebugMode}, skipping gitClient call...");
                    return;
                }
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
                                VersionColor = new SolidColorBrush(Toolbox.ColorFromHex("#FFA0A0A0")),
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

        public void uStatusUpdate(string v)
        {
            try
            {
                uDebugLogAdd(v, DebugType.STATUS);
                DesktopWindow.uStatusUpdate(v);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void UpdateAllSettingsUI()
        {
            try
            {
                uDebugLogAdd("Starting settings UI update");
                DesktopWindow.UpdateSettingsUI();
                UtilBar.UpdateSettingsUI();
                uDebugLogAdd("Finished settings UI update");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CleanupOldFiles()
        {
            try
            {
                uDebugLogAdd("Starting old file cleanup");
                uDebugLogAdd($"Cleaning up Director.Main.LogDirectory: {Director.Main.LogDirectory}");
                var logDirRemoves = 0;
                foreach (var file in Directory.EnumerateFiles(Director.Main.LogDirectory))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    string fileNaem = fileInfo.Name;
                    if ((fileInfo.LastWriteTime <= DateTime.Now.AddDays(-14)))
                    {
                        try
                        {
                            fileInfo.Delete();
                            uStatusUpdate($"Deleted old log file: {fileNaem}");
                            logDirRemoves++;
                        }
                        catch (IOException io)
                        {
                            uDebugLogAdd($"File couldn't be deleted: {fileInfo.Name} | {io.Message}");
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                }
                uDebugLogAdd($"Removed {logDirRemoves} old log(s)");
                uDebugLogAdd($"Cleaning up Director.Main.ExceptionDirectory: {Director.Main.ExceptionDirectory}");
                var exDirRemoves = 0;
                foreach (var file in Directory.EnumerateFiles(Director.Main.ExceptionDirectory))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    string fileNaem = fileInfo.Name;
                    if ((fileInfo.LastWriteTime <= DateTime.Now.AddDays(-14)))
                    {
                        try
                        {
                            fileInfo.Delete();
                            uStatusUpdate($"Deleted old exception file: {fileNaem}");
                            exDirRemoves++;
                        }
                        catch (IOException io)
                        {
                            uDebugLogAdd($"File couldn't be deleted: {fileInfo.Name} | {io.Message}");
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                }
                uDebugLogAdd($"Removed {exDirRemoves} old exception log(s)");
                var diagRemoves = 0;
                if (Directory.Exists($@"{Director.Main.CurrentDirectory}\Diag"))
                {
                    uDebugLogAdd($"Cleaning up diagDir: {$@"{Director.Main.CurrentDirectory}\Diag"}");
                    foreach (var file in Directory.EnumerateFiles($@"{Director.Main.CurrentDirectory}\Diag"))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        string fileNaem = fileInfo.Name;
                        if ((fileInfo.LastWriteTime <= DateTime.Now.AddDays(-14)))
                        {
                            try
                            {
                                fileInfo.Delete();
                                uStatusUpdate($"Deleted old diag zip file: {fileNaem}");
                                diagRemoves++;
                            }
                            catch (IOException io)
                            {
                                uDebugLogAdd($"File couldn't be deleted: {fileInfo.Name} | {io.Message}");
                            }
                            catch (Exception ex)
                            {
                                LogException(ex);
                            }
                        }
                    }
                    uDebugLogAdd($"Removed {diagRemoves} diag zip(s)");
                }
                uDebugLogAdd($"Finished old file cleanup, removed {diagRemoves + exDirRemoves + logDirRemoves} file(s)");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RefreshDisplaySizes()
        {
            try
            {
                CurrentDisplay tempDisplay = new CurrentDisplay();
                foreach (var display in System.Windows.Forms.Screen.AllScreens)
                {
                    tempDisplay.Displays.Add(Display.ConvertFromScreen(display));
                }
                if (CurrentDisplay == null || CurrentDisplay.Displays.Count <= 0)
                {
                    CurrentDisplay = new CurrentDisplay();
                    foreach (var disp in tempDisplay.Displays)
                        CurrentDisplay.Displays.Add(disp);
                    uDebugLogAdd("Current display was null, created new current display");
                }
                tempDisplay.Displays = tempDisplay.Displays.OrderBy(x => x.Bounds.X).ToList();
                bool displayChanged = VerifyDisplayProfileMatch(tempDisplay);
                tempDisplay = null;
                if (displayChanged)
                {
                    Events.TriggerDisplayProfileChange();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private bool VerifyDisplayProfileMatch(CurrentDisplay displayToMatch)
        {
            bool displayProfileChanged = false;
            try
            {
                if (Toolbox.settings.DisplayProfileLibrary == null)
                {
                    uDebugLogAdd("DisplayProfileLibrary was null, instantiating a new one");
                    Toolbox.settings.DisplayProfileLibrary = new DisplayProfileLibrary();
                }
                DisplayProfile matchedProfile = null;
                foreach (var displayProfile in Toolbox.settings.DisplayProfileLibrary.DisplayProfiles)
                {
                    bool isMatch = DisplayProfile.DoDisplaysMatch(displayToMatch, displayProfile.DisplayArea);
                    if (isMatch)
                    {
                        matchedProfile = displayProfile;
                        break;
                    }
                }
                if (matchedProfile == null)
                {
                    uDebugLogAdd("Current DisplayArea didn't match any existing Display Profiles, creating a new one");
                    AddDisplayProfileToLibrary(displayToMatch);
                    displayProfileChanged = true;
                }
                else
                {
                    bool currentMatch = DisplayProfile.DoDisplaysMatch(matchedProfile.DisplayArea, CurrentDisplay);
                    if (currentMatch)
                        displayProfileChanged = false;
                    else
                    {
                        uDebugLogAdd("Display profile match found but isn't current, changing current display to matched display");
                        Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile = matchedProfile;
                        CurrentDisplay = Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile.DisplayArea;
                        displayProfileChanged = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            return displayProfileChanged;
        }

        private void AddDisplayProfileToLibrary(CurrentDisplay display, bool setAsCurrentDisplayProfile = true)
        {
            try
            {
                uDebugLogAdd("Starting Display Profile addition to Library");
                DisplayProfile newDisplay = new DisplayProfile() { DisplayArea = display, PreferredDisplay = display.Displays.Find(x => x.PrimaryDisplay == true) };
                Toolbox.settings.DisplayProfileLibrary.DisplayProfiles.Add(newDisplay);
                uDebugLogAdd("New display added successfully");
                if (setAsCurrentDisplayProfile)
                {
                    Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile = newDisplay;
                    CurrentDisplay = Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile.DisplayArea;
                    uDebugLogAdd($"SetAsCurrent is {setAsCurrentDisplayProfile}, Set new display as current");
                }
                uDebugLogAdd("Finished Display Profile Library Addition");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void UpdateRecordingEndpointList(DeviceState deviceState = DeviceState.Active, bool allLists = false)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (ws, we) =>
            {
                try
                {
                    var audioList = new List<MMDevice>();
                    foreach (var wasapi in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, deviceState))
                    {
                        audioList.Add(wasapi);
                    }
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { Interfaces.AudioMain.EndpointAudioRecordingDeviceList = audioList; } catch (Exception ex) { LogException(ex); } });
                    if (!allLists)
                        Events.TriggerAudioEndpointListUpdate(DataFlow.Capture);
                    else
                        Events.TriggerAudioEndpointListUpdate(DataFlow.All);
                }
                catch (Exception ex)
                {
                    Toolbox.LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        public void UpdatePlaybackEndpointList(DeviceState deviceState = DeviceState.Active, bool allLists = false)
        {

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (ws, we) =>
            {
                try
                {
                    var audioList = new List<MMDevice>();
                    foreach (var wasapi in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, deviceState))
                    {
                        audioList.Add(wasapi);
                    }
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { Interfaces.AudioMain.EndpointAudioPlaybackDeviceList = audioList; } catch (Exception ex) { LogException(ex); } });
                    if (!allLists)
                        Events.TriggerAudioEndpointListUpdate(DataFlow.Render);
                }
                catch (Exception ex)
                {
                    Toolbox.LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        public void OpenWindowHandleFinder()
        {
            try
            {
                uDebugLogAdd($"Opening window handler info window");
                WindowHandleDisplay = new HandleDisplay
                {
                    Topmost = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                uDebugLogAdd($"Window handler info window constructed");
                PopupWindows.Add(WindowHandleDisplay);
                WindowHandleDisplay.Show();
                WindowHandleDisplay.Closed += (s,e) => { Events.TriggerWindowInfoChange(true); PopupWindows.Remove(WindowHandleDisplay); };
                uDebugLogAdd($"Window handler info window shown");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void OpenInfoWindow(HelpMenu menu, bool initialStartup = false)
        {
            try
            {
                var foundWindow = PopupWindows.Find(x => x.GetType() == typeof(HelpWindow));
                if (foundWindow == null)
                {
                    uDebugLogAdd($"Opening info/help window to {menu.ToString()}");
                    HelpWindow help = new HelpWindow(menu);
                    if (initialStartup)
                        help.Closed += (s, e) => { Toolbox.settings.InitialStartup = false; Actions.ShowChangelog(); };
                    help.Show();
                    ShowNotification("Opened Info/Help Window");
                }
                else
                {
                    uDebugLogAdd($"Info/Help window already exists, sliding to {menu.ToString()}");
                    ((HelpWindow)foundWindow).SlideHelpMenu(menu);
                    ((HelpWindow)foundWindow).Focus();
                    ShowNotification($"Slid Help Window to {menu.ToString()}");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void OpenStartProcessWindow(StartProcessItem startItem = null)
        {
            try
            {
                uDebugLogAdd("Initializing new StartProcEditior");
                StartProcEditor editor = new StartProcEditor(startItem);
                PopupWindows.Add(editor);
                editor.Closing += (s, e) => { PopupWindows.Remove(editor); };
                editor.Show();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
        #endregion

        #region Install/Update

        public Version GetVersionNumber()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public void UpdateApplication()
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
