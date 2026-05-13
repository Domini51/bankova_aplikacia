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

        public async Task AktualizujZostatok()
        {
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            App.AktualnyZostatok = zostatok;
            TxtZostatok.Text = "Dostupný zostatok: " + zostatok.ToString("F2") + " €";

            double odporucana = App.AktualnyPrijem * 0.30;
            TxtSporiaci.Text = "Odporúčaná suma: " + odporucana.ToString("F2") + " € (30% z príjmu " + App.AktualnyPrijem.ToString("F2") + " €)";

            UpdateSlidersLock();
        }

        public async Task NacitajCeny()
        {
            SpinnerOverlay.Visibility = Visibility.Visible;
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
                    TxtSPY.Text = "Cena nedostupná";
                    TxtURTH.Text = "Cena nedostupná";
                    TxtAAPL.Text = "Cena nedostupná";
                    TxtTSLA.Text = "Cena nedostupná";
                    TxtNVDA.Text = "Cena nedostupná";
                    TxtBTC.Text = "Cena nedostupná";
                    TxtETH.Text = "Cena nedostupná";
                });
            }
            finally
            {
                Dispatcher.Invoke(() => SpinnerOverlay.Visibility = Visibility.Collapsed);
            }
        }

        private void SetCena(TextBlock txt, Security security)
        {
            double cena = security[Field.RegularMarketPrice];
            double zmena = security[Field.RegularMarketChangePercent];

            string smer = "▲";
            if (zmena < 0) smer = "▼";

            txt.Text = cena.ToString("F2") + " USD  " + smer + " " + Math.Abs(zmena).ToString("F2") + "%";

            if (zmena >= 0)
                txt.Foreground = new SolidColorBrush(Color.FromRgb(50, 180, 50));
            else
                txt.Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 50));
        }

        private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;

            double total = SliderSPY.Value + SliderURTH.Value + SliderAAPL.Value +
                           SliderTSLA.Value + SliderNVDA.Value + SliderBTC.Value + SliderETH.Value;

            double z = App.AktualnyZostatok;

            TxtSPYPercent.Text = SliderSPY.Value.ToString("F0") + "%  (" + (z * SliderSPY.Value / 100).ToString("F2") + " €)";
            TxtURTHPercent.Text = SliderURTH.Value.ToString("F0") + "%  (" + (z * SliderURTH.Value / 100).ToString("F2") + " €)";
            TxtAAPLPercent.Text = SliderAAPL.Value.ToString("F0") + "%  (" + (z * SliderAAPL.Value / 100).ToString("F2") + " €)";
            TxtTSLAPercent.Text = SliderTSLA.Value.ToString("F0") + "%  (" + (z * SliderTSLA.Value / 100).ToString("F2") + " €)";
            TxtNVDAPercent.Text = SliderNVDA.Value.ToString("F0") + "%  (" + (z * SliderNVDA.Value / 100).ToString("F2") + " €)";
            TxtBTCPercent.Text = SliderBTC.Value.ToString("F0") + "%  (" + (z * SliderBTC.Value / 100).ToString("F2") + " €)";
            TxtETHPercent.Text = SliderETH.Value.ToString("F0") + "%  (" + (z * SliderETH.Value / 100).ToString("F2") + " €)";

            TxtCelkovePercento.Text = "Celkovo alokované: " + total.ToString("F0") + "%";
            TxtCelkovaSuma.Text = "Celková suma: " + (z * total / 100).ToString("F2") + " €";
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

        private async void BtnPotvrdit_Click(object sender, RoutedEventArgs e)
        {
            double total = SliderSPY.Value + SliderURTH.Value + SliderAAPL.Value +
                           SliderTSLA.Value + SliderNVDA.Value + SliderBTC.Value + SliderETH.Value;

            if (total > 100)
            {
                MessageBox.Show("Celkový súčet percent presahuje 100%!");
                return;
            }

            if (total == 0)
            {
                MessageBox.Show("Nenastavil si žiadne investície!");
                return;
            }

            var ceny = new Dictionary<string, double>();

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
                MessageBox.Show("Nepodarilo sa načítať aktuálne kurzy!");
                return;
            }

            double z = App.AktualnyZostatok;

            var slidery = new Dictionary<string, double>();
            slidery["SPY"] = SliderSPY.Value;
            slidery["URTH"] = SliderURTH.Value;
            slidery["AAPL"] = SliderAAPL.Value;
            slidery["TSLA"] = SliderTSLA.Value;
            slidery["NVDA"] = SliderNVDA.Value;
            slidery["BTC"] = SliderBTC.Value;
            slidery["ETH"] = SliderETH.Value;

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
            double aktZostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            await Database.UlozZostatok(App.PrihlasenyEmail, aktZostatok - investovana);

            var investicia = new Dictionary<string, object>();
            investicia["Gmail"] = App.PrihlasenyEmail;
            investicia["Datum"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            investicia["SPY"] = SliderSPY.Value.ToString("F0") + "% (" + (z * SliderSPY.Value / 100).ToString("F2") + " €)";
            investicia["URTH"] = SliderURTH.Value.ToString("F0") + "% (" + (z * SliderURTH.Value / 100).ToString("F2") + " €)";
            investicia["AAPL"] = SliderAAPL.Value.ToString("F0") + "% (" + (z * SliderAAPL.Value / 100).ToString("F2") + " €)";
            investicia["TSLA"] = SliderTSLA.Value.ToString("F0") + "% (" + (z * SliderTSLA.Value / 100).ToString("F2") + " €)";
            investicia["NVDA"] = SliderNVDA.Value.ToString("F0") + "% (" + (z * SliderNVDA.Value / 100).ToString("F2") + " €)";
            investicia["BTC"] = SliderBTC.Value.ToString("F0") + "% (" + (z * SliderBTC.Value / 100).ToString("F2") + " €)";
            investicia["ETH"] = SliderETH.Value.ToString("F0") + "% (" + (z * SliderETH.Value / 100).ToString("F2") + " €)";
            investicia["Celkom"] = investovana.ToString("F2") + " €";

            await Database.UlozHistoriu(App.PrihlasenyEmail, investicia);

            MessageBox.Show("Investície potvrdené!\nCelkom investované: " + investovana.ToString("F2") + " €");

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

        public void UpdateSlidersLock()
        {
            bool enabled = App.AktualnyZostatok > 0;
            SliderSPY.IsEnabled = enabled;
            SliderURTH.IsEnabled = enabled;
            SliderAAPL.IsEnabled = enabled;
            SliderTSLA.IsEnabled = enabled;
            SliderNVDA.IsEnabled = enabled;
            SliderBTC.IsEnabled = enabled;
            SliderETH.IsEnabled = enabled;
        }
    }
}