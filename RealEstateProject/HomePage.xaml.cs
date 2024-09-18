using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private string connectionString = "server=localhost;uid=root;pwd="+ Environment.GetEnvironmentVariable("PASSWORD") +";database=TestDB";
        public ObservableCollection<House> Houses { get; set; }
        private List<House> AllHouses { get; set; }  // Store all houses


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
            AllHouses = new List<House>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                // Fetch each property along with its first image
                string query = @"SELECT p.property_id, p.title, COALESCE(pi.image_url, 'C:/RealEstateImages/placeholder.png') AS image_url, price, p.size_in_sqft, p.address
                FROM Property p
                LEFT JOIN(
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
                        decimal price = reader.GetDecimal("price");
                        int sizeInSqft = reader.GetInt32("size_in_sqft");
                        string address = reader.GetString("address");

                        AddProperties(propertyId, title, imageUrl, price, sizeInSqft, address);
                    }
                }
            }
        }

        private void AddProperties(int propertyId, string title, string imageUrl,decimal price, int sizeInSqft, string address)
        {
            var house = new House
            {
                PropertyId = propertyId,
                Title = title,
                ImagePath = imageUrl,
                Price = price,
                SizeInSqft = sizeInSqft,
                Address = address
            };

            Houses.Add(house);  // Add to current view
            AllHouses.Add(house);  // Add to full list
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

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchAttribute = (comboBoxSearchAttribute.SelectedItem as ComboBoxItem)?.Content.ToString();
            string searchText = textBoxSearch.Text;

            if (searchAttribute == "Title" || searchAttribute == "Address")
            {
                PerformLinearSearch(searchText, searchAttribute);
            }
            else if(searchAttribute=="Price"||searchAttribute=="Size")
            {
                PerformBinarySearch(searchText, searchAttribute);
            }
            else
            {
                MessageBox.Show("Please choose a valid search attribute.");
            }
        }


        private void PerformLinearSearch(string searchText, string attribute)
        {
            // Reset the Houses collection to show all houses before filtering
            Houses.Clear();
            foreach (var house in AllHouses)
            {
                Houses.Add(house);
            }

            var filteredHouses = Houses.Where(h =>
                (attribute == "Title" && h.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (attribute == "Address" && h.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (filteredHouses.Count > 0)
            {
                Houses.Clear();  // Clear the displayed houses
                foreach (var house in filteredHouses)
                {
                    Houses.Add(house);
                }
             
            }
            else
                MessageBox.Show("No matching house found.");

        }

        // Binary search for sorted attributes like Price or Size
        private void PerformBinarySearch(string searchValue, string attribute)
        {
            decimal value;
            // Reset the Houses collection before sorting
            if (decimal.TryParse(searchValue, out value))
            {
                Houses.Clear();
                foreach (var house in AllHouses)
                {
                    Houses.Add(house);
                }

                var sortedHouses = Houses.OrderBy(h => attribute == "Price" ? h.Price : h.SizeInSqft).ToList();
                int index = BinarySearch(sortedHouses, value, attribute);
                if (index >= 0)
                {
                    Houses.Clear();
                    Houses.Add(sortedHouses[index]);
                }
                else
                {
                    MessageBox.Show("No matching house found.");
                }
            }
            else
                MessageBox.Show("Enter a valid number");
        }

        private int BinarySearch(List<House> sortedHouses, decimal value, string attribute)
        {
            int left = 0, right = sortedHouses.Count - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                decimal comparisonValue = attribute == "Price" ? sortedHouses[mid].Price : sortedHouses[mid].SizeInSqft;

                if (comparisonValue == value) return mid;
                if (comparisonValue < value) left = mid + 1;
                else right = mid - 1;
            }
            return -1;  // Not found
        }

        // Sort implementation
        private void ButtonSort_Click(object sender, RoutedEventArgs e)
        {
            // Reset the Houses collection to show all houses before sorting
            Houses.Clear();
            foreach (var house in AllHouses)
            {
                Houses.Add(house);
            }

            string sortOption = comboBoxSortAttribute.Text;
            List<House> sortedHouses;

            switch (sortOption)
            {
                case "Quick Sort by Price":
                    sortedHouses = QuickSort(Houses.ToList(), "Price");
                    break;
                case "Quick Sort by Size":
                    sortedHouses = QuickSort(Houses.ToList(), "SizeInSqft");
                    break;
                default:
                    sortedHouses = Houses.ToList();
                    break;
            }

            Houses.Clear();
            foreach (var house in sortedHouses)
            {
                Houses.Add(house);
            }
        }


        private List<House> QuickSort(List<House> houses, string attribute)
        {
            if (houses.Count <= 1) return houses;

            var pivot = houses[houses.Count / 2];
            var left = new List<House>();
            var right = new List<House>();

            foreach (var house in houses)
            {
                if (house == pivot) continue;

                decimal comparisonValue = attribute == "Price" ? house.Price : house.SizeInSqft;
                decimal pivotValue = attribute == "Price" ? pivot.Price : pivot.SizeInSqft;

                if (comparisonValue < pivotValue) left.Add(house);
                else right.Add(house);
            }

            var sortedLeft = QuickSort(left, attribute);
            var sortedRight = QuickSort(right, attribute);

            return sortedLeft.Concat(new[] { pivot }).Concat(sortedRight).ToList();
        }


    }

}