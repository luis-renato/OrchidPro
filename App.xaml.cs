using OrchidPro.Views.Pages;

namespace OrchidPro;

/// <summary>
/// Main application class that manages app lifecycle and window creation
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the application window with initial page
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Set splash page as entry point
        var splashPage = new SplashPage();

        return new Window(splashPage)
        {
            Title = "OrchidPro",
            MinimumWidth = 400,
            MinimumHeight = 600
        };
    }
}
