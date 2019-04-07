using NativeWifi;
using Panacea.Classes;
using Panacea.Windows.Popups;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Panacea.MainWindow;
using INTER = System.Windows.Interop;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for UtilityBar.xaml
    /// </summary>
    public partial class UtilityBar : Window
    {
        public UtilityBar()
        {
            PreInitializeStartup();
            InitializeComponent();
            Startup();
        }

        #region Enums

        public enum NetConnectionType
        {
            Wired,
            Wireless,
            NoConnection,
            Unknown,
            EthWifi
        }

        public enum LinkSpeedNotation
        {
            Tbps,
            Gbps,
            Mbps,
            Kbps,
            Bps
        }

        public enum WifiFrequency
        {
            RF5G,
            RF24G,
            None
        }

        public enum WifiPHYProto
        {
            ac,
            n,
            g,
            b,
            a,
            None
        }

        public enum ConnectivityStatus
        {
            Internet,
            Local,
            None,
            DNSError,
            Layer3,
            Layer4,
            DHCPError
        }

        public enum PopupMenu
        {
            Network,
            Settings,
            Audio,
            Windows,
            Emotes,
            None
        }

        public enum Emotes
        {
            Shrug
        }

        #endregion

        #region Globals

        public static UtilityBar UtilBarMain;
        private WlanClient wClient;
        private List<NetworkInterface> netIfs;
        private List<NetworkInterface> _connectedEthIfs { get { return netIfs.FindAll(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet && x.OperationalStatus == OperationalStatus.Up); } }
        private List<string> _connectedSSIDs = new List<string>();
        private bool _connectedToWifi { get { return _connectedSSIDs.Count > 0 ? true : false; } }
        private bool _connectedToEth { get { return _connectedEthIfs.Count > 0 ? true : false; } }
        private bool _bssidChanged = true;
        private bool _closing = false;
        private bool _notificationPlaying = false;
        private bool _startingUp = true;
        private bool _betaVersion = false;
        private bool _firstShow = true;
        private bool _imHiding = false;
        private int _totalNotificationsRun = 0;
        private List<string> notifications = new List<string>();
        private NetworkInterface _currentEthIf;
        private Display dockedDisplay;
        private NetConnectionType currentConnType = NetConnectionType.Unknown;
        private WifiFrequency currentFrequency = WifiFrequency.None;
        private WifiPHYProto currentPHYProto = WifiPHYProto.None;
        private ConnectivityStatus currentConnectivityStatus = ConnectivityStatus.None;
        private EnterAction currentEnterAction = EnterAction.DNSLookup;
        private double? currentLinkSpeed;
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.7) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.7) };
        private NetworkPopup popupNetwork;
        private AudioPopup popupAudio;
        private WindowPopup popupWindows;
        private EmotePopup popupEmote;
        private SettingsPopup popupSettings;
        private INTER.HwndSource _source;
        public WlanClient.WlanInterface CurrentWifiInterface { get; private set; }

        #endregion

        #region Event Handlers

        #region Menu

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            //CloseUtilBar();
            HideUtilBarInBackground();
        }

        private void WinMain_Loaded(object sender, RoutedEventArgs e)
        {
            tLocationWatcher();
        }

        private void WinMain_Closing(object sender, CancelEventArgs e)
        {
            SavePopoutPreferences();
            //CloseUtilBar();
        }

        private void BtnMenuNetwork_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuPopup(PopupMenu.Network);
        }

        private void BtnMenuWindows_Click(object sender, RoutedEventArgs e)
        {
            MoveProfileWindows();
        }

        private void BtnMenuWindows_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
                ToggleMenuPopup(PopupMenu.Windows);
        }

        private void BtnMenuWindows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleMenuPopup(PopupMenu.Windows);
        }

        private void BtnMenuEmote_Click(object sender, RoutedEventArgs e)
        {
            CopyEmoteToClipbord();
        }

        private void BtnMenuEmote_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
                ToggleMenuPopup(PopupMenu.Emotes);
        }

        private void BtnMenuEmote_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleMenuPopup(PopupMenu.Emotes);
        }

        private void BtnMenuSettings_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuPopup(PopupMenu.Settings);
        }

        private void BtnMenuAudio_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuPopup(PopupMenu.Audio);
        }

        private void TxtNetMain_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        NetToggleEnterAction();
                    }
                    else
                        NetEnterAction();
                }
                else if (e.Key == Key.Left || e.Key == Key.Right)
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        MoveUtilBarToOtherDisplay(e.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CirNetPing_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                NetToggleEnterAction(EnterAction.Ping);
        }

        private void CirNetNSLookup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                NetToggleEnterAction(EnterAction.DNSLookup);
        }

        private void CirNetTrace_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void LblNetType_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                NetToggleEnterAction();
        }

        #endregion

        #endregion

        #region Methods

        #region General

        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            try
            {
                Toolbox.uAddDebugLog($"UTILBAR: {_log}", _type, caller);
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
                uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
                uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
            }
        }

        private void PreInitializeStartup()
        {
            UtilityBar.UtilBarMain = this;
            MoveUtilBarOffScreen();
        }

        private void MoveUtilBarOffScreen()
        {
            this.Top = -32000;
            this.Left = -32000;
        }

        private void Startup()
        {
            SubscribeToEvents();
            tNetworkConnectivityMonitor();
            NetToggleEnterAction(currentEnterAction);
            UpdateCurrentWindowProfile();
            _startingUp = false;
        }

        private void SubscribeToEvents()
        {
            try
            {
                Events.UtilBarMoveTrigger += Events_UtilBarMoveTrigger;
                Events.WinInfoChanged += Events_WinInfoChanged;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Events_WinInfoChanged(WindowInfoArgs args)
        {
            UpdateCurrentWindowProfile();
        }

        private void Events_UtilBarMoveTrigger(UtilMoveArgs args)
        {
            try
            {
                uDebugLogAdd($"Utilbar was moved to: [x]{args.X} [y]{args.Y}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void OnHotKeyPressed()
        {
            try
            {
                this.Activate();
                this.txtNetMain.Focus();
                Keyboard.Focus(this.txtNetMain);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CloseUtilBar()
        {
            if (_closing)
                return;
            try
            {

                _closing = true;
                if (popupNetwork != null)
                {
                    popupNetwork.Close();
                    popupNetwork = null;
                }
                this.Close();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void HideUtilBarInBackground()
        {
            try
            {
                HideSecondaryMenuPopups();
                _imHiding = true;
                this.Hide();
                //this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
                uDebugLogAdd("UtilBar has been hidden in the background");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void BringUtilBarBackFromTheVoid()
        {
            try
            {
                this.WindowState = WindowState.Normal;
                _imHiding = false;
                this.Show();
                VerifyBarLocation();
                if (_firstShow)
                {
                    _firstShow = false;
                    InitializeGlobalHotkey();
                }
                uDebugLogAdd("UtilBar has been brought back from the void");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void tLocationWatcher()
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        while (true)
                        {
                            worker.ReportProgress(1);
                            Thread.Sleep(2000);
                        }
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
                        try
                        {
                            VerifyBarLocation();
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyBarLocation()
        {
            try
            {
                if (_imHiding)
                    return;
                if (Director.Main.CurrentDisplay != null)
                {
                    Display chosenDisplay = null;
                    if (_startingUp)
                        MoveUtilBarToPreferredDisplay();
                    else if (dockedDisplay == null)
                    {
                        chosenDisplay = Director.Main.CurrentDisplay.Displays.Find(x => x.PrimaryDisplay == true);
                        dockedDisplay = chosenDisplay;
                    }
                    else
                        chosenDisplay = dockedDisplay;

                    MoveUtilBar(chosenDisplay);
                }
                else
                    uDebugLogAdd("Current display is null when verifying utility bar location", DebugType.FAILURE);
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Failure to move/verify utility bar location: {ex.Message}", DebugType.FAILURE);
            }
        }

        private void MoveUtilBar(Display chosenDisplay)
        {
            try
            {
                System.Drawing.Rectangle desiredLocation = new System.Drawing.Rectangle
                {
                    //desiredLocation.X = primeDisplay.WorkingArea.X + Convert.ToInt32(primeDisplay.WorkingArea.Width * 0.10);
                    X = Convert.ToInt32(((chosenDisplay.WorkingArea.Width / 2) + chosenDisplay.WorkingArea.X) - (grdMain.ActualWidth / 2)),
                    //desiredLocation.Width = primeDisplay.WorkingArea.Width - Convert.ToInt32(primeDisplay.WorkingArea.Width * 0.20);
                    Y = chosenDisplay.WorkingArea.Height - Convert.ToInt32(rectBackground.ActualHeight) + 1
                };
                if ((this.Left != desiredLocation.X) || (this.Top != desiredLocation.Y))
                {
                    this.Left = desiredLocation.X;
                    //this.Width = desiredLocation.Width;
                    this.Top = desiredLocation.Y;
                    Events.TriggerUtilBarMove(desiredLocation.X, desiredLocation.Y);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MoveUtilBarToPreferredDisplay()
        {
            try
            {
                uDebugLogAdd("Starting util bar move to preferred display");
                var prefDisplay = Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile.PreferredDisplay;
                if (prefDisplay != null)
                {
                    uDebugLogAdd("Preferred display isn't null for current display profile, moving to preferred display");
                    MoveUtilBar(prefDisplay);
                }
                else
                    uDebugLogAdd("Preferred display is null for current display profile, skipping");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyDisplayProfileMatch()
        {
            try
            {
                if (Toolbox.settings.DisplayProfileLibrary == null)
                {
                    uDebugLogAdd("DisplayProfileLibrary was null, instantiating a new one");
                    Toolbox.settings.DisplayProfileLibrary = new DisplayProfileLibrary();
                }
                if (Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile == null || Toolbox.settings.DisplayProfileLibrary.DisplayProfiles.Count <= 0)
                {
                    AddDisplayProfileToLibrary(Director.Main.CurrentDisplay, true);
                }
                var displayMatch = DisplayProfile.DoDisplaysMatch(Director.Main.CurrentDisplay, Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile.DisplayArea);
                if (!displayMatch)
                {
                    uDebugLogAdd($"Current Display Profile returned {displayMatch} for matching the existing Current Display Profile");
                    AddDisplayProfileToLibrary(Director.Main.CurrentDisplay);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void AddDisplayProfileToLibrary(CurrentDisplay display, bool setAsCurrentDisplayProfile = false)
        {
            try
            {
                uDebugLogAdd("Starting Display Profile addition to Library");
                var foundDisplay = Toolbox.settings.DisplayProfileLibrary.DisplayProfiles.Find(x => x.DisplayArea == display);
                if (foundDisplay == null)
                {
                    uDebugLogAdd("Was unable to find a matching display, adding new display to Library");
                    DisplayProfile newDisplay = new DisplayProfile() { DisplayArea = display, PreferredDisplay = display.Displays.Find(x => x.PrimaryDisplay == true) };
                    Toolbox.settings.DisplayProfileLibrary.DisplayProfiles.Add(newDisplay);
                    uDebugLogAdd("New display added successfully");
                    if (setAsCurrentDisplayProfile)
                    {
                        Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile = newDisplay;
                        uDebugLogAdd($"SetAsCurrent is {setAsCurrentDisplayProfile}, Set new display as current");
                    }
                }
                else
                {
                    uDebugLogAdd("Display additon already matches one in the library, cancelling Libary addition");
                }
                uDebugLogAdd("Finished Display Profile Library Addition");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void CopyToClipboard(string clip, string optionalMessage = null)
        {
            try
            {
                Clipboard.SetText(clip);
                if (optionalMessage == null)
                    ShowNotification($"Clipboard Set: {clip}");
                else
                    ShowNotification(optionalMessage);
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Error occured when setting clipboard: {clip} | {ex.Message}", DebugType.FAILURE);
            }
        }

        private void ToggleMenuPopup(PopupMenu menu)
        {
            try
            {
                uDebugLogAdd($"Toggling Popup Menu {menu.ToString()}");
                switch (menu)
                {
                    case PopupMenu.Network:
                        // Network Menu
                        if (popupNetwork == null)
                        {
                            uDebugLogAdd("Network Popup is null, Creating new Network popup");
                            CreatePopup(menu);
                        }
                        else if (popupNetwork != null)
                        {
                            if (popupNetwork.Opacity == 0)
                                popupNetwork.PopupShow();
                            else if (popupNetwork.Opacity == 1.0)
                                popupNetwork.PopupHide();
                        }
                        break;
                    case PopupMenu.Settings:
                        // Settings Menu
                        if (popupSettings == null)
                        {
                            uDebugLogAdd("Settings Popup is null, Creating new Settings popup");
                            CreatePopup(menu);
                        }
                        else if (popupSettings != null)
                        {
                            if (popupSettings.Opacity == 0)
                                popupSettings.PopupShow();
                            else if (popupSettings.Opacity == 1.0)
                                popupSettings.PopupHide();
                        }
                        break;
                    case PopupMenu.Audio:
                        // Audio Menu
                        if (popupAudio == null)
                        {
                            uDebugLogAdd("Audio Popup is null, Creating new Audio popup");
                            CreatePopup(menu);
                        }
                        else if (popupAudio != null)
                        {
                            if (popupAudio.Opacity == 0)
                                popupAudio.PopupShow();
                            else if (popupAudio.Opacity == 1.0)
                                popupAudio.PopupHide();
                        }
                        break;
                    case PopupMenu.Emotes:
                        // Emote Menu
                        if (popupEmote == null)
                        {
                            uDebugLogAdd("Emote Popup is null, Creating new Emote popup");
                            CreatePopup(menu);
                        }
                        else if (popupEmote != null)
                        {
                            if (popupEmote.Opacity == 0)
                                popupEmote.PopupShow();
                            else if (popupEmote.Opacity == 1.0)
                                popupEmote.PopupHide();
                        }
                        break;
                    case PopupMenu.Windows:
                        // Window Profile Menu
                        if (popupWindows == null)
                        {
                            uDebugLogAdd("Windows Popup is null, Creating new Windows popup");
                            CreatePopup(menu);
                        }
                        else if (popupWindows != null)
                        {
                            if (popupWindows.Opacity == 0)
                                popupWindows.PopupShow();
                            else if (popupWindows.Opacity == 1.0)
                                popupWindows.PopupHide();
                        }
                        break;
                }

                // All other menus
                HideSecondaryMenuPopups(menu);
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Popup Menu Toggle Failure: {ex.Message}", DebugType.FAILURE);
            }
        }

        private void CreatePopup(PopupMenu menu)
        {
            try
            {
                uDebugLogAdd($"Initializing new popup: {menu.ToString()}");
                switch (menu)
                {
                    case PopupMenu.Network:
                        popupNetwork = new NetworkPopup();
                        popupNetwork.Show();
                        uDebugLogAdd($"Initialized new Network Popup");
                        break;
                    case PopupMenu.Settings:
                        popupSettings = new SettingsPopup();
                        if (Director.Main.UpdateAvailable)
                            popupSettings.TriggerUpdateAvailable();
                        if (_betaVersion)
                            popupSettings.TriggerBetaReleaseUI();
                        popupSettings.Show();
                        uDebugLogAdd("Initialized new Settings Popup");
                        break;
                    case PopupMenu.Audio:
                        popupAudio = new AudioPopup();
                        popupAudio.Show();
                        uDebugLogAdd("Initialized new Audio Popup");
                        break;
                    case PopupMenu.Emotes:
                        popupEmote = new EmotePopup();
                        popupEmote.Show();
                        uDebugLogAdd("Initialized new Emote Popup");
                        break;
                    case PopupMenu.Windows:
                        popupWindows = new WindowPopup();
                        popupWindows.Show();
                        uDebugLogAdd("Initialized new Windows Popup");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void HideSecondaryMenuPopups(PopupMenu primary = PopupMenu.None)
        {
            try
            {
                var audioPopped = popupAudio != null ? popupAudio.PoppedOut : true;
                var networkPopped = popupNetwork != null ? popupNetwork.PoppedOut : true;
                var settingsPopped = popupSettings != null ? popupSettings.PoppedOut : true;
                var emotePopped = popupEmote != null ? popupEmote.PoppedOut : true;
                var windowsPopped = popupWindows != null ? popupWindows.PoppedOut : true;
                uDebugLogAdd($"Hide Secondary Popups: Prime[{primary.ToString()}] Audio[{audioPopped}] Net[{networkPopped}] Sett[{settingsPopped}] Emote[{emotePopped}] Win[{windowsPopped}]");
                switch (primary)
                {
                    case PopupMenu.Network:
                        if (!audioPopped)
                            popupAudio.PopupHide();
                        if (!emotePopped)
                            popupEmote.PopupHide();
                        if (!windowsPopped)
                            popupWindows.PopupHide();
                        if (!settingsPopped)
                            popupSettings.PopupHide();
                        break;
                    case PopupMenu.Settings:
                        if (!audioPopped)
                            popupAudio.PopupHide();
                        if (!emotePopped)
                            popupEmote.PopupHide();
                        if (!windowsPopped)
                            popupWindows.PopupHide();
                        if (!networkPopped)
                            popupNetwork.PopupHide();
                        break;
                    case PopupMenu.Audio:
                        if (!settingsPopped)
                            popupSettings.PopupHide();
                        if (!emotePopped)
                            popupEmote.PopupHide();
                        if (!windowsPopped)
                            popupWindows.PopupHide();
                        if (!networkPopped)
                            popupNetwork.PopupHide();
                        break;
                    case PopupMenu.Emotes:
                        if (!settingsPopped)
                            popupSettings.PopupHide();
                        if (!audioPopped)
                            popupAudio.PopupHide();
                        if (!windowsPopped)
                            popupWindows.PopupHide();
                        if (!networkPopped)
                            popupNetwork.PopupHide();
                        break;
                    case PopupMenu.Windows:
                        if (!settingsPopped)
                            popupSettings.PopupHide();
                        if (!audioPopped)
                            popupAudio.PopupHide();
                        if (!emotePopped)
                            popupEmote.PopupHide();
                        if (!networkPopped)
                            popupNetwork.PopupHide();
                        break;
                    case PopupMenu.None:
                        if (!settingsPopped)
                            popupSettings.PopupHide();
                        if (!audioPopped)
                            popupAudio.PopupHide();
                        if (!emotePopped)
                            popupEmote.PopupHide();
                        if (!networkPopped)
                            popupNetwork.PopupHide();
                        if (!windowsPopped)
                            popupWindows.PopupHide();
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CopyEmoteToClipbord()
        {
            try
            {
                string emoteString = string.Empty;
                switch (Toolbox.settings.CurrentEmote)
                {
                    case Emotes.Shrug:
                        emoteString = @"¯\_(ツ)_/¯";
                        break;
                }
                CopyToClipboard(emoteString);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void ShowNotification(string notification)
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker()
                {
                    WorkerReportsProgress = true
                };
                worker.ProgressChanged += (sender, e) =>
                {
                    ToggleNotification(e.ProgressPercentage);
                };
                worker.DoWork += (sender, e) =>
                {
                    try
                    {
                        uDebugLogAdd($"Adding Notification: {notification} [ms: {9 * notification.Length}]");
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { notifications.Add(notification); _totalNotificationsRun++; } catch (Exception ex) { LogException(ex); } });
                        if (_notificationPlaying) { uDebugLogAdd("Notification is currently playing, returning"); return; }
                        _notificationPlaying = true;
                        uDebugLogAdd("Notification wasn't playing, starting notification play cycles");
                        int notificationCount = 0;
                        while (notifications.Count != 0)
                        {
                            foreach (var message in notifications.ToList())
                            {
                                notificationCount++;
                                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lblNotification.Text = message; lblNotificationCount.Text = $"{notificationCount}\\{_totalNotificationsRun}"; } catch (Exception ex) { LogException(ex); } });
                                worker.ReportProgress(1);
                                //Thread.Sleep(TimeSpan.FromMilliseconds(95 * notification.Replace(" ", "").Length));
                                Thread.Sleep(TimeSpan.FromSeconds(2));
                                worker.ReportProgress(2);
                                notifications.Remove(message);
                                uDebugLogAdd(string.Format("Removed notification: {0}", message));
                                uDebugLogAdd(string.Format("Notifications left: {0}", notifications.Count));
                            }
                        }
                        _notificationPlaying = false;
                        _totalNotificationsRun = 0;
                        uDebugLogAdd("Finished playing all notifications");
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

        private void ToggleNotification(int state)
        {
            try
            {
                ThicknessAnimation slideIn = new ThicknessAnimation { AccelerationRatio = .5, Duration = new Duration(TimeSpan.FromSeconds(0.2)), To = new Thickness(534, 0, 30, -30) };
                ThicknessAnimation slideOut = new ThicknessAnimation { AccelerationRatio = .5, Duration = new Duration(TimeSpan.FromSeconds(0.2)), To = new Thickness(534, 30, 30, -30) };
                switch (state)
                {
                    case 1:
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { grdNotification.BeginAnimation(FrameworkElement.MarginProperty, slideIn); } catch (Exception ex) { LogException(ex); } });
                        break;
                    case 2:
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { grdNotification.BeginAnimation(FrameworkElement.MarginProperty, slideOut); } catch (Exception ex) { LogException(ex); } });
                        break;
                    default:
                        uDebugLogAdd("Something happened and the incorrect notification state was used, accepted states: 1 or 2");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SavePopoutPreferences()
        {
            try
            {
                uDebugLogAdd("Saving popout preferences");
                if (popupNetwork != null)
                {
                    uDebugLogAdd($"Popout isn't null: {PopupMenu.Network}");
                    if (Toolbox.settings.PopoutPreferencesList.Find(x => x.PopupType == PopupMenu.Network) != null)
                    {
                        UpdatePopoutPreferenceEntry(PopupMenu.Network);
                    }
                    else
                    {
                        Toolbox.settings.PopoutPreferencesList.Add(new PopoutPreferences()
                        {
                            PopupType = PopupMenu.Network,
                            PoppedOut = popupNetwork.PoppedOut,
                            PreferredLocation = new System.Drawing.Rectangle(Convert.ToInt32(popupNetwork.Left), Convert.ToInt32(popupNetwork.Top), Convert.ToInt32(popupNetwork.ActualWidth), Convert.ToInt32(popupNetwork.ActualHeight))
                        });
                        uDebugLogAdd($"Created new popout preferences entry for one that didn't previously exist: [type]{PopupMenu.Network}  [po]{popupNetwork.PoppedOut}  [left]{popupNetwork.Left}  [top]{popupNetwork.Top}  [w]{popupNetwork.ActualWidth}  [h]{popupNetwork.ActualHeight}");
                    }
                }
                if (popupAudio != null)
                {
                    uDebugLogAdd($"Popout isn't null: {PopupMenu.Audio}");
                    if (Toolbox.settings.PopoutPreferencesList.Find(x => x.PopupType == PopupMenu.Audio) != null)
                    {
                        UpdatePopoutPreferenceEntry(PopupMenu.Audio);
                    }
                    else
                    {
                        Toolbox.settings.PopoutPreferencesList.Add(new PopoutPreferences()
                        {
                            PopupType = PopupMenu.Audio,
                            PoppedOut = popupAudio.PoppedOut,
                            PreferredLocation = new System.Drawing.Rectangle(Convert.ToInt32(popupAudio.Left), Convert.ToInt32(popupAudio.Top), Convert.ToInt32(popupAudio.ActualWidth), Convert.ToInt32(popupAudio.ActualHeight))
                        });
                        uDebugLogAdd($"Created new popout preferences entry for one that didn't previously exist: [type]{PopupMenu.Audio}  [po]{popupAudio.PoppedOut}  [left]{popupAudio.Left}  [top]{popupAudio.Top}  [w]{popupAudio.ActualWidth}  [h]{popupAudio.ActualHeight}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdatePopoutPreferenceEntry(PopupMenu menu)
        {
            try
            {
                uDebugLogAdd($"Updating popout preference entry for {menu.ToString()} popout");
                Window winToUpdate = null;
                bool poppedOut = false;
                switch (menu)
                {
                    case PopupMenu.Network:
                        winToUpdate = popupNetwork;
                        poppedOut = popupNetwork.PoppedOut;
                        break;
                    case PopupMenu.Settings:
                        winToUpdate = popupSettings;
                        poppedOut = popupSettings.PoppedOut;
                        break;
                    case PopupMenu.Audio:
                        winToUpdate = popupAudio;
                        poppedOut = popupAudio.PoppedOut;
                        break;
                    case PopupMenu.Emotes:
                        winToUpdate = popupEmote;
                        poppedOut = popupEmote.PoppedOut;
                        break;
                    case PopupMenu.Windows:
                        winToUpdate = popupWindows;
                        poppedOut = popupWindows.PoppedOut;
                        break;
                }
                uDebugLogAdd($"Secured winToUpdate: {winToUpdate.Name}");
                var prefToUpdate = Toolbox.settings.PopoutPreferencesList.Find(x => x.PopupType == menu);
                uDebugLogAdd($"Secured prefToUpdate: {prefToUpdate.PopupType}");
                prefToUpdate.PoppedOut = poppedOut;
                prefToUpdate.PreferredLocation = new System.Drawing.Rectangle(Convert.ToInt32(winToUpdate.Left), Convert.ToInt32(winToUpdate.Top), Convert.ToInt32(winToUpdate.ActualWidth), Convert.ToInt32(winToUpdate.ActualHeight));
                uDebugLogAdd($"Finished updating popout preference: [type]{prefToUpdate.PopupType}  [po]{poppedOut}  [left]{winToUpdate.Left}  [top]{winToUpdate.Top}  [w]{winToUpdate.ActualWidth}  [h]{winToUpdate.ActualHeight}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void MoveUtilBarToOtherDisplay(Key key)
        {
            try
            {
                if (Director.Main.CurrentDisplay.Displays.Count <= 1)
                {
                    uDebugLogAdd($"Current display count is {Director.Main.CurrentDisplay.Displays.Count}, unable to move to another display, will default to primary display");
                    ShowNotification("Only 1 display is visible, can't move util bar");
                    return;
                }
                else
                {
                    uDebugLogAdd($"Starting UtilBar move: [Disp#] {Director.Main.CurrentDisplay.Displays.Count} [Key] {key.ToString()}");
                }
                if (key == Key.Left)
                { 
                    var currDisplayNum = Director.Main.CurrentDisplay.Displays.FindIndex(x => x.DeviceName == dockedDisplay.DeviceName && x.Bounds == dockedDisplay.Bounds && x.WorkingArea == dockedDisplay.WorkingArea && x.PrimaryDisplay == dockedDisplay.PrimaryDisplay);
                    if (currDisplayNum - 1 >= 0)
                    {
                        var chosenDisplay = Director.Main.CurrentDisplay.Displays[currDisplayNum - 1];
                        dockedDisplay = chosenDisplay;
                    }
                    else
                    {
                        var newDisplayNum = Director.Main.CurrentDisplay.Displays.IndexOf(Director.Main.CurrentDisplay.Displays.ToArray().Last());
                        var chosenDisplay = Director.Main.CurrentDisplay.Displays[newDisplayNum];
                        dockedDisplay = chosenDisplay;
                    }
                }
                else if (key == Key.Right)
                {
                    var currDisplayNum = Director.Main.CurrentDisplay.Displays.FindIndex(x => x.DeviceName == dockedDisplay.DeviceName && x.Bounds == dockedDisplay.Bounds && x.WorkingArea == dockedDisplay.WorkingArea && x.PrimaryDisplay == dockedDisplay.PrimaryDisplay);
                    if (currDisplayNum + 1 <= Director.Main.CurrentDisplay.Displays.Count - 1)
                    {
                        var chosenDisplay = Director.Main.CurrentDisplay.Displays[currDisplayNum + 1];
                        dockedDisplay = chosenDisplay;
                    }
                    else
                    {
                        var chosenDisplay = Director.Main.CurrentDisplay.Displays[0];
                        dockedDisplay = chosenDisplay;
                    }
                }
                Toolbox.settings.DisplayProfileLibrary.CurrentDisplayProfile.PreferredDisplay = dockedDisplay;
                MoveUtilBar(dockedDisplay);
                //ShowNotification("Moved UtilityBar Location!");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void TriggerUpdateAvailable()
        {
            try
            {
                btnMenuSettings.Foreground = Toolbox.SolidBrushFromHex("#FF00CD00");
                if (popupSettings != null)
                    popupSettings.TriggerUpdateAvailable();
                ShowNotification("Update Available!");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void TriggerBetaRelease()
        {
            try
            {
                _betaVersion = true;
                if (popupSettings != null)
                    popupSettings.TriggerBetaReleaseUI();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void UpdateSettingsUI()
        {
            try
            {
                if (popupSettings != null)
                {
                    uDebugLogAdd("Settings popup exists, triggering settings Ui update");
                    popupSettings.UpdateUISettings();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void InitializeGlobalHotkey()
        {
            try
            {
                var helper = new INTER.WindowInteropHelper(this);
                _source = INTER.HwndSource.FromHwnd(helper.Handle);
                _source.AddHook(HwndHook);
                RegisterHotKey();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void TearDownGlobalHotkey()
        {
            try
            {
                _source.RemoveHook(HwndHook);
                _source = null;
                UnregisterHotKey();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case WinAPIWrapper.HOTKEY_ID:
                            OnHotKeyPressed();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void RegisterHotKey()
        {
            try
            {
                var helper = new INTER.WindowInteropHelper(this);
                const uint VK_RETURN = 0x0D;
                const uint MOD_CTRL = 0x0002;
                if (!WinAPIWrapper.RegisterHotKey(helper.Handle, WinAPIWrapper.HOTKEY_ID, MOD_CTRL, VK_RETURN))
                {
                    uDebugLogAdd("Failure registering global hotkey", DebugType.FAILURE);
                    ShowNotification("Couldn't register Ctrl+Enter Globally");
                }
                else
                    uDebugLogAdd("Registered Global Hotkey: Ctrl+Enter");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UnregisterHotKey()
        {
            try
            {
                var helper = new INTER.WindowInteropHelper(this);
                WinAPIWrapper.UnregisterHotKey(helper.Handle, WinAPIWrapper.HOTKEY_ID);
                uDebugLogAdd("Unregistered Global Hotkey: Ctrl+Enter");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region Network

        private void NetEnterAction()
        {
            try
            {
                if (popupNetwork == null)
                {
                    uDebugLogAdd("Network popup is null, initializing new network popup");
                    CreatePopup(PopupMenu.Network);
                }
                switch (currentEnterAction)
                {
                    case EnterAction.DNSLookup:
                        popupNetwork.PopupShow();
                        NetNSLookupEntryAdd();
                        break;
                    case EnterAction.Ping:
                        popupNetwork.PopupShow();
                        NetPingEntryAdd();
                        break;
                    case EnterAction.Trace:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void NetToggleEnterAction(EnterAction action = EnterAction.NA)
        {
            try
            {
                uDebugLogAdd($"Current Enter Action being changed, current: {currentEnterAction.ToString()}");
                if (action == EnterAction.NA)
                {
                    if (currentEnterAction == EnterAction.DNSLookup)
                    {
                        currentEnterAction = EnterAction.Ping;
                    }
                    else if (currentEnterAction == EnterAction.Ping)
                    {
                        currentEnterAction = EnterAction.DNSLookup;
                    }
                }
                else
                {
                    currentEnterAction = action;
                }
                uDebugLogAdd($"Current Enter Action Changed to: {currentEnterAction.ToString()}");
                switch (currentEnterAction)
                {
                    case EnterAction.DNSLookup:
                        cirNetNSLookup.Fill = Defaults.NetRadioSelected;
                        lblNetType.Content = "NSLookup";
                        cirNetPing.Fill = Defaults.NetRadioNotSelected;
                        cirNetTrace.Fill = Defaults.NetRadioNotSelected;
                        uDebugLogAdd("Set Enter Action to NSLookup");
                        break;
                    case EnterAction.Ping:
                        cirNetPing.Fill = Defaults.NetRadioSelected;
                        lblNetType.Content = "Ping";
                        cirNetNSLookup.Fill = Defaults.NetRadioNotSelected;
                        cirNetTrace.Fill = Defaults.NetRadioNotSelected;
                        uDebugLogAdd("Set Enter Action to Ping");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void NetPingEntryAdd()
        {
            try
            {
                var address = txtNetMain.Text;
                if (string.IsNullOrWhiteSpace(address))
                {
                    ShowNotification("Nothing was entered, try again");
                    return;
                }
                if (VerifyIfMacAddress(address))
                {
                    uDebugLogAdd($"NetAddress value was found to be a Mac Address, opening Macpopup: {address}");
                    OpenMacAddressWindow(address);
                    return;
                }
                var sendNotif = false;
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                var validEntries = string.Empty;
                foreach (var entry in entries)
                {
                    if (!VerifyInput(entry, EnterAction.Ping) || string.IsNullOrWhiteSpace(entry))
                    {
                        uDebugLogAdd($"Input entered was invalid, sending notification and canceling ping | Input: {entry}");
                        sendNotif = true;
                    }
                    else
                        validEntries = $"{validEntries}{entry},";
                }
                if (validEntries.Length > 0)
                    validEntries = validEntries.Remove(validEntries.Length - 1);
                if (sendNotif && validEntries == string.Empty)
                {
                    ShowNotification("Address(es) entered incorrect or duplicate, try again");
                    return;
                }
                else if (sendNotif && validEntries != string.Empty)
                {
                    ShowNotification("Address(es) entered incorrect or duplicate, added non duplicate(s)");
                    address = validEntries;
                }
                popupNetwork.AddPingEntry(address);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private bool VerifyInput(string input, EnterAction action)
        {
            var isCorrect = true;
            isCorrect = !string.IsNullOrWhiteSpace(input);
            if (action == EnterAction.Ping)
                isCorrect = !popupNetwork.DoesPingSessionExist(input);
            uDebugLogAdd($"Verified input, answer: {isCorrect} | input: {input}");
            return isCorrect;
        }

        private void OpenMacAddressWindow(string v = null)
        {
            try
            {
                MacPopup macPopup = new MacPopup(v);
                macPopup.Show();
                uDebugLogAdd("Opened MacPopup window");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void NetNSLookupEntryAdd()
        {
            try
            {
                var address = txtNetMain.Text;
                if (string.IsNullOrWhiteSpace(address))
                {
                    popupNetwork.lbNetNSLookup.Items.Clear();
                    return;
                }
                if (VerifyIfMacAddress(address))
                {
                    uDebugLogAdd($"NetAddress value was found to be a Mac Address, opening Macpopup: {address}");
                    OpenMacAddressWindow(address);
                    return;
                }
                if (!VerifyInput(address, EnterAction.DNSLookup))
                {
                    uDebugLogAdd($"Input entered was invalid, sending notification and canceling dns lookup | Input: {address}");
                    ShowNotification("Address(es) entered incorrect or duplicate, try again");
                    return;
                }
                if (popupNetwork.resolvingDNS)
                {
                    uDebugLogAdd($"resolvingDNS is {popupNetwork.resolvingDNS}, cancelling NsLookup");
                    ShowNotification("A DNS Lookup is in progress already");
                }
                else
                    popupNetwork.AddNSLookupEntry(address);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void tNetworkConnectivityMonitor()
        {
            try
            {
                tUpdateConnectedEthernet();
                tUpdateConnectedSSIDs();
                tUpdateNetworkConnectivity();
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    while (true)
                    {
                        try
                        {
                            worker.ReportProgress(1);
                            Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    if (pe.ProgressPercentage == 1)
                    {
                        try
                        {
                            UpdateNetworkLinkUI();
                            UpdateNetworkConnectivityUI();
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateNetworkConnectivityUI()
        {
            try
            {
                lblConnectivityStatus.Content = currentConnectivityStatus.ToString();
                switch (currentConnectivityStatus)
                {
                    case ConnectivityStatus.Internet:
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 39, 216, 0));
                        break;
                    case ConnectivityStatus.Layer4:
                        lblConnectivityStatus.Content = "Internet (L4)";
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 39, 216, 0));
                        break;
                    case ConnectivityStatus.Layer3:
                        lblConnectivityStatus.Content = "Internet (L3)";
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 39, 216, 0));
                        break;
                    case ConnectivityStatus.Local:
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 207, 228, 0));
                        break;
                    case ConnectivityStatus.None:
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
                        break;
                    case ConnectivityStatus.DNSError:
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 223, 0, 224));
                        break;
                    case ConnectivityStatus.DHCPError:
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 223, 0, 224));
                        break;
                }
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"ConnectivityUI update failure: {ex.Message}", DebugType.FAILURE);
            }
        }

        private void tUpdateNetworkConnectivity()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                Ping p = new Ping();
                int prevSuccess = -1;
                int prevFailure = -1;
                while (true)
                {
                    try
                    {
                        int successCounter = 0;
                        int failureCounter = 0;
                        var timeout = TimeSpan.FromSeconds(1);

                        /// Layer 4 connectivity verification
                        // Google https
                        if (successCounter <= 0)
                        {
                            try
                            {
                                var httpsGoogle = Network.IsPortOpen("google.com", 443, timeout);
                                if (httpsGoogle)
                                {
                                    successCounter++;
                                    currentConnectivityStatus = ConnectivityStatus.Internet;
                                }
                                else
                                    failureCounter++;
                            }
                            catch (Exception ex)
                            {
                                LogException(ex);
                            }
                        }
                        // Microsoft https
                        if (successCounter <= 0)
                        {
                            try
                            {
                                var httpsGoogle = Network.IsPortOpen("microsoft.com", 443, timeout);
                                if (httpsGoogle)
                                {
                                    successCounter++;
                                    currentConnectivityStatus = ConnectivityStatus.Internet;
                                }
                                else
                                    failureCounter++;
                            }
                            catch (Exception ex)
                            {
                                LogException(ex);
                            }
                        }
                        // Apple https
                        if (successCounter <= 0)
                        {
                            try
                            {
                                var httpsGoogle = Network.IsPortOpen("apple.com", 443, timeout);
                                if (httpsGoogle)
                                {
                                    successCounter++;
                                    currentConnectivityStatus = ConnectivityStatus.Internet;
                                }
                                else
                                    failureCounter++;
                            }
                            catch (Exception ex)
                            {
                                LogException(ex);
                            }
                        }

                        /// Layer 3 connectivity verification
                        // Google dns A
                        if (successCounter <= 0)
                        {
                            try
                            {
                                var googleA = p.Send("8.8.8.8", 100);
                                if (googleA.Status == IPStatus.Success)
                                {
                                    successCounter++;
                                    currentConnectivityStatus = ConnectivityStatus.Layer3;
                                }
                                else
                                    failureCounter++;
                            }
                            catch (Exception)
                            {
                                failureCounter++;
                            }
                        }
                        // Google dns B
                        if (successCounter <= 0)
                        {
                            try
                            {
                                var googleB = p.Send("8.8.4.4", 100);
                                if (googleB.Status == IPStatus.Success)
                                {
                                    successCounter++;
                                    currentConnectivityStatus = ConnectivityStatus.Layer3;
                                }
                                else
                                    failureCounter++;
                            }
                            catch (Exception)
                            {
                                failureCounter++;
                            }
                        }
                        // Cloudflare dns 1
                        if (successCounter <= 0)
                        {
                            try
                            {
                                var cloudFlare = p.Send("1.1.1.1", 100);
                                if (cloudFlare.RoundtripTime > 5)
                                {
                                    if (cloudFlare.Status == IPStatus.Success)
                                    {
                                        successCounter++;
                                        currentConnectivityStatus = ConnectivityStatus.Layer3;
                                    }
                                    else
                                        failureCounter++;
                                }
                            }
                            catch (Exception) { }
                        }
                        // Open DNS/Cisco Umbrella 1
                        if (successCounter <= 0)
                        {
                            try
                            {
                                var oDNS = p.Send("208.67.222.222", 100);
                                if (oDNS.Status == IPStatus.Success)
                                {
                                    successCounter++;
                                    currentConnectivityStatus = ConnectivityStatus.Layer3;
                                }
                                else
                                    failureCounter++;
                            }
                            catch (Exception) { }
                        }
                        // Telstra 1
                        if (successCounter <= 0)
                        {
                            try
                            {
                                var telstra = p.Send("139.130.4.5", 100);
                                if (telstra.Status == IPStatus.Success)
                                {
                                    successCounter++;
                                    currentConnectivityStatus = ConnectivityStatus.Layer3;
                                }
                                else
                                    failureCounter++;
                            }
                            catch (Exception) { }
                        }

                        /// Local connectivity verification
                        if (successCounter <= 0)
                        {
                            var defGateway = string.Empty;
                            if (_connectedToWifi)
                            {
                                try { defGateway = CurrentWifiInterface.NetworkInterface.GetIPProperties().GatewayAddresses[0].Address.ToString(); }
                                catch (Exception) { }
                            }
                            else if (_connectedToEth)
                            {
                                try { defGateway = _currentEthIf.GetIPProperties().GatewayAddresses[0].Address.ToString(); }
                                catch (Exception) { }
                            }

                            if (String.IsNullOrWhiteSpace(defGateway))
                            {
                                failureCounter++;
                                currentConnectivityStatus = ConnectivityStatus.None;
                            }
                            else
                            {
                                try
                                {
                                    var localGatewayRep = p.Send(defGateway, 50);
                                    if (localGatewayRep.Status == IPStatus.Success)
                                    {
                                        successCounter++;
                                        currentConnectivityStatus = ConnectivityStatus.Local;
                                    }
                                    else
                                    {
                                        failureCounter++;
                                        currentConnectivityStatus = ConnectivityStatus.None;
                                    }
                                }
                                catch (Exception)
                                {
                                    failureCounter++;
                                    currentConnectivityStatus = ConnectivityStatus.None;
                                }
                            }
                        }

                        /// Log findings
                        if ((prevSuccess != successCounter) && (prevFailure != failureCounter))
                            uDebugLogAdd($"Success: {successCounter} | Failure: {failureCounter} | CurrConn: {currentConnectivityStatus.ToString()}");
                        prevSuccess = successCounter;
                        prevFailure = failureCounter;
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                    Thread.Sleep(1000);
                }
            };
            worker.RunWorkerAsync();
        }

        private void tUpdateConnectedEthernet()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                while (true)
                {
                    try
                    {
                        if (netIfs == null)
                            netIfs = new List<NetworkInterface>();
                        NetworkInterface tempEthIf = _currentEthIf;
                        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                        foreach (var adapter in interfaces.ToList().FindAll(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet || x.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx || x.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT || x.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit))
                        {
                            netIfs.Add(adapter);
                        }
                        if (_connectedEthIfs.Count > 0)
                            _currentEthIf = _connectedEthIfs[0];
                        if (_currentEthIf != null)
                            if (tempEthIf != _currentEthIf)
                            {
                                uDebugLogAdd($"Connected eth ifs found: {_connectedEthIfs.Count}");
                                uDebugLogAdd($"Primary connected eth if: {_currentEthIf.Name}");
                            }
                    }
                    catch (Win32Exception) { }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                    Thread.Sleep(10000);
                }
            };
            worker.RunWorkerAsync();
        }

        private void tUpdateConnectedSSIDs()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                uint? wlanSignalQuality = null;
                while (true)
                {
                    try
                    {
                        if (wClient == null)
                        {
                            wClient = new WlanClient();
                            uDebugLogAdd("WlanClient was null, initialized a new one");
                        }
                        _connectedSSIDs.Clear();
                        foreach (var wlanIf in wClient.Interfaces)
                        {
                            Wlan.Dot11Ssid ssid = wlanIf.CurrentConnection.wlanAssociationAttributes.dot11Ssid;
                            _connectedSSIDs.Add(new String(Encoding.ASCII.GetChars(ssid.SSID, 0, (int)ssid.SSIDLength)));
                            if (CurrentWifiInterface != null)
                                if (CurrentWifiInterface.CurrentConnection.wlanAssociationAttributes.Dot11Bssid.ToString() != wlanIf.CurrentConnection.wlanAssociationAttributes.Dot11Bssid.ToString())
                                    _bssidChanged = true;
                            CurrentWifiInterface = wlanIf;
                            if (wlanSignalQuality != null)
                                if (wlanSignalQuality != CurrentWifiInterface.CurrentConnection.wlanAssociationAttributes.wlanSignalQuality)
                                    uDebugLogAdd($"Wlan Signal quality changed: [B] {wlanSignalQuality} [A] {CurrentWifiInterface.CurrentConnection.wlanAssociationAttributes.wlanSignalQuality}");
                            wlanSignalQuality = CurrentWifiInterface.CurrentConnection.wlanAssociationAttributes.wlanSignalQuality;
                        }
                        if (_bssidChanged)
                        {
                            uDebugLogAdd($"Current Connected SSID's: {_connectedSSIDs.Count}");
                            foreach (var ssid in _connectedSSIDs)
                            {
                                uDebugLogAdd($"Connected SSID: {ssid}");
                            }
                        }
                    }
                    catch (Win32Exception) { }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                    Thread.Sleep(3000);
                }
            };
            worker.RunWorkerAsync();
        }

        private void UpdateNetworkLinkUI()
        {
            try
            {
                if (_connectedToEth && _connectedToWifi)
                {
                    if (currentConnType != NetConnectionType.EthWifi)
                        ToggleConnectionType(NetConnectionType.EthWifi);
                    var wifiLinkSpeed = GetWifiLinkSpeed();
                    currentLinkSpeed = wifiLinkSpeed;
                    if (wifiLinkSpeed != null)
                    {
                        UpdateLinkSpeed(wifiLinkSpeed, _currentEthIf.Speed);
                    }
                    else
                    {
                        UpdateLinkSpeed(_currentEthIf.Speed);
                    }
                    UpdateWifiStats();
                }
                else if (_connectedToWifi)
                {
                    if (currentConnType != NetConnectionType.Wireless)
                        ToggleConnectionType(NetConnectionType.Wireless);
                    var wifiLinkSpeed = GetWifiLinkSpeed();
                    if (wifiLinkSpeed != null)
                    {
                        UpdateLinkSpeed(wifiLinkSpeed);
                    }
                    else
                    {
                        UpdateLinkSpeed(null);
                    }
                    UpdateWifiStats();
                }
                else if (_connectedToEth)
                {
                    if (currentConnType != NetConnectionType.Wired)
                        ToggleConnectionType(NetConnectionType.Wired);
                    UpdateLinkSpeed(_currentEthIf.Speed);
                    UpdateWifiStats();
                }
                else if (currentConnType == NetConnectionType.EthWifi || currentConnType == NetConnectionType.Wired || currentConnType == NetConnectionType.Wireless) { }
                else
                {
                    ToggleConnectionType(NetConnectionType.NoConnection);
                    UpdateLinkSpeed(0);
                    UpdateWifiStats();
                }
            }
            catch (Exception ex)
            {
                ToggleConnectionType(NetConnectionType.Unknown);
                uDebugLogAdd($"Network link failure: {ex.Message}", DebugType.FAILURE);
            }
        }

        private void UpdateWifiStats()
        {
            try
            {
                UpdateWlanRSSI();
                UpdateWlanPHYType();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateWlanPHYType()
        {
            try
            {
                if ((currentConnType == NetConnectionType.Wireless || currentConnType == NetConnectionType.EthWifi) && _bssidChanged)
                {
                    _bssidChanged = false;

                    Process p = new Process();
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.Arguments = $"/c netsh wlan show networks interface=\"{CurrentWifiInterface.InterfaceName}\" mode=Bssid";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    Regex rbssid = new Regex($@"({MacPopup.ConvertMacAddrCol(CurrentWifiInterface.CurrentConnection.wlanAssociationAttributes.Dot11Bssid.ToString()).ToLower()})([\s\S]*?)Basic rates");
                    Regex rphyProto = new Regex(@"Radio.*");
                    var bssidOut = rbssid.Match(output);
                    var phyProtoOut = Regex.Replace(rphyProto.Match(bssidOut.Value).Value.Replace("Radio type", "").Replace(":", ""), @"\s+", "");

                    currentFrequency = GetCurrentWifiFrequency();
                    currentPHYProto = GetCurrentWifiPHYProtocol(phyProtoOut);

                    UpdateWifiConnType();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateWifiConnType()
        {
            try
            {
                if (currentConnType == NetConnectionType.Wireless || currentConnType == NetConnectionType.EthWifi)
                {
                    var phystring = string.Empty;
                    switch (currentFrequency)
                    {
                        case WifiFrequency.RF5G:
                            phystring = "5";
                            break;
                        case WifiFrequency.RF24G:
                            phystring = "2.4";
                            break;
                        case WifiFrequency.None:
                            phystring = "???";
                            break;
                    }
                    phystring = $"{phystring}{currentPHYProto.ToString()}";
                    if (lblLinkType.Content.ToString().Contains('(') && (!lblLinkType.Content.ToString().Contains(phystring)))
                    {
                        lblLinkType.Content = Regex.Replace(lblLinkType.Content.ToString(), @".(\(.*\))", "");
                        lblLinkType.Content = $"{lblLinkType.Content.ToString()} ({phystring})";
                    }
                    else if (!lblLinkType.Content.ToString().Contains('('))
                        lblLinkType.Content = $"{lblLinkType.Content.ToString()} ({phystring})";
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private WifiPHYProto GetCurrentWifiPHYProtocol(string phy)
        {
            WifiPHYProto proto = WifiPHYProto.None;
            switch (phy)
            {
                case "802.11n":
                    proto = WifiPHYProto.n;
                    break;
                case "802.11ac":
                    proto = WifiPHYProto.ac;
                    break;
                case "802.11a":
                    proto = WifiPHYProto.a;
                    break;
                case "802.11b":
                    proto = WifiPHYProto.b;
                    break;
                case "802.11g":
                    proto = WifiPHYProto.g;
                    break;
                default:
                    proto = WifiPHYProto.None;
                    break;
            }
            return proto;
        }

        private WifiFrequency GetCurrentWifiFrequency()
        {
            WifiFrequency freq = WifiFrequency.None;
            if (CurrentWifiInterface.Channel > 14)
                freq = WifiFrequency.RF5G;
            else
                freq = WifiFrequency.RF24G;
            return freq;
        }

        /// <summary>
        /// Updates the Link Speed label w/ the provided linkspeed value (double)
        /// </summary>
        /// <param name="linkSpeed">Default value, if Both Eth and Wifi are connected this parameter will be used for the Wifi linkspeed</param>
        /// <param name="linkSpeed2">Used if both Eth and Wifi are connected, this parameter will then be used for the Eth linkspeed</param>
        private void UpdateLinkSpeed(double? linkSpeed, double? linkSpeed2 = null)
        {
            try
            {
                LinkSpeedNotation speedNotation = GetSpeedNotation(linkSpeed);
                string speedString = GetSpeedString(linkSpeed, speedNotation);
                if (linkSpeed2 != null)
                {
                    speedString = $"[Wifi] {speedString}";
                    //if (linkSpeed > linkSpeed2)
                    //{
                    //    speedString = $"[Wifi] {speedString}";
                    //}
                    //else
                    //{
                    //    speedNotation = GetSpeedNotation(linkSpeed2);
                    //    speedString = $"[Eth] {GetSpeedString(linkSpeed2, speedNotation)}";
                    //}
                }
                lblLinkSpeed.Content = speedString;
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Failure updating link speed: {ex.Message}", DebugType.FAILURE);
            }
        }

        private string GetSpeedString(double? linkSpeed, LinkSpeedNotation speedNotation)
        {
            string speed = string.Empty;
            switch (speedNotation)
            {
                case LinkSpeedNotation.Tbps:
                    speed = $"{linkSpeed / 1000000000000} {speedNotation.ToString()}";
                    break;
                case LinkSpeedNotation.Gbps:
                    speed = $"{linkSpeed / 1000000000} {speedNotation.ToString()}";
                    break;
                case LinkSpeedNotation.Mbps:
                    speed = $"{linkSpeed / 1000000} {speedNotation.ToString()}";
                    break;
                case LinkSpeedNotation.Kbps:
                    speed = $"{linkSpeed / 1000} {speedNotation.ToString()}";
                    break;
                case LinkSpeedNotation.Bps:
                    speed = $"{linkSpeed} {speedNotation.ToString()}";
                    break;
            }
            return speed;
        }

        private LinkSpeedNotation GetSpeedNotation(double? linkSpeed)
        {
            var speedNotation = LinkSpeedNotation.Bps;
            if (linkSpeed > 999999999999)
                speedNotation = LinkSpeedNotation.Tbps;
            else if (linkSpeed > 999999999)
                speedNotation = LinkSpeedNotation.Gbps;
            else if (linkSpeed > 999999)
                speedNotation = LinkSpeedNotation.Mbps;
            else if (linkSpeed > 999)
                speedNotation = LinkSpeedNotation.Kbps;
            return speedNotation;
        }

        private void UpdateWlanRSSI()
        {
            try
            {

                if (!(currentConnType == NetConnectionType.EthWifi || currentConnType == NetConnectionType.Wireless))
                    lblWlanRSSI.Content = "";
                else
                {
                    var signalQuality = CurrentWifiInterface.CurrentConnection.wlanAssociationAttributes.wlanSignalQuality;
                    var rssi = string.Empty;
                    double half = signalQuality / 2;
                    double dBm = half - 100;
                    if (signalQuality <= 0)
                        rssi = "-100 dBm";
                    else if (signalQuality >= 100)
                        rssi = "< -50 dBm";
                    else
                        rssi = $"{dBm} dBm";
                    lblWlanRSSI.Content = rssi;
                }
            }
            catch (Exception)
            {
                lblWlanRSSI.Content = "???? dBm";
            }
        }

        private void ToggleConnectionType(NetConnectionType connType)
        {
            try
            {
                currentConnType = connType;
                switch (connType)
                {
                    case NetConnectionType.Wired:
                        lblLinkType.Content = connType.ToString();
                        break;
                    case NetConnectionType.Wireless:
                        lblLinkType.Content = connType.ToString();
                        break;
                    case NetConnectionType.NoConnection:
                        lblLinkType.Content = "None";
                        break;
                    case NetConnectionType.Unknown:
                        lblLinkType.Content = connType.ToString();
                        break;
                    case NetConnectionType.EthWifi:
                        lblLinkType.Content = "Eth & Wifi";
                        break;
                }
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Failure updating link type: {ex.Message}", DebugType.FAILURE);
            }
        }

        private double? GetWifiLinkSpeed()
        {
            ulong speed = 0;
            double? dblSpeed = null;
            string adapter = "";

            try
            {
                string[] nameSearches = { "Wireless", "WiFi", "802.11", "Wi-Fi" };

                // The enum value of `AF_INET` will select only IPv4 adapters.
                // You can change this to `AF_INET6` for IPv6 likewise
                // And `AF_UNSPEC` for either one
                foreach (IPIntertop.IP_ADAPTER_ADDRESSES net in IPIntertop.GetIPAdapters(IPIntertop.FAMILY.AF_INET))
                {
                    bool containsName = false;
                    foreach (string name in nameSearches)
                    {
                        if (net.FriendlyName.Contains(name))
                        {
                            containsName = true;
                        }
                    }
                    if (!containsName) continue;

                    speed = net.TrasmitLinkSpeed;
                    adapter = net.FriendlyName;
                    break;
                }

                if (speed == 0)
                {
                    dblSpeed = null;
                }
                else
                {
                    dblSpeed = speed;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            return dblSpeed;
        }

        private bool VerifyIfMacAddress(string content)
        {
            bool isMac = false;
            if (content.Length == 12 || content.Length == 14 || content.Length == 17)
            {
                if (content.Contains('.') && content.Length == 14)
                {
                    if (content.ToArray().ToList().FindAll(x => x == '.').Count == 2)
                    {
                        isMac = true;
                    }
                }
                else if ((content.Contains('-') || content.Contains(':')) && content.Length == 17)
                {
                    isMac = true;
                }
                else if ((!content.Contains('.')) && (!content.Contains('-')) && (!content.Contains(':')) && (content.Any(char.IsDigit)))
                {
                    isMac = true;
                }
            }
            return isMac;
        }

        #endregion

        #region Windows

        private void MoveProfileWindows()
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
                    ShowNotification("Moved all windows");
                };
                verifyier.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateCurrentWindowProfile()
        {
            try
            {
                uDebugLogAdd($"Current window profile is {Toolbox.settings.CurrentWindowProfile}");
                string profileName = string.Empty;
                switch (Toolbox.settings.CurrentWindowProfile)
                {
                    case WindowProfile.Profile1:
                        profileName = Toolbox.settings.WindowProfileName1;
                        break;
                    case WindowProfile.Profile2:
                        profileName = Toolbox.settings.WindowProfileName2;
                        break;
                    case WindowProfile.Profile3:
                        profileName = Toolbox.settings.WindowProfileName3;
                        break;
                    case WindowProfile.Profile4:
                        profileName = Toolbox.settings.WindowProfileName4;
                        break;
                }
                uDebugLogAdd($"Profile name is: {profileName}");
                btnMenuWindows.Content = profileName;
                uDebugLogAdd("Finished updating current windows profile");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MoveProcessHandle(WindowItem selectedWindow)
        {
            try
            {
                bool moveAll = false;

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
                            }
                            else
                                uDebugLogAdd($"Skipping handle window as it has another place to be | [{detProc.Handle}]{detProc.Name} {detProc.Title}");
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
        }

        #endregion

        #endregion
    }
}
