﻿using Cyclestreets.Managers;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Controls;
using Polenter.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Cyclestreets.Pages
{
    public partial class LoadRoute : PhoneApplicationPage
    {
        public LoadRoute()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames("*.route");
            saveFiles.ItemsSource = names;

            NavigationService.RemoveBackEntry();
        }

        private void saveFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

            IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
            var stream = new IsolatedStorageFileStream(e.AddedItems[0].ToString(), FileMode.Open, myFile);

            if (!myFile.FileExists(e.AddedItems[0].ToString()))
            {
                MessageBoxResult result = MessageBox.Show("Save file not found.", "Error", MessageBoxButton.OK);
                return;
            }

            var serializer = new SharpSerializer(true);

            // deserialize (to check the serialization)
            var res = serializer.Deserialize(stream);
            rm.RouteCacheForSaving = (Dictionary<string, string>)res;
            stream.Close();

            NavigationService.Navigate(new Uri("/Pages/DirectionsPage.xaml?plan=saved", UriKind.Relative));

        }
    }
}