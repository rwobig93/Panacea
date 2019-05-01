using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using System.Windows.Shell;
using System.Windows.Threading;
using static Panacea.MainWindow;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for NetworkPopup.xaml
    /// </summary>
    public partial class NetworkPopup : Window
    {
        public NetworkPopup()
        {
            SetDefaultLocation();
            InitializeComponent();
            Startup();
        }

        #region Globals

        private bool _startingUp = true;
        private bool _pingPaused = false;
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.2) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.2) };
        private double PopinLeft { get { return UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuNetwork.Margin.Left; } }
        private double PopinTop { get { return UtilityBar.UtilBarMain.Top - this.ActualHeight; } }
        private double PopinWidth { get { return 535; } }
        private double PopinHeight { get { return 300; } }
        public bool PoppedOut { get; set; } = false;
        public bool resolvingDNS = false;
        private int resolvedEntries = 0;

        #endregion

        #region Form Handling

        private void WinNetMain_Loaded(object sender, RoutedEventArgs e)
        {
            EnableWindowResizing();
        }

        private void WinNetMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_startingUp)
                VerifyResetButtonRequirement();
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

        private void RectGrabBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left && PoppedOut)
                    winNetMain.DragMove();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region Event Handlers

        private void lblNetDNS_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int retries = 3;
            int delay = 100;
            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        var textBlock = (TextBlock)e.OriginalSource;
                        CopyToClipboard(textBlock.Text);
                    }
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

        private void lblNetDNSFull_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int retries = 3;
            int delay = 100;
            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        var dnsEntry = (HostEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                        CopyToClipboard($"{dnsEntry.IPAddress} = {dnsEntry.HostName}");
                    }
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

        private void BtnBasicPingEntryClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (BasicPingEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                lbNetPing.Items.Remove(lbItem);
                lbItem.Destroy();
                lbItem = null;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnBasicPingEntryToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (BasicPingEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                lbItem.TogglePing();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnPopInOut_Click(object sender, RoutedEventArgs e)
        {
            TogglePopout();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetPopupSizeAndLocation();
        }

        private void BtnPingPauseAll_Click(object sender, RoutedEventArgs e)
        {
            TogglePingsStatus();
        }

        private void BtnPingRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            DestroyAllPings();
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

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            Director.Main.OpenInfoWindow(HelpMenu.NetworkMenu);
        }

        #endregion

        #region Methods

        private void Startup()
        {
            PopupShow();
            SubscribeToEvents();
            FinishStartup();
        }

        private void SubscribeToEvents()
        {
            try
            {
                Events.UtilBarMoveTrigger += Events_UtilBarMoveTrigger;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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

        private void SetDefaultLocation()
        {
            try
            {
                this.Top = UtilityBar.UtilBarMain.Top - PopinHeight;
                this.Left = UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuNetwork.Margin.Left;
                this.Opacity = 0;
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

        private void FinishStartup()
        {
            try
            {
                _startingUp = false;
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
                Toolbox.uAddDebugLog($"POPNET: {_log}", _type, caller);
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

        private void CopyToClipboard(string clip, string optionalMessage = null)
        {
            try
            {
                UtilityBar.UtilBarMain.CopyToClipboard(clip, optionalMessage);
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Error occured when setting clipboard: {clip} | {ex.Message}", DebugType.FAILURE);
            }
        } 

        public void PopupHide()
        {
            try
            {
                if (this.Opacity == 1.0)
                    this.BeginAnimation(Window.OpacityProperty, outAnimation);
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
                if (this.Opacity == 0)
                    this.BeginAnimation(Window.OpacityProperty, inAnimation);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public bool DoesPingSessionExist(string address)
        {
            var exists = false;
            foreach (BasicPingEntry entry in lbNetPing.Items)
            {
                if (entry.Address == address || entry.HostName.ToLower() == address)
                    exists = true;
            }
            return exists;
        }

        public void AddPingEntry(string address)
        {
            try
            {
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                foreach (var entry in entries)
                    lbNetPing.Items.Add(new BasicPingEntry(entry));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void AddNSLookupEntry(string address)
        {
            try
            {
                lbNetNSLookup.Items.Clear();
                resolvingDNS = true;
                int entriesToLookFor = 0;
                string resolvedAddress = string.Empty;
                string currentAddress = string.Empty;
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                uDebugLogAdd("Starting address lookup");
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrWhiteSpace(entry))
                    {
                        entriesToLookFor++;
                        tResolveDNSEntry(entry);
                    }
                }
                tDNSResolvingHelper(entriesToLookFor);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            finally
            {
                uDebugLogAdd("Finished address lookup method");
            }
        }

        private void tResolveDNSEntry(string entry)
        {
            var resolvedEntry = new HostEntry()
            {
                HostName = "Host Not Found",
                IPAddress = entry
            };
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    var dnsEntry = Dns.GetHostEntry(entry);
                    uDebugLogAdd("Attempted DNS entry lookup");
                    if (dnsEntry != null)
                    {
                        uDebugLogAdd("DNS entry wasn't null");
                        if (!string.IsNullOrWhiteSpace(dnsEntry.HostName))
                        {
                            resolvedEntry.HostName = dnsEntry.HostName;
                            resolvedEntry.IPAddress = dnsEntry.AddressList[0].ToString();
                            uDebugLogAdd($"DNS entry hostname isn't empty, resolved to: {resolvedEntry.HostName}");
                        }
                    }
                    worker.ReportProgress(1);
                    resolvedEntries++;
                }
                catch (SocketException se)
                {
                    uDebugLogAdd($"DNS lookup error: {se.Message}");
                    resolvedEntry.HostName = se.Message;
                    worker.ReportProgress(1);
                    resolvedEntries++;
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.ProgressChanged += (sender2, e2) =>
            {
                if (e2.ProgressPercentage == 1)
                {
                    uDebugLogAdd($"Worker progress is {e2.ProgressPercentage}, adding ResolvedEntry | IP({resolvedEntry.IPAddress}) | HN({resolvedEntry.HostName})");
                    lbNetNSLookup.Items.Add(resolvedEntry);
                }
            };
            worker.RunWorkerAsync();
        }

        private void tDNSResolvingHelper(int entriesToLookFor)
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (work, we) =>
            {
                while (resolvedEntries != entriesToLookFor)
                {
                    worker.ReportProgress(1);
                    Thread.Sleep(500);
                    worker.ReportProgress(0);
                }
                worker.ReportProgress(2);
            };
            worker.ProgressChanged += (pc, pe) =>
            {
                try
                {
                    if (pe.ProgressPercentage == 1)
                    {
                        if (lblNetNSLookupTitle.Content.ToString() != "NSlookup..." && lblNetNSLookupTitle.Content.ToString().Length < 12)
                            lblNetNSLookupTitle.Content = $"{lblNetNSLookupTitle.Content}.";
                        else
                            lblNetNSLookupTitle.Content = "NSlookup";
                    }
                    if (pe.ProgressPercentage == 2)
                    {
                        lblNetNSLookupTitle.Content = "NSlookup";
                        resolvedEntries = 0;
                        resolvingDNS = false;
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
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
                    plyNetVisualSlider.Visibility = Visibility.Visible;
                    btnMinimize.Visibility = Visibility.Hidden;
                    btnPopInOut.Content = "🢅";
                }
                else if (!PoppedOut)
                {
                    PoppedOut = true;
                    this.Left += 10;
                    this.Top -= 10;
                    this.ResizeMode = ResizeMode.CanResize;
                    this.ShowInTaskbar = true;
                    plyNetVisualSlider.Visibility = Visibility.Hidden;
                    btnMinimize.Visibility = Visibility.Visible;
                    btnPopInOut.Content = "🢇";
                }
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
                uDebugLogAdd("Resetting Network Popup Size and Location to Default");
                this.Left = PopinLeft;
                this.Top = PopinTop;
                this.Width = PopinWidth;
                this.Height = PopinHeight;
                btnReset.Visibility = Visibility.Hidden;
                TogglePopout();
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
                if ((this.Left != PopinLeft || this.Top != PopinTop || this.ActualWidth != PopinWidth || this.ActualHeight != PopinHeight) && btnReset.Visibility != Visibility.Visible)
                {
                    btnReset.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShowNotification(string text)
        {
            try
            {
                uDebugLogAdd($"Calling ShowNotification from NetPopup: {text}");
                UtilityBar.UtilBarMain.ShowNotification(text);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TogglePingsStatus()
        {
            try
            {
                if (lbNetPing.Items.Count <= 0)
                {
                    ShowNotification("You aren't pinging anything currently");
                    return;
                }

                PingStat setPingStat = PingStat.Unknown;

                if (_pingPaused)
                {
                    _pingPaused = false;
                    setPingStat = PingStat.Active;
                    btnPingPauseAll.Content = @"❚❚ All";
                }
                else
                {
                    _pingPaused = true;
                    setPingStat = PingStat.Paused;
                    btnPingPauseAll.Content = @"▶ All";
                }

                foreach (BasicPingEntry item in lbNetPing.Items)
                {
                    item.TogglePing(setPingStat);
                }

                if (setPingStat == PingStat.Active)
                    ShowNotification("All Pings Resumed");
                else if (setPingStat == PingStat.Paused)
                    ShowNotification("All Pings Paused");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DestroyAllPings()
        {
            try
            {
                if (lbNetPing.Items.Count <= 0)
                {
                    ShowNotification("You aren't pinging anything currently");
                    return;
                }

                foreach (var item in lbNetPing.Items.Cast<BasicPingEntry>().ToList())
                {
                    lbNetPing.Items.Remove(item);
                    item.Destroy();
                }

                ShowNotification("All Pings Destroyed!");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion
    }
}
