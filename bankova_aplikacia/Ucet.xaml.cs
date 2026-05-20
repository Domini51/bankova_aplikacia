using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YahooFinanceApi;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace bankova_aplikacia
{
    public partial class Ucet : UserControl
    {
        public Ucet()
        {
            InitializeComponent();
        }

        public async Task NacitajPortfolio()
        {
            SpinnerOverlay.Visibility = Visibility.Visible;
            await Task.Delay(1500);

            var portfolio = await Database.NacitajPortfolio(App.PrihlasenyEmail);
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            TxtUcetZostatok.Text = zostatok.ToString("F2") + " €";

            if (portfolio.Count == 0)
            {
                ZoznamPortfolia.ItemsSource = null;
                TxtPrazdnePortfolio.Visibility = Visibility.Visible;
                SpinnerOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            TxtPrazdnePortfolio.Visibility = Visibility.Collapsed;

            var zoznam = new List<object>();
            foreach (var poz in portfolio)
            {
                string Get(string k) => poz.ContainsKey(k) ? poz[k].ToString()! : "-";
                double GetD(string k) => poz.ContainsKey(k) ? Convert.ToDouble(poz[k]) : 0;

                string symbol = Get("Symbol");
                double kusy   = GetD("Kusy");
                double suma   = GetD("SumaEur");
                string docId  = Get("DocId");
                string datum  = Get("Datum");

                zoznam.Add(new
                {
                    Symbol = symbol,
                    Info = kusy.ToString("F4") + " ks • Kúpené: " + datum + " • Zaplatené: " + suma.ToString("F2") + " €",
                    AktualnaHodnota = suma.ToString("F2") + " €",
                    ZiskStrata = "Načítavam kurz...",
                    ZiskStrataFarba = "#888888",
                    DocId = docId,
                    Kusy = kusy,
                    SumaEur = suma,
                    Symbol2 = symbol  // pre predaj
                });
            }

            ZoznamPortfolia.ItemsSource = zoznam;
            SpinnerOverlay.Visibility = Visibility.Collapsed;

            // donacitaj aktualne kurzy na pozadi
            _ = AktualizujKurzy(portfolio);
        }

        private async void BtnPredat_Click(object sender, RoutedEventArgs e)
        {
            dynamic item = ((Button)sender).DataContext;
            string docId   = item.DocId.ToString();
            string symbol  = item.Symbol.ToString();
            double celkKusy = (double)item.Kusy;
            double sumaEur  = (double)item.SumaEur;

            // Nacitaj aktualnu cenu
            double aktCena = celkKusy > 0 ? sumaEur / celkKusy : 0;
            try
            {
                string ySymbol = (symbol == "BTC" || symbol == "ETH") ? symbol + "-USD" : symbol;
                var data = await Yahoo.Symbols(ySymbol).Fields(Field.RegularMarketPrice).QueryAsync();
                aktCena = (double)data[ySymbol][Field.RegularMarketPrice];
            }
            catch { }

            // Opytaj sa kolko kusov chce predat
            string vstup = Microsoft.VisualBasic.Interaction.InputBox(
                $"Máš {celkKusy:F4} ks {symbol} (aktuálna cena: {aktCena:F2} €/ks)\n\nKoľko kusov chceš predať?",
                "Predaj", celkKusy.ToString("F4"));

            if (string.IsNullOrWhiteSpace(vstup)) return;

            if (!double.TryParse(vstup.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double predavaneKusy) || predavaneKusy <= 0)
            { MessageBox.Show("Zadaj platné číslo!"); return; }

            if (predavaneKusy > celkKusy)
            { MessageBox.Show($"Nemáš dosť kusov! Máš len {celkKusy:F4} ks."); return; }

            double predavanaSuma = predavaneKusy * aktCena;

            var result = MessageBox.Show(
                $"Predať {predavaneKusy:F4} ks {symbol} za {predavanaSuma:F2} €?",
                "Potvrdiť predaj", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            await Database.CiastocnyPredaj(docId, predavaneKusy, predavaneKusy / celkKusy * sumaEur);
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            await Database.UlozZostatok(App.PrihlasenyEmail, zostatok + predavanaSuma);
            await Database.UlozHistoriu(App.PrihlasenyEmail, new Dictionary<string, object>
            {
                ["Gmail"] = App.PrihlasenyEmail,
                ["Datum"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                ["Typ"]   = "Predaj",
                [symbol]  = predavaneKusy.ToString("F4") + " ks (" + predavanaSuma.ToString("F2") + " €)"
            });

            MessageBox.Show($"Predaných {predavaneKusy:F4} ks {symbol} za {predavanaSuma:F2} €");
            await NacitajPortfolio();
        }

        async Task AktualizujKurzy(List<Dictionary<string, object>> portfolio)
        {
            // ziskaj unikatne symboly
            var symboly = new List<string>();
            foreach (var p in portfolio)
            {
                if (!p.ContainsKey("Symbol")) continue;
                string s = p["Symbol"].ToString()!;
                string ySymbol = (s == "BTC" || s == "ETH") ? s + "-USD" : s;
                if (!symboly.Contains(ySymbol)) symboly.Add(ySymbol);
            }

            if (symboly.Count == 0) return;

            try
            {
                var data = await Yahoo.Symbols(symboly.ToArray())
                    .Fields(Field.RegularMarketPrice)
                    .QueryAsync();

                // aktualizuj polozky v zozname
                var aktualizovany = new List<object>();
                foreach (var poz in portfolio)
                {
                    string Get(string k) => poz.ContainsKey(k) ? poz[k].ToString()! : "-";
                    double GetD(string k) => poz.ContainsKey(k) ? Convert.ToDouble(poz[k]) : 0;

                    string symbol = Get("Symbol");
                    double kusy   = GetD("Kusy");
                    double suma   = GetD("SumaEur");
                    string docId  = Get("DocId");
                    string datum  = Get("Datum");

                    string ySymbol = (symbol == "BTC" || symbol == "ETH") ? symbol + "-USD" : symbol;
                    double aktCena = data.ContainsKey(ySymbol) ? (double)data[ySymbol][Field.RegularMarketPrice] : 0;
                    double aktHodnota = kusy * aktCena;
                    double zisk = aktHodnota - suma;

                    string ziskText = (zisk >= 0 ? "+" : "") + zisk.ToString("F2") + " €";
                    string farba = zisk >= 0 ? "#32B432" : "#DC3232";

                    aktualizovany.Add(new
                    {
                        Symbol = symbol,
                        Info = kusy.ToString("F4") + " ks • Kúpené: " + datum + " • Zaplatené: " + suma.ToString("F2") + " €",
                        AktualnaHodnota = aktHodnota.ToString("F2") + " €",
                        ZiskStrata = ziskText,
                        ZiskStrataFarba = farba,
                        DocId = docId,
                        Kusy = kusy,
                        SumaEur = suma,
                        Symbol2 = symbol
                    });
                }

                Dispatcher.Invoke(() => ZoznamPortfolia.ItemsSource = aktualizovany);
            }
            catch { /* kurzy nedostupne, necháme posledné hodnoty */ }
        }
    }
}
