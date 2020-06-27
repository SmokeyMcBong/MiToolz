using ControlzEx.Theming;
using LibreHardwareMonitor.Hardware;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;

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
        private static string _activePowerPlan;
        private static string _powerPlanBalanced;
        private static string _powerPlanPerformance;
        private static string _sbControlFile;
        private static string _sbControlActiveProfile;
        private static string _msiabFile;
        private static string _isMonitoringEnabled;
        private static string _appTheme;
        private static string _gameModeHotKey;
        private static string _audioDeviceSwitchHotKey;
        private static string _exitAppHotKey;
        private static string _audioDevice1;
        private static string _audioDevice2;
        private static string _defaultAudioDevice;
        private string _gpuClock;
        private string _gpuMemClock;
        private string _gpuLoad;
        private string _gpuMemLoad;
        private string _gpuTemp;
        private string _gpuPower;
        private string _cpuClock;
        private string _cpuTemp;
        //private const int DelayShort = 500;
        private const int DelayLong = 1000;

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
            ShowActivePowerPlan();
            ShowDefaultAudioDevice();
            SetupComboListSources();

            //add MainWindow event handlers
            Closed += MainWindow_Closed;
            Activated += MainWindow_Activated;

            //start OhM Monitoring
            _thisPc = new Computer
            {
                IsGpuEnabled = true,
                IsCpuEnabled = true
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
            var powerPlanBalanced = Properties.Resources.PowerPlanBalanced;
            var powerPlanPerformance = Properties.Resources.PowerPlanPerformance;
            var defaultMonitoringEnabled = Properties.Resources.DefaultMonitoringEnabled;
            var defaultAppTheme = Properties.Resources.DefaultAppTheme;
            var defaultGameModeHotKey = Properties.Resources.DefaultGameModeHotKey;
            var defaultAudioDeviceSwitchHotKey = Properties.Resources.DefaultAudioDeviceSwitchHotKey;
            var defaultExitAppHotKey = Properties.Resources.DefaultExitAppHotkey;
            var audioDevice1 = ListManager.AudioDevicesList.ElementAt(0);
            var audioDevice2 = ListManager.AudioDevicesList.ElementAt(1);
            var defaultAudioDevice = ListManager.DefaultAudioDevice;

            if (!File.Exists(myConfigManager))
            {
                ConfigManager.IniWrite("StockProfile", defaultStockProfile);
                ConfigManager.IniWrite("OCProfile", defaultOcProfile);
                ConfigManager.IniWrite("PowerPlanBalanced", powerPlanBalanced);
                ConfigManager.IniWrite("PowerPlanPerformance", powerPlanPerformance);
                ConfigManager.IniWrite("SBControl_File", sbControlFilePath);
                ConfigManager.IniWrite("MSIAB_File", msiabFilePath);
                ConfigManager.IniWrite("IsMonitoringEnabled", defaultMonitoringEnabled);
                ConfigManager.IniWrite("AppTheme", defaultAppTheme);
                ConfigManager.IniWrite("GameModeHotKey", defaultGameModeHotKey);
                ConfigManager.IniWrite("AudioDeviceSwitchHotKey", defaultAudioDeviceSwitchHotKey);
                ConfigManager.IniWrite("ExitAppHotKey", defaultExitAppHotKey);
                ConfigManager.IniWrite("AudioDeviceNo1", audioDevice1);
                ConfigManager.IniWrite("AudioDeviceNo2", audioDevice2);
                ConfigManager.IniWrite("DefaultAudioDevice", defaultAudioDevice);
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
            _powerPlanBalanced = ConfigManager.IniRead("PowerPlanBalanced");
            _powerPlanPerformance = ConfigManager.IniRead("PowerPlanPerformance");
            _sbControlFile = ConfigManager.IniRead("SBControl_File");
            _msiabFile = ConfigManager.IniRead("MSIAB_File");
            _isMonitoringEnabled = ConfigManager.IniRead("IsMonitoringEnabled");
            _appTheme = ConfigManager.IniRead("AppTheme");
            _gameModeHotKey = ConfigManager.IniRead("GameModeHotKey");
            _audioDeviceSwitchHotKey = ConfigManager.IniRead("AudioDeviceSwitchHotKey");
            _exitAppHotKey = ConfigManager.IniRead("ExitAppHotKey");
            _audioDevice1 = ConfigManager.IniRead("AudioDeviceNo1");
            _audioDevice2 = ConfigManager.IniRead("AudioDeviceNo2");
            _defaultAudioDevice = ConfigManager.IniRead("DefaultAudioDevice");

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
                GpuProfileBadge.Badge = " Overclock ";
            }
            else
            {
                GpuProfileBadge.Badge = " Default ";
            }

            showProfileProcess.WaitForExit();
        }

        //show which Audio profile is active
        private void ShowActiveAudioProfile()
        {
            var soundBlasterActiveProfile = " " + _sbControlActiveProfile + " ";
            var soundBlasterBadge = AudioProfileBadge.Badge.ToString();

            if (soundBlasterBadge != soundBlasterActiveProfile)
            {
                AudioProfileBadge.Badge = " " + _sbControlActiveProfile + " ";
            }
        }

        //show which Power Plan is active
        private void ShowActivePowerPlan()
        {
            // get active power plan
            var getActivePowerPlan = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "powercfg",
                    Arguments = "/GetActiveScheme",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            getActivePowerPlan.Start();

            var activePlan = getActivePowerPlan.StandardOutput.ReadToEnd();
            getActivePowerPlan.WaitForExit();

            if (activePlan.Contains(_powerPlanBalanced))
            {
                _activePowerPlan = " " + "Balanced" + " ";
            }
            if (activePlan.Contains(_powerPlanPerformance))
            {
                _activePowerPlan = " " + "Performance" + " ";
            }

            if (PowerPlanBadge.ToString() != _activePowerPlan)
            {
                PowerPlanBadge.Badge = _activePowerPlan;
            }
        }

        //show which Audio profile is active
        private void ShowDefaultAudioDevice()
        {
            var defaultAudioDevice = " " + _defaultAudioDevice + " ";
            var defaultAudioDeviceBadge = AudioDeviceBadge.Badge.ToString();

            if (defaultAudioDeviceBadge != defaultAudioDevice)
            {
                AudioDeviceBadge.Badge = " " + _defaultAudioDevice + " ";
            }
        }

        private void SetupComboListSources()
        {
            //define List sources
            GameModeHotKey.ItemsSource = ListManager.HotKey;
            DefaultProfileList.ItemsSource = ListManager.MsiAbProfileList;
            OverclockProfileList.ItemsSource = ListManager.MsiAbProfileList;
            AudioDevice1.ItemsSource = ListManager.AudioDevicesList;
            AudioDevice2.ItemsSource = ListManager.AudioDevicesList;
            AudioDeviceHotKey.ItemsSource = ListManager.HotKey;
            ExitAppHotKey.ItemsSource = ListManager.HotKey;

            //show relative list indexes
            SetComboListIndexes();
        }

        private void SetComboListIndexes()
        {
            var hotKeyGameMode = ListManager.HotKey.FindIndex(a => a.Contains(_gameModeHotKey));
            GameModeHotKey.SelectedIndex = hotKeyGameMode;
            var msiAbStockIndex = ListManager.MsiAbProfileList.FindIndex(a => a.Contains(_stockProfile));
            DefaultProfileList.SelectedIndex = msiAbStockIndex;
            var msiAbOcIndex = ListManager.MsiAbProfileList.FindIndex(a => a.Contains(_ocProfile));
            OverclockProfileList.SelectedIndex = msiAbOcIndex;
            var audioDeviceSwitcher1 = ListManager.AudioDevicesList.FindIndex(a => a.Contains(_audioDevice1));
            AudioDevice1.SelectedIndex = audioDeviceSwitcher1;
            var audioDeviceSwitcher2 = ListManager.AudioDevicesList.FindIndex(a => a.Contains(_audioDevice2));
            AudioDevice2.SelectedIndex = audioDeviceSwitcher2;
            var hotKeyAudioSwitcher = ListManager.HotKey.FindIndex(a => a.Contains(_audioDeviceSwitchHotKey));
            AudioDeviceHotKey.SelectedIndex = hotKeyAudioSwitcher;
            var hotKeyExitApp = ListManager.HotKey.FindIndex(a => a.Contains(_exitAppHotKey));
            ExitAppHotKey.SelectedIndex = hotKeyExitApp;
        }

        private void StartOhMMonitor()
        {
            int roundValue;

            Task.Factory.StartNew(async () =>
            {
                _thisPc.Open();

                while (true)
                {
                    foreach (var hardware in _thisPc.Hardware)
                    {
                        hardware.Update();

                        if (hardware.HardwareType == HardwareType.GpuNvidia)
                            foreach (var sensor in hardware.Sensors)
                            {
                                switch (sensor.SensorType)
                                {
                                    case SensorType.Clock:
                                        {
                                            if (sensor.Name.Equals("GPU Core"))
                                            {
                                                if (sensor.Value.Value >= 0)
                                                {
                                                    roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                    _gpuClock = roundValue + " Mhz";
                                                }
                                                else
                                                {
                                                    _gpuClock = " -no data- ";
                                                }
                                            }
                                            if (sensor.Name.Equals("GPU Memory"))
                                            {
                                                if (sensor.Value.Value >= 0)
                                                {
                                                    roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                    _gpuMemClock = roundValue + " Mhz";
                                                }
                                                else
                                                {
                                                    _gpuMemClock = " -no data- ";
                                                }
                                            }
                                            break;
                                        }
                                    case SensorType.Temperature:
                                        {
                                            if (sensor.Name.Equals("GPU Core"))
                                            {
                                                if (sensor.Value.Value >= 0)
                                                {
                                                    roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                    _gpuTemp = roundValue + " °C";
                                                }
                                                else
                                                {
                                                    _gpuTemp = " -no data- ";
                                                }
                                            }
                                            break;
                                        }
                                    case SensorType.Load:
                                        {
                                            if (sensor.Name.Equals("GPU Core"))
                                            {
                                                if (sensor.Value.Value >= 0)
                                                {
                                                    roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                    _gpuLoad = roundValue + " %";
                                                }
                                                else
                                                {
                                                    _gpuLoad = " -no data- ";
                                                }
                                            }
                                            if (sensor.Name.Equals("GPU Memory"))
                                            {
                                                if (sensor.Value.Value >= 0)
                                                {
                                                    roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                    _gpuMemLoad = roundValue + " %";
                                                }
                                                else
                                                {
                                                    _gpuMemLoad = " -no data- ";
                                                }
                                            }
                                            break;
                                        }
                                    case SensorType.Power:
                                        {
                                            if (sensor.Name.Equals("GPU Package"))
                                            {
                                                if (sensor.Value.Value >= 0)
                                                {
                                                    roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                    _gpuPower = roundValue + " W";
                                                }
                                                else
                                                {
                                                    _gpuPower = " -no data- ";
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

                        else if (hardware.HardwareType == HardwareType.Cpu)
                            foreach (var sensor in hardware.Sensors)
                            {
                                switch (sensor.SensorType)
                                {
                                    case SensorType.Clock:
                                        {
                                            if (sensor.Name.Equals("CPU Core #1"))
                                            {
                                                if (sensor.Value.Value >= 0)
                                                {
                                                    roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                    _cpuClock = roundValue + " Mhz";
                                                }
                                                else
                                                {
                                                    _cpuClock = " -no data- ";
                                                }
                                            }
                                            break;
                                        }
                                    case SensorType.Temperature:
                                        {
                                            if (sensor.Name.Equals("CPU Core #1"))
                                            {
                                                if (sensor.Value.Value >= 0)
                                                {
                                                    roundValue = (int)Math.Round(sensor.Value.GetValueOrDefault());
                                                    _cpuTemp = roundValue + " °C";
                                                }
                                                else
                                                {
                                                    _cpuTemp = " -no data- ";
                                                }
                                            }
                                            break;
                                        }
                                    case SensorType.Voltage:
                                        break;
                                    case SensorType.Load:
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
                                    case SensorType.Power:
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
                    }
                    await Task.Delay(DelayLong);

                    Dispatcher.Invoke(() =>
                    {
                        //set corresponding Tile Elements with GPU data values
                        CoreSpeed.Text = _gpuClock;
                        MemSpeed.Text = _gpuMemClock;
                        CoreLoad.Text = _gpuLoad;
                        MemLoad.Text = _gpuMemLoad;
                        CoreTemp.Text = _gpuTemp;
                        TotalPower.Text = _gpuPower;
                        CpuSpeed.Text = _cpuClock;
                        CpuTemp.Text = _cpuTemp;
                    });
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
        private void GpuTile_OnClick(object sender, RoutedEventArgs e)
        {
            var badgeString = GpuProfileBadge.Badge.ToString();

            switch (badgeString)
            {
                case " Overclock ":
                    ApplyProfile("SetStock");
                    break;
                case " Default ":
                    ApplyProfile("SetOC");
                    break;
            }
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
                await Task.Delay(DelayLong);

                //after setting the profile terminate MSIAfterburner process
                TerminateApp("MSIAfterburner");

                Dispatcher.Invoke(ShowActiveGpuProfile);
            });
        }

        //open SoundBlaster Control Panel
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

        //apply Power Plan Scheme
        private void PowerPlanTile_OnCLick(object sender, RoutedEventArgs e)
        {
            var badgeString = PowerPlanBadge.Badge.ToString();

            switch (badgeString)
            {
                case " Balanced ":
                    ApplyPowerPlan(_powerPlanPerformance);
                    break;
                case " Performance ":
                    ApplyPowerPlan(_powerPlanBalanced);
                    break;
            }
        }

        //Apply selected profile
        private static void ApplyPowerPlan(string powerPlan)
        {
            // set active power plan from Powercfg
            var setActivePowerPlan = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = "powercfg",
                    Arguments = "/s " + powerPlan
                }
            };
            setActivePowerPlan.Start();
        }


        //open MSIAfterburner application
        private void MsIabTile_OnClick(object sender, RoutedEventArgs e)
        {
            var msiabProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = _msiabFile
                }
            };
            msiabProcess.Start();
        }

        //switch default audio device
        private void AudioDeviceSwitchTile_OnClick(object sender, RoutedEventArgs e)
        {
            var device = "";
            if (_defaultAudioDevice == _audioDevice1)
            {
                device = _audioDevice2;
                ConfigManager.IniWrite("DefaultAudioDevice", _audioDevice2);
            }
            if (_defaultAudioDevice == _audioDevice2)
            {
                device = _audioDevice1;
                ConfigManager.IniWrite("DefaultAudioDevice", _audioDevice1);
            }

            // set default audio output device
            var setDefaultAudioDevice = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = "nircmdc.exe",
                    Arguments = " setdefaultsounddevice " + "\"" + device + "\""
                }
            };
            setDefaultAudioDevice.Start();
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
                    CpuMonitorLabel.Visibility = Visibility.Visible;
                    GpuMonitorLabel.Visibility = Visibility.Visible;
                    LabelSeparator1.Visibility = Visibility.Visible;
                    LabelSeparator2.Visibility = Visibility.Visible;
                    LabelSeparator3.Visibility = Visibility.Visible;
                    LabelSeparator4.Visibility = Visibility.Visible;
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

                    CpuMonitorLabel.Visibility = Visibility.Collapsed;
                    GpuMonitorLabel.Visibility = Visibility.Collapsed;
                    LabelSeparator1.Visibility = Visibility.Collapsed;
                    LabelSeparator2.Visibility = Visibility.Collapsed;
                    LabelSeparator3.Visibility = Visibility.Collapsed;
                    LabelSeparator4.Visibility = Visibility.Collapsed;
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

        private void AudioDevice1_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var audioDeviceSwitch1 = AudioDevice1.SelectedValue.ToString();
            ConfigManager.IniWrite("AudioDeviceNo1", audioDeviceSwitch1);
        }

        private void AudioDevice2_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var audioDeviceSwitch2 = AudioDevice2.SelectedValue.ToString();
            ConfigManager.IniWrite("AudioDeviceNo2", audioDeviceSwitch2);
        }

        private void GameModeHotKey_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var gameModeHotKeyValue = GameModeHotKey.SelectedValue.ToString();
            if (gameModeHotKeyValue != _audioDeviceSwitchHotKey && gameModeHotKeyValue != _exitAppHotKey)
            {
                ConfigManager.IniWrite("GameModeHotKey", gameModeHotKeyValue);
            }
            else
            {
                var metroWindow = (Application.Current.MainWindow as MetroWindow);
                metroWindow.ShowMessageAsync("HotKey already assigned...", "Choose another HotKey for this task");
            }
        }

        private void AudioDeviceHotKey_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var audioDeviceHotKeyValue = AudioDeviceHotKey.SelectedValue.ToString();

            if (audioDeviceHotKeyValue != _gameModeHotKey && audioDeviceHotKeyValue != _exitAppHotKey)
            {
                ConfigManager.IniWrite("AudioDeviceSwitchHotKey", audioDeviceHotKeyValue);
            }
            else
            {
                var metroWindow = (Application.Current.MainWindow as MetroWindow);
                metroWindow.ShowMessageAsync("HotKey already assigned...", "Choose another HotKey for this task");
            }
        }

        private void ExitAppHotKey_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var exitAppHotKeyValue = ExitAppHotKey.SelectedValue.ToString();

            if (exitAppHotKeyValue != _gameModeHotKey && exitAppHotKeyValue != _audioDeviceSwitchHotKey)
            {
                ConfigManager.IniWrite("ExitAppHotKey", exitAppHotKeyValue);
            }
            else
            {
                var metroWindow = (Application.Current.MainWindow as MetroWindow);
                metroWindow.ShowMessageAsync("HotKey already assigned...", "Choose another HotKey for this task");
            }
        }

        private void HotKeyManager(object sender, KeyEventArgs e)
        {
            var keyPress = e.Key.ToString();

            if (keyPress.Equals(_gameModeHotKey))
            {
                GpuTile_OnClick(sender, e);
                PowerPlanTile_OnCLick(sender, e);
            }
            if (keyPress.Equals(_audioDeviceSwitchHotKey))
            {
                AudioDeviceSwitchTile_OnClick(sender, e);
            }
            if (keyPress.Equals(_exitAppHotKey))
            {
                MainWindow_Closed(sender, e);
            }
        }

        //reflect updated settings in UI when window is re-focused
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            ReadSettings();
            ShowActiveAudioProfile();
            ShowActiveGpuProfile();
            ShowActivePowerPlan();
            ShowDefaultAudioDevice();
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
