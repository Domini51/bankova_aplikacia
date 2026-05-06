using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YahooFinanceApi;

namespace bankova_aplikacia
{
    public partial class Investicie : UserControl
    {
        public Investicie()
        {
            InitializeComponent();
        }

        // -- aktualizuje zostatok a sporiaci odporucanu sumu --
        public async Task AktualizujZostatok()
        {
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            App.AktualnyZostatok = zostatok;
            TxtZostatok.Text = $"Dostupný zostatok: {zostatok:F2} €";
            TxtSporiaci.Text = $"Odporúčaná suma: {App.AktualnyPrijem * 0.30:F2} € (30% z príjmu {App.AktualnyPrijem:F2} €)";
            UpdateSlidersLock();
        }

        // -- nacita aktualne ceny z Yahoo Finance --
        public async Task NacitajCeny()
        {
            // -- zobraz spinner kym sa nacitavaju ceny --
            SpinnerOverlay.Visibility = Visibility.Visible;

            // -- umelé spomalenie na testovanie spinnera, potom zmazat --
            await Task.Delay(2000);

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
            finally
            {
                // -- skry spinner ked sa nacitavanie skoncilo --
                Dispatcher.Invoke(() => SpinnerOverlay.Visibility = Visibility.Collapsed);
            }
        }

        // -- nastavi text a farbu pre cenu akcie --
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

        // -- aktualizuje percenta a sumy pri pohybe slidermi --
        private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            double total = SliderSPY.Value + SliderURTH.Value + SliderAAPL.Value +
                           SliderTSLA.Value + SliderNVDA.Value + SliderBTC.Value + SliderETH.Value;

            double z = App.AktualnyZostatok;
            TxtSPYPercent.Text = $"{SliderSPY.Value:F0}%  ({z * SliderSPY.Value / 100:F2} €)";
            TxtURTHPercent.Text = $"{SliderURTH.Value:F0}%  ({z * SliderURTH.Value / 100:F2} €)";
            TxtAAPLPercent.Text = $"{SliderAAPL.Value:F0}%  ({z * SliderAAPL.Value / 100:F2} €)";
            TxtTSLAPercent.Text = $"{SliderTSLA.Value:F0}%  ({z * SliderTSLA.Value / 100:F2} €)";
            TxtNVDAPercent.Text = $"{SliderNVDA.Value:F0}%  ({z * SliderNVDA.Value / 100:F2} €)";
            TxtBTCPercent.Text = $"{SliderBTC.Value:F0}%  ({z * SliderBTC.Value / 100:F2} €)";
            TxtETHPercent.Text = $"{SliderETH.Value:F0}%  ({z * SliderETH.Value / 100:F2} €)";

            TxtCelkovePercento.Text = $"Celkovo alokované: {total:F0}%";
            TxtCelkovaSuma.Text = $"Celková suma: {z * total / 100:F2} €";
            ProgressInvest.Value = Math.Min(total, 100);

            if (total > 100)
            {
                ProgressInvest.Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 50));
                TxtCelkovePercento.Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 50));
                ((Slider)sender).Value -= e.NewValue - e.OldValue;
            }
            else
            {
                ProgressInvest.Foreground = new SolidColorBrush(Color.FromRgb(50, 180, 50));
                TxtCelkovePercento.Foreground = new SolidColorBrush(Color.FromRgb(50, 180, 50));
            }
        }

        // -- potvrdi investicie a ulozi ich do databazy --
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

            double z = App.AktualnyZostatok;
            var slidery = new Dictionary<string, double>
            {
                { "SPY", SliderSPY.Value }, { "URTH", SliderURTH.Value },
                { "AAPL", SliderAAPL.Value }, { "TSLA", SliderTSLA.Value },
                { "NVDA", SliderNVDA.Value }, { "BTC", SliderBTC.Value },
                { "ETH", SliderETH.Value }
            };

            foreach (var slider in slidery)
            {
                if (slider.Value > 0)
                {
                    double sumaEur = z * slider.Value / 100;
                    double nakupnaCena = ceny[slider.Key];
                    double kusy = sumaEur / nakupnaCena;
                    await Database.UlozPozíciu(App.PrihlasenyEmail, slider.Key, kusy, nakupnaCena, sumaEur);
                }
            }

            double investovana = z * total / 100;
            double aktualnyZostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            await Database.UlozZostatok(App.PrihlasenyEmail, aktualnyZostatok - investovana);

            var investicia = new Dictionary<string, object>
            {
                { "Gmail", App.PrihlasenyEmail },
                { "Datum", DateTime.Now.ToString("dd.MM.yyyy HH:mm") },
                { "SPY", $"{SliderSPY.Value:F0}% ({z * SliderSPY.Value / 100:F2} €)" },
                { "URTH", $"{SliderURTH.Value:F0}% ({z * SliderURTH.Value / 100:F2} €)" },
                { "AAPL", $"{SliderAAPL.Value:F0}% ({z * SliderAAPL.Value / 100:F2} €)" },
                { "TSLA", $"{SliderTSLA.Value:F0}% ({z * SliderTSLA.Value / 100:F2} €)" },
                { "NVDA", $"{SliderNVDA.Value:F0}% ({z * SliderNVDA.Value / 100:F2} €)" },
                { "BTC", $"{SliderBTC.Value:F0}% ({z * SliderBTC.Value / 100:F2} €)" },
                { "ETH", $"{SliderETH.Value:F0}% ({z * SliderETH.Value / 100:F2} €)" },
                { "Celkom", $"{investovana:F2} €" }
            };

            await Database.UlozHistoriu(App.PrihlasenyEmail, investicia);

            MessageBox.Show($"Investície potvrdené!\nCelkom investované: {investovana:F2} €",
                "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);

            ResetSliders();
            UpdateSlidersLock();
        }

        // -- vynuluje vsetky slidery --
        private void ResetSliders()
        {
            SliderSPY.Value = 0; SliderURTH.Value = 0; SliderAAPL.Value = 0;
            SliderTSLA.Value = 0; SliderNVDA.Value = 0; SliderBTC.Value = 0;
            SliderETH.Value = 0;
        }

        // -- zamkne slidery ak zostatok je 0 --
        public void UpdateSlidersLock()
        {
            bool enabled = App.AktualnyZostatok > 0;
            SliderSPY.IsEnabled = enabled; SliderURTH.IsEnabled = enabled;
            SliderAAPL.IsEnabled = enabled; SliderTSLA.IsEnabled = enabled;
            SliderNVDA.IsEnabled = enabled; SliderBTC.IsEnabled = enabled;
            SliderETH.IsEnabled = enabled;
        }
    }
}