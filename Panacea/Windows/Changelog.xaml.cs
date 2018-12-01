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
    /// Interaction logic for Changelog.xaml
    /// </summary>
    public partial class Changelog : Window
    {
        public Changelog()
        {
            InitializeComponent();
            Startup();
        }

        private void Startup()
        {
            DisplayChangeLogs();
            SelectCurrentVersion();
        }

        private void SelectCurrentVersion()
        {
            try
            {
                var currentVersion = Toolbox.settings.CurrentVersion.ToString();
                ChangeLogItem curVerItem = null;
                foreach (var item in lbVersions.Items)
                {
                    if (((ChangeLogItem)item).Version == $"v{currentVersion}")
                    {
                        Toolbox.uAddDebugLog($"Found current version changelog item | [v]{currentVersion} [iv]{((ChangeLogItem)item).Version}");
                        curVerItem = ((ChangeLogItem)item);
                        break;
                    }
                }
                if (curVerItem != null)
                {
                    Toolbox.uAddDebugLog("We found the changelog item for the current version, changing current selected item");
                    lbVersions.SelectedItem = curVerItem;
                    Toolbox.uAddDebugLog($"Changed selected item to: {curVerItem.Version}");
                }
                else
                    Toolbox.uAddDebugLog("Couldn't find a matching changelog item for the current running version, didn't auto select");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DisplayChangeLogs()
        {
            try
            {
                lbVersions.ItemsSource = null;
                lbVersions.ItemsSource = Toolbox.changeLogs.OrderByDescending(x => x.Version);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            Toolbox.LogException(ex, lineNum, caller, path);
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

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LbVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Toolbox.uAddDebugLog("Setting changelog textbox to selected changelog body");
                txtChangeLogText.Text = Toolbox.changeLogs.Find(x => x.Version == ((ChangeLogItem)lbVersions.SelectedItem).Version).Body;
                Toolbox.uAddDebugLog("Now showing changelog body");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
    }
}
