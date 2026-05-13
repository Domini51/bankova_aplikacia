using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
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

        // -- prida vydavok do prislusneho zoznamu --
        private void BtnPridajVydavok_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            TextBox nazovBox;
            TextBox sumaBox;
            ListBox zoznam;

            if (btn.Parent is Grid grid)
            {
                if (grid.Children.Contains(NazovV1))
                { nazovBox = NazovV1; sumaBox = Suma1; zoznam = ZoznamVydavkov; }
                else if (grid.Children.Contains(NazovV2))
                { nazovBox = NazovV2; sumaBox = Suma2; zoznam = ZoznamVydavkov2; }
                else if (grid.Children.Contains(NazovV3))
                { nazovBox = NazovV3; sumaBox = Suma3; zoznam = ZoznamVydavkov3; }
                else
                { nazovBox = NazovV4; sumaBox = Suma4; zoznam = ZoznamVydavkov4; }
            }
            else return;

            string nazov = nazovBox.Text;
            string sumaText = sumaBox.Text;

            if (string.IsNullOrWhiteSpace(nazov) || nazov == "Názov výdavku")
                return;

            if (!double.TryParse(sumaText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double suma))
            {
                MessageBox.Show("Zadaj platnú číselnú hodnotu!");
                return;
            }

            double celkom = SpocitajVsetkyVydavky();
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double prijem);

            if (celkom + suma > prijem)
            {
                MessageBox.Show($"Nemáš dostatok peňazí! Zostatok: {prijem - celkom:F2} €",
                    "Nedostatok financií", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            zoznam.Items.Add($"{nazov} - {suma} €");

            nazovBox.Text = "Názov výdavku";
            nazovBox.Foreground = Brushes.Gray;
            sumaBox.Text = "0";
            sumaBox.Foreground = Brushes.Gray;

            AktualizujMetriky();
            AktualizujGrafVydavkov();
        }

        // -- odstrani vydavok zo zoznamu po kliknuti na X --
        private void BtnOdstranVydavok_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string? polozka = btn.Tag?.ToString();

            // -- zisti z ktoreho zoznamu polozka pochodzi --
            if (ZoznamVydavkov.Items.Contains(polozka))
                ZoznamVydavkov.Items.Remove(polozka);
            else if (ZoznamVydavkov2.Items.Contains(polozka))
                ZoznamVydavkov2.Items.Remove(polozka);
            else if (ZoznamVydavkov3.Items.Contains(polozka))
                ZoznamVydavkov3.Items.Remove(polozka);
            else if (ZoznamVydavkov4.Items.Contains(polozka))
                ZoznamVydavkov4.Items.Remove(polozka);

            // -- prepocitaj metriky po odstraneni --
            AktualizujMetriky();
            AktualizujGrafVydavkov();
        }

        // -- spocita vsetky vydavky zo vsetkych zoznamov --
        public double SpocitajVsetkyVydavky()
        {
            double celkom = 0;
            foreach (var item in ZoznamVydavkov.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov2.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov3.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov4.Items) celkom += ParseSuma(item.ToString());
            return celkom;
        }

        // -- rozparsuje sumu z textu ako "Nazov - 50 €" --
        private double ParseSuma(string? item)
        {
            if (item == null) return 0;
            var parts = item.Split('-');
            if (parts.Length < 2) return 0;
            var sumaStr = parts[^1].Replace("€", "").Trim();
            double.TryParse(sumaStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double suma);
            return suma;
        }

        // -- aktualizuje textbloky s prijem a celkove minute --
        public void AktualizujMetriky()
        {
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double prijem);

            double celkom = SpocitajVsetkyVydavky();
            double zostatok = prijem - celkom;

            MetPrijem.Text = $"{prijem:F2} €";
            MetMinute.Text = $"{celkom:F2} €";

            // -- ulozime zostatok a prijem do App aby ho videli aj ine panely --
            App.AktualnyZostatok = zostatok;
            App.AktualnyPrijem = prijem;
        }

        // -- ulozi zostatok na ucet do databazy a vymaze vsetky vydavky --
        private async void BtnUlozZostatokNaUcet_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double prijem);
            double celkom = SpocitajVsetkyVydavky();
            double zostatok = prijem - celkom;

            if (zostatok <= 0)
            {
                MessageBox.Show("Nemáš žiadny zostatok na uloženie!", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double aktualnyZostatok = await Database.NacitajZostatok(App.PrihlasenyEmail);
            double novyZostatok = aktualnyZostatok + zostatok;
            await Database.UlozZostatok(App.PrihlasenyEmail, novyZostatok);

            MessageBox.Show($"Na účet bolo pripísaných {zostatok:F2} €\nCelkový zostatok: {novyZostatok:F2} €",
                "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);

            // -- vymaz vsetky vydavky a vynuluj prijem po ulozeni --
            ZoznamVydavkov.Items.Clear();
            ZoznamVydavkov2.Items.Clear();
            ZoznamVydavkov3.Items.Clear();
            ZoznamVydavkov4.Items.Clear();

            MPrijem.Text = "0";
            MPrijem.Foreground = Brushes.Gray;

            // -- aktualizuj metriky a vymaz graf po ulozeni --
            AktualizujMetriky();
            GrafVydavkov.Series = null;
        }

        // -- aktualizuje kruhovy graf vydavkov --
        private void AktualizujGrafVydavkov()
        {
            double nutne = 0, hlavne = 0, osobne = 0, volne = 0;
            foreach (var item in ZoznamVydavkov.Items) nutne += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov2.Items) hlavne += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov3.Items) osobne += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov4.Items) volne += ParseSuma(item.ToString());

            if (nutne + hlavne + osobne + volne == 0) return;

            GrafVydavkov.Series = new ISeries[]
            {
                new PieSeries<double> { Values = new double[] { nutne }, Name = "Nutné", Fill = new SolidColorPaint(SKColor.Parse("#E24B4A")) },
                new PieSeries<double> { Values = new double[] { hlavne }, Name = "Hlavné", Fill = new SolidColorPaint(SKColor.Parse("#2E6DA4")) },
                new PieSeries<double> { Values = new double[] { osobne }, Name = "Osobné", Fill = new SolidColorPaint(SKColor.Parse("#32B432")) },
                new PieSeries<double> { Values = new double[] { volne }, Name = "Voľné", Fill = new SolidColorPaint(SKColor.Parse("#FFA500")) }
            };
        }

        // -- vymaze placeholder text ked uzivatel klikne na pole nazov --
        private void NazovBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (box.Text == "Názov výdavku") { box.Text = ""; box.Foreground = Brushes.Black; }
        }

        // -- obnovi placeholder text ak pole ostane prazdne --
        private void NazovBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(box.Text)) { box.Text = "Názov výdavku"; box.Foreground = Brushes.Gray; }
        }

        // -- vymaze nulu ked uzivatel klikne na pole sumy --
        private void SumaBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (box.Text == "0") { box.Text = ""; box.Foreground = Brushes.Black; }
        }

        // -- obnovi nulu ak pole sumy ostane prazdne --
        private void SumaBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(box.Text)) { box.Text = "0"; box.Foreground = Brushes.Gray; }
        }

        // -- pri zmene prijmu prepocita metriky --
        private void MPrijem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) AktualizujMetriky();
        }

        // -- enter v poli prijmu presunie focus dalej --
        private void MPrijem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (sender is TextBox tb)
                    tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        // -- enter v poli nazov presunie focus na pole sumy --
        private void NazovVydavok_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is not TextBox tb) return;
                if (tb.Parent is not Grid grid) return;
                var sumaBox = grid.Children.OfType<TextBox>().FirstOrDefault(x => x != tb);
                sumaBox?.Focus();
                sumaBox?.SelectAll();
            }
        }

        // -- enter v poli sumy klikne tlacidlo pridat --
        private void SumaVydavok_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is not TextBox tb) return;
                if (tb.Parent is not Grid grid) return;
                var btn = grid.Children.OfType<Button>().FirstOrDefault();
                btn?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        // -- prazdna metoda pre event loaded grafu --
        private void GrafVydavkov_Loaded(object sender, RoutedEventArgs e) { }
    }
}