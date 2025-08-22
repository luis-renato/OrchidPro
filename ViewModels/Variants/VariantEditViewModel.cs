using CommunityToolkit.Mvvm.Input;
using OrchidPro.Extensions;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Variants;

/// <summary>
/// MINIMAL Variant edit ViewModel - follows exact pattern of FamilyEditViewModel.
/// Variant is independent like Family, so no parent relationship management needed.
/// Uses enhanced BaseEditViewModel constructor for maximum code reuse.
/// </summary>
public partial class VariantEditViewModel : BaseEditViewModel<Models.Variant>
{
    #region Private Fields

    private readonly IVariantRepository _variantRepository;

    #endregion

    #region Required Base Class Overrides (Fallback for Enhanced Constructor)

    protected override string GetEntityName() => "Variant";
    protected override string GetEntityNamePlural() => "Variants";

    #endregion

    #region UI Properties - EXACTLY like Family

    /// <summary>
    /// Controls visibility of Save and Add Another button - only show in CREATE mode
    /// </summary>
    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor - Using Enhanced Base Constructor

    /// <summary>
    /// Initializes the variant edit ViewModel using enhanced base constructor.
    /// Automatically sets up all base functionality without relationship management.
    /// </summary>
    public VariantEditViewModel(IVariantRepository variantRepository, INavigationService navigationService)
        : base(variantRepository, navigationService, "Variant", "Variants") // Enhanced constructor!
    {
        _variantRepository = variantRepository;
        this.LogInfo("Initialized - using enhanced base functionality");
    }

    #endregion

    #region Variant-Specific Delete with Validation

    /// <summary>
    /// Delete command with simple confirmation (variants have no dependencies)
    /// </summary>
    [RelayCommand]
    public async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            // Simple confirmation for variants (no dependencies to check)
            string message = "Are you sure you want to delete this variant?";

            // Use base method for confirmation
            var confirmed = await this.ShowConfirmation("Delete Variant", message, "Delete", "Cancel");
            if (!confirmed) return;

            // Use base repository for deletion
            var success = await _variantRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Variant deleted successfully");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete variant");
            }
        }, "Delete Variant");
    }

    #endregion

    // ALL OTHER FUNCTIONALITY INHERITED AUTOMATICALLY:
    // ✅ Name, Description properties with validation
    // ✅ IsActive, IsFavorite toggles
    // ✅ Save, Cancel, Delete commands
    // ✅ Save & Continue functionality
    // ✅ Form state management
    // ✅ Validation with debouncing
    // ✅ Loading states and error handling
    // ✅ Navigation management
    // ✅ Messaging and notifications
    // ✅ Connection status monitoring
    // ✅ Performance optimizations
}