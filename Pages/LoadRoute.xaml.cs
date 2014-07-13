using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Cyclestreets.Pages
{
	public partial class LoadRoute : PhoneApplicationPage
	{
		public LoadRoute()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );

			string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames( "*.route" );
			saveFiles.ItemsSource = names;
		}

		private void saveFiles_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
			string sFile = e.AddedItems[ 0 ].ToString();

			if( !myFile.FileExists( sFile ) )
			{
				MessageBoxResult result = MessageBox.Show( "Save file not found.", "Error", MessageBoxButton.OK );
				return;
			}

			//Reading and loading data
			StreamReader reader = new StreamReader( new IsolatedStorageFileStream( sFile, FileMode.Open, myFile ) );
			string rawData = reader.ReadToEnd();
			reader.Close();

			PhoneApplicationService.Current.State[ "loadedRoute" ] = rawData;
			NavigationService.GoBack();

		}
	}
}