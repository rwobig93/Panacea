using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using static Panacea.MainWindow;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for MacPopup.xaml
    /// </summary>
    public partial class MacPopup : Window
    {
        public MacPopup(string str = null, MACNotation notation = MACNotation.Lowercase)
        {
            try
            {
                InitializeComponent();
                this.txtMacAddress.Text = str != null ? str : "FF:FF:FF:FF:FF:FF";
                if (str != null)
                {
                    ConvertMacAddress();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #region Globals

        public enum MACFormat
        {
            Dash,
            Colon,
            Net,
            Mixed
        }

        public enum MACNotation
        {
            Uppercase,
            Lowercase
        }

        private bool _notificationPlaying = false;
        private List<string> _notificationList = new List<string>();

        #endregion

        #region Event Handlers

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            MinimizeWindow();
        }

        private void btnConvertMac_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var value = txtMacAddress.Text;
                if (VerifyIfMacAddress(value))
                    ConvertMacAddress();
                else
                {
                    ShowNotification($"Value entered isn't a Mac address: {value}");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void lblMacAddressDashValue_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboard(lblMacAddressDashValue.Text);
        }

        private void lblMacAddressColValue_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboard(lblMacAddressColValue.Text);
        }

        private void lblMacAddressNetValue_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboard(lblMacAddressNetValue.Text);
        }

        private void txtMacAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConvertMacAddress();
            }
        }

        private void togMACCase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConvertMacAddress();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region Methods

        private void CloseWindow()
        {
            try
            {
                uDebugLogAdd("Closing MacPopup window");
                this.Close();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MinimizeWindow()
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

        private void ConvertMacAddress()
        {
            try
            {
                MACFormat format = GetMacFormat();
                MACNotation notation = GetMacNotation();
                uDebugLogAdd($"Converting MAC address [f]{format.ToString()} [n]{notation.ToString()} [a]{txtMacAddress.Text}");
                string macAddr = txtMacAddress.Text.Replace(":", "").Replace("-", "").Replace(".", "");
                if (notation == MACNotation.Uppercase)
                {
                    lblMacAddressDashValue.Text = ConvertMacAddrDash(macAddr).ToUpper();
                    lblMacAddressColValue.Text = ConvertMacAddrCol(macAddr).ToUpper();
                    lblMacAddressNetValue.Text = ConvertMacAddrNet(macAddr).ToUpper();
                    uDebugLogAdd("Set MAC Addresses to Upper");
                }
                else if (notation == MACNotation.Lowercase)
                {
                    lblMacAddressDashValue.Text = ConvertMacAddrDash(macAddr).ToLower();
                    lblMacAddressColValue.Text = ConvertMacAddrCol(macAddr).ToLower();
                    lblMacAddressNetValue.Text = ConvertMacAddrNet(macAddr).ToLower();
                    uDebugLogAdd("Set MAC Addresses to Lower");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public static string ConvertMacAddrNet(string macAddr)
        {
            string newMac = string.Empty;
            var counter = 1;
            foreach (var chara in macAddr)
            {
                if (counter % 4 == 0)
                    newMac = $"{newMac}{chara}.";
                else
                    newMac = $"{newMac}{chara}";
                counter++;
            }
            return newMac.TrimEnd('.');
        }

        public static string ConvertMacAddrCol(string macAddr)
        {
            string newMac = string.Empty;
            var counter = 1;
            foreach (var chara in macAddr)
            {
                if (counter % 2 == 0)
                    newMac = $"{newMac}{chara}:";
                else
                    newMac = $"{newMac}{chara}";
                counter++;
            }
            return newMac.TrimEnd(':');
        }

        public static string ConvertMacAddrDash(string macAddr)
        {
            string newMac = string.Empty;
            var counter = 1;
            foreach (var chara in macAddr)
            {
                if (counter % 2 == 0)
                    newMac = $"{newMac}{chara}-";
                else
                    newMac = $"{newMac}{chara}";
                counter++;
            }
            return newMac.TrimEnd('-');
        }

        private MACFormat GetMacFormat()
        {
            var str = txtMacAddress.Text;
            var matches = 0;
            MACFormat format = MACFormat.Colon;
            try
            {
                if (str.Contains(":"))
                {
                    format = MACFormat.Colon;
                    matches++;
                }
                else if (str.Contains("-"))
                {
                    format = MACFormat.Dash;
                    matches++;
                }
                else if (str.Contains("."))
                {
                    format = MACFormat.Net;
                    matches++;
                }
                if (matches > 1)
                    format = MACFormat.Mixed;
                else if (matches < 1)
                {
                    uDebugLogAdd($"Mac address had less than 1 match, something went wrong: [a]{str} [m]{matches}", DebugType.FAILURE);
                    throw new ArgumentException($"Mac address had less than 1 match, something went wrong: [a]{str} [m]{matches}", "MAC Format");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            return format;
        }

        private MACNotation GetMacNotation()
        {
            MACNotation notation = MACNotation.Lowercase;
            if (togMACCase.IsChecked == true)
                notation = MACNotation.Uppercase;
            else
                notation = MACNotation.Lowercase;
            return notation;
        }

        private void CopyToClipboard(string clip)
        {
            try
            {
                Clipboard.SetText(clip);
                ShowNotification($"Copied to clipboard: {clip}");
            }
            catch (Exception ex)
            {
                uDebugLogAdd($"Error occured when setting clipboard: {clip} | {ex.Message}", DebugType.FAILURE);
            }
        }

        private void ShowNotification(string message)
        {
            try
            {
                uDebugLogAdd($"Adding notification: {message}");
                _notificationList.Add(message);
                if (!_notificationPlaying)
                {
                    uDebugLogAdd("Notification wasn't previously playing, starting Notification Helper");
                    tNotificationHelper();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void tNotificationHelper()
        {
            try
            {
                _notificationPlaying = true;
                DoubleAnimation animation = new DoubleAnimation()
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(1)
                };
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {

                    try
                    {
                        while (_notificationList.ToList().Count > 0)
                        {
                            worker.ReportProgress(1);
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                            _notificationList.RemoveAt(0);
                        }
                        _notificationPlaying = false;
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
                            lblNotification.Text = _notificationList.ToList()[0];
                            lblNotification.BeginAnimation(TextBlock.OpacityProperty, animation);
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

        private void uDebugLogAdd(string _log, DebugType debugType = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            Toolbox.uAddDebugLog(_log, debugType, caller);
        }

        private void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            try
            {
                Toolbox.LogException(ex, lineNum, caller, path);
                uDebugLogAdd($"{caller} at line {lineNum} with type {ex.GetType().Name}", DebugType.EXCEPTION);
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
                uDebugLogAdd($"{caller} at line {lineNum} with type {ex.GetType().Name}", DebugType.EXCEPTION);
            }
        }

        #endregion
    }
}
