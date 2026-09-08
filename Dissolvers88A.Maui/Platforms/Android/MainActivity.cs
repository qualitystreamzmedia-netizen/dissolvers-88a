using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace Dissolvers88A.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.FullUser,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Keep app content below the status bar / above the nav bar instead of
        // drawing edge-to-edge (which clipped the header on some phones).
        if (Window is not null)
            WindowCompat.SetDecorFitsSystemWindows(Window, true);
    }
}
