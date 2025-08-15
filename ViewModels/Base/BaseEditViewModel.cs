using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using OrchidPro.Services;
using OrchidPro.Constants;
using OrchidPro.Extensions;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// Base ViewModel for entity editing providing comprehensive CRUD operations, validation, and form management.
/// Implements generic patterns for create/edit workflows across different entity types.
/// OPTIMIZED VERSION - Reduced validation calls and improved performance.
/// </summary>
public abstract partial class BaseEditViewModel<T> : BaseViewModel, IQueryAttributable
    where T : class, IBaseEntity, new()
{
    #region Protected Fields

    protected readonly IBaseRepository<T> _repository;
    protected readonly INavigationService _navigationService;

    private T? _originalEntity;
    protected bool _isEditMode;
    private Timer? _validationTimer;
    protected TimeSpan ValidationDebounceTime = TimeSpan.FromMilliseconds(ValidationConstants.NAME_VALIDATION_DEBOUNCE_DELAY);
    private List<T> _allEntities = new();
    private bool _isInitializing = true;

    // 🔧 PERFORMANCE: Enhanced suppression flags
    private bool _suppressValidation = false;
    private bool _suppressPropertyChangeHandling = false;
    private DateTime _lastValidationTime = DateTime.MinValue;
    private const int VALIDATION_THROTTLE_MS = 300;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private Guid? entityId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private bool isSystemDefault;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime updatedAt;

    [ObservableProperty]
    private bool hasUnsavedChanges;

    [ObservableProperty]
    private bool isNameValid = true;

    [ObservableProperty]
    private bool isDescriptionValid = true;

    [ObservableProperty]
    private string nameError = string.Empty;

    [ObservableProperty]
    private string descriptionError = string.Empty;

    [ObservableProperty]
    private string nameValidationMessage = string.Empty;

    [ObservableProperty]
    private bool isValidatingName;

    [ObservableProperty]
    private double formCompletionProgress;

    [ObservableProperty]
    private bool canSave;

    [ObservableProperty]
    private bool canDelete;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string saveButtonText = TextConstants.SAVE_CHANGES;

    [ObservableProperty]
    private Color saveButtonColor = Colors.Green;

    [ObservableProperty]
    private string connectionStatus = TextConstants.STATUS_CONNECTED;

    [ObservableProperty]
    private Color connectionStatusColor = ColorConstants.CONNECTED_COLOR;

    [ObservableProperty]
    private bool isConnected = true;

    [ObservableProperty]
    private string connectionTestResult = "";

    [ObservableProperty]
    private string loadingMessage = TextConstants.LOADING_DEFAULT;

    [ObservableProperty]
    private bool isNameFocused;

    [ObservableProperty]
    private bool isDescriptionFocused;

    [ObservableProperty]
    private bool isFavorite;

    #endregion

    #region Abstract Properties

    /// <summary>
    /// Entity name for display purposes (e.g., "Family", "Species")
    /// </summary>
    public abstract string EntityName { get; }

    /// <summary>
    /// Plural entity name for display purposes (e.g., "Families", "Species")
    /// </summary>
    public abstract string EntityNamePlural { get; }

    #endregion

    #region Generic Commands Framework

    /// <summary>
    /// Generic save command with comprehensive validation and error handling
    /// </summary>
    [RelayCommand]
    public virtual async Task SaveAsync()
    {
        if (!CanSave || IsSaving) return;

        using (this.LogPerformance($"Save {EntityName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                IsSaving = true;
                LoadingMessage = IsEditMode ? $"Updating {EntityName.ToLower()}..." : $"Creating {EntityName.ToLower()}...";
                IsBusy = true;

                // Validate before saving
                if (!await ValidateEntityAsync())
                {
                    await ShowValidationErrorsAsync();
                    return null;
                }

                // Create/update entity
                T entity = await PrepareEntityForSaveAsync();
                T savedEntity;

                if (IsEditMode && EntityId.HasValue)
                {
                    savedEntity = await _repository.UpdateAsync(entity);
                    this.LogDataOperation("Updated", EntityName, savedEntity.Name);
                }
                else
                {
                    savedEntity = await _repository.CreateAsync(entity);
                    this.LogDataOperation("Created", EntityName, savedEntity.Name);
                }

                // Update local data
                await UpdateLocalDataFromSavedEntity(savedEntity);

                // Mark as saved
                HasUnsavedChanges = false;
                _originalEntity = (T)savedEntity.Clone();

                // Notify success
                await OnSaveSuccessAsync(savedEntity);

                return savedEntity;
            }, EntityName);

            if (result.Success && result.Data != null)
            {
                await ShowSuccessAsync("Success", $"{EntityName} {(IsEditMode ? "updated" : "created")} successfully");
                await NavigateBackAsync();
            }
            else
            {
                await ShowErrorAsync("Save Error", result.Message);
            }

            IsSaving = false;
            IsBusy = false;
        }
    }

    /// <summary>
    /// Generic cancel command with unsaved changes confirmation
    /// </summary>
    [RelayCommand]
    public virtual async Task CancelAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Cancel command - HasUnsavedChanges: {HasUnsavedChanges}");

            if (HasUnsavedChanges)
            {
                bool shouldDiscard = await ShowConfirmationAsync(
                    "Discard Changes?",
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Discard",
                    "Keep Editing"
                );

                if (!shouldDiscard)
                {
                    this.LogInfo("User chose to keep editing");
                    return;
                }

                this.LogInfo("User chose to discard changes");
            }

            HasUnsavedChanges = false;
            await NavigateBackAsync();
        }, "Cancel Operation");
    }

    /// <summary>
    /// Generic connection test command with status updates
    /// </summary>
    [RelayCommand]
    public virtual async Task TestConnectionAsync()
    {
        using (this.LogPerformance("Connection Test"))
        {
            ConnectionTestResult = "Testing...";
            IsConnected = false;
            ConnectionStatus = TextConstants.STATUS_CONNECTING;
            ConnectionStatusColor = ColorConstants.CONNECTING_COLOR;

            // Simulate connection delay for better UX
            await Task.Delay(PerformanceConstants.MIN_LOADING_DISPLAY_TIME);

            var result = await this.SafeDataExecuteAsync(async () =>
            {
                return await _repository.GetAllAsync();
            }, "Connection Test");

            if (result.Success)
            {
                IsConnected = true;
                ConnectionStatus = TextConstants.STATUS_CONNECTED;
                ConnectionStatusColor = ColorConstants.CONNECTED_COLOR;
                ConnectionTestResult = "Connection successful";
                await ShowSuccessAsync("Connection", "Connection test successful");
            }
            else
            {
                IsConnected = false;
                ConnectionStatus = TextConstants.STATUS_DISCONNECTED;
                ConnectionStatusColor = ColorConstants.DISCONNECTED_COLOR;
                ConnectionTestResult = $"Connection failed: {result.Message}";
                await ShowErrorAsync("Connection Error", result.Message);
            }
        }
    }

    #endregion

    #region Generic Validation Framework - OPTIMIZED

    /// <summary>
    /// Setup validation event handlers and timers
    /// </summary>
    protected virtual void SetupValidation()
    {
        this.SafeExecute(() =>
        {
            PropertyChanged += OnPropertyChangedForValidation;
            this.LogInfo($"Validation framework setup for {EntityName}");
        }, "Setup Validation");
    }

    /// <summary>
    /// Handle property changes for validation and unsaved change tracking
    /// OPTIMIZED: Skip unnecessary validation calls during initialization
    /// </summary>
    protected virtual async void OnPropertyChangedForValidation(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 🔧 PERFORMANCE: Early exit conditions
        if (_isInitializing || _suppressPropertyChangeHandling || _suppressValidation)
        {
            return;
        }

        await this.SafeExecuteAsync(async () =>
        {
            // Detect changes and mark as unsaved
            if (e.PropertyName is nameof(Name) or nameof(Description) or nameof(IsActive) or nameof(IsFavorite))
            {
                HasUnsavedChanges = true;
                UpdateFormCompletionProgress();

                // 🔧 PERFORMANCE: Throttled validation for Name only
                if (e.PropertyName == nameof(Name))
                {
                    // Throttle validation calls
                    var now = DateTime.Now;
                    if (now - _lastValidationTime < TimeSpan.FromMilliseconds(VALIDATION_THROTTLE_MS))
                    {
                        return; // Skip this validation call
                    }
                    _lastValidationTime = now;

                    await ValidateEntityNameAsync();
                }
                else
                {
                    UpdateSaveButton();
                }
            }
        }, "Property Change Validation");
    }

    /// <summary>
    /// Schedule name validation with debounce to avoid excessive API calls
    /// OPTIMIZED: Enhanced throttling and suppression checks
    /// </summary>
    protected virtual void ScheduleNameValidation()
    {
        // 🔧 PERFORMANCE: Skip if suppressed
        if (_suppressValidation) return;

        this.SafeExecute(() =>
        {
            _validationTimer?.Dispose();
            _validationTimer = new Timer(async _ => await ValidateNameWithDebounce(), null, (int)ValidationDebounceTime.TotalMilliseconds, Timeout.Infinite);
        }, "Schedule Name Validation");
    }

    /// <summary>
    /// Validate name with debounce and thread-safe UI updates
    /// OPTIMIZED: Additional suppression checks
    /// </summary>
    private async Task ValidateNameWithDebounce()
    {
        // 🔧 PERFORMANCE: Skip if suppressed during delay
        if (_suppressValidation) return;

        await this.SafeExecuteAsync(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Double-check suppression after thread switch
                if (_suppressValidation) return;

                IsValidatingName = true;
                UpdateSaveButton();

                await ValidateNameAsync();

                IsValidatingName = false;
                UpdateSaveButton();
            });
        }, "Validate Name With Debounce");
    }

    /// <summary>
    /// Virtual method for entity-specific name validation - can be overridden
    /// </summary>
    protected virtual async Task ValidateEntityNameAsync()
    {
        await ValidateNameAsync();
    }

    /// <summary>
    /// Validate name uniqueness against all entities
    /// OPTIMIZED: Skip validation if suppressed
    /// </summary>
    protected virtual async Task ValidateNameAsync()
    {
        // 🔧 PERFORMANCE: Skip if suppressed
        if (_suppressValidation) return;

        await this.SafeValidateAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                IsNameValid = false;
                NameValidationMessage = string.Format(ValidationConstants.NAME_REQUIRED_TEMPLATE, EntityName);
                UpdateSaveButton();
                return false;
            }

            if (Name.Length < ValidationConstants.NAME_OPTIMAL_MIN_LENGTH)
            {
                IsNameValid = false;
                NameValidationMessage = string.Format(ValidationConstants.NAME_TOO_SHORT_TEMPLATE, EntityName, ValidationConstants.NAME_OPTIMAL_MIN_LENGTH);
                UpdateSaveButton();
                return false;
            }

            // Check for duplicate names
            var existingEntity = _allEntities.FirstOrDefault(e =>
                e.Name.Equals(Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                e.Id != EntityId);

            if (existingEntity != null)
            {
                IsNameValid = false;
                NameValidationMessage = string.Format(ValidationConstants.NAME_DUPLICATE_TEMPLATE, EntityName.ToLower());
                UpdateSaveButton();
                return false;
            }

            IsNameValid = true;
            NameValidationMessage = string.Empty;
            UpdateSaveButton();
            this.LogSuccess($"Name validation passed: {Name}");
            return true;
        }, "Name Validation");
    }

    /// <summary>
    /// Comprehensive entity validation before save operations
    /// </summary>
    protected virtual async Task<bool> ValidateEntityAsync()
    {
        return await this.SafeValidateAsync(async () =>
        {
            await ValidateNameAsync();
            return IsNameValid && !string.IsNullOrWhiteSpace(Name);
        }, "Entity Validation");
    }

    /// <summary>
    /// Display validation errors to user
    /// </summary>
    protected virtual async Task ShowValidationErrorsAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!IsNameValid)
            {
                await ShowErrorAsync("Validation Error", NameValidationMessage);
            }
        }, "Show Validation Errors");
    }

    /// <summary>
    /// Set validation error for a specific property
    /// </summary>
    protected void SetValidationError(string propertyName, string? errorMessage)
    {
        this.SafeExecute(() =>
        {
            switch (propertyName)
            {
                case nameof(Name):
                    IsNameValid = string.IsNullOrEmpty(errorMessage);
                    NameValidationMessage = errorMessage ?? string.Empty;
                    break;
                case nameof(Description):
                    IsDescriptionValid = string.IsNullOrEmpty(errorMessage);
                    DescriptionError = errorMessage ?? string.Empty;
                    break;
            }
        }, "Set Validation Error");
    }

    /// <summary>
    /// Check if there are any validation errors
    /// </summary>
    protected bool HasValidationErrors => !IsNameValid || !IsDescriptionValid;

    #endregion

    #region Form Progress Framework

    /// <summary>
    /// Update form completion progress for UI feedback
    /// OPTIMIZED: Skip during suppression
    /// </summary>
    protected virtual void UpdateFormCompletionProgress()
    {
        // 🔧 PERFORMANCE: Skip during suppression
        if (_suppressPropertyChangeHandling) return;

        this.SafeExecute(() =>
        {
            int totalFields = GetTotalFormFields();
            int completedFields = GetCompletedFormFields();

            FormCompletionProgress = totalFields > 0 ? (double)completedFields / totalFields : 0;

            this.LogDebug($"Form progress: {completedFields}/{totalFields} = {FormCompletionProgress:P0}");
        }, "Update Form Progress");
    }

    /// <summary>
    /// Get total number of form fields for progress calculation
    /// </summary>
    protected virtual int GetTotalFormFields()
    {
        return 3; // Name + Description + IsFavorite by default
    }

    /// <summary>
    /// Get number of completed form fields
    /// </summary>
    protected virtual int GetCompletedFormFields()
    {
        return this.SafeExecute(() =>
        {
            int completed = 0;

            if (!string.IsNullOrWhiteSpace(Name)) completed++;
            if (!string.IsNullOrWhiteSpace(Description)) completed++;
            if (IsFavorite) completed++;

            return completed;
        }, fallbackValue: 0, "Get Completed Fields");
    }

    /// <summary>
    /// Update save button state based on validation and form completion
    /// OPTIMIZED: Skip during suppression
    /// </summary>
    protected virtual void UpdateSaveButton()
    {
        // 🔧 PERFORMANCE: Skip during suppression
        if (_suppressPropertyChangeHandling) return;

        this.SafeExecute(() =>
        {
            CanSave = IsNameValid &&
                     IsDescriptionValid &&
                     !string.IsNullOrWhiteSpace(Name) &&
                     !IsSaving &&
                     !IsBusy &&
                     !IsValidatingName;

            SaveButtonColor = CanSave ? Colors.Green : Colors.Gray;
            SaveButtonText = IsEditMode ? "Update" : "Create";

            this.LogDebug($"UpdateSaveButton - CanSave: {CanSave} (Name: {IsNameValid}, Desc: {IsDescriptionValid}, " +
                         $"NotEmpty: {!string.IsNullOrWhiteSpace(Name)}, NotSaving: {!IsSaving}, NotBusy: {!IsBusy}, NotValidating: {!IsValidatingName})");
        }, "Update Save Button");
    }

    #endregion

    #region Navigation Framework

    /// <summary>
    /// Navigate back to previous page with safe execution
    /// </summary>
    protected virtual async Task NavigateBackAsync()
    {
        var success = await this.SafeNavigationExecuteAsync(async () =>
        {
            await _navigationService.GoBackAsync();
        }, $"Navigate Back from {EntityName}");

        if (!success)
        {
            this.LogWarning("Navigation back failed, attempting fallback");
        }
    }

    #endregion

    #region Computed Properties

    public string PageTitle => IsEditMode ? $"Edit {EntityName}" : $"Add {EntityName}";
    public string PageSubtitle => IsEditMode ? $"Modify {EntityName.ToLower()} information" : $"Create a new {EntityName.ToLower()}";
    public bool IsEditMode => _isEditMode;
    public INavigationService NavigationService => _navigationService;

    #endregion

    #region Constructor - OPTIMIZED

    protected BaseEditViewModel(IBaseRepository<T> repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;

        Title = $"{EntityName} Details";
        SaveButtonColor = Colors.Green;

        // 🔧 PERFORMANCE: Initialize suppression flags
        _suppressValidation = true;
        _suppressPropertyChangeHandling = true;

        // Initialize connection status
        IsConnected = true;
        ConnectionStatus = TextConstants.STATUS_CONNECTED;
        ConnectionStatusColor = ColorConstants.CONNECTED_COLOR;

        // Load entities for validation
        _ = LoadAllEntitiesForValidationAsync();

        SetupValidation();

        this.LogInfo($"Enhanced initialized for {EntityName}");
    }

    /// <summary>
    /// Load all entities for name uniqueness validation
    /// OPTIMIZED: Clear suppression flags after loading
    /// </summary>
    protected virtual async Task LoadAllEntitiesForValidationAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            return await _repository.GetAllAsync();
        }, "Validation Entities");

        if (result.Success && result.Data != null)
        {
            _allEntities = result.Data.ToList();
            this.LogInfo($"Loaded {_allEntities.Count} {EntityNamePlural.ToLower()} for validation");
        }
        else
        {
            _allEntities = new List<T>();
            this.LogWarning("Failed to load entities for validation, using empty list");
        }

        // 🔧 PERFORMANCE: Clear suppression flags after initial loading
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _suppressValidation = false;
            _suppressPropertyChangeHandling = false;
        });
    }

    #endregion

    #region Navigation and Query Attributes - OPTIMIZED

    /// <summary>
    /// Apply navigation parameters for edit mode initialization
    /// OPTIMIZED: Set suppression flags during initialization
    /// </summary>
    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ApplyQueryAttributes for {EntityName} with {query.Count} parameters");

            // 🔧 PERFORMANCE: Set suppression flags
            _suppressValidation = true;
            _suppressPropertyChangeHandling = true;

            // Check multiple key variations
            Guid? entityId = null;
            var possibleKeys = new[] { $"{EntityName}Id", "Id", "EntityId", "id" };

            foreach (var key in possibleKeys)
            {
                if (query.TryGetValue(key, out var value))
                {
                    entityId = ConvertToGuid(value);
                    if (entityId.HasValue)
                    {
                        this.LogInfo($"Found {key} parameter: {entityId}");
                        break;
                    }
                }
            }

            if (entityId.HasValue && entityId.Value != Guid.Empty)
            {
                EntityId = entityId;
                _isEditMode = true;
                Title = $"Edit {EntityName}";
                SaveButtonText = "Update";
                this.LogInfo($"EDIT MODE for {EntityName} ID: {entityId}");

                // Load entity data
                _ = LoadEntityAsync();
            }
            else
            {
                EntityId = null;
                _isEditMode = false;
                Title = $"New {EntityName}";
                SaveButtonText = "Create";
                this.LogInfo($"CREATE MODE for {EntityName}");

                // 🔧 PERFORMANCE: Clear suppression flags for creation mode
                _isInitializing = false;
                _suppressValidation = false;
                _suppressPropertyChangeHandling = false;
                HasUnsavedChanges = false;
            }
        }, "Apply Query Attributes");
    }

    /// <summary>
    /// Safely convert object to Guid
    /// </summary>
    private Guid? ConvertToGuid(object obj)
    {
        return this.SafeExecute(() =>
        {
            if (obj == null) return (Guid?)null;

            if (obj is Guid guid)
                return guid;

            if (obj is string str && Guid.TryParse(str, out var parsedGuid))
                return parsedGuid;

            this.LogWarning($"Cannot convert {obj} ({obj.GetType().Name}) to Guid");
            return (Guid?)null;
        }, fallbackValue: (Guid?)null, "Convert To Guid");
    }

    #endregion

    #region Entity Loading and Preparation - OPTIMIZED

    /// <summary>
    /// Load entity data for edit mode
    /// OPTIMIZED: Manage suppression flags properly
    /// </summary>
    protected virtual async Task LoadEntityAsync()
    {
        if (!EntityId.HasValue) return;

        using (this.LogPerformance($"Load {EntityName}"))
        {
            LoadingMessage = $"Loading {EntityName.ToLower()}...";
            IsBusy = true;

            var result = await this.SafeDataExecuteAsync(async () =>
            {
                return await _repository.GetByIdAsync(EntityId.Value);
            }, EntityName);

            if (result.Success && result.Data != null)
            {
                _originalEntity = result.Data;
                await PopulateFromEntityAsync(result.Data);
                this.LogSuccess($"Loaded {EntityName}: {Name}");
            }
            else
            {
                this.LogError($"{EntityName} not found: {EntityId}");
                await ShowErrorAsync("Not Found", $"{EntityName} not found");
            }

            IsBusy = false;
        }
    }

    /// <summary>
    /// Populate form fields from loaded entity
    /// OPTIMIZED: Use suppression flags during bulk property setting
    /// </summary>
    protected virtual async Task PopulateFromEntityAsync(T entity)
    {
        await this.SafeExecuteAsync(async () =>
        {
            // 🔧 PERFORMANCE: Suppress notifications during bulk loading
            _isInitializing = true;
            _suppressValidation = true;
            _suppressPropertyChangeHandling = true;

            Name = entity.Name;
            Description = entity.Description ?? string.Empty;
            IsActive = entity.IsActive;
            IsFavorite = entity.IsFavorite;
            IsSystemDefault = entity.IsSystemDefault;
            CreatedAt = entity.CreatedAt;
            UpdatedAt = entity.UpdatedAt;

            // 🔧 PERFORMANCE: Clear suppression flags after bulk loading
            _isInitializing = false;
            _suppressValidation = false;
            _suppressPropertyChangeHandling = false;
            HasUnsavedChanges = false;

            // Update UI
            UpdateFormCompletionProgress();
            UpdateSaveButton();

            this.LogSuccess($"Populated from {EntityName}: {Name}");
        }, "Populate From Entity");
    }

    /// <summary>
    /// Prepare entity object for save operation
    /// </summary>
    protected virtual async Task<T> PrepareEntityForSaveAsync()
    {
        return this.SafeExecute(() =>
        {
            T entity = new T
            {
                Id = EntityId ?? Guid.NewGuid(),
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                UserId = null, // Will be set by repository if needed
                CreatedAt = IsEditMode ? CreatedAt : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            this.LogInfo($"Prepared {EntityName} for save: {entity.Name}");
            return entity;
        }, fallbackValue: new T(), "Prepare Entity For Save");
    }

    /// <summary>
    /// Update local data from saved entity
    /// </summary>
    protected virtual async Task UpdateLocalDataFromSavedEntity(T savedEntity)
    {
        await this.SafeExecuteAsync(async () =>
        {
            EntityId = savedEntity.Id;
            UpdatedAt = savedEntity.UpdatedAt;

            this.LogInfo($"Updated local data from saved {EntityName}");
        }, "Update Local Data");
    }

    /// <summary>
    /// Callback after successful save operation
    /// </summary>
    protected virtual async Task OnSaveSuccessAsync(T savedEntity)
    {
        this.LogSuccess($"{EntityName} saved successfully");
        await Task.CompletedTask;
    }

    #endregion

    #region User Interface Methods

    /// <summary>
    /// Show success message using standardized toast extensions
    /// </summary>
    protected virtual async Task ShowSuccessAsync(string title, string message)
    {
        await this.ShowSuccessToast(message);
    }

    /// <summary>
    /// Show error message using standardized toast extensions
    /// </summary>
    protected virtual async Task ShowErrorAsync(string title, string message)
    {
        await this.ShowErrorToast(message);
    }

    /// <summary>
    /// Show confirmation dialog using standardized extensions
    /// </summary>
    protected virtual async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
    {
        return await this.ShowConfirmation(title, message, accept, cancel);
    }

    #endregion

    #region Lifecycle Management - OPTIMIZED

    /// <summary>
    /// Virtual method for ViewModel initialization - can be overridden by derived classes
    /// </summary>
    protected virtual async Task InitializeAsync()
    {
        this.LogDebug($"Base ViewModel initialization (override in derived classes)");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Virtual method for OnAppearing lifecycle event
    /// </summary>
    public virtual async Task OnAppearingAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"ViewModel Appearing - initializing if needed");

            if (_isInitializing)
            {
                this.LogInfo($"Initializing ViewModel for first appearance");
                await InitializeAsync();
                _isInitializing = false;
                this.LogSuccess($"ViewModel initialization completed successfully");
            }
        }, "ViewModel Appearing");
    }

    /// <summary>
    /// Clean up resources and dispose timers
    /// </summary>
    public virtual void Dispose()
    {
        this.SafeDispose(_validationTimer, "Validation Timer");
        this.LogInfo($"Disposed resources for {EntityName}");
    }

    #endregion

    #region Helper Methods - PERFORMANCE

    /// <summary>
    /// Suppress all property change handling temporarily
    /// </summary>
    protected void SuppressPropertyChangeHandling(bool suppress)
    {
        _suppressPropertyChangeHandling = suppress;
        this.LogDebug($"Property change handling suppression: {suppress}");
    }

    /// <summary>
    /// Suppress validation temporarily
    /// </summary>
    protected void SuppressValidation(bool suppress)
    {
        _suppressValidation = suppress;
        this.LogDebug($"Validation suppression: {suppress}");
    }

    /// <summary>
    /// Execute action with suppressed property changes
    /// </summary>
    protected void ExecuteWithSuppressedChanges(Action action)
    {
        var wasSupressing = _suppressPropertyChangeHandling;
        _suppressPropertyChangeHandling = true;

        try
        {
            action();
        }
        finally
        {
            _suppressPropertyChangeHandling = wasSupressing;
        }
    }

    /// <summary>
    /// Execute async action with suppressed property changes
    /// </summary>
    protected async Task ExecuteWithSuppressedChangesAsync(Func<Task> action)
    {
        var wasSupressing = _suppressPropertyChangeHandling;
        _suppressPropertyChangeHandling = true;

        try
        {
            await action();
        }
        finally
        {
            _suppressPropertyChangeHandling = wasSupressing;
        }
    }

    #endregion
}