using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Substrates;

public partial class SubstratesEditViewModel : BaseEditViewModel<Substrate>
{
    #region Private Fields

    private readonly ISubstrateRepository _substrateRepository;

    #endregion

    #region Required Base Class Overrides

    protected override string GetEntityName() => "Substrate";
    protected override string GetEntityNamePlural() => "Substrates";

    #endregion

    #region Observable Properties (Specific Fields)

    [ObservableProperty]
    private string components = "";

    [ObservableProperty]
    private string phRange = "";

    [ObservableProperty]
    private string drainageLevel = "";

    [ObservableProperty]
    private string supplier = "";

    #endregion

    #region UI Properties

    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor

    public SubstratesEditViewModel(ISubstrateRepository substrateRepository, INavigationService navigationService)
        : base(substrateRepository, navigationService, "Substrate", "Substrates")
    {
        _substrateRepository = substrateRepository;
        this.LogInfo("Initialized - using enhanced base functionality");
    }

    #endregion

    #region Navigation Parameter Handling

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Applying query attributes for Substrate edit");

            if (query.TryGetValue("SubstrateId", out var substrateIdObj) &&
                Guid.TryParse(substrateIdObj?.ToString(), out var substrateId))
            {
                _ = InitializeForEditAsync(substrateId);
            }
            else
            {
                _ = InitializeForCreateAsync();
            }
        }, "Apply Query Attributes");
    }

    #endregion

    #region Initialization Methods

    public Task InitializeForCreateAsync()
    {
        return this.SafeExecuteAsync(() =>
        {
            this.LogInfo("Initializing for create mode");
            HasUnsavedChanges = false;
            OnPropertyChanged(nameof(ShowSaveAndContinue));
            this.LogInfo("Create mode initialization completed - form is clean");
            return Task.CompletedTask;
        }, "Initialize for Create");
    }

    public async Task InitializeForEditAsync(Guid substrateId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {substrateId}");

            EntityId = substrateId;
            _isEditMode = true;

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(ShowSaveAndContinue));

            await LoadEntityAsync();
            this.LogInfo("Edit mode initialization completed");
        }, "Initialize for Edit");
    }

    #endregion

    #region Substrate-Specific Delete

    [RelayCommand]
    public async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            var confirmed = await this.ShowConfirmation("Delete Substrate",
                "Are you sure you want to delete this substrate?", "Delete", "Cancel");
            if (!confirmed) return;

            var success = await _substrateRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Substrate deleted successfully");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete substrate");
            }
        }, "Delete Substrate");
    }

    #endregion
}