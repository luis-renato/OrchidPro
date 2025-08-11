using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;
using OrchidPro.Messages;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// ViewModel for editing and creating botanical genus records.
/// Provides genus-specific functionality while leveraging enhanced base edit operations.
/// Includes family relationship management and hierarchical validation.
/// </summary>
public partial class GenusEditViewModel : BaseEditViewModel<Genus>, IQueryAttributable
{
    #region Private Fields

    private readonly IGenusRepository _genusRepository;
    private readonly IFamilyRepository _familyRepository;
    private bool _isLoadingData = false; // Flag to prevent HasUnsavedChanges during data loading

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private Guid? selectedFamilyId;

    [ObservableProperty]
    private string selectedFamilyName = string.Empty;

    [ObservableProperty]
    private bool isFamilySelectionVisible = true;

    [ObservableProperty]
    private List<Family> availableFamilies = new();

    [ObservableProperty]
    private bool isLoadingFamilies;

    [ObservableProperty]
    private string saveAndContinueButtonText = "Save & Add Another";

    [ObservableProperty]
    private bool showSaveAndContinue = true;

    /// <summary>
    /// Currently selected family object for ComboBox binding
    /// </summary>
    public Family? SelectedFamily
    {
        get => AvailableFamilies?.FirstOrDefault(f => f.Id == SelectedFamilyId);
        set
        {
            if (value != null)
            {
                SetSelectedFamily(value);
            }
        }
    }

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Genus";
    public override string EntityNamePlural => "Genera";

    #endregion

    #region Page Title Management

    /// <summary>
    /// Dynamic page title based on edit mode state and family context
    /// </summary>
    public new string PageTitle => IsEditMode ? "Edit Genus" : "New Genus";

    /// <summary>
    /// Current edit mode state for UI binding
    /// </summary>
    public bool IsEditMode => _isEditMode;

    /// <summary>
    /// Family context for display
    /// </summary>
    public string FamilyContext =>
        !string.IsNullOrEmpty(SelectedFamilyName) ? $"in {SelectedFamilyName}" : "";

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize genus edit ViewModel with enhanced base functionality and family management
    /// </summary>
    public GenusEditViewModel(IGenusRepository genusRepository, IFamilyRepository familyRepository, INavigationService navigationService)
        : base(genusRepository, navigationService)
    {
        _genusRepository = genusRepository;
        _familyRepository = familyRepository;

        SaveAndContinueCommand = new AsyncRelayCommand(SaveAndCreateAnotherAsync, () => CanCreateAnother);
        DeleteCommand = new AsyncRelayCommand(DeleteGenusAsync, () => CanDelete);

        // Subscribe to family created message
        WeakReferenceMessenger.Default.Register<FamilyCreatedMessage>(this, OnFamilyCreated);

        // Monitor genus-specific properties for HasUnsavedChanges
        PropertyChanged += OnGenusPropertyChanged;

        this.LogInfo("Initialized - using base functionality with family relationship management");

        // Load available families for selection
        _ = Task.Run(LoadAvailableFamiliesAsync);
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand SaveAndContinueCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }

    #endregion

    #region Family Management

    /// <summary>
    /// Monitor genus-specific properties for HasUnsavedChanges tracking.
    /// Base handles Name, Description, IsActive, IsFavorite automatically.
    /// </summary>
    private void OnGenusPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            // IGNORE changes during data loading (same pattern as BaseEditViewModel)
            if (_isLoadingData) return;

            // Monitor ALL properties that should trigger HasUnsavedChanges INCLUDING Name
            // (Base might not be working correctly, so we handle ALL here)
            if (e.PropertyName is nameof(Name) or nameof(Description) or nameof(IsActive) or nameof(IsFavorite) or
                nameof(SelectedFamilyId) or nameof(SelectedFamilyName))
            {
                HasUnsavedChanges = true;
                this.LogDebug($"Property {e.PropertyName} changed - marked HasUnsavedChanges = true");
            }
        }, "Genus Property Change");
    }

    /// <summary>
    /// Load available families for selection dropdown
    /// </summary>
    private async Task LoadAvailableFamiliesAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            IsLoadingFamilies = true;
            this.LogInfo("Loading available families for genus selection");

            var families = await _familyRepository.GetAllAsync(false); // Only active families
            AvailableFamilies = families.OrderBy(f => f.Name).ToList();

            this.LogSuccess($"Loaded {AvailableFamilies.Count} families for selection");

            // Notify SelectedFamily after families are loaded
            OnPropertyChanged(nameof(SelectedFamily));

        }, "Failed to load available families");

        IsLoadingFamilies = false;
    }

    /// <summary>
    /// Set selected family for genus
    /// </summary>
    [RelayCommand]
    private void SetSelectedFamily(Family? family)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Setting selected family: {family?.Name ?? "None"}");

            SelectedFamilyId = family?.Id;
            SelectedFamilyName = family?.Name ?? string.Empty;

            // Update UI
            OnPropertyChanged(nameof(SelectedFamily));
            OnPropertyChanged(nameof(FamilyContext));
            OnPropertyChanged(nameof(CanCreateAnother));

            // FIXED: Only mark as having changes if we're not loading data
            if (!_isLoadingData)
            {
                HasUnsavedChanges = true;
            }

        }, "Set Selected Family");
    }

    /// <summary>
    /// Handle family created message
    /// </summary>
    private async void OnFamilyCreated(object recipient, FamilyCreatedMessage message)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Received family created message: {message.FamilyName}");

            // Reload families
            await LoadAvailableFamiliesAsync();

            // Auto-select the newly created family
            var newFamily = AvailableFamilies.FirstOrDefault(f => f.Id == message.FamilyId);
            if (newFamily != null)
            {
                SetSelectedFamily(newFamily);
                this.LogSuccess($"Auto-selected newly created family: {newFamily.Name}");
            }

        }, "OnFamilyCreated");
    }

    #endregion

    #region Save Operations - ENHANCED WITH GENUS CREATED MESSAGE

    /// <summary>
    /// Override save to include family relationship
    /// </summary>
    [RelayCommand]
    private async Task SaveWithFamilyAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (SelectedFamilyId == null)
            {
                await this.ShowErrorToast("Please select a family first");
                return;
            }

            this.LogInfo($"Saving genus: {Name} in family {SelectedFamilyName}");

            // Create the genus with family relationship
            var genus = new Genus
            {
                Id = EntityId ?? Guid.NewGuid(),
                FamilyId = SelectedFamilyId.Value,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = _isEditMode
                ? await _genusRepository.UpdateAsync(genus)
                : await _genusRepository.CreateAsync(genus);

            if (result != null)
            {
                // SEND GENUS CREATED MESSAGE FOR SPECIES AUTO-SELECTION - IGUAL FAMILY
                if (!_isEditMode)
                {
                    var message = new GenusCreatedMessage(result.Id, result.Name ?? "Unknown");
                    WeakReferenceMessenger.Default.Send(message);
                    this.LogInfo($"Sent GenusCreatedMessage for: {result.Name}");
                }

                // Clear unsaved changes BEFORE showing toast and navigating
                HasUnsavedChanges = false;
                this.LogInfo("Cleared HasUnsavedChanges flag");

                await this.ShowSuccessToast($"Genus {(_isEditMode ? "updated" : "created")} successfully!");

                // Send message to refresh genus list
                WeakReferenceMessenger.Default.Send(new GenusUpdatedMessage());

                // Navigate back with forced clear of unsaved changes
                await ForceNavigateBackAsync();
            }
            else
            {
                await this.ShowErrorToast($"Failed to {(_isEditMode ? "update" : "create")} genus.");
            }
        }, "Save Genus with Family");
    }

    /// <summary>
    /// Save and create another genus - EXACT pattern from GenusEditViewModel
    /// </summary>
    [RelayCommand]
    private async Task SaveAndCreateAnotherAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (SelectedFamilyId == null)
            {
                await this.ShowErrorToast("Please select a family first");
                return;
            }

            // Remember current family selection
            var currentFamilyId = SelectedFamilyId;
            var currentFamilyName = SelectedFamilyName;

            this.LogInfo($"Saving and creating another genus in family: {currentFamilyName}");

            var genus = new Genus
            {
                Id = Guid.NewGuid(),
                FamilyId = SelectedFamilyId.Value,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _genusRepository.CreateAsync(genus);

            if (result != null)
            {
                // SEND GENUS CREATED MESSAGE FOR SPECIES AUTO-SELECTION - IGUAL FAMILY
                var message = new GenusCreatedMessage(result.Id, result.Name ?? "Unknown");
                WeakReferenceMessenger.Default.Send(message);
                this.LogInfo($"Sent GenusCreatedMessage for: {result.Name}");

                // Clear unsaved changes BEFORE showing toast
                HasUnsavedChanges = false;
                this.LogInfo("Cleared HasUnsavedChanges flag for save and continue");

                await this.ShowSuccessToast("Genus created! Ready for next one.");

                // Send message to refresh genus list
                WeakReferenceMessenger.Default.Send(new GenusUpdatedMessage());

                // Clear form but keep family selection
                ClearFormButKeepFamily();
            }
            else
            {
                await this.ShowErrorToast("Failed to create genus.");
            }
        }, "Save Genus and Continue");
    }

    /// <summary>
    /// Delete genus with confirmation
    /// </summary>
    [RelayCommand]
    private async Task DeleteGenusAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!EntityId.HasValue) return;

            var confirmed = await this.ShowConfirmationAsync(
                "Delete Genus",
                $"Are you sure you want to delete '{Name}'? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            this.LogInfo($"Deleting genus: {Name}");

            var success = await _genusRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Genus deleted successfully!");

                // Send message to refresh genus list
                WeakReferenceMessenger.Default.Send(new GenusUpdatedMessage());

                await NavigateBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete genus.");
            }
        }, "Delete Genus");
    }

    #endregion

    #region Form Management

    /// <summary>
    /// Clear form but keep family selection for creating another genus
    /// </summary>
    private void ClearFormButKeepFamily()
    {
        this.SafeExecute(() =>
        {
            var currentFamily = SelectedFamily;

            // Clear form fields
            Name = string.Empty;
            Description = string.Empty;
            IsActive = true;
            IsFavorite = false;
            EntityId = null;
            _isEditMode = false;
            HasUnsavedChanges = false;

            // Restore family selection
            SelectedFamily = currentFamily;

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));

            this.LogInfo($"Form cleared but kept family: {SelectedFamilyName}");
        }, "Clear Form But Keep Family");
    }

    #endregion

    #region Navigation Override

    /// <summary>
    /// Force navigation back without checking unsaved changes
    /// </summary>
    private async Task ForceNavigateBackAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Ensure no unsaved changes flag
            HasUnsavedChanges = false;

            // Use Shell navigation directly to bypass BaseEditViewModel checks
            await Shell.Current.GoToAsync("..");

            this.LogInfo("Forced navigation back completed");
        }, "Force Navigate Back");
    }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Description for family selection requirements
    /// </summary>
    public string FamilySelectionDescription => SelectedFamilyId.HasValue
        ? $"Creating genus in {SelectedFamilyName}"
        : "Please select a family for this genus. This is required for proper taxonomic classification.";

    /// <summary>
    /// Whether we can create another genus (useful for bulk entry)
    /// </summary>
    public bool CanCreateAnother => !IsEditMode && SelectedFamilyId.HasValue;

    /// <summary>
    /// Can delete genus (edit mode only)
    /// </summary>
    public bool CanDelete => IsEditMode && EntityId.HasValue;

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initialize when appearing
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        await this.SafeExecuteAsync(async () =>
        {
            // Only load families if not already loaded
            if (!AvailableFamilies.Any() && !IsLoadingFamilies)
            {
                await LoadAvailableFamiliesAsync();
            }

            // Auto-select family if only one available and none selected
            if (!SelectedFamilyId.HasValue && AvailableFamilies.Count == 1)
            {
                var onlyFamily = AvailableFamilies.First();
                SetSelectedFamily(onlyFamily);
                this.LogInfo($"Auto-selected single available family: {onlyFamily.Name}");
            }

        }, "OnAppearingAsync failed");
    }

    #endregion

    #region IQueryAttributable Implementation

    /// <summary>
    /// Handle navigation parameters
    /// </summary>
    public override async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Applying query attributes for Genus edit");

            if (query.TryGetValue("GenusId", out var genusIdObj) &&
                Guid.TryParse(genusIdObj.ToString(), out var genusId))
            {
                await InitializeForEditAsync(genusId);
            }
            else if (query.TryGetValue("FamilyId", out var familyIdObj) &&
                     Guid.TryParse(familyIdObj.ToString(), out var familyId))
            {
                await InitializeForCreateAsync(familyId);
            }
            else
            {
                await InitializeForCreateAsync();
            }
        }, "Apply Query Attributes");
    }

    /// <summary>
    /// Initialize for creating new genus with optional family preselection
    /// </summary>
    public async Task InitializeForCreateAsync(Guid? familyId = null)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for create mode with family preselection: {familyId}");

            _isEditMode = false;
            EntityId = null;

            // Reset form
            ClearForm();

            // Ensure families are loaded
            if (!AvailableFamilies.Any())
            {
                await LoadAvailableFamiliesAsync();
            }

            // Preselect family if provided
            if (familyId.HasValue)
            {
                var family = AvailableFamilies.FirstOrDefault(f => f.Id == familyId.Value);
                if (family != null)
                {
                    SetSelectedFamily(family);
                    this.LogInfo($"Preselected family: {family.Name}");
                }
            }

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));
        }, "Initialize for Create");
    }

    /// <summary>
    /// Initialize for editing existing genus
    /// </summary>
    public async Task InitializeForEditAsync(Guid genusId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {genusId}");

            _isEditMode = true;
            EntityId = genusId;

            // Ensure families are loaded first
            if (!AvailableFamilies.Any())
            {
                await LoadAvailableFamiliesAsync();
            }

            // Load genus data
            var genus = await _genusRepository.GetByIdAsync(genusId);
            if (genus != null)
            {
                LoadGenusData(genus);
                this.LogSuccess($"Loaded genus for editing: {genus.Name}");
            }
            else
            {
                this.LogError($"Genus not found: {genusId}");
                await this.ShowErrorToast("Genus not found.");
                await NavigateBackAsync();
                return;
            }

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));
        }, "Initialize for Edit");
    }

    /// <summary>
    /// Load genus data into form fields
    /// FIXED: Set loading flag to prevent HasUnsavedChanges during data loading
    /// </summary>
    private void LoadGenusData(Genus genus)
    {
        this.SafeExecute(() =>
        {
            // Set loading flag to prevent HasUnsavedChanges during data loading
            _isLoadingData = true;

            Name = genus.Name ?? string.Empty;
            Description = genus.Description ?? string.Empty;
            IsActive = genus.IsActive;
            IsFavorite = genus.IsFavorite;

            // Select the family
            var family = AvailableFamilies.FirstOrDefault(f => f.Id == genus.FamilyId);
            if (family != null)
            {
                SetSelectedFamily(family);
            }

            // Clear loading flag and reset HasUnsavedChanges
            _isLoadingData = false;
            HasUnsavedChanges = false;

            this.LogInfo($"Loaded genus data: {genus.Name} in family {SelectedFamilyName}");
        }, "Load Genus Data");
    }

    /// <summary>
    /// Clear all form fields
    /// </summary>
    private void ClearForm()
    {
        this.SafeExecute(() =>
        {
            Name = string.Empty;
            Description = string.Empty;
            IsActive = true;
            IsFavorite = false;
            SelectedFamilyId = null;
            SelectedFamilyName = string.Empty;

            OnPropertyChanged(nameof(SelectedFamily));
        }, "Clear Form");
    }

    #endregion
}