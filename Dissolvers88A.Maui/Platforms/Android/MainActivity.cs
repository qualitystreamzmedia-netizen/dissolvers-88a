using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace Dissolvers88A.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.FullUser,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
        | ConfigChanges.KeyboardHidden)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Keep app content below the status bar / above the nav bar instead of
        // drawing edge-to-edge (which clipped the header on some phones).
        if (Window is not null)
            WindowCompat.SetDecorFitsSystemWindows(Window, true);

        ForceResize();
    }

    protected override void OnResume()
    {
        base.OnResume();
        // MAUI sets the window to AdjustPan during startup, which slides the
        // whole page up and buries the R console's input line under the soft
        // keyboard. Force AdjustResize so the page (and the R WebView) shrink
        // instead. Re-applied on every resume in case MAUI resets it.
        ForceResize();
    }

    void ForceResize() => Window?.SetSoftInputMode(SoftInput.AdjustResize);
}
