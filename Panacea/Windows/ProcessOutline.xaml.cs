using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for ProcessOutline.xaml
    /// </summary>
    public partial class ProcessOutline : Window
    {
        public ProcessOutline(WindowItem windowItem)
        {
            InitializeComponent();
            UpdateLocation(windowItem);
        }
        public ProcessOutline(IntPtr handle)
        {
            InitializeComponent();
            UpdateLocation(handle);
        }

        public void UpdateLocation(WindowItem windowItem)
        {
            try
            {
                this.Left = windowItem.WindowInfo.XValue;
                this.Top = windowItem.WindowInfo.YValue;
                this.Height = windowItem.WindowInfo.Height;
                this.Width = windowItem.WindowInfo.Width;
                Toolbox.uAddDebugLog($"Updated Process Outline Location: L{this.Left} T{this.Top} H{this.Height} W{this.Width}");
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }

        public void UpdateLocation(IntPtr handle)
        {
            try
            {
                var rect = WindowInfo.GetHandleDimensions(handle);
                this.Left = rect.Left;
                this.Top = rect.Top;
                this.Width = rect.Right - rect.Left;
                this.Height = rect.Bottom - rect.Top;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }
    }
}
