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

        public async Task NacitajUdaje()
        {
            string meno = await Database.NacitajMeno(App.PrihlasenyEmail);
            TxtMeno.Text = meno;
            TxtEmail.Text = App.PrihlasenyEmail;
            TxtInitials.Text = meno.Length > 0 ? meno[0].ToString().ToUpper() : "?";
            
        }

      

        private async void BtnZmenitMeno_Click(object sender, RoutedEventArgs e)
        {
            string noveMeno = Microsoft.VisualBasic.Interaction.InputBox("Zadaj nové meno:", "Zmena mena", "");
            if (string.IsNullOrEmpty(noveMeno)) return;

            if (await Database.ZmenMeno(App.PrihlasenyEmail, noveMeno))
            {
                TxtMeno.Text = noveMeno;
                TxtInitials.Text = noveMeno.Length > 0 ? noveMeno[0].ToString().ToUpper() : "?";
                MessageBox.Show("Meno bolo úspešne zmenené!");
            }
        }

        private async void BtnZmenitHeslo_Click(object sender, RoutedEventArgs e)
        {
            string noveHeslo = Microsoft.VisualBasic.Interaction.InputBox("Zadaj nové heslo:", "Zmena hesla", "");
            if (string.IsNullOrEmpty(noveHeslo)) return;

            if (await Database.ZmenHeslo(App.PrihlasenyEmail, noveHeslo))
                MessageBox.Show("Heslo bolo úspešne zmenené!");
        }

       
    }
}
