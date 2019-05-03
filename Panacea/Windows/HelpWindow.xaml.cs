using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow(HelpMenu menu = HelpMenu.NotificationIcon)
        {
            InitializeComponent();
            Startup(menu);
        }

        private void Startup(HelpMenu menu)
        {
            Director.Main.PopupWindows.Add(this);
            SlideHelpMenu(menu);
        }

        private void BtnSlide_Click(object sender, RoutedEventArgs e)
        {
            SlideHelpMenu(sender);
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
                Toolbox.uAddDebugLog($"POPINFO: {_log}", _type, caller);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SlideHelpMenu(object sender)
        {
            try
            {
                HelpMenu menu = HelpMenu.NotificationIcon;
                if ((Button)sender == BtnSlideNotification)
                    menu = HelpMenu.NotificationIcon;
                else if ((Button)sender == BtnSlideDesktopWin)
                    menu = HelpMenu.DesktopWindow;
                else if ((Button)sender == BtnSlideUtilBar)
                    menu = HelpMenu.UtilityBar;
                else if ((Button)sender == BtnSlideAudioMenu)
                    menu = HelpMenu.AudioMenu;
                else if ((Button)sender == BtnSlideNetworkMenu)
                    menu = HelpMenu.NetworkMenu;
                else if ((Button)sender == BtnSlideMacPopup)
                    menu = HelpMenu.MacPopup;
                else if ((Button)sender == BtnSlideSettingsMenu)
                    menu = HelpMenu.SettingsMenu;
                else if ((Button)sender == BtnSlideWindowsMenu)
                    menu = HelpMenu.WindowsMenu;
                else if ((Button)sender == BtnSlideAddWindows)
                    menu = HelpMenu.AddWindowsMenu;
                else if ((Button)sender == BtnSlideStartProc)
                    menu = HelpMenu.StartProcessMenu;
                else if ((Button)sender == BtnSlidePopoutFeat)
                    menu = HelpMenu.PopoutFeature;
                else
                    menu = HelpMenu.NotificationIcon;
                SlideHelpMenu(menu);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void SlideHelpMenu(HelpMenu menu)
        {
            try
            {
                Thickness slideTo = new Thickness(140, 0, 0, -7618);
                switch (menu)
                {
                    case HelpMenu.NotificationIcon:
                        slideTo.Top -= 0;
                        slideTo.Bottom += 0;
                        break;
                    case HelpMenu.DesktopWindow:
                        slideTo.Top -= 760;
                        slideTo.Bottom += 760;
                        break;
                    case HelpMenu.UtilityBar:
                        slideTo.Top -= (760 * 2);
                        slideTo.Bottom += (760 * 2);
                        break;
                    case HelpMenu.AudioMenu:
                        slideTo.Top -= (760 * 3);
                        slideTo.Bottom += (760 * 3);
                        break;
                    case HelpMenu.NetworkMenu:
                        slideTo.Top -= (760 * 4);
                        slideTo.Bottom += (760 * 4);
                        break;
                    case HelpMenu.MacPopup:
                        slideTo.Top -= (760 * 5);
                        slideTo.Bottom += (760 * 5);
                        break;
                    case HelpMenu.SettingsMenu:
                        slideTo.Top -= (760 * 6);
                        slideTo.Bottom += (760 * 6);
                        break;
                    case HelpMenu.WindowsMenu:
                        slideTo.Top -= (760 * 7);
                        slideTo.Bottom += (760 * 7);
                        break;
                    case HelpMenu.AddWindowsMenu:
                        slideTo.Top -= (760 * 8);
                        slideTo.Bottom += (760 * 8);
                        break;
                    case HelpMenu.StartProcessMenu:
                        slideTo.Top -= (760 * 9);
                        slideTo.Bottom += (760 * 9);
                        break;
                    case HelpMenu.PopoutFeature:
                        slideTo.Top -= (760 * 10);
                        slideTo.Bottom += (760 * 10);
                        break;
                    default:
                        slideTo.Top -= 0;
                        slideTo.Bottom += 0;
                        break;
                }
                Toolbox.AnimateGrid(GridHelpSlides, slideTo);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Director.Main.PopupWindows.Remove(this);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                    DragMove();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
    }
}
