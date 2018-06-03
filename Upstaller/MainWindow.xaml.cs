using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Upstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public bool argUpdate { get; set; } = false;
        public string currentDir { get; set; }
        private Octokit.GitHubClient gitClient = null;

        private void mainWin_Loaded(object sender, RoutedEventArgs e)
        {
            Startup();
        }

        private void Startup()
        {
            InitializeVisualElements();
        }

        private void InitializeVisualElements()
        {
            UpdateDirectory();
            VerifyInstallation();
            tCheckForUpdates();
        }

        private void VerifyInstallation()
        {
            try
            {
                var panacea = $@"{currentDir}\Panacea.exe";
                if (File.Exists(panacea))
                {
                    ToggleInstallationLabel(true);
                }
                else
                    ToggleInstallationLabel(false);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ToggleInstallationLabel(bool v)
        {
            try
            {
                if (v)
                {
                    txtLabelInstalled.Foreground = new SolidColorBrush(Colors.LawnGreen);
                }
                else
                {
                    txtLabelInstalled.Foreground = new SolidColorBrush(Colors.Red);
                    txtLabelInstalled.Text = "Not Installed";
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateDirectory()
        {
            try
            {
                currentDir = Directory.GetCurrentDirectory();
                txtDirectory.Text = currentDir;
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
                Toolbox.uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
                uStatusUpdate("An Exception was logged");
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
                Toolbox.uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
                uStatusUpdate("An Exception was logged");
            }
        }

        private void uStatusUpdate(string update)
        {
            try
            {
                txtStatus.Text += $"{update}{Environment.NewLine}";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO)
        {
            try
            {
                Toolbox.uDebugLogAdd(_log, _type);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private Version GetVersionNumber(string assemblyPath)
        {
            return Assembly.LoadFile(assemblyPath).GetName().Version;
        }

        private void tCheckForUpdates()
        {
            try
            {
                uDebugLogAdd("Checking for updates...");
                if (gitClient == null)
                {
                    gitClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("Panacea"));
                    uDebugLogAdd("gitClient was null, initialized gitClient");
                }
                string executable = "Panacea.exe";
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        var currentVersion = GetVersionNumber();
                        uDebugLogAdd($"Current Version: {Toolbox.settings.CurrentVersion}");
                        Task t = GetUpdate(executable);
                        t.Start();
                        while (!t.IsCompleted)
                        {
                            Thread.Sleep(500);
                        }
                        worker.ReportProgress(1);
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
                            if (Toolbox.settings.CurrentVersion.CompareTo(Toolbox.settings.ProductionVersion) < 0)
                            {
                                uDebugLogAdd($"New version found: [c]{Toolbox.settings.CurrentVersion} [p]{Toolbox.settings.ProductionVersion}");
                                ShowNotification("A new version is available, update when ready");
                                Toolbox.settings.UpdateAvailable = true;
                                btnMenuUpdate.IsEnabled = true;
                                uDebugLogAdd("Enabled update button");
                            }
                            else
                                uDebugLogAdd($"Current version is the same or newer than release: [c]{Toolbox.settings.CurrentVersion} [p]{Toolbox.settings.ProductionVersion}");
                            SaveSettings();
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
    }
}
