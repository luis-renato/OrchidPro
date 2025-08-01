using OrchidPro.ViewModels.Genera;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Genus edit page with enhanced animations and form interactions
/// Follows exact pattern from FamilyEditPage with genus-specific adaptations
/// </summary>
public partial class GenusEditPage : ContentPage
{
    private readonly GenusEditViewModel _viewModel;

    /// <summary>
    /// Initialize genus edit page with dependency injection
    /// </summary>
    public GenusEditPage(GenusEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        this.LogInfo("GenusEditPage created with ViewModel binding");
    }

    #region Page Lifecycle

    /// <summary>
    /// Handle page appearing with smooth animations and data loading
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("=== GENUS EDIT PAGE APPEARING ===");
            this.LogInfo($"Edit Mode: {_viewModel.IsEditMode}");
            this.LogInfo($"Entity Name: {_viewModel.EntityName}");

            // ✅ Enhanced entrance animations
            await AnimatePageEntrance();

            // ✅ Initialize data if needed
            if (_viewModel.IsEditMode && string.IsNullOrEmpty(_viewModel.Name))
            {
                this.LogInfo("Edit mode but no data loaded - triggering load");
                // ViewModel will handle loading via ApplyQueryAttributes
            }

            this.LogSuccess("Genus edit page appeared successfully");
        }, "Genus Edit Page Appearing");
    }

    /// <summary>
    /// Handle page disappearing with cleanup
    /// </summary>
    protected override async void OnDisappearing()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Genus edit page disappearing");

            // ✅ Animate page exit
            await AnimatePageExit();

            base.OnDisappearing();
        }, "Genus Edit Page Disappearing");
    }

    #endregion

    #region Page Animations

    /// <summary>
    /// Animate page entrance with staggered effects
    /// </summary>
    private async Task AnimatePageEntrance()
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Find all form fields for animation
            var formFields = GetAllFormFields();

            // Start all fields hidden and shifted
            foreach (var field in formFields)
            {
                field.Opacity = 0;
                field.TranslationY = 30;
            }

            // Animate each field with staggered timing
            var animationTasks = new List<Task>();
            for (int i = 0; i < formFields.Count; i++)
            {
                var field = formFields[i];
                var delay = i * 100; // 100ms stagger

                animationTasks.Add(AnimateFieldEntrance(field, delay));
            }

            await Task.WhenAll(animationTasks);
            this.LogInfo("Page entrance animations completed");
        }, "Page Entrance Animation");
    }

    /// <summary>
    /// Animate individual field entrance
    /// </summary>
    private async Task AnimateFieldEntrance(View field, int delay)
    {
        await this.SafeExecuteAsync(async () =>
        {
            await Task.Delay(delay);

            var tasks = new[]
            {
                field.FadeTo(1, 300, Easing.CubicOut),
                field.TranslateTo(0, 0, 400, Easing.CubicOut)
            };

            await Task.WhenAll(tasks);
        }, "Field Entrance Animation");
    }

    /// <summary>
    /// Animate page exit with fade
    /// </summary>
    private async Task AnimatePageExit()
    {
        await this.SafeExecuteAsync(async () =>
        {
            var formFields = GetAllFormFields();
            var animationTasks = formFields.Select(field =>
                field.FadeTo(0.5, 200, Easing.CubicIn)
            );

            await Task.WhenAll(animationTasks);
            this.LogInfo("Page exit animations completed");
        }, "Page Exit Animation");
    }

    /// <summary>
    /// Get all form fields for animation
    /// </summary>
    private List<View> GetAllFormFields()
    {
        return this.SafeExecute(() =>
        {
            var fields = new List<View>();

            // Find all ContentViews with FormFieldFrameStyle
            if (Content is ScrollView scrollView &&
                scrollView.Content is StackLayout mainStack)
            {
                foreach (var child in mainStack.Children)
                {
                    if (child is ContentView contentView)
                    {
                        fields.Add(contentView);
                    }
                }
            }

            this.LogInfo($"Found {fields.Count} form fields for animation");
            return fields;
        }, fallbackValue: new List<View>(), operationName: "Get Form Fields");
    }

    #endregion

    #region Form Interaction Handlers

    /// <summary>
    /// Handle Entry focused events for enhanced UX
    /// </summary>
    private async void OnEntryFocused(object sender, FocusEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Entry entry)
            {
                this.LogInfo($"Entry focused: {entry.Placeholder}");

                // Subtle animation on focus
                await entry.ScaleTo(1.02, 150, Easing.CubicOut);

                // Scroll to ensure visibility on smaller screens
                if (Parent is ScrollView scrollView)
                {
                    await scrollView.ScrollToAsync(entry, ScrollToPosition.MakeVisible, true);
                }
            }
        }, "Entry Focused");
    }

    /// <summary>
    /// Handle Entry unfocused events
    /// </summary>
    private async void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Entry entry)
            {
                this.LogInfo($"Entry unfocused: {entry.Placeholder}");

                // Return to normal scale
                await entry.ScaleTo(1.0, 150, Easing.CubicOut);
            }
        }, "Entry Unfocused");
    }

    /// <summary>
    /// Handle Picker selection changes
    /// </summary>
    private void OnFamilyPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (sender is Picker picker && picker.SelectedItem != null)
            {
                this.LogInfo($"Family picker selection changed: {picker.SelectedItem}");

                // Trigger visual feedback
                _ = AnimatePickerSelection(picker);
            }
        }, "Family Picker Selection Changed");
    }

    /// <summary>
    /// Animate picker selection feedback
    /// </summary>
    private async Task AnimatePickerSelection(View picker)
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Subtle pulse animation
            await picker.ScaleTo(1.05, 100, Easing.CubicOut);
            await picker.ScaleTo(1.0, 100, Easing.CubicOut);
        }, "Picker Selection Animation");
    }

    #endregion

    #region Switch Animations

    /// <summary>
    /// Handle switch toggled events with animations
    /// </summary>
    private async void OnSwitchToggled(object sender, ToggledEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Switch switchControl)
            {
                this.LogInfo($"Switch toggled: {e.Value}");

                // Find the parent grid to animate the entire setting
                if (switchControl.Parent is Grid grid)
                {
                    await AnimateSwitchToggle(grid, e.Value);
                }
            }
        }, "Switch Toggled");
    }

    /// <summary>
    /// Animate switch toggle with visual feedback
    /// </summary>
    private async Task AnimateSwitchToggle(View container, bool isOn)
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Pulse animation with color feedback
            var color = isOn ? Colors.LightGreen : Colors.LightGray;

            container.BackgroundColor = color.WithAlpha(0.3f);
            await container.ScaleTo(1.02, 100, Easing.CubicOut);
            await container.ScaleTo(1.0, 100, Easing.CubicOut);

            // Fade back to transparent
            await Task.Delay(200);
            container.BackgroundColor = Colors.Transparent;
        }, "Switch Toggle Animation");
    }

    #endregion

    #region Button Interactions

    /// <summary>
    /// Handle button pressed events for tactile feedback
    /// </summary>
    private async void OnButtonPressed(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Button button)
            {
                this.LogInfo($"Button pressed: {button.Text}");

                // Tactile feedback animation
                await button.ScaleTo(0.95, 50, Easing.CubicOut);

                // Haptic feedback
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
        }, "Button Pressed");
    }

    /// <summary>
    /// Handle button released events
    /// </summary>
    private async void OnButtonReleased(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Button button)
            {
                // Return to normal scale
                await button.ScaleTo(1.0, 50, Easing.CubicOut);
            }
        }, "Button Released");
    }

    #endregion

    #region Visual State Management

    /// <summary>
    /// Handle validation state changes
    /// </summary>
    private void OnValidationStateChanged()
    {
        this.SafeExecute(() =>
        {
            var hasErrors = !_viewModel.IsNameValid || !_viewModel.IsFamilyValid;
            this.LogInfo($"Validation state changed: {(hasErrors ? "Has Errors" : "Valid")}");

            // Update visual state
            VisualStateManager.GoToState(this, hasErrors ? "Invalid" : "Valid");
        }, "Validation State Changed");
    }

    /// <summary>
    /// Handle loading state visual changes
    /// </summary>
    private void OnLoadingStateChanged()
    {
        this.SafeExecute(() =>
        {
            var isLoading = _viewModel.IsSaving || _viewModel.IsLoadingFamilies;
            this.LogInfo($"Loading state changed: {(isLoading ? "Loading" : "Idle")}");

            // Update visual state
            VisualStateManager.GoToState(this, isLoading ? "Loading" : "Idle");
        }, "Loading State Changed");
    }

    #endregion

    #region Event Handler Setup and Cleanup

    /// <summary>
    /// Setup event handlers when page is created
    /// </summary>
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        this.SafeExecute(() =>
        {
            if (args.NewHandler == null)
            {
                // Page is being destroyed, cleanup event handlers
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                this.LogInfo("Cleaned up GenusEditPage event handlers");
            }
            else if (args.OldHandler == null)
            {
                // Page is being created, setup event handlers
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                SetupFormEventHandlers();
                this.LogInfo("Setup GenusEditPage event handlers");
            }

            base.OnHandlerChanging(args);
        }, "Handler Changing");
    }

    /// <summary>
    /// Setup form-specific event handlers
    /// </summary>
    private void SetupFormEventHandlers()
    {
        this.SafeExecute(() =>
        {
            // Find and setup Entry event handlers
            var entries = GetControlsOfType<Entry>();
            foreach (var entry in entries)
            {
                entry.Focused += OnEntryFocused;
                entry.Unfocused += OnEntryUnfocused;
            }

            // Find and setup Switch event handlers
            var switches = GetControlsOfType<Switch>();
            foreach (var switchControl in switches)
            {
                switchControl.Toggled += OnSwitchToggled;
            }

            // Find and setup Button event handlers
            var buttons = GetControlsOfType<Button>();
            foreach (var button in buttons)
            {
                button.Pressed += OnButtonPressed;
                button.Released += OnButtonReleased;
            }

            this.LogInfo($"Setup event handlers for {entries.Count} entries, {switches.Count} switches, {buttons.Count} buttons");
        }, "Setup Form Event Handlers");
    }

    /// <summary>
    /// Get all controls of a specific type
    /// </summary>
    private List<T> GetControlsOfType<T>() where T : View
    {
        return this.SafeExecute(() =>
        {
            var controls = new List<T>();
            TraverseViewHierarchy(Content, controls);
            return controls;
        }, fallbackValue: new List<T>(), operationName: "Get Controls Of Type");
    }

    /// <summary>
    /// Recursively traverse view hierarchy to find controls
    /// </summary>
    private void TraverseViewHierarchy<T>(Element element, List<T> controls) where T : View
    {
        this.SafeExecute(() =>
        {
            if (element is T control)
            {
                controls.Add(control);
            }

            if (element is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    TraverseViewHierarchy(child, controls);
                }
            }
            else if (element is ContentView contentView && contentView.Content != null)
            {
                TraverseViewHierarchy(contentView.Content, controls);
            }
            else if (element is ScrollView scrollView && scrollView.Content != null)
            {
                TraverseViewHierarchy(scrollView.Content, controls);
            }
        }, "Traverse View Hierarchy");
    }

    /// <summary>
    /// Handle view model property changes
    /// </summary>
    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsNameValid):
                case nameof(_viewModel.IsFamilyValid):
                    OnValidationStateChanged();
                    break;

                case nameof(_viewModel.IsSaving):
                case nameof(_viewModel.IsLoadingFamilies):
                    OnLoadingStateChanged();
                    break;

                case nameof(_viewModel.SelectedFamily):
                    this.LogInfo($"Selected family changed: {_viewModel.SelectedFamily?.Name ?? "None"}");
                    break;
            }
        }, "ViewModel Property Changed");
    }

    #endregion
}