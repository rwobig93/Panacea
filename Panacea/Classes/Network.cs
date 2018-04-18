using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        private Int32 _historyLength { get; set; } = Toolbox.settings.PingChartLength + 2;
        private Int32 _gridHeight { get; set; } = 104;
        private Int32 _gridWidth { get; set; }
        private Int32 _chartLength { get; set; } = Toolbox.settings.PingChartLength;
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

    #region Trace

    public class TraceModel : INotifyPropertyChanged
    {
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
        private bool tracing = false;
        private List<TraceModel> _traceList = new List<TraceModel>();
        private string[] _labels;
        private string _chartTitle { get; set; }
        private double _gridHeight { get; set; } = 165;
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

        public TraceEntry()
        {
            var fakeList = new List<string>() { "192.168.1.1", "192.168.1.91", "8.8.8.8", "8.8.4.4", "216.69.100.100" };
            var ohlcValues = new ChartValues<OhlcPoint>();
            var lineValues = new ChartValues<double>();
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (ws, we) =>
            {
                int indexNum = 0;
                var labelArray = new string[5];
                foreach (var trace in fakeList)
                {
                    //Ping ping = new Ping();
                    //var reply = ping.Send(trace);
                    //if (reply != null)
                    //    _traceList.Add(new TraceModel()
                    //    {
                    //        Address = reply.Address == null ? "Unknown" : reply.Address.ToString(),
                    //        HostName = "Hostname Not Found",
                    //        HighRTT = reply.RoundtripTime,
                    //        LowRTT = reply.RoundtripTime,
                    //        CloseRTT = reply.RoundtripTime,
                    //        OpenRTT = reply.RoundtripTime,
                    //        Index = indexNum
                    //    });
                    //else
                    //    _traceList.Add(new TraceModel()
                    //    {
                    //        Address = reply.Address == null ? "Unknown" : reply.Address.ToString(),
                    //        HostName = "Hostname Not Found",
                    //        HighRTT = 0,
                    //        LowRTT = 0,
                    //        CloseRTT = 0,
                    //        OpenRTT = 0,
                    //        Index = indexNum
                    //    });
                    _traceList.Add(new TraceModel()
                    {
                        Address = trace,
                        HostName = "Hostname Not Found",
                        HighRTT = Toolbox.GenerateRandomNumber(55, 100),
                        LowRTT = Toolbox.GenerateRandomNumber(0, 30),
                        CloseRTT = Toolbox.GenerateRandomNumber(30, 55),
                        OpenRTT = Toolbox.GenerateRandomNumber(30, 55),
                        Index = indexNum
                    });
                    Thread addressLookup = new Thread(() =>
                    {
                        try
                        {
                            var dnsEntry = Dns.GetHostEntry(trace);
                            if (dnsEntry != null)
                            {
                                if (!string.IsNullOrWhiteSpace(dnsEntry.HostName))
                                {
                                    _traceList.Find(x => x.Address == trace).HostName = dnsEntry.HostName;
                                }
                            }
                        }
                        catch (SocketException se)
                        {
                            Toolbox.uAddDebugLog($"{tracing}: {se.Message}");
                        }
                        catch (Exception ex)
                        {
                            Toolbox.LogException(ex);
                        }
                    });
                    addressLookup.Start();
                    indexNum++;
                }
                foreach (var entry in _traceList)
                {
                    ohlcValues.Add(new OhlcPoint(entry.OpenRTT, entry.HighRTT, entry.LowRTT, entry.CloseRTT));
                    lineValues.Add(entry.CloseRTT);
                    labelArray[entry.Index] = $"({entry.Address}){Environment.NewLine}{entry.HostName}";
                }
                worker.ReportProgress(2);
                Labels = labelArray;
                tracing = true;
                worker.ReportProgress(1);
            };
            worker.ProgressChanged += (ps, pe) =>
            {
                if (pe.ProgressPercentage == 1)
                {
                    TraceRouteContinous();
                }
                if (pe.ProgressPercentage == 2)
                {
                    _seriesCollection = new SeriesCollection
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
                }
            };
            worker.RunWorkerAsync();
        }

        public TraceEntry(IEnumerable<IPAddress> traceResults)
        {
            var ohlcValues = new ChartValues<OhlcPoint>();
            var lineValues = new ChartValues<double>();
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (ws, we) =>
            {
                int indexNum = 0;
                var labelArray = new string[traceResults.Count()];
                foreach (var trace in traceResults)
                {
                    Ping ping = new Ping();
                    var reply = ping.Send(trace);
                    if (reply != null)
                        _traceList.Add(new TraceModel()
                        {
                            Address = reply.Address == null ? "Unknown" : reply.Address.ToString(),
                            HostName = "Hostname Not Found",
                            HighRTT = reply.RoundtripTime,
                            LowRTT = reply.RoundtripTime,
                            CloseRTT = reply.RoundtripTime,
                            OpenRTT = reply.RoundtripTime,
                            Index = indexNum
                        });
                    else
                        _traceList.Add(new TraceModel()
                        {
                            Address = reply.Address == null ? "Unknown" : reply.Address.ToString(),
                            HostName = "Hostname Not Found",
                            HighRTT = 0,
                            LowRTT = 0,
                            CloseRTT = 0,
                            OpenRTT = 0,
                            Index = indexNum
                        });
                    Thread addressLookup = new Thread(() => 
                    {
                        try
                        {
                            var dnsEntry = Dns.GetHostEntry(trace);
                            if (dnsEntry != null)
                            {
                                if (!string.IsNullOrWhiteSpace(dnsEntry.HostName))
                                {
                                    _traceList.Find(x => x.Address == reply.Address.ToString()).HostName = dnsEntry.HostName;
                                }
                            }
                        }
                        catch (SocketException se)
                        {
                            Toolbox.uAddDebugLog($"{tracing}: {se.Message}");
                        }
                        catch (Exception ex)
                        {
                            Toolbox.LogException(ex);
                        }
                    });
                    addressLookup.Start();
                    indexNum++;
                }
                foreach (var entry in _traceList)
                {
                    ohlcValues.Add(new OhlcPoint(entry.OpenRTT, entry.HighRTT, entry.LowRTT, entry.CloseRTT));
                    lineValues.Add(entry.CloseRTT);
                    labelArray[entry.Index] = $"({entry.Address}){Environment.NewLine}{entry.HostName}";
                }
                worker.ReportProgress(2);
                Labels = labelArray;
                tracing = true;
                worker.ReportProgress(1);
            };
            worker.ProgressChanged += (ps, pe) =>
            {
                if (pe.ProgressPercentage == 1)
                {
                    TraceRouteContinous();
                }
                if (pe.ProgressPercentage == 2)
                {
                    _seriesCollection = new SeriesCollection
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
                }
            };
            worker.RunWorkerAsync();
        }

        private void TraceRouteContinous()
        {
            BackgroundWorker worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (sender, e) =>
            {
                try
                {
                    Ping ping = new Ping();
                    while (true)
                    {
                        while (tracing)
                        {
                            foreach (var entry in _traceList)
                            {
                                var reply = ping.Send(entry.Address);
                                entry.HighRTT = reply.RoundtripTime > entry.HighRTT ? reply.RoundtripTime : entry.HighRTT;
                                entry.LowRTT = reply.RoundtripTime < entry.LowRTT ? reply.RoundtripTime : entry.LowRTT;
                                entry.OpenRTT = reply.RoundtripTime;
                                entry.CloseRTT = reply.RoundtripTime;
                            }
                        }
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {
                    Toolbox.LogException(ex);
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

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class TestTrace : INotifyPropertyChanged
    {
        private string[] _labels;
        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels
        {
            get { return _labels; }
            set
            {
                _labels = value;
                OnPropertyChanged("Labels");
            }
        }

        public TestTrace(List<TraceModel> addresses)
        {
            SeriesCollection = new SeriesCollection
            {
                new OhlcSeries()
                {
                    Values = new ChartValues<OhlcPoint>
                    {
                        new OhlcPoint(addresses[0].OpenRTT, addresses[0].HighRTT, addresses[0].LowRTT, addresses[0].CloseRTT),
                        new OhlcPoint(addresses[1].OpenRTT, addresses[1].HighRTT, addresses[1].LowRTT, addresses[1].CloseRTT),
                        new OhlcPoint(addresses[2].OpenRTT, addresses[2].HighRTT, addresses[2].LowRTT, addresses[2].CloseRTT),
                        new OhlcPoint(addresses[3].OpenRTT, addresses[3].HighRTT, addresses[3].LowRTT, addresses[3].CloseRTT),
                        new OhlcPoint(addresses[4].OpenRTT, addresses[4].HighRTT, addresses[4].LowRTT, addresses[4].CloseRTT)
                    }
                },
                new LineSeries
                {
                    Values = new ChartValues<double> { addresses[0].OpenRTT, addresses[1].OpenRTT, addresses[2].OpenRTT, addresses[3].OpenRTT, addresses[4].OpenRTT},
                    Fill = Brushes.Transparent
                }
            };
            
            Labels = new[]
            {
                addresses[0].Address,
                addresses[1].Address,
                addresses[2].Address,
                addresses[3].Address,
                addresses[4].Address,
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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

        /// <summary>
        /// Runs traceroute and writes result to console.
        /// </summary>
        public async Task<List<TraceModel>> TraceRouteAsync(string hostNameOrAddress)
        {
            EnsureCommonArguments(hostNameOrAddress);
            Contract.EndContractBlock();

            return await TryTraceRouteAsync(hostNameOrAddress);
        }

        /// <summary>
        /// Runs traceroute and writes result to provided stream.
        /// </summary>
        public async Task<List<TraceModel>> TryTraceRouteAsync(string hostNameOrAddress)
        {
            EnsureCommonArguments(hostNameOrAddress);
            Contract.EndContractBlock();

            //dispatch parallel tasks for each hop
            var arrTraceRouteTasks = new Task<TraceModel>[MAX_HOPS];
            for (int zeroBasedHop = 0; zeroBasedHop < MAX_HOPS; zeroBasedHop++)
            {
                arrTraceRouteTasks[zeroBasedHop] = TryTraceRouteInternalAsync(hostNameOrAddress, zeroBasedHop);
            }

            //and wait for them to finish
            await Task.WhenAll(arrTraceRouteTasks);

            //now just collect all results and write them to output stream
            for (int hop = 0; hop < MAX_HOPS; hop++)
            {
                var traceTask = arrTraceRouteTasks[hop];
                if (traceTask.Status == TaskStatus.RanToCompletion)
                {
                    var model = traceTask.Result;
                    traceModelList.Add(model);
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

        public async Task<TraceModel> TryTraceRouteInternalAsync(string hostNameOrAddress, int zeroBasedHop)
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

                return new TraceModel() { Address = pingReplyAddress, CloseRTT = elapsedMilliseconds, HighRTT = elapsedMilliseconds, LowRTT = elapsedMilliseconds, OpenRTT = elapsedMilliseconds };
            }
        }
    }

    #endregion
}
