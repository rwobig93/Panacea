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
        private bool _updatingUI = false;
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.2) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.2) };
        private double PopinLeft { get { return UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuWindows.Margin.Left; } }
        private double PopinTop { get { return UtilityBar.UtilBarMain.Top - this.ActualHeight; } }
        private double PopinWidth { get { return 545; } }
        private double PopinHeight { get { return 429; } }
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
            TriggerWindowProfileMove(WindowProfile.Profile1);
            //ChangeWindowProfile(WindowProfile.Profile1);
        }

        private void BtnWinProfile2_Click(object sender, RoutedEventArgs e)
        {
            TriggerWindowProfileMove(WindowProfile.Profile2);
            //ChangeWindowProfile(WindowProfile.Profile2);
        }

        private void BtnWinProfile3_Click(object sender, RoutedEventArgs e)
        {
            TriggerWindowProfileMove(WindowProfile.Profile3);
            //ChangeWindowProfile(WindowProfile.Profile3);
        }

        private void BtnWinProfile4_Click(object sender, RoutedEventArgs e)
        {
            TriggerWindowProfileMove(WindowProfile.Profile4);
            //ChangeWindowProfile(WindowProfile.Profile4);
        }

        private void BtnStartProfile1_Click(object sender, RoutedEventArgs e)
        {
            Actions.TriggerProcessProfileStart(StartProfile.Start1);
        }

        private void BtnStartProfile2_Click(object sender, RoutedEventArgs e)
        {
            Actions.TriggerProcessProfileStart(StartProfile.Start2);
        }

        private void BtnStartProfile3_Click(object sender, RoutedEventArgs e)
        {
            Actions.TriggerProcessProfileStart(StartProfile.Start3);
        }

        private void BtnStartProfile4_Click(object sender, RoutedEventArgs e)
        {
            Actions.TriggerProcessProfileStart(StartProfile.Start4);
        }

        private void BtnMenuWindows_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuGrid(GrdWinWindows);
        }

        private void BtnMenuStartProc_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuGrid(GrdWinStartProc);
        }

        private void BtnWinAdd_Click(object sender, RoutedEventArgs e)
        {
            Director.Main.OpenWindowHandleFinder();
        }

        private void BtnWinDel_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedWindowItem();
        }

        private void BtnWinMove_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedWindow();
        }

        private void BtnWinUpdateLocate_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedWindowPosition();
        }

        private void BtnStartDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedStartItem();
        }

        private void BtnStartAdd_Click(object sender, RoutedEventArgs e)
        {
            Director.Main.OpenStartProcessWindow();
        }

        private void BtnStartProc_Click(object sender, RoutedEventArgs e)
        {
            StartSelectedProcess();
        }

        private void BtnStartEdit_Click(object sender, RoutedEventArgs e)
        {
            EditSelectedStartProcessItem();
        }

        private void ComboWinSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatingUI)
                ChangeSelectedWindowProfile((ComboBoxItem)ComboWinSelected.SelectedItem);
        }

        private void ComboStartSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatingUI)
                ChangeSelectedStartProfile((ComboBoxItem)ComboStartSelected.SelectedItem);
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
                    WinPopupMain.DragMove();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void WinPopupMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!startingUp)
                VerifyResetButtonRequirement();
        }
        #endregion

        #region Methods
        private void Startup()
        {
            UpdateSettingsUI();
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
                Events.StartProcInfoChanged += Events_StartProcInfoChanged;
                Events.WinInfoChanged += Events_WinInfoChanged;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Events_WinInfoChanged(WindowInfoArgs args)
        {
            UpdateSettingsUI();
        }

        private void Events_StartProcInfoChanged(StartProcArgs args)
        {
            UpdateSettingsUI();
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
                displayArea = grdWindows.Margin;
                uDebugLogAdd($"displayArea <Before>: [T]{displayArea.Top} [L]{displayArea.Left} [B]{displayArea.Bottom} [R]{displayArea.Right}");
                displayArea.Top = 30;
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
                UpdateTitle(grid);
                if (grid.Margin != displayArea)
                {
                    grid.Visibility = Visibility.Visible;
                    Toolbox.AnimateGrid(grid, displayArea);
                }
                else
                {
                    Toolbox.AnimateGrid(grid, GetWindowHiddenArea(grid));
                    grid.Visibility = Visibility.Hidden;
                }
                HideUnusedMenuGrids(grid);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateTitle(Grid grid)
        {
            try
            {
                if (grid == GrdWinStartProc)
                {
                    uDebugLogAdd("Updating title for Start Processes");
                    LabelHeader.Content = "Start Processes";
                }
                else if (grid == GrdWinWindows)
                {
                    uDebugLogAdd("Updating title for Window Handles");
                    LabelHeader.Content = "Window Mover";
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private Thickness GetWindowHiddenArea(Grid grid)
        {
            double left = 0;
            double right = 0;
            if (grid == GrdWinStartProc) { left = 550; right = -548; }
            else if (grid == GrdWinWindows) { left = -548; right = 550; }
            else { left = 550; right = -548; }
            Thickness hiddenArea = new Thickness();
            try
            {
                uDebugLogAdd("Getting window hidden area");
                hiddenArea = grdWindows.Margin;
                uDebugLogAdd($"hiddenArea <Before>: [T]{hiddenArea.Top} [L]{hiddenArea.Left} [B]{hiddenArea.Bottom} [R]{hiddenArea.Right}");
                hiddenArea.Left = left;
                //hiddenArea.Top = 60;
                //hiddenArea.Bottom = 60;
                hiddenArea.Right = right; // 60
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
                    Toolbox.AnimateGrid(GrdWinStartProc, GetWindowHiddenArea(grid));
                }
                if (grid != GrdWinWindows)
                {
                    GrdWinWindows.Visibility = Visibility.Hidden;
                    Toolbox.AnimateGrid(GrdWinWindows, GetWindowHiddenArea(grid));
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateSettingsUI()
        {
            try
            {
                _updatingUI = true;
                uDebugLogAdd("Updating Settings UI");
                // Update windows UI
                ListWindows.ItemsSource = Toolbox.settings.ActiveWindowList.OrderBy(x => x.WindowName).ToList();
                if (!VerifyComboBoxItems(ComboWinSelected)) { UpdateComboBoxItems(ComboWinSelected); }
                string winProfileName = string.Empty;
                switch (Toolbox.settings.CurrentWindowProfile)
                {
                    case WindowProfile.Profile1:
                        winProfileName = Toolbox.settings.WindowProfileName1;
                        break;
                    case WindowProfile.Profile2:
                        winProfileName = Toolbox.settings.WindowProfileName2;
                        break;
                    case WindowProfile.Profile3:
                        winProfileName = Toolbox.settings.WindowProfileName3;
                        break;
                    case WindowProfile.Profile4:
                        winProfileName = Toolbox.settings.WindowProfileName4;
                        break;
                }
                var comboWinItem = Toolbox.FindComboBoxItemByString(ComboWinSelected, winProfileName);
                if (comboWinItem != null)
                {
                    if (ComboWinSelected.SelectedItem != comboWinItem)
                    {
                        ComboWinSelected.SelectedItem = comboWinItem;
                    }
                }
                BtnWinProfile1.Content = Toolbox.settings.WindowProfileName1;
                BtnWinProfile2.Content = Toolbox.settings.WindowProfileName2;
                BtnWinProfile3.Content = Toolbox.settings.WindowProfileName3;
                BtnWinProfile4.Content = Toolbox.settings.WindowProfileName4;

                // Update start proc UI
                ListStartProc.ItemsSource = Toolbox.settings.ActiveStartList.OrderBy(x => x.Name).ToList();
                if (!VerifyComboBoxItems(ComboStartSelected)) { UpdateComboBoxItems(ComboStartSelected); }
                string startProfileName = string.Empty;
                switch (Toolbox.settings.CurrentStartProfile)
                {
                    case StartProfile.Start1:
                        startProfileName = Toolbox.settings.StartProfileName1;
                        break;
                    case StartProfile.Start2:
                        startProfileName = Toolbox.settings.StartProfileName2;
                        break;
                    case StartProfile.Start3:
                        startProfileName = Toolbox.settings.StartProfileName3;
                        break;
                    case StartProfile.Start4:
                        startProfileName = Toolbox.settings.StartProfileName4;
                        break;
                }
                var comboStartItem = Toolbox.FindComboBoxItemByString(ComboStartSelected, startProfileName);
                if (comboStartItem != null)
                {
                    if (ComboStartSelected.SelectedItem != comboStartItem)
                    {
                        ComboStartSelected.SelectedItem = comboStartItem;
                    }
                }
                BtnStartProfile1.Content = Toolbox.settings.StartProfileName1;
                BtnStartProfile2.Content = Toolbox.settings.StartProfileName2;
                BtnStartProfile3.Content = Toolbox.settings.StartProfileName3;
                BtnStartProfile4.Content = Toolbox.settings.StartProfileName4;
                uDebugLogAdd("Finished updating settings UI");
                _updatingUI = false;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateComboBoxItems(ComboBox comboBox)
        {
            try
            {
                uDebugLogAdd($"Triggered Combobox items update: {comboBox.Name.ToString()}");
                comboBox.Items.Clear();
                if (comboBox == ComboWinSelected)
                {
                    comboBox.Items.Add(new ComboBoxItem() { Content = Toolbox.settings.WindowProfileName1 });
                    comboBox.Items.Add(new ComboBoxItem() { Content = Toolbox.settings.WindowProfileName2 });
                    comboBox.Items.Add(new ComboBoxItem() { Content = Toolbox.settings.WindowProfileName3 });
                    comboBox.Items.Add(new ComboBoxItem() { Content = Toolbox.settings.WindowProfileName4 });
                }
                else if (comboBox == ComboStartSelected)
                {
                    comboBox.Items.Add(new ComboBoxItem() { Content = Toolbox.settings.StartProfileName1 });
                    comboBox.Items.Add(new ComboBoxItem() { Content = Toolbox.settings.StartProfileName2 });
                    comboBox.Items.Add(new ComboBoxItem() { Content = Toolbox.settings.StartProfileName3 });
                    comboBox.Items.Add(new ComboBoxItem() { Content = Toolbox.settings.StartProfileName4 });
                }
                uDebugLogAdd($"Finished updating Combobox items: {comboBox.Name.ToString()}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private bool VerifyComboBoxItems(ComboBox comboBox)
        {
            bool matches = true;
            try
            {
                if (comboBox == ComboWinSelected)
                {
                    if (Toolbox.FindComboBoxItemByString(comboBox, Toolbox.settings.WindowProfileName1) == null) matches = false;
                    if (Toolbox.FindComboBoxItemByString(comboBox, Toolbox.settings.WindowProfileName2) == null) matches = false;
                    if (Toolbox.FindComboBoxItemByString(comboBox, Toolbox.settings.WindowProfileName3) == null) matches = false;
                    if (Toolbox.FindComboBoxItemByString(comboBox, Toolbox.settings.WindowProfileName4) == null) matches = false;
                }
                else if (comboBox == ComboStartSelected)
                {
                    if (Toolbox.FindComboBoxItemByString(comboBox, Toolbox.settings.StartProfileName1) == null) matches = false;
                    if (Toolbox.FindComboBoxItemByString(comboBox, Toolbox.settings.StartProfileName2) == null) matches = false;
                    if (Toolbox.FindComboBoxItemByString(comboBox, Toolbox.settings.StartProfileName3) == null) matches = false;
                    if (Toolbox.FindComboBoxItemByString(comboBox, Toolbox.settings.StartProfileName4) == null) matches = false;
                }
            }
            catch (Exception ex)
            {
                matches = false;
                LogException(ex);
            }
            uDebugLogAdd($"Verified combobox items: {matches} | {comboBox.Name.ToString()}");
            return matches;
        }

        private void ChangeSelectedStartProfile(ComboBoxItem selectedItem)
        {
            try
            {
                StartProfile profile = StartProfile.Start1;
                if (selectedItem.Content.ToString() == Toolbox.settings.StartProfileName1)
                    profile = StartProfile.Start1;
                else if (selectedItem.Content.ToString() == Toolbox.settings.StartProfileName2)
                    profile = StartProfile.Start2;
                else if (selectedItem.Content.ToString() == Toolbox.settings.StartProfileName3)
                    profile = StartProfile.Start3;
                else if (selectedItem.Content.ToString() == Toolbox.settings.StartProfileName4)
                    profile = StartProfile.Start4;
                uDebugLogAdd($"Starting start profile change: [comboItem] {selectedItem.Content.ToString()} [profile] {profile.ToString()}");
                Toolbox.settings.ChangeStartProfile(profile);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ChangeSelectedWindowProfile(ComboBoxItem selectedItem)
        {
            try
            {
                WindowProfile profile = WindowProfile.Profile1;
                if (selectedItem.Content.ToString() == Toolbox.settings.WindowProfileName1)
                    profile = WindowProfile.Profile1;
                else if (selectedItem.Content.ToString() == Toolbox.settings.WindowProfileName2)
                    profile = WindowProfile.Profile2;
                else if (selectedItem.Content.ToString() == Toolbox.settings.WindowProfileName3)
                    profile = WindowProfile.Profile3;
                else if (selectedItem.Content.ToString() == Toolbox.settings.WindowProfileName4)
                    profile = WindowProfile.Profile4;
                uDebugLogAdd($"Starting window profile change: [comboItem] {selectedItem.Content.ToString()} [profile] {profile.ToString()}");
                Toolbox.settings.ChangeWindowProfile(profile);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DeleteSelectedWindowItem()
        {
            try
            {
                WindowItem item = (WindowItem)ListWindows.SelectedItem;
                Toolbox.settings.RemoveWindow(item);
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
                WindowItem item = (WindowItem)ListWindows.SelectedItem;
                Actions.MoveProcessHandle(item);
                Director.Main.ShowNotification($"Moved Window: {item.WindowName}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TriggerWindowProfileMove(WindowProfile profile)
        {
            try
            {
                uDebugLogAdd($"Triggering Windows Profile Move: {profile.ToString()}");
                Actions.MoveProfileWindows(profile);
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
                WindowItem windowItem = (WindowItem)ListWindows.SelectedItem;
                Actions.GetWindowItemLocation(windowItem);
                Director.Main.ShowNotification($"Updated Location: {windowItem.WindowName}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DeleteSelectedStartItem()
        {
            try
            {
                StartProcessItem item = (StartProcessItem)ListStartProc.SelectedItem;
                Toolbox.settings.RemoveStartProcess(item);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void StartSelectedProcess()
        {
            try
            {
                StartProcessItem startItem = (StartProcessItem)ListStartProc.SelectedItem;
                Actions.StartProcess(startItem.Path, startItem.Args);
                Director.Main.ShowNotification($"Started Process: {startItem.Name}");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void EditSelectedStartProcessItem()
        {
            try
            {
                StartProcessItem startItem = (StartProcessItem)ListStartProc.SelectedItem;
                Director.Main.OpenStartProcessWindow(startItem);
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
                    plyWindowVisualSlider.Visibility = Visibility.Visible;
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
                    plyWindowVisualSlider.Visibility = Visibility.Hidden;
                    btnMinimize.Visibility = Visibility.Visible;
                    btnPopInOut.Content = "🢇";
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
                uDebugLogAdd($"{utilTop - actHeight}");
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
        #endregion
    }
}
