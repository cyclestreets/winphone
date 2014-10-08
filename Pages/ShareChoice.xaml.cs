using System;
using Cyclestreets.Annotations;
using Cyclestreets.Managers;
using Cyclestreets.Resources;
using GalaSoft.MvvmLight.Ioc;
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
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

			ShareLinkTask shareLinkTask = new ShareLinkTask();

			shareLinkTask.Title = AppResources.ShareChoice_socialChoice_Tap_CycleStreets_Cycle_Route;
            shareLinkTask.LinkUri = new Uri(string.Format(@"http://www.cyclestreets.net/journey/{0}/", rm.Overview.RouteNumber), UriKind.Absolute);
			shareLinkTask.Message = AppResources.ShareChoice_socialChoice_Tap_Cycling_directions;

			shareLinkTask.Show();

			FlurryWP8SDK.Api.LogEvent( @"SocialShare" );
		}

		private void messagingChoice_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

			SmsComposeTask smsComposeTask = new SmsComposeTask();
            smsComposeTask.Body = string.Format(AppResources.smsBody, rm.Overview.RouteNumber);
			smsComposeTask.Show();

			FlurryWP8SDK.Api.LogEvent( @"SMSShare" );
		}

		private void emailChoice_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

			EmailComposeTask emailTask = new EmailComposeTask();
			emailTask.Subject = AppResources.ShareChoice_socialChoice_Tap_CycleStreets_Cycle_Route;
            emailTask.Body = string.Format(AppResources.emailBody, rm.Overview.RouteNumber);
			emailTask.Show();

			FlurryWP8SDK.Api.LogEvent( @"EmailShare" );
		}
	}
}