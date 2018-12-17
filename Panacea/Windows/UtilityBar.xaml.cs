using NativeWifi;
using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
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
            InitializeComponent();
            Startup();
        }

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

        #region Globals

        private WlanClient wClient;
        private List<string> _connectedEthIfs = new List<string>();
        private List<string> _connectedSSIDs = new List<string>();
        private bool _connectedToWifi { get { return _connectedSSIDs.Count > 0 ? true : false; } }
        private bool _connectedToEth { get { return _connectedEthIfs.Count > 0 ? true : false; } }
        private WlanClient.WlanInterface _currentWifiIf;
        private NetworkInterface _currentEthIf;
        private CurrentDisplay currentDisplay;

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
                    desiredLocation.Y = primeScreen.WorkingArea.Height - Convert.ToInt32(rectBackground.ActualHeight);
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
                            UpdateConnectedEthernet();
                            UpdateConnectedSSIDs();
                            UpdateNetworkLink();
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

        private void UpdateConnectedEthernet()
        {
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                var tempInterface = _currentEthIf;
                _connectedEthIfs.Clear();
                foreach (var adapter in interfaces)
                {
                    if (adapter.OperationalStatus == OperationalStatus.Up && adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        _connectedEthIfs.Add(adapter.Name);
                        _currentEthIf = adapter;
                    }
                }
                if (_currentEthIf != tempInterface)
                {
                    foreach (var inter in _connectedEthIfs)
                    {
                        uDebugLogAdd($"Eth interface up: {inter}");
                    }
                    uDebugLogAdd($"Total up Eth ifs: {_connectedEthIfs.Count}");
                }
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
                    _currentWifiIf = wlanIf;
                }
                uDebugLogAdd($"Current Connected SSID's: {_connectedSSIDs.Count}");
                foreach (var ssid in _connectedSSIDs)
                {
                    uDebugLogAdd($"Connected SSID: {ssid}");
                }
            }
            catch (Win32Exception) { }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateNetworkLink()
        {
            try
            {
                if (_connectedToEth && _connectedToWifi)
                {
                    ToggleConnectionType(NetConnectionType.EthWifi);
                    var wifiLinkSpeed = GetWifiLinkSpeed();
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
                    ToggleConnectionType(NetConnectionType.Wired);
                    UpdateLinkSpeed(_currentEthIf.Speed);
                    UpdateWlanRSSI(-1337);
                }
                else
                    ToggleConnectionType(NetConnectionType.NoConnection);
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
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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

        private void UpdateWlanRSSI(int rssi = 0)
        {
            try
            {
                if (rssi == -1337)
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
                    uDebugLogAdd($"Not currently connected to wifi via adapter {adapter}");
                    dblSpeed = null;
                }
                else
                {
                    dblSpeed = speed;
                    uDebugLogAdd($"Current Wi-Fi Speed: {dblSpeed} on {adapter}");
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

        #endregion
    }
}
