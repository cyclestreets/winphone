using System;
using Cyclestreets.Annotations;
using Cyclestreets.Resources;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Cyclestreets.Pages
{
    [UsedImplicitly]
    public partial class ShareChoice
	{
		public ShareChoice()
		{
			InitializeComponent();
		}

		private void socialChoice_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			ShareLinkTask shareLinkTask = new ShareLinkTask();

			shareLinkTask.Title = AppResources.ShareChoice_socialChoice_Tap_CycleStreets_Cycle_Route;
			shareLinkTask.LinkUri = new Uri( string.Format(@"http://www.cyclestreets.net/journey/{0}/", (int)PhoneApplicationService.Current.State[ @"routeIndex" ]), UriKind.Absolute );
			shareLinkTask.Message = AppResources.ShareChoice_socialChoice_Tap_Cycling_directions;

			shareLinkTask.Show();

			FlurryWP8SDK.Api.LogEvent( @"SocialShare" );
		}

		private void messagingChoice_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			SmsComposeTask smsComposeTask = new SmsComposeTask();
			smsComposeTask.Body = string.Format(AppResources.smsBody, (int)PhoneApplicationService.Current.State[ @"routeIndex" ]);
			smsComposeTask.Show();

			FlurryWP8SDK.Api.LogEvent( @"SMSShare" );
		}

		private void emailChoice_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			EmailComposeTask emailTask = new EmailComposeTask();
			emailTask.Subject = AppResources.ShareChoice_socialChoice_Tap_CycleStreets_Cycle_Route;
			emailTask.Body = string.Format(AppResources.emailBody, (int)PhoneApplicationService.Current.State[ @"routeIndex" ]);
			emailTask.Show();

			FlurryWP8SDK.Api.LogEvent( @"EmailShare" );
		}
	}
}