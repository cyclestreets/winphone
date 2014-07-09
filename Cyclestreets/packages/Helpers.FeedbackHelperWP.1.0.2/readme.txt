Welcome to the Windows Phone Feedback helper

To use the helper simply add the following line to your startup pages code behind (e.g. MainPage.xaml.cs) loaded event:

	FeedbackHelper.Default.Initialise("<support email address>", "<Application Name">);

Or alternatively if you don't use a Loaded event, add the following into your pages constructor:

	Loaded += (s, e) => FeedbackHelperLib.FeedbackHelper.Default.Initialise("<support email address>", "<Application Name">);

To add support for the additional languages that feedback helper provides then just alter the "Supported Cultures" for your project.
Either by editing the .csproj file and adding languages or through project properties.

Current supported cultures include:
	* EN - International English and English derivatives
	* NL - Dutch and Dutch derivatives
(Feel free to contribute additional languages on the CodePlex Git project)

Feedback any problems or issues to the codeplex page