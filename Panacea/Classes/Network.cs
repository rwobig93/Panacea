using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Panacea.Classes
{
    #region Supporting

    public enum DTFormat
    {
        Sec,
        Min,
        Hours
    }

    public enum EnterAction
    {
        Ping,
        DNSLookup,
        Trace,
        NA
    }

    public enum PingStat
    {
        Active,
        Paused,
        Canceled,
        Unknown
    }

    #endregion

    #region Ping

    public class PingModel
    {
        public DateTime DateTime { get; set; }
        public long TripTime { get; set; }
        public bool Response { get; set; }
    }

    public class PingDetail
    {
        public DateTime TimeStamp { get; set; } = DateTime.Now.ToLocalTime();
        public long TripTime { get; set; }
        public IPStatus IPStatus { get; set; }
        public IPAddress Address { get; set; }
    }

    public class PingEntry : INotifyPropertyChanged
    {
        #region Variables

        private string _address { get; set; }
        private string _hostName { get; set; }
        private PingStat pinging { get; set; } = PingStat.Active;
        private bool _disposed { get; set; } = false;
        private double _axisMax;
        private double _axisMin;
        private string _chartTitle { get { return $"{_address} | {_hostName}"; } set { _chartTitle = value; } }
        private List<string> _pingExList = new List<string>();
        private SolidColorBrush _chartStroke { get; set; } = Toolbox.settings.PingSuccessStroke;
        private SolidColorBrush _chartFill { get; set; } = Toolbox.settings.PingSuccessFill;
        private Int32 _historyLength { get; set; } = Toolbox.settings.PingChartLength + 2;
        private Int32 _gridHeight { get; set; } = 104;
        private Int32 _gridWidth { get; set; }
        private Int32 _chartLength { get; set; } = Toolbox.settings.PingChartLength;
        public ChartValues<PingModel> ChartValues { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public PingStat Pinging
        {
            get { return pinging; }
            set { pinging = value; OnPropertyChanged("Pinging"); }
        }
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
            set { _address = value; OnPropertyChanged("Address"); OnPropertyChanged("ChartTitle"); }
        }
        public string HostName
        {
            get { return _hostName; }
            set { _hostName = value; OnPropertyChanged("HostName"); OnPropertyChanged("ChartTitle"); }
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

        public PingEntry(string address, bool active = true)
        {
            try
            {
                Address = address;

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

                PingAddress(_address, active);
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

        private void PingAddress(string address, bool active = true)
        {
            try
            {
                if (active)
                    TogglePing(PingStat.Active);
                else
                    TogglePing(PingStat.Paused);
                HostName = $"HostName Not Found";
                BackgroundWorker worker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
                worker.DoWork += (sender2, e2) =>
                {
                    Ping ping = new Ping();
                    while (!_disposed)
                    {
                        while (Pinging == PingStat.Active)
                        {
                            try
                            {
                                var pingReply = ping.Send(address);
                                AddPingSuccess(pingReply);
                            }
                            catch (PingException pe)
                            {
                                if (!HostName.Contains(pe.InnerException.Message))
                                {
                                    HostName = $"({pe.InnerException.Message}) {HostName}";
                                    if (_pingExList.Find(x => x == pe.InnerException.Message) == null)
                                        _pingExList.Add(pe.InnerException.Message);
                                }
                                AddFailPingResponse();
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

        private void AddPingSuccess(PingReply pingReply)
        {
            var valid = VerifyPingStatus(pingReply);
            if ((ChartStroke != Toolbox.settings.PingSuccessStroke || ChartFill != Toolbox.settings.PingSuccessFill) && Pinging == PingStat.Active && valid)
            {
                ChartStroke = Toolbox.settings.PingSuccessStroke;
                ChartFill = Toolbox.settings.PingSuccessFill;
            }
            foreach (var ex in _pingExList)
                if (HostName.Contains(ex))
                    HostName.Replace($"({ex}) ", "");
            if ((HostName.Contains("(Timing Out) ") || HostName.Contains("(Packet Error) ") || HostName.Contains("(TTL Expired) ") || HostName.Contains("(Unreachable) ") || HostName.Contains("(Unknown Error) ")) && valid)
                HostName = HostName.Replace("(Timing Out) ", "").Replace("(Packet Error) ", "").Replace("(TTL Expired) ", "").Replace("(Unreachable) ", "").Replace("(Unknown Error) ", "");
            else if (!valid)
            {
                AddFailPingResponse();
                return;
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
            try
            {
                if ((ChartStroke != Toolbox.settings.PingFailStroke || ChartFill != Toolbox.settings.PingFailFill) && Pinging != PingStat.Paused)
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
            catch (Exception ex)
            {
                LogException(ex);
            }
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
                    catch (SocketException se)
                    {
                        Toolbox.uAddDebugLog($"{address}: {se.Message}");
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
                        //ChartTitle = $"{resolvedAddress} | {resolvedHostname}";
                        Address = resolvedAddress;
                        HostName = resolvedHostname;
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private bool VerifyPingStatus(PingReply reply)
        {
            bool valid = false;
            var status = reply.Status;
            if (status == IPStatus.Success)
                valid = true;
            else if (status == IPStatus.TimedOut)
            {
                if (!HostName.StartsWith("(Timing Out) "))
                    HostName = $"(Timing Out) {HostName}";
            }
            else if ((status == IPStatus.BadDestination ||
                 status == IPStatus.BadHeader ||
                 status == IPStatus.BadOption ||
                 status == IPStatus.BadRoute ||
                 status == IPStatus.DestinationScopeMismatch ||
                 status == IPStatus.HardwareError ||
                 status == IPStatus.IcmpError ||
                 status == IPStatus.NoResources ||
                 status == IPStatus.PacketTooBig ||
                 status == IPStatus.ParameterProblem ||
                 status == IPStatus.SourceQuench ||
                 status == IPStatus.UnrecognizedNextHeader)
                 )
            {
                if (!HostName.StartsWith("(Packet Error) "))
                    HostName = $"(Packet Error) {HostName}";
            }
            else if (status == IPStatus.TimeExceeded ||
                status == IPStatus.TtlExpired ||
                status == IPStatus.TtlReassemblyTimeExceeded
                )
            {
                if (!HostName.StartsWith("(TTL Expired) "))
                    HostName = $"(TTL Expired) {HostName}";
            }
            else if (status == IPStatus.DestinationHostUnreachable ||
                status == IPStatus.DestinationNetworkUnreachable ||
                status == IPStatus.DestinationPortUnreachable ||
                status == IPStatus.DestinationProtocolUnreachable ||
                status == IPStatus.DestinationUnreachable)
            {
                if (!HostName.StartsWith("(Unreachable) "))
                    HostName = $"(Unreachable) {HostName}";
            }
            else
            {
                if (!HostName.StartsWith("(Unknown Error) "))
                    HostName = $"(Unknown Error) {HostName}";
            }
            return valid;
        }

        public void TogglePing(PingStat stat = PingStat.Unknown)
        {
            try
            {
                if (stat == PingStat.Unknown)
                {
                    if (Pinging == PingStat.Active)
                    {
                        Pinging = PingStat.Paused;
                        ChartStroke = Toolbox.settings.PingPauseStroke;
                        ChartFill = Toolbox.settings.PingPauseFill;
                    }
                    else if (Pinging == PingStat.Paused)
                    {
                        Pinging = PingStat.Active;
                    }
                    else
                        Toolbox.uAddDebugLog($"Pinging for {Address} | {HostName} is {Pinging.ToString()} | Skipping toggle request");
                }
                else
                {
                    Pinging = stat;
                    if (Pinging == PingStat.Paused)
                    {
                        ChartStroke = Toolbox.settings.PingPauseStroke;
                        ChartFill = Toolbox.settings.PingPauseFill;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void Destroy()
        {
            try
            {
                Pinging = PingStat.Canceled;
                _disposed = true;
                ChartValues.Clear();
                ChartValues = null;
            }
            catch (Exception ex)
            {
                Toolbox.uAddDebugLog($"Error occured while destroying pingentry: {ex.Message}", DebugType.FAILURE);
            }
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

    public class BasicPingEntry : INotifyPropertyChanged
    {
        #region Variables

        private SolidColorBrush _colorPingSuccess = new SolidColorBrush(Color.FromArgb(100, 0, 195, 0));
        private SolidColorBrush _colorPingFailure = new SolidColorBrush(Color.FromArgb(100, 195, 0, 0));
        private SolidColorBrush _colorPingPaused = new SolidColorBrush(Color.FromArgb(100, 195, 195, 0));
        private List<PingDetail> _pingHistory = new List<PingDetail>();
        private string _displayName { get { return $"{_address} | {_hostName}"; } set { _displayName = value; } }
        private string _address { get; set; }
        private string _hostName { get; set; }
        private string _toggleButton { get; set; } = @"❚❚";
        private bool _disposed = false;
        private int _highPing { get; set; }
        private int _lowPing { get; set; }
        private int _avgPing { get; set; }
        private int _currentPing { get; set; }
        private PingStat _pinging { get; set; } = PingStat.Active;
        private SolidColorBrush _pingResultColor { get; set; } = new SolidColorBrush(Color.FromArgb(100, 0, 195, 0));
        private List<string> _pingExList = new List<string>();
        public string DisplayName { get { return _displayName; } }
        public string Address
        {
            get { return _address; }
            set { _address = value; OnPropertyChanged("Address"); OnPropertyChanged("DisplayName"); }
        }
        public string HostName
        {
            get { return _hostName; }
            set { _hostName = value; OnPropertyChanged("HostName"); OnPropertyChanged("DisplayName"); }
        }
        public string ToggleButton
        {
            get { return _toggleButton; }
            set { _toggleButton = value; OnPropertyChanged("ToggleButton"); }
        }
        public int HighPing
        {
            get { return _highPing; }
            set { _highPing = value; OnPropertyChanged("HighPing"); }
        }
        public int LowPing
        {
            get { return _lowPing; }
            set { _lowPing = value; OnPropertyChanged("LowPing"); }
        }
        public int AvgPing
        {
            get { return _avgPing; }
            set { _avgPing = value; OnPropertyChanged("AvgPing"); }
        }
        public int CurrentPing
        {
            get { return _currentPing; }
            set { _currentPing = value; OnPropertyChanged("CurrentPing"); }
        }
        public PingStat Pinging
        {
            get { return _pinging; }
            set { _pinging = value; OnPropertyChanged("Pinging"); }
        }
        public SolidColorBrush PingResultColor
        {
            get { return _pingResultColor; }
            set { _pingResultColor = value; OnPropertyChanged("PingResultColor"); }
        }
        public List<PingDetail> PingHistory
        {
            get { return _pingHistory; }
            set { _pingHistory = value; OnPropertyChanged("PingHistory"); }
        }

        #endregion

        public BasicPingEntry(string address, bool active = true)
        {
            try
            {
                Address = address;
                PingAddress(Address, true);
                LookupAddress(Address);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #region Methods

        private void PingAddress(string address, bool active = true)
        {
            try
            {
                if (active)
                    TogglePing(PingStat.Active);
                else
                    TogglePing(PingStat.Paused);
                Address = address;
                HostName = "Hostname not found";
                BackgroundWorker worker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
                worker.DoWork += (sender2, e2) =>
                {
                    Ping ping = new Ping();
                    while (!_disposed)
                    {
                        while (Pinging == PingStat.Active)
                        {
                            try
                            {
                                var pingReply = ping.Send(address);
                                AddPingSuccess(pingReply);
                            }
                            catch (PingException pe)
                            {
                                if (!HostName.Contains(pe.InnerException.Message))
                                {
                                    HostName = $"({pe.InnerException.Message}) {HostName}";
                                    if (_pingExList.Find(x => x == pe.InnerException.Message) == null)
                                        _pingExList.Add(pe.InnerException.Message);
                                }
                                if (Pinging != PingStat.Paused)
                                    PingResultColor = _colorPingFailure;
                                HighPing = -1;
                                LowPing = -1;
                                AvgPing = -1;
                                CurrentPing = -1;
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
                        AddPingFailure();
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void Destroy()
        {
            try
            {
                Pinging = PingStat.Canceled;
                _disposed = true;
                PingHistory.Clear();
                PingHistory = null;
            }
            catch (Exception ex)
            {
                Toolbox.uAddDebugLog($"Error occured while destroying basicpingentry: {ex.Message}", DebugType.FAILURE);
            }
        }

        private void AddPingFailure()
        {
            try
            {
                IPAddress ip;
                IPAddress.TryParse(Address, out ip);
                var pingDetail = new PingDetail()
                {
                    Address = ip,
                    IPStatus = IPStatus.DestinationUnreachable,
                    TimeStamp = DateTime.Now.ToLocalTime(),
                    TripTime = -1
                };
                PingHistory.Add(pingDetail);
                HighPing = Convert.ToInt32(PingHistory.OrderBy(x => x.TripTime).Last().TripTime);
                LowPing = Convert.ToInt32(PingHistory.OrderBy(x => x.TripTime).First().TripTime);
                AvgPing = CalculateRTTAvg(PingHistory);
                CurrentPing = Convert.ToInt32(pingDetail.TripTime);
                if (Pinging != PingStat.Paused)
                    PingResultColor = _colorPingFailure;
            }
            catch (Exception ex)
            {
                Toolbox.uAddDebugLog($"Unable to get ping failure: [{ex.GetType().ToString()}]{ex.Message}", DebugType.FAILURE);
            }
        }

        private void AddPingSuccess(PingReply pingReply)
        {
            try
            {
                var valid = VerifyPingStatus(pingReply);
                IPAddress ip;
                IPAddress.TryParse(Address, out ip);
                var pingDetail = new PingDetail()
                {
                    Address = pingReply.Address != null ? pingReply.Address : ip,
                    IPStatus = pingReply.Status,
                    TimeStamp = DateTime.Now.ToLocalTime(),
                    TripTime = valid ? pingReply.RoundtripTime : -1
                };
                if ((HostName.Contains("(Timing Out) ") || HostName.Contains("(Packet Error) ") || HostName.Contains("(TTL Expired) ") || HostName.Contains("(Unreachable) ") || HostName.Contains("(Unknown Error) ")) && valid)
                    HostName = HostName.Replace("(Timing Out) ", "").Replace("(Packet Error) ", "").Replace("(TTL Expired) ", "").Replace("(Unreachable) ", "").Replace("(Unknown Error) ", "");
                foreach (var ex in _pingExList)
                    if (HostName.Contains(ex))
                        HostName.Replace($"({ex}) ", "");
                PingHistory.Add(pingDetail);
                HighPing = PingHistory.FindAll(x => x.TripTime != -1).Count > 0 ? Convert.ToInt32(PingHistory.FindAll(x => x.TripTime != -1).OrderBy(x => x.TripTime).Last().TripTime) : -1;
                LowPing = PingHistory.FindAll(x => x.TripTime != -1).Count > 0 ? Convert.ToInt32(PingHistory.FindAll(x => x.TripTime != -1).OrderBy(x => x.TripTime).First().TripTime) : -1;
                AvgPing = PingHistory.FindAll(x => x.TripTime != -1).Count > 0 ? CalculateRTTAvg(PingHistory) : -1;
                CurrentPing = PingHistory.FindAll(x => x.TripTime != -1).Count > 0 ? Convert.ToInt32(pingDetail.TripTime) : -1;
                if (Pinging != PingStat.Paused)
                    PingResultColor = valid ? _colorPingSuccess : _colorPingFailure;
            }
            catch (Exception ex)
            {
                Toolbox.uAddDebugLog($"Unable to get ping success: [{ex.GetType().ToString()}]{ex.Message}", DebugType.FAILURE);
            }
        }

        private bool VerifyPingStatus(PingReply reply)
        {
            bool valid = false;
            var status = reply.Status;
            if (status == IPStatus.Success)
                valid = true;
            else if (status == IPStatus.TimedOut)
            {
                if (!HostName.StartsWith("(Timing Out) "))
                    HostName = $"(Timing Out) {HostName}";
            }
            else if ((status == IPStatus.BadDestination ||
                 status == IPStatus.BadHeader ||
                 status == IPStatus.BadOption ||
                 status == IPStatus.BadRoute ||
                 status == IPStatus.DestinationScopeMismatch ||
                 status == IPStatus.HardwareError ||
                 status == IPStatus.IcmpError ||
                 status == IPStatus.NoResources ||
                 status == IPStatus.PacketTooBig ||
                 status == IPStatus.ParameterProblem ||
                 status == IPStatus.SourceQuench ||
                 status == IPStatus.UnrecognizedNextHeader)
                 )
            {
                if (!HostName.StartsWith("(Packet Error) "))
                    HostName = $"(Packet Error) {HostName}";
            }
            else if (status == IPStatus.TimeExceeded ||
                status == IPStatus.TtlExpired ||
                status == IPStatus.TtlReassemblyTimeExceeded
                )
            {
                if (!HostName.StartsWith("(TTL Expired) "))
                    HostName = $"(TTL Expired) {HostName}";
            }
            else if (status == IPStatus.DestinationHostUnreachable ||
                status == IPStatus.DestinationNetworkUnreachable ||
                status == IPStatus.DestinationPortUnreachable ||
                status == IPStatus.DestinationProtocolUnreachable ||
                status == IPStatus.DestinationUnreachable)
            {
                if (!HostName.StartsWith("(Unreachable) "))
                    HostName = $"(Unreachable) {HostName}";
            }
            else
            {
                if (!HostName.StartsWith("(Unknown Error) "))
                    HostName = $"(Unknown Error) {HostName}";
            }
            return valid;
        }

        private int CalculateRTTAvg(List<PingDetail> pingHistory)
        {
            int sum = 0;
            int count = 0;
            foreach (var ping in pingHistory)
            {
                if (ping.TripTime != -1)
                {
                    count++;
                    sum = sum + Convert.ToInt32(ping.TripTime);
                }
            }
            return sum / count;
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

        public void TogglePing(PingStat stat = PingStat.Unknown)
        {
            try
            {
                if (stat == PingStat.Unknown)
                {
                    if (Pinging == PingStat.Active)
                    {
                        Pinging = PingStat.Paused;
                        ToggleButton = @"▶";
                        PingResultColor = _colorPingPaused;
                    }
                    else if (Pinging == PingStat.Paused)
                    {
                        Pinging = PingStat.Active;
                        ToggleButton = @"❚❚";
                        PingResultColor = _colorPingPaused;
                    }
                    else
                    {
                        Toolbox.uAddDebugLog($"Pinging for {Address} | {HostName} is {Pinging.ToString()} | Skipping toggle request");
                    }
                }
                else
                {
                    Pinging = stat;
                    if (Pinging == PingStat.Active)
                    {
                        ToggleButton = @"❚❚";
                    }
                    else if (Pinging == PingStat.Paused)
                    {
                        ToggleButton = @"▶";
                        PingResultColor = _colorPingPaused;
                    }
                    else
                    {
                        Toolbox.uAddDebugLog($"Pinging for {Address} | {HostName} is {Pinging.ToString()} | Skipping toggle request");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LookupAddress(string address)
        {
            try
            {
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
                                Address = dnsEntry.AddressList[0].ToString();
                                HostName = dnsEntry.HostName;
                            }
                        }
                    }
                    catch (SocketException se)
                    {
                        Toolbox.uAddDebugLog($"{address}: {se.Message}");
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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

    #region Trace

    public class TraceModel : INotifyPropertyChanged
    {
        private IPStatus _status { get; set; }
        private int _index { get; set; }
        private string _address { get; set; }
        private string _hostName { get; set; }
        private double _highRTT { get; set; }
        private double _lowRTT { get; set; }
        private double _openRTT { get; set; }
        private double _closeRTT { get; set; }
        public int Index
        {
            get { return _index; }
            set
            {
                _index = value;
                OnPropertyChanged("Index");
            }
        }
        public string Address
        {
            get { return _address; }
            set
            {
                _address = value;
                OnPropertyChanged("Address");
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
        public double HighRTT
        {
            get { return _highRTT; }
            set
            {
                _highRTT = value;
                OnPropertyChanged("HighRTT");
            }
        }
        public double LowRTT
        {
            get { return _lowRTT; }
            set
            {
                _lowRTT = value;
                OnPropertyChanged("LowRTT");
            }
        }
        public double OpenRTT
        {
            get { return _openRTT; }
            set
            {
                _openRTT = value;
                OnPropertyChanged("OpenRTT");
            }
        }
        public double CloseRTT
        {
            get { return _closeRTT; }
            set
            {
                _closeRTT = value;
                OnPropertyChanged("CloseRTT");
            }
        }
        public IPStatus IPStatus
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("IPStatus");
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

    public class TraceEntry : INotifyPropertyChanged
    {
        private string _destAddress = string.Empty;
        private bool exists = true;
        private bool tracing = true;
        private List<TraceModel> _traceList = new List<TraceModel>();
        private string[] _labels;
        private string _chartTitle { get; set; }
        private double _gridHeight { get; set; } = 150;
        private SeriesCollection _seriesCollection { get; set; }
        public string[] Labels
        {
            get { return _labels; }
            set
            {
                _labels = value;
                OnPropertyChanged("Labels");
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
        public SeriesCollection SeriesCollection
        {
            get { return _seriesCollection; }
            set
            {
                _seriesCollection = value;
                OnPropertyChanged("SeriesCollection");
            }
        }
        public double GridHeight
        {
            get { return _gridHeight; }
            set
            {
                _gridHeight = value;
                OnPropertyChanged("GridHeight");
            }
        }
        public List<TraceModel> TraceList
        {
            get { return _traceList; }
            set
            {
                _traceList = value;
                OnPropertyChanged("TraceList");
            }
        }
        public string DestinationAddress
        {
            get { return _destAddress; }
            set
            {
                _destAddress = value;
                OnPropertyChanged("DestinationAddress");
            }
        }

        public TraceEntry(List<TraceModel> traceList)
        {
            TraceList = traceList;
            //foreach (var trace in TraceList)
            //{
            //    ResolveTraceHost(trace.Address, trace.Index);
            //}
            var ohlcValues = new ChartValues<OhlcPoint>();
            var lineValues = new ChartValues<double>();
            var labelValues = new string[_traceList.Count];
            var index = 0;

            foreach (var address in _traceList)
            {
                ohlcValues.Add(new OhlcPoint(address.OpenRTT, address.HighRTT, address.LowRTT, address.CloseRTT));
                lineValues.Add(address.OpenRTT);
                labelValues[index] = address.Address;
                if (index == _traceList.Count - 1)
                    _destAddress = address.Address;
                index++;
            }

            SeriesCollection = new SeriesCollection
            {
                new OhlcSeries()
                {
                    Values = ohlcValues
                },
                new LineSeries
                {
                    Values = lineValues,
                    Fill = Brushes.Transparent
                }
            };
            Labels = labelValues;
            TraceRouteContinous();
        }

        private void ResolveTraceHost(string address, int index)
        {
            var hostName = string.Empty;
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    var hostEntry = Dns.GetHostEntry(address);
                    if (hostEntry != null)
                        if (!string.IsNullOrWhiteSpace(hostEntry.HostName))
                        {
                            hostName = hostEntry.HostName;
                            worker.ReportProgress(1);
                        }
                }
                catch (SocketException) { return; }
                catch (Exception ex)
                {
                    Toolbox.LogException(ex);
                }
            };
            worker.ProgressChanged += (ps, pe) =>
            {
                try
                {
                    if (pe.ProgressPercentage == 1)
                    {
                        Labels[index] = hostName;
                    }
                }
                catch (Exception ex)
                {
                    Toolbox.LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        private void TraceRouteContinous()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    foreach (var entry in TraceList)
                    {
                        ContinuousTraceEntryPing(entry);
                    }
                }
                catch (Exception ex)
                {
                    Toolbox.LogException(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        private void ContinuousTraceEntryPing(TraceModel entry)
        {
            TraceModel currentModel = null;
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (ws, we) =>
            {
                Toolbox.uAddDebugLog($"Started continuous ping for TraceEntry {entry.Address}");
                Ping ping = new Ping();
                while (exists)
                {
                    while (tracing)
                    {
                            try
                            {
                                if (entry.Address != "*")
                                {
                                    var reply = ping.Send(entry.Address);
                                    entry.HighRTT = reply.RoundtripTime > entry.HighRTT ? reply.RoundtripTime : entry.HighRTT;
                                    entry.LowRTT = reply.RoundtripTime < entry.LowRTT ? reply.RoundtripTime : entry.LowRTT;
                                    entry.OpenRTT = reply.RoundtripTime;
                                    currentModel = entry;
                                    Toolbox.uAddDebugLog($"Updated TraceModel {entry.Address}");
                                    worker.ReportProgress(1);
                                }
                                else
                                    Toolbox.uAddDebugLog($"Entry {entry.Address} doesn't respond to trace");
                            }
                            catch (PingException pe) { Toolbox.uAddDebugLog($"PingException: {entry}::{pe.Message}"); }
                            catch (Exception ex)
                            {
                                Toolbox.LogException(ex);
                            }
                        Thread.Sleep(1000);
                    }
                    Thread.Sleep(1000);
                }
            };
            worker.ProgressChanged += (ps, pe) =>
            {
                if (pe.ProgressPercentage == 1)
                {
                    Toolbox.uAddDebugLog($"Updating currentModel ohlcpoint {currentModel.Address}");
                    SeriesCollection[0].Values[currentModel.Index] = new OhlcPoint(currentModel.OpenRTT, currentModel.HighRTT, currentModel.LowRTT, currentModel.CloseRTT);
                    SeriesCollection[1].Values[currentModel.Index] = currentModel.OpenRTT;
                    Toolbox.uAddDebugLog($"Ohlcpoint for {currentModel.Address} successfully updated");
                }
            };
            worker.RunWorkerAsync();
            
        }

        public void ToggleTrace(bool? setTracing = null)
        {
            if (setTracing == null)
            {
                if (tracing)
                    tracing = false;
                else
                    tracing = true;
            }
            else
                tracing = (bool)setTracing;
        }

        public void Dispose()
        {
            exists = false;
            tracing = false;
            Toolbox.uAddDebugLog("Disposed TraceEntry");
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

    public class TraceRoute
    {
        /// <summary>
        /// Based off model displayed at blogs.appbeat.io:
        /// http://blogs.appbeat.io/post/c-sharp-method-for-very-fast-and-efficient-traceroute-network-diagnostic-tool
        /// </summary>
        private const string DATA = "Panacea is like Zalgo, it's coming"; //34 Bytes
        private static readonly byte[] _buffer = Encoding.ASCII.GetBytes(DATA);
        private const int MAX_HOPS = 30;
        private const int REQUEST_TIMEOUT = 4000;
        public List<TraceModel> traceModelList = new List<TraceModel>();
        
        public async Task<List<TraceModel>> TraceRouteAsync(string hostNameOrAddress)
        {
            EnsureCommonArguments(hostNameOrAddress);
            Contract.EndContractBlock();
            var arrTraceRouteTasks = new Task<TraceModel>[MAX_HOPS];
            for (int zeroBasedHop = 0; zeroBasedHop < MAX_HOPS; zeroBasedHop++)
            {
                arrTraceRouteTasks[zeroBasedHop] = TraceRouteHopGatherAsync(hostNameOrAddress, zeroBasedHop);
            }
            await Task.WhenAll(arrTraceRouteTasks);
            for (int hop = 0; hop < MAX_HOPS; hop++)
            {
                var traceTask = arrTraceRouteTasks[hop];
                if (traceTask.Status == TaskStatus.RanToCompletion)
                {
                    var model = traceTask.Result;
                    traceModelList.Add(model);
                    if (model.IPStatus == IPStatus.Success)
                        break;
                }
                else
                {
                    traceModelList.Add(new TraceModel()
                    {
                        Address = "Unknown",
                        HostName = "Unknown",
                        CloseRTT = -1,
                        HighRTT = -1,
                        LowRTT = -1,
                        OpenRTT = -1
                    });
                }
            }
            return traceModelList;
        }

        private static void EnsureCommonArguments(string hostNameOrAddress)
        {
            if (hostNameOrAddress == null)
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            if (string.IsNullOrWhiteSpace(hostNameOrAddress))
            {
                throw new ArgumentException("Hostname or address is required", nameof(hostNameOrAddress));
            }
        }

        public class TraceRouteResult
        {
            public TraceRouteResult(string message, bool isComplete)
            {
                Message = message;
                IsComplete = isComplete;
            }

            public string Message
            {
                get; private set;
            }

            public bool IsComplete
            {
                get; private set;
            }
        }

        public async Task<TraceModel> TraceRouteHopGatherAsync(string hostNameOrAddress, int zeroBasedHop)
        {
            using (Ping pingSender = new Ping())
            {
                var hop = zeroBasedHop + 1;

                PingOptions pingOptions = new PingOptions();
                Stopwatch stopWatch = new Stopwatch();
                pingOptions.DontFragment = true;
                pingOptions.Ttl = hop;

                stopWatch.Start();

                PingReply pingReply = await pingSender.SendPingAsync(
                    hostNameOrAddress,
                    REQUEST_TIMEOUT,
                    _buffer,
                    pingOptions
                );

                stopWatch.Stop();

                var elapsedMilliseconds = stopWatch.ElapsedMilliseconds;

                string pingReplyAddress;

                if (pingReply.Status == IPStatus.TimedOut)
                {
                    pingReplyAddress = "*";
                    elapsedMilliseconds = -1;
                }
                else
                {
                    pingReplyAddress = pingReply.Address.ToString();
                }

                return new TraceModel() { Address = pingReplyAddress, CloseRTT = elapsedMilliseconds, HighRTT = elapsedMilliseconds, LowRTT = elapsedMilliseconds, OpenRTT = elapsedMilliseconds, Index = zeroBasedHop, IPStatus = pingReply.Status };
            }
        }
    }

    #endregion

    public class Network
    {
        public static bool IsPortOpen(string host, int port, TimeSpan timeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(timeout);
                    if (!success)
                    {
                        return false;
                    }

                    client.EndConnect(result);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
