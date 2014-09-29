using Cyclestreets.Resources;

namespace Cyclestreets
{
	/// <summary>
	/// Provides access to string resources.
	/// </summary>
	public class LocalizedStrings
	{
		private static readonly AppResources _localizedResources = new AppResources();

		public static AppResources LocalizedResources { get { return _localizedResources; } }
	}
}