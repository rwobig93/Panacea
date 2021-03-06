﻿using NAudio.CoreAudioApi;
using Panacea.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using static Panacea.MainWindow;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for AudioPopup.xaml
    /// </summary>
    public partial class AudioPopup : Window
    {
        public AudioPopup()
        {
            SetDefaultLocation();
            InitializeComponent();
            Startup();
        }

        #region Globals

        private bool startingUp = true;
        private bool _firstStartup = true;
        private DoubleAnimation outAnimation = new DoubleAnimation() { To = 0.0, Duration = TimeSpan.FromSeconds(.2) };
        private DoubleAnimation inAnimation = new DoubleAnimation() { To = 1.0, Duration = TimeSpan.FromSeconds(.2) };
        private double PopinLeft { get { return UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuAudio.Margin.Left; } }
        private double PopinTop { get { return UtilityBar.UtilBarMain.Top - this.ActualHeight; } }
        private double PopinWidth { get { return 551; } }
        private double PopinHeight { get { return 375; } }
        public bool PoppedOut { get; set; } = false;
        bool playbackDeviceRefreshing = false;
        bool recordingDeviceRefreshing = false;
        private MMDevice selectedPlaybackEndpoint = null;
        private MMDevice selectedRecordingEndpoint = null;

        #endregion

        #region Form Handling

        private void WinAudioMain_Loaded(object sender, RoutedEventArgs e)
        {
            EnableWindowResizing();
        }

        private void WinAudioMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!startingUp)
                VerifyResetButtonRequirement();
        }

        #endregion

        #region Methods

        private void Startup()
        {
            PopupShow();
            RefreshAudioDevicesLists();
            SubscribeToEvents();
            //Audio.UpdateAudioAllEndpointLists();
            FinishStartup();
        }

        private void SubscribeToEvents()
        {
            try
            {
                Events.UtilBarMoveTrigger += Events_UtilBarMoveTrigger;
                Events.AudioEndpointListUpdated += Events_AudioEndpointListUpdated;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Events_AudioEndpointListUpdated(AudioEndpointListArgs args)
        {
            UpdateAudioListUI(args.Flow);
        }

        private void UpdateAudioListUI(DataFlow flow)
        {
            try
            {
                switch (flow)
                {
                    case DataFlow.Capture:
                        recordingDeviceRefreshing = true;
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lbRecordingDevices.ItemsSource = Interfaces.AudioMain.EndpointAudioRecordingDeviceList; } catch (Exception ex) { LogException(ex); } });
                        var defDeviceR = Audio.GetDefaultAudioRecordingDevice();
                        foreach (var item in lbRecordingDevices.Items)
                        {
                            if (((MMDevice)item).ID == defDeviceR.ID)
                            {
                                selectedRecordingEndpoint = ((MMDevice)item);
                                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lbRecordingDevices.SelectedItem = item; } catch (Exception ex) { LogException(ex); } });
                            }
                        }
                        recordingDeviceRefreshing = false;
                        break;
                    case DataFlow.Render:
                        playbackDeviceRefreshing = true;
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lbPlaybackDevices.ItemsSource = Interfaces.AudioMain.EndpointAudioPlaybackDeviceList; } catch (Exception ex) { LogException(ex); } });
                        var defDeviceP = Audio.GetDefaultAudioPlaybackDevice();
                        foreach (var item in lbPlaybackDevices.Items)
                        {
                            if (((MMDevice)item).ID == defDeviceP.ID)
                            {
                                selectedPlaybackEndpoint = ((MMDevice)item);
                                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lbPlaybackDevices.SelectedItem = item; } catch (Exception ex) { LogException(ex); } });
                            }
                        }
                        playbackDeviceRefreshing = false;
                        break;
                    case DataFlow.All:
                        recordingDeviceRefreshing = true;
                        playbackDeviceRefreshing = true;
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lbPlaybackDevices.ItemsSource = Interfaces.AudioMain.EndpointAudioPlaybackDeviceList; } catch (Exception ex) { LogException(ex); } });
                        var defDevicePA = Audio.GetDefaultAudioPlaybackDevice();
                        foreach (var item in lbPlaybackDevices.Items)
                        {
                            if (((MMDevice)item).ID == defDevicePA.ID)
                            {
                                selectedPlaybackEndpoint = ((MMDevice)item);
                                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lbPlaybackDevices.SelectedItem = item; } catch (Exception ex) { LogException(ex); } });
                            }
                        }
                        Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lbRecordingDevices.ItemsSource = Interfaces.AudioMain.EndpointAudioRecordingDeviceList; } catch (Exception ex) { LogException(ex); } });
                        var defDeviceRA = Audio.GetDefaultAudioRecordingDevice();
                        foreach (var item in lbRecordingDevices.Items)
                        {
                            if (((MMDevice)item).ID == defDeviceRA.ID)
                            {
                                selectedRecordingEndpoint = ((MMDevice)item);
                                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { try { lbRecordingDevices.SelectedItem = item; } catch (Exception ex) { LogException(ex); } });
                            }
                        }
                        recordingDeviceRefreshing = false;
                        playbackDeviceRefreshing = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Events_UtilBarMoveTrigger(UtilMoveArgs args)
        {
            try
            {
                if (!PoppedOut)
                    MoveToNewLocation();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MoveToNewLocation()
        {
            try
            {
                this.Top = PopinTop; //UtilityBar.UtilBarMain.Top - PopinHeight;
                this.Left = PopinLeft; //UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuAudio.Margin.Left;
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
                startingUp = false;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

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
                Toolbox.uAddDebugLog($"POPAUD: {_log}", _type, caller);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void EnableWindowResizing()
        {
            try
            {
                // Allow borderless window to be resized
                WindowChrome.SetWindowChrome(this, new WindowChrome() { ResizeBorderThickness = new Thickness(5), CaptionHeight = .05 });
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void SetDefaultLocation()
        {
            try
            {
                this.Top = UtilityBar.UtilBarMain.Top - PopinHeight;
                this.Left = UtilityBar.UtilBarMain.Left + UtilityBar.UtilBarMain.btnMenuAudio.Margin.Left;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void PopupHide()
        {
            try
            {
                //if (this.Opacity == 1.0)
                //    this.BeginAnimation(Window.OpacityProperty, outAnimation);
                this.Hide();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void PopupShow()
        {
            try
            {
                //if (this.Opacity == 0)
                //    this.BeginAnimation(Window.OpacityProperty, inAnimation);
                this.Show();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void VerifyResetButtonRequirement()
        {
            try
            {
                if ((this.Left != PopinLeft || this.Top != PopinTop || this.ActualWidth != PopinWidth || this.ActualHeight != PopinHeight) && btnReset.Visibility != Visibility.Visible)
                {
                    btnReset.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void PlaybackDevicesSelectionChanged()
        {
            try
            {
                if (!playbackDeviceRefreshing)
                {
                    var selectedEndpoint = ((MMDevice)lbPlaybackDevices.SelectedItem);
                    if (selectedEndpoint != selectedPlaybackEndpoint)
                    {
                        selectedPlaybackEndpoint = selectedEndpoint;
                        Audio.SetDefaultAudioDevice(selectedEndpoint);
                        ShowNotification($"Playback Device Changed: {selectedEndpoint.DeviceFriendlyName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RecordingDevicesSelectionChanged()
        {
            try
            {
                if (!recordingDeviceRefreshing)
                {
                    var selectedEndpoint = ((MMDevice)lbRecordingDevices.SelectedItem);
                    if (selectedEndpoint != selectedRecordingEndpoint)
                    {
                        selectedRecordingEndpoint = selectedEndpoint;
                        Audio.SetDefaultAudioDevice(selectedEndpoint);
                        ShowNotification($"Recording Device Changed: {selectedEndpoint.DeviceFriendlyName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RefreshAudioDevicesLists()
        {
            if (playbackDeviceRefreshing || recordingDeviceRefreshing)
            {
                ShowNotification("Audio Devices Already Refreshing");
                return;
            }
            else
            {
                RefreshPlaybackDevices();
                RefreshRecordingDevices();
                if (!startingUp && !_firstStartup)
                    ShowNotification("Refreshed Audio Devices");
                else
                    _firstStartup = false;
            }
        }

        private void RefreshRecordingDevices()
        {
            try
            {
                recordingDeviceRefreshing = true;
                Interfaces.AudioMain.EndpointAudioRecordingDeviceList = Audio.GetAudioRecordingDevices();
                lbRecordingDevices.ItemsSource = Interfaces.AudioMain.EndpointAudioRecordingDeviceList;
                var defDevice = Audio.GetDefaultAudioRecordingDevice();
                foreach (var item in lbRecordingDevices.Items)
                {
                    if (((MMDevice)item).ID == defDevice.ID)
                    {
                        selectedRecordingEndpoint = ((MMDevice)item);
                        lbRecordingDevices.SelectedItem = item;
                    }
                }
                recordingDeviceRefreshing = false;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void RefreshPlaybackDevices()
        {
            try
            {
                playbackDeviceRefreshing = true;
                Interfaces.AudioMain.EndpointAudioPlaybackDeviceList = Audio.GetAudioPlaybackDevices();
                lbPlaybackDevices.ItemsSource = Interfaces.AudioMain.EndpointAudioPlaybackDeviceList;
                var defDevice = Audio.GetDefaultAudioPlaybackDevice();
                foreach (var item in lbPlaybackDevices.Items)
                {
                    if (((MMDevice)item).ID == defDevice.ID)
                    {
                        selectedPlaybackEndpoint = ((MMDevice)item);
                        lbPlaybackDevices.SelectedItem = item;
                    }
                }
                playbackDeviceRefreshing = false;
                UtilityBar.UtilBarMain.ShowNotification("Audio Devices Refreshed");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void TogglePopout(bool? forcePoppin = null)
        {
            try
            {
                if (forcePoppin != null)
                {
                    uDebugLogAdd($"Forcing poppin: {forcePoppin}");
                    PoppedOut = (bool)forcePoppin;
                }
                if (PoppedOut)
                {
                    PoppedOut = false;
                    this.Left = PopinLeft;
                    this.Top = PopinTop;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.ShowInTaskbar = false;
                    plyAudioVisualSlider.Visibility = Visibility.Visible;
                    btnMinimize.Visibility = Visibility.Hidden;
                    btnPopInOut.Content = "🢅";
                }
                else if (!PoppedOut)
                {
                    PoppedOut = true;
                    this.Left += 10;
                    this.Top -= 10;
                    this.ResizeMode = ResizeMode.CanResize;
                    this.ShowInTaskbar = true;
                    plyAudioVisualSlider.Visibility = Visibility.Hidden;
                    btnMinimize.Visibility = Visibility.Visible;
                    btnPopInOut.Content = "🢇";
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ResetPopupSizeAndLocation()
        {
            try
            {
                uDebugLogAdd("Resetting Audio Popup Size and Location to Default");
                this.Left = PopinLeft;
                this.Top = PopinTop;
                this.Width = PopinWidth;
                this.Height = PopinHeight;
                btnReset.Visibility = Visibility.Hidden;
                TogglePopout(true);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShowNotification(string text)
        {
            try
            {
                uDebugLogAdd($"Calling ShowNotification from AudioPopup: {text}");
                UtilityBar.UtilBarMain.ShowNotification(text);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region Event Handlers

        private void LbPlaybackDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlaybackDevicesSelectionChanged();
        }

        private void LbRecordingDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecordingDevicesSelectionChanged();
        }

        private void BtnRefreshAudio_Click(object sender, RoutedEventArgs e)
        {
            RefreshAudioDevicesLists();
            //Audio.UpdateAudioAllEndpointLists();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetPopupSizeAndLocation();
        }

        private void BtnPopInOut_Click(object sender, RoutedEventArgs e)
        {
            TogglePopout();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PoppedOut)
                    TogglePopout();
                PopupHide();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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

        private void LblAudioDevices_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left && PoppedOut)
                    winAudioMain.DragMove();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            Director.Main.OpenInfoWindow(HelpMenu.AudioMenu);
        }

        #endregion
    }
}
