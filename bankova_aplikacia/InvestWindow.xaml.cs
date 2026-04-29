using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YahooFinanceApi;

namespace bankova_aplikacia
{
    public partial class InvestWindow : Window
    {
        private double _zostatok;
        private double _prijem;

        public InvestWindow(double zostatok, double prijem)
        {
            InitializeComponent();
            _zostatok = zostatok;
            _prijem = prijem;
            TxtZostatok.Text = $"Dostupný zostatok: {zostatok:F2} €";
            TxtSporiaci.Text = $"Odporúčaná suma: {prijem * 0.30:F2} € (30% z príjmu {prijem:F2} €)";
            _ = NacitajCeny();
                UpdateSlidersLock();
        }

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

        private void BtnHistoria_Click(object sender, RoutedEventArgs e)
        {
            HistoriaWindow historiaWindow = new HistoriaWindow();
            historiaWindow.Show();
        }
    }
}