using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using static Panacea.Windows.UtilityBar;

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

        private WindowDimensions _windowLocation { get; set; } = new WindowDimensions { Left = 0, Top = 0, Height = 300, Width = 625 };
        private List<WindowItem> _windowList { get; set; } = new List<WindowItem>();
        private string _windowProfileName1 { get; set; } = "Profile 1";
        private string _windowProfileName2 { get; set; } = "Profile 2";
        private string _windowProfileName3 { get; set; } = "Profile 3";
        private string _windowProfileName4 { get; set; } = "Profile 4";
        private string _startupProfileName1 { get; set; } = "Start 1";
        private string _startupProfileName2 { get; set; } = "Start 2";
        private string _startupProfileName3 { get; set; } = "Start 3";
        private string _startupProfileName4 { get; set; } = "Start 4";
        private List<WindowItem> _windowProfile1 { get; set; } = new List<WindowItem>();
        private List<WindowItem> _windowProfile2 { get; set; } = new List<WindowItem>();
        private List<WindowItem> _windowProfile3 { get; set; } = new List<WindowItem>();
        private List<WindowItem> _windowProfile4 { get; set; } = new List<WindowItem>();
        private SolidColorBrush _pingSuccessFill { get; set; } = new SolidColorBrush(Color.FromArgb(100, 0, 195, 0));
        private SolidColorBrush _pingSuccessStroke { get; set; } = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
        private SolidColorBrush _pingFailFill { get; set; } = new SolidColorBrush(Color.FromArgb(100, 195, 0, 0));
        private SolidColorBrush _pingFailStroke { get; set; } = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        private SolidColorBrush _pingPauseStroke { get; set; } = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));
        private SolidColorBrush _pingPauseFill { get; set; } = new SolidColorBrush(Color.FromArgb(100, 195, 195, 0));
        private int _pingChartLength { get; set; } = 10;
        private Version _currentVersion { get; set; }
        private Version _productionVersion { get; set; } = null;
        private Version _upCurrentVersion { get; set; } = null;
        private Version _upProductionVersion { get; set; } = null;
        private bool _updateAvailable { get; set; } = false;
        private bool _basicPing { get; set; } = false;
        private bool _betaUpdate { get; set; } = false;
        private bool _showChangelog { get; set; } = true;
        private bool _pingTypeChosen { get; set; } = false;
        private bool _windowsStartup { get; set; } = false;
        private bool _showUtilBarOnStartup { get; set; } = false;
        private bool _startMinimized { get; set; } = false;
        private string _productionURI { get; set; } = null;
        private string _upProductionURI { get; set; } = null;
        private string _latestChangelog { get; set; } = "I'm a default Changelog! You shouldn't ever see me! :D";
        private WindowProfile _currentWinProfile { get; set; } = WindowProfile.Profile1;
        private EnterAction _toolboxEnterAction { get; set; } = EnterAction.DNSLookup;
        private EnterAction _utilBarEnterAction { get; set; } = EnterAction.DNSLookup;
        private DTFormat _dateTimeFormat { get; set; } = DTFormat.Sec;
        private Emotes _currentEmote { get; set; } = Emotes.Shrug;
        private List<PopoutPreferences> _popoutPreferencesList { get; set; } = new List<PopoutPreferences>();
        private DisplayProfileLibrary _displayProfileLibrary { get; set; } = new DisplayProfileLibrary();

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
        public string WindowProfileName1
        {
            get { return _windowProfileName1; }
            set
            {
                _windowProfileName1 = value;
                OnPropertyChanged("WindowProfileName1");
            }
        }
        public string WindowProfileName2
        {
            get { return _windowProfileName2; }
            set
            {
                _windowProfileName2 = value;
                OnPropertyChanged("WindowProfileName2");
            }
        }
        public string WindowProfileName3
        {
            get { return _windowProfileName3; }
            set
            {
                _windowProfileName3 = value;
                OnPropertyChanged("WindowProfileName3");
            }
        }
        public string WindowProfileName4
        {
            get { return _windowProfileName4; }
            set
            {
                _windowProfileName4 = value;
                OnPropertyChanged("WindowProfileName4");
            }
        }
        public string StartProfileName1
        {
            get { return _startupProfileName1; }
            set
            {
                _startupProfileName1 = value;
                OnPropertyChanged("StartProfileName1");
            }
        }
        public string StartProfileName2
        {
            get { return _startupProfileName2; }
            set
            {
                _startupProfileName2 = value;
                OnPropertyChanged("StartProfileName1");
            }
        }
        public string StartProfileName3
        {
            get { return _startupProfileName3; }
            set
            {
                _startupProfileName3 = value;
                OnPropertyChanged("StartProfileName1");
            }
        }
        public string StartProfileName4
        {
            get { return _startupProfileName4; }
            set
            {
                _startupProfileName4 = value;
                OnPropertyChanged("StartProfileName1");
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
        public SolidColorBrush PingPauseStroke
        {
            get { return _pingPauseStroke; }
            set
            {
                _pingPauseStroke = value;
                OnPropertyChanged("PingPauseStroke");
            }
        }
        public SolidColorBrush PingPauseFill
        {
            get { return _pingPauseFill; }
            set
            {
                _pingPauseFill = value;
                OnPropertyChanged("PingPauseFill");
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
        public bool ShowChangelog
        {
            get { return _showChangelog; }
            set
            {
                _showChangelog = value;
                OnPropertyChanged("ShowChangelog");
            }
        }
        public bool PingTypeChosen
        {
            get { return _pingTypeChosen; }
            set { _pingTypeChosen = value; OnPropertyChanged("PingTypeChosen"); }
        }
        public bool WindowsStartup
        {
            get { return _windowsStartup; }
            set
            {
                _windowsStartup = value;
                OnPropertyChanged("WindowsStartup");
            }
        }
        public bool ShowUtilBarOnStartup
        {
            get { return _showUtilBarOnStartup; }
            set
            {
                _showUtilBarOnStartup = value;
                OnPropertyChanged("ShowUtilBarOnStartup");
            }
        }
        public bool StartMinimized
        {
            get { return _startMinimized; }
            set
            {
                _startMinimized = value;
                OnPropertyChanged("StartMinimized");
            }
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
        public string LatestChangelog
        {
            get { return _latestChangelog; }
            set
            {
                _latestChangelog = value;
                OnPropertyChanged("LatestChangelog");
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
        public WindowProfile CurrentWindowProfile
        {
            get { return _currentWinProfile; }
            set
            {
                _currentWinProfile = value;
                OnPropertyChanged("CurrentWindowProfile");
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
        public EnterAction UtilBarEnterAction
        {
            get { return _utilBarEnterAction; }
            set
            {
                _utilBarEnterAction = value;
                OnPropertyChanged("UtilBarEnterAction");
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
        public Emotes CurrentEmote
        {
            get { return _currentEmote; }
            set
            {
                _currentEmote = value;
                OnPropertyChanged("CurrentEmote");
            }
        }
        public List<PopoutPreferences> PopoutPreferencesList
        {
            get { return _popoutPreferencesList; }
            set
            {
                _popoutPreferencesList = value;
                OnPropertyChanged("PopoutPreferencesList");
            }
        }
        public DisplayProfileLibrary DisplayProfileLibrary
        {
            get { return _displayProfileLibrary; }
            set
            {
                _displayProfileLibrary = value;
                OnPropertyChanged("DisplayProfileLibrary");
            }
        }

        #endregion
    }

    public enum SettingsUpdate
    {
        PingCount,
        PingDTFormat,
        TextBoxAction,
        BasicPing,
        BetaCheck,
        ProfileName,
        WinStartup,
        UtilBar,
        StartMin
    }
}
