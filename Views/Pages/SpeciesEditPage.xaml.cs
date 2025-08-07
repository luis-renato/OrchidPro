using OrchidPro.Extensions;
using OrchidPro.ViewModels.Species;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Species edit page following the exact pattern of FamilyEditPage and GenusEditPage.
/// Provides comprehensive species creation and editing with botanical and cultivation features.
/// </summary>
public partial class SpeciesEditPage : ContentPage
{
    private readonly SpeciesEditViewModel _viewModel;

    /// <summary>
    /// Initialize species edit page with dependency injection
    /// </summary>
    public SpeciesEditPage(SpeciesEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        this.LogInfo("SpeciesEditPage initialized with ViewModel - simplified layout");
    }

    #region Lifecycle Events

    /// <summary>
    /// Handle page appearing with animations and data loading
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("SpeciesEditPage appearing - starting initialization");

            // Initialize ViewModel (no animations needed for simple layout)
            await _viewModel.OnAppearingAsync();

            this.LogSuccess("SpeciesEditPage appeared successfully");

        }, "SpeciesEditPage OnAppearing");
    }

    /// <summary>
    /// Handle page disappearing with cleanup
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("SpeciesEditPage disappearing");
            await _viewModel.OnDisappearingAsync();
        }, "SpeciesEditPage OnDisappearing");
    }

    #endregion
}