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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using Newtonsoft.Json;
using Panacea.Windows;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using NAudio.CoreAudioApi;
using System.Collections;
using System.Reflection;
using System.Net.Sockets;
using System.Net.Http;
using System.IO.Compression;
using System.Net.Mail;
using MahApps.Metro.Controls;
using Octokit;
using System.Windows.Media.Imaging;

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
        private MMDevice selectedAudioEndpoint = null;
        private IntPtr LastFoundWindow = IntPtr.Zero;
        private Point mouseStartPoint = new Point(0, 0);
        private CurrentDisplay currentDisplay = null;
        private bool audioRefreshing = false;
        private bool notificationPlaying = false;
        private bool aboutPlaying = false;
        private bool glblPinging = true;
        private int settingsTimer = 5;
        private bool settingsSaveVerificationInProgress = false;
        private bool resolvingDNS = false;
        private int resolvedEntries = 0;
        private bool settingsBadAlerted = false;
        private bool resizingNetGrid = false;
        private bool capturingHandle = false;
        private bool startingUp = false;
        private bool _updatingUI = false;

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
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            //this.Close();
            HideWinMainInBackground();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                uDebugLogAdd("Minimizing window");
                this.WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowInfoWindow();
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
                ToggleMenuScrollviewer(scrollSettings);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnMenuUpdate_Click(object sender, RoutedEventArgs e)
        {
            Director.Main.UpdateApplication();
        }

        private void Events_UpdateDebugStatus(DebugUpdateArgs args)
        {
            try
            {
                if (Director.Main.DebugMode) Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { txtStatus.AppendText($"{Environment.NewLine}{DateTime.Now.ToLocalTime().ToString("MM-dd-yy")}_{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {args.DebugType.ToString()}: {args.LogUpdate}"); } catch (Exception ex) { LogException(ex); } });
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            TestOpenPort();
        }

        private void TestOpenPort()
        {
            try
            {
                var google = Network.IsPortOpen("google.com", 443, TimeSpan.FromSeconds(1));
                var microsoft = Network.IsPortOpen("microsoft.com", 443, TimeSpan.FromSeconds(1));
                var apple = Network.IsPortOpen("apple.com", 443, TimeSpan.FromSeconds(1));
                Director.Main.ShowNotification($"Port open summary: [g] {google} [m] {microsoft} [a] {apple}");
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
                string verNum = Director.Main.GetVersionNumber().ToString();
                Actions.CopyToClipboard(verNum, $"Copied version to clipboard: {verNum}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnShrug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string clip = @"¯\_(ツ)_/¯";
                Actions.CopyToClipboard(clip, "You now have the almighty shrug on your clipboard!");
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
            Director.Main.OpenWindowHandleFinder();
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
                if (VerifyIfMacAddress(address))
                {
                    uDebugLogAdd($"NetAddress value was found to be a Mac Address, opening Macpopup: {address}");
                    OpenMacAddressWindow(address);
                    return;
                }
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
                if (!Toolbox.settings.PingTypeChosen)
                    PromptForPingPreference();
                var address = txtNetAddress.Text;
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
                    if (!VerifyInput(entry))
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
                lbItem.Destroy();
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

        private void BtnBasicPingEntryClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lbItem = (BasicPingEntry)e.OriginalSource.GetType().GetProperty("DataContext").GetValue(e.OriginalSource, null);
                lbBasicPingSessions.Items.Remove(lbItem);
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

        private void btnNetPingToggleAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (glblPinging == true)
                {
                    glblPinging = false;
                    btnNetPingToggleAll.Content = @"▶ All";
                }
                else
                {
                    glblPinging = true;
                    btnNetPingToggleAll.Content = @"❚❚ All";
                }
                PingStat stat = PingStat.Unknown;
                if (glblPinging)
                    stat = PingStat.Active;
                else
                    stat = PingStat.Paused;
                if (Toolbox.settings.BasicPing)
                    foreach (BasicPingEntry entry in lbBasicPingSessions.Items)
                        entry.TogglePing(stat);
                else
                    foreach (PingEntry entry in lbPingSessions.Items)
                        entry.TogglePing(stat);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnNetPingCloseAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Toolbox.settings.BasicPing)
                {
                    if (lbBasicPingSessions.Items.Count <= 0)
                    {
                        ShowNotification("There currently aren't any pings in progress");
                        return;
                    }
                }
                else
                {
                    if (lbPingSessions.Items.Count <= 0)
                    {
                        ShowNotification("There currently aren't any pings in progress");
                        return;
                    }
                }
                var response = Prompt.YesNo("Are you sure you want to destroy all current pings?");
                if (response == Prompt.PromptResponse.No)
                {
                    uDebugLogAdd($"User answered {response.ToString()} to destroying all pings, canceling");
                    return;
                }
                else
                    uDebugLogAdd($"User answered {response.ToString()} to destroying all pings, starting masacre");
                if (Toolbox.settings.BasicPing)
                {
                    foreach (BasicPingEntry entry in lbBasicPingSessions.Items.Cast<BasicPingEntry>().ToList())
                    {
                        lbBasicPingSessions.Items.Remove(entry);
                        entry.Destroy();
                    }
                }
                else
                {
                    foreach (PingEntry entry in lbPingSessions.Items.Cast<PingEntry>().ToList())
                    {
                        lbPingSessions.Items.Remove(entry);
                        entry.Destroy();
                    }
                }
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
                    var value = txtNetAddress.Text;
                    if (VerifyIfMacAddress(value))
                    {
                        uDebugLogAdd($"NetAddress value was found to be a Mac Address, opening Macpopup: {value}");
                        OpenMacAddressWindow(value);
                        return;
                    }
                    switch (Toolbox.settings.ToolboxEnterAction)
                    {
                        case EnterAction.DNSLookup:
                            btnNetLookup_Click(sender, e);
                            break;
                        case EnterAction.Ping:
                            btnNetPing_Click(sender, e);
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
                        Actions.CopyToClipboard(textBlock.Text);
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
                        Actions.CopyToClipboard($"{dnsEntry.IPAddress} = {dnsEntry.HostName}");
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

        private void btnNetNSLookupClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lbResolvedAddresses.Items.Clear();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnNetMAC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var address = txtNetAddress.Text;
                if (VerifyIfMacAddress(address))
                {
                    uDebugLogAdd($"NetAddress value was found to be a Mac Address, opening Macpopup: {address}");
                    OpenMacAddressWindow(address);
                }
                else
                    OpenMacAddressWindow();
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

        private void chkSettingsBeta_Click(object sender, RoutedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.BetaCheck);
        }

        private void btnDefaultConfig_Click(object sender, RoutedEventArgs e)
        {
            Actions.ResetConfigToDefault();
        }

        private void btnSendDiag_Click(object sender, RoutedEventArgs e)
        {
            SendDiagnostics();
        }

        private void btnViewChangeLog_Click(object sender, RoutedEventArgs e)
        {
            ShowChangelog();
        }

        private void TxtWindowProfile_KeyUp(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void TxtStartProfile_KeyUp(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void chkSettingsStartup_Click(object sender, RoutedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.WinStartup);
        }

        private void chkSetGenUtilBar_Click(object sender, RoutedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.UtilBar);
        }

        private void ChkSettingsStartMinimized_Click(object sender, RoutedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.StartMin);
        }

        private void ComboGenWinPref_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.WinStartup);
        }

        #endregion

        #endregion

        #region Methods

        #region General

        private void Startup()
        {
            if (Director.Main.DebugMode)
                BtnTest.Visibility = Visibility.Visible;
            DataContext = this;
            startingUp = true;
            SubscribeToEvents();
            UpdateSettingsUI();
            SetWindowLocation();
            SetupAudioDeviceList();
            tDisplayWatcher();
            InitializeMenuGrids();
            FinishStartup();
        }

        #region Operational

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

        public void ShowNotification(string notification)
        {
            try
            {
                uStatusUpdate(string.Format("Notification: {0}", notification));
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

        public void uStatusUpdate(string _status)
        {
            try
            {
                if (!Director.Main.DebugMode) Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { txtStatus.AppendText($"{Environment.NewLine}{DateTime.Now.ToLocalTime().ToLongTimeString()} :: {_status}"); } catch (Exception ex) { LogException(ex); } });
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
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                        {
                            try
                            {
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
                            }
                            catch (Exception ex) { LogException(ex); }
                        });
                        break;
                    case 2:
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { grdNotification.BeginAnimation(FrameworkElement.HeightProperty, slideOut); } catch (Exception ex) { LogException(ex); } });
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

        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            try
            {
                Toolbox.uAddDebugLog($"DESKWIN: {_log}", _type, caller);
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

        public object GetPropertyValue(string propertyName)
        {
            //returns value of property Name
            return this.GetType().GetProperty(propertyName).GetValue(this, null);
        }

        private void SubscribeToEvents()
        {
            try
            {
                // Placeholder
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

        private void AddToWindowsStartup(bool startup = true)
        {
            try
            {
                if (startup)
                {
                    uDebugLogAdd($"Add to windows startup is {startup}, adding registry key");
                    Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    key.SetValue("Panacea", $@"{Director.Main.CurrentDirectory}\Panacea.exe");
                    uDebugLogAdd("Successfully added to windows startup");
                    ShowNotification("Panacea set to launch on Windows startup");
                }
                else
                {
                    uDebugLogAdd($"Add to windows startup is {startup}, removing registry key");
                    Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    key.DeleteValue("Panacea", false);
                    uDebugLogAdd("Successfully removed from windows startup");
                    ShowNotification("Panacea set to NOT launch on Windows startup");
                }
            }
            catch (ArgumentNullException ane) { uDebugLogAdd($"Argument was null when writing regkey for startup: [{ane.ParamName}] {ane.Message}", DebugType.FAILURE); ShowNotification("Unable to add open on windows startup, an error occured"); }
            catch (ObjectDisposedException ode) { uDebugLogAdd($"Object was disposed when writing regkey for startup: [{ode.ObjectName}] {ode.Message}"); ShowNotification("Unable to add open on windows startup, an error occured"); }
            catch (System.Security.SecurityException se) { uDebugLogAdd($"Security Exception occured when writing regkey for startup: [{se.PermissionType}][{se.PermissionState}][{se.Method}] {se.Message}"); ShowNotification("Unable to add open on windows startup, an error occured"); }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyIfWindowIsOffscreen()
        {
            try
            {
                if (this.Left < currentDisplay.LeftMostWorkArea)
                {
                    uDebugLogAdd($"Panacea window was is the screen to the left: [l]{this.Left} [wal]{currentDisplay.LeftMostWorkArea}");
                    this.Left = currentDisplay.LeftMostWorkArea;
                    uDebugLogAdd($"Moved Panacea window back on screen: [l]{this.Left} [wal]{currentDisplay.LeftMostWorkArea}");
                }
                else if (this.Left + rectTitle.ActualWidth > currentDisplay.RightMostWorkArea)
                {
                    uDebugLogAdd($"Panacea window is off the screen to the right: [l]{this.Left + rectTitle.ActualWidth} [war]{currentDisplay.RightMostWorkArea}");
                    this.Left = currentDisplay.RightMostWorkArea - rectTitle.ActualWidth;
                    uDebugLogAdd($"Moved Panacea window back on screen: [l]{this.Left + rectTitle.ActualWidth} [war]{currentDisplay.RightMostWorkArea}");
                }
                if (this.Top < currentDisplay.TopMostWorkArea)
                {
                    uDebugLogAdd($"Panacea window is off the screen up top: [t]{this.Top} [wat]{currentDisplay.TopMostWorkArea}");
                    this.Top = currentDisplay.TopMostWorkArea;
                    uDebugLogAdd($"Moved Panacea window back on screen: [t]{this.Top} [wat]{currentDisplay.TopMostWorkArea}");
                }
                else if (this.Top + rectTitle.ActualHeight > currentDisplay.BottomMostWorkArea)
                {
                    uDebugLogAdd($"Panacea window is off the screen down low: [t]{this.Top + rectTitle.ActualHeight} [wab]{currentDisplay.BottomMostWorkArea}");
                    this.Top = currentDisplay.BottomMostWorkArea - rectTitle.ActualHeight;
                    uDebugLogAdd($"Moved Panacea window back on screen: [t]{this.Top + rectTitle.ActualHeight} [wab]{currentDisplay.BottomMostWorkArea}");
                }
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
                if (currentDisplay == null)
                {
                    currentDisplay = new CurrentDisplay();
                    uDebugLogAdd("Current display was null, created new current display");
                }
                currentDisplay.Displays.Clear();
                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    currentDisplay.Displays.Add(Display.ConvertFromScreen(screen));
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void HideWinMainInBackground()
        {
            try
            {
                this.ShowInTaskbar = false;
                this.Hide();
                uDebugLogAdd("MainWindow has been hidden in the background");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void BringWinMainBackFromTheVoid()
        {
            try
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
                uDebugLogAdd("MainWindow has been brought back from the void");
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
                btnWinUpdateManual.Visibility = Visibility.Visible;
                ShowNotification("Update Available!");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void TriggerBetaReleaseUI()
        {
            try
            {
                uDebugLogAdd("Triggering beta release UI label");
                txtBetaLabel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region Display

        private void ToggleMenuGrid(Grid grid)
        {
            try
            {
                uDebugLogAdd($"Call ToggleMenuGrid({grid.Name})");
                //if (grid.Margin != Defaults.MainGridIn)
                var displayArea = GetWindowDisplayArea();
                if (grid.Margin != displayArea)
                {
                    grid.Visibility = Visibility.Visible;
                    Toolbox.AnimateGrid(grid, displayArea);
                }
                else
                {
                    Toolbox.AnimateGrid(grid, GetWindowHiddenArea());
                    grid.Visibility = Visibility.Hidden;
                }
                HideUnusedMenuGrids(grid);
                HideUnusedScrollviewers();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleMenuScrollviewer(ScrollViewer scrollViewer)
        {
            try
            {
                uDebugLogAdd($"Call ToggleMenuGrid({scrollViewer.Name})");
                var displayArea = GetWindowDisplayArea();
                if (scrollViewer.Margin != displayArea)
                {
                    scrollViewer.Visibility = Visibility.Visible;
                    Toolbox.AnimateScrollviewer(scrollViewer, displayArea);
                }
                else
                {
                    Toolbox.AnimateScrollviewer(scrollViewer, GetWindowHiddenArea());
                    scrollViewer.Visibility = Visibility.Hidden;
                }
                HideUnusedMenuGrids();
                HideUnusedScrollviewers(scrollViewer);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void HideUnusedScrollviewers(ScrollViewer scrollViewer = null)
        {
            try
            {
                if (scrollViewer == null)
                    uDebugLogAdd("Call HideUnusedScrollviewers(null)");
                else
                    uDebugLogAdd($"Call HideUnusedScrollviewers({scrollViewer.Name})");
                if (scrollViewer != scrollSettings)
                {
                    scrollSettings.Visibility = Visibility.Hidden;
                    Toolbox.AnimateScrollviewer(scrollSettings, GetWindowHiddenArea());
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void HideUnusedMenuGrids(Grid grid = null)
        {
            try
            {
                if (grid == null)
                    uDebugLogAdd($"Call HideUnusedMenuGrids(null)");
                else
                    uDebugLogAdd($"Call HideUnusedMenuGrids({grid.Name})");
                if (grid != grdAudio)
                {
                    grdAudio.Visibility = Visibility.Hidden;
                    Toolbox.AnimateGrid(grdAudio, GetWindowHiddenArea());
                }
                if (grid != grdWindows)
                {
                    grdWindows.Visibility = Visibility.Hidden;
                    Toolbox.AnimateGrid(grdWindows, GetWindowHiddenArea());
                }
                if (grid != grdNetwork)
                {
                    grdNetwork.Visibility = Visibility.Hidden;
                    Toolbox.AnimateGrid(grdNetwork, GetWindowHiddenArea());
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private Thickness GetWindowDisplayArea()
        {
            Thickness displayArea = new Thickness();
            try
            {
                uDebugLogAdd("Getting window display area");
                displayArea = grdMain.Margin;
                uDebugLogAdd($"displayArea <Before>: [T]{displayArea.Top} [L]{displayArea.Left} [B]{displayArea.Bottom} [R]{displayArea.Right}");
                displayArea.Top = displayArea.Top + rectTitle.ActualHeight;
                displayArea.Left = displayArea.Left + pnlMenu.ActualWidth;
                displayArea.Bottom = displayArea.Bottom + txtStatus.ActualHeight;
                uDebugLogAdd($"displayArea <After>: [T]{displayArea.Top} [L]{displayArea.Left} [B]{displayArea.Bottom} [R]{displayArea.Right}");
                return displayArea;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            return displayArea;
        }

        private Thickness GetWindowHiddenArea()
        {
            Thickness hiddenArea = new Thickness();
            try
            {
                uDebugLogAdd("Getting window hidden area");
                hiddenArea = grdMain.Margin;
                uDebugLogAdd($"hiddenArea <Before>: [T]{hiddenArea.Top} [L]{hiddenArea.Left} [B]{hiddenArea.Bottom} [R]{hiddenArea.Right}");
                hiddenArea.Left = 689; // 120
                //hiddenArea.Top = 60;
                //hiddenArea.Bottom = 60;
                hiddenArea.Right = -615; // 60
                uDebugLogAdd($"hiddenArea <After>: [T]{hiddenArea.Top} [L]{hiddenArea.Left} [B]{hiddenArea.Bottom} [R]{hiddenArea.Right}");
                return hiddenArea;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            return hiddenArea;
        }

        private void InitializeMenuGrids()
        {
            try
            {
                HideUnusedMenuGrids();
                //HideElementsThatNeedHidden();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void HideElementsThatNeedHidden()
        {
            //Retained for future use
            throw new NotImplementedException();
        }

        private void ShowInfoWindow()
        {
            try
            {
                HelpMenu menu = HelpMenu.DesktopWindow;
                if (grdAudio.Visibility == Visibility.Visible)
                    menu = HelpMenu.AudioMenu;
                else if (grdWindows.Visibility == Visibility.Visible)
                    menu = HelpMenu.WindowsMenu;
                else if (grdNetwork.Visibility == Visibility.Visible)
                    menu = HelpMenu.NetworkMenu;
                else
                    menu = HelpMenu.DesktopWindow;
                Director.Main.OpenInfoWindow(menu);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
        #endregion

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

        public void UpdateSettingsUI()
        {
            try
            {
                uDebugLogAdd("Starting settings UI update");
                uDebugLogAdd("SettingsNET: working on ping settings");
                txtSetNetPingCount.Text = Toolbox.settings.PingChartLength.ToString();
                chkNetBasicPing.IsChecked = Toolbox.settings.BasicPing;
                VerifyPingGrids();
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
                cmbxSetNetTextboxAction.Items.Add(lookup);
                cmbxSetNetTextboxAction.Items.Add(ping);
                switch (Toolbox.settings.ToolboxEnterAction)
                {
                    case EnterAction.DNSLookup:
                        cmbxSetNetTextboxAction.SelectedItem = lookup;
                        break;
                    case EnterAction.Ping:
                        cmbxSetNetTextboxAction.SelectedItem = ping;
                        break;
                    default:
                        cmbxSetNetTextboxAction.SelectedItem = lookup;
                        break;
                }
                uDebugLogAdd("SettingsWIN: working on window process settings");
                txtWindowProfileName1.Text = Toolbox.settings.WindowProfileName1;
                txtWindowProfileName2.Text = Toolbox.settings.WindowProfileName2;
                txtWindowProfileName3.Text = Toolbox.settings.WindowProfileName3;
                txtWindowProfileName4.Text = Toolbox.settings.WindowProfileName4;
                btnWinProfile1.Content = Toolbox.settings.WindowProfileName1;
                btnWinProfile2.Content = Toolbox.settings.WindowProfileName2;
                btnWinProfile3.Content = Toolbox.settings.WindowProfileName3;
                btnWinProfile4.Content = Toolbox.settings.WindowProfileName4;
                txtStartProfileName1.Text = Toolbox.settings.StartProfileName1;
                txtStartProfileName2.Text = Toolbox.settings.StartProfileName2;
                txtStartProfileName3.Text = Toolbox.settings.StartProfileName3;
                txtStartProfileName4.Text = Toolbox.settings.StartProfileName4;
                RefreshSavedWindows();
                uDebugLogAdd("SettingsGEN: working on general settings");
                chkSettingsBeta.IsChecked = Toolbox.settings.BetaUpdate;
                chkSettingsStartup.IsChecked = Actions.CheckWinStartupRegKeyExistance();
                ComboGenWinPref.SelectedItem = Toolbox.settings.PreferredWindow == WindowPreference.UtilityBar ? Toolbox.FindComboBoxItemByString(ComboGenWinPref, "Utility Bar") : Toolbox.FindComboBoxItemByString(ComboGenWinPref, "Desktop Window");
                uDebugLogAdd("Finished settings UI update");
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
                if (_updatingUI)
                {
                    uDebugLogAdd("Already updating settings UI, skipping settings update");
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
                            _updatingUI = true;
                            worker.ReportProgress(1);
                            settingsSaveVerificationInProgress = false;
                            SettingsTimerRefresh();
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
                            VerifyPingGrids();
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

        private void ShowChangelog()
        {
            try
            {
                Changelog changelog = new Changelog();
                changelog.ShowDialog();
                //uDebugLogAdd("Showing Changelog Window");
                //Prompt.OK($"Changelog for v{Toolbox.settings.CurrentVersion}:{Environment.NewLine}{Toolbox.settings.LatestChangelog}", TextAlignment.Left);
                //uDebugLogAdd("Closed changelog window");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SendDiagnostics()
        {
            try
            {
                Actions.SendDiagnostics();
                ShowNotification("Diagnostic info sent to the developer, Thank You!");
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
                    MoveProcessHandleThreaded(selectedWindow);
                }
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

        private void MoveProcessHandleThreaded(WindowItem selectedWindow)
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
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
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        private void MoveSingleWindow(WindowItem selectedWindow)
        {
            try
            {
                var procList = Process.GetProcessesByName(selectedWindow.WindowInfo.Name);
                if (procList.Length <= 0)
                {
                    uDebugLogAdd($"No running process found for {selectedWindow.WindowInfo.Name}, ducking out");
                    uStatusUpdate($"Couldn't find an active running process for {selectedWindow.WindowInfo.Name}");
                    return;
                }
                if (!WindowInfo.DoesWindowInfoHaveClass(selectedWindow.WindowInfo))
                {
                    uDebugLogAdd($"Window info doesn't have class, initiating update to get this fool some class!");
                    AttemptWindowInfoUpdate(selectedWindow);
                }
                IntPtr desiredHandle = WinAPIWrapper.SearchForWindow(selectedWindow.WindowInfo.WinClass, selectedWindow.WindowInfo.Title);
                if (desiredHandle == (IntPtr)0x0000000000000000)
                {
                    uDebugLogAdd($"Couldn't find a handle for {selectedWindow.WindowInfo.Name}, skipping MoveWindow | {desiredHandle}");
                    return;
                }
                else
                    uDebugLogAdd($"Active window handle found matching criteria: {desiredHandle} | {selectedWindow.WindowInfo.WinClass} | {selectedWindow.WindowInfo.Title}");
                WinAPIWrapper.MoveWindow(desiredHandle, selectedWindow.WindowInfo.XValue, selectedWindow.WindowInfo.YValue, selectedWindow.WindowInfo.Width, selectedWindow.WindowInfo.Height, true);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void AttemptWindowInfoUpdate(WindowItem selectedWindow)
        {
            try
            {
                uDebugLogAdd($"Attempting to update window info for the window: {selectedWindow.WindowInfo.Name} | {selectedWindow.WindowInfo.PrivateID} | {selectedWindow.WindowInfo.Title}");
                var procList = Process.GetProcessesByName(selectedWindow.WindowInfo.Name);
                StringBuilder sb = new StringBuilder(1024);
                foreach (var proc in procList)
                {
                    try
                    {
                        uDebugLogAdd($"Attempting to get class from process: {proc.ProcessName} | {proc.Id}");
                        WinAPIWrapper.GetClassName(proc.MainWindowHandle, sb, sb.Capacity);
                        string className = sb.ToString();
                        if (!string.IsNullOrWhiteSpace(className))
                        {
                            uDebugLogAdd($"Found a process with class that fool can learn from! {proc.ProcessName} | {proc.Id} | {className}");
                            var oGWindow = Toolbox.settings.ActiveWindowList.ToList().Find(x => x.WindowInfo.PrivateID == selectedWindow.WindowInfo.PrivateID);
                            if (oGWindow != null)
                            {
                                oGWindow.WindowInfo.WinClass = className;
                                uDebugLogAdd($"Updated OG window class to: {className}");
                            }
                            else
                                uDebugLogAdd($"Couldn't find an OG window to up it's class yo! Not cool. [N]{selectedWindow.WindowInfo.Name} [ID]{selectedWindow.WindowInfo.PrivateID}");
                            selectedWindow.WindowInfo.WinClass = className;
                            uDebugLogAdd($"Updated selectedWindow window Class to: {className}");
                            return;
                        }
                        else
                            uDebugLogAdd($"Didn't find class from this process: {proc.ProcessName}, what a hooligan");
                    }
                    catch (Exception)
                    {
                        uDebugLogAdd($"Couldn't get class from {proc.ProcessName}, this process is a whole new level of low");
                    }
                }
                uDebugLogAdd($"Couldn't find an active running process to update windowInfo from: {selectedWindow.WindowInfo.Name}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LookForWindowHandle()
        {
            try
            {
                capturingHandle = true;
                uDebugLogAdd("LookForWindowHandle started, updating cursor");
                // Mouse.OverrideCursor = Cursors.Cross;
                Cursor = new Cursor($@"{Director.Main.CurrentDirectory}\Dependencies\target_small.cur");
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

        private void DeleteSavedWindowItem()
        {
            try
            {
                WindowItem item = (WindowItem)lbSavedWindows.SelectedItem;
                Toolbox.settings.RemoveWindow(item);
                Events.TriggerWindowInfoChange(true);
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
                lbSavedWindows.ItemsSource = Toolbox.settings.ActiveWindowList;
                switch (Toolbox.settings.CurrentWindowProfile)
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
                Actions.GetWindowItemLocation(windowItem);
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
                //UpdateWindowItemOptions();
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
                };
                verifyier.RunWorkerAsync();
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
                foreach (BasicPingEntry entry in lbBasicPingSessions.Items)
                {
                    if (entry.Address == address || entry.HostName.ToLower() == address)
                        exists = true;
                }
            return exists;
        }

        private bool VerifyInput(string input)
        {
            var isCorrect = true;
            isCorrect = !string.IsNullOrWhiteSpace(input);
            isCorrect = !DoesPingSessionExist(input);
            uDebugLogAdd($"Verified input, answer: {isCorrect} | input: {input}");
            return isCorrect;
        }

        private void AddPingEntry(string address)
        {
            try
            {
                var addressNoSpace = Regex.Replace(address, @"\s+", "");
                var entries = addressNoSpace.Split(',');
                if (Toolbox.settings.BasicPing)
                    foreach (var entry in entries)
                        lbBasicPingSessions.Items.Add(new BasicPingEntry(entry));
                else
                    foreach (var entry in entries)
                        lbPingSessions.Items.Add(new PingEntry(entry));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyPingGrids()
        {
            try
            {
                uDebugLogAdd("Starting Ping Grid Verification");
                if (Toolbox.settings.BasicPing && lbBasicPingSessions.Visibility != Visibility.Visible)
                {
                    uDebugLogAdd($"Basic Ping settings option is {Toolbox.settings.BasicPing} and BasicPingSessions viz is {lbBasicPingSessions.Visibility.ToString()}, showing basic ping grid");
                    lbBasicPingSessions.Visibility = Visibility.Visible;
                    lbPingSessions.Visibility = Visibility.Hidden;
                    lbBasicPingSessions.Items.Clear();
                    foreach (PingEntry entry in lbPingSessions.Items)
                        lbBasicPingSessions.Items.Add(new BasicPingEntry(entry.Address));
                    lbPingSessions.Items.Clear();
                }
                else
                {
                    uDebugLogAdd($"Basic Ping settings option is {Toolbox.settings.BasicPing}, showing visual ping grid");
                    lbBasicPingSessions.Visibility = Visibility.Hidden;
                    lbPingSessions.Visibility = Visibility.Visible;
                    lbPingSessions.Items.Clear();
                    foreach (BasicPingEntry entry in lbBasicPingSessions.Items)
                        lbPingSessions.Items.Add(new PingEntry(entry.Address));
                    lbBasicPingSessions.Items.Clear();
                }
                uDebugLogAdd("Finished Ping Grid Verification");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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

        private void PromptForPingPreference()
        {
            try
            {
                uDebugLogAdd($"Prompting for ping preference, pingpref is: {Toolbox.settings.PingTypeChosen}");
                var response = Prompt.PingType();
                if (response == Prompt.PromptResponse.Custom1)
                    Toolbox.settings.BasicPing = false;
                else
                    Toolbox.settings.BasicPing = true;
                chkNetBasicPing.IsChecked = Toolbox.settings.BasicPing;
                Toolbox.settings.PingTypeChosen = true;
                uDebugLogAdd($"Basic Ping is now: {Toolbox.settings.BasicPing} | PingTypeChosen: {Toolbox.settings.PingTypeChosen}");
                Director.Main.SaveSettings();
                VerifyPingGrids();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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

        private void OpenMacAddressWindow(string v = null)
        {
            try
            {
                MacPopup macPopup = new MacPopup(v);
                Director.Main.PopupWindows.Add(macPopup);
                macPopup.Closing += (s, e) => { Director.Main.PopupWindows.Remove(macPopup); };
                macPopup.Show();
                Director.Main.ShowNotification("MAC Address Identified");
                uDebugLogAdd("Opened MacPopup window");
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

        private void tUpdateSettings(SettingsUpdate settingsUpdate, string value = null)
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    // Pingcount settings
                    var num = 0;
                    if (value != null)
                    {
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
                            else
                                return;
                        }
                    }
                    // Update All Settings
                    worker.ReportProgress(1);
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
                            // Set pingchart settings
                            uDebugLogAdd("SETUPDATE: ping chart");
                            Toolbox.settings.PingChartLength = int.Parse(txtSetNetPingCount.Text);
                            foreach (PingEntry entry in lbPingSessions.Items)
                            {
                                entry.ChartLength = Toolbox.settings.PingChartLength;
                            }
                            // Set Date/Time Format settings
                            uDebugLogAdd("SETUPDATE: DTFormat");
                            if (cmbxSetNetDTFormat.Text == "Seconds")
                                Toolbox.settings.DateTimeFormat = DTFormat.Sec;
                            else if (cmbxSetNetDTFormat.Text == "Minutes")
                                Toolbox.settings.DateTimeFormat = DTFormat.Min;
                            else if (cmbxSetNetDTFormat.Text == "Hours")
                                Toolbox.settings.DateTimeFormat = DTFormat.Hours;
                            // Set network textbox enter action
                            uDebugLogAdd("SETUPDATE: Enter action");
                            if (cmbxSetNetTextboxAction.Text == "DNSLookup")
                                Toolbox.settings.ToolboxEnterAction = EnterAction.DNSLookup;
                            else if (cmbxSetNetTextboxAction.Text == "Ping")
                                Toolbox.settings.ToolboxEnterAction = EnterAction.Ping;
                            // Set basic ping
                            uDebugLogAdd("SETUPDATE: Basic ping");
                            if (chkNetBasicPing.IsChecked == true)
                                Toolbox.settings.BasicPing = true;
                            else
                                Toolbox.settings.BasicPing = false;
                            // Set beta check
                            uDebugLogAdd("SETUPDATE: Beta check");
                            if (chkSettingsBeta.IsChecked == true)
                                Toolbox.settings.BetaUpdate = true;
                            else
                                Toolbox.settings.BetaUpdate = false;
                            // Set Window/Start profile names
                            uDebugLogAdd("SETUPDATE: Window/Start profile names");
                            Toolbox.settings.WindowProfileName1 = txtWindowProfileName1.Text;
                            Toolbox.settings.WindowProfileName2 = txtWindowProfileName2.Text;
                            Toolbox.settings.WindowProfileName3 = txtWindowProfileName3.Text;
                            Toolbox.settings.WindowProfileName4 = txtWindowProfileName4.Text;
                            Toolbox.settings.StartProfileName1 = txtStartProfileName1.Text;
                            Toolbox.settings.StartProfileName2 = txtStartProfileName2.Text;
                            Toolbox.settings.StartProfileName3 = txtStartProfileName3.Text;
                            Toolbox.settings.StartProfileName4 = txtStartProfileName4.Text;
                            // Set startup on windows startup
                            uDebugLogAdd("SETUPDATE: Startup on windows startup");
                            if ((chkSettingsStartup.IsChecked == true) && !Actions.CheckWinStartupRegKeyExistance())
                            {
                                AddToWindowsStartup(true);
                            }
                            else if ((chkSettingsStartup.IsChecked == false) && Actions.CheckWinStartupRegKeyExistance())
                            {
                                AddToWindowsStartup(false);
                            }
                            // Set Window Preference
                            uDebugLogAdd("SETUPDATE: Preferred Window");
                            Toolbox.settings.PreferredWindow = ComboGenWinPref.SelectedItem == Toolbox.FindComboBoxItemByString(ComboGenWinPref, "Utility Bar") ? WindowPreference.UtilityBar : WindowPreference.DesktopWindow;
                            // Trigger events
                            Events.TriggerWindowInfoChange();
                            //Events.TriggerStartInfoChange();
                            break;
                        case 99:
                            ShowNotification("Incorrect format entered");
                            break;
                    }
                    uDebugLogAdd("Finished settings update");
                    _updatingUI = false;
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
                try
                {
                    if (e2.ProgressPercentage == 1)
                    {
                        uDebugLogAdd($"Worker progress is {e2.ProgressPercentage}, adding ResolvedEntry | IP({resolvedEntry.IPAddress}) | HN({resolvedEntry.HostName})");
                        lbResolvedAddresses.Items.Add(resolvedEntry);
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
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
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        private void tDisplayWatcher()
        {
            try
            {
                uDebugLogAdd("Starting DisplayWatcher");
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
                        uDebugLogAdd($"Displaywatcher failure: {ex.Message}", DebugType.FAILURE);
                    }
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    if (pe.ProgressPercentage == 1)
                    {
                        try
                        {
                            RefreshDisplaySizes();
                            VerifyIfWindowIsOffscreen();
                        }
                        catch (Exception ex)
                        {
                            uDebugLogAdd($"Displaywatcher failure: {ex.Message}", DebugType.FAILURE);
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

        #endregion
    }
}
