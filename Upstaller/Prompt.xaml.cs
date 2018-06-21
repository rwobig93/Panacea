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

namespace Upstaller
{
    /// <summary>
    /// Interaction logic for Prompt.xaml
    /// </summary>
    public sealed partial class Prompt : Window
    {
        private Prompt(PromptType type)
        {
            InitializeComponent();

            switch (type)
            {
                case PromptType.YesNo:
                    // Display diff elements based on prompt type
                    break;
            }
        }

        public enum PromptType
        {
            YesNo
        }

        public enum PromptResponse
        {
            Yes,
            No,
            Cancel
        }

        public PromptResponse Response { get; set; } = PromptResponse.Cancel;

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            this.Response = PromptResponse.Yes;
            this.DialogResult = true;
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Response = PromptResponse.No;
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }
        
        public static PromptResponse YesNo(string message)
        {
            var response = PromptResponse.Cancel;
            try
            {
                Prompt prompt = new Prompt(PromptType.YesNo);
                prompt.ShowDialog();
                prompt.txtMessage.Text = message;
                response = prompt.Response;
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
            return response;
        }
    }
}
