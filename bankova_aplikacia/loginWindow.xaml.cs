using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace bankova_aplikacia
{
    public partial class loginWindow : Window
    {
        // -- sleduje ci je aktivny login alebo register panel --
        private bool _jeLogin = true;

        public loginWindow()
        {
            InitializeComponent();
        }

        private bool JeValidnyEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            _jeLogin = true;
            PanelLoginBorder.Visibility = Visibility.Visible;
            ForgotPasswordButton.Visibility = Visibility.Visible;
            PanelRegisterBorder.Visibility = Visibility.Collapsed;
            Login.FontWeight = FontWeights.SemiBold;
            Login.Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Login.BorderBrush = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Login.BorderThickness = new Thickness(0, 0, 0, 2);
            Register.FontWeight = FontWeights.Normal;
            Register.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            Register.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            Register.BorderThickness = new Thickness(0, 0, 0, 1);
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            _jeLogin = false;
            PanelLoginBorder.Visibility = Visibility.Collapsed;
            ForgotPasswordButton.Visibility = Visibility.Collapsed;
            PanelRegisterBorder.Visibility = Visibility.Visible;
            Register.FontWeight = FontWeights.SemiBold;
            Register.Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Register.BorderBrush = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Register.BorderThickness = new Thickness(0, 0, 0, 2);
            Login.FontWeight = FontWeights.Normal;
            Login.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            Login.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            Login.BorderThickness = new Thickness(0, 0, 0, 1);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_jeLogin)
            {
                string gmail = LoginMeno.Text;
                string heslo = LoginHeslo.Password;
                if (string.IsNullOrEmpty(gmail) || string.IsNullOrEmpty(heslo))
                {
                    MessageBox.Show("Vyplň všetky polia!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!JeValidnyEmail(gmail))
                {
                    MessageBox.Show("Zadaj platný email!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool uspech = await Database.Prihlas(gmail, heslo);
                if (uspech)
                {
                    App.PrihlasenyEmail = gmail;
                    MainAppWindow main = new MainAppWindow();
                    main.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Nesprávny email alebo heslo!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                string meno = RegisterMeno.Text;
                string gmail = RegisterGmail.Text;
                string heslo = RegisterHeslo.Password;
                if (string.IsNullOrEmpty(meno) || string.IsNullOrEmpty(gmail) || string.IsNullOrEmpty(heslo))
                {
                    MessageBox.Show("Vyplň všetky polia!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!JeValidnyEmail(gmail))
                {
                    MessageBox.Show("Zadaj platný email!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool uspech = await Database.Registruj(meno, gmail, heslo);
                if (uspech)
                {
                    MessageBox.Show("Registrácia úspešná! Teraz sa prihláste.", "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);
                    BtnLogin_Click(null!, null!);
                }
                else
                {
                    MessageBox.Show("Tento email už existuje!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoginHeslo_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (LoginHesloHint != null)
                LoginHesloHint.Visibility = LoginHeslo.Password.Length == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void RegisterHeslo_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (RegisterHesloHint != null)
                RegisterHesloHint.Visibility = RegisterHeslo.Password.Length == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string gmail = Microsoft.VisualBasic.Interaction.InputBox(
                "Zadaj svoj email:", "Reset hesla", "");

            if (string.IsNullOrEmpty(gmail))
                return;

            if (!JeValidnyEmail(gmail))
            {
                MessageBox.Show("Zadaj platný email!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool existuje = await Database.EmailExistuje(gmail);
            if (!existuje)
            {
                MessageBox.Show("Email neexistuje!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string noveHeslo = Microsoft.VisualBasic.Interaction.InputBox(
                "Zadaj nové heslo:", "Reset hesla", "");

            if (string.IsNullOrEmpty(noveHeslo))
                return;

            bool uspech = await Database.ZmenHeslo(gmail, noveHeslo);
            if (uspech)
                MessageBox.Show("Heslo bolo úspešne zmenené!", "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Chyba pri zmene hesla!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}