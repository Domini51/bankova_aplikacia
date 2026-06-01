using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace bankova_aplikacia
{
    public partial class MainAppWindow : Window
    {
        Button? _aktivneTlacidlo;

        public MainAppWindow()
        {
            InitializeComponent();
            _aktivneTlacidlo = BtnPrehlad;
            TopbarTitle.Text = "Výpočet výdavkov";
            _ = NacitajUdajeUzivatela();
        }

        private async Task NacitajUdajeUzivatela()
        {
            string meno = await Database.NacitajMeno(App.PrihlasenyEmail);
            TopbarVitaj.Text = "Vitaj, " + meno + "!";
        }

        void PrepniPanel(UIElement panel, Button btn)
        {
            UIElement[] panely = { PanelPrehlad, PanelHistoria, PanelInvesticie, PanelUcet, PanelNastavenia, PanelAsistent };
            foreach (var p in panely)
                p.Visibility = Visibility.Collapsed;

            panel.Visibility = Visibility.Visible;
            SpustitFadeIn(panel);

            if (_aktivneTlacidlo != null)
            {
                _aktivneTlacidlo.Background = Brushes.Transparent;
                _aktivneTlacidlo.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                _aktivneTlacidlo.BorderThickness = new Thickness(0);
            }

            btn.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            btn.Foreground = Brushes.White;
            btn.BorderThickness = new Thickness(3, 0, 0, 0);
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 109, 164));
            _aktivneTlacidlo = btn;
        }

        void SpustitFadeIn(UIElement el)
        {
            var anim = new DoubleAnimation(0, 1, new Duration(System.TimeSpan.FromSeconds(0.3)));
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        void PulseButton(Button btn)
        {
            var sb = (Storyboard)FindResource("PulseAnimacia");
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

        private async void BtnAsistent_Click(object sender, RoutedEventArgs e)
        {
            PulseButton(BtnAsistent);
            PrepniPanel(PanelAsistent, BtnAsistent);
            TopbarTitle.Text = "AI Asistent";
            await PanelAsistent.NacitajKontext();
        }

        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
            App.PrepniTemu();
            IkonaTema.Text = App.JeTmavy ? "🌞" : "🌙";
        }

        private void BtnOdhlasit_Click(object sender, RoutedEventArgs e)
        {
            App.PrihlasenyEmail = "";
            new loginWindow().Show();
            Close();
        }
    }
}