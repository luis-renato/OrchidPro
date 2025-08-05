using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// MINIMAL Family edit ViewModel - uses BaseEditViewModel SaveCommand + only messaging override
/// </summary>
public partial class FamilyEditViewModel : BaseEditViewModel<Family>
{
    #region Private Fields

    private readonly IFamilyRepository _familyRepository;
    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";

    #endregion

    #region Constructor

    public FamilyEditViewModel(IFamilyRepository familyRepository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        _familyRepository = familyRepository;
        _genusRepository = genusRepository;

        // Only delete command is family-specific
        DeleteWithValidationCommand = new AsyncRelayCommand(DeleteFamilyAsync, () => CanDelete);

        this.LogInfo("Initialized - using base SaveCommand + delete validation");
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand DeleteWithValidationCommand { get; }

    #endregion

    #region Override Save Success for Messaging

    /// <summary>
    /// ✅ CORRETO: Override OnSaveSuccessAsync para enviar mensagem
    /// </summary>
    protected override async Task OnSaveSuccessAsync(Family savedEntity)
    {
        await base.OnSaveSuccessAsync(savedEntity);

        // Send family created message if it was a new family
        if (!IsEditMode)
        {
            SendFamilyCreatedMessage(savedEntity.Name);
        }
    }

    #endregion

    #region Family-Specific Operations

    /// <summary>
    /// Delete family with genus count validation
    /// </summary>
    private async Task DeleteFamilyAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            // Use GetCountByFamilyAsync (exists in IGenusRepository)
            var genusCount = await _genusRepository.GetCountByFamilyAsync(EntityId.Value, includeInactive: true);

            string message = genusCount > 0
                ? $"This family has {genusCount} genera. Delete anyway?"
                : "Are you sure you want to delete this family?";

            // Use ShowConfirmationAsync from BaseEditViewModel
            var confirmed = await ShowConfirmationAsync("Delete Family", message, "Delete", "Cancel");
            if (!confirmed) return;

            var success = await _familyRepository.DeleteAsync(EntityId.Value);
            if (success)
            {
                await ShowSuccessAsync("Success", "Family deleted successfully");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await ShowErrorAsync("Error", "Failed to delete family");
            }
        }, "Delete Family");
    }

    #endregion

    #region Property Overrides

    /// <summary>
    /// Can delete - only in edit mode with valid entity
    /// </summary>
    public bool CanDelete => IsEditMode && !IsBusy && EntityId.HasValue;

    #endregion

    #region Messaging

    /// <summary>
    /// Send family created message after successful save
    /// </summary>
    private void SendFamilyCreatedMessage(string familyName)
    {
        if (EntityId.HasValue && !string.IsNullOrEmpty(familyName))
        {
            var message = new FamilyCreatedMessage(EntityId.Value, familyName);
            WeakReferenceMessenger.Default.Send(message);
            this.LogInfo($"Sent FamilyCreatedMessage for: {familyName}");
        }
    }

    #endregion
}