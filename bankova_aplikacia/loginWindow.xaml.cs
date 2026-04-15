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
            PanelLoginBorder.Visibility = Visibility.Visible;
            PanelRegisterBorder.Visibility = Visibility.Collapsed;

            // Login je aktivny - svetlejsi
            Login.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            Login.Foreground = Brushes.White;

            // Register je neaktivny - tmavy
            Register.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Register.Foreground = Brushes.White;
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            MainButton.Content = "Registrovať";
            PanelLoginBorder.Visibility = Visibility.Collapsed;
            PanelRegisterBorder.Visibility = Visibility.Visible;

            // Register je aktivny - svetlejsi
            Register.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            Register.Foreground = Brushes.White;

            // Login je neaktivny - tmavy
            Login.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Login.Foreground = Brushes.White;
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