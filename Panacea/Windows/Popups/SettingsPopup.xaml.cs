using Panacea.Classes;
using System;
using System.Collections.Generic;
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
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.2) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.2) };
        private double PopinLeft { get { return UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuSettings.Margin.Left; } }
        private double PopinTop { get { return UtilityBar.UtilBarMain.Top - this.ActualHeight; } }
        private double PopinWidth { get { return 455; } }
        private double PopinHeight { get { return 287; } }
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

        }

        private void ChkPopSetGenStartup_Click(object sender, RoutedEventArgs e)
        {

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

        }

        private void TxtPopSetWinPro1_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TxtPopSetWinPro2_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TxtPopSetWinPro3_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TxtPopSetWinPro4_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TxtPopSetStartPro1_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TxtPopSetStartPro2_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TxtPopSetStartPro3_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TxtPopSetStartPro4_KeyDown(object sender, KeyEventArgs e)
        {

        }

        #endregion

        #region Methods

        private void Startup()
        {
            PopupShow();
            EnableWindowResizing();
            //HideTimerActivate();
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

        #endregion
    }
}
