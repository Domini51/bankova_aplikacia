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

        private void BtnPridajVydavok_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            TextBox nazovBox;
            TextBox sumaBox;
            ListBox zoznam;

            if (!(btn.Parent is Grid grid))
                return;

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

            string nazov = nazovBox.Text;
            string sumaText = sumaBox.Text;

            if (string.IsNullOrWhiteSpace(nazov) || nazov == "Názov výdavku")
                return;

            double suma;
            bool ok = double.TryParse(sumaText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out suma);

            if (!ok)
            {
                MessageBox.Show("Zadaj platnú číselnú hodnotu!");
                return;
            }

            double celkom = SpocitajVsetkyVydavky();
            double prijem;
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out prijem);

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

        public double SpocitajVsetkyVydavky()
        {
            double celkom = 0;

            for (int i = 0; i < ZoznamVydavkov.Items.Count; i++)
                celkom += ParseSuma(ZoznamVydavkov.Items[i].ToString());

            for (int i = 0; i < ZoznamVydavkov2.Items.Count; i++)
                celkom += ParseSuma(ZoznamVydavkov2.Items[i].ToString());

            for (int i = 0; i < ZoznamVydavkov3.Items.Count; i++)
                celkom += ParseSuma(ZoznamVydavkov3.Items[i].ToString());

            for (int i = 0; i < ZoznamVydavkov4.Items.Count; i++)
                celkom += ParseSuma(ZoznamVydavkov4.Items[i].ToString());

            return celkom;
        }

        private double ParseSuma(string? text)
        {
            if (text == null)
                return 0;

            string[] parts = text.Split('-');

            if (parts.Length < 2)
                return 0;

            string sumaStr = parts[parts.Length - 1].Replace("€", "").Trim();

            double vysledok;
            double.TryParse(sumaStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out vysledok);

            return vysledok;
        }

        public void AktualizujMetriky()
        {
            double prijem;
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out prijem);

            double celkom = SpocitajVsetkyVydavky();
            double zostatok = prijem - celkom;

            MetPrijem.Text = prijem.ToString("F2") + " €";
            MetMinute.Text = celkom.ToString("F2") + " €";

            App.AktualnyZostatok = zostatok;
            App.AktualnyPrijem = prijem;
        }

        private async void BtnUlozZostatokNaUcet_Click(object sender, RoutedEventArgs e)
        {
            double prijem;
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out prijem);

            double celkom = SpocitajVsetkyVydavky();
            double zostatok = prijem - celkom;

            if (zostatok <= 0)
            {
                MessageBox.Show("Nemáš žiadny zostatok na uloženie!");
                return;
            }

            double aktualny = await Database.NacitajZostatok(App.PrihlasenyEmail);
            double novy = aktualny + zostatok;
            await Database.UlozZostatok(App.PrihlasenyEmail, novy);

            MessageBox.Show("Na účet bolo pripísaných " + zostatok.ToString("F2") + " €\nCelkový zostatok: " + novy.ToString("F2") + " €");
        }

        private void AktualizujGrafVydavkov()
        {
            double nutne = 0;
            double hlavne = 0;
            double osobne = 0;
            double volne = 0;

            for (int i = 0; i < ZoznamVydavkov.Items.Count; i++)
                nutne += ParseSuma(ZoznamVydavkov.Items[i].ToString());

            for (int i = 0; i < ZoznamVydavkov2.Items.Count; i++)
                hlavne += ParseSuma(ZoznamVydavkov2.Items[i].ToString());

            for (int i = 0; i < ZoznamVydavkov3.Items.Count; i++)
                osobne += ParseSuma(ZoznamVydavkov3.Items[i].ToString());

            for (int i = 0; i < ZoznamVydavkov4.Items.Count; i++)
                volne += ParseSuma(ZoznamVydavkov4.Items[i].ToString());

            if (nutne + hlavne + osobne + volne == 0)
                return;

            var serie = new List<ISeries>();

            var s1 = new PieSeries<double>();
            s1.Values = new double[] { nutne };
            s1.Name = "Nutné";
            s1.Fill = new SolidColorPaint(SKColor.Parse("#E24B4A"));
            serie.Add(s1);

            var s2 = new PieSeries<double>();
            s2.Values = new double[] { hlavne };
            s2.Name = "Hlavné";
            s2.Fill = new SolidColorPaint(SKColor.Parse("#2E6DA4"));
            serie.Add(s2);

            var s3 = new PieSeries<double>();
            s3.Values = new double[] { osobne };
            s3.Name = "Osobné";
            s3.Fill = new SolidColorPaint(SKColor.Parse("#32B432"));
            serie.Add(s3);

            var s4 = new PieSeries<double>();
            s4.Values = new double[] { volne };
            s4.Name = "Voľné";
            s4.Fill = new SolidColorPaint(SKColor.Parse("#FFA500"));
            serie.Add(s4);

            GrafVydavkov.Series = serie;
        }

        private void NazovBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (box.Text == "Názov výdavku")
            {
                box.Text = "";
                box.Foreground = Brushes.Black;
            }
        }

        private void NazovBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(box.Text))
            {
                box.Text = "Názov výdavku";
                box.Foreground = Brushes.Gray;
            }
        }

        private void SumaBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (box.Text == "0")
            {
                box.Text = "";
                box.Foreground = Brushes.Black;
            }
        }

        private void SumaBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(box.Text))
            {
                box.Text = "0";
                box.Foreground = Brushes.Gray;
            }
        }

        private void MPrijem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                AktualizujMetriky();
        }

        private void MPrijem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tb = sender as TextBox;
                if (tb != null)
                    tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void NazovVydavok_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            TextBox tb = sender as TextBox;
            if (tb == null) return;

            Grid grid = tb.Parent as Grid;
            if (grid == null) return;

            TextBox sumaBox = null;
            for (int i = 0; i < grid.Children.Count; i++)
            {
                if (grid.Children[i] is TextBox t && t != tb)
                {
                    sumaBox = t;
                    break;
                }
            }

            if (sumaBox != null)
            {
                sumaBox.Focus();
                sumaBox.SelectAll();
            }
        }

        private void SumaVydavok_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            TextBox tb = sender as TextBox;
            if (tb == null) return;

            Grid grid = tb.Parent as Grid;
            if (grid == null) return;

            Button btn = null;
            for (int i = 0; i < grid.Children.Count; i++)
            {
                if (grid.Children[i] is Button b)
                {
                    btn = b;
                    break;
                }
            }

            if (btn != null)
                btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void GrafVydavkov_Loaded(object sender, RoutedEventArgs e) { }
    }
}