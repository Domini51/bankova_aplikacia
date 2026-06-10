using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace bankova_aplikacia
{
    public partial class loginWindow : Window
    {
        bool jeLogin = true;

        public loginWindow()
        {
            InitializeComponent();
        }

        bool JeValidnyEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch { return false; }
        }

        void PrepniTab(bool naLogin)
        {
            jeLogin = naLogin;

            PanelLoginBorder.Visibility = naLogin ? Visibility.Visible : Visibility.Collapsed;
            PanelRegisterBorder.Visibility = naLogin ? Visibility.Collapsed : Visibility.Visible;
            ForgotPasswordButton.Visibility = naLogin ? Visibility.Visible : Visibility.Collapsed;

            Button aktivna;
            Button neaktivna;
            if (naLogin)
            {
                aktivna = Login;
                neaktivna = Register;
            }
            else
            {
                aktivna = Register;
                neaktivna = Login;
            }

            aktivna.FontWeight = FontWeights.SemiBold;
            aktivna.Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            aktivna.BorderBrush = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            aktivna.BorderThickness = new Thickness(0, 0, 0, 2);

            neaktivna.FontWeight = FontWeights.Normal;
            neaktivna.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            neaktivna.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            neaktivna.BorderThickness = new Thickness(0, 0, 0, 1);
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            PrepniTab(true);
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            PrepniTab(false);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (jeLogin)
            {
                string gmail = LoginMeno.Text;
                string heslo = LoginHeslo.Password;

                if (string.IsNullOrEmpty(gmail) || string.IsNullOrEmpty(heslo))
                { MessageBox.Show("Vyplň všetky polia!"); return; }

                if (!JeValidnyEmail(gmail))
                { MessageBox.Show("Zadaj platný email!"); return; }

                try
                {
                    bool ok = await Database.Prihlas(gmail, heslo);
                    if (ok)
                    {
                        App.PrihlasenyEmail = gmail;
                        new MainAppWindow().Show();
                        Close();
                    }
                    else MessageBox.Show("Nesprávny email alebo heslo!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Chyba pripojenia k databáze: " + ex.Message);
                }
            }
            else
            {
                string meno = RegisterMeno.Text;
                string gmail = RegisterGmail.Text;
                string heslo = RegisterHeslo.Password;

                if (string.IsNullOrEmpty(meno) || string.IsNullOrEmpty(gmail) || string.IsNullOrEmpty(heslo))
                { MessageBox.Show("Vyplň všetky polia!"); return; }

                if (!JeValidnyEmail(gmail))
                { MessageBox.Show("Zadaj platný email!"); return; }

                try
                {
                    bool ok = await Database.Registruj(meno, gmail, heslo);
                    if (ok)
                    {
                        MessageBox.Show("Registrácia úspešná! Teraz sa prihláste.");
                        PrepniTab(true);
                    }
                    else MessageBox.Show("Tento email už existuje!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Chyba pripojenia k databáze: " + ex.Message);
                }
            }
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Button_Click(sender, e);
        }

        private void LoginHeslo_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (LoginHesloHint != null)
            {
                if (LoginHeslo.Password.Length == 0)
                    LoginHesloHint.Visibility = Visibility.Visible;
                else
                    LoginHesloHint.Visibility = Visibility.Collapsed;
            }
        }

        private void RegisterHeslo_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (RegisterHesloHint != null)
            {
                if (RegisterHeslo.Password.Length == 0)
                    RegisterHesloHint.Visibility = Visibility.Visible;
                else
                    RegisterHesloHint.Visibility = Visibility.Collapsed;
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string gmail = Microsoft.VisualBasic.Interaction.InputBox("Zadaj svoj email:", "Reset hesla", "");
            if (string.IsNullOrEmpty(gmail)) return;

            if (!JeValidnyEmail(gmail))
            { MessageBox.Show("Zadaj platný email!"); return; }

            try
            {
                bool existuje = await Database.EmailExistuje(gmail);
                if (!existuje)
                { MessageBox.Show("Email neexistuje!"); return; }

                string noveHeslo = Microsoft.VisualBasic.Interaction.InputBox("Zadaj nové heslo:", "Reset hesla", "");
                if (string.IsNullOrEmpty(noveHeslo)) return;

                bool zmenene = await Database.ZmenHeslo(gmail, noveHeslo);
                if (zmenene)
                    MessageBox.Show("Heslo bolo úspešne zmenené!");
                else
                    MessageBox.Show("Chyba pri zmene hesla!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pripojenia k databáze: " + ex.Message);
            }
        }
    }
}
