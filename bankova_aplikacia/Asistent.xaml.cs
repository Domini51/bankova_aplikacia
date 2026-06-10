using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YahooFinanceApi;

namespace bankova_aplikacia
{
    public partial class Asistent : UserControl
    {
        static readonly string KeyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "bankova_aplikacia", "api-key.txt");

        static HttpClient http = new HttpClient();

        string apiKey = "";
        string systemPrompt = "";
        List<Dictionary<string, string>> historia = new List<Dictionary<string, string>>();

        public Asistent()
        {
            InitializeComponent();
        }

        void NacitajKluc()
        {
            if (!File.Exists(KeyPath)) return;
            apiKey = File.ReadAllText(KeyPath).Trim();
            if (!string.IsNullOrEmpty(apiKey))
                ApiKeyPanel.Visibility = Visibility.Collapsed;
        }

        void UlozKluc(string kluc)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(KeyPath));
            File.WriteAllText(KeyPath, kluc);
            apiKey = kluc;
            ApiKeyPanel.Visibility = Visibility.Collapsed;
        }

        public async Task NacitajKontext()
        {
            NacitajKluc();

            // kontext staci nacitat raz
            if (historia.Count > 0) return;

            SpinnerOverlay.Visibility = Visibility.Visible;
            TxtSpinnerText.Text = "Načítavam kontext...";

            try
            {
                double zostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
                var portfolio = await Database.NacitajPortfolio(App.PrihlasenyEmail);

                var sbPortfolio = new StringBuilder();
                if (portfolio.Count == 0)
                {
                    sbPortfolio.Append("Žiadne otvorené pozície");
                }
                else
                {
                    foreach (var p in portfolio)
                    {
                        string sym  = p.ContainsKey("Symbol")  ? p["Symbol"].ToString()          : "?";
                        double kusy = p.ContainsKey("Kusy")    ? Convert.ToDouble(p["Kusy"])      : 0;
                        double suma = p.ContainsKey("SumaEur") ? Convert.ToDouble(p["SumaEur"])   : 0;
                        sbPortfolio.AppendLine("  " + sym + ": " + kusy.ToString("F4") + " ks (nakúpené za " + suma.ToString("F2") + " €)");
                    }
                }

                TxtSpinnerText.Text = "Načítavam kurzy...";
                await Task.Delay(500);

                string cenyText;
                try
                {
                    string[] symboly = { "SPY", "URTH", "AAPL", "TSLA", "NVDA", "BTC-USD", "ETH-USD" };
                    var data = await Yahoo.Symbols(symboly)
                        .Fields(Field.RegularMarketPrice, Field.RegularMarketChangePercent)
                        .QueryAsync();

                    var sbCeny = new StringBuilder();
                    foreach (string sym in symboly)
                    {
                        double cena  = data[sym][Field.RegularMarketPrice];
                        double zmena = data[sym][Field.RegularMarketChangePercent];
                        string znak  = zmena >= 0 ? "+" : "";
                        sbCeny.AppendLine("  " + sym + ": " + cena.ToString("F2") + " USD (" + znak + zmena.ToString("F2") + "%)");
                    }
                    cenyText = sbCeny.ToString();
                }
                catch
                {
                    cenyText = "  Momentálne nedostupné";
                }

                systemPrompt = "Si investičný asistent v aplikácii Investičná Banka.\n\n" +
                    "Aktuálne dáta (" + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + "):\n" +
                    "Zostatok: " + zostatok.ToString("F2") + " €\n\n" +
                    "Portfólio:\n" + sbPortfolio.ToString() + "\n" +
                    "Kurzy (USD):\n" + cenyText + "\n" +
                    "Dostupné aktíva: SPY (S&P 500 ETF), URTH (MSCI World ETF), AAPL, TSLA, NVDA, BTC, ETH.\n\n" +
                    "Odpovedaj vždy v slovenčine. Buď stručný a praktický, dávaj konkrétne rady.";

                PridajSpravu("Ahoj! Vidím tvoj účet aj aktuálne kurzy. Čím ti môžem pomôcť s investovaním?", false);
            }
            finally
            {
                SpinnerOverlay.Visibility = Visibility.Collapsed;
            }
        }

        async Task<string> VoajGeminiAsync()
        {
            var obsah = new List<object>();
            obsah.Add(new { role = "user",  parts = new[] { new { text = systemPrompt } } });
            obsah.Add(new { role = "model", parts = new[] { new { text = "Rozumiem, mám prístup k tvojim finančným údajom a som pripravený pomôcť." } } });

            foreach (var h in historia)
            {
                obsah.Add(new { role = h["role"], parts = new[] { new { text = h["text"] } } });
            }

            var requestObj = new { contents = obsah };
            string json = JsonSerializer.Serialize(requestObj);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(
                "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key=" + apiKey,
                httpContent);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                using var errDoc = JsonDocument.Parse(responseJson);
                string errMsg = errDoc.RootElement
                    .GetProperty("error")
                    .GetProperty("message")
                    .GetString() ?? responseJson;
                throw new Exception(errMsg);
            }

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "(prázdna odpoveď)";
        }

        void PridajSpravu(string text, bool jeUzivatel)
        {
            var container = new StackPanel();
            container.HorizontalAlignment = jeUzivatel ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            container.Margin = new Thickness(0, 6, 0, 6);
            container.MaxWidth = 580;

            Brush bubbleBg;
            Brush textFg;
            if (jeUzivatel)
            {
                bubbleBg = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                textFg = Brushes.White;
            }
            else
            {
                bubbleBg = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                textFg = Application.Current.Resources["HlavnyText"] as Brush
                         ?? new SolidColorBrush(Color.FromRgb(26, 26, 26));
            }

            CornerRadius rohove;
            if (jeUzivatel)
                rohove = new CornerRadius(18, 18, 4, 18);
            else
                rohove = new CornerRadius(18, 18, 18, 4);

            Brush okraj;
            if (jeUzivatel)
                okraj = Brushes.Transparent;
            else
                okraj = new SolidColorBrush(Color.FromRgb(229, 229, 234));

            var bubble = new Border();
            bubble.Padding = new Thickness(14, 10, 14, 10);
            bubble.CornerRadius = rohove;
            bubble.Background = bubbleBg;
            bubble.BorderBrush = okraj;
            bubble.BorderThickness = new Thickness(1);
            bubble.Child = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                LineHeight = 22,
                Foreground = textFg
            };

            var cas = new TextBlock();
            cas.Text = DateTime.Now.ToString("HH:mm");
            cas.FontSize = 11;
            cas.Foreground = Application.Current.Resources["SekundarnyText"] as Brush
                             ?? new SolidColorBrush(Color.FromRgb(142, 142, 147));
            cas.HorizontalAlignment = jeUzivatel ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            cas.Margin = new Thickness(4, 3, 4, 0);

            container.Children.Add(bubble);
            container.Children.Add(cas);
            PanelSprav.Children.Add(container);
            Dispatcher.InvokeAsync(() => ScrollChat.ScrollToEnd());
        }

        private void BtnUlozKluc_Click(object sender, RoutedEventArgs e)
        {
            string kluc = TxtApiKey.Password.Trim();
            if (string.IsNullOrEmpty(kluc)) return;
            UlozKluc(kluc);
        }

        private async void BtnPoslat_Click(object sender, RoutedEventArgs e)
        {
            await PosliSpravu();
        }

        private async void TxtVstup_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                await PosliSpravu();
            }
        }

        async Task PosliSpravu()
        {
            string text = TxtVstup.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (string.IsNullOrEmpty(apiKey))
            {
                ApiKeyPanel.Visibility = Visibility.Visible;
                MessageBox.Show("Najprv zadaj Gemini API kľúč.");
                return;
            }

            TxtVstup.Clear();
            TxtVstup.IsEnabled = false;
            BtnPoslat.IsEnabled = false;

            PridajSpravu(text, true);
            var zaznam = new Dictionary<string, string>();
            zaznam["role"] = "user";
            zaznam["text"] = text;
            historia.Add(zaznam);

            TxtSpinnerText.Text = "Asistent premýšľa...";
            SpinnerOverlay.Visibility = Visibility.Visible;

            try
            {
                string odpoved = await VoajGeminiAsync();
                var odpZaznam = new Dictionary<string, string>();
                odpZaznam["role"] = "model";
                odpZaznam["text"] = odpoved;
                historia.Add(odpZaznam);
                PridajSpravu(odpoved, false);
            }
            catch (Exception ex)
            {
                historia.RemoveAt(historia.Count - 1);
                PridajSpravu("Chyba: " + ex.Message, false);
            }
            finally
            {
                SpinnerOverlay.Visibility = Visibility.Collapsed;
                TxtVstup.IsEnabled = true;
                BtnPoslat.IsEnabled = true;
                TxtVstup.Focus();
            }
        }
    }
}
