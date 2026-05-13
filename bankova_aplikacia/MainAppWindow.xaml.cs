using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace bankova_aplikacia
{
    public partial class MainAppWindow : Window
    {
        public MainAppWindow()
        {
            InitializeComponent();
            _ = NacitajUdajeUzivatela();
        }

        private void PrepniPanel(UIElement panel, Button aktivne)
        {
            PanelPrehlad.Visibility = Visibility.Collapsed;
            PanelHistoria.Visibility = Visibility.Collapsed;
            PanelInvesticie.Visibility = Visibility.Collapsed;
            PanelUcet.Visibility = Visibility.Collapsed;
            PanelNastavenia.Visibility = Visibility.Collapsed;

            panel.Visibility = Visibility.Visible;
            FadeIn(panel);

            Button[] tlacidla = { BtnPrehlad, BtnHistoria, BtnInvesticie, BtnUcet, BtnNastavenia };
            for (int i = 0; i < tlacidla.Length; i++)
            {
                tlacidla[i].Background = Brushes.Transparent;
                tlacidla[i].Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                tlacidla[i].BorderThickness = new Thickness(0);
            }

            aktivne.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            aktivne.Foreground = Brushes.White;
            aktivne.BorderThickness = new Thickness(3, 0, 0, 0);
            aktivne.BorderBrush = Brushes.White;
        }

        private void FadeIn(UIElement panel)
        {
            DoubleAnimation anim = new DoubleAnimation();
            anim.From = 0;
            anim.To = 1;
            anim.Duration = new Duration(System.TimeSpan.FromSeconds(0.3));
            panel.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void PulseButton(Button btn)
        {
            Storyboard sb = (Storyboard)FindResource("PulseAnimacia");
            Storyboard.SetTarget(sb, btn);
            sb.Begin();
        }

        private void BtnPrehlad_Click(object sender, RoutedEventArgs e)
        {
            PulseButton(BtnPrehlad);
            PrepniPanel(PanelPrehlad, BtnPrehlad);
            TopbarTitle.Text = "Výpočet výdavkov";
        }

        private async void BtnHistoria_Click(object sender, RoutedEventArgs e)
        {
            PulseButton(BtnHistoria);
            PrepniPanel(PanelHistoria, BtnHistoria);
            TopbarTitle.Text = "História";
            await PanelHistoria.NacitajHistoriu();
        }

        private async void BtnInvesticie_Click(object sender, RoutedEventArgs e)
        {
            PulseButton(BtnInvesticie);
            PrepniPanel(PanelInvesticie, BtnInvesticie);
            TopbarTitle.Text = "Investície";
            await PanelInvesticie.AktualizujZostatok();
            await PanelInvesticie.NacitajCeny();
        }

        private async void BtnUcet_Click(object sender, RoutedEventArgs e)
        {
            PulseButton(BtnUcet);
            PrepniPanel(PanelUcet, BtnUcet);
            TopbarTitle.Text = "Účet";
            await PanelUcet.NacitajPortfolio();
        }

        private async void BtnNastavenia_Click(object sender, RoutedEventArgs e)
        {
            PulseButton(BtnNastavenia);
            PrepniPanel(PanelNastavenia, BtnNastavenia);
            TopbarTitle.Text = "Nastavenia";
            await PanelNastavenia.NacitajUdaje();
        }

        private void BtnOdhlasit_Click(object sender, RoutedEventArgs e)
        {
            loginWindow login = new loginWindow();
            login.Show();
            this.Close();
        }

        private async Task NacitajUdajeUzivatela()
        {
            string meno = await Database.NacitajMeno(App.PrihlasenyEmail);
            TopbarVitaj.Text = "Vitaj späť, " + meno;
            TopbarTitle.Text = "Výpočet výdavkov";
            FadeIn(PanelPrehlad);
        }
    }
}