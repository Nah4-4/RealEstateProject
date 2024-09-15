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

namespace RealEstateProject.view
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<House> Houses { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Houses = new ObservableCollection<House>
            {
                new House { ImagePath = "/Images/whouse.jpg", HouseId = "House1" },
                new House { ImagePath = "/Images/images.jpg", HouseId = "House1" },
                new House { ImagePath = "/Images/whouse.jpg", HouseId = "House1" },

            };

            // Set DataContext to bind data to the UI
            DataContext = this;
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("11");
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginView loginView = new LoginView();
            loginView.Show();
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState=WindowState.Maximized;
        }

    }
}