using System.Collections.Generic;
using System.Windows;

namespace bankova_aplikacia
{
    // -- Historia investicii - zobrazuje karty so vsetkymi detailmi --
    public partial class HistoriaWindow : Window
    {
        public HistoriaWindow()
        {
            InitializeComponent();
            _ = NacitajHistoriu();
        }

        // -- Nacita historiu z databazy a nastavi ju ako zdroj dat pre ItemsControl --
        private async System.Threading.Tasks.Task NacitajHistoriu()
        {
            var historia = await Database.NacitajHistoriu(App.PrihlasenyEmail);

            if (historia.Count == 0)
            {
                ZoznamHistorie.ItemsSource = new List<object>
                {
                    new {
                        Datum = "Žiadna história",
                        Celkom = "0 €",
                        SPY = "-", URTH = "-", AAPL = "-",
                        TSLA = "-", NVDA = "-", BTC = "-", ETH = "-"
                    }
                };
                return;
            }

            // -- Prekonvertuje zoznam Dictionary na anonymne objekty pre binding --
            var zoznam = new List<object>();
            foreach (var inv in historia)
            {
                zoznam.Add(new
                {
                    Datum = inv.ContainsKey("Datum") ? inv["Datum"].ToString() : "-",
                    Celkom = inv.ContainsKey("Celkom") ? inv["Celkom"].ToString() : "0 €",
                    SPY = inv.ContainsKey("SPY") ? inv["SPY"].ToString() : "-",
                    URTH = inv.ContainsKey("URTH") ? inv["URTH"].ToString() : "-",
                    AAPL = inv.ContainsKey("AAPL") ? inv["AAPL"].ToString() : "-",
                    TSLA = inv.ContainsKey("TSLA") ? inv["TSLA"].ToString() : "-",
                    NVDA = inv.ContainsKey("NVDA") ? inv["NVDA"].ToString() : "-",
                    BTC = inv.ContainsKey("BTC") ? inv["BTC"].ToString() : "-",
                    ETH = inv.ContainsKey("ETH") ? inv["ETH"].ToString() : "-"
                });
            }

            ZoznamHistorie.ItemsSource = zoznam;
        }

        // -- Tlacidlo spat - zavrie toto okno --
        private void BtnSpat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}