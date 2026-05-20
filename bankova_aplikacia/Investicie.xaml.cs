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
        // zoznam symbolov pre jednoduchsi pristup
        readonly string[] _symboly = { "SPY", "URTH", "AAPL", "TSLA", "NVDA", "BTC-USD", "ETH-USD" };

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
            TxtSporiaci.Text = "Odporúčaná suma: " + odporucana.ToString("F2")
                + " € (30% z príjmu " + App.AktualnyPrijem.ToString("F2") + " €)";

            UpdateSlidersLock();
        }

        public async Task NacitajCeny()
        {
            SpinnerOverlay.Visibility = Visibility.Visible;
            await Task.Delay(2000);

            try
            {
                var data = await Yahoo.Symbols(_symboly)
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
        }

        void NastavCenu(TextBlock txt, Security s)
        {
            double cena = s[Field.RegularMarketPrice];
            double zmena = s[Field.RegularMarketChangePercent];
            bool kladna = zmena >= 0;

            txt.Text = cena.ToString("F2") + " USD  " + (kladna ? "▲" : "▼") + " " + Math.Abs(zmena).ToString("F2") + "%";
            txt.Foreground = new SolidColorBrush(kladna
                ? Color.FromRgb(50, 180, 50)
                : Color.FromRgb(220, 50, 50));
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

            // helper na formatovanie textu
            string F(double val) => val.ToString("F0") + "%  (" + (z * val / 100).ToString("F2") + " €)";

            TxtSPYPercent.Text  = F(SliderSPY.Value);
            TxtURTHPercent.Text = F(SliderURTH.Value);
            TxtAAPLPercent.Text = F(SliderAAPL.Value);
            TxtTSLAPercent.Text = F(SliderTSLA.Value);
            TxtNVDAPercent.Text = F(SliderNVDA.Value);
            TxtBTCPercent.Text  = F(SliderBTC.Value);
            TxtETHPercent.Text  = F(SliderETH.Value);

            TxtCelkovePercento.Text = "Celkovo alokované: " + total.ToString("F0") + "%";
            TxtCelkovaSuma.Text = "Celková suma: " + (z * total / 100).ToString("F2") + " €";
            ProgressInvest.Value = Math.Min(total, 100);

            bool prekrocene = total > 100;
            var farba = new SolidColorBrush(prekrocene ? Color.FromRgb(220, 50, 50) : Color.FromRgb(50, 180, 50));
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

            // nacitaj aktualne kurzy
            Dictionary<string, double> ceny;
            try
            {
                var data = await Yahoo.Symbols(_symboly).Fields(Field.RegularMarketPrice).QueryAsync();
                ceny = new Dictionary<string, double>
                {
                    ["SPY"]  = data["SPY"][Field.RegularMarketPrice],
                    ["URTH"] = data["URTH"][Field.RegularMarketPrice],
                    ["AAPL"] = data["AAPL"][Field.RegularMarketPrice],
                    ["TSLA"] = data["TSLA"][Field.RegularMarketPrice],
                    ["NVDA"] = data["NVDA"][Field.RegularMarketPrice],
                    ["BTC"]  = data["BTC-USD"][Field.RegularMarketPrice],
                    ["ETH"]  = data["ETH-USD"][Field.RegularMarketPrice]
                };
            }
            catch { MessageBox.Show("Nepodarilo sa načítať aktuálne kurzy!"); return; }

            double z = App.AktualnyZostatok;

            var slidery = new Dictionary<string, double>
            {
                ["SPY"]  = SliderSPY.Value,
                ["URTH"] = SliderURTH.Value,
                ["AAPL"] = SliderAAPL.Value,
                ["TSLA"] = SliderTSLA.Value,
                ["NVDA"] = SliderNVDA.Value,
                ["BTC"]  = SliderBTC.Value,
                ["ETH"]  = SliderETH.Value
            };

            var investicia = new Dictionary<string, object>
            {
                ["Gmail"] = App.PrihlasenyEmail,
                ["Datum"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                ["Typ"]   = "Nákup"
            };

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

        private async void BtnUlozSporiaci_Click(object sender, RoutedEventArgs e)
        {
            double odporucana = App.AktualnyPrijem * 0.30;
            if (odporucana <= 0) { MessageBox.Show("Najprv nastav mesačný príjem vo Výpočte výdavkov!"); return; }

            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            if (zostatok < odporucana)
            { MessageBox.Show($"Nemáš dostatok! Potrebuješ {odporucana:F2} €, máš {zostatok:F2} €."); return; }

            var result = MessageBox.Show(
                $"Uložiť {odporucana:F2} € na sporenie?",
                "Potvrdiť sporenie", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            await Database.UlozZostatok(App.PrihlasenyEmail, zostatok - odporucana);
            await Database.UlozHistoriu(App.PrihlasenyEmail, new Dictionary<string, object>
            {
                ["Gmail"]  = App.PrihlasenyEmail,
                ["Datum"]  = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                ["Typ"]    = "Sporiaci",
                ["Celkom"] = odporucana.ToString("F2") + " €"
            });

            App.AktualnyZostatok = zostatok - odporucana;
            TxtZostatok.Text = "Dostupný zostatok: " + App.AktualnyZostatok.ToString("F2") + " €";
            MessageBox.Show($"Uložených {odporucana:F2} € na sporenie!");
        }

        void ResetSliders()
        {
            SliderSPY.Value = SliderURTH.Value = SliderAAPL.Value = SliderTSLA.Value = 0;
            SliderNVDA.Value = SliderBTC.Value = SliderETH.Value = 0;
        }

        public void UpdateSlidersLock()
        {
            bool enabled = App.AktualnyZostatok > 0;
            SliderSPY.IsEnabled = SliderURTH.IsEnabled = SliderAAPL.IsEnabled = enabled;
            SliderTSLA.IsEnabled = SliderNVDA.IsEnabled = SliderBTC.IsEnabled = SliderETH.IsEnabled = enabled;
        }
    }
}
