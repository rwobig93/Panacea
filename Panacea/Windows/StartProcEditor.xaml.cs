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

        #region Globals
        private bool _startingUp = true;
        private bool _friendlyNameManual = false;
        private StartEditorAction _action = StartEditorAction.Create;
        private StartProcessItem _startItem = null;
        #endregion

        #region Event Handlers
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
            CloseEditor();
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
                CloseEditor();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RectTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                    WinStartProcEditor.DragMove();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LblTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                    WinStartProcEditor.DragMove();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
        #endregion

        #region Methods
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
                uDebugLogAdd("Initialized new StartProcEditor");
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
                uDebugLogAdd("Finished Initialization");
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
                uDebugLogAdd($"Changing UI for Editing: {startItem.Name}");
                _action = StartEditorAction.Edit;
                lblTitle.Text = "Start Process Editor";
                WinStartProcEditor.Title = "Start Process Editor";
                BtnFinish.Content = "Save";
                TxtProcPath.Text = startItem.Path;
                TxtProcArgs.Text = startItem.Args;
                TxtFriendlyName.Text = startItem.Name;
                ComboWindowMove.Text = startItem.MoveAfterStart ? "Yes" : "No";
                uDebugLogAdd($"Finished UI update for editing");
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
                uDebugLogAdd("Setting fields to default");
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
                uDebugLogAdd("Changing UI for creation");
                _action = StartEditorAction.Create;
                lblTitle.Text = "Start Process Creator";
                WinStartProcEditor.Title = "Start Process Creator";
                BtnFinish.Content = "Create";
                SetFieldsToDefault();
                uDebugLogAdd("Finished changing UI for creation");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void CloseEditor()
        {
            try
            {
                uDebugLogAdd("Closing editor");
                this.Close();
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
                uDebugLogAdd("Updating existing start process item");
                StartProcessItem foundItem = Toolbox.settings.ActiveStartList.Find(x => x == _startItem);
                if (foundItem != null)
                {
                    uDebugLogAdd($"Found existing Start Proc Item: [Name] {foundItem.Name} [Args] {foundItem.Args} [Move] {foundItem.MoveAfterStart} [Path] {foundItem.Path}");
                    foundItem.Name = TxtFriendlyName.Text;
                    foundItem.Path = TxtProcPath.Text;
                    foundItem.Args = TxtProcArgs.Text;
                    foundItem.MoveAfterStart = ComboWindowMove.Text == "Yes" ? true : false;
                    uDebugLogAdd($"Updated existing Start Proc Item: [Name] {foundItem.Name} [Args] {foundItem.Args} [Move] {foundItem.MoveAfterStart} [Path] {foundItem.Path}");
                    Director.Main.ShowNotification($"Updated Start Process for: {foundItem.Name}");
                    CloseEditor();
                }
                else
                {
                    uDebugLogAdd("Couldn't find existing Start Proc Item, canceling...");
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
                uDebugLogAdd($"Displaying Message: [Error] {error} [Message] {v}");
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
            try
            {
                uDebugLogAdd("Creating new Start Proc Item");
                StartProcessItem newItem = new StartProcessItem()
                {
                    Path = TxtProcPath.Text,
                    Args = TxtProcArgs.Text,
                    Name = TxtFriendlyName.Text,
                    MoveAfterStart = ComboWindowMove.Text == "Yes" ? true : false
                };
                uDebugLogAdd($"Adding newly created StartProcItem to list: [Name] {newItem.Name} [Args] {newItem.Args} [Move] {newItem.MoveAfterStart} [Path] {newItem.Path}");
                Toolbox.settings.AddStartProcess(newItem);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
        #endregion
    }
}
