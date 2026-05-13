using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace bankova_aplikacia
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            // -- spusti animaciu loading baru a po 2.5 sekundach otvori login --
            Loaded += async (s, e) => await SpustiSplash();
        }

       
        private async Task SpustiSplash()
        {
            // -- animacia loading baru 
            DoubleAnimation animacia = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = new Duration(System.TimeSpan.FromSeconds(2.5))
            };
            LoadingBar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, animacia);

            
            await Task.Delay(2500);

            
            loginWindow login = new loginWindow();
            login.Show();
            this.Close();
        }
    }
}