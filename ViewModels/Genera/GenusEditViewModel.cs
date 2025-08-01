using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;
using System.Collections.ObjectModel;
using OrchidPro.Services.Data;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// ViewModel for editing genus entities with family relationship management
/// Provides genus-specific form logic while leveraging base edit functionality
/// Follows exact pattern from FamilyEditViewModel with corrected interface
/// </summary>
public partial class GenusEditViewModel : BaseEditViewModel<Genus>
{
    #region Private Fields

    private readonly IGenusRepository _genusRepository;
    private readonly IFamilyRepository _familyRepository;
    private readonly SupabaseService _supabaseService;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<Family> availableFamilies = new();

    [ObservableProperty]
    private Family? selectedFamily;

    [ObservableProperty]
    private Guid? familyId;

    [ObservableProperty]
    private string familyName = string.Empty;

    [ObservableProperty]
    private bool isFamilyValid = true;

    [ObservableProperty]
    private string familyValidationMessage = string.Empty;

    [ObservableProperty]
    private bool isLoadingFamilies;

    [ObservableProperty]
    private bool canSave = false;

    [ObservableProperty]
    private Color saveButtonColor = Colors.Gray;

    [ObservableProperty]
    private bool isNameValid = true;

    [ObservableProperty]
    private string nameValidationMessage = string.Empty;

    [ObservableProperty]
    private string saveButtonText = "Save";

    [ObservableProperty]
    private bool isEditMode = false;

    [ObservableProperty]
    private Guid? entityId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private bool isFavorite = false;

    [ObservableProperty]
    private bool isSaving = false;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Genus";
    public override string EntityNamePlural => "Genera";

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize genus edit ViewModel with family management
    /// </summary>
    public GenusEditViewModel(IGenusRepository repository, IFamilyRepository familyRepository, INavigationService navigationService, SupabaseService supabaseService)
        : base(repository, navigationService)
    {
        _genusRepository = repository;
        _familyRepository = familyRepository;
        _supabaseService = supabaseService;

        // Initialize UI state
        SaveButtonText = "Save";
        SaveButtonColor = Colors.Gray;

        // Load families for selection
        _ = LoadFamiliesAsync();

        // Setup family validation
        SetupFamilyValidation();

        this.LogInfo("GenusEditViewModel initialized with family relationship support");
    }

    #endregion

    #region Family Management

    /// <summary>
    /// Load available families for selection
    /// </summary>
    private async Task LoadFamiliesAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            IsLoadingFamilies = true;
            this.LogInfo("Loading families for genus selection");

            var families = await _familyRepository.GetAllAsync(false); // Only active families

            AvailableFamilies.Clear();
            foreach (var family in families.OrderBy(f => f.Name))
            {
                AvailableFamilies.Add(family);
            }

            this.LogSuccess($"Loaded {families.Count} families");
            IsLoadingFamilies = false;
        }, "Load Families");
    }

    /// <summary>
    /// Setup family validation monitoring
    /// </summary>
    private void SetupFamilyValidation()
    {
        this.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedFamily) || e.PropertyName == nameof(FamilyId))
            {
                ValidateFamily();
                UpdateSaveButtonState();
            }
        };
    }

    /// <summary>
    /// Validate family selection
    /// </summary>
    private void ValidateFamily()
    {
        this.SafeExecute(() =>
        {
            if (SelectedFamily == null && (!FamilyId.HasValue || FamilyId.Value == Guid.Empty))
            {
                IsFamilyValid = false;
                FamilyValidationMessage = "Please select a family for this genus";
            }
            else
            {
                IsFamilyValid = true;
                FamilyValidationMessage = string.Empty;
            }

            this.LogInfo($"Family validation: {(IsFamilyValid ? "Valid" : "Invalid")} - {FamilyValidationMessage}");
        }, "Validate Family");
    }

    /// <summary>
    /// Handle family selection change
    /// </summary>
    [RelayCommand]
    private void SelectFamily(Family family)
    {
        this.SafeExecute(() =>
        {
            SelectedFamily = family;
            FamilyId = family.Id;
            FamilyName = family.Name;

            this.LogInfo($"Selected family: {family.Name}");
            ValidateFamily();
        }, "Select Family");
    }

    #endregion

    #region Enhanced Validation

    /// <summary>
    /// Enhanced name validation checking uniqueness within family
    /// </summary>
    private async Task ValidateGenusNameAsync()
    {
        await this.SafeValidateAsync(async () =>
        {
            // Basic validation first
            if (string.IsNullOrWhiteSpace(Name))
            {
                IsNameValid = false;
                NameValidationMessage = "Genus name is required";
                UpdateSaveButtonState();
                return false;
            }

            if (Name.Length < 2)
            {
                IsNameValid = false;
                NameValidationMessage = "Genus name must be at least 2 characters";
                UpdateSaveButtonState();
                return false;
            }

            // If we have a family, check uniqueness within family
            if (FamilyId.HasValue && !string.IsNullOrWhiteSpace(Name))
            {
                var exists = await _genusRepository.ExistsInFamilyAsync(Name.Trim(), FamilyId.Value, EntityId);
                if (exists)
                {
                    IsNameValid = false;
                    NameValidationMessage = $"A genus named '{Name.Trim()}' already exists in this family";
                    UpdateSaveButtonState();
                    return false;
                }
            }

            IsNameValid = true;
            NameValidationMessage = string.Empty;
            UpdateSaveButtonState();
            return true;
        }, "Enhanced Genus Name Validation");
    }

    /// <summary>
    /// Update save button state with family validation
    /// </summary>
    private void UpdateSaveButtonState()
    {
        this.SafeExecute(() =>
        {
            CanSave = IsNameValid && IsFamilyValid && !IsSaving;

            SaveButtonColor = CanSave ? Colors.Green : Colors.Gray;
            SaveButtonText = IsSaving ? "Saving..." : (IsEditMode ? "Update" : "Create");

            this.LogInfo($"Save button state: CanSave={CanSave}, IsNameValid={IsNameValid}, IsFamilyValid={IsFamilyValid}");
        }, "Update Save Button State");
    }

    #endregion

    #region Data Loading and Entity Management

    /// <summary>
    /// Load genus for editing with family information
    /// </summary>
    private async Task<Genus?> LoadGenusAsync(Guid id)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Loading genus for edit: {id}");
            var genus = await _genusRepository.GetByIdAsync(id);

            if (genus != null)
            {
                // Set family information
                FamilyId = genus.FamilyId;
                FamilyName = genus.FamilyName;

                // Find and set selected family
                SelectedFamily = AvailableFamilies.FirstOrDefault(f => f.Id == genus.FamilyId);
                if (SelectedFamily == null && genus.FamilyId != Guid.Empty)
                {
                    // Family might not be loaded yet, try to load it
                    var family = await _familyRepository.GetByIdAsync(genus.FamilyId);
                    if (family != null)
                    {
                        AvailableFamilies.Add(family);
                        SelectedFamily = family;
                    }
                }

                // Set form fields
                Name = genus.Name;
                Description = genus.Description ?? string.Empty;
                IsActive = genus.IsActive;
                IsFavorite = genus.IsFavorite;

                this.LogSuccess($"Loaded genus: {genus.Name} (Family: {genus.FamilyName})");
            }

            return genus;
        }, "Load Genus")?.Data;
    }

    /// <summary>
    /// Create new genus with family relationship
    /// </summary>
    private Genus CreateNewGenusEntity()
    {
        return this.SafeExecute(() =>
        {
            var genus = new Genus
            {
                Id = Guid.NewGuid(),
                UserId = _supabaseService.GetCurrentUserId(),
                FamilyId = FamilyId ?? Guid.Empty,
                FamilyName = FamilyName,
                Name = Name,
                Description = Description,
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            this.LogInfo($"Created new genus entity with family: {FamilyName}");
            return genus;
        }, fallbackValue: new Genus(), operationName: "Create New Genus");
    }

    /// <summary>
    /// Update entity with form data including family relationship
    /// </summary>
    private void UpdateGenusFromForm(Genus entity)
    {
        this.SafeExecute(() =>
        {
            // Update basic properties
            entity.Name = Name;
            entity.Description = Description;
            entity.IsActive = IsActive;
            entity.IsFavorite = IsFavorite;

            // Set family relationship
            entity.FamilyId = FamilyId ?? Guid.Empty;
            entity.FamilyName = FamilyName;
            entity.UpdatedAt = DateTime.UtcNow;

            this.LogInfo($"Updated genus entity: {entity.Name} (Family: {entity.FamilyName})");
        }, "Update Genus Entity");
    }

    /// <summary>
    /// Update form from entity including family information
    /// </summary>
    private void UpdateFormFromGenus(Genus entity)
    {
        this.SafeExecute(() =>
        {
            // Update basic fields
            Name = entity.Name;
            Description = entity.Description ?? string.Empty;
            IsActive = entity.IsActive;
            IsFavorite = entity.IsFavorite;

            // Set family information
            FamilyId = entity.FamilyId;
            FamilyName = entity.FamilyName;
            SelectedFamily = AvailableFamilies.FirstOrDefault(f => f.Id == entity.FamilyId);

            // Validate family after loading
            ValidateFamily();

            this.LogInfo($"Updated form from genus: {entity.Name} (Family: {entity.FamilyName})");
        }, "Update Form From Genus");
    }

    #endregion

    #region Save and Cancel Operations

    /// <summary>
    /// Save genus with validation
    /// </summary>
    [RelayCommand]
    private async Task Save()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!CanSave) return;

            IsSaving = true;
            UpdateSaveButtonState();

            try
            {
                await ValidateGenusNameAsync();

                if (!IsNameValid || !IsFamilyValid)
                {
                    this.LogWarning("Save cancelled due to validation errors");
                    return;
                }

                Genus result;

                if (IsEditMode && EntityId.HasValue)
                {
                    // Update existing
                    var existing = await _genusRepository.GetByIdAsync(EntityId.Value);
                    if (existing == null)
                    {
                        this.LogError("Cannot find genus to update");
                        return;
                    }

                    UpdateGenusFromForm(existing);
                    result = await _genusRepository.UpdateAsync(existing) ?? existing;
                    this.LogSuccess($"Updated genus: {result.Name}");
                }
                else
                {
                    // Create new
                    var newGenus = CreateNewGenusEntity();
                    result = await _genusRepository.CreateAsync(newGenus) ?? newGenus;
                    this.LogSuccess($"Created genus: {result.Name}");
                }

                // Navigate back
                await _navigationService.GoBackAsync();
            }
            finally
            {
                IsSaving = false;
                UpdateSaveButtonState();
            }
        }, "Save Genus");
    }

    /// <summary>
    /// Cancel editing and navigate back
    /// </summary>
    [RelayCommand]
    private async Task Cancel()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Cancelling genus edit");
            await _navigationService.GoBackAsync();
        }, "Cancel Genus Edit");
    }

    #endregion

    #region Navigation and Query Attributes

    /// <summary>
    /// Enhanced query attributes handling for family context
    /// </summary>
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ApplyQueryAttributes for Genus with {query.Count} parameters");

            // Handle family ID parameter (for creating genus in family context)
            if (query.TryGetValue("familyId", out var familyIdObj) &&
                Guid.TryParse(familyIdObj?.ToString(), out var familyIdValue))
            {
                FamilyId = familyIdValue;

                // Find and select the family
                var family = AvailableFamilies.FirstOrDefault(f => f.Id == familyIdValue);
                if (family != null)
                {
                    SelectedFamily = family;
                    FamilyName = family.Name;
                    this.LogInfo($"Pre-selected family from context: {family.Name}");
                }
            }

            // Handle genus ID parameter
            if (query.TryGetValue("genusId", out var genusIdObj) &&
                Guid.TryParse(genusIdObj?.ToString(), out var genusIdValue))
            {
                EntityId = genusIdValue;
                IsEditMode = true;

                // Load genus data
                _ = LoadGenusAsync(genusIdValue);

                this.LogInfo($"Loading genus for edit: {genusIdValue}");
            }
            else
            {
                IsEditMode = false;
                this.LogInfo("Creating new genus");
            }

            // Update UI state
            UpdateSaveButtonState();
        }, "Apply Genus Query Attributes");
    }

    #endregion

    #region Commands

    /// <summary>
    /// Navigate to family management
    /// </summary>
    [RelayCommand]
    private async Task ManageFamilies()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Navigating to family management");
            await _navigationService.NavigateToAsync("familieslist");
        }, "Navigate to Family Management");
    }

    /// <summary>
    /// Create new family and select it
    /// </summary>
    [RelayCommand]
    private async Task CreateFamily()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Navigating to create new family");

            var parameters = new Dictionary<string, object>
            {
                ["returnRoute"] = "genusedit",
                ["returnParameters"] = new Dictionary<string, object>
                {
                    ["genusId"] = EntityId ?? Guid.Empty
                }
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);
        }, "Navigate to Create Family");
    }

    /// <summary>
    /// Refresh families list
    /// </summary>
    [RelayCommand]
    private async Task RefreshFamilies()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Refreshing families list");
            await LoadFamiliesAsync();

            // Re-select current family if it still exists
            if (FamilyId.HasValue)
            {
                SelectedFamily = AvailableFamilies.FirstOrDefault(f => f.Id == FamilyId.Value);
            }
        }, "Refresh Families");
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handle selected family changes
    /// </summary>
    partial void OnSelectedFamilyChanged(Family? oldValue, Family? newValue)
    {
        this.SafeExecute(() =>
        {
            if (newValue != null && newValue != oldValue)
            {
                FamilyId = newValue.Id;
                FamilyName = newValue.Name;
                this.LogInfo($"Family selection changed to: {newValue.Name}");

                // Re-validate name for uniqueness in new family
                _ = ValidateGenusNameAsync();
            }
        }, "Handle Selected Family Change");
    }

    /// <summary>
    /// Handle family ID changes
    /// </summary>
    partial void OnFamilyIdChanged(Guid? oldValue, Guid? newValue)
    {
        this.SafeExecute(() =>
        {
            ValidateFamily();

            if (newValue.HasValue && newValue != oldValue)
            {
                // Find family in available families
                var family = AvailableFamilies.FirstOrDefault(f => f.Id == newValue.Value);
                if (family != null && SelectedFamily?.Id != family.Id)
                {
                    SelectedFamily = family;
                    FamilyName = family.Name;
                }
            }
        }, "Handle Family ID Change");
    }

    /// <summary>
    /// Handle name changes
    /// </summary>
    partial void OnNameChanged(string oldValue, string newValue)
    {
        this.SafeExecute(() =>
        {
            // Trigger validation with debounce
            _ = ValidateGenusNameAsync();
        }, "Handle Name Change");
    }

    /// <summary>
    /// Handle active status changes
    /// </summary>
    partial void OnIsActiveChanged(bool oldValue, bool newValue)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Active status changed: {newValue}");
            UpdateSaveButtonState();
        }, "Handle Active Change");
    }

    /// <summary>
    /// Handle favorite status changes
    /// </summary>
    partial void OnIsFavoriteChanged(bool oldValue, bool newValue)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Favorite status changed: {newValue}");
        }, "Handle Favorite Change");
    }

    /// <summary>
    /// Handle saving state changes
    /// </summary>
    partial void OnIsSavingChanged(bool oldValue, bool newValue)
    {
        this.SafeExecute(() =>
        {
            UpdateSaveButtonState();
        }, "Handle Saving State Change");
    }

    #endregion

    #region Public Properties for UI

    /// <summary>
    /// Page title based on mode
    /// </summary>
    public string PageTitle => IsEditMode ? $"Edit {EntityName}" : $"Add {EntityName}";

    /// <summary>
    /// Page subtitle
    /// </summary>
    public string PageSubtitle => IsEditMode ? $"Modify {EntityName.ToLower()} information" : $"Create a new {EntityName.ToLower()}";

    #endregion
}