using System.Collections.Generic;
using System.Windows.Input;
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

        private async void DeleteFileWithName(string name)
        {
            // Get the local folder.
            StorageFolder local = ApplicationData.Current.LocalFolder;
            if (local != null)
            {
                var file = await local.GetFileAsync(name + @".route");
                file.DeleteAsync();
                if (Deleted != null)
                    Deleted(this);
            }
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
            List<RouteFile> files = new List<RouteFile>();
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

        private void saveFileName_Tap(object sender, GestureEventArgs e)
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