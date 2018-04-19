﻿using Panacea.Classes;
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
            DataContext = this;
            uDebugLogAdd(string.Format("{0}##################################### Application Start #####################################{0}", Environment.NewLine));
            SetupAppFiles();
            DeSerializeSettings();
            SetDefaultSettings();
            SetWindowLocation();
            SubscribeToEvents();
            SetupAudioDeviceList();
            tSaveSettingsAuto();
            InitializeMenuGrids();
#if DEBUG
            debugMode = true;
#endif
        }

        #region Globals

        private List<string> notifications = new List<string>();
        public static ObservableCollection<WindowItem> savedWindows = new ObservableCollection<WindowItem>();
        private string logDir = $@"{Directory.GetCurrentDirectory()}\Logs\";
        private string confDir = $@"{Directory.GetCurrentDirectory()}\Config\";
        private string exDir = $@"{Directory.GetCurrentDirectory()}\Logs\Exceptions\";
        private MMDevice selectedAudioEndpoint = null;
        private IntPtr LastFoundWindow = IntPtr.Zero;
        private HandleDisplay windowHandleDisplay = null;
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
            if (e.ChangedButton == MouseButton.Left)
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
            SerializeSettings();
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

        private void btnTarget_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LookForWindowHandle(e);
            OpenWindowHandleFinder();
        }

        private void btnTarget_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RevertCursor();
            CloseWindowHandleFinder();
            SaveSelectedWindow();
        }

        private void btnTarget_MouseMove(object sender, MouseEventArgs e)
        {
            CaptureWindowHandle();
        }

        private void BtnDeleteWindowItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteSavedWindowItem();
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
                AddPingEntry(address);
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
                foreach (PingEntry entry in lbPingSessions.Items)
                    entry.TogglePing(glblPinging);
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
                    AddPingEntry(address);
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
                    ToggleListBox(lbTraceSessions);
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

        #endregion

        #region grdSettings

        private void txtSetNetPingCount_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate();
        }

        private void cmbxSetNetDTFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartSettingsUpdate();
        }

        #endregion

        #endregion

        #region Methods

        #region General

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
                    uDebugLogAdd("Created missing config direcotry");
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
                    Toolbox.AnimateListBox(lb, Defaults.TraceLBIn);
                }
                else
                {
                    Toolbox.AnimateListBox(lb, Defaults.TraceLBOut);
                    lb.Visibility = Visibility.Hidden;
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
                txtSetNetPingCount.Text = Toolbox.settings.PingChartLength.ToString();
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

        private void StartSettingsUpdate()
        {

            try
            {
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
                            settingsTimer = 5;
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
                            tUpdateSettings(SettingsUpdate.PingCount, txtSetNetPingCount.Text);
                            tUpdateSettings(SettingsUpdate.PingDTFormat);
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

        private void GetSelectedWindowPosition()
        {
            try
            {
                uDebugLogAdd("Invoked GetSelectedWindowPosition");
                if (lbSavedWindows.SelectedItem != null)
                {
                    uDebugLogAdd("Selected item isn't null");
                    WinAPIWrapper.RECT location = new WinAPIWrapper.RECT();
                    var windowItem = ((WindowItem)lbSavedWindows.SelectedItem);
                    if (windowItem == null)
                    {
                        uDebugLogAdd("Window item from saved windows list is null");
                        return;
                    }
                    uDebugLogAdd("Getting current process location");
                    var proc = WinAPIWrapper.GetProcessFromWinItem(windowItem);
                    if (proc != null)
                        WinAPIWrapper.GetWindowRect(proc.Handle, ref location);
                    else
                    {
                        uDebugLogAdd("Process not found for currently selected saved window");
                        uStatusUpdate($"Process not found from selected saved process");
                        return;
                    }
                    uDebugLogAdd("Updating current entry with new process location");
                    var entry = savedWindows.ToList().Find(x => x.WindowInfo.PrivateID == windowItem.WindowInfo.PrivateID).WindowInfo;
                    entry.XValue = location.Left;
                    entry.YValue = location.Top;
                    entry.Width = location.Right - location.Left;
                    entry.Height = location.Bottom - location.Top;
                    uDebugLogAdd("Finished updating current entry process location");
                }
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
                    var proc = WinAPIWrapper.GetProcessFromWinItem(selectedWindow);
                    if (proc != null)
                        WinAPIWrapper.MoveWindow(proc.Handle, selectedWindow.WindowInfo.XValue, selectedWindow.WindowInfo.YValue, selectedWindow.WindowInfo.Width, selectedWindow.WindowInfo.Height, true);
                    else
                    {
                        uDebugLogAdd("Current process not found for selected item in saved windows list");
                        uStatusUpdate("Running process not found for selected process");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LookForWindowHandle(MouseButtonEventArgs e)
        {
            try
            {
                uDebugLogAdd("LookForWindowHandle started");
                if (e.ChangedButton == MouseButton.Left)
                {
                    uDebugLogAdd("Left mouse button down, updating cursor");
                    Mouse.OverrideCursor = new Cursor("target_small.cur");
                }
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
                uDebugLogAdd($"Opening window handler info window, current ref: {windowHandleDisplay.ToString()}");
                windowHandleDisplay = new HandleDisplay
                {
                    Topmost = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                uDebugLogAdd($"Window handler info window constructed, current ref: {windowHandleDisplay.ToString()}");
                windowHandleDisplay.Show();
                uDebugLogAdd($"Window handler info window shown, current ref: {windowHandleDisplay.ToString()}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RevertCursor()
        {
            try
            {
                uDebugLogAdd("Reverting cursor");
                uDebugLogAdd($"Cursor wasn't Arrow, it was {Cursor.ToString()}");
                Mouse.OverrideCursor = null;
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
                uDebugLogAdd($"Null'd window handler info window, current ref: {windowHandleDisplay.ToString()}");
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
                uDebugLogAdd("Capturing window handle");
                IntPtr FoundWindow = WinAPIWrapper.WindowFromPoint(WinAPIWrapper.GetMousePosition());
                uDebugLogAdd($"FoundWindow = {FoundWindow.ToString()}");
                if (FoundWindow != LastFoundWindow)
                {
                    uDebugLogAdd($"FoundWindow({FoundWindow.ToString()}) != LastFoundWindow({LastFoundWindow.ToString()})");
                    LastFoundWindow = FoundWindow;
                }
                DisplayWindowInfo();
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
                uDebugLogAdd("Starting DisplayWindowInfo, gathering windowProc");
                Process windowProc = Process.GetProcessById(WinAPIWrapper.GetProcessId(LastFoundWindow));
                uDebugLogAdd($"Gathered windowProc: {windowProc.Id}, invoking event UpdateWindowInfo");
                Events.UpdateWindowInfo(windowProc);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SaveSelectedWindow()
        {
            try
            {
                var windowItem = WindowItem.Create(Process.GetProcessById(WinAPIWrapper.GetProcessId(LastFoundWindow)));
                bool duplicate = WindowItem.DoesDuplicateExist(windowItem);
                if (!duplicate)
                    savedWindows.Add(windowItem);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DeleteSavedWindowItem()
        {
            uStatusUpdate("Feature not yet implemented");
        }

        #endregion

        #region Network
        
        private bool DoesPingSessionExist(ListBox lb, string address)
        {
            var exists = false;
            foreach (PingEntry item in lb.Items)
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
            isCorrect = !DoesPingSessionExist(lbPingSessions, input);
            uDebugLogAdd($"Verified input, answer: {isCorrect} | input: {input}");
            return isCorrect;
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

        #endregion

        #endregion

        #region Threaded Methods

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
                    switch (settingsUpdate)
                    {
                        case SettingsUpdate.PingCount:
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
                            break;
                        case SettingsUpdate.PingDTFormat:
                            worker.ReportProgress(2);
                            break;
                    }
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
                            Toolbox.settings.PingChartLength = int.Parse(txtSetNetPingCount.Text);
                            foreach (PingEntry entry in lbPingSessions.Items)
                            {
                                entry.ChartLength = Toolbox.settings.PingChartLength;
                            }
                            break;
                        case 2:
                            if (cmbxSetNetDTFormat.Text == "Seconds")
                                Toolbox.settings.DateTimeFormat = DTFormat.Sec;
                            else if (cmbxSetNetDTFormat.Text == "Minutes")
                                Toolbox.settings.DateTimeFormat = DTFormat.Min;
                            else if (cmbxSetNetDTFormat.Text == "Hours")
                                Toolbox.settings.DateTimeFormat = DTFormat.Hours;
                            break;
                        case 99:
                            ShowNotification("Incorrect format entered");
                            break;
                    }
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
                            uDebugLogAdd($"DNS entry hostname isn't empty, resolved to: {resolvedEntry.HostName}");
                        }
                    }
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
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion
    }
}