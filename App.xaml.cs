﻿#region

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using CrittercismSDK;
using Cyclestreets.Common;
using CycleStreets.Helpers;
using Cyclestreets.Managers;
using Cyclestreets.Resources;
using FlurryWP8SDK;
using GalaSoft.MvvmLight.Ioc;
using MarkedUp;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

#endregion

namespace Cyclestreets
{
    public class NetworkBusy : BindableBase
    {
        private bool _networkIsBusy;

        public bool NetworkIsBusy
        {
            private get { return _networkIsBusy; }
            set
            {
                _networkIsBusy = value;
                OnPropertyChanged(@"Opacity");
            }
        }

        public float Opacity
        {
            get
            {
                if (NetworkIsBusy)
                    return 100;
                return 0;
            }
        }
    }

    public partial class App
    {
        // Locale to force CurrentCulture to in InitializeLanguage().
        // Use "qps-PLOC" to deploy pseudolocalized strings.
        // Use "" to let user Phone Language selection determine locale.
        public static String appForceCulture = ""; //"qps-PLOC";

        public static string hereAppID = "zgcciiZ696xHUiuoyJZi";
        public static string hereAppToken = "tH8mLbASkG9oz6j8DuXn7A";

        public static string apiKey = "d2ff10bbbded8e86";

        public static NetworkBusy networkStatus = new NetworkBusy();

        private static LicenseInformation _licenseInfo = new LicenseInformation();
        private static bool _isTrial;
        public bool trialExpired;

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

            SimpleIoc.Default.Register<RouteManager>();

            // Show graphics profiling information while debugging.
            /*if( Debugger.IsAttached )
			{
				// Display the current frame rate counters.
				Application.Current.Host.Settings.EnableFrameRateCounter = true;

				// Show the areas of the app that are being redrawn in each frame.
				//Application.Current.Host.Settings.EnableRedrawRegions = true;

				// Enable non-production analysis visualization mode,
				// which shows areas of a page that are handed off to GPU with a colored overlay.
				//Application.Current.Host.Settings.EnableCacheVisualization = true;

				// Prevent the screen from turning off while under the debugger by disabling
				// the application's idle detection.
				// Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
				// and consume battery power when the user is not using the phone.
				PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
			}*/

            ReverseGeocodeQueryManager mgr = new ReverseGeocodeQueryManager();
            SettingManager smgr = new SettingManager();

            if (smgr.GetBoolValue("PreventSleep", true))
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
        }

        public bool IsTrial
        {
            get { return _isTrial; }
        }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Check the current license information for this application
        /// </summary>
        private void CheckLicense()
        {
            // When debugging, we want to simulate a trial mode experience. The following conditional allows us to set the _isTrial 
            // property to simulate trial mode being on or off. 
#if DEBUG
            _isTrial = false;
#else
			_isTrial = _licenseInfo.IsTrial();
#endif
        }

        public MessageBoxResult showTrialExpiredMessage()
        {
            MessageBoxResult result = MessageBox.Show(AppResources.TrialExpiredMsg, AppResources.TrialExpiredTitle,
                MessageBoxButton.OK);
            if (result == MessageBoxResult.OK)
            {
                MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
                _marketPlaceDetailTask.Show();
            }
            return result;
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            Crittercism.Init("52b1be13d0d8f72067000007");
            Api.StartSession("JZSMBMX659NW78S35ZPR");
            AnalyticClient.Initialize("87c139ca-14a7-41ff-8b3b-095894a52bdf");
            AnalyticClient.RegisterRootNavigationFrame(RootFrame);
            CheckLicense();

            // Call this on launch to initialise the feedback helper
            FeedbackHelper.Default.Launching();

            SettingManager.instance.SetIntValue("LaunchCount", SettingManager.instance.GetIntValue("LaunchCount", 0) + 1);
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            Api.StartSession("JZSMBMX659NW78S35ZPR");
            AnalyticClient.Initialize("87c139ca-14a7-41ff-8b3b-095894a52bdf");
            //MarkedUp.AnalyticClient.RegisterRootNavigationFrame( RootFrame );

            CheckLicense();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Ensure that required application state is persisted here.
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
            else
            {
                Api.LogError("Unhandled Exception", e.ExceptionObject);
                AnalyticClient.LogLastChanceException(e);
            }
        }

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Force CurrentUICulture to locale defined by appForceCulture.
                // An empty string allows the user's Phone Language setting to
                // determine the locale.
                if (Debugger.IsAttached &&
                    String.IsNullOrWhiteSpace(appForceCulture) == false)
                {
                    Thread.CurrentThread.CurrentCulture =
                        new CultureInfo(appForceCulture);
                    Thread.CurrentThread.CurrentUICulture =
                        new CultureInfo(appForceCulture);
                }

                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow =
                    (FlowDirection) Enum.Parse(typeof (FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized;
        public int trialID;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion
    }
}