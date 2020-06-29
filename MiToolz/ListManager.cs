using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using NAudio.CoreAudioApi;

namespace MiToolz
{
    internal class ListManager
    {
        private readonly MainWindow _mw = (MainWindow)Application.Current.MainWindow;

        private static readonly MMDeviceEnumerator Enumerator = new MMDeviceEnumerator();

        internal static List<string> AudioDevicesList
        {
            get
            {
                var audioDevicesList = new List<string>();
                foreach (var endpoint in
                    Enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    var audioDevices = endpoint.FriendlyName;
                    const string pattern = @"(?: ?[(\[][^)\]]+.)*$";
                    var result = Regex.Replace(audioDevices, pattern, string.Empty);
                    audioDevicesList.Add(result);
                }
                return audioDevicesList;
            }
        }

        internal static string DefaultAudioDevice
        {
            get
            {
                var defaultAudioOutput = Enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).FriendlyName;
                const string pattern = @"(?: ?[(\[][^)\]]+.)*$";
                var result = Regex.Replace(defaultAudioOutput, pattern, string.Empty);
                return result;
            }
        }

        internal IEnumerable<Tile> ControlsMainTiles
        {
            get
            {
                var controlsMainTiles = new List<Tile>
                {
                    _mw.GpuTile,
                    _mw.PowerPlanTile,
                    _mw.AudioTile,
                    _mw.TimerResolutionTile,
                    _mw.AudioDeviceSwitchTile
                };
                return controlsMainTiles;
            }
        }

        internal IEnumerable<TextBlock> ControlsTextBlocks
        {
            get
            {
                var controlsTextBlocks = new List<TextBlock>
                {
                    _mw.CoreSpeed,
                    _mw.MemSpeed,
                    _mw.CoreLoad,
                    _mw.MemLoad,
                    _mw.CoreTemp,
                    _mw.TotalPower,
                    _mw.CpuSpeed,
                    _mw.CpuTemp
                };
                return controlsTextBlocks;
            }
        }

        internal IEnumerable<Tile> ControlsTiles
        {
            get
            {
                var controlsTiles = new List<Tile>
                {
                    _mw.CoreSpeedTile,
                    _mw.MemSpeedTile,
                    _mw.CoreLoadTile,
                    _mw.MemLoadTile,
                    _mw.CoreTempTile,
                    _mw.TotalPowerTile,
                    _mw.CpuSpeedTile,
                    _mw.CpuTempTile
                };
                return controlsTiles;
            }
        }

        internal static List<string> HotKey
        {
            get
            {
                var hotKey = new List<string>
                {
                    "--",
                    "A",
                    "B",
                    "C",
                    "D",
                    "E",
                    "F",
                    "G",
                    "H",
                    "I",
                    "J",
                    "K",
                    "L",
                    "M",
                    "N",
                    "O",
                    "P",
                    "Q",
                    "R",
                    "S",
                    "T",
                    "U",
                    "V",
                    "W",
                    "X",
                    "Y",
                    "Z",
                    "0",
                    "1",
                    "2",
                    "3",
                    "4",
                    "5",
                    "6",
                    "7",
                    "8",
                    "9",
                    "F1",
                    "F2",
                    "F3",
                    "F4",
                    "F5",
                    "F6",
                    "F7",
                    "F8",
                    "F9",
                    "F10",
                    "F11",
                    "F12"
                };
                return hotKey;
            }
        }

        internal static List<string> MsiAbProfileList
        {
            get
            {
                var hotKeyMsiAb = new List<string>
                {
                    "1",
                    "2",
                    "3",
                    "4",
                    "5"
                };
                return hotKeyMsiAb;
            }
        }
    }
}
