using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OrchidPro.Config;
using OrchidPro.Extensions;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Data;
using OrchidPro.Services.Infrastructure.Supabase.Repositories;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels;
using OrchidPro.ViewModels.Botanical.Families;
using OrchidPro.ViewModels.Botanical.Genera;
using OrchidPro.ViewModels.Botanical.Species;
using OrchidPro.ViewModels.Botanical.Variants;
using OrchidPro.ViewModels.Locations;
using OrchidPro.ViewModels.Mounts;
using OrchidPro.ViewModels.Sources;
using OrchidPro.ViewModels.Substrates;
using OrchidPro.Views.Pages;
using OrchidPro.Views.Pages.Botanical;
using OrchidPro.Views.Pages.Locations;
using OrchidPro.Views.Pages.Mounts;
using OrchidPro.Views.Pages.Sources;
using OrchidPro.Views.Pages.Substrates;
using Syncfusion.Maui.Core.Hosting;

namespace OrchidPro;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        ConfigureServices(builder.Services);
        ConfigureLogging(builder.Logging);
        RegisterRoutes();

        return builder.Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<SupabaseService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Repositories
        services.AddSingleton<IFamilyRepository, SupabaseFamilyRepository>();
        services.AddSingleton<IGenusRepository, SupabaseGenusRepository>();
        services.AddSingleton<ISpeciesRepository, SupabaseSpeciesRepository>();
        services.AddSingleton<IVariantRepository, SupabaseVariantRepository>();
        services.AddSingleton<ISourceRepository, SupabaseSourceRepository>();
        services.AddSingleton<ILocationRepository, SupabaseLocationRepository>();
        services.AddSingleton<IMountRepository, SupabaseMountRepository>();
        services.AddSingleton<ISubstrateRepository, SupabaseSubstrateRepository>();


        // ViewModels
        services.AddTransient<MainPageViewModel>();
        // Botanical
        services.AddTransient<FamiliesListViewModel>();
        services.AddTransient<FamilyEditViewModel>();
        services.AddTransient<GeneraListViewModel>();
        services.AddTransient<GenusEditViewModel>();
        services.AddTransient<GenusItemViewModel>();
        services.AddTransient<SpeciesListViewModel>();
        services.AddTransient<SpeciesEditViewModel>();
        services.AddTransient<SpeciesItemViewModel>();
        services.AddTransient<VariantsListViewModel>();
        services.AddTransient<VariantEditViewModel>();
        services.AddTransient<VariantItemViewModel>();
        // Sources
        services.AddSingleton<SourcesListViewModel>();
        services.AddTransient<SourcesEditViewModel>();
        // Locations  
        services.AddSingleton<LocationsListViewModel>();
        services.AddTransient<LocationsEditViewModel>();
        // Mounts
        services.AddSingleton<MountsListViewModel>();
        services.AddTransient<MountsEditViewModel>();
        // Substrates
        services.AddSingleton<SubstratesListViewModel>();
        services.AddTransient<SubstratesEditViewModel>();

        // Pages
        services.AddTransient<MainPage>();
        services.AddTransient<SplashPage>();
        services.AddTransient<LoginPage>();
        // Botanical
        services.AddTransient<FamiliesListPage>();
        services.AddTransient<FamilyEditPage>();
        services.AddTransient<GeneraListPage>();
        services.AddTransient<GenusEditPage>();
        services.AddTransient<SpeciesListPage>();
        services.AddTransient<SpeciesEditPage>();
        services.AddTransient<VariantsListPage>();
        services.AddTransient<VariantEditPage>();
        // Sources
        services.AddTransient<SourcesListPage>();
        services.AddTransient<SourcesEditPage>();
        // Locations
        services.AddTransient<LocationsListPage>();
        services.AddTransient<LocationsEditPage>();
        // Mounts
        services.AddTransient<MountsListPage>();
        services.AddTransient<MountsEditPage>();
        // Substrates
        services.AddTransient<SubstratesListPage>();
        services.AddTransient<SubstratesEditPage>();

        // Shell
        services.AddSingleton<AppShell>();
    }

    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        if (AppSettings.IsDebugMode)
        {
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Debug);
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        }
    }

    private static void RegisterRoutes()
    {
        // Core routes
        Routing.RegisterRoute("MainPage", typeof(MainPage));
        Routing.RegisterRoute("familyedit", typeof(FamilyEditPage));
        Routing.RegisterRoute("genusedit", typeof(GenusEditPage));
        Routing.RegisterRoute("speciesedit", typeof(SpeciesEditPage));
        Routing.RegisterRoute("variantedit", typeof(VariantEditPage));
    }
}