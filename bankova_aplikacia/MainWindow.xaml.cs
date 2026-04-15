using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace bankova_aplikacia
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string nazov = NazovV1.Text;
            string sumaText = Suma1.Text;

            double suma;

            if (string.IsNullOrWhiteSpace(nazov))
                return;

            if (!double.TryParse(sumaText, out suma))
            {
                MessageBox.Show("Zadaj platnú číselnú hodnotu!");
                return;
            }

            ZoznamVydavkov.Items.Add($"{nazov} - {suma} €");

            NazovV1.Clear();
            Suma1.Clear();

            if (string.IsNullOrWhiteSpace(NazovV1.Text))
            {
                NazovV1.Text = "Názov výdavku";
                NazovV1.Foreground = Brushes.Gray;
            }
            if (string.IsNullOrWhiteSpace(Suma1.Text))
            {
                Suma1.Text = "0";
                Suma1.Foreground = Brushes.Gray;
            }
        }
        private void NazovBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (NazovV1.Text == "Názov výdavku")
            {
                NazovV1.Text = "";
                NazovV1.Foreground = Brushes.Black;
            }
        }

        private void NazovBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NazovV1.Text))
            {
                NazovV1.Text = "Názov výdavku";
                NazovV1.Foreground = Brushes.Gray;
            }
        }

        private void SumaBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Suma1.Text == "0")
            {
                Suma1.Text = "";
                Suma1.Foreground = Brushes.Black;
            }
        }

        private void SumaBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Suma1.Text))
            {
                Suma1.Text = "0";
                Suma1.Foreground = Brushes.Gray;
            }
        }
    }
}