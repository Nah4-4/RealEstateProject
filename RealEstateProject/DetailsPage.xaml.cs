using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
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
    /// Interaction logic for DetailsPage.xaml
    /// </summary>
    public partial class DetailsPage : Page
    {
        int userId;
        public ObservableCollection<Housedetails> housedetails { get; set; }

        private int propertyId;
        private int currentImageIndex = 0;
        private List<string> imageUrls = new List<string>();

        private string connectionString = "server=localhost;uid=root;pwd=" + Environment.GetEnvironmentVariable("PASSWORD") + ";database=TestDB";
        public DetailsPage(int propertyId, int userId)
        {
            this.userId = userId;
            this.propertyId = propertyId;
            InitializeComponent();
            housedetails = new ObservableCollection<Housedetails>();
            LoadPropertyDetails(propertyId);
            if (housedetails.Count > 0)
            {
                DataContext = housedetails[0];
            }
        }

        string imageUrl, labelTitle, labelPrice, labelBedrooms, labelBathrooms, labelSize, labelCity, labelDescription, labelSellerPhone, labelSellerName;
        string property_type, address, date;
        private void btnRequest_Click(object sender, RoutedEventArgs e)
        {
            if (userId == 0)
            {
                LoginView loginView = new LoginView();
                loginView.Show();
            }
            else
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Fetch the seller_id based on the property_id
                    int sellerId;
                    string sellerQuery = "SELECT user_id FROM Property WHERE property_id = @propertyId;";
                    using (MySqlCommand sellerCommand = new MySqlCommand(sellerQuery, connection))
                    {
                        sellerCommand.Parameters.AddWithValue("@propertyId", propertyId);
                        sellerId = Convert.ToInt32(sellerCommand.ExecuteScalar());
                    }

                    // Check if the current user is the seller
                    if (sellerId == userId)
                    {
                        MessageBox.Show("You cannot request a visit for a property that you listed.");
                        return; // Exit the method to prevent the request from being submitted
                    }

                    // Check if a request already exists for this property and user
                    string checkRequestQuery = @"
                SELECT COUNT(*) 
                FROM Requests 
                WHERE property_id = @propertyId AND buyer_id = @buyerId";

                    using (MySqlCommand checkRequestCommand = new MySqlCommand(checkRequestQuery, connection))
                    {
                        checkRequestCommand.Parameters.AddWithValue("@propertyId", propertyId);
                        checkRequestCommand.Parameters.AddWithValue("@buyerId", userId);

                        int existingRequestCount = Convert.ToInt32(checkRequestCommand.ExecuteScalar());
                        if (existingRequestCount > 0)
                        {
                            MessageBox.Show("You have already requested a visit for this property.");
                            return;
                        }
                    }

                    // Insert the request into the Requests table if the user is not the seller and hasn't already requested
                    string insertQuery = @"INSERT INTO Requests (property_id, buyer_id, seller_id, status) 
                        VALUES (@propertyId, @buyerId, @sellerId, 'pending')";

                    using (MySqlCommand command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@propertyId", propertyId);
                        command.Parameters.AddWithValue("@buyerId", userId); // The current user requesting the visit
                        command.Parameters.AddWithValue("@sellerId", sellerId); // The owner of the property

                        try
                        {
                            command.ExecuteNonQuery();
                            MessageBox.Show("Visit request submitted successfully.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error: " + ex.Message);
                        }
                    }
                }
            }
        }  

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
                string query = @"SELECT p.title, p.price, p.city, p.description, p.number_of_bedrooms, p.number_of_bathrooms, p.size_in_sqft, p.address,p.property_type,
                                p.listed_at,u.name as seller_name, u.phone_number as seller_phone, i.image_url
                                FROM Property p JOIN Users u ON p.user_id = u.user_id
                               LEFT JOIN PropertyImages i ON p.property_id = i.property_id
                               WHERE p.property_id = @propertyId";

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
                            labelBedrooms = reader["number_of_bedrooms"].ToString()+" Bed";
                            labelBathrooms = reader["number_of_bathrooms"].ToString()+" Baths";
                            labelSize =  reader["size_in_sqft"].ToString() + " sqft";
                            labelCity = "City: " + reader["city"].ToString();
                            labelDescription = "Description: " + reader["description"].ToString();
                            address = "Address: " + reader["address"].ToString();
                            date = "Date Listed: " + reader["listed_at"].ToString();
                            property_type = "Property Type: " + reader["property_type"].ToString();

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
            housedetails.Add(new Housedetails
            {
                ImagePath = imageUrl,
                LabelTitle = labelTitle,
                LabelPrice = labelPrice,
                LabelBathrooms = labelBathrooms,
                LabelBedrooms = labelBedrooms,
                LabelSize = labelSize,
                LabelCity = labelCity,
                LabelDescription = labelDescription,
                LabelSellerName = labelSellerName,
                LabelSellerPhone = labelSellerPhone,
                Property_Type = property_type,
                Address = address,
                Date = date
            });
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

        public string Property_Type { get; set; }
        public string Address { get; set; }
        public string Date { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
