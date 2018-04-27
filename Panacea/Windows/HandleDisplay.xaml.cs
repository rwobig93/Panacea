using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Panacea.MainWindow;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for HandleDisplay.xaml
    /// </summary>
    public partial class HandleDisplay : Window, INotifyPropertyChanged
    {
        public HandleDisplay()
        {
            InitializeComponent();
            //Events.UpdateWinHandle += HandleDisplay_UpdateWinHandleInfo;
        }

        #region Variables

        private ProcessOutline processOutline = null;
        private List<Process> _procList = new List<Process>();
        public List<Process> ProcessList
        {
            get { return _procList; }
            set
            {
                _procList = value;
                OnPropertyChanged("ProcessList");
            }
        }

        #endregion

        #region EventHandlers

        private void HandleDisplay_UpdateWinHandleInfo(Classes.WinInfoArgs args)
        {
            try
            {
                if (args == null)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        try
                        {
                            TxtProcName.Text = $"Process Name: Null";
                            TxtProcTitle.Text = $"Process Title: Null";
                            TxtModName.Text = $"Module Name: Null";
                            TxtFilePath.Text = $"File Path: Null";
                            TxtProcLocation.Text = $"Location:{Environment.NewLine}   X: Null{Environment.NewLine}   Y: Null{Environment.NewLine}   Width: Null{Environment.NewLine}   Height: Null";
                        }
                        catch (Exception ex) { Toolbox.LogException(ex); }
                    });
                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        try
                        {
                            TxtProcName.Text = $"Process Name: {args.WindowInfo.Name}";
                            TxtProcTitle.Text = $"Process Title: {args.WindowInfo.Title}";
                            TxtModName.Text = $"Module Name: {args.WindowInfo.ModName}";
                            TxtFilePath.Text = $"File Path: {args.WindowInfo.FileName}";
                            TxtProcLocation.Text = $"Location:{Environment.NewLine}   X: {args.WindowInfo.XValue}{Environment.NewLine}   Y: {args.WindowInfo.YValue}{Environment.NewLine}   Width: {args.WindowInfo.Width}{Environment.NewLine}   Height: {args.WindowInfo.Height}";
                        }
                        catch (Exception ex) { Toolbox.LogException(ex); }
                    });
                }
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateProcessList();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void btnAddProcess_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddProcessToWinList();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnRefreshProcList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateProcessList();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void lbProcList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (lbProcList.SelectedItem != null)
                {
                    uDebugLogAdd($"Proc item wasn't null, item: {lbProcList.SelectedItem.ToString()}");
                    Process proc = (Process)lbProcList.SelectedItem;
                    if (processOutline != null)
                        MoveProcessOutline(proc);
                    else
                        ShowProcessOutline(proc);
                    DisplayProcessInfo(proc);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                CloseProcessOutline();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Methods

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

        private void uDebugLogAdd(string _log, DebugType debugType = DebugType.INFO)
        {
            Toolbox.uAddDebugLog(_log, debugType);
        }

        private void UpdateProcessList()
        {
            try
            {
                var tempProcList = new List<Process>();
                foreach (var proc in Process.GetProcesses())
                {
                    uDebugLogAdd($"Process {proc.ProcessName} MainWinTitle: {proc.MainWindowTitle}");
                    if (!string.IsNullOrWhiteSpace(proc.MainWindowTitle))
                    {
                        uDebugLogAdd($"Process {proc.ProcessName}'s title wasn't empty, adding to proc list");
                        tempProcList.Add(proc);
                    }
                }
                ProcessList = tempProcList;
                uDebugLogAdd("Updated process list");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MoveProcessOutline(Process proc)
        {
            try
            {
                processOutline.UpdateLocation(WindowItem.Create(proc));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShowProcessOutline(Process proc)
        {
            try
            {
                if (processOutline == null)
                    processOutline = new ProcessOutline(WindowItem.Create(proc));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void AddProcessToWinList()
        {
            try
            {
                Process selProc = (Process)lbProcList.SelectedItem;
                uDebugLogAdd($"Adding process to window list, {selProc.ProcessName}, {selProc.MainWindowTitle}");
                Toolbox.settings.AddWindow(WindowItem.Create(selProc));
                uDebugLogAdd("Added process to window list");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CloseProcessOutline()
        {
            try
            {
                uDebugLogAdd("Closing process outline window");
                processOutline.Close();
                processOutline = null;
                uDebugLogAdd("Closed and null'd process outline window");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DisplayProcessInfo(Process proc)
        {
            try
            {
                WindowItem windowItem = WindowItem.Create(proc);
                TxtProcName.Text = $"Process Name: {windowItem.WindowInfo.Name}";
                TxtProcTitle.Text = $"Process Title: {windowItem.WindowInfo.Title}";
                TxtModName.Text = $"Module Name: {windowItem.WindowInfo.ModName}";
                TxtFilePath.Text = $"File Path: {windowItem.WindowInfo.FileName}";
                TxtProcLocation.Text = $"Location:{Environment.NewLine}   X: {windowItem.WindowInfo.XValue}{Environment.NewLine}   Y: {windowItem.WindowInfo.YValue}{Environment.NewLine}   Width: {windowItem.WindowInfo.Width}{Environment.NewLine}   Height: {windowItem.WindowInfo.Height}";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
