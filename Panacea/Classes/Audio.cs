using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using Panacea.Windows;

namespace Panacea.Classes
{
    static class Interfaces
    {
        public static Audio AudioMain = new Audio();
    }

    public class Audio : INotifyPropertyChanged
    {
        public bool FirstRefresh = true;
        private static List<MMDevice> _endpointAudioDevices = new List<MMDevice>();
        public List<MMDevice> EndpointAudioDeviceList
        {
            get { return _endpointAudioDevices; }
            set
            {
                _endpointAudioDevices = value;
                OnPropertyChanged("EndpointAudioDeviceList");
            }
        }
        private static List<MMDevice> _endpointAudioRecordingDevices = new List<MMDevice>();
        public List<MMDevice> EndpointAudioRecordingDeviceList
        {
            get { return _endpointAudioRecordingDevices; }
            set
            {
                _endpointAudioRecordingDevices = value;
                OnPropertyChanged("EndpointAudioRecordingDeviceList");
            }
        }
        private static List<MMDevice> _endpointAudioPlaybackDevices = new List<MMDevice>();
        public List<MMDevice> EndpointAudioPlaybackDeviceList
        {
            get { return _endpointAudioPlaybackDevices; }
            set
            {
                _endpointAudioPlaybackDevices = value;
                OnPropertyChanged("EndpointAudioPlaybackDeviceList");
            }
        }

        public static List<MMDevice> GetAudioDevices(DeviceState deviceState = DeviceState.Active)
        {
            var audioList = new List<MMDevice>();
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, deviceState))
            {
                audioList.Add(wasapi);
            }
            return audioList;
        }

        public static List<MMDevice> GetAudioRecordingDevices(DeviceState deviceState = DeviceState.Active)
        {
            var audioList = new List<MMDevice>();
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, deviceState))
            {
                audioList.Add(wasapi);
            }
            return audioList;
        }

        public static void UpdateAudioAllEndpointLists(DeviceState deviceState = DeviceState.Active)
        {
            Director.Main.UpdatePlaybackEndpointList(deviceState, true);
            Director.Main.UpdateRecordingEndpointList(deviceState, true);
        }

        public static List<MMDevice> GetAudioPlaybackDevices(DeviceState deviceState = DeviceState.Active)
        {
            var audioList = new List<MMDevice>();
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, deviceState))
            {
                audioList.Add(wasapi);
            }
            return audioList;
        }

        public static MMDevice GetDefaultAudioDevice(Role role = Role.Multimedia)
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, role);
        }

        public static MMDevice GetDefaultAudioPlaybackDevice(Role role = Role.Multimedia)
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, role);
        }

        public static MMDevice GetDefaultAudioRecordingDevice(Role role = Role.Multimedia)
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, role);
        }

        public static void SetDefaultAudioDevice(MMDevice mMDevice)
        {
            try
            {
                var config = new PolicyConfig();
                int hr1;
                int hr2;
                int hr3;
                bool? win10 = null;
                IPolicyConfig2 config2 = config as IPolicyConfig2;
                if (config2 != null)
                {
                    win10 = false;
                    hr1 = config2.SetDefaultEndpoint(mMDevice.ID, AudioDeviceRole.Multimedia);
                    hr2 = config2.SetDefaultEndpoint(mMDevice.ID, AudioDeviceRole.Communications);
                    hr3 = config2.SetDefaultEndpoint(mMDevice.ID, AudioDeviceRole.Console);
                }
                else
                {
                    win10 = true;
                    hr1 = ((IPolicyConfig3)config).SetDefaultEndpoint(mMDevice.ID, AudioDeviceRole.Multimedia);
                    hr2 = ((IPolicyConfig3)config).SetDefaultEndpoint(mMDevice.ID, AudioDeviceRole.Communications);
                    hr3 = ((IPolicyConfig3)config).SetDefaultEndpoint(mMDevice.ID, AudioDeviceRole.Console);
                }
                Toolbox.uAddDebugLog($"NAudio HResult Definitions: s_OK({HResult.S_OK}) | s_FALSE({HResult.S_FALSE}) | e_INVALIDARG({HResult.E_INVALIDARG})");
                Toolbox.uAddDebugLog($"SetAudioDefaultResults: win10({win10}) | hr1({hr1}) | hr2({hr2}) | hr3({hr3})");
            }
            catch (Exception ex)
            {
                Toolbox.LogException(ex);
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

        [ComImport]
        [Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
        internal class PolicyConfig
        {
        }

        [ComImport]
        [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]  // Windows 7 -> Windows 8.1
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPolicyConfig2
        {
            void Reserved1();

            void Reserved2();

            void Reserved3();

            void Reserved4();

            void Reserved5();

            void Reserved6();

            void Reserved7();

            void Reserved8();

            void Reserved9();

            void Reserved10();

            [PreserveSig]
            int SetDefaultEndpoint(string deviceId, AudioDeviceRole role);

            void Reserved11();
        }

        [ComImport]
        [Guid("00000000-0000-0000-C000-000000000046")]  // We just cast it to IUnknown, and pray that v-table layout is correct
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPolicyConfig3
        {
            void Reserved1();

            void Reserved2();

            void Reserved3();

            void Reserved4();

            void Reserved5();

            void Reserved6();

            void Reserved7();

            void Reserved8();

            void Reserved9();

            void Reserved10();

            [PreserveSig]
            int SetDefaultEndpoint(string deviceId, AudioDeviceRole role);

            void Reserved11();
        }
    }

    /// <summary>
    /// The ERole enumeration defines constants that indicate the role 
    /// that the system has assigned to an audio endpoint device
    /// </summary>
    internal enum AudioDeviceRole
    {
        /// <summary>
        /// Games, system notification sounds, and voice commands.
        /// </summary>
        Console,

        /// <summary>
        /// Music, movies, narration, and live music recording
        /// </summary>
	    Multimedia,

        /// <summary>
        /// Voice communications (talking to another person).
        /// </summary>
	    Communications,
    }
}
