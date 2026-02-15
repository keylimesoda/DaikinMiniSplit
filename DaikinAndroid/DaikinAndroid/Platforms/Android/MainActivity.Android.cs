using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace DaikinAndroid.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);

        // Force dark status bar to match dark theme
        if (Window != null)
        {
#pragma warning disable CA1422 // SetStatusBarColor is deprecated on API 35+ but still works
            Window.SetStatusBarColor(Android.Graphics.Color.Black);
#pragma warning restore CA1422
        }
    }

}
