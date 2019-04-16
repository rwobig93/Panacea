using Panacea.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Forms;
using static Panacea.MainWindow;
using System.Windows.Input;
using static Panacea.Windows.UtilityBar;
using NAudio.CoreAudioApi;
using System.Net.NetworkInformation;
using NativeWifi;

namespace Panacea.Classes
{

    #region Enums

    public enum DebugType
    {
        EXCEPTION,
        STATUS,
        INFO,
        FAILURE
    }

    public enum AppUpdate
    {
        Panacea,
        Upstaller
    }

    public enum Direction
    {
        Up,
        Down
    }

    public enum WindowProfile
    {
        Profile1,
        Profile2,
        Profile3,
        Profile4
    }

    public enum StartProfile
    {
        Start1,
        Start2,
        Start3,
        Start4
    }

    public enum StartEditorAction
    {
        Create,
        Edit
    }

    #region General

    public class DetailedProcess
    {
        public IntPtr Handle { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public static DetailedProcess Create(Process process, IntPtr handle)
        {
            return new DetailedProcess()
            {
                Handle = handle,
                Name = process.ProcessName,
                Title = WinAPIWrapper.GetWindowText(handle)
            };
        }
    }
    public class StartProcessItem
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Args { get; set; }
        public bool MoveAfterStart { get; set; } = true;
    }
    public class ChangeLogItem
    {
        public string Version { get; set; }
        public string Body { get; set; }
        public SolidColorBrush VersionColor { get; set; } = new SolidColorBrush(Toolbox.ColorFromHex("#FFA0A0A0"));
        public Visibility BugFixes { get; set; }
        public Visibility NewFeatures { get; set; }
        public Visibility BetaRelease { get; set; }
    }
    public class Display
    {
        public System.Drawing.Rectangle Bounds { get; set; }
        public System.Drawing.Rectangle WorkingArea { get; set; }
        public string DeviceName { get; set; }
        public bool PrimaryDisplay { get; set; }
        public static Display ConvertFromScreen(Screen screen)
        {
            return new Display()
            {
                Bounds = screen.Bounds,
                WorkingArea = screen.WorkingArea,
                PrimaryDisplay = screen.Primary,
                DeviceName = screen.DeviceName
            };
        }
    }
    public class CurrentDisplay
    {
        public List<Display> Displays { get; set; } = new List<Display>();
        public int LeftMostBound {  get { return Displays.OrderBy(x => x.Bounds.X).First().Bounds.X; } }
        public int RightMostBound { get { return Displays.OrderBy(x => x.Bounds.X).Last().Bounds.X + Displays.OrderBy(x => x.Bounds.X).Last().Bounds.Width; } }
        public int TopMostBound { get { return Displays.OrderBy(x => x.Bounds.Y).First().Bounds.Y; } }
        public int BottomMostBound { get { return Displays.OrderBy(x => x.Bounds.Y).Last().Bounds.Y + Displays.OrderBy(x => x.Bounds.Y).Last().Bounds.Height; } }
        public int LeftMostWorkArea { get { return Displays.OrderBy(x => x.WorkingArea.X).First().WorkingArea.X; } }
        public int RightMostWorkArea { get { return Displays.OrderBy(x => x.WorkingArea.X).Last().WorkingArea.X + Displays.OrderBy(x => x.WorkingArea.X).Last().WorkingArea.Width; } }
        public int TopMostWorkArea { get { return Displays.OrderBy(x => x.WorkingArea.Y).First().WorkingArea.Y; } }
        public int BottomMostWorkArea { get { return Displays.OrderBy(x => x.WorkingArea.Height).Last().WorkingArea.Y + Displays.OrderBy(x => x.WorkingArea.Y).Last().WorkingArea.Height; } }
        public System.Drawing.Rectangle TotalBounds { get { return new System.Drawing.Rectangle() { X = LeftMostBound, Width = RightMostBound - LeftMostBound, Y = TopMostBound, Height = BottomMostBound - TopMostBound, Location = new System.Drawing.Point(LeftMostBound, TopMostBound), Size = new System.Drawing.Size(RightMostBound - LeftMostBound, BottomMostBound - TopMostBound) }; } }
        public System.Drawing.Rectangle TotalWorkArea { get { return new System.Drawing.Rectangle() { X = LeftMostWorkArea, Width = RightMostWorkArea - LeftMostWorkArea, Y = TopMostWorkArea, Height = BottomMostWorkArea - TopMostWorkArea, Location = new System.Drawing.Point(LeftMostWorkArea, TopMostWorkArea), Size = new System.Drawing.Size(RightMostWorkArea - LeftMostWorkArea, BottomMostWorkArea - TopMostWorkArea) }; } }
    }
    public class DisplayProfile
    {
        public WindowProfile LinkedWindowProfile { get; set; }
        public CurrentDisplay DisplayArea { get; set; }
        public Display PreferredDisplay { get; set; }
        public static bool DoDisplaysMatch(CurrentDisplay display1, CurrentDisplay display2)
        { 
            bool isAMatch = true;
            if (display1.Displays.Count != display2.Displays.Count)
                isAMatch = false;
            if (display1.LeftMostBound != display2.LeftMostBound)
                isAMatch = false;
            if (display1.RightMostBound != display2.RightMostBound)
                isAMatch = false;
            if (display1.TopMostBound != display2.TopMostBound)
                isAMatch = false;
            if (display1.BottomMostBound != display2.BottomMostBound)
                isAMatch = false;
            if (display1.LeftMostWorkArea != display2.LeftMostWorkArea)
                isAMatch = false;
            if (display1.RightMostWorkArea != display2.RightMostWorkArea)
                isAMatch = false;
            if (display1.TopMostWorkArea != display2.TopMostWorkArea)
                isAMatch = false;
            if (display1.BottomMostWorkArea != display2.BottomMostWorkArea)
                isAMatch = false;
            if (display1.TotalBounds != display2.TotalBounds)
                isAMatch = false;
            if (display1.TotalWorkArea != display2.TotalWorkArea)
                isAMatch = false;
            return isAMatch;
        }
    }
    public class DisplayProfileLibrary
    {
        public List<DisplayProfile> DisplayProfiles { get; set; } = new List<DisplayProfile>();
        public DisplayProfile CurrentDisplayProfile { get; set; }
        public static bool IsCurrentDisplayCorrect()
        {
            return false;
        }
    }
    public class PopoutPreferences
    {
        public PopupMenu PopupType { get; set; }
        public System.Drawing.Rectangle PreferredLocation { get; set; }
        public bool PoppedOut { get; set; }
    }

    #endregion

    #region Events

    public class Events
    {
        // Debug Log Add Event
        public delegate void AddDebugLogStatus(DebugUpdateArgs args);
        public static event AddDebugLogStatus UpdateDebugStatus;
        public static void AddDebugStatus(string logUpdate, DebugType debugType = DebugType.INFO)
        {
            DebugUpdateArgs args = new DebugUpdateArgs(logUpdate, debugType);
            UpdateDebugStatus(args);
        }
        // Util Bar Move Event
        public delegate void UtilBarMoved(UtilMoveArgs args);
        public static event UtilBarMoved UtilBarMoveTrigger;
        public static void TriggerUtilBarMove(int x, int y)
        {
            UtilMoveArgs args = new UtilMoveArgs(x, y);
            UtilBarMoveTrigger(args);
        }
        // Window info changed
        public delegate void WindowInfoChanged(WindowInfoArgs args);
        public static event WindowInfoChanged WinInfoChanged;
        public static void TriggerWindowInfoChange(bool update = true)
        {
            WindowInfoArgs args = new WindowInfoArgs(update);
            WinInfoChanged(args);
        }
        // Audio endpoint list updated
        public delegate void AudioEndpointsUpdated(AudioEndpointListArgs args);
        public static event AudioEndpointsUpdated AudioEndpointListUpdated;
        public static void TriggerAudioEndpointListUpdate(DataFlow flow)
        {
            AudioEndpointListArgs args = new AudioEndpointListArgs(flow);
            AudioEndpointListUpdated(args);
        }
        // Start info changed
        public delegate void StartInfoChanged(StartProcArgs args);
        public static event StartInfoChanged StartProcInfoChanged;
        public static void TriggerStartInfoChange(bool update = true)
        {
            StartProcArgs args = new StartProcArgs(update);
            StartProcInfoChanged(args);
        }
        // Network Connectivity Status Updated
        public delegate void NetConnectionUpdated(NetConnectivityArgs args);
        public static event NetConnectionUpdated NetConnectivityChanged;
        public static void TriggerNetConnectivityUpdate(ConnectivityStatus conStatus, NetConnectionType conType, double? wLinkSpeed, double? eLinkSpeed, NetworkInterface currEth, WlanClient.WlanInterface currWlan)
        {
            NetConnectivityArgs args = new NetConnectivityArgs(conStatus, conType, wLinkSpeed, eLinkSpeed, currEth, currWlan);
            NetConnectivityChanged(args);
        }
    }

    #region Event Args
    public class DebugUpdateArgs : EventArgs
    {
        public DebugUpdateArgs(string logUpdate, DebugType debugType = DebugType.INFO)
        {
            this.LogUpdate = logUpdate;
            this.DebugType = debugType;
        }
        public string LogUpdate { get; }
        public DebugType DebugType { get; }
    }
    public class UtilMoveArgs : EventArgs
    {
        public UtilMoveArgs(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
        public int X { get; }
        public int Y { get; }
    }
    public class WindowInfoArgs : EventArgs
    {
        public WindowInfoArgs(bool update)
        {
            this.Update = update;
        }
        public bool Update { get; }
    }
    public class StartProcArgs : EventArgs
    {
        public StartProcArgs(bool update)
        {
            this.Update = update;
        }
        public bool Update { get; }
    }
    public class AudioEndpointListArgs : EventArgs
    {
        public AudioEndpointListArgs(DataFlow dataFlow)
        {
            this.Flow = dataFlow;
        }
        public DataFlow Flow { get; }
    } 
    public class NetConnectivityArgs : EventArgs
    {
        public NetConnectivityArgs(ConnectivityStatus conStatus, NetConnectionType conType, double? wLinkSpeed, double? eLinkSpeed, NetworkInterface currEth, WlanClient.WlanInterface currWlan)
        {
            this.ConnectionStatus = conStatus;
            this.ConnectionType = conType;
            this.WifiLinkSpeed = wLinkSpeed;
            this.EthLinkSpeed = eLinkSpeed;
            this.CurrentEthIf = CurrentEthIf;
            this.CurrentWlanIf = currWlan;
        }
        public ConnectivityStatus ConnectionStatus { get; }
        public NetConnectionType ConnectionType { get; }
        public double? WifiLinkSpeed { get; }
        public double? EthLinkSpeed { get; }
        public NetworkInterface CurrentEthIf { get; }
        public WlanClient.WlanInterface CurrentWlanIf { get; }
    }
    #endregion

    #endregion

    #endregion

    #region Window Classes

    public class WindowDimensions
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
    }
    public class WindowListItem
    {
        public Process Process { get; set; }
        public string Title { get; set; }
        public IntPtr Handle { get; set; }
        public string Display { get; set; }
        public static WindowListItem Create(Process process, IntPtr handle)
        {
            var title = WinAPIWrapper.GetWindowText(handle);
            return new WindowListItem()
            {
                Process = process,
                Handle = handle,
                Title = title,
                Display = $"{process.ProcessName} | {title}"
            };
        }
    }
    public class WindowInfo
    {
        public string Name { get; set; }
        public string ModName { get; set; }
        public string WinClass { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public int PrivateID { get; set; } = Toolbox.GenerateRandomNumber();
        public int IndexNum { get; set; }
        public int XValue { get; set; }
        public int YValue { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public static WindowInfo GetWindowInfoFromProc(Process proc, ProcessOptions options = null, string winTitle = "")
        {
            if (proc != null)
            {
                var dimensions = GetProcessDimensions(proc);
                StringBuilder sb = new StringBuilder(1024);
                var title = string.IsNullOrWhiteSpace(winTitle) ? proc.MainWindowTitle : winTitle;
                WinAPIWrapper.GetClassName(proc.MainWindowHandle, sb, sb.Capacity);
                if (options == null)
                {
                    return new WindowInfo()
                    {
                        Name = proc.ProcessName,
                        ModName = proc.MainModule.ModuleName,
                        WinClass = sb.ToString(),
                        Title = title,
                        FileName = proc.MainModule.FileName,
                        IndexNum = 0,
                        XValue = dimensions.Left,
                        YValue = dimensions.Top,
                        Width = dimensions.Right - dimensions.Left,
                        Height = dimensions.Bottom - dimensions.Top
                    };
                }
                else
                {
                    return new WindowInfo()
                    {
                        Name = proc.ProcessName,
                        ModName = proc.MainModule.ModuleName,
                        WinClass = sb.ToString(),
                        Title = options.IgnoreProcessTitle ? "*" : title,
                        FileName = proc.MainModule.FileName,
                        IndexNum = 0,
                        XValue = dimensions.Left,
                        YValue = dimensions.Top,
                        Width = dimensions.Right - dimensions.Left,
                        Height = dimensions.Bottom - dimensions.Top
                    };
                }
            }
            else
            {
                return new WindowInfo()
                {
                    Name = "NULL",
                    ModName = "NULL",
                    Title = "NULL",
                    FileName = "NULL",
                    IndexNum = -1,
                    XValue = -1,
                    YValue = -1,
                    Width = -1,
                    Height = -1
                };
            }
        }
        public static WindowInfo GetWindowInfoFromProc(Process proc, IntPtr handle, ProcessOptions options = null, string winTitle = "")
        {
            if (proc != null)
            {
                var dimensions = GetHandleDimensions(handle);
                StringBuilder sb = new StringBuilder(1024);
                var title = string.IsNullOrWhiteSpace(winTitle) ? proc.MainWindowTitle : winTitle;
                WinAPIWrapper.GetClassName(proc.MainWindowHandle, sb, sb.Capacity);
                if (options == null)
                {
                    return new WindowInfo()
                    {
                        Name = proc.ProcessName,
                        ModName = proc.MainModule.ModuleName,
                        WinClass = sb.ToString(),
                        Title = title,
                        FileName = proc.MainModule.FileName,
                        IndexNum = 0,
                        XValue = dimensions.Left,
                        YValue = dimensions.Top,
                        Width = dimensions.Right - dimensions.Left,
                        Height = dimensions.Bottom - dimensions.Top
                    };
                }
                else
                {
                    return new WindowInfo()
                    {
                        Name = proc.ProcessName,
                        ModName = proc.MainModule.ModuleName,
                        WinClass = sb.ToString(),
                        Title = options.IgnoreProcessTitle ? "*" : title,
                        FileName = proc.MainModule.FileName,
                        IndexNum = 0,
                        XValue = dimensions.Left,
                        YValue = dimensions.Top,
                        Width = dimensions.Right - dimensions.Left,
                        Height = dimensions.Bottom - dimensions.Top
                    };
                }
            }
            else
            {
                return new WindowInfo()
                {
                    Name = "NULL",
                    ModName = "NULL",
                    Title = "NULL",
                    FileName = "NULL",
                    IndexNum = -1,
                    XValue = -1,
                    YValue = -1,
                    Width = -1,
                    Height = -1
                };
            }
        }
        public static WinAPIWrapper.RECT GetProcessDimensions(Process proc)
        {
            WinAPIWrapper.RECT dimensions = new WinAPIWrapper.RECT();
            WinAPIWrapper.GetWindowRect(proc.MainWindowHandle, ref dimensions);
            return dimensions;
        }
        public static WinAPIWrapper.RECT GetHandleDimensions(IntPtr handle)
        {
            WinAPIWrapper.RECT dimensions = new WinAPIWrapper.RECT();
            WinAPIWrapper.GetWindowRect(handle, ref dimensions);
            return dimensions;
        }
        public static bool DoesProcessHandleHaveSize(Process proc)
        {
            var rect = GetProcessDimensions(proc);
            if (rect.Left == 0 &&
                rect.Top == 0 &&
                rect.Bottom == 0 &&
                rect.Right == 0
                )
            {
                return false;
            }
            else
                return true;
        }
        public static bool DoesHandleHaveSize(IntPtr handle)
        {
            var rect = GetHandleDimensions(handle);
            if (rect.Left == 0 &&
                rect.Top == 0 &&
                rect.Bottom == 0 &&
                rect.Right == 0
                )
            {
                return false;
            }
            else
                return true;
        }
        public static bool DoesWindowInfoHaveClass(WindowInfo winInfo)
        {
            return !string.IsNullOrWhiteSpace(winInfo.WinClass);
        }
    }
    public class WindowItem
    {
        public WindowInfo WindowInfo { get; set; }
        public string WindowSum { get { return $"{this.WindowInfo.Name} | {this.WindowInfo.Title}"; } }
        public string WindowName { get; set; } = "";
        public string Enabled { get; set; } = "On";
        public string Checked { get; set; } = "";
        public Brush EnableColor { get; set; } = Defaults.WinEnableButtonColorOn;
        public static WindowItem Create(Process process, ProcessOptions options = null, string winTitle = "")
        {
            return new WindowItem()
            {
                WindowInfo = WindowInfo.GetWindowInfoFromProc(process, options, winTitle),
                WindowName = process.ProcessName
            };
        }
        public static WindowItem Create(Process process, IntPtr handle, ProcessOptions options = null, string winTitle = "")
        {
            return new WindowItem()
            {
                WindowInfo = WindowInfo.GetWindowInfoFromProc(process, handle, options, winTitle),
                WindowName = process.ProcessName
            };
        }
        public static WindowItem GetWindowItemUpdate(WindowItem winItem, Process proc)
        {
            return new WindowItem()
            {

            };
        }
        public static bool DoesDuplicateExist(WindowItem windowItem)
        {
            var existingWinItem = Toolbox.settings.ActiveWindowList.ToList().Find(x => x.WindowInfo.Name == windowItem.WindowInfo.Name && x.WindowInfo.Title == windowItem.WindowInfo.Title && x.WindowInfo.FileName == windowItem.WindowInfo.FileName && x.WindowInfo.ModName == windowItem.WindowInfo.ModName);
            if (existingWinItem != null)
                return true;
            else
                return false;
        }
    }

    #endregion

    #region Supporting Classes

    public class Minus15Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value) - 15;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }

    public class Minus25Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value) - 25;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }

    public class Minus204Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value) - 204;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }

    #endregion
}
