using Cyclestreets.Managers;
using GalaSoft.MvvmLight.Ioc;
using Polenter.Serialization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;

namespace Cyclestreets.Pages
{
    public partial class SaveRoute
    {
        public SaveRoute()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames("*.route");
            existingSaves.ItemsSource = names;
        }

        private void saveButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string fileName = saveFileName.Text;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                saveRoute(fileName + ".route");
            }
            NavigationService.GoBack();
        }

        private void saveRoute(string filename)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

            IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
            var stream = new IsolatedStorageFileStream(filename, FileMode.Create, myFile);

            var serializer = new SharpSerializer(true);

            // serialize
            serializer.Serialize(rm.RouteCacheForSaving, stream);

            stream.Seek(0, SeekOrigin.Begin);

            // deserialize (to check the serialization)
            var obj2 = serializer.Deserialize(stream);

            stream.Close();

            MessageBox.Show("This route has been saved and can be loaded even when a data connection is unavailable from the Load Route menu.", "Route Saved", MessageBoxButton.OK);
        }

        private void saveFileName_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }

        private void existingSaves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you want to overwrite this save file with the current route?", "Overwrite?", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                string name = e.AddedItems[0].ToString();
                saveRoute(name);
            }
            NavigationService.GoBack();
        }
    }
}