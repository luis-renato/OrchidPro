using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

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
        this.LogInfo("Initialized - using base functionality with family relationship management");

        // Load available families for selection
        _ = Task.Run(LoadAvailableFamiliesAsync);
    }

    #endregion

    #region Family Management

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
            if (family != null)
            {
                SelectedFamilyId = family.Id;
                SelectedFamilyName = family.Name;
                this.LogInfo($"Selected family: {family.Name}");
            }
            else
            {
                SelectedFamilyId = null;
                SelectedFamilyName = string.Empty;
                this.LogInfo("Cleared family selection");
            }

            // Clear family validation error when selection changes
            // Note: Using simple validation approach since ObservableProperty validation fields aren't available

            // Update family context display
            OnPropertyChanged(nameof(FamilyContext));

        }, "Set Selected Family");
    }

    /// <summary>
    /// Open family selection dialog or picker
    /// </summary>
    [RelayCommand]
    private async Task SelectFamilyAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Opening family selection");

            if (!AvailableFamilies.Any())
            {
                await LoadAvailableFamiliesAsync();
            }

            if (!AvailableFamilies.Any())
            {
                await ShowErrorAsync("No families available", "Please create a family first.");
                return;
            }

            // For now, we'll navigate to family selection or show in UI
            // This could be enhanced with a popup selector
            IsFamilySelectionVisible = true;

        }, "Failed to open family selection");
    }

    /// <summary>
    /// Navigate to create new family
    /// </summary>
    [RelayCommand]
    private async Task CreateNewFamilyAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Navigating to create new family");

            await _navigationService.NavigateToAsync("familyedit");

        }, "Failed to navigate to create family");
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate family selection
    /// </summary>
    private bool ValidateFamilySelection()
    {
        return this.SafeExecute(() =>
        {
            if (!SelectedFamilyId.HasValue)
            {
                this.LogWarning("Validation failed: No family selected");
                return false;
            }

            return true;

        }, fallbackValue: false, operationName: "ValidateFamilySelection");
    }

    /// <summary>
    /// Enhanced pre-save validation
    /// </summary>
    private bool ValidateForSave()
    {
        return this.SafeExecute(() =>
        {
            var isValid = true;

            // Validate family selection
            if (!ValidateFamilySelection())
            {
                isValid = false;
            }

            // Validate name is not empty (base validation should handle this)
            if (string.IsNullOrWhiteSpace(Name))
            {
                isValid = false;
                this.LogWarning("Validation failed: Name is required");
            }

            this.LogInfo($"Genus validation complete: {isValid}");
            return isValid;

        }, fallbackValue: false, operationName: "ValidateForSave");
    }

    #endregion

    #region Save Operations

    /// <summary>
    /// Override save to include family relationship
    /// </summary>
    [RelayCommand]
    private async Task SaveWithFamilyAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!ValidateForSave())
            {
                await ShowErrorAsync("Validation Error", "Please correct the validation errors before saving.");
                return;
            }

            this.LogInfo($"Saving genus: {Name} in family {SelectedFamilyName}");

            // Create or update the genus
            var genus = new Genus
            {
                Id = EntityId ?? Guid.NewGuid(),
                FamilyId = SelectedFamilyId!.Value,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                CreatedAt = IsEditMode ? CreatedAt : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Genus savedGenus;
            if (IsEditMode)
            {
                savedGenus = await _genusRepository.UpdateAsync(genus);
                this.LogSuccess($"Updated genus: {savedGenus.Name}");
            }
            else
            {
                savedGenus = await _genusRepository.CreateAsync(genus);
                this.LogSuccess($"Created genus: {savedGenus.Name}");
            }

            // Update local data
            EntityId = savedGenus.Id;
            UpdatedAt = savedGenus.UpdatedAt;

            await ShowSuccessAsync($"Genus '{savedGenus.Name}' saved successfully!");

            // Navigate back
            await _navigationService.GoBackAsync();

        }, "Failed to save genus");
    }

    /// <summary>
    /// Save and create another genus in same family
    /// </summary>
    [RelayCommand]
    private async Task SaveAndCreateAnotherAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!ValidateForSave())
            {
                await ShowErrorAsync("Validation Error", "Please correct the validation errors before saving.");
                return;
            }

            // Save current genus
            var genus = new Genus
            {
                Id = Guid.NewGuid(),
                FamilyId = SelectedFamilyId!.Value,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var savedGenus = await _genusRepository.CreateAsync(genus);
            this.LogSuccess($"Created genus: {savedGenus.Name}");

            await ShowSuccessAsync($"Genus '{savedGenus.Name}' saved successfully!");

            // Keep the current family selection and reset form
            var currentFamilyId = SelectedFamilyId;
            var currentFamilyName = SelectedFamilyName;

            // Reset form for new entry
            Name = string.Empty;
            Description = string.Empty;
            IsActive = true;
            IsFavorite = false;
            EntityId = null;
            _isEditMode = false;

            // Restore family selection
            SelectedFamilyId = currentFamilyId;
            SelectedFamilyName = currentFamilyName;
            OnPropertyChanged(nameof(FamilyContext));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));

            this.LogSuccess($"Ready to create another genus in {currentFamilyName}");

        }, "Failed to save and create another");
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Load genus data for editing
    /// </summary>
    private async Task LoadGenusDataAsync(Guid genusId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Loading genus data: {genusId}");

            var genus = await _genusRepository.GetByIdAsync(genusId);
            if (genus != null)
            {
                // Populate basic fields
                EntityId = genus.Id;
                Name = genus.Name;
                Description = genus.Description ?? string.Empty;
                IsActive = genus.IsActive;
                IsFavorite = genus.IsFavorite;
                CreatedAt = genus.CreatedAt;
                UpdatedAt = genus.UpdatedAt;

                // Set family information
                SelectedFamilyId = genus.FamilyId;

                // Load family name
                var family = await _familyRepository.GetByIdAsync(genus.FamilyId);
                if (family != null)
                {
                    SelectedFamilyName = family.Name;
                    this.LogInfo($"Loaded genus {genus.Name} in family {family.Name}");
                }
                else
                {
                    this.LogWarning($"Family not found for genus: {genus.FamilyId}");
                    SelectedFamilyName = "Unknown Family";
                }

                _isEditMode = true;
                OnPropertyChanged(nameof(FamilyContext));
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(IsEditMode));
            }

        }, "Failed to load genus entity data");
    }

    #endregion

    #region Query Attributes Handling

    /// <summary>
    /// Handle navigation parameters including family pre-selection
    /// </summary>
    public new void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ApplyQueryAttributes called with {query.Count} parameters");

            // Log all parameters for debugging
            foreach (var param in query)
            {
                this.LogInfo($"Parameter: {param.Key} = {param.Value} ({param.Value?.GetType().Name})");
            }

            // Handle family pre-selection
            if (query.TryGetValue("FamilyId", out var familyIdObj) && familyIdObj != null)
            {
                if (Guid.TryParse(familyIdObj.ToString(), out var familyId))
                {
                    SelectedFamilyId = familyId;

                    if (query.TryGetValue("FamilyName", out var familyNameObj) && familyNameObj != null)
                    {
                        SelectedFamilyName = familyNameObj.ToString() ?? "";
                    }

                    this.LogInfo($"Pre-selected family from navigation: {SelectedFamilyName} ({familyId})");
                }
            }

            // Handle genus editing
            if (query.TryGetValue("GenusId", out var genusIdObj) && genusIdObj != null)
            {
                if (Guid.TryParse(genusIdObj.ToString(), out var genusId))
                {
                    this.LogInfo($"Loading genus for editing: {genusId}");
                    _ = Task.Run(() => LoadGenusDataAsync(genusId));
                }
            }

            // Call base implementation for other parameters
            base.ApplyQueryAttributes(query);

            // Notify UI of changes
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(FamilyContext));

            this.LogSuccess($"Query attributes applied - IsEditMode: {IsEditMode}, PageTitle: {PageTitle}, Family: {SelectedFamilyName}");

        }, "ApplyQueryAttributes");
    }

    #endregion

    #region Property Change Notifications

    /// <summary>
    /// Handle family selection changes
    /// </summary>
    partial void OnSelectedFamilyIdChanged(Guid? value)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Selected family ID changed: {value}");
            OnPropertyChanged(nameof(FamilyContext));
            OnPropertyChanged(nameof(CanCreateAnother));
        }, "OnSelectedFamilyIdChanged");
    }

    #endregion

    #region Public Properties for UI Binding

    /// <summary>
    /// Whether family selection is required (always true for genus)
    /// </summary>
    public bool IsFamilyRequired => true;

    /// <summary>
    /// Help text for family selection
    /// </summary>
    public string FamilySelectionHelpText => "Select the botanical family this genus belongs to. This is required for proper taxonomic classification.";

    /// <summary>
    /// Whether we can create another genus (useful for bulk entry)
    /// </summary>
    public bool CanCreateAnother => !IsEditMode && SelectedFamilyId.HasValue;

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
            // Ensure families are loaded
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
}