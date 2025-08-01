using OrchidPro.ViewModels.Genera;
using OrchidPro.Extensions;
using Syncfusion.Maui.ListView;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Genera list page with enhanced animations and interactions
/// Follows exact pattern from FamiliesListPage with genus-specific adaptations
/// </summary>
public partial class GeneraListPage : ContentPage
{
    private readonly GeneraListViewModel _viewModel;

    /// <summary>
    /// Initialize genera list page with dependency injection
    /// </summary>
    public GeneraListPage(GeneraListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        this.LogInfo("GeneraListPage created with ViewModel binding");
    }

    #region Page Lifecycle

    /// <summary>
    /// Handle page appearing with enhanced animations and data loading
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("=== GENERA LIST PAGE APPEARING ===");

            // ✅ ENHANCED: Animate FAB entrance
            await AnimateFabEntrance();

            // ✅ Enhanced data loading with family context
            if (!_viewModel.HasData)
            {
                this.LogInfo("No genera data - triggering initial load");
                await _viewModel.LoadDataAsync();
            }
            else
            {
                this.LogInfo("Genera data exists - checking for updates");
                await _viewModel.RefreshAsync();
            }

            this.LogSuccess("Genera list page appeared successfully");
        }, "Genera List Page Appearing");
    }

    /// <summary>
    /// Handle page disappearing with cleanup and animations
    /// </summary>
    protected override async void OnDisappearing()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Genera list page disappearing");

            // ✅ Animate FAB exit
            await AnimateFabExit();

            base.OnDisappearing();
        }, "Genera List Page Disappearing");
    }

    #endregion

    #region FAB Animations

    /// <summary>
    /// Animate FAB entrance with bounce effect
    /// </summary>
    private async Task AnimateFabEntrance()
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Start hidden and scaled down
            FabButton.Opacity = 0;
            FabButton.Scale = 0.5;
            FabButton.IsVisible = true;

            // Animate entrance
            var tasks = new[]
            {
                FabButton.FadeTo(1, 300, Easing.CubicOut),
                FabButton.ScaleTo(1, 400, Easing.BounceOut)
            };

            await Task.WhenAll(tasks);
            this.LogInfo("FAB entrance animation completed");
        }, "FAB Entrance Animation");
    }

    /// <summary>
    /// Animate FAB exit with smooth fade
    /// </summary>
    private async Task AnimateFabExit()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (FabButton.IsVisible)
            {
                var tasks = new[]
                {
                    FabButton.FadeTo(0, 200, Easing.CubicIn),
                    FabButton.ScaleTo(0.8, 200, Easing.CubicIn)
                };

                await Task.WhenAll(tasks);
                FabButton.IsVisible = false;
                this.LogInfo("FAB exit animation completed");
            }
        }, "FAB Exit Animation");
    }

    #endregion

    #region ListView Event Handlers

    /// <summary>
    /// Handle ListView item tapped for navigation or selection
    /// </summary>
    private async void OnListViewItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (e.DataItem is GenusItemViewModel genusItem)
            {
                this.LogInfo($"Genus item tapped: {genusItem.Name}");

                if (_viewModel.IsMultiSelectMode)
                {
                    // Toggle selection in multi-select mode
                    genusItem.IsSelected = !genusItem.IsSelected;
                    this.LogInfo($"Toggled selection for: {genusItem.Name} -> {genusItem.IsSelected}");
                }
                else
                {
                    // Navigate to edit in normal mode
                    await _viewModel.EditGenusCommand.ExecuteAsync(genusItem);
                }
            }
        }, "ListView Item Tapped");
    }

    /// <summary>
    /// Handle ListView swipe ended for actions
    /// </summary>
    private async void OnListViewSwipeEnded(object sender, Syncfusion.Maui.ListView.SwipeEndedEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (e.DataItem is GenusItemViewModel genusItem)
            {
                this.LogInfo($"Swipe ended on genus: {genusItem.Name}, Direction: {e.SwipeDirection}");

                if (e.SwipeDirection == SwipeDirection.Left)
                {
                    // Left swipe - Toggle favorite
                    this.LogInfo($"Left swipe - toggling favorite for: {genusItem.Name}");
                    await _viewModel.ToggleFavoriteCommand.ExecuteAsync(genusItem);
                }
                else if (e.SwipeDirection == SwipeDirection.Right)
                {
                    // Right swipe - Delete
                    this.LogInfo($"Right swipe - requesting delete for: {genusItem.Name}");
                    await _viewModel.DeleteGenusCommand.ExecuteAsync(genusItem);
                }
            }
        }, "ListView Swipe Ended");
    }

    #endregion

    #region Search and Filter Handlers

    /// <summary>
    /// Handle search text changes with debouncing
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (sender is Entry searchEntry)
            {
                var newText = e.NewTextValue ?? string.Empty;
                this.LogInfo($"Search text changed: '{newText}'");

                // ViewModel handles debouncing internally
                _viewModel.SearchText = newText;
            }
        }, "Search Text Changed");
    }

    /// <summary>
    /// Handle family filter picker selection
    /// </summary>
    private async void OnFamilyFilterChanged(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Picker picker && picker.SelectedItem is string selectedFamily)
            {
                this.LogInfo($"Family filter changed: {selectedFamily}");

                _viewModel.FamilyFilter = selectedFamily;
                await _viewModel.ApplyFamilyFilterCommand.ExecuteAsync(null);
            }
        }, "Family Filter Changed");
    }

    #endregion

    #region Visual State Management

    /// <summary>
    /// Handle visual state changes based on connection status
    /// </summary>
    private void UpdateConnectionVisualState()
    {
        this.SafeExecute(() =>
        {
            var isConnected = _viewModel.IsConnected;
            this.LogInfo($"Updating connection visual state: {(isConnected ? "Connected" : "Disconnected")}");

            // Update visual states based on connection
            VisualStateManager.GoToState(this, isConnected ? "Connected" : "Disconnected");
        }, "Update Connection Visual State");
    }

    /// <summary>
    /// Handle loading state visual changes
    /// </summary>
    private void UpdateLoadingVisualState()
    {
        this.SafeExecute(() =>
        {
            var isLoading = _viewModel.IsLoading;
            this.LogInfo($"Updating loading visual state: {(isLoading ? "Loading" : "Loaded")}");

            // Update visual states based on loading
            VisualStateManager.GoToState(this, isLoading ? "Loading" : "Loaded");
        }, "Update Loading Visual State");
    }

    #endregion

    #region Enhanced Interactions

    /// <summary>
    /// Handle scroll position changes for FAB visibility
    /// </summary>
    private void OnListViewScrolled(object sender, ScrolledEventArgs e)
    {
        this.SafeExecute(() =>
        {
            // Hide FAB when scrolling down, show when scrolling up
            const double threshold = 50;
            var shouldShowFab = e.ScrollY < threshold;

            if (_viewModel.FabIsVisible != shouldShowFab)
            {
                _viewModel.FabIsVisible = shouldShowFab;
                this.LogInfo($"FAB visibility changed based on scroll: {shouldShowFab}");
            }
        }, "ListView Scrolled");
    }

    /// <summary>
    /// Handle view model property changes for UI updates
    /// </summary>
    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsConnected):
                    UpdateConnectionVisualState();
                    break;

                case nameof(_viewModel.IsLoading):
                    UpdateLoadingVisualState();
                    break;

                case nameof(_viewModel.IsMultiSelectMode):
                    this.LogInfo($"Multi-select mode changed: {_viewModel.IsMultiSelectMode}");
                    break;

                case nameof(_viewModel.SelectedFamily):
                    this.LogInfo($"Selected family changed: {_viewModel.SelectedFamily?.Name ?? "None"}");
                    break;
            }
        }, "ViewModel Property Changed");
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Handle cleanup when page is being destroyed
    /// </summary>
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        this.SafeExecute(() =>
        {
            if (args.NewHandler == null)
            {
                // Page is being destroyed, cleanup event handlers
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                this.LogInfo("Cleaned up GeneraListPage event handlers");
            }
            else if (args.OldHandler == null)
            {
                // Page is being created, setup event handlers
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                this.LogInfo("Setup GeneraListPage event handlers");
            }

            base.OnHandlerChanging(args);
        }, "Handler Changing");
    }

    #endregion
}