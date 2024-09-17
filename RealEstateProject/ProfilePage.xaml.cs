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
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page, INotifyPropertyChanged
    {
        private Profile _selectedProfile;
        public Profile SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                _selectedProfile = value;
                OnPropertyChanged(nameof(SelectedProfile));
            }
        }

        public ObservableCollection<string> SoldHomes { get; set; }
        public ObservableCollection<string> BoughtHomes { get; set; }

        public ObservableCollection<Profile> profiles { get; set; }
        
        int userId;

        private string connectionString = "server=localhost;uid=root;pwd=ushallpass44;database=TestDB";

        public ProfilePage(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            SoldHomes = new ObservableCollection<string>();
            BoughtHomes = new ObservableCollection<string>();
            profiles = new ObservableCollection<Profile>();

            LoadUserHomes(userId);
            LoadUserProfile();
        }

        private void LoadUserProfile()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT name, email FROM Users WHERE user_id = @userId";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string userName = "Name "+reader["name"].ToString();
                            string userEmail = "Email "+reader["email"].ToString();
                            profiles.Add(new Profile { Name = userName, Email = userEmail });

                            // Set DataContext to the first profile after loading it
                            if (profiles.Count > 0)
                            {
                                DataContext = profiles[0];
                            }
                        }
                    }
                }
            }
        }

        public void LoadUserHomes(int userId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Load sold homes
                string soldHomesQuery = "SELECT title FROM Property WHERE user_id = @userId";
                using (MySqlCommand command = new MySqlCommand(soldHomesQuery, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SoldHomes.Add(reader["title"].ToString());
                        }
                    }
                }

                // Load bought homes
                string boughtHomesQuery = "SELECT Property.title FROM BoughtHomes JOIN Property ON BoughtHomes.property_id = Property.property_id WHERE BoughtHomes.buyer_id = @userId";
                using (MySqlCommand command = new MySqlCommand(boughtHomesQuery, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            BoughtHomes.Add(reader["title"].ToString());
                        }
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AddListing_Button_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddListing(userId));
        }
    }


    public class Profile
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}

