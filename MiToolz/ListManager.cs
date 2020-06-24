using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace MiToolz
{
    internal class ListManager
    {
        private readonly MainWindow _mw = (MainWindow)Application.Current.MainWindow;

        internal IEnumerable<Tile> ControlsMainTiles
        {
            get
            {
                var controlsMainTiles = new List<Tile>
                {
                    _mw.GpuTile,
                    _mw.PowerPlanTile,
                    _mw.AudioTile,
                    _mw.MsIabTile,
                    _mw.SoundSwitchTile
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
                    _mw.TotalPower
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
                    _mw.TotalPowerTile
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

        internal static List<string> HotKeyModifier
        {
            get
            {
                var hotKeyModifier = new List<string>
                {
                    "--",
                    "Ctrl",
                    "Shft",
                    "Alt"
                };
                return hotKeyModifier;
            }
        }

        internal static List<string> HotKeyMsiAb
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
