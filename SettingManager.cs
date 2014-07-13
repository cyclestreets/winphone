using System.IO.IsolatedStorage;

namespace Cyclestreets
{
	class SettingManager
	{
		public static SettingManager instance;

		public SettingManager()
		{
			instance = this;
		}

		public void SetBoolValue( string key, bool value )
		{
			if( IsolatedStorageSettings.ApplicationSettings.Contains( key ) )
				IsolatedStorageSettings.ApplicationSettings[ key ] = value;
			else
				IsolatedStorageSettings.ApplicationSettings.Add( key, value );
			IsolatedStorageSettings.ApplicationSettings.Save();
		}

		public bool GetBoolValue( string key, bool defaultValue )
		{
			if( IsolatedStorageSettings.ApplicationSettings.Contains( key ) )
				return (bool)IsolatedStorageSettings.ApplicationSettings[ key ];
			return defaultValue;
		}

		public void SetStringValue( string key, string value )
		{
			if( IsolatedStorageSettings.ApplicationSettings.Contains( key ) )
				IsolatedStorageSettings.ApplicationSettings[ key ] = value;
			else
				IsolatedStorageSettings.ApplicationSettings.Add( key, value );
			IsolatedStorageSettings.ApplicationSettings.Save();
		}

		public string GetStringValue( string key, string defaultValue )
		{
			if( IsolatedStorageSettings.ApplicationSettings.Contains( key ) )
				return (string)IsolatedStorageSettings.ApplicationSettings[ key ];
			return defaultValue;
		}

		public void SetIntValue( string key, int value )
		{
			if( IsolatedStorageSettings.ApplicationSettings.Contains( key ) )
				IsolatedStorageSettings.ApplicationSettings[key] = value;
			else
				IsolatedStorageSettings.ApplicationSettings.Add( key, value );
			IsolatedStorageSettings.ApplicationSettings.Save();
		}

		public int GetIntValue( string key, int defaultValue )
		{
			if( IsolatedStorageSettings.ApplicationSettings.Contains( key ) )
				return (int)IsolatedStorageSettings.ApplicationSettings[key];
			return defaultValue;
		}
	}
}
