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

            for (int i = 0; i < portfolio.Count; i++)
            {
                var poz = portfolio[i];

                string symbol = "-";
                double kusy = 0;
                double sumaEur = 0;
                string docId = "";
                string datum = "-";

                if (poz.ContainsKey("Symbol")) symbol = poz["Symbol"].ToString()!;
                if (poz.ContainsKey("Kusy")) kusy = Convert.ToDouble(poz["Kusy"]);
                if (poz.ContainsKey("SumaEur")) sumaEur = Convert.ToDouble(poz["SumaEur"]);
                if (poz.ContainsKey("DocId")) docId = poz["DocId"].ToString()!;
                if (poz.ContainsKey("Datum")) datum = poz["Datum"].ToString()!;

                string info = kusy.ToString("F4") + " ks • Kúpené: " + datum + " • Zaplatené: " + sumaEur.ToString("F2") + " €";

                zoznam.Add(new
                {
                    Symbol = symbol,
                    Info = info,
                    AktualnaHodnota = sumaEur.ToString("F2") + " €",
                    ZiskStrata = "Načítavam kurz...",
                    ZiskStrataFarba = "#888888",
                    DocId = docId,
                    SumaEur = sumaEur,
                    Kusy = kusy
                });
            }

            ZoznamPortfolia.ItemsSource = zoznam;
            AktualizujGrafPortfolia(portfolio);
            SpinnerOverlay.Visibility = Visibility.Collapsed;

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

                for (int i = 0; i < portfolio.Count; i++)
                {
                    var poz = portfolio[i];

                    string symbol = "-";
                    double kusy = 0;
                    double sumaEur = 0;
                    string docId = "";
                    string datum = "-";

                    if (poz.ContainsKey("Symbol")) symbol = poz["Symbol"].ToString()!;
                    if (poz.ContainsKey("Kusy")) kusy = Convert.ToDouble(poz["Kusy"]);
                    if (poz.ContainsKey("SumaEur")) sumaEur = Convert.ToDouble(poz["SumaEur"]);
                    if (poz.ContainsKey("DocId")) docId = poz["DocId"].ToString()!;
                    if (poz.ContainsKey("Datum")) datum = poz["Datum"].ToString()!;

                    string yahooSymbol = symbol;
                    if (symbol == "BTC") yahooSymbol = "BTC-USD";
                    if (symbol == "ETH") yahooSymbol = "ETH-USD";

                    double aktualnaHodnota = sumaEur;
                    string ziskStrataText = "Kurz nedostupný";
                    string farba = "#888888";

                    if (securities.ContainsKey(yahooSymbol))
                    {
                        double aktCena = securities[yahooSymbol][Field.RegularMarketPrice];
                        aktualnaHodnota = kusy * aktCena;
                        double zisk = aktualnaHodnota - sumaEur;

                        string smer = "▲";
                        if (zisk < 0) smer = "▼";

                        double percent = zisk / sumaEur * 100;
                        ziskStrataText = smer + " " + Math.Abs(zisk).ToString("F2") + " € (" + percent.ToString("F1") + "%)";

                        if (zisk >= 0)
                            farba = "#32B432";
                        else
                            farba = "#DC3232";
                    }

                    string info = kusy.ToString("F4") + " ks • Kúpené: " + datum + " • Zaplatené: " + sumaEur.ToString("F2") + " €";

                    zoznam.Add(new
                    {
                        Symbol = symbol,
                        Info = info,
                        AktualnaHodnota = aktualnaHodnota.ToString("F2") + " €",
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

        private void AktualizujGrafPortfolia(List<Dictionary<string, object>> portfolio)
        {
            if (portfolio.Count == 0)
                return;

            string[] farby = { "#E24B4A", "#2E6DA4", "#32B432", "#FFA500", "#9B59B6", "#1ABC9C", "#F39C12" };
            var skupiny = new Dictionary<string, double>();

            for (int i = 0; i < portfolio.Count; i++)
            {
                var poz = portfolio[i];

                string symbol = "-";
                double suma = 0;

                if (poz.ContainsKey("Symbol")) symbol = poz["Symbol"].ToString()!;
                if (poz.ContainsKey("SumaEur")) suma = Convert.ToDouble(poz["SumaEur"]);

                if (skupiny.ContainsKey(symbol))
                    skupiny[symbol] += suma;
                else
                    skupiny[symbol] = suma;
            }

            var serie = new List<ISeries>();
            int index = 0;

            foreach (var kvp in skupiny)
            {
                var s = new PieSeries<double>();
                s.Values = new double[] { kvp.Value };
                s.Name = kvp.Key;
                s.Fill = new SolidColorPaint(SKColor.Parse(farby[index % farby.Length]));
                serie.Add(s);
                index++;
            }

            GrafPortfolia.Series = serie;
        }

        private async void BtnPredaj_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string docId = btn.Tag?.ToString() ?? "";

            var item = btn.DataContext as dynamic;
            if (item == null) return;

            double celkoveKusy = (double)item.Kusy;
            double sumaEur = (double)item.SumaEur;

            string vstup = Microsoft.VisualBasic.Interaction.InputBox(
                "Koľko kusov chceš predať? (max " + celkoveKusy.ToString("F4") + " ks)",
                "Predaj akcií", celkoveKusy.ToString("F4"));

            if (string.IsNullOrEmpty(vstup)) return;

            double predavaneKusy;
            bool ok = double.TryParse(vstup, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out predavaneKusy);

            if (!ok)
            {
                MessageBox.Show("Zadaj platné číslo!");
                return;
            }

            predavaneKusy = Math.Round(predavaneKusy, 3);
            celkoveKusy = Math.Round(celkoveKusy, 3);

            if (predavaneKusy <= 0 || predavaneKusy > celkoveKusy)
            {
                MessageBox.Show("Zadaj číslo medzi 0 a " + celkoveKusy.ToString("F4") + "!");
                return;
            }

            double predavanaSuma = sumaEur * (predavaneKusy / celkoveKusy);

            var potvrdit = MessageBox.Show(
                "Predať " + predavaneKusy.ToString("F4") + " ks za " + predavanaSuma.ToString("F2") + " €?",
                "Potvrdenie predaja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (potvrdit != MessageBoxResult.Yes) return;

            double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            zostatok += predavanaSuma;
            await Database.UlozZostatok(App.PrihlasenyEmail, zostatok);
            await Database.CiastocnyPredaj(docId, predavaneKusy, predavanaSuma);

            MessageBox.Show("Predaj úspešný! Na účet ti bolo pripísaných " + predavanaSuma.ToString("F2") + " €");

            await NacitajPortfolio();
        }
    }
}