using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace bankova_aplikacia
{
    public partial class Nastavenia : UserControl
    {
        public Nastavenia()
        {
            InitializeComponent();
        }

        // -- nacita meno a email prihlaseneho uzivatela --
        public async Task NacitajUdaje()
        {
            string meno = await Database.NacitajMeno(App.PrihlasenyEmail);
            TxtMeno.Text = meno;
            TxtEmail.Text = App.PrihlasenyEmail;
        }

        // -- zmeni meno uzivatela v databaze --
        private async void BtnZmenitMeno_Click(object sender, RoutedEventArgs e)
        {
            string noveMeno = Microsoft.VisualBasic.Interaction.InputBox(
                "Zadaj nové meno:", "Zmena mena", "");

            if (string.IsNullOrEmpty(noveMeno)) return;

            bool uspech = await Database.ZmenMeno(App.PrihlasenyEmail, noveMeno);
            if (uspech)
            {
                TxtMeno.Text = noveMeno;
                MessageBox.Show("Meno bolo úspešne zmenené!", "Úspech",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // -- zmeni heslo uzivatela v databaze --
        private async void BtnZmenitHeslo_Click(object sender, RoutedEventArgs e)
        {
            string noveHeslo = Microsoft.VisualBasic.Interaction.InputBox(
                "Zadaj nové heslo:", "Zmena hesla", "");

            if (string.IsNullOrEmpty(noveHeslo)) return;

            bool uspech = await Database.ZmenHeslo(App.PrihlasenyEmail, noveHeslo);
            if (uspech)
                MessageBox.Show("Heslo bolo úspešne zmenené!", "Úspech",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}