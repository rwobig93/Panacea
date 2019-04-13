using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private List<WindowItem> _currentWindowList { get; set; } = new List<WindowItem>();
        private List<StartProcessItem> _currentStartList { get; set; } = new List<StartProcessItem>();
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
        private List<StartProcessItem> _startProfile1 { get; set; } = new List<StartProcessItem>();
        private List<StartProcessItem> _startProfile2 { get; set; } = new List<StartProcessItem>();
        private List<StartProcessItem> _startProfile3 { get; set; } = new List<StartProcessItem>();
        private List<StartProcessItem> _startProfile4 { get; set; } = new List<StartProcessItem>();
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
        private StartProfile _currentStartProfile { get; set; } = StartProfile.Start1;
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
            get { return _currentWindowList; }
            set
            {
                _currentWindowList = value;
                OnPropertyChanged("ActiveWindowList");
            }
        }
        public List<StartProcessItem> ActiveStartList
        {
            get { return _currentStartList; }
            set
            {
                _currentStartList = value;
                OnPropertyChanged("ActiveStartList");
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
                _windowProfile1 = value;
                OnPropertyChanged("WindowProfile1");
            }
        }
        public List<WindowItem> WindowProfile2
        {
            get { return _windowProfile2; }
            set
            {
                _windowProfile2 = value;
                OnPropertyChanged("WindowProfile2");
            }
        }
        public List<WindowItem> WindowProfile3
        {
            get { return _windowProfile3; }
            set
            {
                _windowProfile3 = value;
                OnPropertyChanged("WindowProfile3");
            }
        }
        public List<WindowItem> WindowProfile4
        {
            get { return _windowProfile4; }
            set
            {
                _windowProfile4 = value;
                OnPropertyChanged("WindowProfile4");
            }
        }
        public List<StartProcessItem> StartProfile1
        {
            get { return _startProfile1; }
            set
            {
                _startProfile1 = value;
                OnPropertyChanged("StartProfile1");
            }
        }
        public List<StartProcessItem> StartProfile2
        {
            get { return _startProfile2; }
            set
            {
                _startProfile2 = value;
                OnPropertyChanged("StartProfile2");
            }
        }
        public List<StartProcessItem> StartProfile3
        {
            get { return _startProfile3; }
            set
            {
                _startProfile3 = value;
                OnPropertyChanged("StartProfile3");
            }
        }
        public List<StartProcessItem> StartProfile4
        {
            get { return _startProfile4; }
            set
            {
                _startProfile4 = value;
                OnPropertyChanged("StartProfile4");
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
        public WindowProfile CurrentWindowProfile
        {
            get { return _currentWinProfile; }
            set
            {
                _currentWinProfile = value;
                OnPropertyChanged("CurrentWindowProfile");
            }
        }
        public StartProfile CurrentStartProfile
        {
            get { return _currentStartProfile; }
            set
            {
                _currentStartProfile = value;
                OnPropertyChanged("CurrentStartProfile");
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

        #region Methods
        public void AddWindow(WindowItem windowItem)
        {
            _currentWindowList.Add(windowItem);
            OnPropertyChanged("ActiveWindowList");
            Events.TriggerWindowInfoChange();
        }
        public void RemoveWindow(WindowItem windowItem)
        {
            _currentWindowList.Remove(windowItem);
            OnPropertyChanged("ActiveWindowList");
            Events.TriggerWindowInfoChange();
        }
        public void AddStartProcess(StartProcessItem startItem)
        {
            _currentStartList.Add(startItem);
            OnPropertyChanged("ActiveStartList");
            Events.TriggerStartInfoChange();
        }
        public void RemoveStartProcess(StartProcessItem startItem)
        {
            _currentStartList.Remove(startItem);
            OnPropertyChanged("ActiveStartList");
            Events.TriggerStartInfoChange(true);
        }
        public void UpdateWindowLocation(WindowItem windowItem, Process proc)
        {
            var existingItem = _currentWindowList.Find(x => x.WindowInfo.PrivateID == windowItem.WindowInfo.PrivateID);
            WindowInfo windowInfo = WindowInfo.GetWindowInfoFromProc(proc);
            if (proc == null)
                uDebugLogAdd("Update process was null");
            if (existingItem != null && proc != null)
            {
                existingItem.WindowInfo = windowInfo;
                OnPropertyChanged("WindowList");
            }
            else
                uDebugLogAdd("Couldn't find existing window item");
            Events.TriggerWindowInfoChange(true);
        }
        public void ChangeWindowProfile(WindowProfile windowProfile)
        {
            try
            {
                uDebugLogAdd($"Saving current window list for profile: {CurrentWindowProfile.ToString()}");
                switch (CurrentWindowProfile)
                {
                    case WindowProfile.Profile1:
                        WindowProfile1 = ActiveWindowList;
                        break;
                    case WindowProfile.Profile2:
                        WindowProfile2 = ActiveWindowList;
                        break;
                    case WindowProfile.Profile3:
                        WindowProfile3 = ActiveWindowList;
                        break;
                    case WindowProfile.Profile4:
                        WindowProfile4 = ActiveWindowList;
                        break;
                }
                uDebugLogAdd($"Moving from WindowProfile {CurrentWindowProfile.ToString()} to {windowProfile.ToString()}");
                CurrentWindowProfile = windowProfile;
                switch (windowProfile)
                {
                    case WindowProfile.Profile1:
                        ActiveWindowList = WindowProfile1;
                        break;
                    case WindowProfile.Profile2:
                        ActiveWindowList = WindowProfile2;
                        break;
                    case WindowProfile.Profile3:
                        ActiveWindowList = WindowProfile3;
                        break;
                    case WindowProfile.Profile4:
                        ActiveWindowList = WindowProfile4;
                        break;
                }
                uDebugLogAdd("Finished changing window profile");
                Events.TriggerWindowInfoChange();
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }
        public void ChangeStartProfile(StartProfile startProfile)
        {
            try
            {
                uDebugLogAdd($"Saving current start list for profile: {CurrentStartProfile.ToString()}");
                switch (CurrentStartProfile)
                {
                    case StartProfile.Start1:
                        StartProfile1 = ActiveStartList;
                        break;
                    case StartProfile.Start2:
                        StartProfile2 = ActiveStartList;
                        break;
                    case StartProfile.Start3:
                        StartProfile3 = ActiveStartList;
                        break;
                    case StartProfile.Start4:
                        StartProfile4 = ActiveStartList;
                        break;
                }
                uDebugLogAdd($"Moving from StartProfile {CurrentStartProfile.ToString()} to {startProfile.ToString()}");
                CurrentStartProfile = startProfile;
                switch (startProfile)
                {
                    case StartProfile.Start1:
                        ActiveStartList = StartProfile1;
                        break;
                    case StartProfile.Start2:
                        ActiveStartList = StartProfile2;
                        break;
                    case StartProfile.Start3:
                        ActiveStartList = StartProfile3;
                        break;
                    case StartProfile.Start4:
                        ActiveStartList = StartProfile4;
                        break;
                }
                uDebugLogAdd("Finished changing start profile");
                Events.TriggerStartInfoChange();
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
            }
        }
        private void uDebugLogAdd(string _log, DebugType _type = DebugType.INFO, [CallerMemberName] string caller = "")
        {
            try
            {
                Toolbox.uAddDebugLog($"CLSSETTINGS: {_log}", _type, caller);
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
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
