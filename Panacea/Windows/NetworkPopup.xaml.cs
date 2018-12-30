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
using static Panacea.MainWindow;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for NetworkPopup.xaml
    /// </summary>
    public partial class NetworkPopup : Window
    {
        public NetworkPopup(Rect dimensions)
        {
            if (dimensions == null)
                dimensions = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
            this.Top = dimensions.Top;
            this.Left = dimensions.Left;
            this.Opacity = 0;
            InitializeComponent();
            Startup();
        }

        #region Globals

        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.2) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.2) };
        public bool Popout { get; set; } = false;
        public bool resolvingDNS = false;
        private int resolvedEntries = 0;

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

        private void RectGrabBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left && Popout)
                    winNetMain.DragMove();
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
            PopupShow();
        }

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
                        //if (lblNetResolved.Text == "NSlookup:")
                        //    lblNetResolved.Text = ".";
                        //else if (lblNetResolved.Text != ".....")
                        //    lblNetResolved.Text = $"{lblNetResolved.Text}.";
                        //else
                        //    lblNetResolved.Text = ".";
                    }
                    if (pe.ProgressPercentage == 2)
                    {
                        //lblNetResolved.Text = "NSlookup:";
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

        private void TogglePopout()
        {
            try
            {
                if (Popout)
                {
                    Popout = false;
                    UtilityBar utilBar = UtilityBar.UtilBarMain;
                    this.Left = utilBar.Left + utilBar.btnMenuNetwork.Margin.Left;
                    this.Top = utilBar.Top - this.ActualHeight;
                    this.ResizeMode = ResizeMode.NoResize;
                    plyNetVisualSlider.Visibility = Visibility.Visible;
                    btnPopInOut.Content = "🢅";
                }
                else if (!Popout)
                {
                    Popout = true;
                    this.Left += 10;
                    this.Top -= 10;
                    this.ResizeMode = ResizeMode.CanResize;
                    plyNetVisualSlider.Visibility = Visibility.Hidden;
                    btnPopInOut.Content = "🢇";
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
