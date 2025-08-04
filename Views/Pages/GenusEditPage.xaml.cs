using OrchidPro.ViewModels.Genera;
using OrchidPro.Constants;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Page for creating and editing botanical genus records with form validation.
/// Handles navigation interception for unsaved changes and provides smooth animations.
/// </summary>
public partial class GenusEditPage : ContentPage, IQueryAttributable
{
    private readonly GenusEditViewModel _viewModel;
    private bool _isNavigating = false;
    private bool _isNavigationHandlerAttached = false;

    /// <summary>
    /// Initialize the genus edit page with dependency injection and setup
    /// </summary>
    public GenusEditPage(GenusEditViewModel viewModel)
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
        await this.SafeExecuteAsync(async () =>
        {
            if (_isNavigating)
            {
                this.LogInfo("Already navigating, allowing navigation");
                return;
            }

            if (_viewModel?.HasUnsavedChanges == true)
            {
                this.LogInfo("Unsaved changes detected, prompting user");
                e.Cancel();

                var leaveConfirmed = await DisplayAlert(
                    "Unsaved Changes",
                    "You have unsaved changes. Are you sure you want to leave?",
                    "Leave", "Stay");

                if (leaveConfirmed)
                {
                    this.LogInfo("User confirmed leaving with unsaved changes");
                    _isNavigating = true;

                    if (_isNavigationHandlerAttached)
                    {
                        Shell.Current.Navigating -= OnShellNavigating;
                        _isNavigationHandlerAttached = false;
                    }

                    await Shell.Current.GoToAsync(e.Target.Location.ToString());
                }
                else
                {
                    this.LogInfo("User chose to stay and continue editing");
                }
            }
        }, "Shell navigation handling");
    }

    /// <summary>
    /// Handle page appearing with animation and navigation handler setup
    /// </summary>
    protected override async void OnAppearing()
    {
        await this.SafeExecuteAsync(async () =>
        {
            base.OnAppearing();

            this.LogInfo("GenusEditPage appearing");

            // Setup navigation handler for unsaved changes
            if (!_isNavigationHandlerAttached)
            {
                Shell.Current.Navigating += OnShellNavigating;
                _isNavigationHandlerAttached = true;
                this.LogInfo("Navigation handler attached for unsaved changes detection");
            }

            // Trigger ViewModel appearing logic
            if (_viewModel != null)
            {
                await _viewModel.OnAppearingAsync();
            }

            this.LogSuccess("GenusEditPage appeared successfully");
        }, "OnAppearing");
    }

    /// <summary>
    /// Handle page disappearing with cleanup
    /// </summary>
    protected override void OnDisappearing()
    {
        this.SafeExecute(() =>
        {
            base.OnDisappearing();

            this.LogInfo("GenusEditPage disappearing");

            // Clean up navigation handler
            if (_isNavigationHandlerAttached)
            {
                Shell.Current.Navigating -= OnShellNavigating;
                _isNavigationHandlerAttached = false;
                this.LogInfo("Navigation handler detached");
            }

            this.LogSuccess("GenusEditPage disappeared successfully");
        }, "OnDisappearing");
    }

    /// <summary>
    /// Handle query attributes from navigation
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Applying query attributes with {query.Count} parameters");

            // Forward to ViewModel
            if (_viewModel != null)
            {
                _viewModel.ApplyQueryAttributes(query);
            }

            this.LogSuccess("Query attributes applied successfully");
        }, "ApplyQueryAttributes");
    }
}