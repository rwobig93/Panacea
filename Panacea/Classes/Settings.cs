using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Panacea.Classes
{
    public class Settings: INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Private Properties

        private WindowDimensions _windowLocation { get; set; } = new WindowDimensions { Left = 0, Top = 0, Height = 251.309, Width = 454.455 };
        private List<WindowItem> _windowList { get; set; } = new List<WindowItem>();
        private List<WindowItem> _windowProfile1 { get; set; } = new List<WindowItem>();
        private List<WindowItem> _windowProfile2 { get; set; } = new List<WindowItem>();
        private List<WindowItem> _windowProfile3 { get; set; } = new List<WindowItem>();
        private List<WindowItem> _windowProfile4 { get; set; } = new List<WindowItem>();
        private SolidColorBrush _pingSuccessFill { get; set; } = NetworkVariables.defaultSuccessChartFill;
        private SolidColorBrush _pingSuccessStroke { get; set; } = NetworkVariables.defaultSuccessChartStroke;
        private SolidColorBrush _pingFailFill { get; set; } = NetworkVariables.defaultFailChartFill;
        private SolidColorBrush _pingFailStroke { get; set; } = NetworkVariables.defaultFailChartStroke;
        private DTFormat _dateTimeFormat { get; set; } = DTFormat.Sec;
        private Int32 _pingChartLength { get; set; } = NetworkVariables.defaultPingChartLength;
        private EnterAction _toolboxEnterAction { get; set; } = EnterAction.DNSLookup;
        private bool _basicPing { get; set; }
        private WindowProfile _currentWinProfile { get; set; } = WindowProfile.Profile1;
        private Version _currentVersion { get; set; } = null;
        private Version _productionVersion { get; set; } = null;
        private bool _updateAvailable { get; set; } = false;
        private string _productionURI { get; set; } = null;

        #endregion

        #region Public Properties

        public WindowDimensions WindowLocation
        {
            get { return _windowLocation; }
            set
            {
                _windowLocation = value;
                OnPropertyChanged("WindowLocation");
            }
        }
        public List<WindowItem> WindowList
        {
            get { return _windowList; }
            set
            {
                _windowList = value;
                OnPropertyChanged("WindowList");
            }
        }
        public SolidColorBrush PingSuccessFill
        {
            get { return _pingSuccessFill; }
            set
            {
                _pingSuccessFill = value;
                OnPropertyChanged("PingSuccessFill");
            }
        }
        public SolidColorBrush PingSuccessStroke
        {
            get { return _pingSuccessStroke; }
            set
            {
                _pingSuccessStroke = value;
                OnPropertyChanged("PingSuccessStroke");
            }
        }
        public SolidColorBrush PingFailFill
        {
            get { return _pingFailFill; }
            set
            {
                _pingFailFill = value;
                OnPropertyChanged("PingFailFill");
            }
        }
        public SolidColorBrush PingFailStroke
        {
            get { return _pingFailStroke; }
            set
            {
                _pingFailStroke = value;
                OnPropertyChanged("PingFailStroke");
            }
        }
        public DTFormat DateTimeFormat
        {
            get { return _dateTimeFormat; }
            set
            {
                _dateTimeFormat = value;
                OnPropertyChanged("DateTimeFormat");
            }
        }
        public Int32 PingChartLength
        {
            get { return _pingChartLength; }
            set
            {
                _pingChartLength = value;
                OnPropertyChanged("PingChartLength");
            }
        }
        public EnterAction ToolboxEnterAction
        {
            get { return _toolboxEnterAction; }
            set
            {
                _toolboxEnterAction = value;
                OnPropertyChanged("ToolboxEnterAction");
            }
        }
        public bool BasicPing
        {
            get { return _basicPing; }
            set
            {
                _basicPing = value;
                OnPropertyChanged("BasicPing");
            }
        }
        public WindowProfile CurrentWindowProfile
        {
            get { return _currentWinProfile; }
            set
            {
                _currentWinProfile = value;
                OnPropertyChanged("CurrentWindowProfile");
            }
        }
        public void AddWindow(WindowItem windowItem)
        {
            _windowList.Add(windowItem);
            OnPropertyChanged("WindowList");
        }
        public void RemoveWindow(WindowItem windowItem)
        {
            _windowList.Remove(windowItem);
            OnPropertyChanged("WindowList");
        }
        public void UpdateWindowLocation(WindowItem windowItem, Process proc)
        {
            var existingItem = _windowList.Find(x => x.WindowInfo.PrivateID == windowItem.WindowInfo.PrivateID);
            WindowInfo windowInfo = WindowInfo.GetWindowInfoFromProc(proc);
            if (proc == null)
                Toolbox.uAddDebugLog("Update process was null");
            if (existingItem != null && proc != null)
            {
                existingItem.WindowInfo = windowInfo;
                OnPropertyChanged("WindowList");
            }
            else
                Toolbox.uAddDebugLog("Couldn't find existing window item");
        }
        public void ChangeWindowProfile(WindowProfile windowProfile)
        {
            Toolbox.uAddDebugLog($"Saving current window list for profile: {CurrentWindowProfile.ToString()}");
            switch (CurrentWindowProfile)
            {
                case WindowProfile.Profile1:
                    _windowProfile1 = WindowList;
                    break;
                case WindowProfile.Profile2:
                    _windowProfile2 = WindowList;
                    break;
                case WindowProfile.Profile3:
                    _windowProfile3 = WindowList;
                    break;
                case WindowProfile.Profile4:
                    _windowProfile4 = WindowList;
                    break;
            }
            Toolbox.uAddDebugLog($"Moving from WindowProfile {CurrentWindowProfile.ToString()} to {windowProfile}");
            CurrentWindowProfile = windowProfile;
            switch (windowProfile)
            {
                case WindowProfile.Profile1:
                    WindowList = _windowProfile1;
                    break;
                case WindowProfile.Profile2:
                    WindowList = _windowProfile2;
                    break;
                case WindowProfile.Profile3:
                    WindowList = _windowProfile3;
                    break;
                case WindowProfile.Profile4:
                    WindowList = _windowProfile4;
                    break;
            }
            Toolbox.uAddDebugLog("Finished changing window profile");
        }
        public Version CurrentVersion
        {
            get { return _currentVersion; }
            set { _currentVersion = value; }
        }
        public Version ProductionVersion
        {
            get { return _productionVersion; }
            set { _productionVersion = value; }
        }
        public bool UpdateAvailable
        {
            get { return _updateAvailable; }
            set { _updateAvailable = value; }
        }
        public string ProductionURI
        {
            get { return _productionURI; }
            set { _productionURI = value; }
        }

        #endregion
    }

    public enum SettingsUpdate
    {
        PingCount,
        PingDTFormat,
        TextBoxAction,
        BasicPing
    }
}
