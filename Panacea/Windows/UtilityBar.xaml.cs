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
using static Panacea.MainWindow;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for UtilityBar.xaml
    /// </summary>
    public partial class UtilityBar : Window
    {
        public UtilityBar()
        {
            this.Top = 2560;
            this.Left = 1920;
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

        private enum PopupMenu
        {
            Network,
            Settings
        }

        #endregion

        #region Globals

        private WlanClient wClient;
        private List<NetworkInterface> netIfs;
        private List<NetworkInterface> _connectedEthIfs { get { return netIfs.FindAll(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet && x.OperationalStatus == OperationalStatus.Up); } }
        private List<string> _connectedSSIDs = new List<string>();
        private bool _connectedToWifi { get { return _connectedSSIDs.Count > 0 ? true : false; } }
        private bool _connectedToEth { get { return _connectedEthIfs.Count > 0 ? true : false; } }
        private bool _bssidChanged = true;
        private WlanClient.WlanInterface _currentWifiIf;
        private NetworkInterface _currentEthIf;
        private CurrentDisplay currentDisplay;
        private NetConnectionType currentConnType = NetConnectionType.Unknown;
        private WifiFrequency currentFrequency = WifiFrequency.None;
        private WifiPHYProto currentPHYProto = WifiPHYProto.None;
        private ConnectivityStatus currentConnectivityStatus = ConnectivityStatus.None;
        private double? currentLinkSpeed;
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.7) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.7) };
        private NetworkPopup popupNetwork;

        #endregion

        #region Event Handlers

        private void btnExit_Click(object sender, RoutedEventArgs e)
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

        private void BtnMenuNetwork_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuPopup(PopupMenu.Network);
        }

        #endregion

        #region Methods

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

        private void Startup()
        {
            tLocationWatcher();
            tNetworkConnectivityMonitor();
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
                    System.Drawing.Rectangle desiredLocation = new System.Drawing.Rectangle();
                    //desiredLocation.X = primeScreen.WorkingArea.X + Convert.ToInt32(primeScreen.WorkingArea.Width * 0.10);
                    desiredLocation.X = Convert.ToInt32((primeScreen.WorkingArea.Width / 2) - (grdMain.ActualWidth / 2));
                    //desiredLocation.Width = primeScreen.WorkingArea.Width - Convert.ToInt32(primeScreen.WorkingArea.Width * 0.20);
                    desiredLocation.Y = primeScreen.WorkingArea.Height - Convert.ToInt32(rectBackground.ActualHeight) + 1;
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

        private void tNetworkConnectivityMonitor()
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    while (true)
                    {
                        try
                        {
                            UpdateConnectedEthernet();
                            UpdateConnectedSSIDs();
                            UpdateNetworkConnectivity();
                            worker.ReportProgress(1);
                            Thread.Sleep(2000);
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

        private void UpdateNetworkConnectivity()
        {
            try
            {
                Ping p = new Ping();
                int successCounter = 0;
                int failureCounter = 0;

                /// Internet connectivity verification
                // Google dns A
                try
                {
                    var googleA = p.Send("8.8.8.8");
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
                        var googleB = p.Send("8.8.4.4");
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
                        var cloudFlare = p.Send("1.1.1.1");
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
                        var oDNS = p.Send("208.67.222.222");
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
                        var telstra = p.Send("139.130.4.5");
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
                        try { defGateway = _currentWifiIf.NetworkInterface.GetIPProperties().GatewayAddresses[0].Address.ToString(); }
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
                            var localGatewayRep = p.Send(defGateway);
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
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateConnectedEthernet()
        {
            try
            {
                if (netIfs == null)
                {
                    netIfs = new List<NetworkInterface>();
                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var adapter in interfaces)
                    {
                        netIfs.Add(adapter);
                    }
                }
                if (_connectedEthIfs.Count > 0)
                    _currentEthIf = _connectedEthIfs[0];
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateConnectedSSIDs()
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
                    if (_currentWifiIf != null)
                        if (_currentWifiIf.CurrentConnection.wlanAssociationAttributes.Dot11Bssid.ToString() != wlanIf.CurrentConnection.wlanAssociationAttributes.Dot11Bssid.ToString())
                            _bssidChanged = true;
                    _currentWifiIf = wlanIf;
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
                    p.StartInfo.Arguments = $"/c netsh wlan show networks interface=\"{_currentWifiIf.InterfaceName}\" mode=Bssid";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    Regex rbssid = new Regex($@"({MacPopup.ConvertMacAddrCol(_currentWifiIf.CurrentConnection.wlanAssociationAttributes.Dot11Bssid.ToString()).ToLower()})([\s\S]*?)Basic rates");
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
            if (_currentWifiIf.Channel > 14)
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
                    lblWlanRSSI.Content = $"-{_currentWifiIf.RSSI} dBm";
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

        private void CopyToClipboard(string clip, string optionalMessage = null)
        {
            try
            {
                Clipboard.SetText(clip);
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
                            popupNetwork = new NetworkPopup((this.Top - 300), (this.Left + this.btnMenuNetwork.Margin.Left));
                            popupNetwork.Show();
                            uDebugLogAdd("Network Popup was null, Created new Network popup");
                        }
                        else if (popupNetwork != null)
                        {
                            if (popupNetwork.Opacity == 0)
                                popupNetwork.PopupShow();
                            else if (popupNetwork.Opacity == 1.0)
                                popupNetwork.PopupHide();
                        }

                        // All other menus

                        break;
                    case PopupMenu.Settings:
                        break;
                }
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Popup Menu Toggle Failure: {ex.Message}", DebugType.FAILURE);
            }
        }

        #endregion
    }
}
