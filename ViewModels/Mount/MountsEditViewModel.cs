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
    private readonly ILocalizationService _localizationService;

    #endregion

    #region Required Base Class Overrides

    protected override string GetEntityName() => "Mount";
    protected override string GetEntityNamePlural() => "Mounts";

    #endregion

    #region Key Properties (for database storage)

    [ObservableProperty]
    private string materialKey = ""; // Chave para salvar no banco

    [ObservableProperty]
    private string sizeKey = ""; // Chave para salvar no banco

    [ObservableProperty]
    private string drainageTypeKey = ""; // Chave para salvar no banco

    #endregion

    #region Compatibility Properties for XAML Binding

    /// <summary>
    /// Propriedade compatível para XAML - mapeia para MaterialKey
    /// </summary>
    [ObservableProperty]
    private string material = "";

    /// <summary>
    /// Propriedade compatível para XAML - mapeia para SizeKey
    /// </summary>
    [ObservableProperty]
    private string size = "";

    /// <summary>
    /// Propriedade compatível para XAML - mapeia para DrainageTypeKey
    /// </summary>
    [ObservableProperty]
    private string drainageType = "";

    // Handlers para sincronizar displays com keys
    partial void OnMaterialChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var newKey = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetMountMaterialKeys());
            if (MaterialKey != newKey)
            {
                MaterialKey = newKey;
                this.LogInfo($"[OnMaterialChanged] Display: '{value}' -> Key: '{newKey}'");
            }
        }
    }

    partial void OnSizeChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var newKey = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetMountSizeKeys());
            if (SizeKey != newKey)
            {
                SizeKey = newKey;
                this.LogInfo($"[OnSizeChanged] Display: '{value}' -> Key: '{newKey}'");
            }
        }
    }

    partial void OnDrainageTypeChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var newKey = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetDrainageTypeKeys());
            if (DrainageTypeKey != newKey)
            {
                DrainageTypeKey = newKey;
                this.LogInfo($"[OnDrainageTypeChanged] Display: '{value}' -> Key: '{newKey}'");
            }
        }
    }

    // Handlers para sincronizar keys com displays
    partial void OnMaterialKeyChanged(string value)
    {
        var display = string.IsNullOrEmpty(value) ? "" : _fieldOptionsService.GetDisplayForKey(value);
        if (Material != display)
        {
            Material = display;
        }
        OnPropertyChanged(nameof(DisplayMaterial));
    }

    partial void OnSizeKeyChanged(string value)
    {
        var display = string.IsNullOrEmpty(value) ? "" : _fieldOptionsService.GetDisplayForKey(value);
        if (Size != display)
        {
            Size = display;
        }
        OnPropertyChanged(nameof(DisplaySize));
    }

    partial void OnDrainageTypeKeyChanged(string value)
    {
        var display = string.IsNullOrEmpty(value) ? "" : _fieldOptionsService.GetDisplayForKey(value);
        if (DrainageType != display)
        {
            DrainageType = display;
        }
        OnPropertyChanged(nameof(DisplayDrainageType));
    }

    #endregion

    #region Display Properties for UI

    public string DisplayMaterial =>
        string.IsNullOrEmpty(MaterialKey) ? "" :
        _localizationService.GetString(MaterialKey);

    public string DisplaySize =>
        string.IsNullOrEmpty(SizeKey) ? "" :
        _localizationService.GetString(SizeKey);

    public string DisplayDrainageType =>
        string.IsNullOrEmpty(DrainageTypeKey) ? "" :
        _localizationService.GetString(DrainageTypeKey);

    #endregion

    #region Field Options Properties

    public List<string> AvailableMaterialKeys => _fieldOptionsService.GetMountMaterialKeys();
    public List<string> AvailableSizeKeys => _fieldOptionsService.GetMountSizeKeys();
    public List<string> AvailableDrainageTypeKeys => _fieldOptionsService.GetDrainageTypeKeys();

    public List<string> AvailableMaterials => _fieldOptionsService.GetMountMaterialOptions();
    public List<string> AvailableSizes => _fieldOptionsService.GetMountSizeOptions();
    public List<string> AvailableDrainageTypes => _fieldOptionsService.GetDrainageTypeOptions();

    #endregion

    #region UI Properties

    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor

    public MountsEditViewModel(
        IMountRepository mountRepository,
        INavigationService navigationService,
        IFieldOptionsService fieldOptionsService,
        ILocalizationService localizationService)
        : base(mountRepository, navigationService, "Mount", "Mounts")
    {
        _mountRepository = mountRepository;
        _fieldOptionsService = fieldOptionsService;
        _localizationService = localizationService;

        LoadFieldOptions();
        this.LogInfo("Initialized - using enhanced base functionality with localization");
    }

    #endregion

    #region Field Options Methods

    private void LoadFieldOptions()
    {
        // As propriedades são computed, não precisam ser carregadas
        this.LogInfo("Field options are computed properties - no loading needed");
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
            var confirmTitle = _localizationService.GetString("Confirm.DeleteMount.Title");
            var confirmMessage = _localizationService.GetString("Confirm.DeleteMount.Message");
            var deleteButton = _localizationService.GetString("Button.Delete");
            var cancelButton = _localizationService.GetString("Button.Cancel");

            var confirmed = await this.ShowConfirmation(confirmTitle, confirmMessage, deleteButton, cancelButton);
            if (!confirmed) return;

            var success = await _mountRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                var successMessage = _localizationService.GetString("Message.MountDeleted");
                await this.ShowSuccessToast(successMessage);
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
            nameof(Material) or nameof(Size) or nameof(DrainageType) or
            nameof(MaterialKey) or nameof(SizeKey) or nameof(DrainageTypeKey);
    }

    #endregion

    #region Entity Mapping - Usando chaves para campos predefinidos

    /// <summary>
    /// Override entity preparation to include mount-specific fields
    /// </summary>
    protected override void PrepareEntitySpecificFields(Mount entity)
    {
        entity.Material = MaterialKey; // Salva chave no banco
        entity.Size = SizeKey; // Salva chave no banco
        entity.DrainageType = DrainageTypeKey; // Salva chave no banco

        this.LogInfo($"[PrepareEntitySpecificFields] MaterialKey: '{MaterialKey}', SizeKey: '{SizeKey}', DrainageTypeKey: '{DrainageTypeKey}'");
        this.LogInfo($"[PrepareEntitySpecificFields] Material Display: '{Material}', Size Display: '{Size}', DrainageType Display: '{DrainageType}'");
    }

    /// <summary>
    /// Override entity population to include mount-specific fields
    /// </summary>
    protected override async Task PopulateEntitySpecificFieldsAsync(Mount entity)
    {
        await ExecuteWithAllSuppressionsEnabledAsync(async () =>
        {
            MaterialKey = entity.Material ?? ""; // Carrega chave do banco
            SizeKey = entity.Size ?? ""; // Carrega chave do banco
            DrainageTypeKey = entity.DrainageType ?? ""; // Carrega chave do banco

            // Sincronizar propriedades de display para XAML
            Material = string.IsNullOrEmpty(MaterialKey) ? "" : _fieldOptionsService.GetDisplayForKey(MaterialKey);
            Size = string.IsNullOrEmpty(SizeKey) ? "" : _fieldOptionsService.GetDisplayForKey(SizeKey);
            DrainageType = string.IsNullOrEmpty(DrainageTypeKey) ? "" : _fieldOptionsService.GetDisplayForKey(DrainageTypeKey);

            // Notifica propriedades de display
            OnPropertyChanged(nameof(DisplayMaterial));
            OnPropertyChanged(nameof(DisplaySize));
            OnPropertyChanged(nameof(DisplayDrainageType));

            this.LogInfo($"[PopulateEntitySpecificFieldsAsync] MaterialKey: '{MaterialKey}', SizeKey: '{SizeKey}', DrainageTypeKey: '{DrainageTypeKey}'");
            this.LogInfo($"[PopulateEntitySpecificFieldsAsync] Material Display: '{Material}', Size Display: '{Size}', DrainageType Display: '{DrainageType}'");
        });
    }

    #endregion
}