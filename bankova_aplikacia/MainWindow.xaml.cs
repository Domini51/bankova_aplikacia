using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace bankova_aplikacia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // tlacidlo + ktore
            Button btn = (Button)sender;
            TextBox nazovBox;
            TextBox sumaBox;
            ListBox zoznam;

            if (btn.Parent is Grid grid)
            {
                //  spravny nazov a suma box 
                if (grid.Children.Contains(NazovV1))
                {
                    nazovBox = NazovV1; sumaBox = Suma1; zoznam = ZoznamVydavkov;
                }
                else if (grid.Children.Contains(NazovV1_Copy))
                {
                    nazovBox = NazovV1_Copy; sumaBox = Suma1_Copy; zoznam = ZoznamVydavkov_Copy;
                }
                else if (grid.Children.Contains(NazovV1_Copy1))
                {
                    nazovBox = NazovV1_Copy1; sumaBox = Suma1_Copy1; zoznam = ZoznamVydavkov_Copy1;
                }
                else
                {
                    nazovBox = NazovV1_Copy2; sumaBox = Suma1_Copy2; zoznam = ZoznamVydavkov_Copy2;
                }
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

            double prijem = 0;
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out prijem);

            double celkom = 0;
            foreach (var item in ZoznamVydavkov.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy1.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy2.Items) celkom += ParseSuma(item.ToString());

            if (celkom + suma > prijem)
            {
                MessageBox.Show($"Nemáš dostatok peňazí! Zostatok: {prijem - celkom:F2} €", "Nedostatok financií", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            zoznam.Items.Add($"{nazov} - {suma} €");

            nazovBox.Text = "Názov výdavku";
            nazovBox.Foreground = Brushes.Gray;
            sumaBox.Text = "0";
            sumaBox.Foreground = Brushes.Gray;
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

        private void BtnVypocitat_Click(object sender, RoutedEventArgs e)
        {
            double prijem = 0;
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out prijem);

            double celkom = 0;

            foreach (var item in ZoznamVydavkov.Items)
                celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy.Items)
                celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy1.Items)
                celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy2.Items)
                celkom += ParseSuma(item.ToString());

            double zostatok = prijem - celkom;

            TxtMinute.Text = $"{celkom:F2} €";
            TxtZostatok.Text = $"{zostatok:F2} €";

            // farba podla zostatku
            double percento = prijem > 0 ? (celkom / prijem) : 0;
            if (percento >= 0.85)
                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 50));
            else if (percento >= 0.6)
                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0));
            else
                ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(50, 180, 50));

            ProgressBar.Value = Math.Min(percento * 100, 100);
        }

        private double ParseSuma(string? item)
        {
            if (item == null) return 0;
            // format je "Nazov - 123 €"
            var parts = item.Split('-');
            if (parts.Length < 2) return 0;
            var sumaStr = parts[^1].Replace("€", "").Trim();
            double.TryParse(sumaStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out double suma);
            return suma;
        }
        private void BtnInvestovat_Click(object sender, RoutedEventArgs e)
        {
            double prijem = 0;
            double.TryParse(MPrijem.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out prijem);

            double celkom = 0;
            foreach (var item in ZoznamVydavkov.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy1.Items) celkom += ParseSuma(item.ToString());
            foreach (var item in ZoznamVydavkov_Copy2.Items) celkom += ParseSuma(item.ToString());

            double zostatok = prijem - celkom;

            InvestWindow investWindow = new InvestWindow(zostatok, prijem);
            investWindow.Show();
            this.Close();
        }

        private void MPrijem_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}

