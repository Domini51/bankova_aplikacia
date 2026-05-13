using System.Windows;

namespace bankova_aplikacia
{
    public partial class App : Application
    {
        public static string PrihlasenyEmail { get; set; } = "";
        public static double AktualnyZostatok { get; set; } = 0;
        public static double AktualnyPrijem { get; set; } = 0;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            await Database.Init();
            var splash = new SplashWindow();
            splash.Show();
        }
    }
} 