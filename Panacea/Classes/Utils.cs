using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Panacea.Classes
{
    #region General

    public class WindowDimensions
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
    }
    public class WindowInfo
    {
        public string Name { get; set; }
        public string ModName { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public int PrivateID { get; set; } = Toolbox.GenerateRandomNumber();
        public int IndexNum { get; set; }
        public int XValue { get; set; }
        public int YValue { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public static WindowInfo GetWindowInfoFromProc(Process proc)
        {
            WinAPIWrapper.RECT dimensions = new WinAPIWrapper.RECT();
            WinAPIWrapper.GetWindowRect(proc.Handle, ref dimensions);
            return new WindowInfo()
            {
                Name = proc.ProcessName,
                ModName = proc.MainModule.ModuleName,
                Title = proc.MainWindowTitle,
                FileName = proc.MainModule.FileName,
                IndexNum = 0,
                XValue = dimensions.Left,
                YValue = dimensions.Top,
                Width = dimensions.Right - dimensions.Left,
                Height = dimensions.Bottom - dimensions.Top
            };
        }
    }
    public class WindowItem
    {
        public WindowInfo WindowInfo { get; set; }
        public string WindowName { get; set; } = "";
        public string Enabled { get; set; } = "On";
        public string Checked { get; set; } = "";
        public Brush EnableColor { get; set; } = Defaults.WinEnableButtonColorOn;
        public static WindowItem Create(Process process)
        {
            return new WindowItem()
            {
                WindowInfo = WindowInfo.GetWindowInfoFromProc(process),
                WindowName = process.ProcessName
            };
        }
        public static bool DoesDuplicateExist(WindowItem windowItem)
        {
            var existingWinItem = MainWindow.savedWindows.ToList().Find(x => x.WindowInfo.Name == windowItem.WindowInfo.Name && x.WindowInfo.Title == windowItem.WindowInfo.Title && x.WindowInfo.FileName == windowItem.WindowInfo.FileName && x.WindowInfo.ModName == windowItem.WindowInfo.ModName);
            if (existingWinItem != null)
                return true;
            else
                return false;
        }
    }

    public enum Direction
    {
        Up,
        Down
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
