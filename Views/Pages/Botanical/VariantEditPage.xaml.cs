using OrchidPro.ViewModels.Variants;
using OrchidPro.Views.Base;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages;

/// <summary>
/// REFACTORED Variant Edit Page using BaseEditPageLogic composition.
/// Reduced from 200+ lines to ~30 lines using the base edit page logic.
/// Follows exact same pattern as FamilyEditPage with BaseEditPageLogic.
/// </summary>
public partial class VariantEditPage : ContentPage, IQueryAttributable
{
    private readonly BaseEditPageLogic<Models.Variant> _base;

    /// <summary>
    /// Initialize the variant edit page with dependency injection and composition
    /// </summary>
    public VariantEditPage(VariantEditViewModel viewModel)
    {
        InitializeComponent();

        // TRUE composition - use the base logic directly (same pattern as family)
        _base = new BaseEditPageLogic<Models.Variant>(viewModel);
        _base.SetupPage(this);

        this.LogInfo("🚀 REFACTORED VariantEditPage - using BaseEditPageLogic composition");
    }

    #region Query Attributes Management

    /// <summary>
    /// Handle navigation parameters - delegates to base logic
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _base.HandleQueryAttributes(query);
    }

    #endregion

    #region Lifecycle Events - Delegated to Base

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _base.BaseOnAppearing();
    }

    protected override async void OnDisappearing()
    {
        await _base.BaseOnDisappearing();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        return _base.HandleBackButtonPressed();
    }

    #endregion

    #region Event Handlers - All Delegated to Base

    // Core form handlers - FIXED: Removed async since base handlers are already async void
    private void OnSaveButtonTapped(object? sender, EventArgs e) => _base.HandleSaveButtonTapped(sender, e);
    private void OnCancelButtonTapped(object? sender, EventArgs e) => _base.HandleCancelButtonTapped(sender, e);
    private void OnDeleteButtonTapped(object? sender, EventArgs e) => _base.HandleDeleteButtonTapped(sender, e);
    private void OnSaveAndContinueButtonTapped(object? sender, EventArgs e) => _base.HandleSaveAndContinueButtonTapped(sender, e);

    // Focus handlers
    private void OnEntryFocused(object? sender, FocusEventArgs e) => _base.HandleEntryFocused(sender, e);
    private void OnEntryUnfocused(object? sender, FocusEventArgs e) => _base.HandleEntryUnfocused(sender, e);
    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e) => _base.HandleEditorTextChanged(sender, e);
    private void OnPickerSelectionChanged(object? sender, EventArgs e) => _base.HandlePickerSelectionChanged(sender, e);
    private void OnSwitchToggled(object? sender, ToggledEventArgs e) => _base.HandleSwitchToggled(sender, e);

    #endregion

    #region Optional Variant-Specific Customizations

    /// <summary>
    /// Example: Custom handler for variant-specific control if needed
    /// </summary>
    private void OnVariantTypeChanged(object sender, TextChangedEventArgs e)
    {
        // Delegate to base text handler
        _base.HandleEditorTextChanged(sender, e);

        // Add variant-specific logic if needed
        this.LogInfo($"Variant type changed: {e.NewTextValue?.Length ?? 0} characters");
    }

    #endregion
}