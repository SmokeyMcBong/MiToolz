using ControlzEx.Theming;
using LibreHardwareMonitor.Hardware;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiToolz
{
    public partial class MainWindow
    {
        private readonly Computer _thisPc;
        private static readonly ConfigManager ConfigManager = new ConfigManager();
        private static readonly ListManager ListManager = new ListManager();
        private static string _activeProfile;
        private static string _stockProfile;
        private static string _ocProfile;
        private static string _sbControlFile;
        private static string _sbControlActiveProfile;
        private static string _msiabFile;
        private static string _isMonitoringEnabled;
        private static string _appTheme;
        private static string _appHotKey;
        private static string _soundSwitchHotKeyModifier;
        private static string _soundSwitchHotKey;
        private const int DelayN = 500;
        private const int DelayL = 1000;

        public MainWindow()
        {
            //ignore Theme ResourceDictionary before initialize as setting whichever theme (_appTheme) saved in .ini
            Application.Current.Resources.MergedDictionaries.RemoveAt(0);
            InitializeComponent();

            //run startup checks, read all config settings and set corresponding UI elements
            StartupSetup();
            SetAppTheme();
            ShowActiveAudioProfile();
            ShowActiveGpuProfile();
            SetupComboListSources();

            //add MainWindow event handlers
            Closed += MainWindow_Closed;
            Activated += MainWindow_Activated;

            //start OhM Monitoring
            _thisPc = new Computer
            {
                IsGpuEnabled = true
            };

            IsMonitorEnabled();
        }

        //app startup checks
        private static void StartupSetup()
        {
            //check for duplicate instances
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location)).Length > 1)
            {
                Process.GetCurrentProcess().Kill();
            }

            //check for ini file, if not found then create new file and write default values to it
            var myConfigManager = Properties.Resources.MyConfigManager;
            var sbControlFilePath = Properties.Resources.SBControl_FilePath;
            var msiabFilePath = Properties.Resources.MSIAB_FilePath;
            var defaultStockProfile = Properties.Resources.DefaultStockProfile;
            var defaultOcProfile = Properties.Resources.DefaultOCProfile;
            var defaultMonitoringEnabled = Properties.Resources.DefaultMonitoringEnabled;
            var defaultAppTheme = Properties.Resources.DefaultAppTheme;
            var defaultAppHotKey = Properties.Resources.DefaultAppHotKey;
            var defaultSoundSwitchHotKeyModifier = Properties.Resources.DefaultSoundSwitchHotKeyModifier;
            var defaultSoundSwitchHotKey = Properties.Resources.DefaultSoundSwitchHotKey;

            if (!File.Exists(myConfigManager))
            {
                ConfigManager.IniWrite("StockProfile", defaultStockProfile);
                ConfigManager.IniWrite("OCProfile", defaultOcProfile);
                ConfigManager.IniWrite("SBControl_File", sbControlFilePath);
                ConfigManager.IniWrite("MSIAB_File", msiabFilePath);
                ConfigManager.IniWrite("IsMonitoringEnabled", defaultMonitoringEnabled);
                ConfigManager.IniWrite("AppTheme", defaultAppTheme);
                ConfigManager.IniWrite("AppHotKey", defaultAppHotKey);
                ConfigManager.IniWrite("SoundSwitchHotKeyModifier", defaultSoundSwitchHotKeyModifier);
                ConfigManager.IniWrite("SoundSwitchHotKey", defaultSoundSwitchHotKey);
                ReadSettings();
            }
            else
            {
                ReadSettings();
            }
        }

        //read in values from ini file
        private static void ReadSettings()
        {
            _stockProfile = ConfigManager.IniRead("StockProfile");
            _ocProfile = ConfigManager.IniRead("OCProfile");
            _sbControlFile = ConfigManager.IniRead("SBControl_File");
            _msiabFile = ConfigManager.IniRead("MSIAB_File");
            _isMonitoringEnabled = ConfigManager.IniRead("IsMonitoringEnabled");
            _appTheme = ConfigManager.IniRead("AppTheme");
            _appHotKey = ConfigManager.IniRead("AppHotKey");
            _soundSwitchHotKeyModifier = ConfigManager.IniRead("SoundSwitchHotKeyModifier");
            _soundSwitchHotKey = ConfigManager.IniRead("SoundSwitchHotKey");

            var sbControlProfileFilePath = Properties.Resources.SBControl_ProfileFilePath;
            var sbControlProfileRegPath = Properties.Resources.SBControl_ProfileRegPath;

            //full path to profile folder
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sbPath = userProfile + @"\" + sbControlProfileFilePath;

            //get folder name of subfolder/deviceID (HDAUDIO_VEN_10EC_DEV_0899_SUBSYS_11020041 etc)
            var sbGetIdDir = Directory.GetDirectories(sbPath, "HDAUDIO*", SearchOption.TopDirectoryOnly);
            var sbDeviceIdPath = string.Join("", sbGetIdDir);
            var sbDeviceId = sbDeviceIdPath.Substring(sbDeviceIdPath.LastIndexOf(@"\", StringComparison.Ordinal) + 1);

            //get registry key value for active profile using deviceID
            var sbRegKeyName = sbControlProfileRegPath + sbDeviceId;
            var sbGetValue = ConfigManager.RegReadKeyValue(sbRegKeyName, "Profile");
            var sbRegKeyValue = sbGetValue.Substring(sbGetValue.LastIndexOf(@"\", StringComparison.Ordinal) + 1);

            //read profile xml and extract profile_name value
            var fullXmlFilePath = sbDeviceIdPath + @"\" + sbRegKeyValue;
            _sbControlActiveProfile = ConfigManager.XmlRead(fullXmlFilePath);
        }

        private void SetAppTheme()
        {
            if (_appTheme != null)
            {
                ThemeManager.Current.ChangeTheme(this, _appTheme);
            }
        }

        //set if gpu monitoring is enabled and show if true
        private void IsMonitorEnabled()
        {
            switch (_isMonitoringEnabled)
            {
                case "0":
                    HideMonitoring();
                    CheckboxEnableMonitor.IsOn = false;
                    _thisPc.Close();
                    break;
                case "1":
                    ShowMonitoring();
                    CheckboxEnableMonitor.IsOn = true;
                    StartOhMMonitor();
                    break;
            }
        }

        //show which Audio profile is active
        private void ShowActiveAudioProfile()
        {
            var sbAp = " " + _sbControlActiveProfile + " ";
            var sbBadge = AudioProfileBadge.Badge.ToString();

            if (sbBadge != sbAp)
            {
                AudioProfileBadge.Badge = " " + _sbControlActiveProfile + " ";
            }
        }

        //determine which profile is active by checking if power.limit is greater than default_power.limit
        private void ShowActiveGpuProfile()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var nvSmiPath = programFiles + @"\NVIDIA Corporation\NVSMI";
            const string nvSmiQuery = "nvidia-smi -i 0 --query-gpu=power.limit,power.default_limit --format=csv,noheader";
            var fullCmdArgs = @"/c cd " + nvSmiPath + " & " + nvSmiQuery;

            var showProfileProcess = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = fullCmdArgs,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            showProfileProcess.Start();

            _activeProfile = showProfileProcess.StandardOutput.ReadToEnd();

            var splitData = _activeProfile.Split(',');
            var activePowerLimit = Regex.Replace(splitData[0], "[^0-9]", "");
            var defaultPowerLimit = Regex.Replace(splitData[1], "[^0-9]", "");
            var activePLvalue = int.Parse(activePowerLimit);
            var defaultPLvalue = int.Parse(defaultPowerLimit);

            if (activePLvalue > defaultPLvalue)
            {
                OcProfileBadge.Badge = " Enabled ";
                StockProfileBadge.Badge = "";
            }
            else
            {
                StockProfileBadge.Badge = " Enabled ";
                OcProfileBadge.Badge = "";
            }

            showProfileProcess.WaitForExit();
        }

        private void SetupComboListSources()
        {
            //define List sources
            AppHotKey.ItemsSource = ListManager.HotKey;
            DefaultProfileList.ItemsSource = ListManager.HotKeyMsiAb;
            OverclockProfileList.ItemsSource = ListManager.HotKeyMsiAb;
            SoundSwitchHotKeyModifier.ItemsSource = ListManager.HotKeyModifier;
            SoundSwitchHotKey.ItemsSource = ListManager.HotKey;
            //show relative list indexes
            SetComboListIndexes();
        }

        private void SetComboListIndexes()
        {
            var hotKeyApp = ListManager.HotKey.FindIndex(a => a.Contains(_appHotKey));
            AppHotKey.SelectedIndex = hotKeyApp;
            var hotKeyMsiAbiIndex = ListManager.HotKeyMsiAb.FindIndex(a => a.Contains(_stockProfile));
            DefaultProfileList.SelectedIndex = hotKeyMsiAbiIndex;
            var hotKeyMsiAbIndex = ListManager.HotKeyMsiAb.FindIndex(a => a.Contains(_ocProfile));
            OverclockProfileList.SelectedIndex = hotKeyMsiAbIndex;
            var hotKeyModifierSoundSwitch = ListManager.HotKeyModifier.FindIndex(a => a.Contains(_soundSwitchHotKeyModifier));
            SoundSwitchHotKeyModifier.SelectedIndex = hotKeyModifierSoundSwitch;
            var hotKeySoundSwitch = ListManager.HotKey.FindIndex(a => a.Contains(_soundSwitchHotKey));
            SoundSwitchHotKey.SelectedIndex = hotKeySoundSwitch;
        }

        private void StartOhMMonitor()
        {
            var curCoreClock = "";
            var curMemClock = "";
            var curGpuLoad = "";
            var curMemLoad = "";
            var curGpuTemp = "";
            var curGpuPower = "";
            int roundValue;

            Task.Factory.StartNew(async () =>
            {
                _thisPc.Open();

                while (true)
                {
                    foreach (var hardware in _thisPc.Hardware)
                    {
                        hardware.Update();

                        if (hardware.HardwareType != HardwareType.GpuNvidia) continue;
                        foreach (var sensor in hardware.Sensors)
                        {
                            switch (sensor.SensorType)
                            {
                                case SensorType.Clock:
                                    {
                                        if (sensor.Name.Contains("Core"))
                                        {
                                            if (sensor.Value.Value >= 0)
                                            {
                                                roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                curCoreClock = roundValue + " Mhz";
                                            }
                                            else
                                            {
                                                curCoreClock = " -no data- ";
                                            }
                                        }

                                        if (sensor.Name.Contains("Memory"))
                                        {
                                            if (sensor.Value.Value >= 0)
                                            {
                                                roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                curMemClock = roundValue + " Mhz";
                                            }
                                            else
                                            {
                                                curMemClock = " -no data- ";
                                            }
                                        }

                                        break;
                                    }
                                case SensorType.Temperature:
                                    {
                                        if (sensor.Name.Contains("Core"))
                                        {
                                            if (sensor.Value.Value >= 0)
                                            {
                                                curGpuTemp = sensor.Value.GetValueOrDefault().ToString(CultureInfo.CurrentCulture) + " °C";
                                            }
                                            else
                                            {
                                                curGpuTemp = " -no data- ";
                                            }
                                        }

                                        break;
                                    }
                                case SensorType.Load:
                                    {
                                        if (sensor.Name.Contains("Core"))
                                        {
                                            if (sensor.Value.Value >= 0)
                                            {
                                                curGpuLoad = sensor.Value.GetValueOrDefault().ToString(CultureInfo.CurrentCulture) + " %";
                                            }
                                            else
                                            {
                                                curGpuLoad = " -no data- ";
                                            }
                                        }

                                        if (sensor.Name.Contains("Memory Controller"))
                                        {
                                            if (sensor.Value.Value >= 0)
                                            {
                                                curMemLoad = sensor.Value.GetValueOrDefault().ToString(CultureInfo.CurrentCulture) + " %";
                                            }
                                            else
                                            {
                                                curMemLoad = " -no data- ";
                                            }
                                        }

                                        break;
                                    }
                                case SensorType.Power:
                                    {
                                        if (sensor.Name.Contains("GPU Package"))
                                        {
                                            if (sensor.Value.Value >= 0)
                                            {
                                                roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                curGpuPower = roundValue + " W";
                                            }
                                            else
                                            {
                                                curGpuPower = " -no data- ";
                                            }
                                        }

                                        break;
                                    }
                                case SensorType.Voltage:
                                    break;
                                case SensorType.Frequency:
                                    break;
                                case SensorType.Fan:
                                    break;
                                case SensorType.Flow:
                                    break;
                                case SensorType.Control:
                                    break;
                                case SensorType.Level:
                                    break;
                                case SensorType.Factor:
                                    break;
                                case SensorType.Data:
                                    break;
                                case SensorType.SmallData:
                                    break;
                                case SensorType.Throughput:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        var clock = curCoreClock;
                        var memClock = curMemClock;
                        var load = curGpuLoad;
                        var memLoad = curMemLoad;
                        var temp = curGpuTemp;
                        var power = curGpuPower;

                        Dispatcher.Invoke(() =>
                        {
                            //set corresponding Tile Elements with GPU data values
                            CoreSpeed.Text = clock;
                            MemSpeed.Text = memClock;
                            CoreLoad.Text = load;
                            MemLoad.Text = memLoad;
                            CoreTemp.Text = temp;
                            TotalPower.Text = power;
                        });
                    }
                    await Task.Delay(DelayN);
                }
            });
        }

        // Terminate a running app by process name
        private static void TerminateApp(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill();
            }
        }

        // Set Stock GPU profile
        private void GpuStockTile_OnClick(object sender, RoutedEventArgs e)
        {
            ApplyProfile("SetStock");
        }

        // Set Overclock GPU profile
        private void GpuOcTile_OnClick(object sender, RoutedEventArgs e)
        {
            ApplyProfile("SetOC");
        }

        //Apply selected profile
        private void ApplyProfile(string profile)
        {
            string args = null;

            switch (profile)
            {
                case "SetStock":
                    args = "/Profile" + _stockProfile;
                    break;
                case "SetOC":
                    args = "/Profile" + _ocProfile;
                    break;
            }

            //start MSIAfterburner using appropriate /profile switch
            Process.Start(_msiabFile, args);

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(DelayL);

                //after setting the profile terminate MSIAfterburner process
                TerminateApp("MSIAfterburner");

                Dispatcher.Invoke(ShowActiveGpuProfile);
            });
        }

        // Open SoundBlaster Control Panel
        private void AudioTile_OnClick(object sender, RoutedEventArgs e)
        {
            var sbControlProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _sbControlFile
                }
            };
            sbControlProcess.Start();
        }

        //open MSIAfterburner application
        private void MsIabTile_OnClick(object sender, RoutedEventArgs e)
        {
            var msiabProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _msiabFile
                }
            };
            msiabProcess.Start();
        }

        // use SendKeys to switch current audio output device using SoundSwitch
        private void SoundSwitchTile_OnClick(object sender, RoutedEventArgs e)
        {
            var keyPressModifier = _soundSwitchHotKeyModifier;
            var keyPress = _soundSwitchHotKey;
            KeyManager.HotKeySender(keyPressModifier, keyPress);
        }

        private void ToggleSwitch_EnableMonitor(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToggleSwitch toggleSwitch)) return;
            switch (toggleSwitch.IsOn)
            {
                case true:
                    ConfigManager.IniWrite("IsMonitoringEnabled", "1");
                    ShowMonitoring();
                    StartOhMMonitor();
                    break;
                default:
                    ConfigManager.IniWrite("IsMonitoringEnabled", "0");
                    _thisPc.Close();
                    HideMonitoring();
                    break;
            }
        }

        private static void ShowMonitoring()
        {
            MonitoringDecision(ListManager.ControlsTiles, ListManager.ControlsTextBlocks, "Enable");
        }

        private static void HideMonitoring()
        {
            MonitoringDecision(ListManager.ControlsTiles, ListManager.ControlsTextBlocks, "Disable");
        }

        private static void MonitoringDecision(IEnumerable<Tile> controlsTiles, IEnumerable<TextBlock> controlsTextBlocks, string decision)
        {
            if (controlsTiles == null) throw new ArgumentNullException(nameof(controlsTiles));
            if (controlsTextBlocks == null) throw new ArgumentNullException(nameof(controlsTextBlocks));

            foreach (var monitorTiles in controlsTiles)
            {
                switch (decision)
                {
                    case "Enable":
                        monitorTiles.IsEnabled = true;
                        break;
                    case "Disable":
                        monitorTiles.IsEnabled = false;
                        break;
                }
            }

            foreach (var monitorTextBlocks in controlsTextBlocks)
            {
                switch (decision)
                {
                    case "Enable":
                        monitorTextBlocks.Visibility = Visibility.Visible;
                        break;
                    case "Disable":
                        monitorTextBlocks.Text = null;
                        monitorTextBlocks.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }

        private void Settings(object sender, RoutedEventArgs e)
        {
            switch (SettingsDialog.Visibility)
            {
                case Visibility.Visible:
                    ReadSettings();
                    CheckboxEnableMonitor.IsEnabled = true;
                    foreach (var mainTiles in ListManager.ControlsMainTiles)
                    {
                        mainTiles.IsEnabled = true;

                    }
                    SettingsDialog.Visibility = Visibility.Collapsed;
                    SettingsButton.Content = "Settings ";
                    break;
                case Visibility.Collapsed:
                    ReadSettings();
                    SetComboListIndexes();
                    SettingsButton.Content = "< " + SettingsButton.Content;
                    CheckboxEnableMonitor.IsEnabled = false;
                    foreach (var mainTiles in ListManager.ControlsMainTiles)
                    {
                        mainTiles.IsEnabled = false;

                    }
                    SettingsDialog.Visibility = Visibility.Visible;
                    break;
                case Visibility.Hidden:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // check current theme and set RadioButton accordingly when SettingsDialog is opened
            var currentTheme = ThemeManager.Current.DetectTheme(this);
            if (currentTheme != null && currentTheme.Name == "Dark.Red")
            {
                RadioRedTheme.IsChecked = true;
            }
            if (currentTheme != null && currentTheme.Name == "Dark.Amber")
            {
                RadioAmberTheme.IsChecked = true;
            }
            if (currentTheme != null && currentTheme.Name == "Dark.Blue")
            {
                RadioBlueTheme.IsChecked = true;
            }
            if (currentTheme != null && currentTheme.Name == "Dark.Purple")
            {
                RadioPurpleTheme.IsChecked = true;
            }
            if (currentTheme != null && currentTheme.Name == "Dark.Green")
            {
                RadioGreenTheme.IsChecked = true;
            }
            if (currentTheme != null && currentTheme.Name == "Dark.Teal")
            {
                RadioTealTheme.IsChecked = true;
            }
            if (currentTheme != null && currentTheme.Name == "Dark.Steel")
            {
                RadioSteelTheme.IsChecked = true;
            }
        }

        private void RadioRedTheme_OnChecked(object sender, RoutedEventArgs e)
        {
            _appTheme = "Dark.Red";
            SetThemeSave(_appTheme);
        }

        private void RadioAmberTheme_OnChecked(object sender, RoutedEventArgs e)
        {
            _appTheme = "Dark.Amber";
            SetThemeSave(_appTheme);
        }

        private void RadioBlueTheme_OnChecked(object sender, RoutedEventArgs e)
        {
            _appTheme = "Dark.Blue";
            SetThemeSave(_appTheme);
        }

        private void RadioPurpleTheme_OnChecked(object sender, RoutedEventArgs e)
        {
            _appTheme = "Dark.Purple";
            SetThemeSave(_appTheme);
        }

        private void RadioGreenTheme_OnChecked(object sender, RoutedEventArgs e)
        {
            _appTheme = "Dark.Green";
            SetThemeSave(_appTheme);
        }

        private void RadioTealTheme_OnChecked(object sender, RoutedEventArgs e)
        {
            _appTheme = "Dark.Teal";
            SetThemeSave(_appTheme);
        }

        private void RadioSteelTheme_OnChecked(object sender, RoutedEventArgs e)
        {
            _appTheme = "Dark.Steel";
            SetThemeSave(_appTheme);
        }

        // set chosen theme and save to .ini file
        private void SetThemeSave(string themeName)
        {
            _appTheme = themeName;
            ThemeManager.Current.ChangeTheme(this, _appTheme);
            ConfigManager.IniWrite("AppTheme", _appTheme);
        }

        private void DefaultProfileList_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var defaultProfileListValue = DefaultProfileList.SelectedValue.ToString();
            ConfigManager.IniWrite("StockProfile", defaultProfileListValue);
        }

        private void OverclockProfileList_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var overclockProfileListValue = OverclockProfileList.SelectedValue.ToString();
            ConfigManager.IniWrite("OCProfile", overclockProfileListValue);
        }

        private void SoundSwitchHotKeyModifier_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var soundSwitchHotKeyModifierValue = SoundSwitchHotKeyModifier.SelectedValue.ToString();
            ConfigManager.IniWrite("SoundSwitchHotKeyModifier", soundSwitchHotKeyModifierValue);
        }

        private void SoundSwitchHotKey_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var soundSwitchHotKeyValue = SoundSwitchHotKey.SelectedValue.ToString();
            ConfigManager.IniWrite("SoundSwitchHotKey", soundSwitchHotKeyValue);
        }

        private void AppHotKey_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var appHotKeyValue = AppHotKey.SelectedValue.ToString();
            ConfigManager.IniWrite("AppHotKey", appHotKeyValue);
        }

        private void AiOHotKey(object sender, KeyEventArgs e)
        {
            var keyPress = e.Key.ToString();
            if (keyPress.Contains(_soundSwitchHotKeyModifier) || keyPress.Contains(_soundSwitchHotKey)) return;

            if (!keyPress.Contains(_appHotKey)) return;
            if (OcProfileBadge.Badge.ToString() == " Enabled ")
            {
                ApplyProfile("SetStock");
            }
            if (StockProfileBadge.Badge.ToString() == " Enabled ")
            {
                ApplyProfile("SetOC");
            }
            AudioTile_OnClick(sender, e);
        }

        //reflect updated settings in UI when window is re-focused
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            ReadSettings();
            ShowActiveAudioProfile();
            ShowActiveGpuProfile();
            SetComboListIndexes();
            // kill SoundBlaster Control Panel if running when app is refocused
            TerminateApp("SBAdgyFx");
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            //close OhM monitoring when exiting
            _thisPc.Close();
            Close();
        }
    }
}
