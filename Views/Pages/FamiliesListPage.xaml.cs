using OrchidPro.ViewModels;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Families list page with enhanced animations and professional UI
/// </summary>
public partial class FamiliesListPage : ContentPage
{
    private readonly FamiliesListViewModel _viewModel;

    public FamiliesListPage(FamiliesListViewModel viewModel)
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

    /// <summary>
    /// Performs dramatic entrance animation
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        // Set initial states for dramatic effect
        RootGrid.Opacity = 0;
        RootGrid.Scale = 0.95;
        FabButton.Scale = 0;
        FabButton.Rotation = -90;

        // Animate main content with fade + scale
        var contentTask = Task.WhenAll(
            RootGrid.FadeTo(1, 600, Easing.CubicOut),
            RootGrid.ScaleTo(1, 600, Easing.SpringOut)
        );

        // Wait for content, then animate FAB
        await contentTask;

        await Task.WhenAll(
            FabButton.ScaleTo(0.9, 400, Easing.SpringOut),
            FabButton.RotateTo(0, 400, Easing.CubicOut)
        );
    }

    /// <summary>
    /// Performs smooth exit animation
    /// </summary>
    private async Task PerformExitAnimation()
    {
        // Animate FAB out first
        var fabTask = Task.WhenAll(
            FabButton.ScaleTo(0, 300, Easing.CubicIn),
            FabButton.RotateTo(90, 300, Easing.CubicIn)
        );

        // Then animate main content
        var contentTask = Task.WhenAll(
            RootGrid.FadeTo(0, 400, Easing.CubicIn),
            RootGrid.ScaleTo(0.95, 400, Easing.CubicIn)
        );

        await Task.WhenAll(fabTask, contentTask);
    }

    /// <summary>
    /// Handles button press animations for better feedback
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
    /// Handles filter selection with action sheet
    /// </summary>
    private async void OnFilterTapped(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            string action = "";

            if (button.Text?.Contains("Status") == true)
            {
                action = await DisplayActionSheet(
                    "Filter by Status",
                    "Cancel",
                    null,
                    _viewModel.StatusFilterOptions.ToArray());

                if (!string.IsNullOrEmpty(action) && action != "Cancel")
                {
                    _viewModel.StatusFilter = action;
                }
            }
            else if (button.Text?.Contains("Sync") == true)
            {
                action = await DisplayActionSheet(
                    "Filter by Sync Status",
                    "Cancel",
                    null,
                    _viewModel.SyncFilterOptions.ToArray());

                if (!string.IsNullOrEmpty(action) && action != "Cancel")
                {
                    _viewModel.SyncFilter = action;
                }
            }
        }
    }
}