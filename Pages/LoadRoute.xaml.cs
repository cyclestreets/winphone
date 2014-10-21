using System.Collections.ObjectModel;
using System.Windows;
using Windows.Storage;
using Cyclestreets.Annotations;
using Cyclestreets.Managers;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Polenter.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Controls;

namespace Cyclestreets.Pages
{
    [UsedImplicitly]
    public partial class LoadRoute
    {
        public LoadRoute()
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
            string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames("*.route");
            ObservableCollection<RouteFile> files = new ObservableCollection<RouteFile>();
            foreach (var fileObj in names.Select(name => new RouteFile {Name = name.Remove(name.Length - 6, 6)}))
            {
                fileObj.Deleted += FileDeleted;
                files.Add(fileObj);
            }
            saveFiles.ItemsSource = files;
        }

        private void FileDeleted(object sender)
        {
            FindFiles();
        }
    }
}