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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = new MainWindow();
            main.Show();
            this.Close();
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

        private void LoginMeno_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }
    }
}