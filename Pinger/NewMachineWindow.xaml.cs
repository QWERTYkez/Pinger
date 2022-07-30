using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace Pinger
{
    /// <summary>
    /// Логика взаимодействия для NewMachineWindow.xaml
    /// </summary>
    public partial class NewMachineWindow : Window
    {
        private MainWindowDC DC => DataContext as MainWindowDC;

        public NewMachineWindow()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DC.CanApplyNewMachine = !string.IsNullOrWhiteSpace((sender as TextBox).Text) && 
                DC.NewMachineIP.Split('.').Length == 4 && 
                IPAddress.TryParse(DC.NewMachineIP, out IPAddress ip) && 
                !DC.IPaddreses.Contains(ip);
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            var tt = (sender as TextBox).Text;
            DC.CanApplyNewMachine = !string.IsNullOrWhiteSpace(DC.NewMachineName) && 
                tt.Split('.').Length == 4 && 
                IPAddress.TryParse(tt, out IPAddress ip) && 
                !DC.IPaddreses.Contains(ip);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MachineInfo.NewMachine(DC.NewMachineName.Trim(' '), IPAddress.Parse(DC.NewMachineIP));
            DC.CanApplyNewMachine = false;
            DC.NewMachineIP = String.Join('.', DC.AllMachines.First().IP.ToString().Split('.').Take(3)) + '.';
        }
    }
}
