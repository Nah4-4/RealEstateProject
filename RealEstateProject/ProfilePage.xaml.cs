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
    public partial class ProfilePage : Page
    {
        private Profile _selectedProfile;
        private Queue<VisitRequest> visitRequestQueue = new Queue<VisitRequest>(); // Queue for visit requests
        private Queue<VisitRequest> removedRequestsQueue = new Queue<VisitRequest>(); // Queue for removed requests to update DB
        private Stack<ActiveListing> activeListingsStack = new Stack<ActiveListing>(); // Stack for active listings
    
       

        public ObservableCollection<Profile> profiles { get; set; }
        
        int userId;

        private string connectionString = "server=localhost;uid=root;pwd="+ Environment.GetEnvironmentVariable("PASSWORD") +";database=TestDB";

        public ProfilePage(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            profiles = new ObservableCollection<Profile>();
            LoadUserProfile();
            LoadActiveListings(); // Load currently active listings by the user using stack
            LoadVisitRequests();
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
                            string userName = "Name: "+reader["name"].ToString();
                            string userEmail = "Email: "+reader["email"].ToString();
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
        // Load visit requests into the queue
        private void LoadVisitRequests()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT p.title, u.name AS buyer_name, r.request_time
                FROM Requests r
                JOIN Property p ON r.property_id = p.property_id
                JOIN Users u ON r.buyer_id = u.user_id
                WHERE p.user_id = @userId AND r.status = 'pending'
                ORDER BY r.request_time ASC";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        visitRequestQueue.Clear();  // Clear the queue

                        while (reader.Read())
                        {
                            VisitRequest request = new VisitRequest
                            {
                                PropertyTitle = reader["title"].ToString(),
                                BuyerName = reader["buyer_name"].ToString(),
                                RequestTime = reader.GetDateTime("request_time")
                            };

                            visitRequestQueue.Enqueue(request);  // Add each request to the queue
                        }
                    }
                }

                DisplayRequestsFromQueue();
            }
        }
        private void DisplayRequestsFromQueue()
        {
            if (visitRequestQueue.Count == 0)
            {
                listBoxVisitRequests.ItemsSource = new List<string> { "No pending requests." };
            }
            else
            {
                listBoxVisitRequests.ItemsSource = visitRequestQueue.Select(request =>
                    $"Request for {request.PropertyTitle} from {request.BuyerName} at {request.RequestTime}").ToList();
            }

            //listBoxVisitRequests.ItemsSource = visitRequestQueue.Select(request =>$"Request for {request.PropertyTitle} from {request.BuyerName} at {request.RequestTime}").ToList();

            //listBoxVisitRequests.Items.Clear();
            //foreach (var request in visitRequestQueue)
            //{
            //    listBoxVisitRequests.Items.Add($"Request for {request.PropertyTitle} from {request.BuyerName} at {request.RequestTime}");
            //}

            //if (visitRequestQueue.Count == 0)
            //{
            //    listBoxVisitRequests.Items.Add("No pending requests.");
            //}
        }
        private void LoadActiveListings()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Load active listings
                string activeListingsQuery = "SELECT title FROM Property WHERE user_id = @userId AND status = 'available'";
                using (MySqlCommand command = new MySqlCommand(activeListingsQuery, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        activeListingsStack.Clear(); // Clear the stack

                        while (reader.Read())
                        {
                            ActiveListing listing = new ActiveListing
                            {
                                Title = reader["title"].ToString()
                            };
                            activeListingsStack.Push(listing); // Push the active listing onto the stack
                        }
                    }
                }

                DisplayActiveListingsFromStack(); // Display the listings from the stack
            }
        }

        // Display the user's active listings from the stack
        private void DisplayActiveListingsFromStack()
        {
            //listBoxActiveListings.Items.Clear();
            //listBoxActiveListings.Items.Add("Your Active Listings:");
            listBoxActiveListings.ItemsSource = activeListingsStack.Select(listing => listing.Title).ToList();
        }

        private void AddListing_Button_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddListing(userId));
        }

        private void btn_back_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new HomePage(userId));
        }

        private void ButtonProcessQueue_Click(object sender, RoutedEventArgs e)
        {
            if (visitRequestQueue.Count > 0)
            {
                var removedRequest = visitRequestQueue.Dequeue();  // Remove from queue
                removedRequestsQueue.Enqueue(removedRequest);  // Keep track of removed requests to update DB later
                DisplayRequestsFromQueue();  // Update UI
            }
            else
            {
                MessageBox.Show("No more requests to process.");
            }
        }

        private void ButtonRefreshRequests_Click(object sender, RoutedEventArgs e)
        {        
            LoadVisitRequests();         
        }
    }


    public class Profile
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
    public class VisitRequest
    {
        public string PropertyTitle { get; set; }
        public string BuyerName { get; set; }
        public DateTime RequestTime { get; set; }
    }

    // Define ActiveListing class to store active listing information
    public class ActiveListing
    {
        public string Title { get; set; }
    }
}

