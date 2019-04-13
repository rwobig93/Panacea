using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Panacea.Classes;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for StartProcEditor.xaml
    /// </summary>
    public partial class StartProcEditor : Window
    {
        public StartProcEditor(StartProcessItem startItem = null)
        {
            InitializeComponent();
            _startItem = startItem;
            Startup();
        }

        private bool _startingUp = true;
        private bool _friendlyNameManual = false;
        private StartEditorAction _action = StartEditorAction.Create;
        private StartProcessItem _startItem = null;

        private void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            try
            {
                Toolbox.LogException(ex, lineNum, caller, path);
                uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
                uDebugLogAdd(string.Format("{0} at line {1} with type {2}", caller, lineNum, ex.GetType().Name), DebugType.EXCEPTION);
            }
        }

        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            try
            {
                Toolbox.uAddDebugLog($"STRTEDIT: {_log}", _type, caller);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Startup()
        {
            try
            {
                if (_startItem == null)
                    ChangeUIForCreation();
                else if (_startItem != null)
                    ChangeUIForEditing(_startItem);
                FinishStartup();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void FinishStartup()
        {
            try
            {
                _startingUp = false;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ChangeUIForEditing(StartProcessItem startItem)
        {
            try
            {
                _action = StartEditorAction.Edit;
                lblTitle.Text = "Start Process Editor";
                BtnFinish.Content = "Save";
                TxtProcPath.Text = startItem.Path;
                TxtProcArgs.Text = startItem.Args;
                TxtFriendlyName.Text = startItem.Name;
                ComboWindowMove.Text = startItem.MoveAfterStart ? "Yes" : "No";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SetFieldsToDefault()
        {
            try
            {
                TxtProcPath.Text = string.Empty;
                TxtProcArgs.Text = string.Empty;
                TxtFriendlyName.Text = string.Empty;
                ComboWindowMove.Text = "Yes";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ChangeUIForCreation()
        {
            try
            {
                _action = StartEditorAction.Create;
                lblTitle.Text = "Start Process Creator";
                BtnFinish.Content = "Create";
                SetFieldsToDefault();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TxtProcPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_startingUp)
                {
                    var pathArray = TxtProcPath.Text.Split('\\');
                    string executable = pathArray.Last();
                    if (!string.IsNullOrWhiteSpace(TxtProcPath.Text) && !_friendlyNameManual)
                    {
                        TxtFriendlyName.Text = executable;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TxtFriendlyName_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (!_startingUp)
                    _friendlyNameManual = true;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_action == StartEditorAction.Create)
                    CreateNewStartProc();
                else if (_action == StartEditorAction.Edit)
                    UpdateStartProc();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateStartProc()
        {
            try
            {
                StartProcessItem foundItem = Toolbox.settings.ActiveStartList.Find(x => x == _startItem);
                if (foundItem != null)
                {
                    foundItem.Name = TxtFriendlyName.Text;
                    foundItem.Path = TxtProcPath.Text;
                    foundItem.Args = TxtProcArgs.Text;
                    foundItem.MoveAfterStart = ComboWindowMove.Text == "Yes" ? true : false;
                    Director.Main.ShowNotification($"Updated Start Process for: {foundItem.Name}");
                    this.Close();
                }
                else
                {
                    Director.Main.ShowNotification("Was unable to find Process, not updated");
                    UpdateMessage("Error: Start Item Not Found, Not Updated");
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void UpdateMessage(string v, bool error = false)
        {
            try
            {
                if (error)
                    LabelMessage.Foreground = new SolidColorBrush(Toolbox.ColorFromHex("#FF991F1F"));
                else
                    LabelMessage.Foreground = new SolidColorBrush(Toolbox.ColorFromHex("#FF8D8D8D"));
                LabelMessage.Text = "v";
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CreateNewStartProc()
        {
            StartProcessItem newItem = new StartProcessItem()
            {
                Path = TxtProcPath.Text,
                Args = TxtProcArgs.Text,
                Name = TxtFriendlyName.Text,
                MoveAfterStart = ComboWindowMove.Text == "Yes" ? true : false
            };
            Toolbox.settings.AddStartProcess(newItem);
        }
    }
}
