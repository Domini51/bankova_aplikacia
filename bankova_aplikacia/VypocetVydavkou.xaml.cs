using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace bankova_aplikacia
{
    public partial class VypocetVydavkou : UserControl
    {
        public VypocetVydavkou()
        {
            InitializeComponent();
        }

        // vráti dvojicu (nazovBox, sumaBox, listBox) pre dané tlačidlo
        (TextBox nazov, TextBox suma, ListBox zoznam)? NajdiKomponenty(Button btn)
        {
            if (!(btn.Parent is Grid grid)) return null;

            if (grid.Children.Contains(NazovV1)) return (NazovV1, Suma1, ZoznamVydavkov);
            if (grid.Children.Contains(NazovV2)) return (NazovV2, Suma2, ZoznamVydavkov2);
            if (grid.Children.Contains(NazovV3)) return (NazovV3, Suma3, ZoznamVydavkov3);
            return (NazovV4, Suma4, ZoznamVydavkov4);
        }

        private void BtnPridajVydavok_Click(object sender, RoutedEventArgs e)
        {
            var komp = NajdiKomponenty((Button)sender);
            if (komp == null) return;

            var (nazovBox, sumaBox, zoznam) = komp.Value;

            string nazov = nazovBox.Text;
            if (string.IsNullOrWhiteSpace(nazov) || nazov == "Názov výdavku") return;

            if (!double.TryParse(sumaBox.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double suma))
            {
                MessageBox.Show("Zadaj platnú číselnú hodnotu!");
                return;
            }

            double prijem = NacitajPrijem();
            double celkom = SpocitajVsetkyVydavky();

            if (celkom + suma > prijem)
            {
                MessageBox.Show("Nemáš dostatok peňazí! Zostatok: " + (prijem - celkom).ToString("F2") + " €");
                return;
            }

            zoznam.Items.Add(nazov + " - " + suma + " €");

            nazovBox.Text = "Názov výdavku";
            nazovBox.Foreground = Brushes.Gray;
            sumaBox.Text = "0";
            sumaBox.Foreground = Brushes.Gray;

            AktualizujMetriky();
            AktualizujGrafVydavkov();
        }

        double NacitajPrijem()
        {
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double prijem);
            return prijem;
        }

        public double SpocitajVsetkyVydavky()
        {
            double celkom = 0;
            var vsetky = new[] { ZoznamVydavkov, ZoznamVydavkov2, ZoznamVydavkov3, ZoznamVydavkov4 };
            foreach (var zoz in vsetky)
                foreach (var item in zoz.Items)
                    celkom += ParseSuma(item?.ToString());
            return celkom;
        }

        double ParseSuma(string? text)
        {
            if (text == null) return 0;
            var parts = text.Split('-');
            if (parts.Length < 2) return 0;
            string sumaStr = parts[parts.Length - 1].Replace("€", "").Trim();
            double.TryParse(sumaStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double vysledok);
            return vysledok;
        }

        public void AktualizujMetriky()
        {
            double prijem = NacitajPrijem();
            double celkom = SpocitajVsetkyVydavky();

            MetPrijem.Text = prijem.ToString("F2") + " €";
            MetMinute.Text = celkom.ToString("F2") + " €";

            App.AktualnyZostatok = prijem - celkom;
            App.AktualnyPrijem = prijem;
        }

        private async void BtnUlozZostatokNaUcet_Click(object sender, RoutedEventArgs e)
        {
            double zostatok = NacitajPrijem() - SpocitajVsetkyVydavky();

            if (zostatok <= 0)
            { MessageBox.Show("Nemáš žiadny zostatok na uloženie!"); return; }

            double aktualny = await Database.NacitajZostatok(App.PrihlasenyEmail);
            double novy = aktualny + zostatok;
            await Database.UlozZostatok(App.PrihlasenyEmail, novy);

            await Database.UlozHistoriu(App.PrihlasenyEmail, new Dictionary<string, object>
            {
                ["Gmail"]  = App.PrihlasenyEmail,
                ["Datum"]  = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                ["Typ"]    = "Zostatok",
                ["Celkom"] = zostatok.ToString("F2") + " €"
            });

            MessageBox.Show("Na účet bolo pripísaných " + zostatok.ToString("F2") + " €\nCelkový zostatok: " + novy.ToString("F2") + " €");

            // vymazat vsetky vydavky a resetovat panel
            foreach (var lb in new[] { ZoznamVydavkov, ZoznamVydavkov2, ZoznamVydavkov3, ZoznamVydavkov4 })
                lb.Items.Clear();

            MPrijem.Text = "0";
            MPrijem.Foreground = Brushes.Gray;
            GrafVydavkov.Series = null;
            AktualizujMetriky();
        }

        void AktualizujGrafVydavkov()
        {
            double[] hodnoty =
            {
                SumujZoznamu(ZoznamVydavkov),
                SumujZoznamu(ZoznamVydavkov2),
                SumujZoznamu(ZoznamVydavkov3),
                SumujZoznamu(ZoznamVydavkov4)
            };

            if (hodnoty[0] + hodnoty[1] + hodnoty[2] + hodnoty[3] == 0) return;

            string[] nazvy = { "Nutné", "Hlavné", "Osobné", "Voľné" };
            string[] farby = { "#E24B4A", "#2E6DA4", "#32B432", "#FFA500" };

            var serie = new List<ISeries>();
            for (int i = 0; i < 4; i++)
            {
                serie.Add(new PieSeries<double>
                {
                    Values = new double[] { hodnoty[i] },
                    Name = nazvy[i],
                    Fill = new SolidColorPaint(SKColor.Parse(farby[i]))
                });
            }

            GrafVydavkov.Series = serie;
        }

        double SumujZoznamu(ListBox lb)
        {
            double s = 0;
            foreach (var item in lb.Items)
                s += ParseSuma(item?.ToString());
            return s;
        }

        // placeholder handlery pre textboxy
        private void NazovBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var box = (TextBox)sender;
            if (box.Text == "Názov výdavku") { box.Text = ""; box.Foreground = (Brush)Application.Current.Resources["HlavnyText"]; }
        }

        private void NazovBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(box.Text)) { box.Text = "Názov výdavku"; box.Foreground = Brushes.Gray; }
        }

        private void SumaBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var box = (TextBox)sender;
            if (box.Text == "0") { box.Text = ""; box.Foreground = (Brush)Application.Current.Resources["HlavnyText"]; }
        }

        private void SumaBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(box.Text)) { box.Text = "0"; box.Foreground = Brushes.Gray; }
        }

        private void MPrijem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) AktualizujMetriky();
        }

        private void MPrijem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ((TextBox)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void NazovVydavok_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var tb = (TextBox)sender;
            var grid = tb.Parent as Grid;
            if (grid == null) return;

            foreach (var child in grid.Children)
            {
                if (child is TextBox t && t != tb) { t.Focus(); t.SelectAll(); break; }
            }
        }

        private void SumaVydavok_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var grid = ((TextBox)sender).Parent as Grid;
            foreach (var child in grid!.Children)
            {
                if (child is Button btn) { btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); break; }
            }
        }

        private void BtnOdstranVydavok_Click(object sender, RoutedEventArgs e)
        {
            var lbi = ItemsControl.ContainerFromElement(null, (Button)sender) as ListBoxItem;
            if (lbi == null)
            {
                // prejdi vizualny strom nahor po ListBoxItem
                DependencyObject p = (Button)sender;
                while (p != null && p is not ListBoxItem) p = System.Windows.Media.VisualTreeHelper.GetParent(p);
                lbi = p as ListBoxItem;
            }
            if (lbi == null) return;

            var lb = ItemsControl.ItemsControlFromItemContainer(lbi) as ListBox;
            if (lb == null) return;

            lb.Items.Remove(lbi.Content);
            AktualizujMetriky();
            AktualizujGrafVydavkov();
        }

        private void GrafVydavkov_Loaded(object sender, RoutedEventArgs e) { }

        static string PlanPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BankovaAplikacia");
            Directory.CreateDirectory(dir);
            string emailSafe = App.PrihlasenyEmail.Replace("@", "_at_").Replace(".", "_");
            return Path.Combine(dir, "plan_" + emailSafe + ".json");
        }

        private void BtnUlozPlan_Click(object sender, RoutedEventArgs e)
        {
            var plan = new
            {
                Prijem = MPrijem.Text,
                Nutne  = ZoznamVydavkov.Items  .Cast<object>().Select(i => i.ToString()).ToArray(),
                Hlavne = ZoznamVydavkov2.Items .Cast<object>().Select(i => i.ToString()).ToArray(),
                Osobne = ZoznamVydavkov3.Items .Cast<object>().Select(i => i.ToString()).ToArray(),
                Volne  = ZoznamVydavkov4.Items .Cast<object>().Select(i => i.ToString()).ToArray()
            };

            File.WriteAllText(PlanPath(), JsonSerializer.Serialize(plan,
                new JsonSerializerOptions { WriteIndented = true }));
            MessageBox.Show("Plán bol uložený!");
        }

        private void BtnNacitajPlan_Click(object sender, RoutedEventArgs e)
        {
            string path = PlanPath();
            if (!File.Exists(path)) { MessageBox.Show("Žiadny uložený plán!"); return; }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;

                MPrijem.Text = root.GetProperty("Prijem").GetString() ?? "0";
                MPrijem.Foreground = (Brush)Application.Current.Resources["HlavnyText"];

                void NacitajZoznam(ListBox lb, string klic)
                {
                    lb.Items.Clear();
                    foreach (var item in root.GetProperty(klic).EnumerateArray())
                        lb.Items.Add(item.GetString());
                }

                NacitajZoznam(ZoznamVydavkov,  "Nutne");
                NacitajZoznam(ZoznamVydavkov2, "Hlavne");
                NacitajZoznam(ZoznamVydavkov3, "Osobne");
                NacitajZoznam(ZoznamVydavkov4, "Volne");

                AktualizujMetriky();
                AktualizujGrafVydavkov();
                MessageBox.Show("Plán bol načítaný!");
            }
            catch { MessageBox.Show("Chyba pri načítaní plánu."); }
        }
    }
}
