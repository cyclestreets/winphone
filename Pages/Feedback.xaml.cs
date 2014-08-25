using Cyclestreets.Resources;
using Microsoft.Phone.Controls;
using RestSharp;
using System.Windows;
using System.Windows.Controls;

namespace Cyclestreets.Pages
{
    class feedbackType
    {
        public string DisplayName
        {
            get;
            set;
        }
        public string InternalCode
        {
            get;
            set;
        }
        public feedbackType(string d, string i)
        {
            DisplayName = d;
            InternalCode = i;
        }
    }
    public partial class Feedback : PhoneApplicationPage
    {
        public Feedback()
        {
            InitializeComponent();

            feedbackTypeDropdown.ItemsSource = _feedbackTypes;
            feedbackTypeDropdown.DisplayMemberPath = "DisplayName";
            feedbackTypeDropdown.SelectedIndex = 1;
        }

        private readonly feedbackType[] _feedbackTypes =
		{
			new feedbackType(AppResources.Feedback_feedbackTypes_Route_Feedback, "routing"),
			new feedbackType(AppResources.Feedback_feedbackTypes_App_Feedback, "mobile"),
			new feedbackType(AppResources.Feedback_feedbackTypes_Bug_Report, "bug"),
			new feedbackType(AppResources.Feedback_feedbackTypes_Other, "other"),
		};

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey(@"routeID"))
            {
                feedbackTypeDropdown.SelectedIndex = 0;
                itinerary.Text = NavigationContext.QueryString["routeID"];
            }
        }

        private void submitButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(comments.Text))
            {
                MessageBox.Show(AppResources.Feedback_enterComments, AppResources.Feedback_submitButton_Tap_No_comments, MessageBoxButton.OK);
                return;
            }

            var client = new RestClient("https://www.cyclestreets.net/api/feedback.xml?key=" + App.apiKey);

            feedbackType type = feedbackTypeDropdown.SelectedItem as feedbackType;
            var request = new RestRequest("", Method.POST);
            request.AddParameter("type", type.InternalCode);
            request.AddParameter("itinerary", itinerary.Text);
            request.AddParameter("comments", comments.Text);
            request.AddParameter("email", email.Text);
            request.AddParameter("name", name.Text);

            // easy async support
            client.ExecuteAsync(request, response =>
            {
                //Debug.WriteLine( response );
                formScroller.Visibility = System.Windows.Visibility.Collapsed;
                feedbackResult.Visibility = System.Windows.Visibility.Visible;
            });
        }

        private void feedbackType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                feedbackType ftype = e.AddedItems[0] as feedbackType;
                if (ftype.InternalCode.Equals("route"))
                {
                    itineraryHead.Visibility = Visibility.Visible;
                    itinerary.Visibility = Visibility.Visible;
                }
                else
                {
                    itineraryHead.Visibility = Visibility.Collapsed;
                    itinerary.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}