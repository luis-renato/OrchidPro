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
/// MINIMAL Family edit ViewModel - Copy of Species pattern that works
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

    #region Constructor - EXACTLY like Species

    public FamilyEditViewModel(IFamilyRepository familyRepository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(familyRepository, navigationService, "Family", "Families") // Enhanced constructor!
    {
        _familyRepository = familyRepository;
        _genusRepository = genusRepository;

        this.LogInfo("Initialized - using enhanced base functionality");
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