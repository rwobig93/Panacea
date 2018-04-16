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
using static Panacea.Classes.WinInfoArgs;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for HandleDisplay.xaml
    /// </summary>
    public partial class HandleDisplay : Window
    {
        public HandleDisplay()
        {
            InitializeComponent();

            Events.UpdateWinHandle += HandleDisplay_UpdateWinHandleInfo;
        }

        private void HandleDisplay_UpdateWinHandleInfo(Classes.WinInfoArgs args)
        {
            try
            {
                TxtProcName.Text = $"Process Name: {args.WindowInfo.Name}";
                TxtProcTitle.Text = $"Process Title: {args.WindowInfo.Title}";
                TxtModName.Text = $"Module Name: {args.WindowInfo.ModName}";
                TxtFilePath.Text = $"File Path: {args.WindowInfo.FileName}";
                TxtProcLocation.Text = $"Location:{Environment.NewLine}   X: {args.WindowInfo.XValue}{Environment.NewLine}   Y: {args.WindowInfo.YValue}{Environment.NewLine}   Width: {args.WindowInfo.Width}{Environment.NewLine}   Height: {args.WindowInfo.Height}";
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
