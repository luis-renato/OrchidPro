using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// FIXED Family edit ViewModel - Copy of Species pattern that works
/// </summary>
public partial class FamilyEditViewModel : BaseEditViewModel<Family>
{
    #region Private Fields

    private readonly IFamilyRepository _familyRepository;
    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Required Base Class Overrides

    protected override string GetEntityName() => "Family";
    protected override string GetEntityNamePlural() => "Families";

    #endregion

    #region UI Properties - EXACTLY like Species

    /// <summary>
    /// Controls visibility of Save & Add Another button - only show in CREATE mode
    /// </summary>
    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor - EXACTLY like Species

    public FamilyEditViewModel(IFamilyRepository familyRepository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(familyRepository, navigationService, "Family", "Families") // Enhanced constructor!
    {
        _familyRepository = familyRepository;
        _genusRepository = genusRepository;

        this.LogInfo("Initialized - using enhanced base functionality");
    }

    #endregion

    #region Navigation Parameter Handling - EXACTLY like Species

    /// <summary>
    /// Enhanced navigation parameter handling for family-specific scenarios - EXACTLY like Species
    /// </summary>
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Applying query attributes for Family edit");

            if (query.TryGetValue("FamilyId", out var familyIdObj) &&
                Guid.TryParse(familyIdObj?.ToString(), out var familyId))
            {
                // Use family-specific initialization for edit mode - EXACTLY like Species
                _ = InitializeForEditAsync(familyId);
            }
            else
            {
                // Use family-specific initialization for create mode
                _ = InitializeForCreateAsync();
            }
        }, "Apply Query Attributes");
    }

    #endregion

    #region Initialization Methods - EXACTLY like Species

    /// <summary>
    /// Initialize for creating new family - EXACTLY like Species
    /// </summary>
    public async Task InitializeForCreateAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Initializing for create mode");

            HasUnsavedChanges = false;
            OnPropertyChanged(nameof(ShowSaveAndContinue)); // Show Save & Add Another in create mode
            this.LogInfo("Create mode initialization completed - form is clean");
        }, "Initialize for Create");
    }

    /// <summary>
    /// Initialize for editing existing family - EXACTLY like Species
    /// </summary>
    public async Task InitializeForEditAsync(Guid familyId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {familyId}");

            // Set entity ID and edit mode FIRST, then load data via base class
            EntityId = familyId;
            _isEditMode = true;

            // Force update of computed properties that depend on IsEditMode
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(ShowSaveAndContinue)); // Hide Save & Add Another in edit mode

            // Load entity data using base class method
            await LoadEntityAsync();

            this.LogInfo("Edit mode initialization completed");
        }, "Initialize for Edit");
    }

    #endregion

    #region Override Save Success for Messaging - EXACTLY like Species

    /// <summary>
    /// Override save success to send family-specific messages - EXACTLY like Species
    /// </summary>
    protected override async Task OnEntitySavedAsync(Family savedEntity)
    {
        if (!IsEditMode)
        {
            // Send family created message for genus auto-selection
            WeakReferenceMessenger.Default.Send(new FamilyCreatedMessage(savedEntity.Id, savedEntity.Name));
            this.LogInfo($"Sent FamilyCreatedMessage for: {savedEntity.Name}");
        }

        await base.OnEntitySavedAsync(savedEntity);
    }

    #endregion

    #region Family-Specific Delete with Validation

    /// <summary>
    /// Override the base delete to add genus count validation
    /// Uses the existing DeleteCommand from base, just overrides the execution
    /// </summary>
    [RelayCommand]
    public async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            // Check for dependent genera
            var genusCount = await _genusRepository.GetCountByFamilyAsync(EntityId.Value, includeInactive: true);
            string message = genusCount > 0
                ? $"This family has {genusCount} genera. Delete anyway?"
                : "Are you sure you want to delete this family?";

            // Use base method for confirmation
            var confirmed = await this.ShowConfirmation("Delete Family", message, "Delete", "Cancel");
            if (!confirmed) return;

            // Use base repository for deletion
            var success = await _familyRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Family deleted successfully");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete family");
            }
        }, "Delete Family");
    }

    /// <summary>
    /// Expose the validation delete command for XAML binding
    /// The RelayCommand attribute automatically creates DeleteWithValidationCommand
    /// </summary>

    #endregion
}