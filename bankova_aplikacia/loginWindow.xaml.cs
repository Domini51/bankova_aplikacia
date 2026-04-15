using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace bankova_aplikacia
{
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            Database.Init();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            MainButton.Content = "Login";
            PanelLogin.Visibility = Visibility.Visible;
            PanelRegister.Visibility = Visibility.Collapsed;
            Login.Background = new SolidColorBrush(Color.FromRgb(30, 200, 255));
            Register.Background = new SolidColorBrush(Color.FromRgb(20, 155, 210));
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            MainButton.Content = "Register";
            PanelLogin.Visibility = Visibility.Collapsed;
            PanelRegister.Visibility = Visibility.Visible;
            Register.Background = new SolidColorBrush(Color.FromRgb(30, 200, 255));
            Login.Background = new SolidColorBrush(Color.FromRgb(20, 155, 210));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MainButton.Content.ToString() == "Login")
            {
                string gmail = LoginMeno.Text;
                string heslo = LoginHeslo.Password;

                if (Database.Prihlas(gmail, heslo))
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

                if (Database.Registruj(meno, gmail, heslo))
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
    }
}