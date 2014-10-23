using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Navigation;
using Cyclestreets.Managers;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
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
    class RouteFile
    {
        // A delegate type for hooking up change notifications.
        public delegate void DeletedEventHandler(object sender);

        public event DeletedEventHandler Deleted;

        public string Name { get; set; }
        public ICommand DeleteFile
        {
            get
            {
                return new ViewModelCommand(() => DeleteFileWithName(Name));
            }
        }

        public ICommand LoadFile
        {
            get
            {
                return new ViewModelCommand(() => LoadFileWithName(Name));
            }
        }

        public ICommand SaveFile
        {
            get
            {
                return new ViewModelCommand(() => ConfirmOverwriteSave(Name));
            }
        }

        private async void DeleteFileWithName(string name)
        {
            var res = MessageBox.Show(string.Format("Are you sure you want to delete the route named {0}?", name), "Confirm Delete",
                MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                // Get the local folder.
                StorageFolder local = ApplicationData.Current.LocalFolder;
                if (local != null)
                {
                    try
                    {
                        var file = await local.GetFileAsync(name + @".route");
                        file.DeleteAsync();
                        if (Deleted != null)
                            Deleted(this);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(string.Format("Failed to delete the route named {0}. If this continues, please contact the developers at info@cyclestreets.net", name), "Error",
                MessageBoxButton.OK);
                    }
                    
                }
            }
        }

        private async void LoadFileWithName(string name)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

            try
            {
                // Get the local folder.
                StorageFolder local = ApplicationData.Current.LocalFolder;
                if (local != null)
                {
                    // Get the file.

                    // Read the data.
                    using (var file = await local.OpenStreamForReadAsync(string.Format(@"{0}.route", name)))
                    {
                        var serializer = new SharpSerializer(false);

                        file.Seek(0, SeekOrigin.Begin);
                        // deserialize (to check the serialization)
                        var res = serializer.Deserialize(file);
                        rm.RouteCacheForSaving = (Dictionary<string, string>)res;

                        App.RootFrame.Navigate(new Uri("/Pages/RouteOverview.xaml?plan=saved", UriKind.Relative));
                        App.RootFrame.RemoveBackEntry();
                    }
                }
            }
            catch (Exception ex)
            {
                BugSense.BugSenseHandler.Instance.LogException(ex, @"SaveData", @"Could not load");
                MessageBox.Show("Could not load route. Either the save file is corrupt or it is from an earlier version of CycleStreets and is no longer supported.",
                    "Load Error", MessageBoxButton.OK);
            }
        }

        private void ConfirmOverwriteSave(string name)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.OverwriteMessage, AppResources.Overwrite, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                SaveRoute(name);
            }
            App.RootFrame.GoBack();
        }

        public static async void SaveRoute(string filename)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

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

            MessageBox.Show(AppResources.RouteSaved, AppResources.RouteSavedTitle, MessageBoxButton.OK);
        }
    }
    public partial class SaveRoute
    {
        public SaveRoute()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            FindFiles();
        }

        private void FindFiles()
        {
            string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames(@"*.route");
            ObservableCollection<RouteFile> files = new ObservableCollection<RouteFile>();
            foreach (var fileObj in names.Select(name => new RouteFile {Name = name.Remove(name.Length-6,6)}))
            {
                fileObj.Deleted += FileDeleted;
                files.Add(fileObj);
            }
            existingSaves.ItemsSource = files;
        }

        private void FileDeleted(object sender)
        {
            FindFiles();
        }

        private void saveButton_Tap(object sender, GestureEventArgs e)
        {
            Regex rgx = new Regex(@"[^a-zA-Z0-9 -]");
            string fileName = rgx.Replace(saveFileName.Text, @"");
            fileName += @".route";
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                RouteFile.SaveRoute(fileName);
            }
            NavigationService.GoBack();
        }

        private void saveFileName_Tap(object sender, GestureEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
                tb.SelectAll();
        }
    }
}