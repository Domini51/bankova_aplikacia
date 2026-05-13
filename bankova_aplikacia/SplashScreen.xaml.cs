using System.Threading.Tasks;
using System.Windows;

namespace bankova_aplikacia
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
            Loaded += SplashWindow_Loaded;
        }

        private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Načítavam...";
            await Task.Delay(1200);

            TxtStatus.Text = "Pripájam sa k databáze...";
            await Task.Delay(800);

            TxtStatus.Text = "Hotovo";
            await Task.Delay(500);

            loginWindow login = new loginWindow();
            login.Show();
            this.Close();
        }
    }
}