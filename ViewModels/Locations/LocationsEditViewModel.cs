using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Services.Localization;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Locations;

/// <summary>
/// LocationsEditViewModel - Atualizado com sistema de localização
/// </summary>
public partial class LocationsEditViewModel : BaseEditViewModel<PlantLocation>
{
    #region Private Fields

    private readonly ILocationRepository _locationRepository;
    private readonly IFieldOptionsService _fieldOptionsService;
    private readonly ILocalizationService _localizationService;

    #endregion

    #region Required Base Class Overrides

    protected override string GetEntityName() => "Location";
    protected override string GetEntityNamePlural() => "Locations";

    #endregion

    #region Observable Properties (Specific Fields)

    [ObservableProperty]
    private string environmentNotes = "";

    #endregion

    #region Key Properties (for database storage)

    [ObservableProperty]
    private string locationTypeKey = ""; // Chave para salvar no banco

    #endregion

    #region Compatibility Properties for XAML Binding

    /// <summary>
    /// Propriedade compatível para XAML - mapeia para LocationTypeKey
    /// </summary>
    [ObservableProperty]
    private string locationType = "";

    // Handler para sincronizar display com key
    partial void OnLocationTypeChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            // Converter display para key
            var newKey = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetLocationTypeKeys());
            if (LocationTypeKey != newKey)
            {
                LocationTypeKey = newKey;
                this.LogInfo($"[OnLocationTypeChanged] Display: '{value}' -> Key: '{newKey}'");
            }
        }
    }

    // Handler para sincronizar key com display
    partial void OnLocationTypeKeyChanged(string value)
    {
        var display = string.IsNullOrEmpty(value) ? "" : _fieldOptionsService.GetDisplayForKey(value);
        if (LocationType != display)
        {
            LocationType = display;
        }
        OnPropertyChanged(nameof(DisplayLocationType));
    }

    #endregion

    #region Display Properties for UI

    public string DisplayLocationType =>
        string.IsNullOrEmpty(LocationTypeKey) ? "" :
        _localizationService.GetString(LocationTypeKey);

    #endregion

    #region Field Options Properties

    public List<string> AvailableLocationTypeKeys => _fieldOptionsService.GetLocationTypeKeys();
    public List<string> AvailableLocationTypes => _fieldOptionsService.GetLocationTypeOptions();

    #endregion

    #region UI Properties

    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor

    public LocationsEditViewModel(
        ILocationRepository locationRepository,
        INavigationService navigationService,
        IFieldOptionsService fieldOptionsService,
        ILocalizationService localizationService)
        : base(locationRepository, navigationService, "Location", "Locations")
    {
        _locationRepository = locationRepository;
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
            this.LogInfo("Applying query attributes for Location edit");

            if (query.TryGetValue("LocationId", out var locationIdObj) &&
                Guid.TryParse(locationIdObj?.ToString(), out var locationId))
            {
                _ = InitializeForEditAsync(locationId);
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

    public async Task InitializeForEditAsync(Guid locationId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {locationId}");

            EntityId = locationId;
            _isEditMode = true;

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(ShowSaveAndContinue));

            await LoadEntityAsync();
            this.LogInfo("Edit mode initialization completed");
        }, "Initialize for Edit");
    }

    #endregion

    #region Location-Specific Delete

    [RelayCommand]
    public async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            var confirmTitle = _localizationService.GetString("Confirm.DeleteLocation.Title");
            var confirmMessage = _localizationService.GetString("Confirm.DeleteLocation.Message");
            var deleteButton = _localizationService.GetString("Button.Delete");
            var cancelButton = _localizationService.GetString("Button.Cancel");

            var confirmed = await this.ShowConfirmation(confirmTitle, confirmMessage, deleteButton, cancelButton);
            if (!confirmed) return;

            var success = await _locationRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                var successMessage = _localizationService.GetString("Message.LocationDeleted");
                await this.ShowSuccessToast(successMessage);
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete location");
            }
        }, "Delete Location");
    }

    #endregion

    #region Base Class Overrides - Enhanced Tracking

    /// <summary>
    /// Override to include location-specific properties in unsaved changes tracking
    /// </summary>
    protected override bool IsTrackedProperty(string? propertyName)
    {
        return base.IsTrackedProperty(propertyName) || propertyName is
            nameof(LocationType) or nameof(EnvironmentNotes) or nameof(LocationTypeKey);
    }

    #endregion

    #region Entity Mapping - Usando chaves para campos predefinidos

    /// <summary>
    /// Override entity preparation to include location-specific fields
    /// </summary>
    protected override void PrepareEntitySpecificFields(PlantLocation entity)
    {
        entity.LocationType = LocationTypeKey; // Salva chave no banco
        entity.EnvironmentNotes = string.IsNullOrWhiteSpace(EnvironmentNotes) ? null : EnvironmentNotes.Trim();

        this.LogInfo($"[PrepareEntitySpecificFields] LocationTypeKey: '{LocationTypeKey}'");
        this.LogInfo($"[PrepareEntitySpecificFields] LocationType Display: '{LocationType}'");
    }

    /// <summary>
    /// Override entity population to include location-specific fields
    /// </summary>
    protected override async Task PopulateEntitySpecificFieldsAsync(PlantLocation entity)
    {
        await ExecuteWithAllSuppressionsEnabledAsync(async () =>
        {
            LocationTypeKey = entity.LocationType ?? ""; // Carrega chave do banco
            EnvironmentNotes = entity.EnvironmentNotes ?? "";

            // Sincronizar propriedade de display para XAML
            LocationType = string.IsNullOrEmpty(LocationTypeKey) ? "" : _fieldOptionsService.GetDisplayForKey(LocationTypeKey);

            // Notifica propriedade de display
            OnPropertyChanged(nameof(DisplayLocationType));

            this.LogInfo($"[PopulateEntitySpecificFieldsAsync] LocationTypeKey: '{LocationTypeKey}'");
            this.LogInfo($"[PopulateEntitySpecificFieldsAsync] LocationType Display: '{LocationType}'");
        });
    }

    #endregion
}