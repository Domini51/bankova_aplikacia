using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YahooFinanceApi;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace bankova_aplikacia
{
    public partial class MainAppWindow : Window
    {
        private double _zostatok = 0;
        private double _prijem = 0;

        public MainAppWindow()
        {
            InitializeComponent();
            _ = NacitajUdajeUzivatela();
            _ = NacitajCeny();
        }

        // ===== NAVIGACIA =====

        private void PrepniPanel(UIElement panel, Button aktivne)
        {
            PanelPrehlad.Visibility = Visibility.Collapsed;
            PanelHistoria.Visibility = Visibility.Collapsed;
            PanelInvesticie.Visibility = Visibility.Collapsed;
            PanelNastavenia.Visibility = Visibility.Collapsed;
            PanelUcet.Visibility = Visibility.Collapsed;

            panel.Visibility = Visibility.Visible;

            foreach (var btn in new[] { BtnPrehlad, BtnHistoria, BtnInvesticie, BtnNastavenia, BtnUcet })
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                btn.BorderThickness = new Thickness(0);
            }

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

        private async void BtnInvesticie_Click(object sender, RoutedEventArgs e)
        {
            PrepniPanel(PanelInvesticie, BtnInvesticie);
            await AktualizujZostatok();
            UpdateSlidersLock();
        }

        private async void BtnNastavenia_Click(object sender, RoutedEventArgs e)
        {
            PrepniPanel(PanelNastavenia, BtnNastavenia);
            await NacitajUdajeUzivatela();
        }

        private void BtnOdhlasit_Click(object sender, RoutedEventArgs e)
        {
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

            AktualizujMetriky();
            AktualizujGrafVydavkov();
        }

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

        private async Task AktualizujZostatok()
        {
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            _zostatok = zostatok;
            TxtZostatok.Text = $"Dostupný zostatok: {zostatok:F2} €";
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double prijem);
            TxtSporiaci.Text = $"Odporúčaná suma: {prijem * 0.30:F2} € (30% z príjmu {prijem:F2} €)";
        }

        private async void BtnUlozZostatokNaUcet_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double prijem);
            double celkom = SpocitajVsetkyVydavky();
            double zostatok = prijem - celkom;

            if (zostatok <= 0)
            {
                MessageBox.Show("Nemáš žiadny zostatok na uloženie!", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double aktualnyZostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            double novyZostatok = aktualnyZostatok + zostatok;
            await Database.UlozZostatok(App.PrihlasenyEmail, novyZostatok);

            MessageBox.Show($"Na účet bolo pripísaných {zostatok:F2} €\nCelkový zostatok na účte: {novyZostatok:F2} €",
                "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // -- aktualizuje kruhovy graf vydavkov --
        private void AktualizujGrafVydavkov()
        {
            double nutne = 0, hlavne = 0, osobne = 0, volne = 0;
            foreach (var item in ZoznamVydavkov.Items) nutne += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov2.Items) hlavne += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov3.Items) osobne += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov4.Items) volne += ParseSuma(item.ToString());

            if (nutne + hlavne + osobne + volne == 0) return;

            GrafVydavkov.Series = new ISeries[]
            {
                new PieSeries<double>
                {
                    Values = new double[] { nutne },
                    Name = "Nutné",
                    Fill = new SolidColorPaint(SKColor.Parse("#E24B4A"))
                },
                new PieSeries<double>
                {
                    Values = new double[] { hlavne },
                    Name = "Hlavné",
                    Fill = new SolidColorPaint(SKColor.Parse("#2E6DA4"))
                },
                new PieSeries<double>
                {
                    Values = new double[] { osobne },
                    Name = "Osobné",
                    Fill = new SolidColorPaint(SKColor.Parse("#32B432"))
                },
                new PieSeries<double>
                {
                    Values = new double[] { volne },
                    Name = "Voľné",
                    Fill = new SolidColorPaint(SKColor.Parse("#FFA500"))
                }
            };
        }

        // -- aktualizuje kruhovy graf portfolia --
        private void AktualizujGrafPortfolia(List<Dictionary<string, object>> portfolio)
        {
            if (portfolio.Count == 0) return;

            var serie = new List<ISeries>();
            var farby = new[]
            {
                "#E24B4A", "#2E6DA4", "#32B432", "#FFA500",
                "#9B59B6", "#1ABC9C", "#F39C12"
            };

            var skupiny = new Dictionary<string, double>();
            foreach (var poz in portfolio)
            {
                string symbol = poz.ContainsKey("Symbol") ? poz["Symbol"].ToString()! : "-";
                double suma = poz.ContainsKey("SumaEur") ? Convert.ToDouble(poz["SumaEur"]) : 0;
                if (skupiny.ContainsKey(symbol))
                    skupiny[symbol] += suma;
                else
                    skupiny[symbol] = suma;
            }

            int i = 0;
            foreach (var kvp in skupiny)
            {
                serie.Add(new PieSeries<double>
                {
                    Values = new double[] { kvp.Value },
                    Name = kvp.Key,
                    Fill = new SolidColorPaint(SKColor.Parse(farby[i % farby.Length]))
                });
                i++;
            }

            GrafPortfolia.Series = serie;
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

            if (total == 0)
            {
                MessageBox.Show("Nenastavil si žiadne investície!", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Dictionary<string, double> ceny = new Dictionary<string, double>();
            try
            {
                var securities = await Yahoo.Symbols("SPY", "URTH", "AAPL", "TSLA", "NVDA", "BTC-USD", "ETH-USD")
                    .Fields(Field.RegularMarketPrice)
                    .QueryAsync();
                ceny["SPY"] = securities["SPY"][Field.RegularMarketPrice];
                ceny["URTH"] = securities["URTH"][Field.RegularMarketPrice];
                ceny["AAPL"] = securities["AAPL"][Field.RegularMarketPrice];
                ceny["TSLA"] = securities["TSLA"][Field.RegularMarketPrice];
                ceny["NVDA"] = securities["NVDA"][Field.RegularMarketPrice];
                ceny["BTC"] = securities["BTC-USD"][Field.RegularMarketPrice];
                ceny["ETH"] = securities["ETH-USD"][Field.RegularMarketPrice];
            }
            catch
            {
                MessageBox.Show("Nepodarilo sa načítať aktuálne kurzy!", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var slidery = new Dictionary<string, double>
            {
                { "SPY", SliderSPY.Value },
                { "URTH", SliderURTH.Value },
                { "AAPL", SliderAAPL.Value },
                { "TSLA", SliderTSLA.Value },
                { "NVDA", SliderNVDA.Value },
                { "BTC", SliderBTC.Value },
                { "ETH", SliderETH.Value }
            };

            foreach (var slider in slidery)
            {
                if (slider.Value > 0)
                {
                    double sumaEur = _zostatok * slider.Value / 100;
                    double nakupnaCena = ceny[slider.Key];
                    double kusy = sumaEur / nakupnaCena;
                    await Database.UlozPozíciu(App.PrihlasenyEmail, slider.Key, kusy, nakupnaCena, sumaEur);
                }
            }

            double investovana = _zostatok * total / 100;
            double aktualnyZostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            await Database.UlozZostatok(App.PrihlasenyEmail, aktualnyZostatok - investovana);

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
                { "Celkom", $"{investovana:F2} €" }
            };

            await Database.UlozHistoriu(App.PrihlasenyEmail, investicia);

            MessageBox.Show($"Investície potvrdené!\nCelkom investované: {investovana:F2} €",
                "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);

            ResetSliders();
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

        private void UpdateSlidersLock()
        {
            bool enabled = _zostatok > 0;
            SliderSPY.IsEnabled = enabled;
            SliderURTH.IsEnabled = enabled;
            SliderAAPL.IsEnabled = enabled;
            SliderTSLA.IsEnabled = enabled;
            SliderNVDA.IsEnabled = enabled;
            SliderBTC.IsEnabled = enabled;
            SliderETH.IsEnabled = enabled;
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

        // ===== PANEL UCET =====

        private async void BtnUcet_Click(object sender, RoutedEventArgs e)
        {
            PrepniPanel(PanelUcet, BtnUcet);
            await NacitajPortfolio();
        }

        private async Task NacitajPortfolio()
        {
            var portfolio = await Database.NacitajPortfolio(App.PrihlasenyEmail);
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            TxtUcetZostatok.Text = $"{zostatok:F2} €";

            if (portfolio.Count == 0)
            {
                ZoznamPortfolia.ItemsSource = null;
                TxtPrazdnePortfolio.Visibility = Visibility.Visible;
                return;
            }

            TxtPrazdnePortfolio.Visibility = Visibility.Collapsed;

            var zoznam = new List<object>();
            foreach (var poz in portfolio)
            {
                string symbol = poz.ContainsKey("Symbol") ? poz["Symbol"].ToString()! : "-";
                double kusy = poz.ContainsKey("Kusy") ? Convert.ToDouble(poz["Kusy"]) : 0;
                double sumaEur = poz.ContainsKey("SumaEur") ? Convert.ToDouble(poz["SumaEur"]) : 0;
                string docId = poz.ContainsKey("DocId") ? poz["DocId"].ToString()! : "";
                string datum = poz.ContainsKey("Datum") ? poz["Datum"].ToString()! : "-";

                zoznam.Add(new
                {
                    Symbol = symbol,
                    Info = $"{kusy:F4} ks • Kúpené: {datum} • Zaplatené: {sumaEur:F2} €",
                    AktualnaHodnota = $"{sumaEur:F2} €",
                    ZiskStrata = "Načítavam kurz...",
                    ZiskStrataFarba = "#888888",
                    DocId = docId,
                    SumaEur = sumaEur
                });
            }

            ZoznamPortfolia.ItemsSource = zoznam;
            AktualizujGrafPortfolia(portfolio);
            _ = AktualizujHodnotyPortfolia(portfolio);
        }

        private async Task AktualizujHodnotyPortfolia(List<Dictionary<string, object>> portfolio)
        {
            try
            {
                var securities = await Yahoo.Symbols("SPY", "URTH", "AAPL", "TSLA", "NVDA", "BTC-USD", "ETH-USD")
                    .Fields(Field.RegularMarketPrice)
                    .QueryAsync();

                var zoznam = new List<object>();
                foreach (var poz in portfolio)
                {
                    string symbol = poz.ContainsKey("Symbol") ? poz["Symbol"].ToString()! : "-";
                    double kusy = poz.ContainsKey("Kusy") ? Convert.ToDouble(poz["Kusy"]) : 0;
                    double sumaEur = poz.ContainsKey("SumaEur") ? Convert.ToDouble(poz["SumaEur"]) : 0;
                    string docId = poz.ContainsKey("DocId") ? poz["DocId"].ToString()! : "";
                    string datum = poz.ContainsKey("Datum") ? poz["Datum"].ToString()! : "-";

                    string yahooSymbol = symbol == "BTC" ? "BTC-USD" : symbol == "ETH" ? "ETH-USD" : symbol;
                    double aktualnaHodnota = sumaEur;
                    string ziskStrataText = "Kurz nedostupný";
                    string farba = "#888888";

                    if (securities.ContainsKey(yahooSymbol))
                    {
                        double aktCena = securities[yahooSymbol][Field.RegularMarketPrice];
                        aktualnaHodnota = kusy * aktCena;
                        double zisk = aktualnaHodnota - sumaEur;
                        string smer = zisk >= 0 ? "▲" : "▼";
                        ziskStrataText = $"{smer} {Math.Abs(zisk):F2} € ({(zisk / sumaEur * 100):F1}%)";
                        farba = zisk >= 0 ? "#32B432" : "#DC3232";
                    }

                    zoznam.Add(new
                    {
                        Symbol = symbol,
                        Info = $"{kusy:F4} ks • Kúpené: {datum} • Zaplatené: {sumaEur:F2} €",
                        AktualnaHodnota = $"{aktualnaHodnota:F2} €",
                        ZiskStrata = ziskStrataText,
                        ZiskStrataFarba = farba,
                        DocId = docId,
                        SumaEur = sumaEur
                    });
                }

                Dispatcher.Invoke(() => ZoznamPortfolia.ItemsSource = zoznam);
            }
            catch { }
        }

        private async void BtnPredaj_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string docId = btn.Tag?.ToString() ?? "";

            var potvrdit = MessageBox.Show("Naozaj chceš predať túto investíciu?", "Predaj",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (potvrdit != MessageBoxResult.Yes) return;

            var item = btn.DataContext as dynamic;
            if (item == null) return;

            double sumaEur = (double)item.SumaEur;

            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            zostatok += sumaEur;
            await Database.UlozZostatok(App.PrihlasenyEmail, zostatok);

            await Database.PredajPozíciu(docId);

            MessageBox.Show($"Investícia predaná! Na účet ti bolo pripísaných {sumaEur:F2} €",
                "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);

            await NacitajPortfolio();
        }

        private void GrafVydavkov_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}