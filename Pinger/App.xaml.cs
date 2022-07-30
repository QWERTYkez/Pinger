using System.Windows;

namespace Pinger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Pinger.Properties.Settings.Default.Save();
        }
    }
}
