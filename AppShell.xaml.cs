using OrchidPro.Views.Pages;

namespace OrchidPro;

/// <summary>
/// ✅ FINAL CORRIGIDO: AppShell com Footer de Logout e Versão
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

        System.Diagnostics.Debug.WriteLine("✅ [APP_SHELL] Initialized with logout footer");
    }

    /// <summary>
    /// ✅ Registra todas as rotas para navegação
    /// </summary>
    private void RegisterRoutes()
    {
        // Family routes
        Routing.RegisterRoute("familyedit", typeof(FamilyEditPage));
        Routing.RegisterRoute("familieslist", typeof(FamiliesListPage));

        // Debug routes
        Routing.RegisterRoute("testsync", typeof(TestSyncPage));
        Routing.RegisterRoute("login", typeof(LoginPage));

        // Future routes
        // Routing.RegisterRoute("genusedit", typeof(GenusEditPage));
        // Routing.RegisterRoute("speciesedit", typeof(SpeciesEditPage));

        System.Diagnostics.Debug.WriteLine("✅ [APP_SHELL] All navigation routes registered");
    }

    /// <summary>
    /// ✅ SIMPLIFICADO: Handler para logout sem mensagem desnecessária
    /// </summary>
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            // Animação de feedback visual no grid clicado
            if (sender is Grid menuItem)
            {
                await menuItem.ScaleTo(0.96, 120, Easing.CubicOut);
                await menuItem.ScaleTo(1.0, 120, Easing.CubicOut);
            }

            // Confirmação simples e direta
            bool confirm = await DisplayAlert(
                "Sair do OrchidPro",
                "Tem certeza de que deseja sair?",
                "Sim",
                "Cancelar"
            );

            if (confirm)
            {
                try
                {
                    // Get services from DI
                    var services = IPlatformApplication.Current?.Services;
                    if (services != null)
                    {
                        var supabaseService = services.GetRequiredService<OrchidPro.Services.Data.SupabaseService>();
                        var navigationService = services.GetRequiredService<OrchidPro.Services.Navigation.INavigationService>();

                        // Perform logout com logs
                        System.Diagnostics.Debug.WriteLine("🚪 [SHELL] Starting logout process...");
                        supabaseService.Logout();

                        // Navigate to login with transition
                        System.Diagnostics.Debug.WriteLine("🚪 [SHELL] Navigating to login...");
                        await navigationService.NavigateToLoginAsync();

                        System.Diagnostics.Debug.WriteLine("✅ [SHELL] Logout completed successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ [SHELL] Could not get services for logout");
                        await DisplayAlert("Erro", "Não foi possível acessar os serviços do sistema.", "OK");
                    }
                }
                catch (Exception logoutEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ [SHELL] Logout error: {logoutEx.Message}");
                    await DisplayAlert(
                        "Erro ao Sair",
                        $"Ocorreu um erro durante o logout: {logoutEx.Message}",
                        "OK"
                    );
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("🚪 [SHELL] Logout cancelled by user");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [SHELL] General logout error: {ex.Message}");
            await DisplayAlert(
                "Erro",
                $"Erro inesperado: {ex.Message}",
                "OK"
            );
        }
    }

    /// <summary>
    /// ✅ NOVO: Method para obter informações de versão dinamicamente
    /// </summary>
    public static string GetAppVersion()
    {
        try
        {
            return AppInfo.Current.VersionString;
        }
        catch
        {
            return "1.0.0";
        }
    }

    /// <summary>
    /// ✅ NOVO: Method para obter build number
    /// </summary>
    public static string GetBuildInfo()
    {
        try
        {
            var buildDate = DateTime.Now.ToString("yyyy.MM.dd");
            return $"Build {buildDate}";
        }
        catch
        {
            return "Build 2025.01.25";
        }
    }
}