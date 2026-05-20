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
            _ = NacitajUdajeUzivatela();
        }

        private async Task NacitajUdajeUzivatela()
        {
            string meno = await Database.NacitajMeno(App.PrihlasenyEmail);
            TopbarVitaj.Text = "Vitaj, " + meno + "!";
        }

        void PrepniPanel(UIElement panel, Button btn)
        {
            UIElement[] panely = { PanelPrehlad, PanelHistoria, PanelInvesticie, PanelUcet, PanelNastavenia };
            foreach (var p in panely)
                p.Visibility = Visibility.Collapsed;

            panel.Visibility = Visibility.Visible;
            SpustitFadeIn(panel);

            if (_aktivneTlacidlo != null)
            {
                _aktivneTlacidlo.Background = Brushes.Transparent;
                _aktivneTlacidlo.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                _aktivneTlacidlo.BorderThickness = new Thickness(0);
            }

            btn.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            btn.Foreground = Brushes.White;
            btn.BorderThickness = new Thickness(3, 0, 0, 0);
            btn.BorderBrush = Brushes.White;
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