using System.Windows;

namespace bankova_aplikacia
{
    public partial class App : Application
    {
        public static string PrihlasenyEmail { get; set; } = "";

        // -- zdielany zostatok a prijem medzi panelmi --
        public static double AktualnyZostatok { get; set; } = 0;
        public static double AktualnyPrijem { get; set; } = 0;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            await Database.Init();
            var window = new loginWindow();
            window.Show();
        }
    }
}