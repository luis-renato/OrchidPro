using OrchidPro.ViewModels.Substrates;
using OrchidPro.Views.Base;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages.Substrates;

public partial class SubstratesEditPage : ContentPage, IQueryAttributable
{
    private readonly BaseEditPageLogic<Models.Substrate> _base;

    public SubstratesEditPage(SubstratesEditViewModel viewModel)
    {
        InitializeComponent();
        _base = new BaseEditPageLogic<Models.Substrate>(viewModel);
        _base.SetupPage(this);
        this.LogInfo("🚀 REFACTORED SubstratesEditPage - using BaseEditPageLogic composition");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) => _base.HandleQueryAttributes(query);
    protected override async void OnAppearing() { base.OnAppearing(); await _base.BaseOnAppearing(); }
    protected override async void OnDisappearing() { await _base.BaseOnDisappearing(); base.OnDisappearing(); }
    protected override bool OnBackButtonPressed() => _base.HandleBackButtonPressed();

    // All event handlers delegated to base
    private void OnSaveButtonTapped(object? sender, EventArgs e) => _base.HandleSaveButtonTapped(sender, e);
    private void OnCancelButtonTapped(object? sender, EventArgs e) => _base.HandleCancelButtonTapped(sender, e);
    private void OnDeleteButtonTapped(object? sender, EventArgs e) => _base.HandleDeleteButtonTapped(sender, e);
    private void OnSaveAndContinueButtonTapped(object? sender, EventArgs e) => _base.HandleSaveAndContinueButtonTapped(sender, e);
    private void OnEntryFocused(object? sender, FocusEventArgs e) => _base.HandleEntryFocused(sender, e);
    private void OnEntryUnfocused(object? sender, FocusEventArgs e) => _base.HandleEntryUnfocused(sender, e);
    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e) => _base.HandleEditorTextChanged(sender, e);
    private void OnPickerSelectionChanged(object? sender, EventArgs e) => _base.HandlePickerSelectionChanged(sender, e);
    private void OnSwitchToggled(object? sender, ToggledEventArgs e) => _base.HandleSwitchToggled(sender, e);
}