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

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for WindowPopup.xaml
    /// </summary>
    public partial class WindowPopup : Window
    {
        public WindowPopup()
        {
            SetDefaultLocation();
            InitializeComponent();
            Startup();
        }

        #region Globals

        private bool startingUp = true;
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.2) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.2) };
        private double PopinLeft { get { return UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuWindows.Margin.Left; } }
        private double PopinTop { get { return UtilityBar.UtilBarMain.Top - this.ActualHeight; } }
        private double PopinWidth { get { return 545; } }
        private double PopinHeight { get { return 579; } }
        public bool PoppedOut { get; set; } = false;

        #endregion

        #region Form Handling

        private void WinPopupMain_Loaded(object sender, RoutedEventArgs e)
        {
            EnableWindowResizing();
        }

        #endregion

        #region Event Handlers
        private void BtnWinProfile1_Click(object sender, RoutedEventArgs e)
        {
            ChangeWindowProfile(WindowProfile.Profile1);
        }

        private void BtnWinProfile2_Click(object sender, RoutedEventArgs e)
        {
            ChangeWindowProfile(WindowProfile.Profile2);
        }

        private void BtnWinProfile3_Click(object sender, RoutedEventArgs e)
        {
            ChangeWindowProfile(WindowProfile.Profile3);
        }

        private void BtnWinProfile4_Click(object sender, RoutedEventArgs e)
        {
            ChangeWindowProfile(WindowProfile.Profile4);
        }

        private void BtnStartProfile1_Click(object sender, RoutedEventArgs e)
        {
            Actions.StartProcess(@"C:\Users\Rick\AppData\Local\Discord\app-0.0.305\Discord.exe");
            Director.Main.ShowNotification("Started Discord");
        }

        private void BtnStartProfile2_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnStartProfile3_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnStartProfile4_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnMenuWindows_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuGrid(GrdWinWindows);
        }

        private void BtnMenuStartProc_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuGrid(GrdWinStartProc);
        } 
        #endregion

        #region Methods

        private void Startup()
        {
            UpdateWindowProfileButtons();
            PopupShow();
            SubscribeToEvents();
            ToggleMenuGrid(GrdWinWindows);
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

        private void UpdateWindowProfileButtons()
        {
            try
            {
                BtnWinProfile1.Content = Toolbox.settings.WindowProfileName1;
                BtnWinProfile2.Content = Toolbox.settings.WindowProfileName2;
                BtnWinProfile3.Content = Toolbox.settings.WindowProfileName3;
                BtnWinProfile4.Content = Toolbox.settings.WindowProfileName4;
                uDebugLogAdd("Updated window profile buttons");
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
                Toolbox.uAddDebugLog($"POPWIN: {_log}", _type, caller);
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
                this.Left = UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuWindows.Margin.Left;
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
                UpdateWindowProfileButtons();
                if (this.Opacity == 0)
                    this.BeginAnimation(Window.OpacityProperty, inAnimation);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ChangeWindowProfile(WindowProfile profile)
        {
            try
            {
                Toolbox.settings.ChangeWindowProfile(profile);
                string profileName = string.Empty;
                switch (profile)
                {
                    case WindowProfile.Profile1:
                        profileName = Toolbox.settings.WindowProfileName1;
                        break;
                    case WindowProfile.Profile2:
                        profileName = Toolbox.settings.WindowProfileName2;
                        break;
                    case WindowProfile.Profile3:
                        profileName = Toolbox.settings.WindowProfileName3;
                        break;
                    case WindowProfile.Profile4:
                        profileName = Toolbox.settings.WindowProfileName4;
                        break;
                }
                UtilityBar.UtilBarMain.btnMenuWindows.Content = profileName;
                PopupHide();
                uDebugLogAdd("Changed window profile");
                UtilityBar.UtilBarMain.ShowNotification($"Window Profile Changed to: {profileName}");
                UpdateWindowProfileButtons();
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
                displayArea = grdWindows.Margin; // 95+17 from bottom of grid | +1 for border
                uDebugLogAdd($"displayArea <Before>: [T]{displayArea.Top} [L]{displayArea.Left} [B]{displayArea.Bottom} [R]{displayArea.Right}");
                displayArea.Top = 1;
                displayArea.Right = 1;
                displayArea.Bottom = 95;
                displayArea.Left = 1;
                uDebugLogAdd($"displayArea <After>: [T]{displayArea.Top} [L]{displayArea.Left} [B]{displayArea.Bottom} [R]{displayArea.Right}");
                return displayArea;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            return displayArea;
        }

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
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private Thickness GetWindowHiddenArea()
        {
            Thickness hiddenArea = new Thickness();
            try
            {
                uDebugLogAdd("Getting window hidden area");
                hiddenArea = grdWindows.Margin;
                uDebugLogAdd($"hiddenArea <Before>: [T]{hiddenArea.Top} [L]{hiddenArea.Left} [B]{hiddenArea.Bottom} [R]{hiddenArea.Right}");
                hiddenArea.Left = 250;
                //hiddenArea.Top = 60;
                //hiddenArea.Bottom = 60;
                hiddenArea.Right = 300; // 60
                uDebugLogAdd($"hiddenArea <After>: [T]{hiddenArea.Top} [L]{hiddenArea.Left} [B]{hiddenArea.Bottom} [R]{hiddenArea.Right}");
                return hiddenArea;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            return hiddenArea;
        }

        private void HideUnusedMenuGrids(Grid grid = null)
        {
            try
            {
                if (grid == null)
                    uDebugLogAdd($"Call HideUnusedMenuGrids(null)");
                else
                    uDebugLogAdd($"Call HideUnusedMenuGrids({grid.Name})");
                if (grid != GrdWinStartProc)
                {
                    GrdWinStartProc.Visibility = Visibility.Hidden;
                    Toolbox.AnimateGrid(GrdWinStartProc, GetWindowHiddenArea());
                }
                if (grid != GrdWinWindows)
                {
                    GrdWinWindows.Visibility = Visibility.Hidden;
                    Toolbox.AnimateGrid(GrdWinWindows, GetWindowHiddenArea());
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
