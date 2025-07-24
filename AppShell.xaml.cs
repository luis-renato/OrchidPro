using OrchidPro.Views.Pages;

namespace OrchidPro;

/// <summary>
/// ✅ FINAL CORRIGIDO: AppShell sem dependências no construtor
/// </summary>
public partial class AppShell : Shell
{
    public AppShell()
    {
        // Register Syncfusion license
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mzk1ODIxMEAzMzMwMmUzMDJlMzAzYjMzMzAzYkRoeUFQTmxzYk00RkNhN0M0QTZtYzZPcWF5YnViT3Z0Y2tMZlcvWGh4R289");

        InitializeComponent();

        // Register all navigation routes
        RegisterRoutes();

        System.Diagnostics.Debug.WriteLine("✅ [APP_SHELL] Initialized without constructor dependencies");
    }

    /// <summary>
    /// ✅ Registra todas as rotas para navegação
    /// </summary>
    private void RegisterRoutes()
    {
        // Family routes
        Routing.RegisterRoute("familyedit", typeof(FamilyEditPage));
        Routing.RegisterRoute("familieslist", typeof(FamiliesListSyncfusionPage));

        // Debug routes
        Routing.RegisterRoute("testsync", typeof(TestSyncPage));
        Routing.RegisterRoute("login", typeof(LoginPage));

        // Future routes
        // Routing.RegisterRoute("genusedit", typeof(GenusEditPage));
        // Routing.RegisterRoute("speciesedit", typeof(SpeciesEditPage));

        System.Diagnostics.Debug.WriteLine("✅ [APP_SHELL] All navigation routes registered");
    }

    /// <summary>
    /// ✅ NOVO: Handler para logout (se houver botão de logout no Shell)
    /// </summary>
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Logout",
            "Are you sure you want to sign out?",
            "Yes",
            "No"
        );

        if (confirm)
        {
            try
            {
                // Get SupabaseService from DI
                var services = IPlatformApplication.Current?.Services;
                if (services != null)
                {
                    var supabaseService = services.GetRequiredService<OrchidPro.Services.Data.SupabaseService>();
                    var navigationService = services.GetRequiredService<OrchidPro.Services.Navigation.INavigationService>();

                    // Perform logout
                    supabaseService.Logout();

                    // Navigate to login
                    await navigationService.NavigateToLoginAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
            }
        }
    }
}