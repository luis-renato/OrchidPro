using OrchidPro.Services;
using OrchidPro.Services.Data;
using OrchidPro.Views.Pages;
using System.Diagnostics;

namespace OrchidPro;

/// <summary>
/// CORRIGIDO: Main application class com inicialização de singleton services
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// ✅ CORRIGIDO: Creates the application window com inicialização de services
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        Debug.WriteLine("🚀 === APP WINDOW CREATION ===");

        // ✅ CRÍTICO: Garantir que os singleton services sejam inicializados
        _ = Task.Run(async () =>
        {
            try
            {
                Debug.WriteLine("🔧 Initializing singleton services...");

                // ✅ NOVO: Forçar criação e inicialização dos singletons
                var services = IPlatformApplication.Current?.Services;
                if (services != null)
                {
                    Debug.WriteLine("✅ Service provider available");

                    // ✅ FORÇAR inicialização do SupabaseService singleton
                    var supabaseService = services.GetRequiredService<SupabaseService>();
                    await supabaseService.InitializeAsync();
                    Debug.WriteLine("✅ SupabaseService singleton initialized");

                    // ✅ FORÇAR criação dos outros singletons
                    var familyService = services.GetRequiredService<SupabaseFamilyService>();
                    var familyRepo = services.GetRequiredService<IFamilyRepository>();
                    Debug.WriteLine("✅ All singleton services created");
                }
                else
                {
                    Debug.WriteLine("❌ Service provider not available");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error initializing singleton services: {ex.Message}");
            }
        });

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