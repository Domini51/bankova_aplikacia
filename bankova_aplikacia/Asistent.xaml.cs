using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        string _apiKey = "";
        string _systemPrompt = "";
        readonly List<Message> _historia = new();

        public Asistent()
        {
            InitializeComponent();
        }

        void NacitajKluc()
        {
            if (!File.Exists(KeyPath)) return;
            _apiKey = File.ReadAllText(KeyPath).Trim();
            if (!string.IsNullOrEmpty(_apiKey))
                ApiKeyPanel.Visibility = Visibility.Collapsed;
        }

        void UlozKluc(string kluc)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(KeyPath)!);
            File.WriteAllText(KeyPath, kluc);
            _apiKey = kluc;
            ApiKeyPanel.Visibility = Visibility.Collapsed;
        }

        public async Task NacitajKontext()
        {
            NacitajKluc();

            if (_historia.Count > 0) return;

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
                        string sym = p.ContainsKey("Symbol") ? p["Symbol"].ToString()! : "?";
                        double kusy = p.ContainsKey("Kusy") ? Convert.ToDouble(p["Kusy"]) : 0;
                        double suma = p.ContainsKey("SumaEur") ? Convert.ToDouble(p["SumaEur"]) : 0;
                        sbPortfolio.AppendLine($"  - {sym}: {kusy:F4} ks (nakúpené za {suma:F2} €)");
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
                    foreach (var sym in symboly)
                    {
                        double cena = data[sym][Field.RegularMarketPrice];
                        double zmena = data[sym][Field.RegularMarketChangePercent];
                        string sign = zmena >= 0 ? "+" : "";
                        sbCeny.AppendLine($"  {sym}: {cena:F2} USD ({sign}{zmena:F2}%)");
                    }
                    cenyText = sbCeny.ToString();
                }
                catch
                {
                    cenyText = "  Momentálne nedostupné";
                }

                _systemPrompt = $"Si investičný asistent v aplikácii Investičná Banka.\n\n" +
                    $"Aktuálne dáta ({DateTime.Now:dd.MM.yyyy HH:mm}):\n" +
                    $"Zostatok: {zostatok:F2} €\n\n" +
                    $"Portfólio:\n{sbPortfolio}\n" +
                    $"Kurzy (USD):\n{cenyText}\n" +
                    "Dostupné aktíva v appke: SPY (S&P 500 ETF), URTH (MSCI World ETF), AAPL, TSLA, NVDA, BTC, ETH.\n\n" +
                    "Odpovedaj vždy v slovenčine. Buď stručný a praktický, dávaj konkrétne rady.";

                PridajSpravu("Ahoj! Vidím tvoj účet aj aktuálne kurzy. Čím ti môžem pomôcť s investovaním?", jeUzivatel: false);
            }
            finally
            {
                SpinnerOverlay.Visibility = Visibility.Collapsed;
            }
        }

        void PridajSpravu(string text, bool jeUzivatel)
        {
            var container = new StackPanel
            {
                HorizontalAlignment = jeUzivatel ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(0, 4, 0, 4)
            };

            Brush bubbleBg = jeUzivatel
                ? new SolidColorBrush(Color.FromRgb(46, 109, 164))
                : (Application.Current.Resources["PanelBg"] as Brush
                    ?? new SolidColorBrush(Color.FromRgb(30, 30, 30)));

            Brush textFg = jeUzivatel
                ? Brushes.White
                : (Application.Current.Resources["HlavnyText"] as Brush ?? Brushes.White);

            var bubble = new Border
            {
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = jeUzivatel
                    ? new CornerRadius(12, 12, 4, 12)
                    : new CornerRadius(12, 12, 12, 4),
                MaxWidth = 560,
                Background = bubbleBg,
                Child = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    LineHeight = 22,
                    Foreground = textFg
                }
            };

            container.Children.Add(bubble);
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

            if (string.IsNullOrEmpty(_apiKey))
            {
                ApiKeyPanel.Visibility = Visibility.Visible;
                MessageBox.Show("Najprv zadaj Claude API kľúč.");
                return;
            }

            TxtVstup.Clear();
            TxtVstup.IsEnabled = false;
            BtnPoslat.IsEnabled = false;

            PridajSpravu(text, jeUzivatel: true);
            _historia.Add(new Message { Role = RoleType.User, Content = new List<ContentBase> { new TextContent { Text = text } } });

            TxtSpinnerText.Text = "Asistent premýšľa...";
            SpinnerOverlay.Visibility = Visibility.Visible;

            try
            {
                var client = new AnthropicClient(_apiKey);
                var response = await client.Messages.GetClaudeMessageAsync(new MessageParameters
                {
                    Model = "claude-3-5-haiku-20241022",
                    MaxTokens = 1024,
                    System = new List<SystemMessage> { new SystemMessage(_systemPrompt) },
                    Messages = new List<Message>(_historia)
                });

                string odpoved = string.Join("", response.Content
                    .OfType<TextContent>()
                    .Select(c => c.Text));

                if (string.IsNullOrEmpty(odpoved))
                    odpoved = response.Content.FirstOrDefault()?.ToString() ?? "(prázdna odpoveď)";

                _historia.Add(new Message { Role = RoleType.Assistant, Content = new List<ContentBase> { new TextContent { Text = odpoved } } });
                PridajSpravu(odpoved, jeUzivatel: false);
            }
            catch (Exception ex)
            {
                _historia.RemoveAt(_historia.Count - 1);
                PridajSpravu("Chyba: " + ex.Message, jeUzivatel: false);
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
