using OrchidPro.ViewModels.Families;
using OrchidPro.Constants;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Page for creating and editing botanical family records with form validation.
/// Handles navigation interception for unsaved changes and provides smooth animations.
/// </summary>
public partial class FamilyEditPage : ContentPage, IQueryAttributable
{
    private readonly FamilyEditViewModel _viewModel;
    private bool _isNavigating = false;
    private bool _isNavigationHandlerAttached = false;

    /// <summary>
    /// Initialize the family edit page with dependency injection and setup
    /// </summary>
    public FamilyEditPage(FamilyEditViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;

        var success = this.SafeExecute(() =>
        {
            InitializeComponent();
            this.LogSuccess("InitializeComponent completed successfully");
        }, "InitializeComponent");

        if (!success)
        {
            this.LogError("InitializeComponent failed");
        }

        this.LogInfo("Initialized successfully");
    }

    /// <summary>
    /// Intercept navigation events to handle unsaved changes confirmation
    /// </summary>
    private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        // Only intercept back navigation from toolbar
        if (_isNavigating || (e.Source != ShellNavigationSource.Pop && e.Source != ShellNavigationSource.PopToRoot))
            return;

        this.LogInfo($"Toolbar navigation detected - HasUnsavedChanges: {_viewModel.HasUnsavedChanges}");

        // Only intercept if there are unsaved changes
        if (_viewModel.HasUnsavedChanges)
        {
            // Cancel navigation to intercept
            e.Cancel();
            _isNavigating = true;

            await this.SafeExecuteAsync(async () =>
            {
                // Remove handler BEFORE calling CancelCommand to avoid interference
                DetachNavigationHandler();

                this.LogInfo("Handler removed, delegating to CancelCommand");

                if (_viewModel.CancelCommand.CanExecute(null))
                {
                    await _viewModel.CancelCommand.ExecuteAsync(null);
                }
            }, "Navigation handler");

            _isNavigating = false;
        }
        // If no changes, allow normal navigation (don't cancel)
    }

    /// <summary>
    /// Remove navigation event handler safely
    /// </summary>
    private void DetachNavigationHandler()
    {
        this.SafeExecute(() =>
        {
            if (_isNavigationHandlerAttached)
            {
                Shell.Current.Navigating -= OnShellNavigating;
                _isNavigationHandlerAttached = false;
                this.LogInfo("Navigation handler detached");
            }
        }, "DetachNavigationHandler");
    }

    #region Query Attributes Management

    /// <summary>
    /// Handle navigation parameters from Shell routing system
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ApplyQueryAttributes called with {query.Count} parameters");

            foreach (var param in query)
            {
                this.LogInfo($"Parameter: {param.Key} = {param.Value} ({param.Value?.GetType().Name})");
            }

            // Pass parameters to ViewModel
            _viewModel.ApplyQueryAttributes(query);

            this.LogSuccess("Parameters applied to ViewModel");
        }, "ApplyQueryAttributes");
    }

    #endregion

    #region Page Lifecycle Management

    /// <summary>
    /// Handle page appearing with navigation setup and animations
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"OnAppearing - Mode: {(_viewModel.IsEditMode ? "EDIT" : "CREATE")}");

            // Always intercept navigation from toolbar - simpler approach
            if (!_isNavigationHandlerAttached)
            {
                Shell.Current.Navigating += OnShellNavigating;
                _isNavigationHandlerAttached = true;
                this.LogInfo("Navigation handler attached (always active)");
            }

            // Animation and initialization in parallel
            var animationTask = PerformEntranceAnimation();
            var initTask = _viewModel.OnAppearingAsync();

            // Wait for both to complete
            await Task.WhenAll(animationTask, initTask);

            this.LogSuccess("Page fully loaded and initialized");
        }, "OnAppearing");
    }

    /// <summary>
    /// Handle page disappearing with cleanup and animations
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("OnDisappearing");

            // Always remove handler
            DetachNavigationHandler();

            // Perform exit animation
            await PerformExitAnimation();

            // Cleanup ViewModel
            await _viewModel.OnDisappearingAsync();
        }, "OnDisappearing");
    }

    /// <summary>
    /// Handle Android physical back button with unsaved changes check
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        // Check if already navigating to avoid multiple dialogs
        if (_isNavigating)
            return true;

        // For physical button, redirect to Cancel command from base class
        _ = Task.Run(async () =>
        {
            await this.SafeExecuteAsync(async () =>
            {
                this.LogInfo("Physical back button pressed - calling CancelCommand");
                _isNavigating = true;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Use Cancel command from base class which has all the logic
                    if (_viewModel.CancelCommand.CanExecute(null))
                    {
                        await _viewModel.CancelCommand.ExecuteAsync(null);
                    }
                });
            }, "Back button handler");

            _isNavigating = false;
        });

        // Prevent default back button behavior
        return true;
    }

    #endregion

    #region Animation Management

    /// <summary>
    /// Perform smooth entrance animation for better user experience
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Use extension method for standardized page entrance
            await Content.PerformStandardEntranceAsync();

            this.LogSuccess("Entrance animation completed");
        }, "Page entrance animation");
    }

    /// <summary>
    /// Perform smooth exit animation for better user experience
    /// </summary>
    private async Task PerformExitAnimation()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Use extension method for standardized page exit
            await Content.PerformStandardExitAsync();

            this.LogSuccess("Exit animation completed");
        }, "Page exit animation");
    }

    #endregion
}