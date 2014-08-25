using Cyclestreets.Managers;
using Cyclestreets.Resources;
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
                saveRoute(fileName);
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

            //stream.Seek(0, SeekOrigin.Begin);

            // deserialize (to check the serialization)
            //var obj2 = serializer.Deserialize(stream);

            stream.Close();

            MessageBox.Show(AppResources.RouteSaved, AppResources.RouteSavedTitle, MessageBoxButton.OK);
        }

        private void saveFileName_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null) 
                tb.SelectAll();
        }

        private void existingSaves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.OverwriteMessage, AppResources.Overwrite, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                string name = e.AddedItems[0].ToString();
                saveRoute(name);
            }
            NavigationService.GoBack();
        }
    }
}