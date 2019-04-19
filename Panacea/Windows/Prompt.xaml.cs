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
    /// Interaction logic for Prompt.xaml
    /// </summary>
    public sealed partial class Prompt : Window
    {
        #region Private Constructor
        private Prompt(PromptType type)
        {
            InitializeComponent();
            Startup(type);
        } 
        #endregion

        #region Enums
        public enum PromptType
        {
            YesNo,
            OK,
            Custom1,
            Custom2,
            Custom3,
            PingType,
            WindowPreference
        }

        public enum PromptResponse
        {
            Yes,
            No,
            Cancel,
            Custom1,
            Custom2,
            Custom3,
            OK
        }
        #endregion

        #region Public Variables
        public PromptResponse Response { get; set; } = PromptResponse.Cancel; 
        #endregion

        #region Public Methods / Constructors
        public static PromptResponse YesNo(string message, TextAlignment alignment = TextAlignment.Center)
        {
            var response = PromptResponse.Cancel;
            try
            {
                Prompt prompt = new Prompt(PromptType.YesNo);
                Toolbox.uAddDebugLog($"Prompt created with message: {message}");
                prompt.txtMessage.Text = message;
                prompt.txtMessage.TextAlignment = alignment;
                SizeWindowBasedOnMessage(prompt);
                prompt.ShowDialog();
                response = prompt.Response;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
            return response;
        }

        public static PromptResponse OK(string message, TextAlignment alignment = TextAlignment.Center)
        {
            var response = PromptResponse.Cancel;
            try
            {
                Prompt prompt = new Prompt(PromptType.OK);
                Toolbox.uAddDebugLog($"Prompt created with message: {message}");
                prompt.txtMessage.Text = message;
                prompt.txtMessage.TextAlignment = alignment;
                SizeWindowBasedOnMessage(prompt);
                prompt.ShowDialog();
                response = prompt.Response;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
            return response;
        }

        public static PromptResponse WindowPreference()
        {
            var response = PromptResponse.Cancel;
            try
            {
                Prompt prompt = new Prompt(PromptType.WindowPreference);
                prompt.btnCustom1.Content = "Utility Bar";
                prompt.btnCustom2.Content = "Desktop Window";
                prompt.Height = 711;
                prompt.Width = 1363;
                prompt.ShowDialog();
                response = prompt.Response;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
            return response;
        }

        public static PromptResponse PingType()
        {
            var response = PromptResponse.Cancel;
            try
            {
                Prompt prompt = new Prompt(PromptType.PingType);
                string pingQuestion = string.Format(
                    "                                                                             I see this is the first time picking your ping preference.{0}" +
                    "                                                                                  Do you prefer a visual ping or a basic ping style?{0}" +
                    "                                                 Visual:                                                                                                                  Basic: {0}" +
                    "                                         More CPU Usage                                                                                                    More Detail{0}" +
                    "                                         Visual Style Chart                                                                                             Less space per ping{0}" +
                    "                                      Takes up more space                                                                                           Minimal CPU usage", Environment.NewLine);
                prompt.txtMessage.Text = pingQuestion;
                prompt.txtMessage.TextAlignment = TextAlignment.Left;
                prompt.btnCustom1.Content = "Visual Ping";
                prompt.btnCustom2.Content = "Basic Ping";
                prompt.Height = 724;
                prompt.Width = 1327;
                prompt.ShowDialog();
                response = prompt.Response;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
            return response;
        }
        #endregion

        #region Private Methods
        private void Startup(PromptType type)
        {
            this.Topmost = true;
            SetControlVisibility(type);
            ResizeControls(type);
            Toolbox.uAddDebugLog($"Prompt initiated with type: {type.ToString()}");
        }

        private void ResizeControls(PromptType type)
        {
            switch (type)
            {
                case PromptType.YesNo:
                    txtMessage.Margin = new Thickness(10, 10, 10, 61);
                    break;
                case PromptType.OK:
                    txtMessage.Margin = new Thickness(10, 10, 10, 61);
                    break;
                case PromptType.Custom1:
                    txtMessage.Margin = new Thickness(10, 10, 10, 61);
                    break;
                case PromptType.Custom2:
                    txtMessage.Margin = new Thickness(10, 10, 10, 61);
                    break;
                case PromptType.Custom3:
                    txtMessage.Margin = new Thickness(10, 10, 10, 61);
                    break;
                case PromptType.PingType:
                    txtMessage.Margin = new Thickness(10, 10, 10, 61);
                    break;
                case PromptType.WindowPreference:
                    ImgWinStyDeskWin.Margin = new Thickness(10, 152, 0, 0);
                    LabelChoiceWinStyTitle.Margin = new Thickness(10, 10, 0, 0);
                    LabelChoiceWinStyDeskDesc.Margin = new Thickness(803, 229, 0, 0);
                    LabelChoiceWinStyUtilDesc.Margin = new Thickness(430, 508, 0, 0);
                    LabelChoiceWinStyUtilTitle.Margin = new Thickness(235, 543, 0, 0);
                    break;
            }
        }

        private void SetControlVisibility(PromptType type)
        {
            switch (type)
            {
                case PromptType.YesNo:
                    this.btnYes.Visibility = Visibility.Visible;
                    this.btnNo.Visibility = Visibility.Visible;
                    this.txtMessage.Visibility = Visibility.Visible;
                    this.btnOK.Visibility = Visibility.Hidden;
                    this.btnCustom1.Visibility = Visibility.Hidden;
                    this.btnCustom2.Visibility = Visibility.Hidden;
                    this.imgBasicPing.Visibility = Visibility.Hidden;
                    this.imgVisualPing.Visibility = Visibility.Hidden;
                    this.ImgWinStyDeskWin.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyDeskDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyTitle.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilTitle.Visibility = Visibility.Hidden;
                    break;
                case PromptType.OK:
                    this.btnOK.Visibility = Visibility.Visible;
                    this.txtMessage.Visibility = Visibility.Visible;
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnCustom1.Visibility = Visibility.Hidden;
                    this.btnCustom2.Visibility = Visibility.Hidden;
                    this.imgBasicPing.Visibility = Visibility.Hidden;
                    this.imgVisualPing.Visibility = Visibility.Hidden;
                    this.ImgWinStyDeskWin.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyDeskDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyTitle.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilTitle.Visibility = Visibility.Hidden;
                    break;
                case PromptType.Custom1:
                    this.txtMessage.Visibility = Visibility.Visible;
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnOK.Visibility = Visibility.Hidden;
                    this.btnCustom1.Visibility = Visibility.Hidden;
                    this.btnCustom2.Visibility = Visibility.Hidden;
                    this.imgBasicPing.Visibility = Visibility.Hidden;
                    this.imgVisualPing.Visibility = Visibility.Hidden;
                    this.ImgWinStyDeskWin.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyDeskDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyTitle.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilTitle.Visibility = Visibility.Hidden;
                    break;
                case PromptType.Custom2:
                    this.txtMessage.Visibility = Visibility.Visible;
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnOK.Visibility = Visibility.Hidden;
                    this.btnCustom1.Visibility = Visibility.Hidden;
                    this.btnCustom2.Visibility = Visibility.Hidden;
                    this.imgBasicPing.Visibility = Visibility.Hidden;
                    this.imgVisualPing.Visibility = Visibility.Hidden;
                    this.ImgWinStyDeskWin.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyDeskDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyTitle.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilTitle.Visibility = Visibility.Hidden;
                    break;
                case PromptType.Custom3:
                    this.txtMessage.Visibility = Visibility.Visible;
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnOK.Visibility = Visibility.Hidden;
                    this.btnCustom1.Visibility = Visibility.Hidden;
                    this.btnCustom2.Visibility = Visibility.Hidden;
                    this.imgBasicPing.Visibility = Visibility.Hidden;
                    this.imgVisualPing.Visibility = Visibility.Hidden;
                    this.ImgWinStyDeskWin.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyDeskDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyTitle.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilTitle.Visibility = Visibility.Hidden;
                    break;
                case PromptType.PingType:
                    this.btnCustom1.Visibility = Visibility.Visible;
                    this.btnCustom2.Visibility = Visibility.Visible;
                    this.imgBasicPing.Visibility = Visibility.Visible;
                    this.imgVisualPing.Visibility = Visibility.Visible;
                    this.txtMessage.Visibility = Visibility.Hidden;
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnOK.Visibility = Visibility.Hidden;
                    this.ImgWinStyDeskWin.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyDeskDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyTitle.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilDesc.Visibility = Visibility.Hidden;
                    this.LabelChoiceWinStyUtilTitle.Visibility = Visibility.Hidden;
                    break;
                case PromptType.WindowPreference:
                    this.btnCustom1.Visibility = Visibility.Visible;
                    this.btnCustom2.Visibility = Visibility.Visible;
                    this.ImgWinStyDeskWin.Visibility = Visibility.Visible;
                    this.LabelChoiceWinStyDeskDesc.Visibility = Visibility.Visible;
                    this.LabelChoiceWinStyTitle.Visibility = Visibility.Visible;
                    this.LabelChoiceWinStyUtilDesc.Visibility = Visibility.Visible;
                    this.LabelChoiceWinStyUtilTitle.Visibility = Visibility.Visible;
                    this.txtMessage.Visibility = Visibility.Hidden;
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnOK.Visibility = Visibility.Hidden;
                    this.imgBasicPing.Visibility = Visibility.Hidden;
                    this.imgVisualPing.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Response = PromptResponse.Yes;
                Toolbox.uAddDebugLog($"User chose option: {this.Response.ToString()}");
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Response = PromptResponse.No;
                Toolbox.uAddDebugLog($"User chose option: {this.Response.ToString()}");
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Response = PromptResponse.OK;
                Toolbox.uAddDebugLog($"User chose option: {this.Response.ToString()}");
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }

        private void btnCustom1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Response = PromptResponse.Custom1;
                Toolbox.uAddDebugLog($"User chose option: {this.Response.ToString()}");
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }

        private void btnCustom2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Response = PromptResponse.Custom2;
                Toolbox.uAddDebugLog($"User chose option: {this.Response.ToString()}");
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }

        private static void SizeWindowBasedOnMessage(Prompt prompt)
        {
            try
            {
                var splitText = prompt.txtMessage.Text.Split('\n');
                var lines = splitText.Length;
                // Adjust Width based on character count
                var additionalWidth = 0;
                int longerLineCounter = 0;
                int longestLine = 33;
                foreach (var split in splitText)
                {
                    if (split.Length > longestLine)
                        longestLine = split.Length;
                    if (split.Length >= 52)
                        longerLineCounter++;
                }
                if (longestLine > 33 && longestLine < 52)
                    additionalWidth = (longestLine - 33) * 18;
                else if (longestLine >= 52)
                    additionalWidth = 18 * 18;
                prompt.Width = prompt.Width + additionalWidth;

                // Adjust Height based on lines
                int additionalHeight = 0;
                if (lines > 4 && lines <= 17)
                    additionalHeight = (lines - 4) * 27;
                else if (lines > 17)
                    additionalHeight = 13 * 27;
                prompt.Height = prompt.Height + additionalHeight;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
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
                Toolbox.LogException(ex);
            }
        } 
        #endregion
    }
}
