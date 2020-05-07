using OpenHardwareMonitor.Hardware;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiToolz
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Computer ThisPC;
        static string ActiveProfile;
        private static string StockProfile;
        private static string OCProfile;
        private static string SBControl_File;
        static string SBControl_ActiveProfile;
        private static string MSIAB_File;
        private static string IsMonitoringEnabled;
        private static bool NeedRestart = false;
        private static readonly ConfigManager ConfigManager = new ConfigManager();
        private static readonly Brush IndicatorReady = Brushes.Green;
        private static readonly Brush IndicatorBusy = Brushes.Orange;
        private static readonly int DelayS = 250;
        private static readonly int DelayN = 1000;

        public MainWindow()
        {
            InitializeComponent();

            //run startup checks, read all config settings and show on UI elements
            StartupSetup();
            SetComboLists();
            ShowActiveSoundProfile();
            ShowActiveMSIabProfile();
            Indicator.Background = IndicatorReady;

            //add MainWindow close event handler
            Closed += new EventHandler(MainWindow_Closed);

            //initialize OhM monitoring for GPU Core & Mem clocks
            ThisPC = new Computer()
            {
                GPUEnabled = true
            };

            CheckStartMonitor();
        }

        //app startup checks
        private void StartupSetup()
        {
            //check for duplicate instances
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1)
            {
                Process.GetCurrentProcess().Kill();
            }

            //check for ini file, if not found then create new file and write default values to it
            string MyConfigManager = Properties.Resources.MyConfigManager;
            string SBControl_FilePath = Properties.Resources.SBControl_FilePath;
            string MSIAB_FilePath = Properties.Resources.MSIAB_FilePath;
            string DefaultStockProfile = Properties.Resources.DefaultStockProfile;
            string DefaultOCProfile = Properties.Resources.DefaultOCProfile;
            IsMonitoringEnabled = "1";

            if (!File.Exists(MyConfigManager))
            {
                ConfigManager.IniWrite("StockProfile", DefaultStockProfile);
                ConfigManager.IniWrite("OCProfile", DefaultOCProfile);
                ConfigManager.IniWrite("SBControl_File", SBControl_FilePath);
                ConfigManager.IniWrite("MSIAB_File", MSIAB_FilePath);
                ConfigManager.IniWrite("IsMonitoringEnabled", IsMonitoringEnabled);
                ReadSettings();
            }
            else
            {
                ReadSettings();
            }
        }

        //read in values from ini file
        private void ReadSettings()
        {
            StockProfile = ConfigManager.IniRead("StockProfile");
            OCProfile = ConfigManager.IniRead("OCProfile");
            SBControl_File = ConfigManager.IniRead("SBControl_File");
            MSIAB_File = ConfigManager.IniRead("MSIAB_File");
            IsMonitoringEnabled = ConfigManager.IniRead("IsMonitoringEnabled");

            string SBControl_ProfileFilePath = Properties.Resources.SBControl_ProfileFilePath;
            string SBControl_ProfileRegPath = Properties.Resources.SBControl_ProfileRegPath;

            //full path to profile folder
            string WinUname = Environment.UserName;
            string SBPAth = @"C:\Users\" + WinUname + @"\" + SBControl_ProfileFilePath;

            //get folder name of subfolder (HDAUDIO_VEN_10EC_DEV_0899_SUBSYS_11020041 etc)
            string[] SBGetIDDir = Directory.GetDirectories(SBPAth, "HDAUDIO*", SearchOption.TopDirectoryOnly);
            string SBDeviceIDPath = string.Join("", SBGetIDDir);
            string SBDeviceID = SBDeviceIDPath.Substring(SBDeviceIDPath.LastIndexOf(@"\") + 1);

            //get registry key value for active profile
            string SBRegKeyName = SBControl_ProfileRegPath + SBDeviceID;
            string SBGetValue = ConfigManager.RegReadKeyValue(SBRegKeyName, "Profile");
            string SBRegKeyValue = SBGetValue.Substring(SBGetValue.LastIndexOf(@"\") + 1);

            //read profile xml and extract profile_name value
            string FullXmlFilePath = SBDeviceIDPath + @"\" + SBRegKeyValue;
            SBControl_ActiveProfile = ConfigManager.XmlRead(FullXmlFilePath);
        }

        //set if frequency monitoring is enabled and show if true
        private void CheckStartMonitor()
        {
            if (IsMonitoringEnabled == "0")
            {
                Checkbox_EnableMonitor.IsChecked = false;
            }
            if (IsMonitoringEnabled == "1")
            {
                Checkbox_EnableMonitor.IsChecked = true;
                StartOhMMonitor();
            }
        }

        // Populate both ComboBox's
        private void SetComboLists()
        {
            Combo_Stock.SelectedIndex = int.Parse(StockProfile) - 1;
            Combo_OC.SelectedIndex = int.Parse(OCProfile) - 1;
        }

        //show which Audio profile is active
        private void ShowActiveSoundProfile()
        {
            TextBlock_Sound.Text = SBControl_ActiveProfile;
        }

        //determin which profile is active by checking if power.limit is greater than defaul_power.limit
        private void ShowActiveMSIabProfile()
        {
            Process ShowProfile_Process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = @"/c cd C:\Program Files\NVIDIA Corporation\NVSMI & nvidia-smi -i 0 --query-gpu=power.limit,power.default_limit --format=csv,noheader",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            ShowProfile_Process.Start();

            ActiveProfile = ShowProfile_Process.StandardOutput.ReadToEnd();

            string[] SplitData = ActiveProfile.Split(',');
            string ActivePowerLimit = Regex.Replace(SplitData[0], "[^0-9]", "");
            string DefaultPowerLimit = Regex.Replace(SplitData[1], "[^0-9]", "");
            int ActivePLvalue = int.Parse(ActivePowerLimit);
            int DefaultPLvalue = int.Parse(DefaultPowerLimit);

            if (ActivePLvalue > DefaultPLvalue)
            {
                TextBlock_OC.Foreground = Brushes.White;
                TextBlock_Stock.Foreground = Brushes.Black;
            }
            else
            {
                TextBlock_OC.Foreground = Brushes.Black;
                TextBlock_Stock.Foreground = Brushes.White;
            }

            ShowProfile_Process.WaitForExit();
        }

        //start OhM GPU clock monitoring
        private void StartOhMMonitor()
        {
            string CurCoreClock = "";
            string CurMemClock = "";
            int ClockMHz;

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(DelayS);

                while (true)
                {
                    ThisPC.Open();

                    foreach (IHardware hardware in ThisPC.Hardware)
                    {
                        hardware.Update();

                        if (hardware.HardwareType == HardwareType.GpuNvidia)
                        {
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Clock)
                                {
                                    if (sensor.Name.Contains("Core"))
                                    {
                                        if (sensor.Value.Value > 0)
                                        {
                                            ClockMHz = (int)Math.Round(sensor.Value.Value);
                                            CurCoreClock = ClockMHz.ToString() + " Mhz";
                                        }
                                        else
                                        {
                                            CurCoreClock = " -no data- ";
                                        }
                                    }

                                    if (sensor.Name.Contains("Memory"))
                                    {
                                        if (sensor.Value.Value > 0)
                                        {
                                            ClockMHz = (int)Math.Round(sensor.Value.Value);
                                            CurMemClock = ClockMHz.ToString() + " Mhz";
                                        }
                                        else
                                        {
                                            CurMemClock = " -no data- ";
                                        }
                                    }
                                }
                            }
                        }
                    }

                    ThisPC.Close();

                    //update UI elements to show clock frequencies
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        //set coresponding text to reflect Core(CurCoreClock) & Memory(CurMemClock) clock speeds
                        ShowCoreMhz.Text = CurCoreClock;
                        ShowMemMhz.Text = CurMemClock;
                    });
                }
            });
        }

        // Close MSIAfterburner
        private void KillMSIAB()
        {
            foreach (var MSIAB_Process in Process.GetProcessesByName("MSIAfterburner"))
            {
                MSIAB_Process.Kill();
            }
        }

        // Set Stock GPU profile
        private void Button_SetStock_Click(object sender, RoutedEventArgs e)
        {
            ApplyProfile("SetStock");
        }

        // Set Overclock GPU profile
        private void Button_SetOC_Click(object sender, RoutedEventArgs e)
        {
            ApplyProfile("SetOC");
        }

        //Apply selected profile
        private void ApplyProfile(string Profile)
        {
            string Args = null;
            Indicator.Background = IndicatorBusy;

            if (Profile == "SetStock")
            {
                Args = "/Profile" + StockProfile;
            }

            if (Profile == "SetOC")
            {
                Args = "/Profile" + OCProfile;
            }

            //start MSIAfterburner using appropriate /profile switch
            Process.Start(MSIAB_File, Args);

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(DelayN);

                //after setting the profile terminate MSIAfterburner process
                KillMSIAB();

                // it only works in WPF
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //change indicator elements on the UI thread.                   
                    Indicator.Background = IndicatorReady;
                    ShowActiveMSIabProfile();
                });
            });
        }

        // Open SoundBlaster Control Panel
        private void Button_OpenSB_Click(object sender, RoutedEventArgs e)
        {
            Indicator.Background = IndicatorBusy;

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(DelayN);

                // it only works in WPF
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Do something on the UI thread.
                    Indicator.Background = IndicatorReady;
                });
            });

            var SBControl_Process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = SBControl_File
                }
            };
            SBControl_Process.Start();
        }

        //show/hide settings portion
        private void Button_Expand_Click(object sender, RoutedEventArgs e)
        {
            ReadSettings();
            SetComboLists();

            if (IsMonitoringEnabled == "0")
            {
                Checkbox_EnableMonitor.IsChecked = false;
            }
            if (IsMonitoringEnabled == "1")
            {
                Checkbox_EnableMonitor.IsChecked = true;
            }

            double GetWindowWidth = Application.Current.MainWindow.Width;
            double StandardWindowWidth = 394;
            double ExtendedWindowWidth = 608;
            string ImageResourcePath = "pack://siteoforigin:,,,/Resources/";

            if (GetWindowWidth == StandardWindowWidth)
            {
                Button_Expand.Background = new ImageBrush(new BitmapImage(new Uri(ImageResourcePath + "Image_MenuRetract.png")));
                Application.Current.MainWindow.Width = ExtendedWindowWidth;
            }
            else
            {
                Button_Expand.Background = new ImageBrush(new BitmapImage(new Uri(ImageResourcePath + "Image_MenuExpand.png")));
                Application.Current.MainWindow.Width = StandardWindowWidth;
            }
        }

        //save profile values to ini file
        private void Button_SaveProfiles_Click(object sender, RoutedEventArgs e)
        {
            Indicator.Background = IndicatorBusy;

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(DelayS);

                // it only works in WPF
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Do something on the UI thread.
                    Indicator.Background = IndicatorReady;
                });
            });

            var GetComboStockValue = Combo_Stock.SelectedIndex + 1;
            var GetComboOCValue = Combo_OC.SelectedIndex + 1;
            ConfigManager.IniWrite("StockProfile", GetComboStockValue.ToString());
            ConfigManager.IniWrite("OCProfile", GetComboOCValue.ToString());

            if (Checkbox_EnableMonitor.IsChecked == false)
            {
                ConfigManager.IniWrite("IsMonitoringEnabled", "0");
            }
            if (Checkbox_EnableMonitor.IsChecked == true)
            {
                ConfigManager.IniWrite("IsMonitoringEnabled", "1");
            }

            if (NeedRestart == false)
            {
                ReadSettings();
            }
            if (NeedRestart == true)
            {
                //restart app to apply frequency monitor setting
                Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        //open MSIAfterburner application
        private void Button_OpenMSIAB_Click(object sender, RoutedEventArgs e)
        {
            Indicator.Background = IndicatorBusy;

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(DelayN);

                // it only works in WPF
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Do something on the UI thread.
                    Indicator.Background = IndicatorReady;
                });
            });

            var MSIAB_Process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = MSIAB_File
                }
            };
            MSIAB_Process.Start();
        }

        //set restart app flag when checkbox has been clicked/changed
        private void Checkbox_EnableMonitor_Clicked(object sender, RoutedEventArgs e)
        {
            NeedRestart = true;
        }

        //MainWindow close application event handler
        void MainWindow_Closed(object sender, EventArgs e)
        {
            //close OhM monitoring when exiting
            ThisPC.Close();
            Close();
        }

        //reflect updated settings in UI when window is re-focused
        private void Window_Activated(object sender, EventArgs e)
        {
            ReadSettings();
            SetComboLists();
            ShowActiveSoundProfile();
        }
    }
}
