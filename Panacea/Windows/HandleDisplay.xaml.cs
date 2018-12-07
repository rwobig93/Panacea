using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
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
        private List<WindowListItem> _windowList = new List<WindowListItem>();
        private List<string> _notificationList = new List<string>();
        private bool _playingNotification = false;
        private LoadingType _loadingInProgress = LoadingType.Done;
        public List<Process> ProcessList
        {
            get { return _procList; }
            set
            {
                _procList = value;
                OnPropertyChanged("ProcessList");
            }
        }
        public List<WindowListItem> WindowList
        {
            get { return _windowList; }
            set
            {
                _windowList = value;
                OnPropertyChanged("WindowList");
            }
        }
        private enum LoadingType
        {
            Start,
            Done
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
                //UpdateProcessList();
                UpdateWindowList();
                SendUserUpdateNotification("Let's get those processes!");
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
                uDebugLogAdd("Refresh clicked, updating process list");
                //UpdateProcessList();
                UpdateWindowList();
                SendUserUpdateNotification("Refreshed Process List");
                uDebugLogAdd("Finished updating process list");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void lbProcList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //try
            //{
            //    if (lbProcList.SelectedItem != null)
            //    {
            //        uDebugLogAdd($"Proc item wasn't null, item: {lbProcList.SelectedItem.ToString()}");
            //        Process proc = (Process)lbProcList.SelectedItem;
            //        if (processOutline != null)
            //            MoveProcessOutline(proc);
            //        else
            //            ShowProcessOutline(proc);
            //        DisplayProcessInfo(proc);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogException(ex);
            //}
            try
            {
                if (lbProcList.SelectedItem != null)
                {
                    try
                    {
                        uDebugLogAdd($"Proc item wasn't null, item: {lbProcList.SelectedItem.ToString()}");
                        WindowListItem proc = (WindowListItem)lbProcList.SelectedItem;
                        if (processOutline != null)
                            MoveProcessOutline(proc.Handle);
                        else
                            ShowProcessOutline(proc.Handle);
                        DisplayProcessInfo(proc);
                    }
                    catch (Win32Exception we) { uDebugLogAdd($"Unable to draw process outline: {we.Message}"); lbProcList.Items.Remove(lbProcList.SelectedItem); return; }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
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
            uDebugLogAdd("Closing HandleDisplay window");
            SendUserUpdateNotification("Closing...");
            this.Close();
            uDebugLogAdd("HandleDisplay window closed");
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

        private void uDebugLogAdd(string _log, DebugType debugType = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            Toolbox.uAddDebugLog(_log, debugType, caller);
        }

        private void UpdateProcessList()
        {
            try
            {
                var tempProcList = new List<Process>();
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    foreach (var proc in Process.GetProcesses())
                    {
                        var rect = WindowInfo.GetProcessDimensions(proc);
                        uDebugLogAdd($"Process {proc.ProcessName} | T{rect.Top} L{rect.Left} H{rect.Bottom - rect.Top} W{rect.Right - rect.Left}");
                        if (WindowInfo.DoesProcessHandleHaveSize(proc))
                        {
                            uDebugLogAdd($"Process {proc.ProcessName} has size");
                            uDebugLogAdd($"Process {proc.ProcessName} doesn't currently exist in the proclist, adding process");
                            tempProcList.Add(proc);
                            try
                            {
                                StringBuilder sb = new StringBuilder(1024);
                                WinAPIWrapper.GetClassName(proc.MainWindowHandle, sb, sb.Capacity);
                                uDebugLogAdd($"Process added: {proc.ProcessName} | Class: {sb.ToString()}");
                            }
                            catch
                            {
                                uDebugLogAdd($"Process {proc.ProcessName} was added but couldn't get the class");
                            }
                        }
                    }
                    worker.ReportProgress(1);
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    if (pe.ProgressPercentage == 1)
                    {

                        lbProcList.ItemsSource = null;
                        ProcessList = tempProcList.OrderBy(x => x.ProcessName).ToList();
                        lbProcList.ItemsSource = ProcessList;
                        uDebugLogAdd("Updated process list");
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateWindowList()
        {
            try
            {
                ToggleLoading(LoadingType.Start);
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var tempWindowList = new List<WindowListItem>();
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        foreach (var proc in Process.GetProcesses())
                        {
                            try
                            {
                                var rect = WindowInfo.GetProcessDimensions(proc);
                                uDebugLogAdd($"Process {proc.ProcessName} | T{rect.Top} L{rect.Left} H{rect.Bottom - rect.Top} W{rect.Right - rect.Left}");
                                if (WindowInfo.DoesProcessHandleHaveSize(proc))
                                {
                                    uDebugLogAdd($"Process {proc.ProcessName} has size");
                                    if (tempWindowList.Find(x => x.Process.MainWindowHandle == proc.MainWindowHandle) == null)
                                    {
                                        uDebugLogAdd($"Process {proc.ProcessName} doesn't currently exist in the windowList, adding process");
                                        foreach (var handle in WinAPIWrapper.EnumerateProcessWindowHandles(proc.Id))
                                        {
                                            try
                                            {
                                                if (WindowInfo.DoesHandleHaveSize(handle) && WinAPIWrapper.IsWindowVisible(handle))
                                                {
                                                    var windowListItem = WindowListItem.Create(proc, handle);
                                                    if (tempWindowList.Find(x => x.Display == windowListItem.Display) == null)
                                                    {
                                                        tempWindowList.Add(windowListItem);
                                                        uDebugLogAdd($"Added to list | [{windowListItem.Handle}]{windowListItem.Display}");
                                                    }
                                                    else
                                                        uDebugLogAdd($"Item already in the list, skipping | {windowListItem.Display}");
                                                }
                                                else
                                                    uDebugLogAdd($"Handle window doesn't have size, skipping | [{handle}]{proc.ProcessName}");
                                            }
                                            catch (Exception ex)
                                            {
                                                uDebugLogAdd($"Unable to add handle to the list | [{handle}]{proc.ProcessName}: {ex.Message}");
                                            }
                                        }
                                    }
                                    else
                                        uDebugLogAdd($"Already enumerated through handles for {proc.ProcessName}, skipping this one");
                                }
                            }
                            catch (Exception ex)
                            {
                                uDebugLogAdd($"Unable to get proc {proc.ProcessName}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                    worker.ReportProgress(1);
                };
                worker.ProgressChanged += (ps, pe) =>
                {
                    if (pe.ProgressPercentage == 1)
                    {

                        lbProcList.ItemsSource = null;
                        WindowList = tempWindowList.OrderBy(x => x.Display).ToList();
                        lbProcList.ItemsSource = WindowList;
                        stopWatch.Stop();
                        uDebugLogAdd($"Updated window list, took: {stopWatch.Elapsed.Seconds}s {stopWatch.Elapsed.Milliseconds}ms");
                        ToggleLoading(LoadingType.Done);
                    }
                };
                worker.RunWorkerAsync();
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

        private void MoveProcessOutline(IntPtr handle)
        {
            try
            {
                processOutline.UpdateLocation(handle);
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
                WindowItem windowItem = WindowItem.Create(proc);
                if (processOutline == null)
                {
                    processOutline = new ProcessOutline(windowItem);
                    processOutline.Show();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShowProcessOutline(IntPtr handle)
        {
            try
            {
                if (processOutline == null)
                {
                    processOutline = new ProcessOutline(handle);
                    processOutline.Show();
                }
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
                bool optionChecked = false;
                WindowListItem selProc = (WindowListItem)lbProcList.SelectedItem;
                ProcessOptions options = new ProcessOptions();
                if (chkIgnoreWinTitle.IsChecked == true)
                {
                    options.IgnoreProcessTitle = true;
                    optionChecked = true;
                }
                uDebugLogAdd($"Adding process to window list, {selProc.Display}");
                if (optionChecked)
                    Toolbox.settings.AddWindow(WindowItem.Create(selProc.Process, selProc.Handle, options, selProc.Title));
                else
                    Toolbox.settings.AddWindow(WindowItem.Create(selProc.Process, selProc.Handle, null, selProc.Title));
                uDebugLogAdd("Added process to window list");
                SendUserUpdateNotification($"Added entry for this process: {selProc.Display} ");
                //lbProcList.Items.Remove(selProc);
                //uDebugLogAdd("Removed existing selected item from the window list");
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
                if (processOutline != null)
                {
                    uDebugLogAdd("Closing process outline window");
                    processOutline.Close();
                    processOutline = null;
                    uDebugLogAdd("Closed and null'd process outline window");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void DisplayProcessInfo(WindowListItem proc)
        {
            try
            {
                var dimensions = WindowInfo.GetHandleDimensions(proc.Handle);
                TxtProcName.Text = $"Process Name: {proc.Process.ProcessName}";
                TxtProcTitle.Text = $"Process Title: {proc.Title}";
                TxtModName.Text = $"Module Name: {proc.Process.MainModule.ModuleName}";
                TxtFilePath.Text = $"File Path: {proc.Process.MainModule.FileName}";
                TxtProcLocation.Text = $"Location:{Environment.NewLine}   X: {dimensions.Left}{Environment.NewLine}   Y: {dimensions.Top}{Environment.NewLine}   Width: {dimensions.Right - dimensions.Left}{Environment.NewLine}   Height: {dimensions.Bottom - dimensions.Top}";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SendUserUpdateNotification(string update)
        {
            try
            {
                uDebugLogAdd($"Adding notification: {update}");
                _notificationList.Add(update);
                if (!_playingNotification)
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

        private void ToggleLoading(LoadingType loadingType)
        {
            try
            {
                uDebugLogAdd($"Toggling loading label to: {loadingType.ToString()}");
                if (_loadingInProgress != LoadingType.Start && loadingType == LoadingType.Start)
                {
                    uDebugLogAdd($"Current loading: {_loadingInProgress.ToString()} | Toggled to: {loadingType.ToString()} | Starting loading label up");
                    tLoadingStart();
                }
                else if (loadingType == LoadingType.Done)
                {
                    uDebugLogAdd($"Setting loading to: {loadingType.ToString()}");
                    _loadingInProgress = LoadingType.Done;
                }
                else
                    _loadingInProgress = LoadingType.Done;
                uDebugLogAdd($"Loading now: {loadingType.ToString()}");
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
                _playingNotification = true;
                DoubleAnimation animation = new DoubleAnimation()
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(2)
                };
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {

                    try
                    {
                        while (_notificationList.ToList().Count > 0)
                        {
                            worker.ReportProgress(1);
                            Thread.Sleep(TimeSpan.FromSeconds(3));
                            _notificationList.RemoveAt(0);
                        }
                        _playingNotification = false;
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
                            lblUpdateNotification.Text = _notificationList.ToList()[0];
                            lblUpdateNotification.BeginAnimation(TextBlock.OpacityProperty, animation);
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

        private void tLoadingStart()
        {
            try
            {
                _loadingInProgress = LoadingType.Start;
                DoubleAnimation animateOut = new DoubleAnimation()
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(.51)
                };
                DoubleAnimation animateIn = new DoubleAnimation()
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromSeconds(.51)
                };
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (ws, we) =>
                {
                    try
                    {
                        uDebugLogAdd("Starting loading label");
                        worker.ReportProgress(1);
                        while (_loadingInProgress == LoadingType.Start)
                        {
                            worker.ReportProgress(3);
                            Thread.Sleep(500);
                            worker.ReportProgress(4);
                            Thread.Sleep(500);
                        }
                        worker.ReportProgress(2);
                        uDebugLogAdd("Finishing loading label");
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
                        switch (pe.ProgressPercentage)
                        {
                            case 1:
                                lblLoading.Visibility = Visibility.Visible;
                                break;
                            case 2:
                                lblLoading.Visibility = Visibility.Hidden;
                                break;
                            case 3:
                                lblLoading.BeginAnimation(TextBlock.OpacityProperty, animateOut);
                                break;
                            case 4:
                                lblLoading.BeginAnimation(TextBlock.OpacityProperty, animateIn);
                                break;
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

    public class ProcessOptions
    {
        public bool IgnoreProcessTitle { get; set; }
    }
}
