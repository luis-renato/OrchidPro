using OrchidPro.ViewModels;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Family edit page with enhanced animations and professional UI
/// </summary>
public partial class FamilyEditPage : ContentPage
{
    private readonly FamilyEditViewModel _viewModel;

    public FamilyEditPage(FamilyEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Perform entrance animation
        await PerformEntranceAnimation();

        // Initialize ViewModel
        await _viewModel.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // Perform exit animation
        await PerformExitAnimation();

        // Cleanup ViewModel
        await _viewModel.OnDisappearingAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        // Handle back button with unsaved changes check
        _ = Task.Run(async () =>
        {
            // Simulate back button check (simplified)
            if (_viewModel.HasUnsavedChanges)
            {
                var canNavigate = await _viewModel.ShowConfirmAsync(
                    "Unsaved Changes",
                    "You have unsaved changes. Discard them?");

                if (canNavigate)
                {
                    await Shell.Current.GoToAsync("..");
                }
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        });

        return true; // Always handle the back button
    }

    /// <summary>
    /// Performs enhanced entrance animation
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        // Set initial states
        RootGrid.Opacity = 0;
        RootGrid.Scale = 0.95;
        RootGrid.TranslationY = 30;

        // Animate with multiple effects
        await Task.WhenAll(
            RootGrid.FadeTo(1, 600, Easing.CubicOut),
            RootGrid.ScaleTo(1, 600, Easing.SpringOut),
            RootGrid.TranslateTo(0, 0, 600, Easing.CubicOut)
        );
    }

    /// <summary>
    /// Performs smooth exit animation
    /// </summary>
    private async Task PerformExitAnimation()
    {
        await Task.WhenAll(
            RootGrid.FadeTo(0, 400, Easing.CubicIn),
            RootGrid.ScaleTo(0.95, 400, Easing.CubicIn),
            RootGrid.TranslateTo(0, -20, 400, Easing.CubicIn)
        );
    }

    /// <summary>
    /// Handles entry focus with animation
    /// </summary>
    private async void OnEntryFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && e.IsFocused)
        {
            // Animate field focus
            if (entry.Parent is Border border)
            {
                await border.ScaleTo(1.02, 200, Easing.CubicOut);
            }
        }
    }

    /// <summary>
    /// Handles entry unfocus with animation
    /// </summary>
    private async void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && !e.IsFocused)
        {
            // Animate field unfocus
            if (entry.Parent is Border border)
            {
                await border.ScaleTo(1, 200, Easing.CubicOut);
            }
        }
    }

    /// <summary>
    /// Handles button press animations
    /// </summary>
    private async void OnButtonPressed(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            await button.ScaleTo(0.95, 50, Easing.CubicOut);
            await button.ScaleTo(1, 50, Easing.CubicOut);
        }
    }

    /// <summary>
    /// Handles switch toggle with visual feedback
    /// </summary>
    private async void OnSwitchToggled(object sender, ToggledEventArgs e)
    {
        if (sender is Switch switchControl)
        {
            // Visual feedback only
            await switchControl.ScaleTo(1.1, 100, Easing.CubicOut);
            await switchControl.ScaleTo(1, 100, Easing.CubicOut);
        }
    }
}