using Android.App;
using Android.Content.PM;
using Android.OS;
using System.Runtime.Versioning;

namespace OrchidPro.Platforms.Android;

[Activity(Theme = "@style/Maui.SplashTheme",
          MainLauncher = true,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetWindowBackground();
    }

    protected override void OnResume()
    {
        base.OnResume();
        SetWindowBackground();
    }

    private void SetWindowBackground()
    {
        try
        {
            if (Window != null)
            {
                var primaryColor = global::Android.Graphics.Color.ParseColor("#A47764");
                var drawable = new global::Android.Graphics.Drawables.ColorDrawable(primaryColor);
                Window.SetBackgroundDrawable(drawable);

                if (OperatingSystem.IsAndroidVersionAtLeast(21))
                {
                    SetStatusAndNavigationBarColors(primaryColor);
                }
            }
        }
        catch
        {
            // Ignora erros de API não disponível
        }
    }

    [SupportedOSPlatform("android21.0")]
    private void SetStatusAndNavigationBarColors(global::Android.Graphics.Color color)
    {
        Window?.SetStatusBarColor(color);
        Window?.SetNavigationBarColor(color);
    }
}