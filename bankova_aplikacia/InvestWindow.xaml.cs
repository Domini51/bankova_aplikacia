using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace bankova_aplikacia
{
    public partial class loginWindow : Window
    {
        public loginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            MainButton.Content = "Login";
            PanelLoginBorder.Visibility = Visibility.Visible;
            PanelRegisterBorder.Visibility = Visibility.Collapsed;
            Login.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            Login.Foreground = Brushes.White;
            Register.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Register.Foreground = Brushes.White;
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            MainButton.Content = "Registrovať";
            PanelLoginBorder.Visibility = Visibility.Collapsed;
            PanelRegisterBorder.Visibility = Visibility.Visible;
            Register.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            Register.Foreground = Brushes.White;
            Login.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Login.Foreground = Brushes.White;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MainButton.Content.ToString() == "Login")
            {
                string gmail = LoginMeno.Text;
                string heslo = LoginHeslo.Password;

                if (string.IsNullOrEmpty(gmail) || string.IsNullOrEmpty(heslo))
                {
                    MessageBox.Show("Vyplň všetky polia!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool uspech = await Database.Prihlas(gmail, heslo);
                if (uspech)
                {
                    MainWindow main = new MainWindow();
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