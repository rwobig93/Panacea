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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using static Panacea.MainWindow;

namespace Panacea.Windows.Popups
{
    /// <summary>
    /// Interaction logic for SettingsPopup.xaml
    /// </summary>
    public partial class SettingsPopup : Window
    {
        public SettingsPopup()
        {
            SetDefaultLocation();
            InitializeComponent();
            Startup();
        }

        #region Globals

        private bool startingUp = true;
        private bool settingsUpdating = false;
        private bool settingsSaveVerificationInProgress = false;
        private bool settingsBadAlerted = false;
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.2) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.2) };
        private double PopinLeft { get { return UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuSettings.Margin.Left; } }
        private double PopinTop { get { return UtilityBar.UtilBarMain.Top - this.ActualHeight; } }
        private double PopinWidth { get { return 455; } }
        private double PopinHeight { get { return 287; } }
        private int settingsTimer = 5;
        public bool PoppedOut { get; set; } = false;

        #endregion

        #region Form Handling

        private void SettingsPopupMain_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left && PoppedOut)
                    SettingsPopupMain.DragMove();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SettingsPopupMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!startingUp)
                VerifyResetButtonRequirement();
        }

        #endregion

        #region Event Handlers

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetPopupSizeAndLocation();
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

        private void ChkPopSetGenBeta_Click(object sender, RoutedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.BetaCheck);
        }

        private void ChkPopSetGenStartup_Click(object sender, RoutedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.WinStartup);
        }

        private void BtnPopSetGenChangelog_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnPopSetGenSendDiag_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPopSetGenConfigDefault_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ComboPopSetNetAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.TextBoxAction);
        }

        private void TxtPopSetWinPro1_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void TxtPopSetWinPro2_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void TxtPopSetWinPro3_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void TxtPopSetWinPro4_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void TxtPopSetStartPro1_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void TxtPopSetStartPro2_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void TxtPopSetStartPro3_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        private void TxtPopSetStartPro4_KeyDown(object sender, KeyEventArgs e)
        {
            StartSettingsUpdate(SettingsUpdate.ProfileName);
        }

        #endregion

        #region Methods

        private void Startup()
        {
            PopupShow();
            EnableWindowResizing();
            UpdateUISettings();
            //HideTimerActivate();
            SubscribeToEvents();
            FinishStartup();
        }

        private void UpdateUISettings()
        {
            try
            {
                uDebugLogAdd("Starting settings UI update");
                settingsUpdating = true;

                uDebugLogAdd("SETUI: General");
                // General Settings
                ChkPopSetGenBeta.IsChecked = Toolbox.settings.BetaUpdate;
                ChkPopSetGenStartup.IsChecked = Actions.CheckWinStartupRegKeyExistance();

                uDebugLogAdd("SETUI: Network");
                // Network Settings
                switch (Toolbox.settings.UtilBarEnterAction)
                {
                    case EnterAction.DNSLookup:
                        ComboPopSetNetAction.SelectedItem = ComboPopSetNetAction.Items[ComboPopSetNetAction.Items.IndexOf("DNSLookup")];
                        break;
                    case EnterAction.Ping:
                        ComboPopSetNetAction.SelectedItem = ComboPopSetNetAction.Items[ComboPopSetNetAction.Items.IndexOf("Ping")];
                        break;
                    default:
                        ComboPopSetNetAction.Text = "???";
                        break;
                }

                uDebugLogAdd("SETUI: Windows");
                // Windows Settings
                TxtPopSetWinPro1.Text = Toolbox.settings.WindowProfileName1;
                TxtPopSetWinPro2.Text = Toolbox.settings.WindowProfileName2;
                TxtPopSetWinPro3.Text = Toolbox.settings.WindowProfileName3;
                TxtPopSetWinPro4.Text = Toolbox.settings.WindowProfileName4;
                TxtPopSetStartPro1.Text = Toolbox.settings.StartProfileName1;
                TxtPopSetStartPro2.Text = Toolbox.settings.StartProfileName2;
                TxtPopSetStartPro3.Text = Toolbox.settings.StartProfileName3;
                TxtPopSetStartPro4.Text = Toolbox.settings.StartProfileName4;

                settingsUpdating = false;
                uDebugLogAdd("Finished settings UI update");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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
                this.Top = PopinTop; //UtilityBar.UtilBarMain.Top - PopinHeight;
                this.Left = PopinLeft; //UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuSettings.Margin.Left;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void HideTimerActivate()
        {
            try
            {
                System.Timers.Timer timer = new System.Timers.Timer(3000);
                timer.Start();
                timer.Elapsed += (s, e) => { Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { PopupHide(); } catch (Exception ex) { LogException(ex); } }); };
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
                Toolbox.uAddDebugLog(_log, _type, caller);
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
                this.Left = UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuSettings.Margin.Left;
                this.Opacity = 0;
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
            finally
            {
                //HideTimerActivate();
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
                    plySettingsVisualSlider.Visibility = Visibility.Visible;
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
                    plySettingsVisualSlider.Visibility = Visibility.Hidden;
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
                uDebugLogAdd("Resetting Audio Popup Size and Location to Default");
                this.Left = PopinLeft;
                this.Top = PopinTop;
                this.Width = PopinWidth;
                this.Height = PopinHeight;
                btnReset.Visibility = Visibility.Hidden;
                TogglePopout(true);
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
                uDebugLogAdd($"Calling ShowNotification from SettingsPopup: {text}");
                UtilityBar.UtilBarMain.ShowNotification(text);
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

        private void StartSettingsUpdate(SettingsUpdate settingsUpdate)
        {
            try
            {
                if (startingUp)
                {
                    uDebugLogAdd("Application is still starting up, skipping settings update");
                    return;
                }
                if (settingsUpdating)
                {
                    uDebugLogAdd("Settings UI updating, skipping settings update");
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
                                Thread.Sleep(500);
                                settingsTimer--;
                            }
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
                            // Set network textbox enter action
                            uDebugLogAdd("SETUPDATE: Enter action");
                            if (ComboPopSetNetAction.Text == "DNSLookup")
                                Toolbox.settings.ToolboxEnterAction = EnterAction.DNSLookup;
                            else if (ComboPopSetNetAction.Text == "Ping")
                                Toolbox.settings.ToolboxEnterAction = EnterAction.Ping;
                            // Set beta check
                            uDebugLogAdd("SETUPDATE: Beta check");
                            if (ChkPopSetGenBeta.IsChecked == true)
                                Toolbox.settings.BetaUpdate = true;
                            else
                                Toolbox.settings.BetaUpdate = false;
                            // Set Window/Start profile names
                            uDebugLogAdd("SETUPDATE: Window/Start profile names");
                            Toolbox.settings.WindowProfileName1 = TxtPopSetWinPro1.Text;
                            Toolbox.settings.WindowProfileName2 = TxtPopSetWinPro2.Text;
                            Toolbox.settings.WindowProfileName3 = TxtPopSetWinPro3.Text;
                            Toolbox.settings.WindowProfileName4 = TxtPopSetWinPro4.Text;
                            Toolbox.settings.StartProfileName1 = TxtPopSetStartPro1.Text;
                            Toolbox.settings.StartProfileName2 = TxtPopSetStartPro2.Text;
                            Toolbox.settings.StartProfileName3 = TxtPopSetStartPro3.Text;
                            Toolbox.settings.StartProfileName4 = TxtPopSetStartPro4.Text;
                            // Set startup on windows startup
                            uDebugLogAdd("SETUPDATE: Startup on windows startup");
                            if ((ChkPopSetGenStartup.IsChecked == true) && !Toolbox.settings.WindowsStartup)
                            {
                                Actions.AddToWindowsStartup(true);
                                Toolbox.settings.WindowsStartup = true;
                            }
                            else if ((ChkPopSetGenStartup.IsChecked == false) && Toolbox.settings.WindowsStartup)
                            {
                                Actions.AddToWindowsStartup(false);
                                Toolbox.settings.WindowsStartup = false;
                            }
                            UpdateUISettings();
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

        #endregion
    }
}
