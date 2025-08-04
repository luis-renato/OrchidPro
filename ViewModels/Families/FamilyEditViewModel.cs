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
/// ViewModel for editing and creating botanical family records.
/// Provides family-specific functionality while leveraging enhanced base edit operations.
/// </summary>
public partial class FamilyEditViewModel : BaseEditViewModel<Family>, IQueryAttributable
{
    #region Private Fields

    private readonly IGenusRepository _genusRepository;
    private int _relatedGeneraCount = 0;

    #endregion

    #region Properties

    public int RelatedGeneraCount
    {
        get => _relatedGeneraCount;
        private set => SetProperty(ref _relatedGeneraCount, value);
    }

    public string RelatedGeneraDisplay => RelatedGeneraCount switch
    {
        0 => "No related genera",
        1 => "1 related genus",
        _ => $"{RelatedGeneraCount} related genera"
    };

    public bool CanDelete => IsEditMode && EntityId.HasValue;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";

    #endregion

    #region Page Title Management

    public new string PageTitle => IsEditMode ? "Edit Family" : "New Family";
    public bool IsEditMode => _isEditMode;

    #endregion

    #region Constructor

    public FamilyEditViewModel(IFamilyRepository familyRepository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        _genusRepository = genusRepository;
        this.LogInfo("Initialized with genus repository for delete validation");
    }

    #endregion

    #region Delete Operations

    [RelayCommand]
    private async Task DeleteWithValidationAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!EntityId.HasValue) return;

            var genusCount = await _genusRepository.GetCountByFamilyAsync(EntityId.Value, includeInactive: true);

            bool confirmed;
            if (genusCount > 0)
            {
                var genusText = genusCount == 1 ? "genus" : "genera";
                confirmed = await ShowConfirmationAsync("Delete Family with Genera",
                    $"This family has {genusCount} {genusText}. Deleting will also remove all {genusText}. Continue?",
                    "Delete", "Cancel");
            }
            else
            {
                confirmed = await ShowConfirmationAsync("Delete Family", $"Are you sure you want to delete '{Name}'?", "Delete", "Cancel");
            }

            if (confirmed)
            {
                await _repository.DeleteAsync(EntityId.Value);
                await ShowSuccessAsync("Deleted", $"{EntityName} deleted successfully");
                await NavigateBackAsync();
            }

        }, $"DeleteWithValidation failed for {Name}");
    }

    #endregion

    #region Save Operations Override

    [RelayCommand]
    private async Task SaveWithMessagingAsync()
    {
        var wasCreateOperation = !IsEditMode;
        var familyName = Name;

        await SaveCommand.ExecuteAsync(null);

        if (wasCreateOperation && EntityId.HasValue)
        {
            await this.SafeExecuteAsync(async () =>
            {
                var message = new FamilyCreatedMessage(EntityId.Value, familyName);
                WeakReferenceMessenger.Default.Send(message);
                this.LogSuccess($"Sent FamilyCreatedMessage for: {familyName}");
            }, "Send FamilyCreatedMessage");
        }
    }

    #endregion

    #region Query Attributes Handling

    public new void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            base.ApplyQueryAttributes(query);

            if (IsEditMode && EntityId.HasValue)
            {
                _ = LoadGenusCountAsync();
            }

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(CanDelete));

        }, "ApplyQueryAttributes");
    }

    private async Task LoadGenusCountAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            var count = await _genusRepository.GetCountByFamilyAsync(EntityId!.Value, includeInactive: true);
            RelatedGeneraCount = count;
            OnPropertyChanged(nameof(RelatedGeneraDisplay));
        }, "LoadGenusCount");
    }

    #endregion
}