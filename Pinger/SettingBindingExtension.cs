using System.Windows.Data;

namespace Pinger
{
    public class SettingBindingExtension : Binding
    {
        public SettingBindingExtension()
        {
            Initialize();
        }

        public SettingBindingExtension(string path)
            : base(path)
        {
            Initialize();
        }

        private void Initialize()
        {
            this.Source = Pinger.Properties.Settings.Default;
            this.Mode = BindingMode.TwoWay;
        }
    }
}