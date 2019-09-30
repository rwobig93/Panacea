using NativeWifi;
using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
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
using System.Windows.Shell;

namespace Panacea.Windows.Popups
{
    /// <summary>
    /// Interaction logic for InfoPopup.xaml
    /// </summary>
    public partial class InfoPopup : Window
    {
        public InfoPopup()
        {
            SetDefaultLocation();
            InitializeComponent();
            Startup();
        }

        #region Globals
        private bool startingUp = true;
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.2) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.2) };
        private double PopinLeft { get { return (UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.ActualWidth) - (UtilityBar.UtilBarMain.lblConnectivityStatus.Margin.Right + (UtilityBar.UtilBarMain.lblConnectivityStatus.ActualWidth / 2)); } } // UtilityBar.UtilBarMain.Left + (UtilityBar.UtilBarMain.lblConnectivityStatus.Margin.Left + (UtilityBar.UtilBarMain.lblConnectivityStatus.ActualWidth / 2))
        private double PopinTop { get { return UtilityBar.UtilBarMain.Top - this.ActualHeight; } }
        private double PopinWidth { get { return 405; } }
        private double PopinHeight { get { return 402; } }
        public bool PoppedOut { get; set; } = false;
        #endregion

        #region Event Handlers
        private void WinInfoPopup_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                /// Removed for now as popup isn't meant to be resized
                //if (!startingUp)
                //    VerifyResetButtonRequirement();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void WinInfoPopup_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Director.Main.PopupWindows.Add(this);
                EnableWindowResizing();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void WinInfoPopup_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Director.Main.PopupWindows.Remove(this);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetPopupSizeAndLocation();
        }

        private void BtnPopInOut_Click(object sender, RoutedEventArgs e)
        {
            TogglePopout();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PoppedOut)
                    TogglePopout();
                PopupHide();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RectTitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left && PoppedOut)
                    WinInfoPopup.DragMove();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LabelNetInfo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string labelText = ((Label)sender).Content.ToString();
                Actions.CopyToClipboard(labelText);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
        #endregion

        #region Methods
        private void Startup()
        {
            SetDefaultUI();
            SubscribeToEvents();
            PopupShow();
            FinishStartup();
        }

        private void SetDefaultUI()
        {
            SetDefaultWlanUI();
            SetDefaultEthUI();
            SetDefaultConnectivityUI();
        }

        private void SetDefaultConnectivityUI()
        {
            try
            {
                LabelConnectivityValue.Content = "None";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SetDefaultEthUI()
        {
            try
            {
                LabelEthIPValue.Content = "N/A";
                LabelEthMacValue.Content = "N/A";
                LabelEthSpeedValue.Content = "N/A";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SetDefaultWlanUI()
        {
            try
            {
                LabelWlanIPValue.Content = "N/A";
                LabelWlanMacValue.Content = "N/A";
                LabelWlanSpeedValue.Content = "N/A";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MoveToNewLocation()
        {
            try
            {
                this.Top = PopinTop;
                this.Left = PopinLeft;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void FinishStartup()
        {
            try
            {
                startingUp = false;
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

        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            try
            {
                Toolbox.uAddDebugLog($"POPINFO: {_log}", _type, caller);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void EnableWindowResizing()
        {
            try
            {
                // Allow borderless window to be resized
                WindowChrome.SetWindowChrome(this, new WindowChrome() { ResizeBorderThickness = new Thickness(5), CaptionHeight = .05 });
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SetDefaultLocation()
        {
            try
            {
                this.Top = UtilityBar.UtilBarMain.Top - PopinHeight;
                this.Left = (UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.ActualWidth) - (UtilityBar.UtilBarMain.lblConnectivityStatus.Margin.Right + (UtilityBar.UtilBarMain.lblConnectivityStatus.ActualWidth / 2));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void PopupHide()
        {
            try
            {
                //if (this.Opacity == 1.0)
                //    this.BeginAnimation(Window.OpacityProperty, outAnimation);
                this.Hide();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void PopupShow()
        {
            try
            {
                //if (this.Opacity == 0)
                //    this.BeginAnimation(Window.OpacityProperty, inAnimation);
                this.Show();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ResetPopupSizeAndLocation()
        {
            try
            {
                uDebugLogAdd("Resetting Info Popup Size and Location to Default");
                this.Left = PopinLeft;
                this.Top = PopinTop;
                this.Width = PopinWidth;
                this.Height = PopinHeight;
                BtnReset.Visibility = Visibility.Hidden;
                TogglePopout(true);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TogglePopout(bool? forcePoppin = null)
        {
            try
            {
                if (forcePoppin != null)
                {
                    uDebugLogAdd($"Forcing poppin: {forcePoppin}");
                    PoppedOut = (bool)forcePoppin;
                }
                if (PoppedOut)
                {
                    PoppedOut = false;
                    this.Left = PopinLeft;
                    this.Top = PopinTop;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.ShowInTaskbar = false;
                    PlyInfoVisualSlider.Visibility = Visibility.Visible;
                    BtnMinimize.Visibility = Visibility.Hidden;
                    BtnPopInOut.Content = "🢅";
                }
                else if (!PoppedOut)
                {
                    PoppedOut = true;
                    this.Left += 10;
                    this.Top -= 10;
                    this.ResizeMode = ResizeMode.CanResize;
                    this.ShowInTaskbar = true;
                    PlyInfoVisualSlider.Visibility = Visibility.Hidden;
                    BtnMinimize.Visibility = Visibility.Visible;
                    BtnPopInOut.Content = "🢇";
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyResetButtonRequirement()
        {
            try
            {
                var utilTop = UtilityBar.UtilBarMain.Top;
                var actHeight = this.ActualHeight;
                if ((this.Left != PopinLeft || this.Top != PopinTop || this.ActualWidth != PopinWidth || this.ActualHeight != PopinHeight) && BtnReset.Visibility != Visibility.Visible)
                {
                    BtnReset.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SubscribeToEvents()
        {
            Events.UtilBarMoveTrigger += Events_UtilBarMoveTrigger;
            Events.NetConnectivityChanged += Events_NetConnectivityChanged;
        }

        private void Events_UtilBarMoveTrigger(UtilMoveArgs args)
        {
            try
            {
                if (!PoppedOut)
                    MoveToNewLocation();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Events_NetConnectivityChanged(NetConnectivityArgs args)
        {
            UpdateNetworkUI(args);
        }

        private void UpdateNetworkUI(NetConnectivityArgs args)
        {
            try
            {
                UIUpdateConnectivity(args.ConnectionStatus);
                UIUpdateWlanInfo(args.ConnectionType, args.CurrentWlanIf, args.WifiLinkSpeed);
                UIUpdateEthInfo(args.ConnectionType, args.CurrentEthIf, args.EthLinkSpeed);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UIUpdateEthInfo(NetConnectionType connectionType, NetworkInterface currentEthIf, double? ethLinkSpeed)
        {
            try
            {
                if (connectionType == NetConnectionType.EthWifi || connectionType == NetConnectionType.Wired)
                {
                    if (currentEthIf != null)
                    {
                        string ipAddress = string.Empty;
                        foreach (var address in currentEthIf.GetIPProperties().UnicastAddresses)
                        {
                            if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                ipAddress = address.Address.ToString();
                            // UnicastIPAddressInformation.Address = {192.168.1.178} | AddressFamily = InterNetwork | PrefixOrigin = Dhcp || Address = {fe80::d9e7:80a9:764c:344b%8} | AddressFamily = InterNetworkV6 | PrefixOrigin = WellKnown
                        }
                        if (string.IsNullOrWhiteSpace(ipAddress))
                            ipAddress = currentEthIf.GetIPProperties().UnicastAddresses[0].Address.ToString();
                        LabelEthIPValue.Content = ipAddress;
                        LabelEthMacValue.Content = Network.ConvertMacAddrToColonNotation(currentEthIf.GetPhysicalAddress().ToString());
                        LabelEthSpeedValue.Content = Network.GetSpeedString(ethLinkSpeed);
                    }
                    else
                        SetDefaultEthUI();
                }
                else
                {
                    SetDefaultEthUI();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UIUpdateWlanInfo(NetConnectionType connectionType, WlanClient.WlanInterface currentWlanIf, double? wifiLinkSpeed)
        {
            try
            {
                if (connectionType == NetConnectionType.EthWifi || connectionType == NetConnectionType.Wireless)
                {
                    if (currentWlanIf != null)
                    {
                        string ipAddress = string.Empty;
                        foreach (var address in currentWlanIf.NetworkInterface.GetIPProperties().UnicastAddresses)
                        {
                            if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                ipAddress = address.Address.ToString();
                            // UnicastIPAddressInformation.Address = {192.168.1.178} | AddressFamily = InterNetwork | PrefixOrigin = Dhcp || Address = {fe80::d9e7:80a9:764c:344b%8} | AddressFamily = InterNetworkV6 | PrefixOrigin = WellKnown
                        }
                        if (string.IsNullOrWhiteSpace(ipAddress))
                            ipAddress = currentWlanIf.NetworkInterface.GetIPProperties().UnicastAddresses[0].Address.ToString();
                        LabelWlanIPValue.Content = ipAddress;
                        LabelWlanMacValue.Content = Network.ConvertMacAddrToColonNotation(currentWlanIf.NetworkInterface.GetPhysicalAddress().ToString());
                        LabelWlanSpeedValue.Content = Network.GetSpeedString(wifiLinkSpeed);
                    }
                    else
                        SetDefaultWlanUI();
                }
                else
                {
                    SetDefaultWlanUI();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UIUpdateConnectivity(ConnectivityStatus connectionStatus)
        {
            try
            {
                switch (connectionStatus)
                {
                    case ConnectivityStatus.Internet:
                        LabelConnectivityValue.Content = "Internet (Layer 4 / TCP)";
                        LabelConnectivityValue.Foreground = new SolidColorBrush(Color.FromArgb(100, 39, 216, 0));
                        break;
                    case ConnectivityStatus.Layer4:
                        LabelConnectivityValue.Content = "Internet (Layer 4 / TCP)";
                        LabelConnectivityValue.Foreground = new SolidColorBrush(Color.FromArgb(100, 39, 216, 0));
                        break;
                    case ConnectivityStatus.Layer3:
                        LabelConnectivityValue.Content = "Internet (Layer 3 / ICMP)";
                        LabelConnectivityValue.Foreground = new SolidColorBrush(Toolbox.ColorFromHex("#FFC9DC05"));
                        break;
                    case ConnectivityStatus.Local:
                        LabelConnectivityValue.Content = "Local (Gateway Only)";
                        LabelConnectivityValue.Foreground = new SolidColorBrush(Color.FromArgb(100, 207, 228, 0));
                        break;
                    case ConnectivityStatus.None:
                        LabelConnectivityValue.Content = "None (Can't Reach Gateway)";
                        LabelConnectivityValue.Foreground = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
                        break;
                    case ConnectivityStatus.DNSError:
                        LabelConnectivityValue.Content = "DNS Error Occured";
                        LabelConnectivityValue.Foreground = new SolidColorBrush(Color.FromArgb(100, 223, 0, 224));
                        break;
                    case ConnectivityStatus.DHCPError:
                        LabelConnectivityValue.Content = "DHCP Error Occured";
                        LabelConnectivityValue.Foreground = new SolidColorBrush(Color.FromArgb(100, 223, 0, 224));
                        break;
                }
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"ConnectivityUI update failure: {ex.Message}", DebugType.FAILURE);
            }
        }

        #endregion
    }
}
