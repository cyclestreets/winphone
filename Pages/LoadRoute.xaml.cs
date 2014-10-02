using System.Diagnostics;
using Windows.Storage;
using Cyclestreets.Managers;
using Cyclestreets.Resources;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Controls;
using Polenter.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;

namespace Cyclestreets.Pages
{
    public partial class LoadRoute
    {
        public LoadRoute()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames("*.route");
            saveFiles.ItemsSource = names.Select(n=>n.Remove(n.Length - 6)).ToList();
        }

        private async void saveFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

#if false
            IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {

                var stream = new IsolatedStorageFileStream(e.AddedItems[0] + ".route", FileMode.Open, myFile);

                if (!myFile.FileExists(e.AddedItems[0].ToString()))
                {
                    MessageBox.Show(AppResources.SaveFileNotFound, AppResources.Error, MessageBoxButton.OK);
                    return;
                }

                var serializer = new SharpSerializer(true);

                // deserialize (to check the serialization)
                var res = serializer.Deserialize(stream);
                rm.RouteCacheForSaving = (Dictionary<string, string>)res;
                stream.Close();

                NavigationService.Navigate(new Uri("/Pages/RouteOverview.xaml?plan=saved", UriKind.Relative));
                NavigationService.RemoveBackEntry();
            }
            catch
            {
                MessageBox.Show(AppResources.SaveFileNotFound, AppResources.Error, MessageBoxButton.OK);
            }
#else
            // Get the local folder.
            StorageFolder local = ApplicationData.Current.LocalFolder;
            if (local != null)
            {
                // Get the file.
        
        // Read the data.
                using (var file = await local.OpenStreamForReadAsync(e.AddedItems[0] + @".route"))
                {
                    var serializer = new SharpSerializer(false);
                    file.Seek(0, SeekOrigin.Begin);
                    using (StreamReader streamReader = new StreamReader(file))
                    {
                        Debug.WriteLine(streamReader.ReadToEnd());
                    }

                    file.Seek(0, SeekOrigin.Begin);
                    // deserialize (to check the serialization)
                    var res = serializer.Deserialize(file);
                    rm.RouteCacheForSaving = (Dictionary<string, string>) res;

                    
                }
            }
#endif
            

        }
    }
}