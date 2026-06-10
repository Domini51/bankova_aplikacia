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
        public List<HistoriaChip> Polozky { get; set; } = new List<HistoriaChip>();
    }

    public partial class Historia : UserControl
    {
        static HashSet<string> ignorovane = new HashSet<string> { "Gmail", "Datum", "Typ", "Celkom" };

        public Historia()
        {
            InitializeComponent();
        }

        public async Task NacitajHistoriu()
        {
            var historia = await Database.NacitajHistoriu(App.PrihlasenyEmail);

            if (historia.Count == 0)
            {
                var prazdna = new HistoriaPolozka();
                prazdna.Datum = "Žiadna história";
                prazdna.Celkom = "";
                prazdna.Typ = "";
                prazdna.Polozky = new List<HistoriaChip>();
                ZoznamHistorie.ItemsSource = new[] { prazdna };
                return;
            }

            var zoznam = new List<HistoriaPolozka>();
            foreach (var inv in historia)
            {
                string datum  = inv.ContainsKey("Datum")  ? inv["Datum"].ToString()  : "-";
                string celkom = inv.ContainsKey("Celkom") ? inv["Celkom"].ToString() : "-";
                string typ    = inv.ContainsKey("Typ")    ? inv["Typ"].ToString()    : "-";

                if (typ != "Nákup" && typ != "Predaj" && typ != "Sporiaci") continue;

                var polozky = new List<HistoriaChip>();
                foreach (var kv in inv)
                {
                    if (ignorovane.Contains(kv.Key)) continue;
                    string val = kv.Value != null ? kv.Value.ToString() : "";
                    if (string.IsNullOrEmpty(val) || val == "-") continue;
                    polozky.Add(new HistoriaChip { Kluc = kv.Key, Hodnota = val.Replace("-", "") });
                }

                var polozka = new HistoriaPolozka();
                polozka.Datum    = datum;
                polozka.Celkom   = celkom;
                polozka.Typ      = typ;
                polozka.TypFarba = TypNaFarbu(typ);
                polozka.Polozky  = polozky;
                zoznam.Add(polozka);
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

        static string TypNaFarbu(string typ)
        {
            if (typ == "Nákup")    return "#2E6DA4";
            if (typ == "Predaj")   return "#E24B4A";
            if (typ == "Zostatok") return "#32B432";
            if (typ == "Sporiaci") return "#FFA500";
            return "#888888";
        }
    }
}
