using Newtonsoft.Json;
using Pinger.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace Pinger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowDC DC => DataContext as MainWindowDC;

        public MainWindow()
        {
            InitializeComponent();

            Thumb.DragDelta += (s, e) =>
            {
                this.Left += e.HorizontalChange;
                this.Top += e.VerticalChange;
            };
        }

        private void Button_NewMachineWindow(object sender, RoutedEventArgs e)
        {
            DC.CanApplyNewMachine = false;
            DC.NewMachineName = "";
            if (DC.AllMachines?.Count > 0)
            {
                DC.NewMachineIP = String.Join('.', DC.AllMachines.First().IP.ToString().Split('.').Take(3)) + '.';
            }
            else
            {
                DC.NewMachineIP = "192.168.";
            }

            var nmw = new NewMachineWindow();
            nmw.DataContext = DataContext;
            nmw.ShowDialog();
        }

        private void Grid_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
            ((MachineInfo)((Grid)sender).DataContext).DeleteMachine();

        private void Button_Click(object sender, RoutedEventArgs e) => 
            App.Current.Shutdown();

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e) =>
            this.Height = 30 + e.NewSize.Height;
    }

    public class MainWindowDC : Notified
    {
        public static MainWindowDC Curr;
        public ObservableCollection<MachineInfo> AllMachines { get; set; }
        public List<IPAddress> IPaddreses = new List<IPAddress>();

        public string NewMachineName { get; set; }
        public string NewMachineIP { get; set; }
        public bool CanApplyNewMachine { get; set; }

        public MainWindowDC()
        {
            Curr = this;

            var lst = JsonConvert.DeserializeObject<List<MachineInfo>>(Settings.Default.Machines);
            App.Current.Dispatcher.Invoke(() =>
            {
                AllMachines = new ObservableCollection<MachineInfo>();
                foreach (var m in lst)
                    AllMachines.Add(m);
            });
            ScanMachines();
        }

        private bool scanning = false;
        public void ScanMachines()
        {
            if (AllMachines?.Count > 0 && !scanning)
            {
                scanning = true;
                Task.Run(async () =>
                {
                    while (AllMachines.Count > 0)
                    {
                        var alm = AllMachines.ToList();
                        var span = 2000 / alm.Count;   ///////////////////////////
                        foreach (var m in alm)
                        {
                            m.Ping();
                            await Task.Delay(span);
                        }
                    }
                    scanning = false;
                });
            }
        }
    }

    public class MachineInfo : Notified
    {
        public static void NewMachine(string Name, IPAddress IP)
        {
            if (MainWindowDC.Curr.AllMachines == null)
            {
                MainWindowDC.Curr.AllMachines = new ObservableCollection<MachineInfo>();
            }
            var Allm = MainWindowDC.Curr.AllMachines.ToList();
            var nm = new MachineInfo(Name, IP);
            Allm.Add(nm);
            Allm = Allm.OrderBy(m => m.MachineName).ToList();
            MainWindowDC.Curr.AllMachines.Insert(Allm.IndexOf(nm), nm);
            MainWindowDC.Curr.IPaddreses = MainWindowDC.Curr.AllMachines.Select(m => m.IP).ToList();

            Settings.Default.Machines = JsonConvert.SerializeObject(MainWindowDC.Curr.AllMachines);

            MainWindowDC.Curr.ScanMachines();
        }
        private MachineInfo(string Name, IPAddress IP)
        {
            MachineName = Name;
            this.IP = IP;
        }
        public MachineInfo() { }

        private Ping PingSender = new Ping();
        private IPStatus SendPing(IPAddress IP) => PingSender.Send(IP, 500).Status;  ///////////////////////////

        public string MachineName { get; set; }

        [JsonIgnore]
        public IPAddress IP { get; set; }
        public string IPs 
        {
            get => IP?.ToString() ?? "";
            set => IP = IPAddress.Parse(value);
        }
        [JsonIgnore]
        public string MAC { get; set; }
        [JsonIgnore]
        public MachineStatus Status { get; set; } = MachineStatus.Offline;
        
        public void Ping()
        {
            Task.Run(async () => 
            {
                var LastStatus = Status;
                int faults = 3;
                Status = MachineStatus.Ping;

                TryGetPing:
                var b = SendPing(IP) == IPStatus.Success;
                await Task.Delay(50);
                if (b)
                {
                    if (LastStatus == MachineStatus.Offline)
                    {
                        MAC = ConvertIpToMAC(IP);
                        SoundEnable();
                    }
                    Status = MachineStatus.Online;
                }
                else
                {
                    if(LastStatus == MachineStatus.Online)
                    {
                        faults--;
                        if (faults > 0) goto TryGetPing;
                        SoundDisable();
                    }
                    else MAC = null;
                    Status = MachineStatus.Offline;
                }
            });
        }

        private void SoundEnable()
        {
            Task.Run(() =>
            {
                Console.Beep(200, 100);
                Console.Beep(1000, 100);
                Console.Beep(2500, 100);
            });
        }
        private void SoundDisable()
        {
            Task.Run(() =>
            {
                Console.Beep(2000, 100);
                Console.Beep(700, 100);
                Console.Beep(200, 100);
            });
        }

        public static string ConvertIpToMAC(IPAddress ip)
        {
            byte[] macAddr = new byte[6];
            uint macAddrLen = (uint)macAddr.Length;

            if (SendARP(BitConverter.ToInt32(ip.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
                throw new InvalidOperationException("SendARP failed.");

            string[] str = new string[(int)macAddrLen];
            for (int i = 0; i < macAddrLen; i++)
                str[i] = macAddr[i].ToString("x2");

            return string.Join(":", str);
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int destIp, int srcIP, byte[] macAddr, ref uint physicalAddrLen);

        internal void DeleteMachine()
        {
            MainWindowDC.Curr.AllMachines.Remove(this);
            MainWindowDC.Curr.IPaddreses = MainWindowDC.Curr.AllMachines.Select(m => m.IP).ToList();

            Settings.Default.Machines = JsonConvert.SerializeObject(MainWindowDC.Curr.AllMachines);
        }
    }

    public class CoollectionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ObservableCollection<MachineInfo>)value).Count < 1 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public enum MachineStatus
    {
        Online,
        Offline,
        Ping
    }
    public class StatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case MachineStatus.Ping: return Brushes.Red;
                case MachineStatus.Online: return Brushes.Lime;
                case MachineStatus.Offline: return Brushes.Gray;
                default: return new NotImplementedException();
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StringToVisabilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty((string)value))
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
