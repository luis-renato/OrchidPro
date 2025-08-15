using CommunityToolkit.Mvvm.Input;
using OrchidPro.Constants;
using OrchidPro.Extensions;
using OrchidPro.Models.Base;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.Views.Pages.Base;

/// <summary>
/// UNIFIED Logic class containing ALL edit page functionality for composition.
/// Handles navigation interception, animations, lifecycle, and form interactions.
/// Equivalent to BaseListPageLogic but for edit/create pages.
/// </summary>
public class BaseEditPageLogic<T> where T : class, IBaseEntity, new()
{
    private readonly BaseEditViewModel<T> _viewModel;
    private ContentPage? _page;
    private bool _isNavigating = false;
    private bool _isNavigationHandlerAttached = false;

    public BaseEditPageLogic(BaseEditViewModel<T> viewModel)
    {
        _viewModel = viewModel;
        this.LogInfo($"Initialized BaseEditPageLogic for {typeof(T).Name}");
    }

    public void SetupPage(ContentPage page)
    {
        _page = page;
        _page.BindingContext = _viewModel;
        this.LogInfo($"🔧 Edit page setup completed for {typeof(T).Name}");
    }

    #region Navigation Interception

    /// <summary>
    /// Intercept navigation events to handle unsaved changes confirmation
    /// </summary>
    public async void HandleShellNavigating(object? sender, ShellNavigatingEventArgs e)
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
    /// Attach navigation event handler
    /// </summary>
    public void AttachNavigationHandler()
    {
        this.SafeExecute(() =>
        {
            if (!_isNavigationHandlerAttached)
            {
                Shell.Current.Navigating += HandleShellNavigating;
                _isNavigationHandlerAttached = true;
                this.LogInfo("Navigation handler attached");
            }
        }, "AttachNavigationHandler");
    }

    /// <summary>
    /// Remove navigation event handler safely
    /// </summary>
    public void DetachNavigationHandler()
    {
        this.SafeExecute(() =>
        {
            if (_isNavigationHandlerAttached)
            {
                Shell.Current.Navigating -= HandleShellNavigating;
                _isNavigationHandlerAttached = false;
                this.LogInfo("Navigation handler detached");
            }
        }, "DetachNavigationHandler");
    }

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// Handle page appearing with navigation setup and animations
    /// </summary>
    public async Task BaseOnAppearing()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"OnAppearing - Mode: {(_viewModel.IsEditMode ? "EDIT" : "CREATE")}");

            // Always attach navigation handler
            AttachNavigationHandler();

            // Animation and initialization in parallel
            var animationTask = PerformEntranceAnimation();
            var initTask = _viewModel.OnAppearingAsync();

            // Wait for both to complete
            await Task.WhenAll(animationTask, initTask);

            this.LogInfo("Page fully loaded and initialized");
        }, "OnAppearing");
    }

    /// <summary>
    /// Handle page disappearing with cleanup and animations
    /// </summary>
    public async Task BaseOnDisappearing()
    {
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
    public bool HandleBackButtonPressed()
    {
        // Check if already navigating to avoid multiple dialogs
        if (_isNavigating)
            return true;

        // For physical button, redirect to Cancel command
        _ = Task.Run(async () =>
        {
            await this.SafeExecuteAsync(async () =>
            {
                this.LogInfo("Physical back button pressed - calling CancelCommand");
                _isNavigating = true;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (_viewModel.CancelCommand.CanExecute(null))
                    {
                        await _viewModel.CancelCommand.ExecuteAsync(null);
                    }
                });

                _isNavigating = false;
            }, "OnBackButtonPressed");
        });

        // Always prevent default behavior to handle via command
        return true;
    }

    #endregion

    #region Animation Methods

    /// <summary>
    /// Smooth entrance animation for better user experience
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        if (_page == null) return;

        await this.SafeExecuteAsync(async () =>
        {
            // Start with invisible content
            _page.Opacity = 0;
            _page.TranslationY = 20;

            // Animate to visible
            var fadeIn = _page.FadeTo(1, AnimationConstants.PAGE_ENTRANCE_DURATION, Easing.CubicOut);
            var slideUp = _page.TranslateTo(0, 0, AnimationConstants.PAGE_ENTRANCE_DURATION, Easing.CubicOut);

            await Task.WhenAll(fadeIn, slideUp);

            this.LogInfo("Entrance animation completed");
        }, "Entrance Animation");
    }

    /// <summary>
    /// Smooth exit animation for better user experience
    /// </summary>
    private async Task PerformExitAnimation()
    {
        if (_page == null) return;

        await this.SafeExecuteAsync(async () =>
        {
            var fadeOut = _page.FadeTo(0.8, AnimationConstants.PAGE_EXIT_DURATION, Easing.CubicIn);
            var slideDown = _page.TranslateTo(0, 10, AnimationConstants.PAGE_EXIT_DURATION, Easing.CubicIn);

            await Task.WhenAll(fadeOut, slideDown);

            this.LogInfo("Exit animation completed");
        }, "Exit Animation");
    }

    #endregion

    #region Query Attributes Management

    /// <summary>
    /// Handle navigation parameters from Shell routing system
    /// </summary>
    public void HandleQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ApplyQueryAttributes called with {query.Count} parameters");

            foreach (var param in query)
            {
                this.LogInfo($"Parameter: {param.Key} = {param.Value} ({param.Value?.GetType().Name})");
            }

            // Pass parameters to ViewModel if it supports IQueryAttributable
            if (_viewModel is IQueryAttributable queryAttributable)
            {
                queryAttributable.ApplyQueryAttributes(query);
                this.LogInfo("Parameters applied to ViewModel");
            }
            else
            {
                this.LogWarning("ViewModel does not implement IQueryAttributable");
            }
        }, "ApplyQueryAttributes");
    }

    #endregion

    #region Form Interaction Handlers

    /// <summary>
    /// Handle save button tapped
    /// </summary>
    public async void HandleSaveButtonTapped(object? sender, EventArgs e)
    {
        try
        {
            if (_viewModel.SaveCommand.CanExecute(null))
            {
                await _viewModel.SaveCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error handling save button");
        }
    }

    /// <summary>
    /// Handle cancel button tapped
    /// </summary>
    public async void HandleCancelButtonTapped(object? sender, EventArgs e)
    {
        try
        {
            if (_viewModel.CancelCommand.CanExecute(null))
            {
                await _viewModel.CancelCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error handling cancel button");
        }
    }

    /// <summary>
    /// Handle delete button tapped (if available)
    /// </summary>
    public async void HandleDeleteButtonTapped(object? sender, EventArgs e)
    {
        try
        {
            // Check if ViewModel has DeleteCommand (for hierarchical ViewModels)
            var deleteCommand = _viewModel.GetType().GetProperty("DeleteCommand")?.GetValue(_viewModel);
            if (deleteCommand is IAsyncRelayCommand asyncCommand)
            {
                if (asyncCommand.CanExecute(null))
                {
                    await asyncCommand.ExecuteAsync(null);
                }
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error handling delete button");
        }
    }

    /// <summary>
    /// Handle save and continue button tapped (if available)
    /// </summary>
    public async void HandleSaveAndContinueButtonTapped(object? sender, EventArgs e)
    {
        try
        {
            // Check if ViewModel has SaveAndContinueCommand (for continuous ViewModels)
            var saveAndContinueCommand = _viewModel.GetType().GetProperty("SaveAndContinueCommand")?.GetValue(_viewModel);
            if (saveAndContinueCommand is IAsyncRelayCommand asyncCommand)
            {
                if (asyncCommand.CanExecute(null))
                {
                    await asyncCommand.ExecuteAsync(null);
                }
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error handling save and continue button");
        }
    }

    /// <summary>
    /// Handle create new parent button tapped (for hierarchical forms)
    /// </summary>
    public async void HandleCreateNewParentButtonTapped(object? sender, EventArgs e)
    {
        try
        {
            // Check if ViewModel has CreateNewParentCommand (for hierarchical ViewModels)
            var createParentCommand = _viewModel.GetType().GetProperty("CreateNewParentCommand")?.GetValue(_viewModel);
            if (createParentCommand is IAsyncRelayCommand asyncCommand)
            {
                if (asyncCommand.CanExecute(null))
                {
                    await asyncCommand.ExecuteAsync(null);
                }
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error handling create new parent button");
        }
    }

    #endregion

    #region Form Focus Handlers

    /// <summary>
    /// Handle entry focused events
    /// </summary>
    public void HandleEntryFocused(object? sender, FocusEventArgs e)
    {
        // Intentionally empty - can be overridden for specific behavior
    }

    /// <summary>
    /// Handle entry unfocused events
    /// </summary>
    public void HandleEntryUnfocused(object? sender, FocusEventArgs e)
    {
        // Intentionally empty - can be overridden for specific behavior
    }

    /// <summary>
    /// Handle editor text changed events
    /// </summary>
    public void HandleEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Can be used for character counting, validation, etc.
    }

    /// <summary>
    /// Handle picker selection changed events
    /// </summary>
    public void HandlePickerSelectionChanged(object? sender, EventArgs e)
    {
        // Intentionally empty - binding handles the data updates
    }

    /// <summary>
    /// Handle switch toggled events
    /// </summary>
    public void HandleSwitchToggled(object? sender, ToggledEventArgs e)
    {
        // Intentionally empty - binding handles the data updates
    }

    #endregion

    #region Validation Display Helpers

    /// <summary>
    /// Show validation error for a specific field
    /// </summary>
    public async Task ShowValidationErrorAsync(string fieldName, string message)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (_page != null)
            {
                await _page.DisplayAlert("Validation Error", $"{fieldName}: {message}", "OK");
            }
        }, "Show Validation Error");
    }

    /// <summary>
    /// Show general form validation summary
    /// </summary>
    public async Task ShowFormValidationSummaryAsync(List<string> errors)
    {
        if (errors?.Any() != true) return;

        await this.SafeExecuteAsync(async () =>
        {
            var errorMessage = string.Join("\n• ", errors);
            if (_page != null)
            {
                await _page.DisplayAlert("Please correct the following errors:", $"• {errorMessage}", "OK");
            }
        }, "Show Form Validation Summary");
    }

    #endregion

    #region Utility Properties and Methods

    /// <summary>
    /// Check if the form has unsaved changes
    /// </summary>
    public bool HasUnsavedChanges => _viewModel.HasUnsavedChanges;

    /// <summary>
    /// Check if the form is in edit mode
    /// </summary>
    public bool IsEditMode => _viewModel.IsEditMode;

    /// <summary>
    /// Get the current entity name for display
    /// </summary>
    public string EntityName => _viewModel.EntityName;

    /// <summary>
    /// Force refresh of CanExecute states for all commands
    /// </summary>
    public void RefreshCommandStates()
    {
        this.SafeExecute(() =>
        {
            _viewModel.SaveCommand.NotifyCanExecuteChanged();
            _viewModel.CancelCommand.NotifyCanExecuteChanged();

            // Refresh additional commands if they exist
            var deleteCommand = _viewModel.GetType().GetProperty("DeleteCommand")?.GetValue(_viewModel);
            if (deleteCommand is IAsyncRelayCommand deleteAsyncCommand)
            {
                deleteAsyncCommand.NotifyCanExecuteChanged();
            }

            var saveAndContinueCommand = _viewModel.GetType().GetProperty("SaveAndContinueCommand")?.GetValue(_viewModel);
            if (saveAndContinueCommand is IAsyncRelayCommand saveAndContinueAsyncCommand)
            {
                saveAndContinueAsyncCommand.NotifyCanExecuteChanged();
            }

            this.LogInfo("Command states refreshed");
        }, "Refresh Command States");
    }

    /// <summary>
    /// Show success message to user
    /// </summary>
    public async Task ShowSuccessAsync(string message)
    {
        if (_page != null)
        {
            await _page.ShowSuccessToast(message);
        }
    }

    /// <summary>
    /// Show error message to user
    /// </summary>
    public async Task ShowErrorAsync(string message)
    {
        if (_page != null)
        {
            await _page.ShowErrorToast(message);
        }
    }

    /// <summary>
    /// Show confirmation dialog
    /// </summary>
    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        if (_page != null)
        {
            return await _page.DisplayAlert(title, message, accept, cancel);
        }
        return false;
    }

    #endregion
}