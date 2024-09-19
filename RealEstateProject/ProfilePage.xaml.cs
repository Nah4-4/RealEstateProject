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
        }
        private void LoadActiveListings()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Load active listings
                string activeListingsQuery = "SELECT property_id, title FROM Property WHERE user_id = @userId AND status = 'available'";
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
                                PropertyId = reader.GetInt32("property_id"),
                                Title = reader["title"].ToString()
                            };
                            activeListingsStack.Push(listing); // Push the active listing onto the stack
                        }
                    }
                }

                DisplayActiveListingsFromStack(); // Display the listings from the stack
            }
        }

        
        // Mark a property as sold
        private void MarkPropertyAsSold(int propertyId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string updateQuery = "UPDATE Property SET status = 'sold' WHERE property_id = @propertyId";

                using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@propertyId", propertyId);
                    command.ExecuteNonQuery();
                }
            }

            // Update stack and reload listings
            UpdateActiveListingsStack(propertyId, "sold");
            LoadActiveListings();
        }

        // Delete a property
        //private void DeleteProperty(int propertyId)
        //{
        //    using (MySqlConnection connection = new MySqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        string deleteQuery = "DELETE FROM Property WHERE property_id = @propertyId";

        //        using (MySqlCommand command = new MySqlCommand(deleteQuery, connection))
        //        {
        //            command.Parameters.AddWithValue("@propertyId", propertyId);
        //            command.ExecuteNonQuery();
        //        }
        //    }

        //    // Update stack and reload listings
        //    UpdateActiveListingsStack(propertyId, "delete");
        //    LoadActiveListings();
        //}

        // Update the stack after marking a property as sold or deleting it
        private void UpdateActiveListingsStack(int propertyId, string action)
        {
            Stack<ActiveListing> tempStack = new Stack<ActiveListing>();

            while (activeListingsStack.Count > 0)
            {
                ActiveListing listing = activeListingsStack.Pop();

                if (listing.PropertyId == propertyId && action == "sold")
                {
                    // Don't push to tempStack; property is marked as sold
                }
                else if (listing.PropertyId == propertyId && action == "delete")
                {
                    // Don't push to tempStack; property is deleted
                }
                else
                {
                    tempStack.Push(listing);
                }
            }

            // Restore the stack with updated listings
            while (tempStack.Count > 0)
            {
                activeListingsStack.Push(tempStack.Pop());
            }
        }

        // Display the user's active listings from the stack
        private void DisplayActiveListingsFromStack()
        {
            listBoxActiveListings.Items.Clear();
            //listBoxActiveListings.Items.Add("Your Active Listings:");

            // Display and keep listings in a temporary stack to preserve original stack order
            Stack<ActiveListing> tempStack = new Stack<ActiveListing>();
            while (activeListingsStack.Count > 0)
            {
                ActiveListing listing = activeListingsStack.Pop();
                tempStack.Push(listing); // Push to temporary stack
                listBoxActiveListings.Items.Add($"{listing.PropertyId}: {listing.Title}");
            }

            // Restore the original stack order
            while (tempStack.Count > 0)
            {
                activeListingsStack.Push(tempStack.Pop());
            }

            if (listBoxActiveListings.Items.Count == 0)
            {
                listBoxActiveListings.Items.Add("No active listings.");
            }
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
                ProfileForm_Closing();

            }
            else
            {
                MessageBox.Show("No more requests to process.");
            }
        }
        private void ProfileForm_Closing()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Process each removed request and update the database
                while (removedRequestsQueue.Count > 0)
                {
                    var removedRequest = removedRequestsQueue.Dequeue();

                    string updateQuery = "UPDATE Requests SET status = 'done' WHERE property_id = (SELECT property_id FROM Property WHERE title = @title) AND buyer_id = (SELECT user_id FROM Users WHERE name = @buyerName)";

                    using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@title", removedRequest.PropertyTitle);
                        command.Parameters.AddWithValue("@buyerName", removedRequest.BuyerName);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        private void ButtonRefreshRequests_Click(object sender, RoutedEventArgs e)
        {        
            LoadVisitRequests();         
        }
        private void listBoxActiveListings_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listBoxActiveListings.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a valid property from the list.");
                return;
            }

            string selectedItem = listBoxActiveListings.SelectedItem.ToString();
            int propertyId = int.Parse(selectedItem.Split(':')[0]); // Get property_id from the selected item

            MessageBoxResult result = MessageBox.Show("Do you want to mark this property as sold? ", "Action", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                MarkPropertyAsSold(propertyId);
            }
            else if (result == MessageBoxResult.Cancel)
            {
               
            }
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
        public int PropertyId { get; set; } // Property ID
        public string Title { get; set; }
    }
}

