using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Windows;

namespace bankova_aplikacia
{
    public partial class App : Application
    {
        public static string PrihlasenyEmail { get; set; } = "";
        public static double AktualnyZostatok { get; set; } = 0;
        public static double AktualnyPrijem { get; set; } = 0;
        public static bool JeTmavy { get; private set; } = false;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LiveCharts.Configure(config => config
                .AddSkiaSharp()
                .AddDefaultMappers()
                .AddLightTheme());
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            await Database.Init();
            var splash = new SplashWindow();
            splash.Show();
        }

        public static void PrepniTemu()
        {
            JeTmavy = !JeTmavy;
            string nazovTemy;
            if (JeTmavy)
                nazovTemy = "Dark";
            else
                nazovTemy = "Light";
            var dict = new ResourceDictionary();
            dict.Source = new Uri("Themes/" + nazovTemy + ".xaml", UriKind.Relative);
            Current.Resources.MergedDictionaries[0] = dict;
        }
    }
} 