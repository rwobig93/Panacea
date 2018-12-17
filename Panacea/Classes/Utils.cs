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

namespace Panacea.Classes
{
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
    public class ChangeLogItem
    {
        public string Version { get; set; }
        public string Body { get; set; }
        public Visibility BugFixes { get; set; }
        public Visibility NewFeatures { get; set; }
        public Visibility BetaRelease { get; set; }
    }
    public class CurrentDisplay
    {
        public List<Screen> Screens { get; set; } = new List<Screen>();
        public int LeftMostBound {  get { return Screens.OrderBy(x => x.Bounds.X).First().Bounds.X; } }
        public int RightMostBound { get { return Screens.OrderBy(x => x.Bounds.X).Last().Bounds.X + Screens.OrderBy(x => x.Bounds.X).Last().Bounds.Width; } }
        public int TopMostBound { get { return Screens.OrderBy(x => x.Bounds.Y).First().Bounds.Y; } }
        public int BottomMostBound { get { return Screens.OrderBy(x => x.Bounds.Y).Last().Bounds.Y + Screens.OrderBy(x => x.Bounds.Y).Last().Bounds.Height; } }
        public int LeftMostWorkArea { get { return Screens.OrderBy(x => x.WorkingArea.X).First().WorkingArea.X; } }
        public int RightMostWorkArea { get { return Screens.OrderBy(x => x.WorkingArea.X).Last().WorkingArea.X + Screens.OrderBy(x => x.WorkingArea.X).Last().WorkingArea.Width; } }
        public int TopMostWorkArea { get { return Screens.OrderBy(x => x.WorkingArea.Y).First().WorkingArea.Y; } }
        public int BottomMostWorkArea { get { return Screens.OrderBy(x => x.WorkingArea.Height).Last().WorkingArea.Y + Screens.OrderBy(x => x.WorkingArea.Y).Last().WorkingArea.Height; } }
        public System.Drawing.Rectangle TotalBounds { get { return new System.Drawing.Rectangle() { X = LeftMostBound, Width = RightMostBound - LeftMostBound, Y = TopMostBound, Height = BottomMostBound - TopMostBound, Location = new System.Drawing.Point(LeftMostBound, TopMostBound), Size = new System.Drawing.Size(RightMostBound - LeftMostBound, BottomMostBound - TopMostBound) }; } }
        public System.Drawing.Rectangle TotalWorkArea { get { return new System.Drawing.Rectangle() { X = LeftMostWorkArea, Width = RightMostWorkArea - LeftMostWorkArea, Y = TopMostWorkArea, Height = BottomMostWorkArea - TopMostWorkArea, Location = new System.Drawing.Point(LeftMostWorkArea, TopMostWorkArea), Size = new System.Drawing.Size(RightMostWorkArea - LeftMostWorkArea, BottomMostWorkArea - TopMostWorkArea) }; } }
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

    #endregion

    #region Events

    public class Events
    {
        public delegate void UpdateWinHandleInfo(WinInfoArgs args);
        public static event UpdateWinHandleInfo UpdateWinHandle;
        public static void UpdateWindowInfo(Process process)
        {
            WinInfoArgs args = new WinInfoArgs(WindowInfo.GetWindowInfoFromProc(process));
            UpdateWinHandle(args);
        }
        public delegate void AddDebugLogStatus(DebugUpdateArgs args);
        public static event AddDebugLogStatus UpdateDebugStatus;
        public static void AddDebugStatus(string logUpdate, DebugType debugType = DebugType.INFO)
        {
            DebugUpdateArgs args = new DebugUpdateArgs(logUpdate, debugType);
            UpdateDebugStatus(args);
        }
    }
    public class WinInfoArgs : EventArgs
    {
        private WindowInfo winInfo;
        public WinInfoArgs(WindowInfo windowInfo)
        {
            this.winInfo = windowInfo;
        }
        public WindowInfo WindowInfo { get { return winInfo; } }
    }
    public class DebugUpdateArgs : EventArgs
    {
        private string _logUpdate;
        private DebugType _debugType;
        public DebugUpdateArgs(string logUpdate, DebugType debugType = DebugType.INFO)
        {
            this._logUpdate = logUpdate;
            this._debugType = debugType;
        }
        public string LogUpdate { get { return _logUpdate; } }
        public DebugType DebugType { get { return _debugType; } }
    }

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
