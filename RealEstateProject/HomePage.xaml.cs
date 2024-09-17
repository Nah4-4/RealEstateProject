using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using RealEstateProject.view;

namespace RealEstateProject
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        int propertyId;
        private int? userId;
        private string connectionString = "server=localhost;uid=root;pwd=;database=TestDB";
        public ObservableCollection<House> Houses { get; set; }

        public HomePage(int? userId = null)
        {
            this.userId = userId;
            InitializeComponent();
            Houses = new ObservableCollection<House>();
            LoadProperties();
            // Set DataContext to bind data to the UI
            DataContext = this;
        }

        private void LoadProperties()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                // Fetch each property along with its first image
                string query = @"
                    SELECT p.property_id, p.title, COALESCE(pi.image_url, 'C:/RealEstateImages/placeholder.png') AS image_url,price
                    FROM Property p
                    LEFT JOIN (
                        SELECT property_id, MIN(image_url) AS image_url
                        FROM PropertyImages
                        GROUP BY property_id
                    ) pi ON p.property_id = pi.property_id";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        propertyId = reader.GetInt32("property_id");
                        string title = reader.GetString("title");
                        string imageUrl = reader.GetString("image_url");
                        int price = reader.GetInt32("price");
                        AddProperties(propertyId, title, imageUrl,price);
                    }
                }
            }
        }

        private void AddProperties(int propertyId, string title, string imageUrl,int price )
        {
            Houses.Add(new House { ImagePath = imageUrl, Title = title, PropertyId = propertyId,Price=price });
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Border clickedBorder = sender as Border;
            if (clickedBorder != null)
            {
                // Retrieve the propertyId from the Tag
                int propertyId = (int)clickedBorder.Tag;
                // Navigate to the DetailsPage
                if(userId.HasValue)
                    this.NavigationService.Navigate(new DetailsPage(propertyId, userId.Value));
                else
                    this.NavigationService.Navigate(new DetailsPage(propertyId,0));
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                if (window.WindowState == WindowState.Maximized)
                    window.WindowState = WindowState.Normal;
                else
                    window.WindowState = WindowState.Maximized;
            }
        }

    }
}
