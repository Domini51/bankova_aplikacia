using System.Windows;

namespace bankova_aplikacia
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            await Database.Init();
            var window = new loginWindow();
            window.Show();
        }
    }
}