using System;
using System.Collections.Generic;
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
            IsVisibleChanged += (_, e) => { if ((bool)e.NewValue) AktualizujGrafVydavkov(); };
        }

        private void BtnPridajVydavok_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            Grid grid = btn.Parent as Grid;
            if (grid == null) return;

            TextBox nazovBox = null;
            TextBox sumaBox = null;
            ListBox zoznam = null;

            if (grid.Children.Contains(NazovV1))
            {
                nazovBox = NazovV1;
                sumaBox = Suma1;
                zoznam = ZoznamVydavkov;
            }
            else if (grid.Children.Contains(NazovV2))
            {
                nazovBox = NazovV2;
                sumaBox = Suma2;
                zoznam = ZoznamVydavkov2;
            }
            else if (grid.Children.Contains(NazovV3))
            {
                nazovBox = NazovV3;
                sumaBox = Suma3;
                zoznam = ZoznamVydavkov3;
            }
            else
            {
                nazovBox = NazovV4;
                sumaBox = Suma4;
                zoznam = ZoznamVydavkov4;
            }

            if (nazovBox == null) return;

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

        double ParseSuma(string text)
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

            var zaznam = new Dictionary<string, object>();
            zaznam["Gmail"]  = App.PrihlasenyEmail;
            zaznam["Datum"]  = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            zaznam["Typ"]    = "Zostatok";
            zaznam["Celkom"] = zostatok.ToString("F2") + " €";
            await Database.UlozHistoriu(App.PrihlasenyEmail, zaznam);

            MessageBox.Show("Na účet bolo pripísaných " + zostatok.ToString("F2") + " €\nCelkový zostatok: " + novy.ToString("F2") + " €");

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
                var ps = new PieSeries<double>();
                ps.Values = new double[] { hodnoty[i] };
                ps.Name = nazvy[i];
                ps.Fill = new SolidColorPaint(SKColor.Parse(farby[i]));
                serie.Add(ps);
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
            if (grid == null) return;
            foreach (var child in grid.Children)
            {
                if (child is Button btn) { btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); break; }
            }
        }

        private void BtnOdstranVydavok_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject p = (Button)sender;
            while (p != null && p is not ListBoxItem) p = System.Windows.Media.VisualTreeHelper.GetParent(p);
            var lbi = p as ListBoxItem;
            if (lbi == null) return;

            var lb = ItemsControl.ItemsControlFromItemContainer(lbi) as ListBox;
            if (lb == null) return;

            lb.Items.Remove(lbi.Content);
            AktualizujMetriky();
            AktualizujGrafVydavkov();
        }

        private void GrafVydavkov_Loaded(object sender, RoutedEventArgs e) { }
    }
}
