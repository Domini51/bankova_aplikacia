using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YahooFinanceApi;

namespace bankova_aplikacia
{
    public partial class MainAppWindow : Window
    {
        // -- uchovava zostatok a prijem pre panel investicii --
        private double _zostatok = 0;
        private double _prijem = 0;

        public MainAppWindow()
        {
            InitializeComponent();
            // -- nacitaj meno a email prihlasenho uzivatela do nastaveni --
            _ = NacitajUdajeUzivatela();
            // -- nacitaj ceny akcii hned pri starte --
            _ = NacitajCeny();
        }

        // ===== NAVIGACIA =====

        // -- prepne aktivny panel a zvyrazni tlacidlo v sidebari --
        private void PrepniPanel(UIElement panel, Button aktivne)
        {
            PanelPrehlad.Visibility = Visibility.Collapsed;
            PanelHistoria.Visibility = Visibility.Collapsed;
            PanelInvesticie.Visibility = Visibility.Collapsed;
            PanelNastavenia.Visibility = Visibility.Collapsed;

            panel.Visibility = Visibility.Visible;

            // -- reset vsetkych tlacidiel --
            foreach (var btn in new[] { BtnPrehlad, BtnHistoria, BtnInvesticie, BtnNastavenia })
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

        private void BtnPrehlad_Click(object sender, RoutedEventArgs e)
        {
            PrepniPanel(PanelPrehlad, BtnPrehlad);
            UpdateSlidersLock();
        }

        private async void BtnHistoria_Click(object sender, RoutedEventArgs e)
        {
            PrepniPanel(PanelHistoria, BtnHistoria);
            await NacitajHistoriu();
        }

        private void BtnInvesticie_Click(object sender, RoutedEventArgs e)
        {
            PrepniPanel(PanelInvesticie, BtnInvesticie);
            AktualizujZostatok();
            UpdateSlidersLock();
        }

        private async void BtnNastavenia_Click(object sender, RoutedEventArgs e)
        {
            PrepniPanel(PanelNastavenia, BtnNastavenia);
            await NacitajUdajeUzivatela();
        }

        private void BtnOdhlasit_Click(object sender, RoutedEventArgs e)
        {
            // -- odhlasi uzivatela a vrati ho na login okno --
            loginWindow login = new loginWindow();
            login.Show();
            this.Close();
        }

        // ===== PANEL PREHLAD =====

        private void BtnPridajVydavok_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            TextBox nazovBox;
            TextBox sumaBox;
            ListBox zoznam;

            if (btn.Parent is Grid grid)
            {
                if (grid.Children.Contains(NazovV1))
                { nazovBox = NazovV1; sumaBox = Suma1; zoznam = ZoznamVydavkov; }
                else if (grid.Children.Contains(NazovV2))
                { nazovBox = NazovV2; sumaBox = Suma2; zoznam = ZoznamVydavkov2; }
                else if (grid.Children.Contains(NazovV3))
                { nazovBox = NazovV3; sumaBox = Suma3; zoznam = ZoznamVydavkov3; }
                else
                { nazovBox = NazovV4; sumaBox = Suma4; zoznam = ZoznamVydavkov4; }
            }
            else return;

            string nazov = nazovBox.Text;
            string sumaText = sumaBox.Text;

            if (string.IsNullOrWhiteSpace(nazov) || nazov == "Názov výdavku")
                return;

            if (!double.TryParse(sumaText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double suma))
            {
                MessageBox.Show("Zadaj platnú číselnú hodnotu!");
                return;
            }

            // -- skontroluj ci vydavok neprekroci prijem --
            double celkom = SpocitajVsetkyVydavky();
            double prijem = 0;
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out prijem);

            if (celkom + suma > prijem)
            {
                MessageBox.Show($"Nemáš dostatok peňazí! Zostatok: {prijem - celkom:F2} €",
                    "Nedostatok financií", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            zoznam.Items.Add($"{nazov} - {suma} €");

            nazovBox.Text = "Názov výdavku";
            nazovBox.Foreground = Brushes.Gray;
            sumaBox.Text = "0";
            sumaBox.Foreground = Brushes.Gray;

            // -- automaticky aktualizuj metriky hore --
            AktualizujMetriky();
        }

        // -- spocita vsetky vydavky zo vsetkych zoznamov --
        private double SpocitajVsetkyVydavky()
        {
            double celkom = 0;
            foreach (var item in ZoznamVydavkov.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov2.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov3.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov4.Items) celkom += ParseSuma(item.ToString());
            return celkom;
        }

        private double ParseSuma(string? item)
        {
            if (item == null) return 0;
            var parts = item.Split('-');
            if (parts.Length < 2) return 0;
            var sumaStr = parts[^1].Replace("€", "").Trim();
            double.TryParse(sumaStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double suma);
            return suma;
        }

        // -- aktualizuje tri metriky hore v prehlad paneli --
        private void AktualizujMetriky()
        {
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double prijem);

            double celkom = SpocitajVsetkyVydavky();
            double zostatok = prijem - celkom;

            MetPrijem.Text = $"{prijem:F2} €";
            MetMinute.Text = $"{celkom:F2} €";
            MetZostatok.Text = $"{zostatok:F2} €";

            _zostatok = zostatok;
            _prijem = prijem;
        }

        private void AktualizujZostatok()
        {
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double prijem);
            double celkom = SpocitajVsetkyVydavky();
            _zostatok = prijem - celkom;
            _prijem = prijem;
            TxtZostatok.Text = $"Dostupný zostatok: {_zostatok:F2} €";
            TxtSporiaci.Text = $"Odporúčaná suma: {prijem * 0.30:F2} € (30% z príjmu {prijem:F2} €)";
        }

        // ===== FOCUS HANDLERY PRE TEXTBOXY =====

        private void NazovBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (box.Text == "Názov výdavku")
            {
                box.Text = "";
                box.Foreground = Brushes.Black;
            }
        }

        private void NazovBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(box.Text))
            {
                box.Text = "Názov výdavku";
                box.Foreground = Brushes.Gray;
            }
        }

        private void SumaBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (box.Text == "0")
            {
                box.Text = "";
                box.Foreground = Brushes.Black;
            }
        }

        private void SumaBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(box.Text))
            {
                box.Text = "0";
                box.Foreground = Brushes.Gray;
            }
        }

        private void MPrijem_TextChanged(object sender, TextChangedEventArgs e)
        {
            // -- pri zmene prijmu automaticky aktualizuj metriky --
            if (IsLoaded) AktualizujMetriky();
        }

        private void MPrijem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox tb)
                    tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void NazovVydavok_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is not TextBox tb) return;
                if (tb.Parent is not Grid grid) return;
                var sumaBox = grid.Children.OfType<TextBox>().FirstOrDefault(x => x != tb);
                sumaBox?.Focus();
                sumaBox?.SelectAll();
            }
        }

        private void SumaVydavok_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is not TextBox tb) return;
                if (tb.Parent is not Grid grid) return;
                var btn = grid.Children.OfType<Button>().FirstOrDefault();
                btn?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        // ===== PANEL INVESTICIE =====

        private async Task NacitajCeny()
        {
            try
            {
                var securities = await Yahoo.Symbols("SPY", "URTH", "AAPL", "TSLA", "NVDA", "BTC-USD", "ETH-USD")
                    .Fields(Field.RegularMarketPrice, Field.RegularMarketChangePercent)
                    .QueryAsync();

                Dispatcher.Invoke(() =>
                {
                    SetCena(TxtSPY, securities["SPY"]);
                    SetCena(TxtURTH, securities["URTH"]);
                    SetCena(TxtAAPL, securities["AAPL"]);
                    SetCena(TxtTSLA, securities["TSLA"]);
                    SetCena(TxtNVDA, securities["NVDA"]);
                    SetCena(TxtBTC, securities["BTC-USD"]);
                    SetCena(TxtETH, securities["ETH-USD"]);
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    TxtSPY.Text = TxtURTH.Text = TxtAAPL.Text =
                    TxtTSLA.Text = TxtNVDA.Text = TxtBTC.Text =
                    TxtETH.Text = "Cena nedostupná";
                });
            }
        }

        private void SetCena(TextBlock txt, Security security)
        {
            double cena = security[Field.RegularMarketPrice];
            double zmena = security[Field.RegularMarketChangePercent];
            string smer = zmena >= 0 ? "▲" : "▼";
            txt.Text = $"{cena:F2} USD  {smer} {Math.Abs(zmena):F2}%";
            txt.Foreground = zmena >= 0
                ? new SolidColorBrush(Color.FromRgb(50, 180, 50))
                : new SolidColorBrush(Color.FromRgb(220, 50, 50));
        }

        private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            double total = SliderSPY.Value + SliderURTH.Value + SliderAAPL.Value +
                           SliderTSLA.Value + SliderNVDA.Value + SliderBTC.Value + SliderETH.Value;

            TxtSPYPercent.Text = $"{SliderSPY.Value:F0}%  ({_zostatok * SliderSPY.Value / 100:F2} €)";
            TxtURTHPercent.Text = $"{SliderURTH.Value:F0}%  ({_zostatok * SliderURTH.Value / 100:F2} €)";
            TxtAAPLPercent.Text = $"{SliderAAPL.Value:F0}%  ({_zostatok * SliderAAPL.Value / 100:F2} €)";
            TxtTSLAPercent.Text = $"{SliderTSLA.Value:F0}%  ({_zostatok * SliderTSLA.Value / 100:F2} €)";
            TxtNVDAPercent.Text = $"{SliderNVDA.Value:F0}%  ({_zostatok * SliderNVDA.Value / 100:F2} €)";
            TxtBTCPercent.Text = $"{SliderBTC.Value:F0}%  ({_zostatok * SliderBTC.Value / 100:F2} €)";
            TxtETHPercent.Text = $"{SliderETH.Value:F0}%  ({_zostatok * SliderETH.Value / 100:F2} €)";

            TxtCelkovePercento.Text = $"Celkovo alokované: {total:F0}%";
            TxtCelkovaSuma.Text = $"Celková suma: {_zostatok * total / 100:F2} €";
            ProgressInvest.Value = Math.Min(total, 100);

            ProgressInvest.Foreground = total > 100
                ? new SolidColorBrush(Color.FromRgb(220, 50, 50))
                : new SolidColorBrush(Color.FromRgb(50, 180, 50));

            if (total > 100)
            {
                TxtCelkovePercento.Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 50));
                ((Slider)sender).Value -= e.NewValue - e.OldValue;
            }
            else
            {
                TxtCelkovePercento.Foreground = new SolidColorBrush(Color.FromRgb(50, 180, 50));
            }
        }

        private async void BtnPotvrdit_Click(object sender, RoutedEventArgs e)
        {
            double total = SliderSPY.Value + SliderURTH.Value + SliderAAPL.Value +
                           SliderTSLA.Value + SliderNVDA.Value + SliderBTC.Value + SliderETH.Value;

            if (total > 100)
            {
                MessageBox.Show("Celkový súčet percent presahuje 100%!", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var investicia = new Dictionary<string, object>
            {
                { "Gmail", App.PrihlasenyEmail },
                { "Datum", DateTime.Now.ToString("dd.MM.yyyy HH:mm") },
                { "SPY", $"{SliderSPY.Value:F0}% ({_zostatok * SliderSPY.Value / 100:F2} €)" },
                { "URTH", $"{SliderURTH.Value:F0}% ({_zostatok * SliderURTH.Value / 100:F2} €)" },
                { "AAPL", $"{SliderAAPL.Value:F0}% ({_zostatok * SliderAAPL.Value / 100:F2} €)" },
                { "TSLA", $"{SliderTSLA.Value:F0}% ({_zostatok * SliderTSLA.Value / 100:F2} €)" },
                { "NVDA", $"{SliderNVDA.Value:F0}% ({_zostatok * SliderNVDA.Value / 100:F2} €)" },
                { "BTC", $"{SliderBTC.Value:F0}% ({_zostatok * SliderBTC.Value / 100:F2} €)" },
                { "ETH", $"{SliderETH.Value:F0}% ({_zostatok * SliderETH.Value / 100:F2} €)" },
                { "Celkom", $"{_zostatok * total / 100:F2} €" }
            };

            await Database.UlozHistoriu(App.PrihlasenyEmail, investicia);

            MessageBox.Show($"Investície potvrdené!\nCelkom investované: {_zostatok * total / 100:F2} €",
                "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);
            ResetSliders();
            resetZostatokPrijem();
            UpdateSlidersLock();
        }

        private void ResetSliders()
        {
            SliderSPY.Value = 0;
            SliderURTH.Value = 0;
            SliderAAPL.Value = 0;
            SliderTSLA.Value = 0;
            SliderNVDA.Value = 0;
            SliderBTC.Value = 0;
            SliderETH.Value = 0;
        }
        private void resetZostatokPrijem()
        {
            _zostatok = 0;
            _prijem = 0;
            MPrijem.Text = "0";
            MPrijem.Foreground = Brushes.Gray;
            TxtZostatok.Text = $"Dostupný zostatok: {_zostatok:F2} €";
            TxtSporiaci.Text = $"Odporúčaná suma: {_prijem * 0.30:F2} € (30% z príjmu {_prijem:F2} €)";
        }

        private void UpdateSlidersLock()
        {
            if (_zostatok > 0)
            {
                SliderSPY.IsEnabled = true;
                SliderURTH.IsEnabled = true;
                SliderAAPL.IsEnabled = true;
                SliderTSLA.IsEnabled = true;
                SliderNVDA.IsEnabled = true;
                SliderBTC.IsEnabled = true;
                SliderETH.IsEnabled = true;
            }
            else
            {
                SliderSPY.IsEnabled = false;
                SliderURTH.IsEnabled = false;
                SliderAAPL.IsEnabled = false;
                SliderTSLA.IsEnabled = false;
                SliderNVDA.IsEnabled = false;
                SliderBTC.IsEnabled = false;
                SliderETH.IsEnabled = false;
            }
        }

        // ===== PANEL HISTORIA =====

        private async Task NacitajHistoriu()
        {
            var historia = await Database.NacitajHistoriu(App.PrihlasenyEmail);

            if (historia.Count == 0)
            {
                ZoznamHistorie.ItemsSource = new List<object>
                {
                    new {
                        Datum = "Žiadna história",
                        Celkom = "0 €",
                        SPY = "-", URTH = "-", AAPL = "-",
                        TSLA = "-", NVDA = "-", BTC = "-", ETH = "-"
                    }
                };
                return;
            }

            var zoznam = new List<object>();
            foreach (var inv in historia)
            {
                zoznam.Add(new
                {
                    Datum = inv.ContainsKey("Datum") ? inv["Datum"].ToString() : "-",
                    Celkom = inv.ContainsKey("Celkom") ? inv["Celkom"].ToString() : "0 €",
                    SPY = inv.ContainsKey("SPY") ? inv["SPY"].ToString() : "-",
                    URTH = inv.ContainsKey("URTH") ? inv["URTH"].ToString() : "-",
                    AAPL = inv.ContainsKey("AAPL") ? inv["AAPL"].ToString() : "-",
                    TSLA = inv.ContainsKey("TSLA") ? inv["TSLA"].ToString() : "-",
                    NVDA = inv.ContainsKey("NVDA") ? inv["NVDA"].ToString() : "-",
                    BTC = inv.ContainsKey("BTC") ? inv["BTC"].ToString() : "-",
                    ETH = inv.ContainsKey("ETH") ? inv["ETH"].ToString() : "-"
                });
            }

            ZoznamHistorie.ItemsSource = zoznam;
        }

        // ===== PANEL NASTAVENIA =====

        private async Task NacitajUdajeUzivatela()
        {
            // -- nacitaj meno uzivatela z databazy --
            string meno = await Database.NacitajMeno(App.PrihlasenyEmail);
            TxtMeno.Text = meno;
            TxtEmail.Text = App.PrihlasenyEmail;
            TopbarVitaj.Text = $"Vitaj späť, {meno}";
        }

        private async void BtnZmenitMeno_Click(object sender, RoutedEventArgs e)
        {
            string noveMeno = Microsoft.VisualBasic.Interaction.InputBox(
                "Zadaj nové meno:", "Zmena mena", "");

            if (string.IsNullOrEmpty(noveMeno)) return;

            bool uspech = await Database.ZmenMeno(App.PrihlasenyEmail, noveMeno);
            if (uspech)
            {
                TxtMeno.Text = noveMeno;
                MessageBox.Show("Meno bolo úspešne zmenené!", "Úspech",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnZmenitHeslo_Click(object sender, RoutedEventArgs e)
        {
            string noveHeslo = Microsoft.VisualBasic.Interaction.InputBox(
                "Zadaj nové heslo:", "Zmena hesla", "");

            if (string.IsNullOrEmpty(noveHeslo)) return;

            bool uspech = await Database.ZmenHeslo(App.PrihlasenyEmail, noveHeslo);
            if (uspech)
                MessageBox.Show("Heslo bolo úspešne zmenené!", "Úspech",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}