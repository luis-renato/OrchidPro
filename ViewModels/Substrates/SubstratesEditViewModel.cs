using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Services.Localization;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Substrates;

public partial class SubstratesEditViewModel : BaseEditViewModel<Substrate>
{
    private readonly ISubstrateRepository _substrateRepository;
    private readonly IFieldOptionsService _fieldOptionsService;
    private readonly ILocalizationService _localizationService;
    private readonly ILanguageService _languageService;

    #region Entity Properties

    [ObservableProperty]
    private string components = "";

    [ObservableProperty]
    private string supplier = "";

    #endregion

    #region Key Properties (for database)

    [ObservableProperty]
    private string phRangeKey = "";

    [ObservableProperty]
    private string drainageLevelKey = "";

    #endregion

    #region Display Properties (for XAML compatibility)

    [ObservableProperty]
    private string phRange = "";

    [ObservableProperty]
    private string drainageLevel = "";

    #endregion

    #region Field Options Properties

    public List<string> AvailablePhRangeKeys => _fieldOptionsService.GetPhRangeKeys();
    public List<string> AvailableDrainageLevelKeys => _fieldOptionsService.GetDrainageLevelKeys();

    public List<string> AvailablePhRanges => _fieldOptionsService.GetPhRangeOptions();
    public List<string> AvailableDrainageLevels => _fieldOptionsService.GetDrainageLevelOptions();

    #endregion

    #region UI Properties - Matching .resx Keys

    public new string PageTitle => IsEditMode
        ? _localizationService.GetString("Substrates.Page.Edit.Title", "Edit Substrate")
        : _localizationService.GetString("Substrates.Page.Add.Title", "Add Substrate");

    public string NameLabel => _localizationService.GetString("Global.Label.Name", "Name");
    public string DescriptionLabel => _localizationService.GetString("Global.Label.Description", "Description");
    public string ComponentsLabel => _localizationService.GetString("Substrates.Label.Components", "Components");
    public string PhRangeLabel => _localizationService.GetString("Substrates.Label.PhRange", "pH Range");
    public string DrainageLevelLabel => _localizationService.GetString("Substrates.Label.DrainageLevel", "Drainage Level");
    public string SupplierLabel => _localizationService.GetString("Substrates.Label.Supplier", "Supplier");

    public string NamePlaceholder => _localizationService.GetString("Global.Placeholder.EnterName", "Enter name");
    public string DescriptionPlaceholder => _localizationService.GetString("Global.Placeholder.EnterDescription", "Enter description");
    public string ComponentsPlaceholder => _localizationService.GetString("Substrates.Placeholder.EnterComponents", "Enter components");
    public string PhRangePlaceholder => _localizationService.GetString("Substrates.Placeholder.SelectPhRange", "Select pH range");
    public string DrainageLevelPlaceholder => _localizationService.GetString("Substrates.Placeholder.SelectDrainageLevel", "Select drainage level");
    public string SupplierPlaceholder => _localizationService.GetString("Substrates.Placeholder.EnterSupplier", "Enter supplier");

    public new string SaveButtonText => IsEditMode
        ? _localizationService.GetString("Global.Button.Update", "Update")
        : _localizationService.GetString("Global.Button.Create", "Create");

    public string CancelButtonText => _localizationService.GetString("Global.Button.Cancel", "Cancel");
    public string DeleteButtonText => _localizationService.GetString("Global.Button.Delete", "Delete");
    public string SaveAndContinueButtonText => _localizationService.GetString("Global.Button.SaveAndContinue", "Save & Continue");

    public string SaveSuccessMessage => _localizationService.GetString("Substrates.Message.SaveSuccess", "Substrate saved successfully");
    public string DeleteSuccessMessage => _localizationService.GetString("Substrates.Message.DeleteSuccess", "Substrate deleted successfully");
    public string DeleteConfirmationMessage => _localizationService.GetString("Substrates.Message.DeleteConfirmation", "Are you sure you want to delete this substrate?");

    #endregion

    #region Constructor

    public SubstratesEditViewModel(
        ISubstrateRepository substrateRepository,
        INavigationService navigationService,
        IFieldOptionsService fieldOptionsService,
        ILocalizationService localizationService,
        ILanguageService languageService)
        : base(substrateRepository, navigationService, "Substrate", "Substrates")
    {
        _substrateRepository = substrateRepository;
        _fieldOptionsService = fieldOptionsService;
        _localizationService = localizationService;
        _languageService = languageService;

        _languageService.LanguageChanged += OnLanguageChanged;
        _localizationService.LanguageChanged += OnLocalizationChanged;

        this.LogInfo("Initialized with correct .resx localization keys");
    }

    #endregion

    #region Abstract Methods Implementation

    protected override string GetEntityName() => "Substrate";
    protected override string GetEntityNamePlural() => "Substrates";

    #endregion

    #region Language Change Handling

    private void OnLanguageChanged(object? sender, string newLanguage)
    {
        RefreshAllUIProperties();
    }

    private void OnLocalizationChanged(object? sender, EventArgs e)
    {
        RefreshAllUIProperties();
    }

    private void RefreshAllUIProperties()
    {
        OnPropertyChanged(nameof(AvailablePhRanges));
        OnPropertyChanged(nameof(AvailableDrainageLevels));

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(NameLabel));
        OnPropertyChanged(nameof(DescriptionLabel));
        OnPropertyChanged(nameof(ComponentsLabel));
        OnPropertyChanged(nameof(PhRangeLabel));
        OnPropertyChanged(nameof(DrainageLevelLabel));
        OnPropertyChanged(nameof(SupplierLabel));

        OnPropertyChanged(nameof(NamePlaceholder));
        OnPropertyChanged(nameof(DescriptionPlaceholder));
        OnPropertyChanged(nameof(ComponentsPlaceholder));
        OnPropertyChanged(nameof(PhRangePlaceholder));
        OnPropertyChanged(nameof(DrainageLevelPlaceholder));
        OnPropertyChanged(nameof(SupplierPlaceholder));

        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(CancelButtonText));
        OnPropertyChanged(nameof(DeleteButtonText));
        OnPropertyChanged(nameof(SaveAndContinueButtonText));

        OnPropertyChanged(nameof(SaveSuccessMessage));
        OnPropertyChanged(nameof(DeleteSuccessMessage));
        OnPropertyChanged(nameof(DeleteConfirmationMessage));

        if (!string.IsNullOrEmpty(PhRangeKey))
        {
            PhRange = _fieldOptionsService.GetDisplayForKey(PhRangeKey);
        }

        if (!string.IsNullOrEmpty(DrainageLevelKey))
        {
            DrainageLevel = _fieldOptionsService.GetDisplayForKey(DrainageLevelKey);
        }

        this.LogInfo("[RefreshAllUIProperties] All UI properties refreshed after language change");
    }

    #endregion

    #region Property Change Handlers

    partial void OnPhRangeChanged(string value)
    {
        var key = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetPhRangeKeys());
        if (PhRangeKey != key)
        {
            PhRangeKey = key;
        }
    }

    partial void OnDrainageLevelChanged(string value)
    {
        var key = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetDrainageLevelKeys());
        if (DrainageLevelKey != key)
        {
            DrainageLevelKey = key;
        }
    }

    partial void OnPhRangeKeyChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var display = _fieldOptionsService.GetDisplayForKey(value);
            if (PhRange != display)
            {
                PhRange = display;
            }
        }
    }

    partial void OnDrainageLevelKeyChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var display = _fieldOptionsService.GetDisplayForKey(value);
            if (DrainageLevel != display)
            {
                DrainageLevel = display;
            }
        }
    }

    #endregion

    #region Entity Mapping

    protected override void PrepareEntitySpecificFields(Substrate entity)
    {
        entity.Components = Components;
        entity.PhRange = PhRangeKey;
        entity.DrainageLevel = DrainageLevelKey;
        entity.Supplier = Supplier;

        this.LogInfo($"[PrepareEntitySpecificFields] PhRangeKey: '{PhRangeKey}', DrainageLevelKey: '{DrainageLevelKey}'");
    }

    protected override Task PopulateEntitySpecificFieldsAsync(Substrate entity)
    {
        ExecuteWithAllSuppressionsEnabled(() =>
        {
            Components = entity.Components ?? "";
            PhRangeKey = entity.PhRange ?? "";
            DrainageLevelKey = entity.DrainageLevel ?? "";
            Supplier = entity.Supplier ?? "";

            PhRange = _fieldOptionsService.GetDisplayForKey(PhRangeKey);
            DrainageLevel = _fieldOptionsService.GetDisplayForKey(DrainageLevelKey);

            this.LogInfo($"[PopulateEntitySpecificFieldsAsync] Loaded - PhRange: '{PhRange}', DrainageLevel: '{DrainageLevel}'");
        });

        return Task.CompletedTask;
    }

    #endregion

    #region Validation and Change Detection

    protected override bool IsTrackedProperty(string? propertyName)
    {
        return base.IsTrackedProperty(propertyName) || propertyName is
            nameof(Components) or nameof(PhRange) or nameof(DrainageLevel) or nameof(Supplier) or
            nameof(PhRangeKey) or nameof(DrainageLevelKey);
    }

    #endregion

    #region Delete Confirmation

    public async Task<bool> ShowDeleteConfirmationAsync()
    {
        return await Shell.Current.DisplayAlert("Confirm Delete", DeleteConfirmationMessage, "Yes", "No");
    }

    #endregion

    #region DEBUG

    [RelayCommand]
    public void DebugLocalization()
    {
        var currentLang = _localizationService.GetCurrentLanguage();
        var newLang = currentLang.StartsWith("en") ? "pt-BR" : "en-US";

        System.Diagnostics.Debug.WriteLine($"[DEBUG] Current: {currentLang}, Switching to: {newLang}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] PageTitle: '{PageTitle}'");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] ComponentsLabel: '{ComponentsLabel}'");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] First PhRange option: '{AvailablePhRanges.FirstOrDefault()}'");

        _ = _languageService.SetLanguageAsync(newLang);
    }

    #endregion
}