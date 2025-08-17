using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// Genus list ViewModel - ULTRA CLEAN using BaseListViewModel core
/// FIXED: Override OnAppearingAsync to use GetAllWithFamilyAsync for proper Family loading
/// </summary>
public partial class GeneraListViewModel : BaseListViewModel<Models.Genus, GenusItemViewModel>
{
    #region Private Fields

    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Genus";
    public override string EntityNamePlural => "Genera";
    public override string EditRoute => "genusedit";

    #endregion

    #region Constructor

    public GeneraListViewModel(IGenusRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _genusRepository = repository;
        this.LogInfo("🚀 CLEAN GeneraListViewModel initialized using BaseListViewModel core");
    }

    #endregion

    #region ONLY REQUIRED: CreateItemViewModel

    /// <summary>
    /// Only required override - creates GenusItemViewModel instances
    /// </summary>
    protected override GenusItemViewModel CreateItemViewModel(Models.Genus entity)
    {
        return new GenusItemViewModel(entity);
    }

    #endregion

    #region Genus-Specific: IMMEDIATE Family Loading

    /// <summary>
    /// Override OnAppearingAsync to load genera WITH family data immediately
    /// FIXES: "Unknown Family" issue by loading genera with family data from the start
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        try
        {
            this.LogInfo("GeneraListViewModel OnAppearing - loading with family data");

            // First, load with family data immediately to avoid "Unknown Family"
            await LoadGeneraWithFamilyAsync();

            // Then call base for standard functionality
            await base.OnAppearingAsync();
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during Genus ViewModel appearing");
            throw;
        }
    }

    /// <summary>
    /// Load genera with family data immediately on first load
    /// REPLACES: Family hydration monitoring with immediate loading
    /// </summary>
    private async Task LoadGeneraWithFamilyAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            IsLoading = true;

            try
            {
                this.LogInfo("🔍 Loading genera WITH family data immediately");

                // Use GetAllWithFamilyAsync to load genera with family data from start
                var generaWithFamily = await _genusRepository.GetAllWithFamilyAsync(true);

                // Populate Items collection
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Items.Clear();
                    foreach (var genus in generaWithFamily)
                    {
                        var itemViewModel = CreateItemViewModel(genus);
                        Items.Add(itemViewModel);
                    }
                });

                this.LogSuccess($"✅ Loaded {generaWithFamily.Count} genera with family data immediately");
            }
            finally
            {
                IsLoading = false;
            }
        }, "Load Genera With Family Immediate");
    }

    #endregion

    #region REMOVED - Now in Base

    // ❌ REMOVED: RefreshGeneraAsync - Use base RefreshAsync
    // ❌ REMOVED: RefreshGeneraCommand - Use base RefreshCommand
    // ❌ REMOVED: All UI compatibility exposures - Use base commands directly
    // ❌ REMOVED: _hasInitialized flag - Base handles initialization
    // ❌ REMOVED: Custom refresh logic - Base handles all refresh scenarios

    #endregion
}