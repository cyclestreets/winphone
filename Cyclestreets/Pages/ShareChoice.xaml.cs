using System;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Cyclestreets.Pages
{
	public partial class ShareChoice : PhoneApplicationPage
	{
		public ShareChoice()
		{
			InitializeComponent();
		}

		private void socialChoice_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			ShareLinkTask shareLinkTask = new ShareLinkTask();

			shareLinkTask.Title = "CycleStreets Cycle Route";
			shareLinkTask.LinkUri = new Uri( "http://www.cyclestreets.net/journey/" + (int)PhoneApplicationService.Current.State[ "routeIndex" ] + "/", UriKind.Absolute );
			shareLinkTask.Message = "Cycling directions";

			shareLinkTask.Show();

			FlurryWP8SDK.Api.LogEvent( "SocialShare" );
		}

		private void messagingChoice_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			SmsComposeTask smsComposeTask = new SmsComposeTask();
			smsComposeTask.Body = "Here is a link to the CycleStreets route http://www.cyclestreets.net/journey/" + (int)PhoneApplicationService.Current.State[ "routeIndex" ] + "/";
			smsComposeTask.Show();

			FlurryWP8SDK.Api.LogEvent( "SMSShare" );
		}

		private void emailChoice_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			EmailComposeTask emailTask = new EmailComposeTask();
			emailTask.Subject = "CycleStreets Cycle Route";
			emailTask.Body = "Here is a link to the CycleStreets route http://www.cyclestreets.net/journey/" + (int)PhoneApplicationService.Current.State[ "routeIndex" ] + "/";
			emailTask.Show();

			FlurryWP8SDK.Api.LogEvent( "EmailShare" );
		}
	}
}