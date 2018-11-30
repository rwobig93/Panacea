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
        }

        List<ChangeLogItem> changeLogs = new List<ChangeLogItem>();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateTestVersions();
        }

        private void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            Toolbox.LogException(ex, lineNum, caller, path);
        }

        private void PopulateTestVersions()
        {
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    int bugs = Toolbox.GenerateRandomNumber(0, 2);
                    int features = Toolbox.GenerateRandomNumber(0, 2);
                    int beta = Toolbox.GenerateRandomNumber(0, 1);
                    changeLogs.Add(new ChangeLogItem()
                    {
                        Version = $"0.1.6903.387{i}",
                        BugFixes = bugs == 1 ? Visibility.Visible : Visibility.Hidden,
                        NewFeatures = features == 1 ? Visibility.Visible : Visibility.Hidden,
                        BetaRelease = Visibility.Visible
                    });
                }
                lbVersions.ItemsSource = changeLogs.OrderBy(x => x.Version);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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
    }
}
