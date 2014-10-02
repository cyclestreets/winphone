using Cyclestreets.Managers;
using Cyclestreets.Resources;
using GalaSoft.MvvmLight.Ioc;
using Polenter.Serialization;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Windows.Storage;

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

            string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames(@"*.route");
            existingSaves.ItemsSource = names.Select(n => n.Remove(n.Length - 6)).ToList();
        }

        private void saveButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Regex rgx = new Regex(@"[^a-zA-Z0-9 -]");
            string fileName = rgx.Replace(saveFileName.Text, @"");
            fileName += @".route";
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                saveRoute(fileName);
            }
            NavigationService.GoBack();
        }

        private async void saveRoute(string filename)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

#if false
            IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
            var stream = new IsolatedStorageFileStream(filename, FileMode.Create, myFile);

            var serializer = new SharpSerializer(true);

            // serialize
            serializer.Serialize(rm.RouteCacheForSaving, stream);

            //stream.Seek(0, SeekOrigin.Begin);

            // deserialize (to check the serialization)
            //var obj2 = serializer.Deserialize(stream);

            stream.Close();
#else
            // Get the local folder.
            StorageFolder local = ApplicationData.Current.LocalFolder;
            var file = await local.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            // Write the data from the textbox.
            using (var s = await file.OpenStreamForWriteAsync())
            {
                var serializer = new SharpSerializer(false);

                // serialize
                serializer.Serialize(rm.RouteCacheForSaving, s);
            }
#endif

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