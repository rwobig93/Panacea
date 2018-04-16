using LiveCharts;
using LiveCharts.Configurations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Panacea.Classes
{
    #region Supporting

    public static class NetworkVariables
    {
        public static SolidColorBrush defaultSuccessChartStroke = new SolidColorBrush(Color.FromArgb(100, 6, 12, 133));
        public static SolidColorBrush defaultFailChartStroke = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        public static SolidColorBrush defaultSuccessChartFill = new SolidColorBrush(Color.FromArgb(100, 4, 164, 48));
        public static SolidColorBrush defaultFailChartFill = new SolidColorBrush(Color.FromArgb(100, 139, 2, 2));
        public static Int32 defaultPingChartLength = 10;
    }

    public enum DTFormat
    {
        Sec,
        Min,
        Hours
    }

    #endregion

    #region Ping

    public class PingModel
    {
        public DateTime DateTime { get; set; }
        public long TripTime { get; set; }
        public bool Response { get; set; }
    }

    public class PingEntry : INotifyPropertyChanged
    {
        #region Variables

        private string _address { get; set; }
        private bool pinging { get; set; }
        private double _axisMax;
        private double _axisMin;
        private string _chartTitle { get; set; }
        private SolidColorBrush _chartStroke { get; set; }
        private SolidColorBrush _chartFill { get; set; }
        private Int32 _historyLength { get; set; } = 12;
        private Int32 _gridHeight { get; set; } = 104;
        private Int32 _gridWidth { get; set; }
        private Int32 _chartLength { get; set; } = 10;
        public ChartValues<PingModel> ChartValues { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }
        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }
        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
            }
        }
        public SolidColorBrush ChartStroke
        {
            get { return _chartStroke; }
            set
            {
                _chartStroke = value;
                OnPropertyChanged("ChartStroke");
            }
        }
        public SolidColorBrush ChartFill
        {
            get { return _chartFill; }
            set
            {
                _chartFill = value;
                OnPropertyChanged("ChartFill");
            }
        }
        public string ChartTitle
        {
            get { return _chartTitle; }
            set
            {
                _chartTitle = value;
                OnPropertyChanged("ChartTitle");
            }
        }
        public string Address
        {
            get { return _address; }
        }
        public Int32 GridHeight
        {
            get { return _gridHeight; }
            set
            {
                _gridHeight = value;
                OnPropertyChanged("GridHeight");
            }
        }
        public Int32 GridWidth
        {
            get { return _gridWidth; }
            set
            {
                _gridWidth = value;
                OnPropertyChanged("GridWidth");
            }
        }
        public Int32 ChartLength
        {
            get { return _chartLength; }
            set
            {
                _chartLength = value;
                _historyLength = value + 2;
                OnPropertyChanged("ChartLength");
            }
        }

        #endregion

        public PingEntry(string address)
        {
            try
            {
                _address = address;

                var mapper = Mappers.Xy<PingModel>()
                .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.TripTime);           //use the value property as Y

                //lets save the mapper globally.
                Charting.For<PingModel>(mapper);

                //the values property will store our values array
                ChartValues = new ChartValues<PingModel>();

                //lets set how to display the X Labels
                switch (Toolbox.settings.DateTimeFormat)
                {
                    case DTFormat.Sec:
                        DateTimeFormatter = value => new DateTime((long)value).ToString("ss");
                        break;
                    case DTFormat.Min:
                        DateTimeFormatter = value => new DateTime((long)value).ToString("mm:ss");
                        break;
                    case DTFormat.Hours:
                        DateTimeFormatter = value => new DateTime((long)value).ToString("hh:mm:ss");
                        break;
                    default:
                        DateTimeFormatter = value => new DateTime((long)value).ToString("ss");
                        break;
                }

                //AxisStep forces the distance between each separator in the X axis
                AxisStep = TimeSpan.FromSeconds(1).Ticks;
                //AxisUnit forces lets the axis know that we are plotting seconds
                //this is not always necessary, but it can prevent wrong labeling
                AxisUnit = TimeSpan.TicksPerSecond;

                SetAxisLimits(DateTime.Now);

                PingAddress(_address);
                LookupAddress(_address);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #region Methods

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(_chartLength).Ticks; // and 10 seconds behind
        }

        private void LogException(Exception ex, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            try
            {
                Toolbox.LogException(ex, lineNum, caller, path);
            }
            catch (Exception)
            {
                Random rand = new Random();
                Toolbox.LogException(ex, lineNum, caller, path, rand.Next(816456489).ToString());
            }
        }

        private void PingAddress(string address)
        {
            try
            {
                pinging = true;
                ChartTitle = $"{address} | HostName Not Found";
                BackgroundWorker worker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
                worker.DoWork += (sender2, e2) =>
                {
                    Ping ping = new Ping();
                    while (true)
                    {
                        while (pinging)
                        {
                            try
                            {
                                var pingReply = ping.Send(address);
                                AddPingResponse(pingReply);
                            }
                            catch (PingException)
                            {
                                worker.ReportProgress(1);
                                worker.ReportProgress(0);
                            }
                            catch (Exception ex)
                            {
                                LogException(ex);
                            }
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                };
                worker.ProgressChanged += (sender3, e3) =>
                {
                    if (e3.ProgressPercentage == 1)
                    {
                        AddFailPingResponse();
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void AddPingResponse(PingReply pingReply)
        {
            if (ChartStroke != Toolbox.settings.PingSuccessStroke || ChartFill != Toolbox.settings.PingSuccessFill)
            {
                ChartStroke = Toolbox.settings.PingSuccessStroke;
                ChartFill = Toolbox.settings.PingSuccessFill;
            }
            var now = DateTime.Now;
            ChartValues.Add(new PingModel
            {
                DateTime = now,
                TripTime = pingReply.RoundtripTime
            });

            SetAxisLimits(now);

            if (ChartValues.Count > 22) ChartValues.RemoveAt(0);
        }

        private void AddFailPingResponse()
        {
            if (ChartStroke != Toolbox.settings.PingFailFill && ChartFill != Toolbox.settings.PingFailFill)
            {
                ChartStroke = Toolbox.settings.PingFailStroke;
                ChartFill = Toolbox.settings.PingFailFill;
            }
            var now = DateTime.Now;
            ChartValues.Add(new PingModel
            {
                DateTime = now,
                TripTime = -1
            });

            SetAxisLimits(now);

            if (ChartValues.Count > 22) ChartValues.RemoveAt(0);
        }

        private void LookupAddress(string address)
        {
            try
            {
                string resolvedAddress = string.Empty;
                string resolvedHostname = string.Empty;
                BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
                worker.DoWork += (sender, e) =>
                {
                    try
                    {
                        var dnsEntry = Dns.GetHostEntry(address);
                        if (dnsEntry != null)
                        {
                            if (!string.IsNullOrWhiteSpace(dnsEntry.HostName))
                            {
                                resolvedAddress = dnsEntry.AddressList[0].ToString();
                                resolvedHostname = dnsEntry.HostName;
                                worker.ReportProgress(1);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                };
                worker.ProgressChanged += (sender2, e2) =>
                {
                    if (e2.ProgressPercentage == 1)
                    {
                        ChartTitle = $"{resolvedAddress} | {resolvedHostname}";
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void TogglePing(bool? setPinging = null)
        {
            if (setPinging == null)
            {
                if (pinging)
                    pinging = false;
                else
                    pinging = true;
            }
            else
                pinging = (bool)setPinging;
        }

        #endregion

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #endregion

    #region NSLookup

    public class HostEntry : INotifyPropertyChanged
    {
        private string _ipAddress { get; set; }
        private string _hostName { get; set; }
        public string IPAddress
        {
            get { return _ipAddress; }
            set
            {
                _ipAddress = value;
                OnPropertyChanged("IPAddress");
            }
        }
        public string HostName
        {
            get { return _hostName; }
            set
            {
                _hostName = value;
                OnPropertyChanged("HostName");
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

    #endregion
}
