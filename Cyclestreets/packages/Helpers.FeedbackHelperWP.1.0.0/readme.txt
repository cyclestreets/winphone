Welcome to the Windows Phone Feedback helper

To use the helper simply add the following line to your startup pages code behind (e.g. MainPage.xaml.cs) loaded event:

	FeedbackHelper.Default.Initialise("<support email address>");

Or alternatively if you don't use a Loaded event, add the following into your pages constructor:

	Loaded += (s, e) => FeedbackHelper.Default.Initialise("<support email address>");

Not forgeting to add the using reference to the helper lib

	using FeedbackHelperLib;

Feedback any problems or issues to the codeplex page