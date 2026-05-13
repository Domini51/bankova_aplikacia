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
                var prazdny = new List<object>();
                prazdny.Add(new
                {
                    Datum = "Žiadna história",
                    Celkom = "0 €",
                    SPY = "-",
                    URTH = "-",
                    AAPL = "-",
                    TSLA = "-",
                    NVDA = "-",
                    BTC = "-",
                    ETH = "-"
                });
                ZoznamHistorie.ItemsSource = prazdny;
                return;
            }

            var zoznam = new List<object>();

            for (int i = 0; i < historia.Count; i++)
            {
                var inv = historia[i];

                string datum = "-";
                string celkom = "0 €";
                string spy = "-";
                string urth = "-";
                string aapl = "-";
                string tsla = "-";
                string nvda = "-";
                string btc = "-";
                string eth = "-";

                if (inv.ContainsKey("Datum")) datum = inv["Datum"].ToString();
                if (inv.ContainsKey("Celkom")) celkom = inv["Celkom"].ToString();
                if (inv.ContainsKey("SPY")) spy = inv["SPY"].ToString();
                if (inv.ContainsKey("URTH")) urth = inv["URTH"].ToString();
                if (inv.ContainsKey("AAPL")) aapl = inv["AAPL"].ToString();
                if (inv.ContainsKey("TSLA")) tsla = inv["TSLA"].ToString();
                if (inv.ContainsKey("NVDA")) nvda = inv["NVDA"].ToString();
                if (inv.ContainsKey("BTC")) btc = inv["BTC"].ToString();
                if (inv.ContainsKey("ETH")) eth = inv["ETH"].ToString();

                zoznam.Add(new
                {
                    Datum = datum,
                    Celkom = celkom,
                    SPY = spy,
                    URTH = urth,
                    AAPL = aapl,
                    TSLA = tsla,
                    NVDA = nvda,
                    BTC = btc,
                    ETH = eth
                });
            }

            ZoznamHistorie.ItemsSource = zoznam;
        }
    }
}