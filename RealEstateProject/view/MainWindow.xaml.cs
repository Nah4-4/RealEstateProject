using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;

namespace RealEstateProject.view
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static int? UserId { get; set; } // Static property to store user ID

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new HomePage());
        }
  
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }  
        private void btnProfile_Click(object sender, RoutedEventArgs e)
        {
            if (UserId>0)
                MainFrame.Navigate(new ProfilePage(UserId.Value));
            else
            {
                LoginView loginView = new LoginView();
                loginView.Show();
            }

        }
        public void gotoprof()
        {
            MainFrame.Navigate(new ProfilePage(UserId.Value));
        }
    }
}