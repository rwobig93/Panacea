using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Panacea.Classes
{
    public class Settings: INotifyPropertyChanged
    {
        private WindowDimensions _windowLocation { get; set; } = new WindowDimensions { Left = 0, Top = 0, Height = 251.309, Width = 454.455 };
        private SolidColorBrush _pingSuccessFill { get; set; } = NetworkVariables.defaultSuccessChartFill;
        private SolidColorBrush _pingSuccessStroke { get; set; } = NetworkVariables.defaultSuccessChartStroke;
        private SolidColorBrush _pingFailFill { get; set; } = NetworkVariables.defaultFailChartFill;
        private SolidColorBrush _pingFailStroke { get; set; } = NetworkVariables.defaultFailChartStroke;
        private DTFormat _dateTimeFormat { get; set; } = DTFormat.Sec;
        private Int32 _pingChartLength { get; set; } = NetworkVariables.defaultPingChartLength;
        private EnterAction _toolboxEnterAction { get; set; } = EnterAction.DNSLookup;
        private bool _basicPing { get; set; }
        public WindowDimensions WindowLocation
        {
            get { return _windowLocation; }
            set
            {
                _windowLocation = value;
                OnPropertyChanged("WindowLocation");
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
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public enum SettingsUpdate
    {
        PingCount,
        PingDTFormat,
        TextBoxAction
    }
}
