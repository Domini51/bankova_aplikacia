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
            // -- nacitaj len udaje uzivatela pri starte --
            _ = NacitajUdajeUzivatela();
        }

        // ===== NAVIGACIA =====

        // -- prepne viditelnost panelov, zvyrazni aktivne tlacidlo a spusti animaciu --
        private void PrepniPanel(UIElement panel, Button aktivne)
        {
            PanelPrehlad.Visibility = Visibility.Collapsed;
            PanelHistoria.Visibility = Visibility.Collapsed;
            PanelInvesticie.Visibility = Visibility.Collapsed;
            PanelUcet.Visibility = Visibility.Collapsed;
            PanelNastavenia.Visibility = Visibility.Collapsed;

            panel.Visibility = Visibility.Visible;

            // -- spusti fade in animaciu na novom paneli --
            FadeIn(panel);

            // -- reset vsetkych tlacidiel na neaktivny stav --
            foreach (var btn in new[] { BtnPrehlad, BtnHistoria, BtnInvesticie, BtnUcet, BtnNastavenia })
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                btn.BorderThickness = new Thickness(0);
            }

            // -- zvyrazni aktivne tlacidlo --
            aktivne.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            aktivne.Foreground = Brushes.White;
            aktivne.BorderThickness = new Thickness(3, 0, 0, 0);
            aktivne.BorderBrush = Brushes.White;
        }

        // -- fade in animacia: opacity ide z 0 na 1 za 0.3 sekundy --
        private void FadeIn(UIElement panel)
        {
            DoubleAnimation animacia = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(System.TimeSpan.FromSeconds(0.3))
            };
            panel.BeginAnimation(UIElement.OpacityProperty, animacia);
        }

        // -- spusti pulse animaciu na tlacidlo --
        private void PulseButton(Button btn)
        {
            Storyboard pulse = (Storyboard)FindResource("PulseAnimacia");
            Storyboard.SetTarget(pulse, btn);
            pulse.Begin();
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

        // -- odhlasi uzivatela a zobrazi login okno --
        private void BtnOdhlasit_Click(object sender, RoutedEventArgs e)
        {
            loginWindow login = new loginWindow();
            login.Show();
            this.Close();
        }

        // -- nacita meno a email prihlaseneho uzivatela do topbaru --
        private async Task NacitajUdajeUzivatela()
        {
            string meno = await Database.NacitajMeno(App.PrihlasenyEmail);
            TopbarVitaj.Text = $"Vitaj späť, {meno}";
            TopbarTitle.Text = "Výpočet výdavkov";

            // -- fade in na prvom paneli pri starte --
            FadeIn(PanelPrehlad);
        }
    }
}