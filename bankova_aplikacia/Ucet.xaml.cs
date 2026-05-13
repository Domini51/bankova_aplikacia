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

        // -- nacita portfolio a zostatok z databazy --
        public async Task NacitajPortfolio()
        {
            // -- zobraz spinner kym sa nacitava portfolio --
            SpinnerOverlay.Visibility = Visibility.Visible;

            // -- spomalenie aby bol spinner viditelny --
            await Task.Delay(1500);

            var portfolio = await Database.NacitajPortfolio(App.PrihlasenyEmail);
            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            TxtUcetZostatok.Text = $"{zostatok:F2} €";

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
                    SumaEur = sumaEur,
                    Kusy = kusy
                });
            }

            ZoznamPortfolia.ItemsSource = zoznam;
            AktualizujGrafPortfolia(portfolio);

            // -- skry spinner po nacitani --
            SpinnerOverlay.Visibility = Visibility.Collapsed;

            _ = AktualizujHodnotyPortfolia(portfolio);
        }

        // -- aktualizuje aktualne hodnoty pozicii z Yahoo Finance --
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
                        SumaEur = sumaEur,
                        Kusy = kusy
                    });
                }

                Dispatcher.Invoke(() => ZoznamPortfolia.ItemsSource = zoznam);
            }
            catch { }
        }

        // -- aktualizuje kruhovy graf portfolia --
        private void AktualizujGrafPortfolia(List<Dictionary<string, object>> portfolio)
        {
            if (portfolio.Count == 0) return;

            var serie = new List<ISeries>();
            var farby = new[] { "#E24B4A", "#2E6DA4", "#32B432", "#FFA500", "#9B59B6", "#1ABC9C", "#F39C12" };

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

        // -- predaj cast alebo vsetky akcie z pozicie --
        private async void BtnPredaj_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string docId = btn.Tag?.ToString() ?? "";

            var item = btn.DataContext as dynamic;
            if (item == null) return;

            double celkoveKusy = (double)item.Kusy;
            double sumaEur = (double)item.SumaEur;

            // -- opytaj sa kolko kusov chce predat --
            string vstup = Microsoft.VisualBasic.Interaction.InputBox(
                $"Koľko kusov chceš predať? (max {celkoveKusy:F4} ks)",
                "Predaj akcií", celkoveKusy.ToString("F4"));

            if (string.IsNullOrEmpty(vstup)) return;

            if (!double.TryParse(vstup, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double predavaneKusy))
            {
                MessageBox.Show("Zadaj platné číslo!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (predavaneKusy <= 0 || predavaneKusy > celkoveKusy)
            {
                MessageBox.Show($"Zadaj číslo medzi 0 a {celkoveKusy:F4}!", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // -- vypocitaj kolko eur dostane za predavane kusy --
            double predavanaSuma = sumaEur * (predavaneKusy / celkoveKusy);

            var potvrdit = MessageBox.Show(
                $"Predať {predavaneKusy:F4} ks za {predavanaSuma:F2} €?",
                "Potvrdenie predaja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (potvrdit != MessageBoxResult.Yes) return;

            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            zostatok += predavanaSuma;
            await Database.UlozZostatok(App.PrihlasenyEmail, zostatok);
            await Database.CiastocnyPredaj(docId, predavaneKusy, predavanaSuma);

            MessageBox.Show($"Predaj úspešný! Na účet ti bolo pripísaných {predavanaSuma:F2} €",
                "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);

            await NacitajPortfolio();
        }
    }
}