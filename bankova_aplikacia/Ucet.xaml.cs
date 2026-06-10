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
using System.Globalization;

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
                string symbol = ZistiText(poz, "Symbol");
                double kusy   = ZistiCislo(poz, "Kusy");
                double suma   = ZistiCislo(poz, "SumaEur");
                string docId  = ZistiText(poz, "DocId");
                string datum  = ZistiText(poz, "Datum");

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
                    Symbol2 = symbol
                });
            }

            ZoznamPortfolia.ItemsSource = zoznam;

            double celkomNakup = 0;
            var grafData = new List<(string, double)>();
            foreach (var poz in portfolio)
            {
                double suma = poz.ContainsKey("SumaEur") ? Convert.ToDouble(poz["SumaEur"]) : 0;
                string sym  = poz.ContainsKey("Symbol")  ? poz["Symbol"].ToString()          : "";
                celkomNakup += suma;
                grafData.Add((sym, suma));
            }
            TxtCelkovePortfolio.Text = celkomNakup.ToString("F2") + " €";
            AktualizujGrafPortfolia(grafData);

            SpinnerOverlay.Visibility = Visibility.Collapsed;

            _ = AktualizujKurzy(portfolio);
        }

        private async void BtnVybrat_Click(object sender, RoutedEventArgs e)
        {
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);

            string vstup = Microsoft.VisualBasic.Interaction.InputBox(
                "Dostupný zostatok: " + zostatok.ToString("F2") + " €\n\nKoľko chceš vybrať?",
                "Výber", "0");

            if (string.IsNullOrWhiteSpace(vstup)) return;

            if (!double.TryParse(vstup.Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture,
                    out double suma) || suma <= 0)
            { MessageBox.Show("Zadaj platné číslo!"); return; }

            if (suma > zostatok)
            { MessageBox.Show("Nemáš dostatok! Zostatok: " + zostatok.ToString("F2") + " €"); return; }

            await Database.UlozZostatok(App.PrihlasenyEmail, zostatok - suma);

            var zaznam = new Dictionary<string, object>();
            zaznam["Gmail"]  = App.PrihlasenyEmail;
            zaznam["Datum"]  = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            zaznam["Typ"]    = "Výber";
            zaznam["Celkom"] = suma.ToString("F2") + " €";
            await Database.UlozHistoriu(App.PrihlasenyEmail, zaznam);

            TxtUcetZostatok.Text = (zostatok - suma).ToString("F2") + " €";
            MessageBox.Show("Vybratých " + suma.ToString("F2") + " €");
        }

        private async void BtnPredat_Click(object sender, RoutedEventArgs e)
        {
            dynamic item = ((Button)sender).DataContext;
            string docId    = item.DocId.ToString();
            string symbol   = item.Symbol.ToString();
            double celkKusy = (double)item.Kusy;
            double sumaEur  = (double)item.SumaEur;

            double aktCena = celkKusy > 0 ? sumaEur / celkKusy : 0;
            try
            {
                string ySymbol = (symbol == "BTC" || symbol == "ETH") ? symbol + "-USD" : symbol;
                var data = await Yahoo.Symbols(ySymbol).Fields(Field.RegularMarketPrice).QueryAsync();
                aktCena = (double)data[ySymbol][Field.RegularMarketPrice];
            }
            catch { }

            string vstup = Microsoft.VisualBasic.Interaction.InputBox(
                "Máš " + celkKusy.ToString("F4") + " ks " + symbol + " (aktuálna cena: " + aktCena.ToString("F2") + " €/ks)\n\nKoľko kusov chceš predať?",
                "Predaj", celkKusy.ToString("F4"));

            if (string.IsNullOrWhiteSpace(vstup)) return;

            if (!double.TryParse(vstup.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double predavaneKusy) || predavaneKusy <= 0)
            { MessageBox.Show("Zadaj platné číslo!"); return; }

            if (predavaneKusy > celkKusy)
            { MessageBox.Show("Nemáš dosť kusov! Máš len " + celkKusy.ToString("F4") + " ks."); return; }

            double predavanaSuma = predavaneKusy * aktCena;

            var result = MessageBox.Show(
                "Predať " + predavaneKusy.ToString("F4") + " ks " + symbol + " za " + predavanaSuma.ToString("F2") + " €?",
                "Potvrdiť predaj", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            await Database.CiastocnyPredaj(docId, predavaneKusy, predavaneKusy / celkKusy * sumaEur);
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            await Database.UlozZostatok(App.PrihlasenyEmail, zostatok + predavanaSuma);

            var zaznam = new Dictionary<string, object>();
            zaznam["Gmail"]  = App.PrihlasenyEmail;
            zaznam["Datum"]  = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            zaznam["Typ"]    = "Predaj";
            zaznam["Celkom"] = predavanaSuma.ToString("F2") + " €";
            zaznam[symbol]   = predavaneKusy.ToString("F4") + " ks (" + predavanaSuma.ToString("F2") + " €)";
            await Database.UlozHistoriu(App.PrihlasenyEmail, zaznam);

            MessageBox.Show("Predaných " + predavaneKusy.ToString("F4") + " ks " + symbol + " za " + predavanaSuma.ToString("F2") + " €");
            await NacitajPortfolio();
        }

        async Task AktualizujKurzy(List<Dictionary<string, object>> portfolio)
        {
            var symboly = new List<string>();
            foreach (var p in portfolio)
            {
                if (!p.ContainsKey("Symbol")) continue;
                string s = p["Symbol"].ToString();
                string ySymbol = (s == "BTC" || s == "ETH") ? s + "-USD" : s;
                if (!symboly.Contains(ySymbol)) symboly.Add(ySymbol);
            }

            if (symboly.Count == 0) return;

            try
            {
                var data = await Yahoo.Symbols(symboly.ToArray())
                    .Fields(Field.RegularMarketPrice)
                    .QueryAsync();

                var aktualizovany = new List<object>();
                foreach (var poz in portfolio)
                {
                    string symbol = ZistiText(poz, "Symbol");
                    double kusy   = ZistiCislo(poz, "Kusy");
                    double suma   = ZistiCislo(poz, "SumaEur");
                    string docId  = ZistiText(poz, "DocId");
                    string datum  = ZistiText(poz, "Datum");

                    string ySymbol = (symbol == "BTC" || symbol == "ETH") ? symbol + "-USD" : symbol;
                    double aktCena = data.ContainsKey(ySymbol) ? (double)data[ySymbol][Field.RegularMarketPrice] : 0;
                    double aktHodnota = kusy * aktCena;
                    double zisk = aktHodnota - suma;

                    string ziskText;
                    if (zisk >= 0)
                        ziskText = "+" + zisk.ToString("F2") + " €";
                    else
                        ziskText = zisk.ToString("F2") + " €";

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

                double celkomHodnota = 0;
                foreach (var poz in portfolio)
                {
                    string sym = poz.ContainsKey("Symbol") ? poz["Symbol"].ToString() : "";
                    string yS  = (sym == "BTC" || sym == "ETH") ? sym + "-USD" : sym;
                    double k   = poz.ContainsKey("Kusy") ? Convert.ToDouble(poz["Kusy"]) : 0;
                    double c   = data.ContainsKey(yS) ? (double)data[yS][Field.RegularMarketPrice] : 0;
                    celkomHodnota += k * c;
                }

                var grafData2 = new List<(string, double)>();
                foreach (var poz in portfolio)
                {
                    string sym = poz.ContainsKey("Symbol") ? poz["Symbol"].ToString() : "";
                    string yS  = (sym == "BTC" || sym == "ETH") ? sym + "-USD" : sym;
                    double k   = poz.ContainsKey("Kusy") ? Convert.ToDouble(poz["Kusy"]) : 0;
                    double c   = data.ContainsKey(yS) ? (double)data[yS][Field.RegularMarketPrice] : 0;
                    grafData2.Add((sym, k * c));
                }

                Dispatcher.Invoke(() =>
                {
                    ZoznamPortfolia.ItemsSource = aktualizovany;
                    TxtCelkovePortfolio.Text = celkomHodnota.ToString("F2") + " €";
                    AktualizujGrafPortfolia(grafData2);
                });
            }
            catch { }
        }

        void AktualizujGrafPortfolia(List<(string sym, double val)> items)
        {
            string[] farby = { "#2E6DA4", "#E24B4A", "#32B432", "#FFA500", "#9B59B6", "#E67E22", "#1ABC9C" };
            var serie = new List<ISeries>();
            int i = 0;
            foreach (var (sym, val) in items)
            {
                if (val <= 0) continue;
                var ps = new PieSeries<double>();
                ps.Values = new double[] { val };
                ps.Name = sym;
                ps.Fill = new SolidColorPaint(SKColor.Parse(farby[i % farby.Length]));
                serie.Add(ps);
                i++;
            }
            GrafPortfolia.Series = serie;
        }

        static string ZistiText(Dictionary<string, object> d, string k)
        {
            if (d.ContainsKey(k))
                return d[k].ToString();
            return "-";
        }

        static double ZistiCislo(Dictionary<string, object> d, string k)
        {
            if (!d.ContainsKey(k))
                return 0;
            return Convert.ToDouble(d[k]);
        }
    }
}
