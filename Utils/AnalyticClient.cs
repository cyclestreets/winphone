using System.Windows;
using Microsoft.Phone.Controls;

namespace Cyclestreets.Utils
{
    internal class AnalyticClient
    {
        public static void Error(string format)
        {

        }

        public static void TrialConversionComplete(TrialConversion tc)
        {

        }

        public static void Initialize(string key)
        {

        }

        public static void RegisterRootNavigationFrame(PhoneApplicationFrame rootFrame)
        {
        }

        public static void LogLastChanceException(ApplicationUnhandledExceptionEventArgs applicationUnhandledExceptionEventArgs)
        {
        }
    }

    internal class TrialConversion
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string CurrentMarket { get; set; }
        public string CommerceEngine { get; set; }
        public string Currency { get; set; }
        public double Price { get; set; }
    }
}
