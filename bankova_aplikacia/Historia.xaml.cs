using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace bankova_aplikacia
{
    public partial class Historia : UserControl
    {
        public Historia()
        {
            InitializeComponent();
        }

        public async Task NacitajHistoriu()
        {
            var historia = await Database.NacitajHistoriu(App.PrihlasenyEmail);

            if (historia.Count == 0)
            {
                ZoznamHistorie.ItemsSource = new[] { new
                {
                    Datum = "Žiadna história", Celkom = "0 €",
                    SPY = "-", URTH = "-", AAPL = "-",
                    TSLA = "-", NVDA = "-", BTC = "-", ETH = "-"
                }};
                return;
            }

            var zoznam = new List<object>();
            foreach (var inv in historia)
            {
                string Get(string k) => inv.ContainsKey(k) ? inv[k].ToString()! : "-";

                zoznam.Add(new
                {
                    Datum   = Get("Datum"),
                    Celkom  = Get("Celkom"),
                    SPY     = Get("SPY"),
                    URTH    = Get("URTH"),
                    AAPL    = Get("AAPL"),
                    TSLA    = Get("TSLA"),
                    NVDA    = Get("NVDA"),
                    BTC     = Get("BTC"),
                    ETH     = Get("ETH")
                });
            }

            ZoznamHistorie.ItemsSource = zoznam;
        }
    }
}
