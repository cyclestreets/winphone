using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Cyclestreets.Pages
{
	public partial class SaveRoute : PhoneApplicationPage
	{
		public SaveRoute()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );

			string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames( "*.route" );
			existingSaves.ItemsSource = names;
		}

		private void saveButton_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			string fileName = saveFileName.Text;
			if( !string.IsNullOrWhiteSpace( fileName ) )
			{
				saveRoute( fileName + ".route" );
			}
			NavigationService.GoBack();
		}

		private void saveRoute( string filename )
		{
			IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
			string jsonData = (string)PhoneApplicationService.Current.State[ "route" ];
			StreamWriter sw = new StreamWriter( new IsolatedStorageFileStream( filename, FileMode.Create, myFile ) );
			sw.WriteLine( jsonData ); //Wrting to the file
			sw.Close();

			MessageBoxResult result = MessageBox.Show( "This route has been saved and can be loaded even when a data connection is unavailable from the Load Route menu.", "Route Saved", MessageBoxButton.OK );
		}

		private void saveFileName_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			TextBox tb = sender as TextBox;
			tb.SelectAll();
		}

		private void existingSaves_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			MessageBoxResult result = MessageBox.Show( "Do you want to overwrite this save file with the current route?", "Overwrite?", MessageBoxButton.OKCancel );
			if( result == MessageBoxResult.OK )
			{
				string name = e.AddedItems[ 0 ].ToString();
				saveRoute( name );
			}
			NavigationService.GoBack();
		}
	}
}