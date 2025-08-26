
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
/// SourcesEditViewModel - CORRIGIDO seguindo padrão FamilyEditViewModel com Field Options
/// </summary>
public partial class SourcesEditViewModel : BaseEditViewModel<Source>
{
    #region Private Fields

    private readonly ISourceRepository _sourceRepository;
    private readonly IFieldOptionsService _fieldOptionsService;

    #endregion

    #region Required Base Class Overrides - IGUAL ao Family

    protected override string GetEntityName() => "Source";
    protected override string GetEntityNamePlural() => "Sources";

    #endregion

    #region Observable Properties (Specific Fields)

    [ObservableProperty]
    private string supplierType = "";

    [ObservableProperty]
    private string contactInfo = "";

    [ObservableProperty]
    private string website = "";

    #endregion

    #region Field Options Properties

    [ObservableProperty]
    private List<string> availableSupplierTypes = [];

    #endregion

    #region UI Properties - IGUAL ao Family

    /// <summary>
    /// Controls visibility of Save and Add Another button - only show in CREATE mode
    /// </summary>
    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor - IGUAL ao Family (usando enhanced constructor)

    public SourcesEditViewModel(
        ISourceRepository sourceRepository,
        INavigationService navigationService,
        IFieldOptionsService fieldOptionsService)
        : base(sourceRepository, navigationService, "Source", "Sources") // Enhanced constructor!
    {
        _sourceRepository = sourceRepository;
        _fieldOptionsService = fieldOptionsService;

        LoadFieldOptions();
        this.LogInfo("Initialized - using enhanced base functionality");
    }

    #endregion

    #region Field Options Methods

    private void LoadFieldOptions()
    {
        AvailableSupplierTypes = _fieldOptionsService.GetSupplierTypeOptions();
    }

    #endregion

    #region Navigation Parameter Handling - IGUAL ao Family

    /// <summary>
    /// Enhanced navigation parameter handling for source-specific scenarios
    /// </summary>
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

    #region Initialization Methods - IGUAL ao Family

    /// <summary>
    /// Initialize for creating new source
    /// </summary>
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

    /// <summary>
    /// Initialize for editing existing source
    /// </summary>
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

    #region Source-Specific Delete with Validation - IGUAL ao Family

    /// <summary>
    /// Override the base delete with simple confirmation
    /// </summary>
    [RelayCommand]
    public async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            var confirmed = await this.ShowConfirmation("Delete Source",
                "Are you sure you want to delete this source?", "Delete", "Cancel");
            if (!confirmed) return;

            var success = await _sourceRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Source deleted successfully");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete source");
            }
        }, "Delete Source");
    }

    #endregion

    #region Entity Mapping - OBRIGATÓRIO para campos específicos

    /// <summary>
    /// Prepara a entidade para save copiando valores do ViewModel
    /// </summary>
    protected override async Task<Source> PrepareEntityForSaveAsync()
    {
        var entity = await base.PrepareEntityForSaveAsync();

        // Copiar campos específicos do ViewModel para a entidade
        entity.SupplierType = this.SupplierType;
        entity.ContactInfo = this.ContactInfo;
        entity.Website = this.Website;

        // Log para debug
        this.LogInfo($"[PrepareEntityForSaveAsync] Prepared - SupplierType: '{entity.SupplierType}', ContactInfo: '{entity.ContactInfo}', Website: '{entity.Website}'");

        return entity;
    }

    /// <summary>
    /// Popula o ViewModel a partir da entidade carregada
    /// </summary>
    protected override async Task PopulateFromEntityAsync(Source entity)
    {
        await base.PopulateFromEntityAsync(entity);

        // Copiar campos específicos da entidade para o ViewModel
        SupplierType = entity.SupplierType ?? "";
        ContactInfo = entity.ContactInfo ?? "";
        Website = entity.Website ?? "";

        // Log para debug
        this.LogInfo($"[PopulateFromEntityAsync] Populated - SupplierType: '{SupplierType}', ContactInfo: '{ContactInfo}', Website: '{Website}'");
    }

    #endregion

    #region Base Class Overrides - Enhanced Tracking

    /// <summary>
    /// Override to include source-specific properties in unsaved changes tracking
    /// </summary>
    protected override bool IsTrackedProperty(string? propertyName)
    {
        return base.IsTrackedProperty(propertyName) || propertyName is
            nameof(SupplierType) or nameof(ContactInfo) or nameof(Website);
    }

    #endregion
    #region Entity Mapping - Seguindo padrão SpeciesEditViewModel

    /// <summary>
    /// Override entity preparation to include source-specific fields
    /// </summary>
    protected override void PrepareEntitySpecificFields(Source entity)
    {
        entity.SupplierType = string.IsNullOrWhiteSpace(SupplierType) ? null : SupplierType.Trim();
        entity.ContactInfo = string.IsNullOrWhiteSpace(ContactInfo) ? null : ContactInfo.Trim();
        entity.Website = string.IsNullOrWhiteSpace(Website) ? null : Website.Trim();

        // Log para debug
        this.LogInfo($"[PrepareEntitySpecificFields] Prepared - SupplierType: '{entity.SupplierType}', ContactInfo: '{entity.ContactInfo}', Website: '{entity.Website}'");
    }

    /// <summary>
    /// Override entity population to include source-specific fields
    /// </summary>
    protected override async Task PopulateEntitySpecificFieldsAsync(Source entity)
    {
        await ExecuteWithAllSuppressionsEnabledAsync(async () =>
        {
            SupplierType = entity.SupplierType ?? "";
            ContactInfo = entity.ContactInfo ?? "";
            Website = entity.Website ?? "";

            // Log para debug
            this.LogInfo($"[PopulateEntitySpecificFieldsAsync] Populated - SupplierType: '{SupplierType}', ContactInfo: '{ContactInfo}', Website: '{Website}'");
        });
    }

    #endregion
}