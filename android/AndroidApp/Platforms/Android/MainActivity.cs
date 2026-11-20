using Android.App;
using Android.Content.PM;
using Android.OS;

namespace AndroidApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", 
					   EnableOnBackInvokedCallback = false, 
					   MainLauncher = true, 
					   LaunchMode = LaunchMode.SingleTop, 
					   ConfigurationChanges = ConfigChanges.ScreenSize | 
											 ConfigChanges.Orientation | 
											 ConfigChanges.UiMode | 
											 ConfigChanges.ScreenLayout | 
											 ConfigChanges.SmallestScreenSize | 
											 ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
