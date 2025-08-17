using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using OrchidPro.Services;
using OrchidPro.Constants;
using OrchidPro.Extensions;
using System.Collections.ObjectModel;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// Enhanced Base ViewModel for entity editing providing comprehensive CRUD operations, validation, form management,
/// and generic parent-child relationship patterns. Maximum reusability with minimal complexity.
/// FINAL VERSION - Includes essential relationship management, collection loading, and messaging patterns.
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
    private List<T> _allEntities = [];
    private bool _isInitializing = true;
    private bool _isLoadingData = false;

    // Performance optimization flags
    private bool _suppressValidation = false;
    private bool _suppressPropertyChangeHandling = false;
    private DateTime _lastValidationTime = DateTime.MinValue;
    private const int VALIDATION_THROTTLE_MS = 300;

    // Backing fields for virtual properties
    private bool _canSave;
    private bool _canDelete;

    // Enhanced constructor support
    private readonly string _entityName = string.Empty;
    private readonly string _entityNamePlural = string.Empty;
    private readonly string _parentEntityName = string.Empty;

    #endregion

    #region Observable Properties - Base Entity Fields

    [ObservableProperty] private Guid? entityId;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isSystemDefault;
    [ObservableProperty] private DateTime createdAt;
    [ObservableProperty] private DateTime updatedAt;
    [ObservableProperty] private bool hasUnsavedChanges;
    [ObservableProperty] private bool isNameValid = true;
    [ObservableProperty] private bool isDescriptionValid = true;
    [ObservableProperty] private string nameError = string.Empty;
    [ObservableProperty] private string descriptionError = string.Empty;
    [ObservableProperty] private string nameValidationMessage = string.Empty;
    [ObservableProperty] private bool isValidatingName;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private string saveButtonText = TextConstants.SAVE_CHANGES;
    [ObservableProperty] private Color saveButtonColor = Colors.Green;
    [ObservableProperty] private string connectionStatus = TextConstants.STATUS_CONNECTED;
    [ObservableProperty] private Color connectionStatusColor = ColorConstants.CONNECTED_COLOR;
    [ObservableProperty] private bool isConnected = true;
    [ObservableProperty] private string connectionTestResult = "";
    [ObservableProperty] private string loadingMessage = TextConstants.LOADING_DEFAULT;
    [ObservableProperty] private bool isNameFocused;
    [ObservableProperty] private bool isDescriptionFocused;
    [ObservableProperty] private bool isFavorite;

    #endregion

    #region Abstract Properties

    /// <summary>
    /// Entity name for display purposes (e.g., "Family", "Species")
    /// Can be overridden or provided via enhanced constructor
    /// </summary>
    public virtual string EntityName => !string.IsNullOrEmpty(_entityName) ? _entityName : GetEntityName();

    /// <summary>
    /// Plural entity name for display purposes (e.g., "Families", "Species")
    /// Can be overridden or provided via enhanced constructor
    /// </summary>
    public virtual string EntityNamePlural => !string.IsNullOrEmpty(_entityNamePlural) ? _entityNamePlural : GetEntityNamePlural();

    /// <summary>
    /// Abstract method fallback for entity name when constructor not used
    /// </summary>
    protected abstract string GetEntityName();

    /// <summary>
    /// Abstract method fallback for plural entity name when constructor not used
    /// </summary>
    protected abstract string GetEntityNamePlural();

    #endregion

    #region 🔗 GENERIC RELATIONSHIP MANAGEMENT - NEW!

    /// <summary>
    /// Parent entity name for navigation and relationship management (e.g., "Genus", "Species", "Family")
    /// Override in derived classes or provide via enhanced constructor
    /// </summary>
    public virtual string ParentEntityName => _parentEntityName;

    /// <summary>
    /// ID of the currently selected parent entity for relationship binding
    /// Override in derived classes to return the selected parent ID (e.g., SelectedGenus?.Id)
    /// </summary>
    public virtual Guid? ParentEntityId => null;

    /// <summary>
    /// Display name of the currently selected parent entity
    /// Override in derived classes to return the selected parent name (e.g., SelectedGenus?.Name)
    /// </summary>
    public virtual string ParentDisplayName => string.Empty;

    /// <summary>
    /// Parent context string for display in UI labels and headers
    /// Automatically formatted as "in [ParentName]" when parent is selected
    /// </summary>
    public virtual string ParentContext =>
        !string.IsNullOrEmpty(ParentDisplayName) ? $"in {ParentDisplayName}" : string.Empty;

    /// <summary>
    /// Determines if another entity can be created in the current parent context
    /// Useful for Save and Continue workflows within the same parent
    /// </summary>
    public virtual bool CanCreateAnother => CanSave && ParentEntityId.HasValue;

    /// <summary>
    /// Generic parent selection change handler - reusable pattern for all parent-child relationships
    /// Call this from derived classes' OnSelectedParentChanged methods
    /// </summary>
    protected virtual void OnParentSelectionChanged()
    {
        this.SafeExecute(() =>
        {
            // Batch property notifications for parent-related properties
            OnPropertyChanged(nameof(ParentDisplayName));
            OnPropertyChanged(nameof(ParentEntityId));
            OnPropertyChanged(nameof(ParentContext));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanCreateAnother));

            UpdateSaveButton();

            // Re-validate name when parent context changes (for uniqueness within parent)
            if (!string.IsNullOrWhiteSpace(Name))
            {
                ScheduleNameValidation();
            }
        }, "Handle Parent Selection Change");
    }

    /// <summary>
    /// Generic parent validation - override for specific parent validation rules
    /// Example: return SelectedGenus != null; // for Species
    /// </summary>
    protected virtual bool ValidateParentRelationship()
    {
        // Base implementation - no parent required
        return true;
    }

    /// <summary>
    /// Generic navigation to create parent entity
    /// Automatically constructs route from ParentEntityName (e.g., "genus" -> "genusedit")
    /// </summary>
    [RelayCommand]
    public virtual async Task NavigateToCreateParentAsync()
    {
        if (string.IsNullOrEmpty(ParentEntityName))
        {
            this.LogWarning("ParentEntityName not set - cannot navigate to create parent");
            return;
        }

        var route = $"{ParentEntityName.ToLower()}edit";

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Navigating to create {ParentEntityName}");

            try
            {
                await _navigationService.NavigateToAsync(route);
                this.LogSuccess($"Navigation to {route} succeeded");
            }
            catch (Exception ex)
            {
                this.LogWarning($"NavigationService failed: {ex.Message}");

                try
                {
                    await Shell.Current.GoToAsync(route);
                    this.LogSuccess($"Shell navigation to {route} succeeded");
                }
                catch (Exception ex2)
                {
                    this.LogError($"All navigation attempts failed: {ex2.Message}");
                    await this.ShowErrorToast($"Navigation failed: {ex2.Message}");
                }
            }
        }, $"Navigate to Create {ParentEntityName}");
    }

    #endregion

    #region 📋 GENERIC COLLECTION MANAGEMENT - NEW!

    /// <summary>
    /// Generic pattern for loading parent collections (Genera, Species, Families, etc.)
    /// Eliminates repetitive LoadAvailableXXX methods across all ViewModels
    /// Usage: await LoadParentCollectionAsync(_genusRepository, AvailableGenera, "Available Genera");
    /// </summary>
    protected virtual async Task LoadParentCollectionAsync<TParent>(
        IBaseRepository<TParent> parentRepository,
        ObservableCollection<TParent> collection,
        string collectionName) where TParent : class, IBaseEntity
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Loading {collectionName}");

            var items = await parentRepository.GetAllAsync();
            var sortedItems = items.OrderBy(i => i.Name).ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                collection.Clear();
                foreach (var item in sortedItems)
                {
                    collection.Add(item);
                }
            });

            this.LogSuccess($"Loaded {items.Count} {collectionName.ToLower()}");
        }, $"Load {collectionName}");
    }

    /// <summary>
    /// Generic method to find and select parent entity by ID after creation
    /// Usage: SelectParentById(AvailableGenera, genusId, genus => SelectedGenus = genus, "Genus");
    /// </summary>
    protected virtual void SelectParentById<TParent>(
        ObservableCollection<TParent> collection,
        Guid parentId,
        Action<TParent> setSelectedParent,
        string entityName) where TParent : class, IBaseEntity
    {
        this.SafeExecute(() =>
        {
            var entity = collection.FirstOrDefault(e => e.Id == parentId);
            if (entity != null)
            {
                setSelectedParent(entity);
                this.LogSuccess($"Auto-selected {entityName}: {entity.Name}");
            }
            else
            {
                this.LogWarning($"{entityName} with ID {parentId} not found in collection");
            }
        }, $"Select {entityName} By ID");
    }

    #endregion

    #region 📨 GENERIC MESSAGING PATTERNS - NEW!

    /// <summary>
    /// Generic parent entity creation message handler
    /// Eliminates repetitive OnXXXCreated methods across all ViewModels
    /// Usage: await HandleParentCreatedAsync(genusId, genusName, _genusRepository, AvailableGenera, g => SelectedGenus = g, "Genus");
    /// </summary>
    protected virtual async Task HandleParentCreatedAsync<TParent>(
        Guid createdEntityId,
        string createdEntityName,
        IBaseRepository<TParent> parentRepository,
        ObservableCollection<TParent> collection,
        Action<TParent> setSelectedParent,
        string collectionName) where TParent : class, IBaseEntity
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Received {collectionName} created message: {createdEntityName}");

            // Reload collection and auto-select new entry
            await LoadParentCollectionAsync(parentRepository, collection, collectionName);

            // Auto-select the newly created entity
            SelectParentById(collection, createdEntityId, setSelectedParent, collectionName);
        }, $"Handle {collectionName} Created");
    }

    /// <summary>
    /// Generic messenger subscription helper for parent creation events
    /// </summary>
    protected virtual void SubscribeToParentCreatedMessages<TMessage>(
        Func<TMessage, Guid> getEntityId,
        Func<TMessage, string> getEntityName,
        Func<Guid, string, Task> handleCreated) where TMessage : class
    {
        WeakReferenceMessenger.Default.Register<TMessage>(this, async (recipient, message) =>
        {
            var entityId = getEntityId(message);
            var entityName = getEntityName(message);
            await handleCreated(entityId, entityName);
        });
    }

    #endregion

    #region Virtual Properties - Can be overridden by derived classes

    /// <summary>
    /// Virtual property for form completion progress - can be overridden
    /// </summary>
    public virtual double FormCompletionProgress
    {
        get
        {
            var totalFields = GetTotalFormFields();
            var completedFields = GetCompletedFormFields();
            return totalFields > 0 ? (double)completedFields / totalFields : 0;
        }
    }

    /// <summary>
    /// Virtual property for save capability - includes parent validation
    /// </summary>
    public virtual bool CanSave
    {
        get => _canSave;
        protected set
        {
            if (_canSave != value)
            {
                _canSave = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanCreateAnother)); // Update dependent properties
            }
        }
    }

    /// <summary>
    /// Virtual property for delete capability - can be overridden
    /// </summary>
    public virtual bool CanDelete
    {
        get => _canDelete;
        protected set
        {
            if (_canDelete != value)
            {
                _canDelete = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Enhanced page title with parent context when available
    /// </summary>
    public virtual string PageTitle => IsEditMode ? $"Edit {EntityName}" :
        !string.IsNullOrEmpty(ParentDisplayName) ? $"New {EntityName} {ParentContext}" : $"New {EntityName}";

    public string PageSubtitle => IsEditMode ? $"Modify {EntityName.ToLower()} information" : $"Create a new {EntityName.ToLower()}";
    public bool IsEditMode => _isEditMode;
    public INavigationService NavigationService => _navigationService;

    #endregion

    #region Command Framework - Enhanced and Reusable

    /// <summary>
    /// Primary save command - can be overridden for entity-specific logic
    /// </summary>
    [RelayCommand]
    public virtual async Task SaveAsync()
    {
        this.LogInfo($"SaveAsync called - CanSave: {CanSave}, IsSaving: {IsSaving}");

        if (!CanSave || IsSaving)
        {
            this.LogWarning($"Save blocked - CanSave: {CanSave}, IsSaving: {IsSaving}");
            return;
        }

        using (this.LogPerformance($"Save {EntityName}"))
        {
            try
            {
                IsSaving = true;
                LoadingMessage = IsEditMode ? $"Updating {EntityName.ToLower()}..." : $"Creating {EntityName.ToLower()}...";
                IsBusy = true;

                this.LogInfo($"Validating entity before save...");
                if (!await ValidateEntityAsync())
                {
                    this.LogWarning("Validation failed, showing errors");
                    await ShowValidationErrorsAsync();
                    return;
                }

                this.LogInfo("Preparing entity for save...");
                T entity = await PrepareEntityForSaveAsync();
                T? savedEntity = null;

                if (IsEditMode && EntityId.HasValue)
                {
                    this.LogInfo($"Updating existing {EntityName} with ID: {EntityId}");
                    savedEntity = await _repository.UpdateAsync(entity);
                    this.LogDataOperation("Updated", EntityName, savedEntity?.Name ?? "Unknown");
                }
                else
                {
                    this.LogInfo($"Creating new {EntityName}");
                    savedEntity = await _repository.CreateAsync(entity);
                    this.LogDataOperation("Created", EntityName, savedEntity?.Name ?? "Unknown");
                }

                if (savedEntity != null)
                {
                    this.LogInfo("Entity saved successfully, updating local data");
                    await UpdateLocalDataFromSavedEntity(savedEntity);
                    HasUnsavedChanges = false;
                    _originalEntity = (T)savedEntity.Clone();
                    await OnSaveSuccessAsync(savedEntity);

                    this.LogSuccess($"{EntityName} saved successfully");
                    await ShowSuccessAsync("Success", $"{EntityName} {(IsEditMode ? "updated" : "created")} successfully");
                    await NavigateBackAsync();
                }
                else
                {
                    this.LogError($"Failed to save {EntityName} - repository returned null");
                    await ShowErrorAsync("Save Error", $"Failed to save {EntityName}. Please try again.");
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Exception during save: {ex.Message}");
                await ShowErrorAsync("Save Error", $"An error occurred while saving: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
                IsBusy = false;
            }
        }
    }

    /// <summary>
    /// Save and create another command - enhanced with parent context preservation
    /// </summary>
    [RelayCommand]
    public virtual async Task SaveAndContinueAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!ValidateForSave())
            {
                await this.ShowErrorToast("Please correct the validation errors before saving.");
                return;
            }

            this.LogInfo($"Saving and creating another {EntityName}");

            var entity = await PrepareEntityForSaveAsync();
            var result = await _repository.CreateAsync(entity);

            if (result != null)
            {
                await this.ShowSuccessToast($"{EntityName} created! Ready for next one.");
                await OnEntitySavedAsync(result);
                await ClearFormForNextEntryAsync();
            }
            else
            {
                await this.ShowErrorToast($"Failed to create {EntityName.ToLower()}.");
            }
        }, $"Save {EntityName} and Continue");
    }

    /// <summary>
    /// Delete command with confirmation - reusable across entities
    /// </summary>
    [RelayCommand]
    public virtual async Task DeleteAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!EntityId.HasValue) return;

            var confirmed = await this.ShowConfirmation(
                $"Delete {EntityName}",
                $"Are you sure you want to delete '{Name}'? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            this.LogInfo($"Deleting {EntityName}: {Name}");

            var success = await _repository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast($"{EntityName} deleted successfully!");
                await OnEntityDeletedAsync();
                await NavigateBackAsync();
            }
            else
            {
                await this.ShowErrorToast($"Failed to delete {EntityName.ToLower()}.");
            }
        }, $"Delete {EntityName}");
    }

    /// <summary>
    /// Cancel command with unsaved changes confirmation
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
    /// Connection test command with status updates
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

    #region Enhanced Property Change Handling

    /// <summary>
    /// Enhanced property change monitoring with comprehensive tracking
    /// </summary>
    protected virtual async void OnPropertyChangedForValidation(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isInitializing || _suppressPropertyChangeHandling || _suppressValidation || _isLoadingData)
        {
            return;
        }

        await this.SafeExecuteAsync(() =>
        {
            if (IsTrackedProperty(e.PropertyName))
            {
                HasUnsavedChanges = true;
                UpdateFormCompletionProgress();

                if (e.PropertyName == nameof(Name))
                {
                    var now = DateTime.Now;
                    if (now - _lastValidationTime < TimeSpan.FromMilliseconds(VALIDATION_THROTTLE_MS))
                    {
                        return Task.CompletedTask;
                    }
                    _lastValidationTime = now;

                    if (!string.IsNullOrWhiteSpace(Name))
                    {
                        ScheduleNameValidation();
                        this.LogDebug($"Name changed to '{Name}' - scheduled validation");
                    }
                    else
                    {
                        NameValidationMessage = string.Empty;
                        IsNameValid = true;
                        OnPropertyChanged(nameof(CanSave));
                        UpdateSaveButton();
                    }
                }
                else
                {
                    UpdateSaveButton();
                }
            }

            return Task.CompletedTask;
        }, "Property Change Validation");
    }

    /// <summary>
    /// Virtual method to determine which properties should be tracked for unsaved changes
    /// Can be overridden by derived classes to add entity-specific properties
    /// </summary>
    protected virtual bool IsTrackedProperty(string? propertyName)
    {
        return propertyName is nameof(Name) or nameof(Description) or nameof(IsActive) or nameof(IsFavorite);
    }

    #endregion

    #region Enhanced Validation Framework

    /// <summary>
    /// Setup validation event handlers and timers
    /// </summary>
    protected virtual void SetupValidation()
    {
        this.SafeExecute(() =>
        {
            PropertyChanged += OnPropertyChangedForValidation;
            this.LogInfo($"Enhanced validation framework setup for {EntityName}");
        }, "Setup Validation");
    }

    /// <summary>
    /// Schedule name validation with debounce to avoid excessive API calls
    /// </summary>
    protected virtual void ScheduleNameValidation()
    {
        if (_suppressValidation) return;

        this.SafeExecute(() =>
        {
            _validationTimer?.Dispose();
            _validationTimer = new Timer(async _ => await ValidateNameWithDebounce(), null, (int)ValidationDebounceTime.TotalMilliseconds, Timeout.Infinite);
        }, "Schedule Name Validation");
    }

    /// <summary>
    /// Validate name with debounce and thread-safe UI updates
    /// </summary>
    private async Task ValidateNameWithDebounce()
    {
        if (_suppressValidation) return;

        await this.SafeExecuteAsync(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (_suppressValidation) return;

                IsValidatingName = true;
                UpdateSaveButton();

                await ValidateEntityNameAsync();

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
    /// Enhanced validate name uniqueness against all entities
    /// </summary>
    protected virtual async Task ValidateNameAsync()
    {
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

            var isDuplicate = await CheckForDuplicateNameAsync();
            if (isDuplicate)
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
    /// Virtual method for entity-specific duplicate name checking
    /// Can be overridden for hierarchical entities (like Species in Genus)
    /// </summary>
    protected virtual async Task<bool> CheckForDuplicateNameAsync()
    {
        return await Task.FromResult(_allEntities.Any(e =>
            e.Name.Equals(Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
            e.Id != EntityId));
    }

    /// <summary>
    /// Virtual method for comprehensive form validation before save
    /// Can be overridden for entity-specific validation rules
    /// </summary>
    protected virtual bool ValidateForSave()
    {
        var isValid = true;

        this.SafeExecute(() =>
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                isValid = false;
                this.LogWarning("Validation failed: Name is empty");
            }

            if (!IsNameValid)
            {
                isValid = false;
                this.LogWarning($"Validation failed: Name validation error - {NameValidationMessage}");
            }

            isValid = isValid && ValidateEntitySpecificRules();

            this.LogDebug($"Form validation result: {isValid} (Name: {!string.IsNullOrWhiteSpace(Name)}, NameValid: {IsNameValid}, EditMode: {_isEditMode}, EntityId: {EntityId})");
        }, "Validate Form for Save");

        return isValid;
    }

    /// <summary>
    /// Virtual method for entity-specific validation rules
    /// Override in derived classes for additional validation including parent relationships
    /// </summary>
    protected virtual bool ValidateEntitySpecificRules()
    {
        return ValidateParentRelationship(); // Include parent validation by default
    }

    /// <summary>
    /// Comprehensive entity validation before save operations
    /// </summary>
    protected virtual async Task<bool> ValidateEntityAsync()
    {
        this.LogInfo("Starting entity validation...");

        try
        {
            await ValidateNameAsync();

            var nameValid = IsNameValid;
            var nameNotEmpty = !string.IsNullOrWhiteSpace(Name);
            var entitySpecificValid = ValidateEntitySpecificRules();

            var result = nameValid && nameNotEmpty && entitySpecificValid;

            this.LogInfo($"Validation result: {result} (NameValid: {nameValid}, NameNotEmpty: {nameNotEmpty}, EntitySpecific: {entitySpecificValid})");

            return result;
        }
        catch (Exception ex)
        {
            this.LogError($"Error during validation: {ex.Message}");
            return false;
        }
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

            await ShowEntitySpecificValidationErrorsAsync();
        }, "Show Validation Errors");
    }

    /// <summary>
    /// Virtual method for entity-specific validation error display
    /// </summary>
    protected virtual Task ShowEntitySpecificValidationErrorsAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Enhanced Form Management

    /// <summary>
    /// Enhanced form completion progress calculation
    /// </summary>
    protected virtual void UpdateFormCompletionProgress()
    {
        if (_suppressPropertyChangeHandling) return;

        this.SafeExecute(() =>
        {
            // Form progress calculation is now handled by the virtual property
            OnPropertyChanged(nameof(FormCompletionProgress));
        }, "Update Form Progress");
    }

    /// <summary>
    /// Virtual method to get total number of form fields - can be overridden
    /// </summary>
    protected virtual int GetTotalFormFields()
    {
        return 3; // Name + Description + IsFavorite by default
    }

    /// <summary>
    /// Virtual method to get number of completed form fields - can be overridden
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
    /// Enhanced save button state management - includes parent validation
    /// </summary>
    protected virtual void UpdateSaveButton()
    {
        if (_suppressPropertyChangeHandling) return;

        this.SafeExecute(() =>
        {
            var canSave = CalculateCanSave();
            var canDelete = CalculateCanDelete();

            CanSave = canSave;
            CanDelete = canDelete;

            SaveButtonColor = CanSave ? Colors.Green : Colors.Gray;
            SaveButtonText = IsEditMode ? "Update" : "Create";

            this.LogInfo($"UpdateSaveButton - CanSave: {CanSave}, CanDelete: {CanDelete}, IsEditMode: {IsEditMode}, SaveButtonText: {SaveButtonText}");
            this.LogInfo($"CanSave breakdown - IsNameValid: {IsNameValid}, IsDescriptionValid: {IsDescriptionValid}, NameNotEmpty: {!string.IsNullOrWhiteSpace(Name)}, NotSaving: {!IsSaving}, NotBusy: {!IsBusy}, NotValidating: {!IsValidatingName}, ParentValid: {ValidateParentRelationship()}");
        }, "Update Save Button");
    }

    /// <summary>
    /// Virtual method to calculate CanSave state - includes parent validation
    /// </summary>
    protected virtual bool CalculateCanSave()
    {
        return IsNameValid &&
               IsDescriptionValid &&
               !string.IsNullOrWhiteSpace(Name) &&
               !IsSaving &&
               !IsBusy &&
               !IsValidatingName &&
               ValidateParentRelationship();
    }

    /// <summary>
    /// Virtual method to calculate CanDelete state - can be overridden
    /// </summary>
    protected virtual bool CalculateCanDelete()
    {
        return IsEditMode && EntityId.HasValue;
    }

    /// <summary>
    /// Virtual method for clearing form for next entry - can be overridden
    /// </summary>
    protected virtual async Task ClearFormForNextEntryAsync()
    {
        await this.SafeExecuteAsync(() =>
        {
            var wasSupressing = _suppressPropertyChangeHandling;
            _suppressPropertyChangeHandling = true;
            _isLoadingData = true;

            try
            {
                Name = string.Empty;
                Description = string.Empty;
                IsFavorite = false;
                IsActive = true;

                ClearValidationErrors();
            }
            finally
            {
                _isLoadingData = false;
                _suppressPropertyChangeHandling = wasSupressing;
                HasUnsavedChanges = false;
            }

            this.LogInfo("Form cleared for next entry");
            return Task.CompletedTask;
        }, "Clear Form For Next Entry");
    }

    /// <summary>
    /// Enhanced validation error clearing
    /// </summary>
    protected virtual void ClearValidationErrors()
    {
        this.SafeExecute(() =>
        {
            NameValidationMessage = string.Empty;
            IsNameValid = true;
            NameError = string.Empty;
            DescriptionError = string.Empty;
            IsDescriptionValid = true;
        }, "Clear Validation Errors");
    }

    #endregion

    #region Enhanced Entity Loading

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
    /// Enhanced populate form fields from loaded entity
    /// </summary>
    protected virtual async Task PopulateFromEntityAsync(T entity)
    {
        await this.SafeExecuteAsync(async () =>
        {
            _isInitializing = true;
            _suppressValidation = true;
            _suppressPropertyChangeHandling = true;
            _isLoadingData = true;

            Name = entity.Name;
            Description = entity.Description ?? string.Empty;
            IsActive = entity.IsActive;
            IsFavorite = entity.IsFavorite;
            IsSystemDefault = entity.IsSystemDefault;
            CreatedAt = entity.CreatedAt;
            UpdatedAt = entity.UpdatedAt;

            await PopulateEntitySpecificFieldsAsync(entity);

            _isInitializing = false;
            _suppressValidation = false;
            _suppressPropertyChangeHandling = false;
            _isLoadingData = false;
            HasUnsavedChanges = false;

            UpdateFormCompletionProgress();
            UpdateSaveButton();

            this.LogSuccess($"Populated from {EntityName}: {Name}");
        }, "Populate From Entity");
    }

    /// <summary>
    /// Virtual method for entity-specific field population
    /// Override in derived classes for additional fields
    /// </summary>
    protected virtual Task PopulateEntitySpecificFieldsAsync(T entity)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Entity Preparation

    /// <summary>
    /// Enhanced prepare entity object for save operation
    /// </summary>
    protected virtual Task<T> PrepareEntityForSaveAsync()
    {
        try
        {
            T entity = new()
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

            PrepareEntitySpecificFields(entity);

            this.LogInfo($"Prepared {EntityName} for save: {entity.Name}");
            return Task.FromResult(entity);
        }
        catch (Exception ex)
        {
            this.LogError($"Error preparing entity for save: {ex.Message}");
            return Task.FromResult(new T());
        }
    }

    /// <summary>
    /// Virtual method for entity-specific field preparation
    /// Override in derived classes for additional fields
    /// </summary>
    protected virtual void PrepareEntitySpecificFields(T entity)
    {
        // Base implementation - no additional fields
    }

    /// <summary>
    /// Enhanced update local data from saved entity
    /// </summary>
    protected virtual async Task UpdateLocalDataFromSavedEntity(T savedEntity)
    {
        await this.SafeExecuteAsync(async () =>
        {
            EntityId = savedEntity.Id;
            UpdatedAt = savedEntity.UpdatedAt;

            await UpdateEntitySpecificLocalDataAsync(savedEntity);

            this.LogInfo($"Updated local data from saved {EntityName}");
        }, "Update Local Data");
    }

    /// <summary>
    /// Virtual method for entity-specific local data updates
    /// Override in derived classes for additional fields
    /// </summary>
    protected virtual Task UpdateEntitySpecificLocalDataAsync(T savedEntity)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Enhanced Lifecycle Events

    /// <summary>
    /// Virtual callback after successful save operation
    /// </summary>
    protected virtual async Task OnSaveSuccessAsync(T savedEntity)
    {
        this.LogSuccess($"{EntityName} saved successfully");
        await OnEntitySavedAsync(savedEntity);
    }

    /// <summary>
    /// Virtual method for entity saved notifications - can be overridden
    /// </summary>
    protected virtual Task OnEntitySavedAsync(T savedEntity)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Virtual method for entity deleted notifications - can be overridden
    /// </summary>
    protected virtual Task OnEntityDeletedAsync()
    {
        return Task.CompletedTask;
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
    /// Show error message using standardized toast extensions - override base method
    /// </summary>
    protected override async Task ShowErrorAsync(string title, string message)
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

    #region Navigation Framework

    /// <summary>
    /// Enhanced apply navigation parameters for edit mode initialization
    /// </summary>
    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ApplyQueryAttributes for {EntityName} with {query.Count} parameters");

            _suppressValidation = true;
            _suppressPropertyChangeHandling = true;
            _isLoadingData = true;

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

                _ = LoadEntityAsync();
            }
            else
            {
                EntityId = null;
                _isEditMode = false;
                Title = $"New {EntityName}";
                SaveButtonText = "Create";
                this.LogInfo($"CREATE MODE for {EntityName}");

                _isInitializing = false;
                _suppressValidation = false;
                _suppressPropertyChangeHandling = false;
                _isLoadingData = false;
                HasUnsavedChanges = false;
            }
        }, "Apply Query Attributes");
    }

    /// <summary>
    /// Safely convert object to Guid
    /// </summary>
    private Guid? ConvertToGuid(object? obj)
    {
        try
        {
            if (obj == null) return null;

            if (obj is Guid guid)
                return guid;

            if (obj is string str && Guid.TryParse(str, out var parsedGuid))
                return parsedGuid;

            this.LogWarning($"Cannot convert {obj} ({obj.GetType().Name}) to Guid");
            return null;
        }
        catch (Exception ex)
        {
            this.LogError($"Error converting to Guid: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Navigate back to previous page with safe execution
    /// </summary>
    protected virtual async Task NavigateBackAsync()
    {
        HasUnsavedChanges = false;
        this.LogInfo("Cleared HasUnsavedChanges before navigating back");

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

    #region Lifecycle Management

    /// <summary>
    /// Enhanced virtual method for ViewModel initialization - override base method
    /// </summary>
    protected override Task InitializeAsync()
    {
        this.LogDebug($"Base ViewModel initialization (override in derived classes)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Enhanced virtual method for OnAppearing lifecycle event - override base method
    /// </summary>
    public override async Task OnAppearingAsync()
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

            await OnAppearingEntitySpecificAsync();
        }, "ViewModel Appearing");
    }

    /// <summary>
    /// Virtual method for entity-specific appearing logic
    /// Override in derived classes for additional appearing logic
    /// </summary>
    protected virtual Task OnAppearingEntitySpecificAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Enhanced clean up resources and dispose timers
    /// </summary>
    public virtual void Dispose()
    {
        this.SafeDispose(_validationTimer, "Validation Timer");
        this.LogInfo($"Disposed resources for {EntityName}");
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Standard constructor - requires abstract method implementation
    /// </summary>
    protected BaseEditViewModel(IBaseRepository<T> repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;

        Title = $"{EntityName} Details";
        SaveButtonColor = Colors.Green;

        _suppressValidation = true;
        _suppressPropertyChangeHandling = true;
        _isLoadingData = true;

        IsConnected = true;
        ConnectionStatus = TextConstants.STATUS_CONNECTED;
        ConnectionStatusColor = ColorConstants.CONNECTED_COLOR;

        _ = LoadAllEntitiesForValidationAsync();
        SetupValidation();

        this.LogInfo($"Enhanced BaseEditViewModel initialized for {EntityName}");
    }

    /// <summary>
    /// Enhanced constructor with entity names - eliminates need for abstract methods
    /// Usage: base(repository, navigationService, "Species", "Species", "Genus")
    /// </summary>
    protected BaseEditViewModel(
        IBaseRepository<T> repository,
        INavigationService navigationService,
        string entityName,
        string entityNamePlural,
        string parentEntityName = "") : this(repository, navigationService)
    {
        _entityName = entityName;
        _entityNamePlural = entityNamePlural;
        _parentEntityName = parentEntityName;

        this.LogInfo($"Enhanced BaseEditViewModel initialized for {entityName} with parent {parentEntityName}");
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
            _allEntities = [.. result.Data];
            this.LogInfo($"Loaded {_allEntities.Count} {EntityNamePlural.ToLower()} for validation");
        }
        else
        {
            _allEntities = [];
            this.LogWarning("Failed to load entities for validation, using empty list");
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _suppressValidation = false;
            _suppressPropertyChangeHandling = false;
            _isLoadingData = false;
        });
    }

    #endregion

    #region Helper Methods - Performance

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
    /// Suppress data loading flag temporarily
    /// </summary>
    protected void SuppressDataLoading(bool suppress)
    {
        _isLoadingData = suppress;
        this.LogDebug($"Data loading suppression: {suppress}");
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

    /// <summary>
    /// Execute action with all suppressions enabled
    /// </summary>
    protected void ExecuteWithAllSuppressionsEnabled(Action action)
    {
        var wasSuppressingPropertyChange = _suppressPropertyChangeHandling;
        var wasSuppressingValidation = _suppressValidation;
        var wasLoadingData = _isLoadingData;

        _suppressPropertyChangeHandling = true;
        _suppressValidation = true;
        _isLoadingData = true;

        try
        {
            action();
        }
        finally
        {
            _suppressPropertyChangeHandling = wasSuppressingPropertyChange;
            _suppressValidation = wasSuppressingValidation;
            _isLoadingData = wasLoadingData;
        }
    }

    /// <summary>
    /// Execute async action with all suppressions enabled
    /// </summary>
    protected async Task ExecuteWithAllSuppressionsEnabledAsync(Func<Task> action)
    {
        var wasSuppressingPropertyChange = _suppressPropertyChangeHandling;
        var wasSuppressingValidation = _suppressValidation;
        var wasLoadingData = _isLoadingData;

        _suppressPropertyChangeHandling = true;
        _suppressValidation = true;
        _isLoadingData = true;

        try
        {
            await action();
        }
        finally
        {
            _suppressPropertyChangeHandling = wasSuppressingPropertyChange;
            _suppressValidation = wasSuppressingValidation;
            _isLoadingData = wasLoadingData;
        }
    }

    #endregion
}