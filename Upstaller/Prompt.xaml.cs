﻿using System;
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
            this.Topmost = true;
            switch (type)
            {
                case PromptType.YesNo:
                    this.btnYes.Visibility = Visibility.Visible;
                    this.btnNo.Visibility = Visibility.Visible;
                    this.btnOK.Visibility = Visibility.Hidden;
                    break;
                case PromptType.OK:
                    this.btnOK.Visibility = Visibility.Visible;
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    break;
                case PromptType.Custom1:
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnOK.Visibility = Visibility.Hidden;
                    break;
                case PromptType.Custom2:
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnOK.Visibility = Visibility.Hidden;
                    break;
                case PromptType.Custom3:
                    this.btnYes.Visibility = Visibility.Hidden;
                    this.btnNo.Visibility = Visibility.Hidden;
                    this.btnOK.Visibility = Visibility.Hidden;
                    break;
            }
        }

        public enum PromptType
        {
            YesNo,
            OK,
            Custom1,
            Custom2,
            Custom3
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

        public PromptResponse Response { get; set; } = PromptResponse.Cancel;

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Response = PromptResponse.Yes;
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
                prompt.txtMessage.Text = message;
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

        public static PromptResponse OK(string message)
        {
            var response = PromptResponse.Cancel;
            try
            {
                Prompt prompt = new Prompt(PromptType.OK);
                prompt.txtMessage.Text = message;
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
                for (int i = longerLineCounter; i > 0; i--)
                {
                    if (additionalHeight < 13 * 27)
                        additionalHeight = additionalHeight + 27;
                }
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
    }
}
