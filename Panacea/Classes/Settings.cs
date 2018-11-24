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
    public class Settings : INotifyPropertyChanged
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
        private bool _betaUpdate { get; set; } = false;
        private WindowProfile _currentWinProfile { get; set; } = WindowProfile.Profile1;
        private Version _currentVersion { get; set; } = new Version("0.0.0.0");
        private Version _productionVersion { get; set; } = null;
        private Version _upCurrentVersion { get; set; } = null;
        private Version _upProductionVersion { get; set; } = null;
        private bool _updateAvailable { get; set; } = false;
        private string _productionURI { get; set; } = null;
        private string _upProductionURI { get; set; } = null;

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
        public List<WindowItem> ActiveWindowList
        {
            get { return _windowList; }
            set
            {
                _windowList = value;
                OnPropertyChanged("ActiveWindowList");
            }
        }
        public List<WindowItem> WindowProfile1
        {
            get { return _windowProfile1; }
            set
            {
                _windowList = value;
                OnPropertyChanged("WindowProfile1");
            }
        }
        public List<WindowItem> WindowProfile2
        {
            get { return _windowProfile2; }
            set
            {
                _windowList = value;
                OnPropertyChanged("WindowProfile2");
            }
        }
        public List<WindowItem> WindowProfile3
        {
            get { return _windowProfile3; }
            set
            {
                _windowList = value;
                OnPropertyChanged("WindowProfile3");
            }
        }
        public List<WindowItem> WindowProfile4
        {
            get { return _windowProfile4; }
            set
            {
                _windowList = value;
                OnPropertyChanged("WindowProfile4");
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
        public bool BetaUpdate
        {
            get { return _betaUpdate; }
            set
            {
                _betaUpdate = value;
                OnPropertyChanged("BetaUpdate");
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
                    _windowProfile1 = ActiveWindowList;
                    break;
                case WindowProfile.Profile2:
                    _windowProfile2 = ActiveWindowList;
                    break;
                case WindowProfile.Profile3:
                    _windowProfile3 = ActiveWindowList;
                    break;
                case WindowProfile.Profile4:
                    _windowProfile4 = ActiveWindowList;
                    break;
            }
            Toolbox.uAddDebugLog($"Moving from WindowProfile {CurrentWindowProfile.ToString()} to {windowProfile}");
            CurrentWindowProfile = windowProfile;
            switch (windowProfile)
            {
                case WindowProfile.Profile1:
                    ActiveWindowList = _windowProfile1;
                    break;
                case WindowProfile.Profile2:
                    ActiveWindowList = _windowProfile2;
                    break;
                case WindowProfile.Profile3:
                    ActiveWindowList = _windowProfile3;
                    break;
                case WindowProfile.Profile4:
                    ActiveWindowList = _windowProfile4;
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
        public Version UpCurrentVersion
        {
            get { return _upCurrentVersion; }
            set { _upCurrentVersion = value; }
        }
        public Version UpProductionVersion
        {
            get { return _upProductionVersion; }
            set { _upProductionVersion = value; }
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
        public string UpProductionURI
        {
            get { return _upProductionURI; }
            set { _upProductionURI = value; }
        }

        #endregion
    }

    public enum SettingsUpdate
    {
        PingCount,
        PingDTFormat,
        TextBoxAction,
        BasicPing,
        BetaCheck
    }
}
