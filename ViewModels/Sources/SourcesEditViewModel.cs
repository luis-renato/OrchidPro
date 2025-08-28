using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Services.Localization;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Sources;

/// <summary>
/// SourcesEditViewModel - Atualizado com sistema de localização
/// </summary>
public partial class SourcesEditViewModel : BaseEditViewModel<Source>
{
    #region Private Fields

    private readonly ISourceRepository _sourceRepository;
    private readonly IFieldOptionsService _fieldOptionsService;
    private readonly ILocalizationService _localizationService;

    #endregion

    #region Required Base Class Overrides

    protected override string GetEntityName() => "Source";
    protected override string GetEntityNamePlural() => "Sources";

    #endregion

    #region Observable Properties (Specific Fields)

    [ObservableProperty]
    private string contactInfo = "";

    [ObservableProperty]
    private string website = "";

    #endregion

    #region Key Properties (for database storage)

    [ObservableProperty]
    private string supplierTypeKey = ""; // Chave para salvar no banco

    #endregion

    #region Compatibility Properties for XAML Binding

    /// <summary>
    /// Propriedade compatível para XAML - mapeia para SupplierTypeKey
    /// </summary>
    [ObservableProperty]
    private string supplierType = "";

    // Handler para sincronizar display com key
    partial void OnSupplierTypeChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            // Converter display para key
            var newKey = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetSupplierTypeKeys());
            if (SupplierTypeKey != newKey)
            {
                SupplierTypeKey = newKey;
                this.LogInfo($"[OnSupplierTypeChanged] Display: '{value}' -> Key: '{newKey}'");
            }
        }
    }

    // Handler para sincronizar key com display
    partial void OnSupplierTypeKeyChanged(string value)
    {
        var display = string.IsNullOrEmpty(value) ? "" : _fieldOptionsService.GetDisplayForKey(value);
        if (SupplierType != display)
        {
            SupplierType = display;
        }
        OnPropertyChanged(nameof(DisplaySupplierType));
    }

    #endregion

    #region Display Properties for UI

    public string DisplaySupplierType =>
        string.IsNullOrEmpty(SupplierTypeKey) ? "" :
        _localizationService.GetString(SupplierTypeKey);

    #endregion

    #region Field Options Properties

    public List<string> AvailableSupplierTypeKeys => _fieldOptionsService.GetSupplierTypeKeys();
    public List<string> AvailableSupplierTypes => _fieldOptionsService.GetSupplierTypeOptions();

    #endregion

    #region UI Properties

    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor

    public SourcesEditViewModel(
        ISourceRepository sourceRepository,
        INavigationService navigationService,
        IFieldOptionsService fieldOptionsService,
        ILocalizationService localizationService)
        : base(sourceRepository, navigationService, "Source", "Sources")
    {
        _sourceRepository = sourceRepository;
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
            this.LogInfo("Applying query attributes for Source edit");

            if (query.TryGetValue("SourceId", out var sourceIdObj) &&
                Guid.TryParse(sourceIdObj?.ToString(), out var sourceId))
            {
                _ = InitializeForEditAsync(sourceId);
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

    public async Task InitializeForEditAsync(Guid sourceId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {sourceId}");

            EntityId = sourceId;
            _isEditMode = true;

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(ShowSaveAndContinue));

            await LoadEntityAsync();
            this.LogInfo("Edit mode initialization completed");
        }, "Initialize for Edit");
    }

    #endregion

    #region Source-Specific Delete

    [RelayCommand]
    public async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            var confirmTitle = _localizationService.GetString("Confirm.DeleteSource.Title");
            var confirmMessage = _localizationService.GetString("Confirm.DeleteSource.Message");
            var deleteButton = _localizationService.GetString("Button.Delete");
            var cancelButton = _localizationService.GetString("Button.Cancel");

            var confirmed = await this.ShowConfirmation(confirmTitle, confirmMessage, deleteButton, cancelButton);
            if (!confirmed) return;

            var success = await _sourceRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                var successMessage = _localizationService.GetString("Message.SourceDeleted");
                await this.ShowSuccessToast(successMessage);
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete source");
            }
        }, "Delete Source");
    }

    #endregion

    #region Base Class Overrides - Enhanced Tracking

    /// <summary>
    /// Override to include source-specific properties in unsaved changes tracking
    /// </summary>
    protected override bool IsTrackedProperty(string? propertyName)
    {
        return base.IsTrackedProperty(propertyName) || propertyName is
            nameof(SupplierType) or nameof(ContactInfo) or nameof(Website) or nameof(SupplierTypeKey);
    }

    #endregion

    #region Entity Mapping - Usando chaves para campos predefinidos

    /// <summary>
    /// Override entity preparation to include source-specific fields
    /// </summary>
    protected override void PrepareEntitySpecificFields(Source entity)
    {
        entity.SupplierType = SupplierTypeKey; // Salva chave no banco
        entity.ContactInfo = string.IsNullOrWhiteSpace(ContactInfo) ? null : ContactInfo.Trim();
        entity.Website = string.IsNullOrWhiteSpace(Website) ? null : Website.Trim();

        this.LogInfo($"[PrepareEntitySpecificFields] SupplierTypeKey: '{SupplierTypeKey}'");
        this.LogInfo($"[PrepareEntitySpecificFields] SupplierType Display: '{SupplierType}'");
    }

    /// <summary>
    /// Override entity population to include source-specific fields
    /// </summary>
    protected override async Task PopulateEntitySpecificFieldsAsync(Source entity)
    {
        await ExecuteWithAllSuppressionsEnabledAsync(async () =>
        {
            SupplierTypeKey = entity.SupplierType ?? ""; // Carrega chave do banco
            ContactInfo = entity.ContactInfo ?? "";
            Website = entity.Website ?? "";

            // Sincronizar propriedade de display para XAML
            SupplierType = string.IsNullOrEmpty(SupplierTypeKey) ? "" : _fieldOptionsService.GetDisplayForKey(SupplierTypeKey);

            // Notifica propriedade de display
            OnPropertyChanged(nameof(DisplaySupplierType));

            this.LogInfo($"[PopulateEntitySpecificFieldsAsync] SupplierTypeKey: '{SupplierTypeKey}'");
            this.LogInfo($"[PopulateEntitySpecificFieldsAsync] SupplierType Display: '{SupplierType}'");
        });
    }

    #endregion
}