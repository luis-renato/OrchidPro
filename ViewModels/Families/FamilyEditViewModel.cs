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
/// MINIMAL Family edit ViewModel - reduced from ~200 lines to essential code only!
/// All common functionality moved to BaseEditViewModel and pattern classes.
/// </summary>
public partial class FamilyEditViewModel : BaseEditViewModel<Family>, IQueryAttributable
{
    #region Private Fields

    private readonly IGenusRepository _genusRepository;
    private int _relatedGeneraCount = 0;

    #endregion

    #region Family-Specific Properties

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

    #region Constructor

    public FamilyEditViewModel(IFamilyRepository familyRepository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        _genusRepository = genusRepository;
        this.LogInfo("Initialized with genus repository for delete validation");
    }

    #endregion

    #region Delete Operations Using Base Pattern

    [RelayCommand]
    private async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await BaseDeleteOperations.ExecuteHierarchicalDeleteAsync<Family, Genus, Family>(
            new Family { Id = EntityId.Value, Name = Name },
            (IBaseRepository<Family>)_repository,
            _genusRepository,
            EntityName,
            "genus",
            "genera",
            new List<Family>(), // Not used for single delete
            () => { }, // Not used for single delete
            async (title, message) => await ShowConfirmationAsync(title, message, "Delete", "Cancel"),
            async (message) =>
            {
                await ShowSuccessAsync("Deleted", $"{EntityName} deleted successfully");
                await NavigateBackAsync();
            });
    }

    #endregion

    #region Save Operations with Messaging

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