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
    private readonly int _validationDelay = ValidationConstants.NAME_VALIDATION_DEBOUNCE_DELAY;
    private List<T> _allEntities = new();
    private bool _isInitializing = true;

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

    #region Generic Validation Framework

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
    /// </summary>
    private void OnPropertyChangedForValidation(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (_isInitializing) return;

            // Detect changes and mark as unsaved
            if (e.PropertyName is nameof(Name) or nameof(Description) or nameof(IsActive) or nameof(IsFavorite))
            {
                HasUnsavedChanges = true;
                UpdateFormCompletionProgress();

                // Validation with debounce for Name
                if (e.PropertyName == nameof(Name))
                {
                    ScheduleNameValidation();
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
    /// </summary>
    protected virtual void ScheduleNameValidation()
    {
        this.SafeExecute(() =>
        {
            _validationTimer?.Dispose();
            _validationTimer = new Timer(async _ => await ValidateNameWithDebounce(), null, _validationDelay, Timeout.Infinite);
        }, "Schedule Name Validation");
    }

    /// <summary>
    /// Validate name with debounce and thread-safe UI updates
    /// </summary>
    private async Task ValidateNameWithDebounce()
    {
        await this.SafeExecuteAsync(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                IsValidatingName = true;
                UpdateSaveButton();

                await ValidateNameAsync();

                IsValidatingName = false;
                UpdateSaveButton();
            });
        }, "Validate Name With Debounce");
    }

    /// <summary>
    /// Validate name uniqueness against all entities
    /// </summary>
    protected virtual async Task ValidateNameAsync()
    {
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

    #endregion

    #region Form Progress Framework

    /// <summary>
    /// Update form completion progress for UI feedback
    /// </summary>
    protected virtual void UpdateFormCompletionProgress()
    {
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
    /// </summary>
    protected virtual void UpdateSaveButton()
    {
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

    #region Constructor

    protected BaseEditViewModel(IBaseRepository<T> repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;

        Title = $"{EntityName} Details";
        SaveButtonColor = Colors.Green;

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
    }

    #endregion

    #region Navigation and Query Attributes

    /// <summary>
    /// Apply navigation parameters for edit mode initialization
    /// </summary>
    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ApplyQueryAttributes for {EntityName} with {query.Count} parameters");

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

                // Finish initialization for creation mode
                _isInitializing = false;
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

    #region Entity Loading and Preparation

    /// <summary>
    /// Load entity data for edit mode
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
    /// </summary>
    protected virtual async Task PopulateFromEntityAsync(T entity)
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Disable change processing during loading
            _isInitializing = true;

            Name = entity.Name;
            Description = entity.Description ?? string.Empty;
            IsActive = entity.IsActive;
            IsFavorite = entity.IsFavorite;
            IsSystemDefault = entity.IsSystemDefault;
            CreatedAt = entity.CreatedAt;
            UpdatedAt = entity.UpdatedAt;

            // Finish initialization
            _isInitializing = false;
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

    #region Lifecycle Management

    /// <summary>
    /// Clean up resources and dispose timers
    /// </summary>
    public void Dispose()
    {
        this.SafeDispose(_validationTimer, "Validation Timer");
        this.LogInfo($"Disposed resources for {EntityName}");
    }

    #endregion
}