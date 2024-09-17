using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Win32;
using MySql.Data.MySqlClient;

namespace RealEstateProject
{
    /// <summary>
    /// Interaction logic for AddListing.xaml
    /// </summary>
    public partial class AddListing : Page
    {
        private List<string> imagePaths = new List<string>();
        int userId;
        private string connectionString = "server=localhost;uid=root;pwd=ushallpass44;database=TestDB";
        public AddListing(int userId)
        {
            this.userId = userId;
            InitializeComponent();
        }
        private void buttonSubmit_Click(object sender, RoutedEventArgs e)
        {
            string title = TBTitle.Text;
            string description = TBDesc.Text;
            string price = TBPrice.Text;
            string propertyType = CBType.Text.ToString();
            string bedrooms = TBBed.Text;
            string bathrooms = TBBath.Text;
            string size = TBSize.Text;
            string address = TBAddress.Text;
            string city = TBCity.Text;

            // Simple validation
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(price) || string.IsNullOrWhiteSpace(bedrooms) ||
                string.IsNullOrWhiteSpace(bathrooms) || string.IsNullOrWhiteSpace(size) ||
                string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrEmpty(propertyType) || imagePaths.Count == 0)
            {
                MessageBox.Show("All fields are required, and you must upload at least one image.");
                return;
            }

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                MySqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Insert the property into the Property table
                    string query = "INSERT INTO Property (user_id, title, description, price, property_type, number_of_bedrooms, number_of_bathrooms, size_in_sqft, address, city) " +
                                   "VALUES (@userId, @Title, @Description, @Price, @PropertyType, @Bedrooms, @Bathrooms, @Size, @Address, @City)";
                    using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@Title", title);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@Price", price);
                        command.Parameters.AddWithValue("@PropertyType", propertyType);
                        command.Parameters.AddWithValue("@Bedrooms", bedrooms);
                        command.Parameters.AddWithValue("@Bathrooms", bathrooms);
                        command.Parameters.AddWithValue("@Size", size);
                        command.Parameters.AddWithValue("@Address", address);
                        command.Parameters.AddWithValue("@City", city);
                        command.ExecuteNonQuery();
                    }

                    ulong propertyId = (ulong)new MySqlCommand("SELECT LAST_INSERT_ID();", connection, transaction).ExecuteScalar();

                    // Insert each image into the PropertyImages table
                    foreach (string imagePath in imagePaths)
                    {
                        string saveImagePath = SaveImageToServer(imagePath);
                        string imageQuery = "INSERT INTO PropertyImages (property_id, image_url) VALUES (@PropertyId, @ImageUrl)";
                        using (MySqlCommand imageCommand = new MySqlCommand(imageQuery, connection, transaction))
                        {
                            imageCommand.Parameters.AddWithValue("@PropertyId", propertyId);
                            imageCommand.Parameters.AddWithValue("@ImageUrl", saveImagePath);
                            imageCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    MessageBox.Show("Property listed successfully!");
                    this.NavigationService.Navigate(new ProfilePage(userId));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private string SaveImageToServer(string imagePath)
        {
            // Define the server image path
            string serverDirectory = @"C:\RealEstateImages\";

            // Ensure the directory exists, if not create it
            if (!Directory.Exists(serverDirectory))
            {
                Directory.CreateDirectory(serverDirectory);
            }

            // Create the file path for the image
            string serverImagePath = System.IO.Path.Combine(serverDirectory, System.IO.Path.GetFileName(imagePath));

            // Save the file to the server directory
            File.Copy(imagePath, serverImagePath, true);

            return serverImagePath;
        }
       private void AddImagebtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (openFileDialog.FileNames.Length + imagePaths.Count > 5)
                {
                    MessageBox.Show("You can only upload up to 5 images.");
                }
                else
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        imagePaths.Add(fileName);
                        AddImageToPreview(fileName);  // Add images to the preview panel
                    }
                }
            }
        }
        private void AddImageToPreview(string fileName)
        {
            if (ImagePreviewPanel.Children.Contains(ImagePlaceholder))
            {
                ImagePreviewPanel.Children.Remove(ImagePlaceholder);
            }

            // Create a new Image control
            Image image = new Image
            {
                Source = new BitmapImage(new Uri(fileName)),
                Width = 130, // Set a fixed width for images
                Height = 130, // Set a fixed height for images
                Margin = new Thickness(5) // Margin around each image
            };

            // Add the Image control to the WrapPanel
            ImagePreviewPanel.Children.Add(image);
        }
    }
}
