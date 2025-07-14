using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// ViewModel for creating and editing Family entities
/// </summary>
public partial class FamilyEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IFamilyRepository _familyRepository;
    private readonly INavigationService _navigationService;

    private Family? _originalFamily;
    private bool _isEditMode;

    [ObservableProperty]
    private Guid? familyId;

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
    private SyncStatus syncStatus;

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
    private bool canSave;

    [ObservableProperty]
    private bool canDelete;

    [ObservableProperty]
    private bool isNameFocused;

    [ObservableProperty]
    private bool isDescriptionFocused;

    [ObservableProperty]
    private string saveButtonText = "Save";

    [ObservableProperty]
    private Color saveButtonColor;

    public string PageTitle => _isEditMode ? "Edit Family" : "Add Family";
    public string PageSubtitle => _isEditMode ? "Modify family information" : "Create a new botanical family";

    public FamilyEditViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
    {
        _familyRepository = familyRepository;
        _navigationService = navigationService;

        Title = "Family Details";
        SaveButtonColor = Colors.Green;

        // Set up validation
        SetupValidation();
    }

    /// <summary>
    /// Applies query attributes for navigation parameters
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("FamilyId", out var familyIdObj) && familyIdObj is Guid id)
        {
            FamilyId = id;
            _isEditMode = true;
        }
        else
        {
            _isEditMode = false;
            FamilyId = null;
        }

        Title = PageTitle;
        Subtitle = PageSubtitle;
    }

    /// <summary>
    /// Initializes the ViewModel
    /// </summary>
    protected override async Task InitializeAsync()
    {
        if (_isEditMode && FamilyId.HasValue)
        {
            await LoadFamilyAsync();
        }
        else
        {
            SetupNewFamily();
        }

        UpdateSaveButton();
    }

    /// <summary>
    /// Loads family data for editing
    /// </summary>
    [RelayCommand]
    private async Task LoadFamilyAsync()
    {
        if (!FamilyId.HasValue) return;

        try
        {
            IsBusy = true;

            var family = await _familyRepository.GetByIdAsync(FamilyId.Value);
            if (family != null)
            {
                _originalFamily = family.Clone();
                PopulateFromFamily(family);
                CanDelete = !family.IsSystemDefault;
            }
            else
            {
                await ShowErrorAsync("Family Not Found", "The requested family could not be found.");
                await _navigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading family: {ex.Message}");
            await ShowErrorAsync("Load Error", "Failed to load family data.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Sets up a new family with default values
    /// </summary>
    private void SetupNewFamily()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        SyncStatus = SyncStatus.Local;
        IsActive = true;
        IsSystemDefault = false;
        CanDelete = false;

        // Focus on name field for new entries
        IsNameFocused = true;
    }

    /// <summary>
    /// Populates fields from a family entity
    /// </summary>
    private void PopulateFromFamily(Family family)
    {
        Name = family.Name;
        Description = family.Description ?? string.Empty;
        IsActive = family.IsActive;
        IsSystemDefault = family.IsSystemDefault;
        CreatedAt = family.CreatedAt;
        UpdatedAt = family.UpdatedAt;
        SyncStatus = family.SyncStatus;

        // Reset change tracking after loading
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Saves the family (create or update)
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!await ValidateFormAsync()) return;

        try
        {
            IsBusy = true;
            SaveButtonText = "Saving...";
            SaveButtonColor = Colors.Orange;

            Family family;

            if (_isEditMode && _originalFamily != null)
            {
                // Update existing family
                family = _originalFamily.Clone();
                family.Name = Name.Trim();
                family.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
                family.IsActive = IsActive;

                family = await _familyRepository.UpdateAsync(family);
                await ShowSuccessAsync("Family updated successfully!");
            }
            else
            {
                // Create new family
                family = new Family
                {
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    IsActive = IsActive
                };

                family = await _familyRepository.CreateAsync(family);
                await ShowSuccessAsync("Family created successfully!");
            }

            // Reset change tracking
            HasUnsavedChanges = false;

            // Navigate back to list
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving family: {ex.Message}");
            await ShowErrorAsync("Save Error", "Failed to save family. Please try again.");
        }
        finally
        {
            IsBusy = false;
            SaveButtonText = "Save";
            SaveButtonColor = Colors.Green;
        }
    }

    /// <summary>
    /// Deletes the family
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!_isEditMode || !FamilyId.HasValue || IsSystemDefault) return;

        var confirmed = await ShowConfirmAsync(
            "Delete Family",
            $"Are you sure you want to delete '{Name}'? This action cannot be undone.");

        if (!confirmed) return;

        try
        {
            IsBusy = true;

            var success = await _familyRepository.DeleteAsync(FamilyId.Value);

            if (success)
            {
                await ShowSuccessAsync("Family deleted successfully!");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await ShowErrorAsync("Delete Error", "Failed to delete family. It may be protected or in use.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting family: {ex.Message}");
            await ShowErrorAsync("Delete Error", "Failed to delete family. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Cancels editing and navigates back
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasUnsavedChanges)
        {
            var confirmed = await ShowConfirmAsync(
                "Discard Changes",
                "You have unsaved changes. Are you sure you want to discard them?");

            if (!confirmed) return;
        }

        await _navigationService.GoBackAsync();
    }

    /// <summary>
    /// Validates the name field
    /// </summary>
    [RelayCommand]
    private async Task ValidateNameAsync()
    {
        try
        {
            var trimmedName = Name?.Trim() ?? string.Empty;

            // Basic validation
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                NameError = "Family name is required";
                IsNameValid = false;
                return;
            }

            if (trimmedName.Length < 2)
            {
                NameError = "Family name must be at least 2 characters";
                IsNameValid = false;
                return;
            }

            if (trimmedName.Length > 255)
            {
                NameError = "Family name cannot exceed 255 characters";
                IsNameValid = false;
                return;
            }

            // Check for duplicates
            var excludeId = _isEditMode ? FamilyId : null;
            var nameExists = await _familyRepository.NameExistsAsync(trimmedName, excludeId);

            if (nameExists)
            {
                NameError = "A family with this name already exists";
                IsNameValid = false;
                return;
            }

            // All validations passed
            NameError = string.Empty;
            IsNameValid = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error validating name: {ex.Message}");
            NameError = "Error validating name";
            IsNameValid = false;
        }
        finally
        {
            UpdateSaveButton();
        }
    }

    /// <summary>
    /// Validates the description field
    /// </summary>
    [RelayCommand]
    private void ValidateDescription()
    {
        var trimmedDescription = Description?.Trim() ?? string.Empty;

        if (trimmedDescription.Length > 2000)
        {
            DescriptionError = "Description cannot exceed 2000 characters";
            IsDescriptionValid = false;
        }
        else
        {
            DescriptionError = string.Empty;
            IsDescriptionValid = true;
        }

        UpdateSaveButton();
    }

    /// <summary>
    /// Validates the entire form
    /// </summary>
    private async Task<bool> ValidateFormAsync()
    {
        await ValidateNameAsync();
        ValidateDescription();

        return IsNameValid && IsDescriptionValid;
    }

    /// <summary>
    /// Updates the save button state
    /// </summary>
    private void UpdateSaveButton()
    {
        CanSave = IsNameValid && IsDescriptionValid && !string.IsNullOrWhiteSpace(Name);
    }

    /// <summary>
    /// Sets up validation event handlers
    /// </summary>
    private void SetupValidation()
    {
        // Validate on property changes with debouncing
        PropertyChanged += async (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Name):
                    HasUnsavedChanges = true;
                    // Debounce validation
                    await Task.Delay(300);
                    if (e.PropertyName == nameof(Name)) // Still the same property
                    {
                        await ValidateNameAsync();
                    }
                    break;

                case nameof(Description):
                    HasUnsavedChanges = true;
                    ValidateDescription();
                    break;

                case nameof(IsActive):
                    HasUnsavedChanges = true;
                    break;
            }
        };
    }

    [RelayCommand] private void OnNameFocused() => IsNameFocused = true;
    [RelayCommand] private async Task OnNameUnfocusedAsync() { IsNameFocused = false; await ValidateNameAsync(); }
    [RelayCommand] private void OnDescriptionFocused() => IsDescriptionFocused = true;
    [RelayCommand] private void OnDescriptionUnfocused() { IsDescriptionFocused = false; ValidateDescription(); }
    [RelayCommand] private void ToggleActiveStatus() { IsActive = !IsActive; HasUnsavedChanges = true; }
    [RelayCommand] private void ClearDescription() => Description = string.Empty;

    [RelayCommand]
    private async Task ShowInfoAsync()
    {
        var message = _isEditMode
            ? $"Family ID: {FamilyId}\nCreated: {CreatedAt:F}\nLast Updated: {UpdatedAt:F}\nSync Status: {SyncStatus}"
            : "This will create a new botanical family in your collection.";

        await ShowErrorAsync("Family Information", message);
    }

    public string SyncStatusDisplay => SyncStatus switch
    {
        SyncStatus.Synced => "✅ Synced with server",
        SyncStatus.Local => "📱 Local only",
        SyncStatus.Pending => "⏳ Pending sync",
        SyncStatus.Error => "❌ Sync error",
        _ => "❓ Unknown status"
    };

    public Color SyncStatusColor => SyncStatus switch
    {
        SyncStatus.Synced => Colors.Green,
        SyncStatus.Local => Colors.Orange,
        SyncStatus.Pending => Colors.Blue,
        SyncStatus.Error => Colors.Red,
        _ => Colors.Gray
    };
}