using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Services.Localization;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Mounts;

public partial class MountsEditViewModel : BaseEditViewModel<Mount>
{
    #region Private Fields

    private readonly IMountRepository _mountRepository;
    private readonly IFieldOptionsService _fieldOptionsService;

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

    #region Field Options Properties

    [ObservableProperty]
    private List<string> availableMaterials = new();

    [ObservableProperty]
    private List<string> availableSizes = new();

    [ObservableProperty]
    private List<string> availableDrainageTypes = new();

    #endregion

    #region UI Properties

    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor

    public MountsEditViewModel(
        IMountRepository mountRepository,
        INavigationService navigationService,
        IFieldOptionsService fieldOptionsService)
        : base(mountRepository, navigationService, "Mount", "Mounts")
    {
        _mountRepository = mountRepository;
        _fieldOptionsService = fieldOptionsService;

        LoadFieldOptions();
        this.LogInfo("Initialized - using enhanced base functionality");
    }

    #endregion

    #region Field Options Methods

    private void LoadFieldOptions()
    {
        AvailableMaterials = _fieldOptionsService.GetMountMaterialOptions();
        AvailableSizes = _fieldOptionsService.GetMountSizeOptions();
        AvailableDrainageTypes = _fieldOptionsService.GetDrainageTypeOptions();
    }

    #endregion

    #region Navigation Parameter Handling

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Applying query attributes for Mount edit");

            if (query.TryGetValue("MountId", out var mountIdObj) &&
                Guid.TryParse(mountIdObj?.ToString(), out var mountId))
            {
                _ = InitializeForEditAsync(mountId);
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

    public async Task InitializeForEditAsync(Guid mountId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {mountId}");

            EntityId = mountId;
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
                "Are you sure you want to delete this mount?", "Delete", "Cancel");
            if (!confirmed) return;

            var success = await _mountRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Mount deleted successfully");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete mount");
            }
        }, "Delete Mount");
    }

    #endregion

    #region Base Class Overrides - Enhanced Tracking

    /// <summary>
    /// Override to include mount-specific properties in unsaved changes tracking
    /// </summary>
    protected override bool IsTrackedProperty(string? propertyName)
    {
        return base.IsTrackedProperty(propertyName) || propertyName is
            nameof(Material) or nameof(Size) or nameof(DrainageType);
    }

    #endregion

    #region Entity Mapping - Seguindo padrão SpeciesEditViewModel

    /// <summary>
    /// Override entity preparation to include mount-specific fields
    /// </summary>
    protected override void PrepareEntitySpecificFields(Mount entity)
    {
        entity.Material = string.IsNullOrWhiteSpace(Material) ? null : Material.Trim();
        entity.Size = string.IsNullOrWhiteSpace(Size) ? null : Size.Trim();
        entity.DrainageType = string.IsNullOrWhiteSpace(DrainageType) ? null : DrainageType.Trim();
    }

    /// <summary>
    /// Override entity population to include mount-specific fields
    /// </summary>
    protected override async Task PopulateEntitySpecificFieldsAsync(Mount entity)
    {
        await ExecuteWithAllSuppressionsEnabledAsync(async () =>
        {
            Material = entity.Material ?? "";
            Size = entity.Size ?? "";
            DrainageType = entity.DrainageType ?? "";
        });
    }

    #endregion

}