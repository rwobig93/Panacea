using NativeWifi;
using Panacea.Classes;
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
            DNSError
        }

        public enum PopupMenu
        {
            Network,
            Settings,
            Audio,
            Windows,
            Emotes
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
        private int _totalNotificationsRun = 0;
        private List<string> notifications = new List<string>();
        private NetworkInterface _currentEthIf;
        private CurrentDisplay currentDisplay;
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
        private INTER.HwndSource _source;

        public WlanClient.WlanInterface CurrentWifiInterface { get; private set; }

        #endregion

        #region Event Handlers

        #region Menu

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            CloseUtilBar();
        }

        private void WinMain_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeGlobalHotkey();
        }

        private void WinMain_Closing(object sender, CancelEventArgs e)
        {
            CloseUtilBar();
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

        private void TxtNetMain_KeyDown(object sender, KeyEventArgs e)
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
                Toolbox.uAddDebugLog(_log, _type, caller);
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
            this.Top = -32000;
            this.Left = -32000;
            tLocationWatcher();
        }

        private void Startup()
        {
            tNetworkConnectivityMonitor();
            NetToggleEnterAction(currentEnterAction);
        }

        private void InitializeGlobalHotkey()
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

        private void TearDownGlobalHotkey()
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

        private void OnHotKeyPressed()
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
                TearDownGlobalHotkey();

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
                            RefreshDisplaySizes();
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
                if (currentDisplay != null)
                {
                    var primeScreen = currentDisplay.Screens.Find(x => x.Primary == true);
                    System.Drawing.Rectangle desiredLocation = new System.Drawing.Rectangle
                    {
                        //desiredLocation.X = primeScreen.WorkingArea.X + Convert.ToInt32(primeScreen.WorkingArea.Width * 0.10);
                        X = Convert.ToInt32((primeScreen.WorkingArea.Width / 2) - (grdMain.ActualWidth / 2)),
                        //desiredLocation.Width = primeScreen.WorkingArea.Width - Convert.ToInt32(primeScreen.WorkingArea.Width * 0.20);
                        Y = primeScreen.WorkingArea.Height - Convert.ToInt32(rectBackground.ActualHeight) + 1
                    };
                    if ((this.Left != desiredLocation.X) && (this.Top != desiredLocation.Y))
                    {
                        this.Left = desiredLocation.X;
                        //this.Width = desiredLocation.Width;
                        this.Top = desiredLocation.Y;
                        uDebugLogAdd("Moved utility bar to new location");
                    }
                }
                else
                    uDebugLogAdd("Current display is null when verifying utility bar location", DebugType.FAILURE);
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Failure to move/verify utility bar location: {ex.Message}", DebugType.FAILURE);
            }
        }

        private void RefreshDisplaySizes()
        {
            try
            {
                if (currentDisplay == null)
                {
                    currentDisplay = new CurrentDisplay();
                    uDebugLogAdd("Current display was null, created new current display");
                }
                currentDisplay.Screens.Clear();
                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    currentDisplay.Screens.Add(screen);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CopyToClipboard(string clip)
        {
            try
            {
                Clipboard.SetText(clip);
                ShowNotification($"Clipboard Set: {clip}");
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
                        throw new NotImplementedException();
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
                        throw new NotImplementedException();
                    case PopupMenu.Windows:
                        throw new NotImplementedException();
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
                        throw new NotImplementedException();
                    case PopupMenu.Audio:
                        popupAudio = new AudioPopup();
                        popupAudio.Show();
                        uDebugLogAdd("Initialized new Audio Popup");
                        break;
                    case PopupMenu.Emotes:
                        throw new NotImplementedException();
                    case PopupMenu.Windows:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void HideSecondaryMenuPopups(PopupMenu primary)
        {
            try
            {
                switch (primary)
                {
                    case PopupMenu.Network:
                        if (!popupAudio.PoppedOut)
                            popupAudio.PopupHide();
                        break;
                    case PopupMenu.Settings:
                        break;
                    case PopupMenu.Audio:
                        if (!popupNetwork.PoppedOut)
                        popupNetwork.PopupHide();
                        break;
                    case PopupMenu.Emotes:
                        break;
                    case PopupMenu.Windows:
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
                    case ConnectivityStatus.Local:
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 207, 228, 0));
                        break;
                    case ConnectivityStatus.None:
                        lblConnectivityStatus.Foreground = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
                        break;
                    case ConnectivityStatus.DNSError:
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
                        /// Internet connectivity verification
                        // Google dns A
                        try
                        {
                            var googleA = p.Send("8.8.8.8", 100);
                            if (googleA.Status == IPStatus.Success)
                            {
                                successCounter++;
                                currentConnectivityStatus = ConnectivityStatus.Internet;
                            }
                            else
                                failureCounter++;
                        }
                        catch (Exception)
                        {
                            failureCounter++;
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
                                    currentConnectivityStatus = ConnectivityStatus.Internet;
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
                                        currentConnectivityStatus = ConnectivityStatus.Internet;
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
                                    currentConnectivityStatus = ConnectivityStatus.Internet;
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
                                    currentConnectivityStatus = ConnectivityStatus.Internet;
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
                else if ((!content.Contains('.')) && (!content.Contains('-')) && (!content.Contains(':')))
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
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
