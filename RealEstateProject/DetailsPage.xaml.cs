using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace RealEstateProject
{
    /// <summary>
    /// Interaction logic for DetailsPage.xaml
    /// </summary>
    public partial class DetailsPage : Page
    {
        int userId;
        public ObservableCollection<Housedetails> housedetails { get; set; }

        private int propertyId;
        private int currentImageIndex = 0;
        private List<string> imageUrls = new List<string>();

        private string connectionString = "server=localhost;uid=root;pwd=ushallpass44;database=TestDB";
        public DetailsPage(int propertyId,int userId)
        {
            this.userId = userId;
            InitializeComponent();
            housedetails = new ObservableCollection<Housedetails>();
            LoadPropertyDetails(propertyId);
            if (housedetails.Count > 0)
            {
                DataContext = housedetails[0];
            }
        }

        string  imageUrl,labelTitle, labelPrice, labelBedrooms, labelBathrooms, labelSize, labelCity, labelDescription, labelSellerPhone, labelSellerName;

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            currentImageIndex = (currentImageIndex + 1) % imageUrls.Count;
            //imageUrl=imageUrls[currentImageIndex];
            housedetails[0].ImagePath = imageUrls[currentImageIndex];

        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new HomePage(userId));
        }

        private void LoadPropertyDetails(int propertyId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT p.title, p.price, p.city, p.description, p.number_of_bedrooms, " +
                               "p.number_of_bathrooms, p.size_in_sqft, u.name as seller_name, u.phone_number as seller_phone, i.image_url " +
                               "FROM Property p " +
                               "JOIN Users u ON p.user_id = u.user_id " +
                               "LEFT JOIN PropertyImages i ON p.property_id = i.property_id " +
                               "WHERE p.property_id = @propertyId";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@propertyId", propertyId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Set property details
                            labelTitle = "Title: " + reader["title"].ToString();
                            //imageUrl = reader.GetString("image_url");
                            labelPrice = "$" + reader["price"].ToString();
                            labelBedrooms = "Bedrooms: " + reader["number_of_bedrooms"].ToString();
                            labelBathrooms = "Bathrooms: " + reader["number_of_bathrooms"].ToString();
                            labelSize = "Size: " + reader["size_in_sqft"].ToString() + " sqft";
                            labelCity = "City: " + reader["city"].ToString();
                            labelDescription = "Description: " + reader["description"].ToString();

                            // Set seller info
                            labelSellerName = "Seller: " + reader["seller_name"].ToString();
                            labelSellerPhone = "Phone: " + reader["seller_phone"].ToString();

                            // Add image URL to list
                            if (!reader.IsDBNull(reader.GetOrdinal("image_url")))
                            {
                                imageUrls.Add(reader["image_url"].ToString());
                            }
                        }
                    }
                }

            }
            if (imageUrls.Count > 0)
            {
                imageUrl=imageUrls[0];
                if (imageUrls.Count <= 1)
                {
                    nextbutton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                MessageBox.Show("No images available for this property.");
            }
            AddProperties();
        }
        private void AddProperties()
        {
            housedetails.Add(new Housedetails { ImagePath=imageUrl,LabelTitle=labelTitle,LabelPrice=labelPrice,LabelBathrooms=labelBathrooms,LabelBedrooms=labelBedrooms,LabelSize=labelSize,LabelCity=labelCity,LabelDescription=labelDescription,LabelSellerName=labelSellerName,LabelSellerPhone=labelSellerPhone});
        }

    }
public class Housedetails : INotifyPropertyChanged
    {
        private string _imagePath;
        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                _imagePath = value;
                OnPropertyChanged(nameof(ImagePath));
            }
        }

        public int PropertyId { get; set; }
        public string LabelTitle { get; set; }
        public string LabelPrice { get; set; }
        public string LabelBedrooms { get; set; }
        public string LabelBathrooms { get; set; }
        public string LabelSize { get; set; }
        public string LabelCity { get; set; }
        public string LabelDescription { get; set; }
        public string LabelSellerPhone { get; set; }
        public string LabelSellerName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
