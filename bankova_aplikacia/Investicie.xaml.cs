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
        string[] symboly = { "SPY", "URTH", "AAPL", "TSLA", "NVDA", "BTC-USD", "ETH-USD" };

        public Investicie()
        {
            InitializeComponent();
        }

        public async Task AktualizujZostatok()
        {
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            App.AktualnyZostatok = zostatok;
            TxtZostatok.Text = "Dostupný zostatok: " + zostatok.ToString("F2") + " €";

            UpdateSlidersLock();
        }

        public async Task NacitajCeny()
        {
            SpinnerOverlay.Visibility = Visibility.Visible;
            await Task.Delay(2000);

            try
            {
                var data = await Yahoo.Symbols(symboly)
                    .Fields(Field.RegularMarketPrice, Field.RegularMarketChangePercent)
                    .QueryAsync();

                Dispatcher.Invoke(() =>
                {
                    NastavCenu(TxtSPY,  data["SPY"]);
                    NastavCenu(TxtURTH, data["URTH"]);
                    NastavCenu(TxtAAPL, data["AAPL"]);
                    NastavCenu(TxtTSLA, data["TSLA"]);
                    NastavCenu(TxtNVDA, data["NVDA"]);
                    NastavCenu(TxtBTC,  data["BTC-USD"]);
                    NastavCenu(TxtETH,  data["ETH-USD"]);
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    foreach (var txt in new[] { TxtSPY, TxtURTH, TxtAAPL, TxtTSLA, TxtNVDA, TxtBTC, TxtETH })
                        txt.Text = "Cena nedostupná";
                });
            }
            finally
            {
                Dispatcher.Invoke(() => SpinnerOverlay.Visibility = Visibility.Collapsed);
            }

            await ChatPanel.NacitajKontext();
        }

        void NastavCenu(TextBlock txt, Security s)
        {
            double cena = s[Field.RegularMarketPrice];
            double zmena = s[Field.RegularMarketChangePercent];
            bool kladna = zmena >= 0;

            string smer = kladna ? "▲" : "▼";
            txt.Text = cena.ToString("F2") + " USD  " + smer + " " + Math.Abs(zmena).ToString("F2") + "%";
            if (kladna)
                txt.Foreground = new SolidColorBrush(Color.FromRgb(50, 180, 50));
            else
                txt.Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 50));
        }

        double SpocitajAlokáciu()
        {
            return SliderSPY.Value + SliderURTH.Value + SliderAAPL.Value
                 + SliderTSLA.Value + SliderNVDA.Value + SliderBTC.Value + SliderETH.Value;
        }

        private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            double total = SpocitajAlokáciu();
            double z = App.AktualnyZostatok;

            TxtSPYPercent.Text  = SliderSPY.Value.ToString("F0")  + "%  (" + (z * SliderSPY.Value / 100).ToString("F2")  + " €)";
            TxtURTHPercent.Text = SliderURTH.Value.ToString("F0") + "%  (" + (z * SliderURTH.Value / 100).ToString("F2") + " €)";
            TxtAAPLPercent.Text = SliderAAPL.Value.ToString("F0") + "%  (" + (z * SliderAAPL.Value / 100).ToString("F2") + " €)";
            TxtTSLAPercent.Text = SliderTSLA.Value.ToString("F0") + "%  (" + (z * SliderTSLA.Value / 100).ToString("F2") + " €)";
            TxtNVDAPercent.Text = SliderNVDA.Value.ToString("F0") + "%  (" + (z * SliderNVDA.Value / 100).ToString("F2") + " €)";
            TxtBTCPercent.Text  = SliderBTC.Value.ToString("F0")  + "%  (" + (z * SliderBTC.Value / 100).ToString("F2")  + " €)";
            TxtETHPercent.Text  = SliderETH.Value.ToString("F0")  + "%  (" + (z * SliderETH.Value / 100).ToString("F2")  + " €)";

            TxtCelkovePercento.Text = "Celkovo alokované: " + total.ToString("F0") + "%";
            TxtCelkovaSuma.Text = "Celková suma: " + (z * total / 100).ToString("F2") + " €";
            double zostokPo = z - (z * total / 100);
            TxtZostokPo.Text = "Zostatok po investovaní: " + zostokPo.ToString("F2") + " €";
            if (zostokPo > 0)
                TxtZostokPo.Foreground = new SolidColorBrush(Color.FromRgb(50, 180, 50));
            else
                TxtZostokPo.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            ProgressInvest.Value = Math.Min(total, 100);

            bool prekrocene = total > 100;
            SolidColorBrush farba;
            if (prekrocene)
                farba = new SolidColorBrush(Color.FromRgb(220, 50, 50));
            else
                farba = new SolidColorBrush(Color.FromRgb(50, 180, 50));
            ProgressInvest.Foreground = farba;
            TxtCelkovePercento.Foreground = farba;

            if (prekrocene)
                ((Slider)sender).Value -= e.NewValue - e.OldValue;
        }

        private async void BtnPotvrdit_Click(object sender, RoutedEventArgs e)
        {
            double total = SpocitajAlokáciu();
            if (total > 100) { MessageBox.Show("Celkový súčet percent presahuje 100%!"); return; }
            if (total == 0)  { MessageBox.Show("Nenastavil si žiadne investície!"); return; }

            Dictionary<string, double> ceny;
            try
            {
                var data = await Yahoo.Symbols(symboly).Fields(Field.RegularMarketPrice).QueryAsync();
                ceny = new Dictionary<string, double>();
                ceny["SPY"]  = data["SPY"][Field.RegularMarketPrice];
                ceny["URTH"] = data["URTH"][Field.RegularMarketPrice];
                ceny["AAPL"] = data["AAPL"][Field.RegularMarketPrice];
                ceny["TSLA"] = data["TSLA"][Field.RegularMarketPrice];
                ceny["NVDA"] = data["NVDA"][Field.RegularMarketPrice];
                ceny["BTC"]  = data["BTC-USD"][Field.RegularMarketPrice];
                ceny["ETH"]  = data["ETH-USD"][Field.RegularMarketPrice];
            }
            catch { MessageBox.Show("Nepodarilo sa načítať aktuálne kurzy!"); return; }

            double z = App.AktualnyZostatok;

            var slidery = new Dictionary<string, double>();
            slidery["SPY"]  = SliderSPY.Value;
            slidery["URTH"] = SliderURTH.Value;
            slidery["AAPL"] = SliderAAPL.Value;
            slidery["TSLA"] = SliderTSLA.Value;
            slidery["NVDA"] = SliderNVDA.Value;
            slidery["BTC"]  = SliderBTC.Value;
            slidery["ETH"]  = SliderETH.Value;

            var investicia = new Dictionary<string, object>();
            investicia["Gmail"] = App.PrihlasenyEmail;
            investicia["Datum"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            investicia["Typ"]   = "Nákup";

            foreach (var kv in slidery)
            {
                if (kv.Value <= 0) continue;
                double suma = z * kv.Value / 100;
                await Database.UlozPozíciu(App.PrihlasenyEmail, kv.Key, suma / ceny[kv.Key], ceny[kv.Key], suma);
                investicia[kv.Key] = kv.Value.ToString("F0") + "% (" + suma.ToString("F2") + " €)";
            }

            double investovana = z * total / 100;
            double aktZostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            await Database.UlozZostatok(App.PrihlasenyEmail, aktZostatok - investovana);

            investicia["Celkom"] = investovana.ToString("F2") + " €";
            await Database.UlozHistoriu(App.PrihlasenyEmail, investicia);

            MessageBox.Show("Investície potvrdené!\nCelkom investované: " + investovana.ToString("F2") + " €");
            ResetSliders();
            UpdateSlidersLock();
        }

        void ResetSliders()
        {
            SliderSPY.Value  = 0;
            SliderURTH.Value = 0;
            SliderAAPL.Value = 0;
            SliderTSLA.Value = 0;
            SliderNVDA.Value = 0;
            SliderBTC.Value  = 0;
            SliderETH.Value  = 0;
        }

        public void UpdateSlidersLock()
        {
            bool enabled = App.AktualnyZostatok > 0;
            SliderSPY.IsEnabled  = enabled;
            SliderURTH.IsEnabled = enabled;
            SliderAAPL.IsEnabled = enabled;
            SliderTSLA.IsEnabled = enabled;
            SliderNVDA.IsEnabled = enabled;
            SliderBTC.IsEnabled  = enabled;
            SliderETH.IsEnabled  = enabled;
        }
    }
}
