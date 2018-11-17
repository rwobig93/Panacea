using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Panacea.Windows;
using System.Collections.ObjectModel;
using LiveCharts;
using LiveCharts.Configurations;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using System.Globalization;
using NAudio.CoreAudioApi;
using System.Collections;
using System.Management;
using System.Reflection;
using System.Net.Sockets;

namespace Panacea
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Startup();
        }

        #region Globals

        private List<string> notifications = new List<string>();
        private string logDir = $@"{Directory.GetCurrentDirectory()}\Logs\";
        private string confDir = $@"{Directory.GetCurrentDirectory()}\Config\";
        private string exDir = $@"{Directory.GetCurrentDirectory()}\Logs\Exceptions\";
        private MMDevice selectedAudioEndpoint = null;
        private IntPtr LastFoundWindow = IntPtr.Zero;
        private HandleDisplay windowHandleDisplay = null;
        private Point mouseStartPoint = new Point(0, 0);
        private Octokit.GitHubClient gitClient = null;
        private bool audioRefreshing = false;
        private bool debugMode = false;
        private bool notificationPlaying = false;
        private bool aboutPlaying = false;
        private bool glblPinging = true;
        private int settingsTimer = 5;
        private bool settingsSaveVerificationInProgress = false;
        private bool resolvingDNS = false;
        private int resolvedEntries = 0;
        private bool settingsBadAlerted = false;
        private bool traceLoading = false;
        private bool resizingNetGrid = false;
        private bool capturingHandle = false;
        private bool startingUp = false;

        #endregion

        #region Enums

        public enum DebugType
        {
            EXCEPTION,
            STATUS,
            INFO
        }

        #endregion

        #region Form Handling

        private void winMain_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && resizingNetGrid == false && capturingHandle == false)
                DragMove();
        }

        private void winMain_Loaded(object sender, RoutedEventArgs e)
        {
            // Allow borderless window to be resized
            WindowChrome.SetWindowChrome(this, new WindowChrome() { ResizeBorderThickness = new Thickness(5), CaptionHeight = .05 });
            CleanupOldFiles();
        }

        private void winMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            uDebugLogAdd("Application closing, starting closing methods");
            SaveSettings();
            uDebugLogAdd(string.Format("{0}##################################### Application Closing #####################################{0}", Environment.NewLine));
            DumpDebugLog();
        }

        private void winMain_Closed(object sender, EventArgs e)
        {
            var p = Process.GetCurrentProcess();
            p.Kill();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Event Handlers

        #region grdMain

        private void btnStatusToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleStatusSize();
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            uDebugLogAdd("About button clicked");
            if (aboutPlaying)
            {
                uDebugLogAdd("About is already playing, returning");
                return;
            }
            aboutPlaying = true;
            uDebugLogAdd("showAbout thread started");
            ShowNotification("Created by Rick Wobig");
            uStatusUpdate("Created by Rick Wobig");
            ShowNotification("Questions or Improvements?");
            uStatusUpdate("Questions or Improvements?");
            ShowNotification("Email: rick@wobigtech.net");
            uStatusUpdate("Email: rick@wobigtech.net");
            Thread showAbout = new Thread(() =>
            {
                Thread.Sleep(15000);
                aboutPlaying = false;
                uDebugLogAdd("Finished waiting 15 seconds before allowing about button clicks");
            });
            showAbout.Start();
        }

        private void btnMenuAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleMenuGrid(grdAudio);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnMenuWindows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleMenuGrid(grdWindows);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnMenuNetwork_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleMenuGrid(grdNetwork);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnMenuSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleMenuGrid(grdSettings);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnMenuUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateApplication();
        }

        private void Events_UpdateDebugStatus(DebugUpdateArgs args)
        {
            try
            {
                if (debugMode) Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { txtStatus.AppendText($"{Environment.NewLine}{DateTime.Now.ToLocalTime().ToString("MM-dd-yy")}_{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {args.DebugType.ToString()}: {args.LogUpdate}"); } catch (Exception ex) { LogException(ex); } });
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

        private void lblTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string verNum = GetVersionNumber().ToString();
                Clipboard.SetText(verNum);
                uStatusUpdate($"Copied version to clipboard: {verNum}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region grdAudio

        private void lbAudioDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!audioRefreshing)
                {
                    //var selectedDevice = ((AudioDevice)lbAudioDevices.SelectedItem).DeviceIndex;
                    //if (((AudioDevice)lbAudioDevices.SelectedItem) != selectedAudioDevice)
                    //{
                    //    selectedAudioDevice = ((AudioDevice)lbAudioDevices.SelectedItem);
                    //    SetAudioDevice(controllerPath, selectedDevice);
                    //    uStatusUpdate($"Changed Playback Device To: {selectedAudioDevice.DeviceName}");
                    //}
                    var selectedEndpoint = ((MMDevice)lbAudioDevices.SelectedItem);
                    if (selectedEndpoint != selectedAudioEndpoint)
                    {
                        selectedAudioEndpoint = selectedEndpoint;
                        Audio.SetDefaultAudioDevice(selectedEndpoint);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnRefreshAudio_Click(object sender, RoutedEventArgs e)
        {
            RefreshAudioList();
            uStatusUpdate("Refreshed Playback Device List");
        }

        #endregion

        #region grdWindows

        private void btnGetPosition_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedWindowPosition();
        }

        private void btnMoveWindow_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedWindow();
        }

        private void btnWindowItemEnabled(object sender, RoutedEventArgs e)
        {
            ToggleWindowButtonEnabled(sender);
        }

        private void btnWindowItemChecked(object sender, RoutedEventArgs e)
        {
            ToggleWindowButtonChecked(sender);
        }

        private void BtnDeleteWindowItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteSavedWindowItem();
        }

        private void rectTarget_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void btnWinTarget_Click(object sender, RoutedEventArgs e)
        {
            OpenWindowHandleFinder();
        }

        private void MouseHook_OnMouseUp(object sender, System.Drawing.Point p)
        {
            if (capturingHandle)
            {
                RevertCursor();
            }
            //CloseWindowHandleFinder();
            //SaveSelectedWindow();
        }

        private void rectTarget_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LookForWindowHandle();
            CaptureWindowHandle();
            OpenWindowHandleFinder();
            DisplayWindowInfo();
            //OpenCaptureOverlay();
        }

        private void btnWinProfile1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Toolbox.settings.CurrentWindowProfile == WindowProfile.Profile1)
                    MoveAllWindows();
                else
                    ChangeWindowProfiles(WindowProfile.Profile1);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnWinProfile2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Toolbox.settings.CurrentWindowProfile == WindowProfile.Profile2)
                    MoveAllWindows();
                else
                    ChangeWindowProfiles(WindowProfile.Profile2);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnWinProfile3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Toolbox.settings.CurrentWindowProfile == WindowProfile.Profile3)
                    MoveAllWindows();
                else
                    ChangeWindowProfiles(WindowProfile.Profile3);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnWinProfile4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Toolbox.settings.CurrentWindowProfile == WindowProfile.Profile4)
                    MoveAllWindows();
                else
                    ChangeWindowProfiles(WindowProfile.Profile4);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnWinUpdateManual_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnWinRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshSavedWindows();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region grdNetwork

        private void btnNetLookup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var address = txtNetAddress.Text;
                if (!VerifyInput(address))
                {
                    uDebugLogAdd($"Input entered was invalid, sending notification and canceling dns lookup | Input: {address}");
                    ShowNotification("Address(es) entered incorrect or duplicate, try again");
                    return;
                }
                if (resolvingDNS)
                {
                    uDebugLogAdd($"resolvingDNS is {resolvingDNS}, cancelling NsLookup");
                    ShowNotification("A DNS Lookup is in progress already");
                }
                else
                    LookupAddress(address);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnNetPing_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var address = txtNetAddress.Text;
                var sendNotif = false;
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                var validEntries = string.Empty;
                foreach (var entry in entries)
                {
                    if (!VerifyInput(entry))
                    {
                        uDebugLogAdd($"Input entered was invalid, sending notification and canceling ping | Input: {entry}");
                        sendNotif = true;
                    }
                    else
                        validEntries = $"{validEntries}{entries},";
                }
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
                if (!Toolbox.settings.BasicPing)
                {
                    if (lbPingSessions.Visibility == Visibility.Visible)
                        AddPingEntry(address);
                    else
                        AddBasicPing(address);
                }
                else
                    AddBasicPing(address);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnPingEntryUpArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (PingEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                MovePingEntry(lbItem, Direction.Up);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnPingEntryDownArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (PingEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                MovePingEntry(lbItem, Direction.Down);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnPingEntryClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (PingEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                lbPingSessions.Items.Remove(lbItem);
                lbItem = null;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnPingEntryToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (PingEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                lbItem.TogglePing();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnTraceEntryUpArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (TraceEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                MoveTraceEntry(lbItem, Direction.Up);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnTraceEntryDownArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (TraceEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                MoveTraceEntry(lbItem, Direction.Down);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnTraceEntryClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (TraceEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                lbTraceSessions.Items.Remove(lbItem);
                lbItem.Dispose();
                if (lbTraceSessions.Items.Count <= 0)
                {
                    ToggleListBox(lbTraceSessions);
                    if (Toolbox.settings.BasicPing)

                        SwapPingGrids();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnTraceEntryToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (TraceEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                lbItem.ToggleTrace();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnNetPingToggleAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (glblPinging == true)
                    glblPinging = false;
                else
                    glblPinging = true;
                if (lbPingSessions.Visibility == Visibility.Visible)
                {
                    foreach (PingEntry entry in lbPingSessions.Items)
                        entry.TogglePing(glblPinging);
                }
                else
                {
                    foreach (PingBasic entry in lbPingBasic.Items)
                        entry.TogglePing(glblPinging);
                }
                foreach (TraceEntry entry in lbTraceSessions.Items)
                    entry.ToggleTrace(glblPinging);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void txtNetAddress_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    switch (Toolbox.settings.ToolboxEnterAction)
                    {
                        case EnterAction.DNSLookup:
                            btnNetLookup_Click(sender, e);
                            break;
                        case EnterAction.Ping:
                            btnNetPing_Click(sender, e);
                            break;
                        case EnterAction.Trace:
                            btnNetTrace_Click(sender, e);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

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
                        Clipboard.SetText(textBlock.Text);
                        uStatusUpdate($"Set Clipboard: {textBlock.Text}");
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
                        Clipboard.SetText($"{dnsEntry.IPAddress} = {dnsEntry.HostName}");
                        uStatusUpdate($"Set Clipboard: {dnsEntry.IPAddress} = {dnsEntry.HostName}");
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

        private void btnNetTrace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (traceLoading)
                    return;
                if (lbTraceSessions.Items.Count <= 0)
                {
                    ToggleListBox(lbTraceSessions);
                }
                if (lbPingSessions.Items.Count > 0)
                {
                    SwapPingGrids();
                }
                var address = txtNetAddress.Text;
                var sendNotif = false;
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                var validEntries = string.Empty;
                foreach (var entry in entries)
                {
                    if (!VerifyInput(entry))
                    {
                        uDebugLogAdd($"Input entered was invalid, sending notification and canceling trace | Input: {entry}");
                        sendNotif = true;
                    }
                    else
                        validEntries = $"{validEntries}{entries},";
                }
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
                AddTraceEntry(address);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            TestWifi();
        }

        #endregion

        #region grdSettings

        private void txtSetNetPingCount_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.PingCount);
        }

        private void cmbxSetNetDTFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.PingDTFormat);
        }

        private void cmbxSetNetTextboxAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.TextBoxAction);
        }

        private void chkNetBasicPing_Click(object sender, RoutedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.BasicPing);
        }

        #endregion

        #endregion

        #region Methods

        #region General

        private void Startup()
        {
#if DEBUG
            debugMode = true;
            BtnTest.Visibility = Visibility.Visible;
#endif
#if DEBUG == false
            DataContext = this;
#endif
            startingUp = true;
            SubscribeToEvents();
            SetupAppFiles();
            uDebugLogAdd(string.Format("{0}##################################### Application Start #####################################{0}", Environment.NewLine));
            DeSerializeSettings();
            SetDefaultSettings();
            SetWindowLocation();
            SetupAudioDeviceList();
            tSaveSettingsAuto();
            InitializeMenuGrids();
            tCheckForUpdates();
            FinishStartup();
        }

        private void ToggleStatusSize()
        {
            try
            {
                var currentSize = 0.00;
                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { currentSize = txtStatus.Height; } catch (Exception ex) { LogException(ex); } });
                if (currentSize != 230.00)
                {
                    uDebugLogAdd(string.Format("Sliding txtStatus to Height of 230.00, current: {0}", currentSize));
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { btnStatusToggle.Content = "Close"; } catch (Exception ex) { LogException(ex); } });
                    DoubleAnimation slide = new DoubleAnimation { AccelerationRatio = .9, Duration = new Duration(TimeSpan.FromSeconds(0.3)), To = 230.00 };
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { txtStatus.BeginAnimation(FrameworkElement.HeightProperty, slide); } catch (Exception ex) { LogException(ex); } });
                }
                else
                {
                    uDebugLogAdd(string.Format("Sliding txtStatus to Height of 20.00, current: {0}", currentSize));
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { btnStatusToggle.Content = "Open"; } catch (Exception ex) { LogException(ex); } });
                    DoubleAnimation slide = new DoubleAnimation { AccelerationRatio = .9, Duration = new Duration(TimeSpan.FromSeconds(0.3)), To = 20.00 };
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { txtStatus.BeginAnimation(FrameworkElement.HeightProperty, slide); } catch (Exception ex) { LogException(ex); } });
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShowNotification(string notification)
        {
            try
            {
                uStatusUpdate(string.Format("Notification: {0}", notification));
                BackgroundWorker worker = new BackgroundWorker()
                {
                    WorkerReportsProgress = true
                };
                worker.ProgressChanged += Worker_ProgressChanged;
                worker.DoWork += (sender, e) =>
                {
                    try
                    {
                        uDebugLogAdd($"Adding Notification: {notification} [ms: {9 * notification.Length}]");
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { notifications.Add(notification); } catch (Exception ex) { LogException(ex); } });
                        if (notificationPlaying) { uDebugLogAdd("Notification is currently playing, returning"); return; }
                        notificationPlaying = true;
                        uDebugLogAdd("Notification wasn't playing, starting notification play cycles");
                        while (notifications.Count != 0)
                        {
                            foreach (var message in notifications.ToList())
                            {
                                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lblNotification.Text = message; } catch (Exception ex) { LogException(ex); } });
                                worker.ReportProgress(1);
                                Thread.Sleep(TimeSpan.FromMilliseconds(95 * notification.Replace(" ", "").Length));
                                worker.ReportProgress(2);
                                notifications.Remove(message);
                                uDebugLogAdd(string.Format("Removed notification: {0}", message));
                                uDebugLogAdd(string.Format("Notifications left: {0}", notifications.Count));
                            }
                        }
                        notificationPlaying = false;
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

        private void Worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            try
            {
                ToggleNotification(e.ProgressPercentage);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void uStatusUpdate(string _status)
        {
            try
            {
                if (!debugMode) Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { txtStatus.AppendText($"{Environment.NewLine}{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {_status}"); } catch (Exception ex) { LogException(ex); } });
                uDebugLogAdd(_status, DebugType.STATUS);
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
                DoubleAnimation slideIn = new DoubleAnimation { AccelerationRatio = .9, Duration = new Duration(TimeSpan.FromSeconds(0.3)), To = 55 };
                DoubleAnimation slideIn2 = new DoubleAnimation { AccelerationRatio = .9, Duration = new Duration(TimeSpan.FromSeconds(0.3)), To = 75 };
                DoubleAnimation slideOut = new DoubleAnimation { AccelerationRatio = .9, Duration = new Duration(TimeSpan.FromSeconds(0.3)), To = 30 };
                DoubleAnimation twoLiner = new DoubleAnimation { AccelerationRatio = .9, Duration = new Duration(TimeSpan.FromSeconds(0.3)), To = 45 };
                DoubleAnimation oneLiner = new DoubleAnimation { AccelerationRatio = .9, Duration = new Duration(TimeSpan.FromSeconds(0.3)), To = 25 };
                switch (state)
                {
                    case 1:
                        if (lblNotification.Text.Length <= 98)
                        {
                            grdNotification.BeginAnimation(FrameworkElement.HeightProperty, slideIn);
                            lblNotification.BeginAnimation(FrameworkElement.HeightProperty, oneLiner);
                        }
                        else
                        {
                            grdNotification.BeginAnimation(FrameworkElement.HeightProperty, slideIn2);
                            lblNotification.BeginAnimation(FrameworkElement.HeightProperty, twoLiner);
                        }
                        break;
                    case 2:
                        grdNotification.BeginAnimation(FrameworkElement.HeightProperty, slideOut);
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

        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO)
        {
            try
            {
                Toolbox.uAddDebugLog(_log, _type);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DumpDebugLog()
        {
            int retries = 3;
            int delay = 1000;
            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    using (StreamWriter sw = File.AppendText($@"{logDir}DebugLog_{DateTime.Now.ToString("MM-dd-yy")}.log"))
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
                        using (StreamWriter sw = File.AppendText($@"{logDir}DebugLog_{DateTime.Now.ToString("MM-dd-yy")}_{Toolbox.GenerateRandomNumber()}.log"))
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
                uStatusUpdate("An Exception was logged");
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
                uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
                uStatusUpdate("An Exception was logged");
            }
        }

        private void ToggleMenuGrid(Grid grid)
        {
            try
            {
                if (grid.Margin != Defaults.MainGridIn)
                {
                    grid.Visibility = Visibility.Visible;
                    Toolbox.AnimateGrid(grid, Defaults.MainGridIn);
                }
                else
                {
                    Toolbox.AnimateGrid(grid, Defaults.MainGridOut);
                    grid.Visibility = Visibility.Hidden;
                }
                HideUnusedMenuGrids(grid);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleListBox(ListBox lb)
        {
            try
            {
                if (lb.Margin != Defaults.TraceLBIn)
                {
                    lb.Visibility = Visibility.Visible;
                    lbPingBasic.Visibility = Visibility.Visible;
                    Toolbox.AnimateListBox(lb, Defaults.TraceLBIn);
                    Toolbox.AnimateListBox(lbPingBasic, Defaults.PingLBasicIn);
                }
                else
                {
                    Toolbox.AnimateListBox(lbPingBasic, Defaults.PingLBasicOut);
                    Toolbox.AnimateListBox(lb, Defaults.TraceLBOut);
                    lb.Visibility = Visibility.Hidden;
                    lbPingBasic.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void HideUnusedMenuGrids(Grid grid = null)
        {
            if (grid != grdAudio)
            {
                grdAudio.Visibility = Visibility.Hidden;
                Toolbox.AnimateGrid(grdAudio, Defaults.MainGridOut);
            }
            if (grid != grdWindows)
            {
                grdWindows.Visibility = Visibility.Hidden;
                Toolbox.AnimateGrid(grdWindows, Defaults.MainGridOut);
            }
            if (grid != grdNetwork)
            {
                grdNetwork.Visibility = Visibility.Hidden;
                Toolbox.AnimateGrid(grdNetwork, Defaults.MainGridOut);
            }
            if (grid != grdSettings)
            {
                grdSettings.Visibility = Visibility.Hidden;
                Toolbox.AnimateGrid(grdSettings, Defaults.MainGridOut);
            }
        }

        private void InitializeMenuGrids()
        {
            try
            {
                HideUnusedMenuGrids();
                HideElementsThatNeedHidden();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void HideElementsThatNeedHidden()
        {
            lbTraceSessions.Visibility = Visibility.Hidden;
            SetChosenPing();
        }

        private void SetChosenPing()
        {
            if (Toolbox.settings.BasicPing)
            {
                Toolbox.AnimateListBox(lbPingBasic, Defaults.PingLBasicPIn);
                lbPingSessions.Visibility = Visibility.Hidden;
            }
            else
            {
                Toolbox.AnimateListBox(lbPingSessions, Defaults.PingLBasicPIn);
                lbPingBasic.Visibility = Visibility.Hidden;
            }
        }

        public object GetPropertyValue(string propertyName)
        {
            //returns value of property Name
            return this.GetType().GetProperty(propertyName).GetValue(this, null);
        }

        private void CleanupOldFiles()
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(logDir))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    string fileNaem = fileInfo.Name;
                    if ((fileInfo.LastWriteTime <= DateTime.Now.AddDays(-14)))
                    {
                        try
                        {
                            fileInfo.Delete();
                            uStatusUpdate($"Deleted old log file: {fileNaem}");
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
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SubscribeToEvents()
        {
            Events.UpdateDebugStatus += Events_UpdateDebugStatus;
            WinAPIWrapper.MouseHook.OnMouseUp += MouseHook_OnMouseUp;
        }

        private Version GetVersionNumber()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void UpdateApplication()
        {
            try
            {
                uDebugLogAdd("Starting update process");
                SaveSettings();
                var upstaller = $"{Directory.GetCurrentDirectory()}\\Upstaller.exe";
                uDebugLogAdd($"Upstaller dir: {upstaller}");
                if (File.Exists(upstaller))
                {
                    uDebugLogAdd("Upstaller exists, starting the proc");
                    Process proc = new Process() { StartInfo = new ProcessStartInfo() { FileName = upstaller } };
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

        private void TestWifi()
        {
            try
            {
                var wifiLinkSpeed = GetWifiLinkSpeed();
                if (wifiLinkSpeed != null)
                    ShowNotification($"Wifi Link Speed: {wifiLinkSpeed} Mbps");
                else
                    ShowNotification("Not currently connected to wifi...");
            }
            catch (Exception ex)
            {
                LogException(ex);
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

        #endregion

        #region Audio

        private void SetupAudioDeviceList()
        {
            try
            {
                RefreshAudioList();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RefreshAudioList()
        {
            try
            {
                Interfaces.AudioMain.EndpointAudioDeviceList = Audio.GetAudioDevices();
                lbAudioDevices.ItemsSource = Interfaces.AudioMain.EndpointAudioDeviceList;
                var defDevice = Audio.GetDefaultAudioDevice();
                foreach (var item in lbAudioDevices.Items)
                {
                    if (((MMDevice)item).ID == defDevice.ID)
                    {
                        selectedAudioEndpoint = ((MMDevice)item);
                        lbAudioDevices.SelectedItem = item;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region Settings

        private void SerializeSettings()
        {
            int retries = 3;
            int delay = 1000;
            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    uDebugLogAdd("Starting settings serialization");
                    string serializedObj = JsonConvert.SerializeObject(Toolbox.settings, Formatting.Indented);
                    using (StreamWriter sw = new StreamWriter($@"{confDir}Settings.conf"))
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
                if (File.Exists($@"{confDir}Settings.conf"))
                {
                    uDebugLogAdd($@"Settings file found: {confDir}Settings.conf");
                    using (StreamReader sr = new StreamReader($@"{confDir}Settings.conf"))
                    {
                        var tmpSettings = JsonConvert.DeserializeObject<Settings>(sr.ReadToEnd());
                        if (tmpSettings != null)
                            Toolbox.settings = tmpSettings;
                    }
                    uDebugLogAdd($@"Deserialized file: {confDir}Settings.conf");
                }
                else { uDebugLogAdd($@"Setings file not found at: {confDir}Settings.conf"); }
                uDebugLogAdd("Finished settings deserialization");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SetDefaultSettings()
        {
            try
            {
                uDebugLogAdd("Setting default settings");
                uDebugLogAdd("SettingsNET: working on ping settings");
                txtSetNetPingCount.Text = Toolbox.settings.PingChartLength.ToString();
                chkNetBasicPing.IsChecked = Toolbox.settings.BasicPing;
                uDebugLogAdd("SettingsNET: working on DTFormat settings");
                var seconds = "Seconds";
                var minutes = "Minutes";
                var hours = "Hours";
                cmbxSetNetDTFormat.Items.Add(seconds);
                cmbxSetNetDTFormat.Items.Add(minutes);
                cmbxSetNetDTFormat.Items.Add(hours);
                switch (Toolbox.settings.DateTimeFormat)
                {
                    case DTFormat.Sec:
                        cmbxSetNetDTFormat.SelectedItem = seconds;
                        break;
                    case DTFormat.Min:
                        cmbxSetNetDTFormat.SelectedItem = minutes;
                        break;
                    case DTFormat.Hours:
                        cmbxSetNetDTFormat.SelectedItem = hours;
                        break;
                }
                uDebugLogAdd("SettingsNET: working on textbox action settings");
                var lookup = "DNSLookup";
                var ping = "Ping";
                var trace = "Trace";
                cmbxSetNetTextboxAction.Items.Add(lookup);
                cmbxSetNetTextboxAction.Items.Add(ping);
                cmbxSetNetTextboxAction.Items.Add(trace);
                switch (Toolbox.settings.ToolboxEnterAction)
                {
                    case EnterAction.DNSLookup:
                        cmbxSetNetTextboxAction.SelectedItem = lookup;
                        break;
                    case EnterAction.Ping:
                        cmbxSetNetTextboxAction.SelectedItem = ping;
                        break;
                    case EnterAction.Trace:
                        cmbxSetNetTextboxAction.SelectedItem = trace;
                        break;
                }
                uDebugLogAdd("SettingsWIN: working on window process settings");
                ChangeWindowProfiles(Toolbox.settings.CurrentWindowProfile);
                lbSavedWindows.ItemsSource = Toolbox.settings.WindowList;
                uDebugLogAdd("Default settings set");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SaveSettings()
        {
            try
            {
                uDebugLogAdd("Saving settings");
                Toolbox.settings.WindowLocation = new WindowDimensions() { Left = this.Left, Top = this.Top, Height = this.Height, Width = this.Width };
                SerializeSettings();
                uDebugLogAdd("Settings saved");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void StartSettingsUpdate(SettingsUpdate settingsUpdate)
        {
            try
            {
                if (startingUp)
                {
                    uDebugLogAdd("Application is still starting up, skipping settings update");
                    return;
                }
                SettingsTimerRefresh();
                if (!settingsSaveVerificationInProgress)
                {
                    settingsSaveVerificationInProgress = true;
                    BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                    worker.DoWork += (ws, we) =>
                    {
                        try
                        {
                            while (settingsTimer > 0)
                            {
                                Thread.Sleep(1000);
                                settingsTimer--;
                            }
                            worker.ReportProgress(1);
                            settingsSaveVerificationInProgress = false;
                            settingsTimer = 2;
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
                            tUpdateSettings(settingsUpdate);
                        }
                    };
                    worker.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SettingsTimerRefresh()
        {
            settingsTimer = 2;
        }

        #endregion

        #region WindowPreferences

        private void SetWindowLocation()
        {
            try
            {
                uDebugLogAdd("Setting window location");
                this.Left = Toolbox.settings.WindowLocation.Left;
                this.Top = Toolbox.settings.WindowLocation.Top;
                this.Height = Toolbox.settings.WindowLocation.Height;
                this.Width = Toolbox.settings.WindowLocation.Width;
                uDebugLogAdd($"Set Window Dimensions: [l]{this.Left} [t]{this.Top} [h]{this.Height} [w]{this.Width}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MoveSelectedWindow()
        {
            try
            {
                uDebugLogAdd("Invoking MoveSelectedWindow");
                if (lbSavedWindows.SelectedItem != null)
                {
                    uDebugLogAdd("Selected item not null");
                    var selectedWindow = ((WindowItem)lbSavedWindows.SelectedItem);
                    if (selectedWindow == null)
                    {
                        uDebugLogAdd("Selected window from saved window list is null");
                        return;
                    }
                    uDebugLogAdd("Got selected saved window");
                    MoveProcessHandle(selectedWindow);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MoveProcessHandle(WindowItem selectedWindow)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (ws, we) =>
            {
                try
                {
                    //Process process = WinAPIWrapper.GetProcessFromWinItem(selectedWindow);
                    //if (process != null)
                    //{
                    //    uDebugLogAdd($"Moving process handle {process.ProcessName} {process.MainWindowHandle.ToInt32()}");
                    //    WinAPIWrapper.MoveWindow(process.MainWindowHandle, selectedWindow.WindowInfo.XValue, selectedWindow.WindowInfo.YValue, selectedWindow.WindowInfo.Width, selectedWindow.WindowInfo.Height, true);
                    //    uDebugLogAdd($"Moved process handle {process.ProcessName} {process.MainWindowHandle.ToInt32()}");

                    //}
                    //else
                    //{
                    //    uDebugLogAdd("Current process not found for selected item in saved windows list");
                    //    uStatusUpdate("Running process not found for selected process");
                    //    return;
                    //}
                    var procList = Process.GetProcessesByName(selectedWindow.WindowInfo.Name);
                    foreach (var process in procList)
                    {
                        if (WinAPIWrapper.ProcIsWhatWeAreLookingFor(selectedWindow, process))
                        {
                            var matchingHandles = WinAPIWrapper.FindWindowsWithText(process.MainWindowTitle);
                            foreach (var hndl in matchingHandles)
                            {
                                uDebugLogAdd($"Proc {process.ProcessName} {process.Handle.ToInt32()} matched the selected window item");
                                uDebugLogAdd($"Moving process handle {process.ProcessName}  {process.Handle.ToInt32()} {process.MainWindowHandle.ToInt32()}");
                                WinAPIWrapper.SetWindowPos(hndl, 0, selectedWindow.WindowInfo.XValue, selectedWindow.WindowInfo.YValue, selectedWindow.WindowInfo.Width, selectedWindow.WindowInfo.Height, 0x0001);
                                uDebugLogAdd($"Moved process handle {process.ProcessName}  {process.Handle.ToInt32()} {process.MainWindowHandle.ToInt32()}");
                            }
                        }
                        else
                            uDebugLogAdd($"Skipping proc {process.ProcessName} {process.Handle.ToInt32()} as it didn't match the selected window item");
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        private void LookForWindowHandle()
        {
            try
            {
                capturingHandle = true;
                uDebugLogAdd("LookForWindowHandle started, updating cursor");
                // Mouse.OverrideCursor = Cursors.Cross;
                Cursor = new Cursor($@"{Directory.GetCurrentDirectory()}\Dependencies\target_small.cur");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleWindowButtonEnabled(object sender)
        {
            try
            {
                uDebugLogAdd("Invoked ToggleWindowButtonEnabled");
                var button = (Button)sender;
                if (button == null)
                {
                    uDebugLogAdd($"Sender button was null: {sender.ToString()}");
                    return;
                }
                if ((string)button.Content == "On")
                {
                    uDebugLogAdd("Button was set to enabled, disabling button and changing color");
                    button.Content = "Off";
                    button.Background = new LinearGradientBrush() { EndPoint = new Point(0.5, 1), StartPoint = new Point(0.5, 0), GradientStops = new GradientStopCollection() { new GradientStop() { Color = (Color)ColorConverter.ConvertFromString("#FF1C1C1C"), Offset = 0 }, new GradientStop() { Color = Defaults.disabledColor, Offset = 1 } } };
                    uDebugLogAdd("Finished disabling button");
                }
                else if ((string)button.Content == "Off")
                {
                    uDebugLogAdd("Button was set to disabled, enabling button and changing color");
                    button.Content = "On";
                    button.Background = new LinearGradientBrush() { EndPoint = new Point(0.5, 1), StartPoint = new Point(0.5, 0), GradientStops = new GradientStopCollection() { new GradientStop() { Color = (Color)ColorConverter.ConvertFromString("#FF1C1C1C"), Offset = 0 }, new GradientStop() { Color = Defaults.enabledColor, Offset = 1 } } };
                    uDebugLogAdd("Finished enabling button");
                }
                else
                {
                    uDebugLogAdd($"button.Content == {button.Content.ToString()}");
                    return;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleWindowButtonChecked(object sender)
        {
            try
            {
                uDebugLogAdd("Invoked ToggleWindowButtonChecked");
                var button = (Button)sender;
                if (button == null)
                {
                    uDebugLogAdd($"Sender button was null: {sender.ToString()}");
                    return;
                }
                if ((string)button.Content == "X")
                {
                    uDebugLogAdd("Button was checked, unchecking button");
                    button.Content = "";
                    uDebugLogAdd("Finished unchecking button");
                }
                else if ((string)button.Content == "")
                {
                    uDebugLogAdd("Button was unchecked, checking button");
                    button.Content = "X";
                    uDebugLogAdd("Finished checking button");
                }
                else
                {
                    uDebugLogAdd($"button.Content wasn't checked or unchecked, content was: {button.Content.ToString()}");
                    return;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void OpenWindowHandleFinder()
        {
            try
            {
                uDebugLogAdd($"Opening window handler info window");
                windowHandleDisplay = new HandleDisplay
                {
                    Topmost = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                uDebugLogAdd($"Window handler info window constructed");
                windowHandleDisplay.Show();
                windowHandleDisplay.Closed += WindowHandleDisplay_Closed;
                uDebugLogAdd($"Window handler info window shown");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void WindowHandleDisplay_Closed(object sender, EventArgs e)
        {
            RefreshSavedWindows();
        }

        private void RevertCursor()
        {
            try
            {
                capturingHandle = false;
                CloseWindowHandleFinder();
                uDebugLogAdd("Reverting cursor");
                Cursor = Cursors.Arrow;
                //Mouse.Capture(null);
                //Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CloseWindowHandleFinder()
        {
            try
            {
                uDebugLogAdd($"Closing Window handler info window, current ref: {windowHandleDisplay.ToString()}");
                windowHandleDisplay.Close();
                uDebugLogAdd($"Closed window handler info window, current ref: {windowHandleDisplay.ToString()}");
                windowHandleDisplay = null;
                uDebugLogAdd("Null'd window handler info window");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CaptureWindowHandle()
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (sender, e) =>
                {
                    try
                    {
                        while (capturingHandle)
                        {
                            uDebugLogAdd("Capturing window handle");
                            IntPtr FoundWindow = ChildWindowFromPoint(WinAPIWrapper.GetMousePosition());
                            uDebugLogAdd($"FoundWindow = {FoundWindow.ToString()}");
                            if (FoundWindow != LastFoundWindow)
                            {
                                uDebugLogAdd($"FoundWindow({FoundWindow.ToString()}) != LastFoundWindow({LastFoundWindow.ToString()})");
                                LastFoundWindow = FoundWindow;
                            }
                            Thread.Sleep(500);
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

        private void DisplayWindowInfo()
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (sender, e) =>
                {
                    try
                    {
                        uDebugLogAdd("Starting DisplayWindowInfo, gathering windowProc");
                        while (capturingHandle)
                        {
                            try
                            {
                                if (LastFoundWindow != IntPtr.Zero)
                                {
                                    Process windowProc = GetProcessIDViaHAndle(LastFoundWindow);
                                    //Process windowProc = Process.GetProcessById(WinAPIWrapper.GetProcessId(LastFoundWindow));
                                    uDebugLogAdd($"Gathered windowProc: {windowProc.Id}, invoking event UpdateWindowInfo");
                                    Events.UpdateWindowInfo(windowProc);
                                }
                                else
                                {
                                    uDebugLogAdd($"Window handle was {IntPtr.Zero}, defaulting window info");
                                    Events.UpdateWindowInfo(null);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogException(ex);
                            }
                            Thread.Sleep(500);
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

        private void DeleteSavedWindowItem()
        {
            try
            {
                WindowItem item = (WindowItem)lbSavedWindows.SelectedItem;
                Toolbox.settings.RemoveWindow(item);
                RefreshSavedWindows();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RefreshSavedWindows()
        {
            try
            {
                lbSavedWindows.ItemsSource = null;
                lbSavedWindows.ItemsSource = Toolbox.settings.WindowList;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private Process GetProcessIDViaHAndle(IntPtr handle)
        {
            return Process.GetProcesses().Single(
            p => p.Id != 0 && p.MainWindowHandle == handle);
        }

        static IntPtr ChildWindowFromPoint(System.Drawing.Point point)
        {
            IntPtr WindowPoint = WinAPIWrapper.WindowFromPoint(point);
            if (WindowPoint == IntPtr.Zero)
                return IntPtr.Zero;

            if (WinAPIWrapper.ScreenToClient(WindowPoint, ref point) == false)
                Toolbox.uAddDebugLog("ScreenToClient failed");

            IntPtr Window = WinAPIWrapper.ChildWindowFromPointEx(WindowPoint, point, 0);
            if (Window == IntPtr.Zero)
                return WindowPoint;

            if (WinAPIWrapper.ClientToScreen(WindowPoint, ref point) == false)
                Toolbox.uAddDebugLog("ClientToScreen failed");

            if (WinAPIWrapper.IsChild(WinAPIWrapper.GetParent(Window), Window) == false)
                return Window;

            // create a list to hold all childs under the point
            ArrayList WindowList = new ArrayList();
            while (Window != IntPtr.Zero)
            {
                System.Drawing.Rectangle rect = WinAPIWrapper.GetWindowRect(Window);
                if (rect.Contains(point))
                    WindowList.Add(Window);
                Window = WinAPIWrapper.GetWindow(Window, (uint)WinAPIWrapper.GetWindow_Cmd.GW_HWNDNEXT);
            }

            // search for the smallest window in the list
            int MinPixel = WinAPIWrapper.GetSystemMetrics((int)WinAPIWrapper.GetSystem_Metrics.SM_CXFULLSCREEN) * WinAPIWrapper.GetSystemMetrics((int)WinAPIWrapper.GetSystem_Metrics.SM_CYFULLSCREEN);
            for (int i = 0; i < WindowList.Count; ++i)
            {
                System.Drawing.Rectangle rect = WinAPIWrapper.GetWindowRect((IntPtr)WindowList[i]);
                int ChildPixel = rect.Width * rect.Height;
                if (ChildPixel < MinPixel)
                {
                    MinPixel = ChildPixel;
                    Window = (IntPtr)WindowList[i];
                }
            }
            return Window;
        }

        private void GetSelectedWindowPosition()
        {
            try
            {
                WindowItem windowItem = (WindowItem)lbSavedWindows.SelectedItem;
                Process foundProc = null;
                foreach (var proc in Process.GetProcesses())
                {
                    if (
                        proc.ProcessName == windowItem.WindowInfo.Name &&
                        proc.MainModule.ModuleName == windowItem.WindowInfo.ModName &&
                        proc.MainWindowTitle == windowItem.WindowInfo.Title &&
                        proc.MainModule.FileName == windowItem.WindowInfo.FileName
                       )
                        foundProc = proc;
                }
                if (foundProc == null)
                    uDebugLogAdd("Running process matching windowItem wasn't found");
                else
                {
                    Toolbox.settings.UpdateWindowLocation(windowItem, foundProc);
                    uDebugLogAdd($"Updated {windowItem.WindowName} windowItem");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ChangeWindowProfiles(WindowProfile profile)
        {
            try
            {
                Toolbox.settings.ChangeWindowProfile(profile);
                lbSavedWindows.ItemsSource = Toolbox.settings.WindowList;
                switch (profile)
                {
                    case WindowProfile.Profile1:
                        btnWinProfile1.BorderBrush = Defaults.ButtonBorderSelected;
                        btnWinProfile2.BorderBrush = Defaults.BaseBorderBrush;
                        btnWinProfile3.BorderBrush = Defaults.BaseBorderBrush;
                        btnWinProfile4.BorderBrush = Defaults.BaseBorderBrush;
                        break;
                    case WindowProfile.Profile2:
                        btnWinProfile2.BorderBrush = Defaults.ButtonBorderSelected;
                        btnWinProfile1.BorderBrush = Defaults.BaseBorderBrush;
                        btnWinProfile3.BorderBrush = Defaults.BaseBorderBrush;
                        btnWinProfile4.BorderBrush = Defaults.BaseBorderBrush;
                        break;
                    case WindowProfile.Profile3:
                        btnWinProfile3.BorderBrush = Defaults.ButtonBorderSelected;
                        btnWinProfile2.BorderBrush = Defaults.BaseBorderBrush;
                        btnWinProfile1.BorderBrush = Defaults.BaseBorderBrush;
                        btnWinProfile4.BorderBrush = Defaults.BaseBorderBrush;
                        break;
                    case WindowProfile.Profile4:
                        btnWinProfile4.BorderBrush = Defaults.ButtonBorderSelected;
                        btnWinProfile2.BorderBrush = Defaults.BaseBorderBrush;
                        btnWinProfile3.BorderBrush = Defaults.BaseBorderBrush;
                        btnWinProfile1.BorderBrush = Defaults.BaseBorderBrush;
                        break;
                }
                UpdateWindowItemOptions();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateWindowItemOptions()
        {

        }

        private void MoveAllWindows()
        {
            try
            {
                foreach (var window in Toolbox.settings.WindowList.ToList())
                {
                    MoveProcessHandle(window);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region Network

        private bool DoesPingSessionExist(string address)
        {
            var exists = false;
            if (lbPingSessions.Visibility == Visibility.Visible)
                foreach (PingEntry item in lbPingSessions.Items)
                {
                    if (item.Address == address)
                        exists = true;
                }
            else
                foreach (PingBasic item in lbPingBasic.Items)
                {
                    if (item.Address == address)
                        exists = true;
                }
            return exists;
        }

        private bool VerifyInput(string input)
        {
            var isCorrect = true;
            isCorrect = !string.IsNullOrWhiteSpace(input);
            isCorrect = !DoesPingSessionExist(input);
            isCorrect = !DoesTraceEntryExist(input);
            uDebugLogAdd($"Verified input, answer: {isCorrect} | input: {input}");
            return isCorrect;
        }

        private bool DoesTraceEntryExist(string input)
        {
            var exists = false;
            foreach (TraceEntry item in lbTraceSessions.Items)
            {
                if (item.DestinationAddress == input)
                    exists = true;
            }
            return exists;
        }

        private void AddPingEntry(string address)
        {
            try
            {
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                foreach (var entry in entries)
                    lbPingSessions.Items.Add(new PingEntry(entry));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void AddBasicPing(string address)
        {
            try
            {
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                foreach (var entry in entries)
                    lbPingBasic.Items.Add(new PingBasic(entry));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void AddTraceEntry(string address)
        {
            try
            {
                TraceLoading();
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                var testRoute = new List<TraceModel>();
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    TraceRoute trace = new TraceRoute();
                    Stopwatch stopwatch = new Stopwatch();
                    foreach (var entry in entries)
                    {
                        uDebugLogAdd($"Starting trace entry {entry}");
                        testRoute.Clear();
                        uDebugLogAdd("Trace starting now");
                        stopwatch.Start();
                        testRoute = trace.TraceRouteAsync(entry).Result;
                        stopwatch.Stop();
                        uDebugLogAdd($"Finished trace: {stopwatch.ElapsedMilliseconds}ms");
                        uDebugLogAdd($"Adding trace entry {entry}");
                        worker.ReportProgress(1);
                        uDebugLogAdd($"Finished adding trace entry {entry}");
                        stopwatch.Reset();
                    }
                    traceLoading = false;
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    if (pe.ProgressPercentage == 1)
                    {
                        lbTraceSessions.Items.Add(new TraceEntry(testRoute));
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TraceLoading()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (ws, we) =>
            {
                traceLoading = true;
                worker.ReportProgress(0);
                while (traceLoading)
                {
                    Thread.Sleep(500);
                    worker.ReportProgress(1);
                }
                worker.ReportProgress(2);
            };
            worker.ProgressChanged += (ps, pe) =>
            {
                if (pe.ProgressPercentage == 0)
                {
                    lblTraceStatus.Visibility = Visibility.Visible;
                    lblTraceStatus.Text = "Tracing";
                }
                if (pe.ProgressPercentage == 1)
                {
                    if (lblTraceStatus.Text != "Tracing.....")
                        lblTraceStatus.Text = $"{lblTraceStatus.Text}.";
                    else
                        lblTraceStatus.Text = "Tracing";
                }
                if (pe.ProgressPercentage == 2)
                {
                    lblTraceStatus.Text = "Done";
                    lblTraceStatus.Visibility = Visibility.Hidden;
                }
            };
            worker.RunWorkerAsync();
        }

        private void MovePingEntry(PingEntry pingEntry, Direction direction)
        {
            try
            {
                var currentIndex = lbPingSessions.Items.IndexOf(pingEntry);
                if (currentIndex < 0 || currentIndex >= lbPingSessions.Items.Count)
                    return;
                var directionInt = 0;
                if (direction == Direction.Up && currentIndex == 0)
                    return;
                if (direction == Direction.Down && currentIndex == lbPingSessions.Items.Count - 1)
                    return;
                if (direction == Direction.Down)
                    directionInt = 1;
                else
                    directionInt = -1;
                var newIndex = lbPingSessions.Items.IndexOf(pingEntry) + directionInt;
                lbPingSessions.Items.Remove(pingEntry);
                lbPingSessions.Items.Insert(newIndex, pingEntry);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MoveTraceEntry(TraceEntry traceEntry, Direction direction)
        {
            try
            {
                var currentIndex = lbTraceSessions.Items.IndexOf(traceEntry);
                if (currentIndex < 0 || currentIndex >= lbTraceSessions.Items.Count)
                    return;
                var directionInt = 0;
                if (direction == Direction.Up && currentIndex == 0)
                    return;
                if (direction == Direction.Down && currentIndex == lbTraceSessions.Items.Count - 1)
                    return;
                if (direction == Direction.Down)
                    directionInt = 1;
                else
                    directionInt = -1;
                var newIndex = lbTraceSessions.Items.IndexOf(traceEntry) + directionInt;
                lbTraceSessions.Items.Remove(traceEntry);
                lbTraceSessions.Items.Insert(newIndex, traceEntry);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LookupAddress(string address)
        {
            try
            {
                lbResolvedAddresses.Items.Clear();
                resolvingDNS = true;
                int entriesToLookFor = 0;
                string resolvedAddress = string.Empty;
                string currentAddress = string.Empty;
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                uDebugLogAdd("Starting address lookup");
                foreach (var entry in entries)
                {
                    entriesToLookFor++;
                    tResolveDNSEntry(entry);
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

        public static IEnumerable<IPAddress> GetTraceRoute(string hostname)
        {
            // following are the defaults for the "traceroute" command in unix.
            const int timeout = 10000;
            const int maxTTL = 30;
            const int bufferSize = 32;

            byte[] buffer = new byte[bufferSize];
            new Random().NextBytes(buffer);
            Ping pinger = new Ping();

            for (int ttl = 1; ttl <= maxTTL; ttl++)
            {
                PingOptions options = new PingOptions(ttl, true);
                PingReply reply = pinger.Send(hostname, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {
                    // Success means the tracert has completed
                    yield return reply.Address;
                    break;
                }
                if (reply.Status == IPStatus.TtlExpired)
                {
                    // TtlExpired means we've found an address, but there are more addresses
                    yield return reply.Address;
                    continue;
                }
                if (reply.Status == IPStatus.TimedOut)
                {
                    // TimedOut means this ttl is no good, we should continue searching
                    continue;
                }

                // if we reach here, it's a status we don't recognize and we should exit.
                break;
            }
        }

        private void SwapPingGrids()
        {
            try
            {
                if (Toolbox.settings.BasicPing)
                {
                    lbPingSessions.Visibility = Visibility.Hidden;
                    Toolbox.AnimateListBox(lbPingBasic, Defaults.PingLBasicPIn);
                    foreach (PingEntry entry in lbPingSessions.Items)
                    {
                        lbPingBasic.Items.Add(new PingBasic(entry.Address));
                    }
                    lbPingSessions.Items.Clear();
                }
                else
                {
                    lbPingSessions.Visibility = Visibility.Visible;
                    lbPingBasic.Visibility = Visibility.Hidden;
                    Toolbox.AnimateListBox(lbPingSessions, Defaults.PingLBasicPIn);
                    foreach (PingBasic entry in lbPingBasic.Items)
                    {
                        lbPingSessions.Items.Add(new PingEntry(entry.Address));
                    }
                    lbPingBasic.Items.Clear();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #endregion

        #region Threaded/Async Methods

        public void tCheckAudioDevices()
        {
            uDebugLogAdd("Starting audio check thread");
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.ProgressChanged += (sender, e) =>
            {
                if (e.ProgressPercentage == 1)
                {
                    uDebugLogAdd($"Progress is {e.ProgressPercentage}, starting audio list refresh");
                    RefreshAudioList();
                    uStatusUpdate("Audio Playback Devices changed, list refreshed");
                    uDebugLogAdd("Changing progress back to 0");
                    worker.ReportProgress(0);
                }
            };
            worker.DoWork += (sender, e) =>
            {
                uDebugLogAdd("Started auto audio device refresh worker");
                while (true)
                {
                    Thread.Sleep(5000);
                    uDebugLogAdd("Comparing previous ADL with current ADL");
                    var tmpADL = Audio.GetAudioDevices();
                    if (Interfaces.AudioMain.EndpointAudioDeviceList.Count != tmpADL.Count)
                    {
                        uDebugLogAdd("Current ADL has changed from previous, changing progress to 1");
                        worker.ReportProgress(1);
                    }
                    else
                        uDebugLogAdd("Current ADL hasn't changed, not changing progress");
                    uDebugLogAdd("Finished worker refresh loop");
                }
            };
            worker.RunWorkerAsync();
            uDebugLogAdd("Audio check thread started");
        }

        public void tSaveSettingsAuto()
        {
            try
            {
                uDebugLogAdd("Starting auto save worker");
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.ProgressChanged += (sender, e) =>
                {
                    try
                    {
                        if (e.ProgressPercentage == 1)
                            SaveSettings();
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
                        while (true)
                        {
                            Thread.Sleep(TimeSpan.FromMinutes(5));
                            uDebugLogAdd("5min passed, now auto saving settings");
                            worker.ReportProgress(1);
                            worker.ReportProgress(0);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                };
                worker.RunWorkerAsync();
                uDebugLogAdd("Auto save worker started");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void tUpdateSettings(SettingsUpdate settingsUpdate, string value = null)
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                        //switch (settingsUpdate)
                        //{
                        //    case SettingsUpdate.PingCount:
                        //        break;
                        //    case SettingsUpdate.PingDTFormat:
                        //        worker.ReportProgress(2);
                        //        break;
                        //    case SettingsUpdate.TextBoxAction:
                        //        worker.ReportProgress(3);
                        //        break;
                        //}

                        // Pingcount settings
                        var num = 0;
                    if (int.TryParse(value, out num))
                        worker.ReportProgress(1);
                    else
                    {
                        if (!settingsBadAlerted)
                        {
                            worker.ReportProgress(99);
                            settingsBadAlerted = true;
                            Thread.Sleep(TimeSpan.FromSeconds(5));
                            settingsBadAlerted = false;
                        }
                    }
                        // Update All Settings
                        worker.ReportProgress(1);

                        // Flip ping basic if checked
                        if (settingsUpdate == SettingsUpdate.BasicPing)
                        worker.ReportProgress(2);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.ProgressChanged += (psend, pe) =>
            {
                try
                {
                    switch (pe.ProgressPercentage)
                    {
                        case 1:
                            uDebugLogAdd("Starting settings update");
                            uDebugLogAdd("SETUPDATE: ping chart");
                                // Set pingchart settings
                                Toolbox.settings.PingChartLength = int.Parse(txtSetNetPingCount.Text);
                            foreach (PingEntry entry in lbPingSessions.Items)
                            {
                                entry.ChartLength = Toolbox.settings.PingChartLength;
                            }
                            uDebugLogAdd("SETUPDATE: DTFormat");
                                // Set Date/Time Format settings
                                if (cmbxSetNetDTFormat.Text == "Seconds")
                                Toolbox.settings.DateTimeFormat = DTFormat.Sec;
                            else if (cmbxSetNetDTFormat.Text == "Minutes")
                                Toolbox.settings.DateTimeFormat = DTFormat.Min;
                            else if (cmbxSetNetDTFormat.Text == "Hours")
                                Toolbox.settings.DateTimeFormat = DTFormat.Hours;
                            uDebugLogAdd("SETUPDATE: Enter action");
                                // Set network textbox enter action
                                if (cmbxSetNetTextboxAction.Text == "DNSLookup")
                                Toolbox.settings.ToolboxEnterAction = EnterAction.DNSLookup;
                            else if (cmbxSetNetTextboxAction.Text == "Ping")
                                Toolbox.settings.ToolboxEnterAction = EnterAction.Ping;
                            else if (cmbxSetNetTextboxAction.Text == "Trace")
                                Toolbox.settings.ToolboxEnterAction = EnterAction.Trace;
                            uDebugLogAdd("SETUPDATE: Basic ping");
                                // Set basic ping
                                if (chkNetBasicPing.IsChecked == true)
                                Toolbox.settings.BasicPing = true;
                            else
                                Toolbox.settings.BasicPing = false;
                            break;
                        case 2:
                            uDebugLogAdd("Starting ping grid swap from settings update");
                            SwapPingGrids();
                            break;
                        case 99:
                            ShowNotification("Incorrect format entered");
                            break;
                    }
                    uDebugLogAdd("Finished settings update");
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
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
                    lbResolvedAddresses.Items.Add(resolvedEntry);
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
                if (pe.ProgressPercentage == 1)
                {
                    if (lblNetResolved.Text == "NSlookup:")
                        lblNetResolved.Text = ".";
                    else if (lblNetResolved.Text != ".....")
                        lblNetResolved.Text = $"{lblNetResolved.Text}.";
                    else
                        lblNetResolved.Text = ".";
                }
                if (pe.ProgressPercentage == 2)
                {
                    lblNetResolved.Text = "NSlookup:";
                    resolvedEntries = 0;
                    resolvingDNS = false;
                }
            };
            worker.RunWorkerAsync();
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
                string executable = "Panacea.exe";
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        Toolbox.settings.CurrentVersion = GetVersionNumber();
                        uDebugLogAdd($"Current Version: {Toolbox.settings.CurrentVersion}");
                        Task t = GetUpdate(executable);
                        t.Start();
                        while (!t.IsCompleted)
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
                                ShowNotification("A new version is available, update when ready");
                                Toolbox.settings.UpdateAvailable = true;
                                btnMenuUpdate.IsEnabled = true;
                                uDebugLogAdd("Enabled update button");
                            }
                            else
                                uDebugLogAdd($"Current version is the same or newer than release: [c]{Toolbox.settings.CurrentVersion} [p]{Toolbox.settings.ProductionVersion}");
                            SaveSettings();
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

        public async Task GetUpdate(string exe)
        {
            try
            {
                var releases = await gitClient.Repository.Release.GetAll("rwobig93", "Panacea");
                var recentRelease = releases.ToList().FindAll(x => x.Prerelease == true).OrderBy(x => x.TagName).First();
                Version prodVersion = new Version(recentRelease.TagName);
                Toolbox.settings.ProductionVersion = prodVersion;
                uDebugLogAdd($"ProdVer: {Toolbox.settings.ProductionVersion}");
                Toolbox.settings.ProductionURI = $@"{Defaults.GitUpdateURIBase}/{recentRelease.TagName}/{exe}";
                uDebugLogAdd($"URI: {Toolbox.settings.ProductionURI}");
                uDebugLogAdd($"Finished getting github recent version");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion
    }
}
