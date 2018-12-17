using NativeWifi;
using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            Unknown
        }

        #region Globals

        private WlanClient wClient;
        private List<string> _connectedSSIDs = new List<string>();
        private bool _connectedToWifi { get { return _connectedSSIDs.Count > 0 ? true : false; } }
        private CurrentDisplay currentDisplay;

        #endregion

        #region Event Handlers

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
                    desiredLocation.X = primeScreen.WorkingArea.X + Convert.ToInt32(primeScreen.WorkingArea.Width * 0.10);
                    desiredLocation.Width = primeScreen.WorkingArea.Width - Convert.ToInt32(primeScreen.WorkingArea.Width * 0.10);
                    desiredLocation.Y = primeScreen.WorkingArea.Height - Convert.ToInt32(rectBackground.ActualHeight);
                    if ((this.Left != desiredLocation.X) && (this.Width != desiredLocation.Width) && (this.Top != desiredLocation.Y))
                    {
                        this.Left = desiredLocation.X;
                        this.Width = desiredLocation.Width;
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
                if (_connectedToWifi)
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
                }
                else
                {
                    ToggleConnectionType(NetConnectionType.Wired);
                    UpdateLinkSpeed(1000);
                }
                
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Network link failure: {ex.Message}", DebugType.FAILURE);
            }
        }

        private void UpdateLinkSpeed(double? linkSpeed)
        {
            try
            {
                if (linkSpeed != null && linkSpeed < 10000)
                    lblLinkSpeed.Content = $"{linkSpeed} Mbps";
                else if (linkSpeed != null && linkSpeed > 10000)
                {
                    lblLinkSpeed.Content = "Broken Mbps";
                }
                else
                    lblLinkSpeed.Content = "????? Mbps";
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Failure updating link speed: {ex.Message}", DebugType.FAILURE);
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
                    dblSpeed = speed / 1000000.0;
                    uDebugLogAdd($"Current Wi-Fi Speed: {dblSpeed} Mbps on {adapter}");
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
    }
}
