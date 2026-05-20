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
            var dict = new ResourceDictionary
            {
                Source = new Uri($"Themes/{(JeTmavy ? "Dark" : "Light")}.xaml", UriKind.Relative)
            };
            Current.Resources.MergedDictionaries[0] = dict;
        }
    }
} 