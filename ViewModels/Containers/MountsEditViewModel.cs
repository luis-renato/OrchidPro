using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Mounts;

public partial class MountsEditViewModel : BaseEditViewModel<Mount>
{
    #region Private Fields

    private readonly IMountRepository _MountRepository;

    #endregion

    #region Required Base Class Overrides

    protected override string GetEntityName() => "Mount";
    protected override string GetEntityNamePlural() => "Mounts";

    #endregion

    #region Observable Properties (Specific Fields)

    [ObservableProperty]
    private string material = "";

    [ObservableProperty]
    private string size = "";

    [ObservableProperty]
    private string drainageType = "";

    #endregion

    #region UI Properties

    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor

    public MountsEditViewModel(IMountRepository MountRepository, INavigationService navigationService)
        : base(MountRepository, navigationService, "Mount", "Mounts")
    {
        _MountRepository = MountRepository;
        this.LogInfo("Initialized - using enhanced base functionality");
    }

    #endregion

    #region Navigation Parameter Handling

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Applying query attributes for Mount edit");

            if (query.TryGetValue("MountId", out var MountIdObj) &&
                Guid.TryParse(MountIdObj?.ToString(), out var MountId))
            {
                _ = InitializeForEditAsync(MountId);
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

    public async Task InitializeForEditAsync(Guid MountId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {MountId}");

            EntityId = MountId;
            _isEditMode = true;

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(ShowSaveAndContinue));

            await LoadEntityAsync();
            this.LogInfo("Edit mode initialization completed");
        }, "Initialize for Edit");
    }

    #endregion

    #region Mount-Specific Delete

    [RelayCommand]
    public async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            var confirmed = await this.ShowConfirmation("Delete Mount",
                "Are you sure you want to delete this Mount?", "Delete", "Cancel");
            if (!confirmed) return;

            var success = await _MountRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Mount deleted successfully");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete Mount");
            }
        }, "Delete Mount");
    }

    #endregion
}
