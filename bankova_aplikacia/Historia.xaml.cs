using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace bankova_aplikacia
{
    public class HistoriaChip
    {
        public string Kluc { get; set; } = "";
        public string Hodnota { get; set; } = "";
    }

    public class HistoriaPolozka
    {
        public string Datum { get; set; } = "";
        public string Celkom { get; set; } = "";
        public string Typ { get; set; } = "";
        public string TypFarba { get; set; } = "#888888";
        public List<HistoriaChip> Polozky { get; set; } = new();
    }

    public partial class Historia : UserControl
    {
        static readonly HashSet<string> _ignorovane = new() { "Gmail", "Datum", "Typ", "Celkom" };

        public Historia()
        {
            InitializeComponent();
        }

        public async Task NacitajHistoriu()
        {
            var historia = await Database.NacitajHistoriu(App.PrihlasenyEmail);

            if (historia.Count == 0)
            {
                ZoznamHistorie.ItemsSource = new[] { new HistoriaPolozka
                {
                    Datum = "Žiadna história",
                    Celkom = "",
                    Typ = "",
                    Polozky = new()
                }};
                return;
            }

            var zoznam = new List<HistoriaPolozka>();
            foreach (var inv in historia)
            {
                string Get(string k) => inv.ContainsKey(k) ? inv[k].ToString()! : "-";
                string typ = Get("Typ");

                var polozky = new List<HistoriaChip>();
                foreach (var kv in inv)
                {
                    if (_ignorovane.Contains(kv.Key)) continue;
                    string val = kv.Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(val) || val == "-") continue;
                    polozky.Add(new HistoriaChip { Kluc = kv.Key, Hodnota = val });
                }

                zoznam.Add(new HistoriaPolozka
                {
                    Datum = Get("Datum"),
                    Celkom = Get("Celkom"),
                    Typ = typ,
                    TypFarba = TypNaFarbu(typ),
                    Polozky = polozky
                });
            }

            zoznam.Sort((a, b) =>
            {
                bool pa = DateTime.TryParseExact(a.Datum, "dd.MM.yyyy HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime da);
                bool pb = DateTime.TryParseExact(b.Datum, "dd.MM.yyyy HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime db);
                if (!pa && !pb) return 0;
                if (!pa) return 1;
                if (!pb) return -1;
                return db.CompareTo(da);
            });

            ZoznamHistorie.ItemsSource = zoznam;
        }

        static string TypNaFarbu(string typ) => typ switch
        {
            "Nákup" => "#2E6DA4",
            "Predaj" => "#E24B4A",
            "Zostatok" => "#32B432",
            "Sporiaci" => "#FFA500",
            _ => "#888888"
        };
    }
}
