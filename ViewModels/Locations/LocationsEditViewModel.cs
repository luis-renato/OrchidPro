using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Locations;

/// <summary>
/// LocationsEditViewModel - CORRIGIDO seguindo padrão FamilyEditViewModel
/// </summary>
public partial class LocationsEditViewModel : BaseEditViewModel<PlantLocation>
{
    #region Private Fields

    private readonly ILocationRepository _locationRepository;

    #endregion

    #region Required Base Class Overrides - IGUAL ao Family

    protected override string GetEntityName() => "Location";
    protected override string GetEntityNamePlural() => "Locations";

    #endregion

    #region Observable Properties (Specific Fields)

    [ObservableProperty]
    private string locationType = "";

    [ObservableProperty]
    private string environmentNotes = "";

    #endregion

    #region UI Properties - IGUAL ao Family

    /// <summary>
    /// Controls visibility of Save and Add Another button - only show in CREATE mode
    /// </summary>
    public bool ShowSaveAndContinue => !IsEditMode;

    #endregion

    #region Constructor - IGUAL ao Family (usando enhanced constructor)

    public LocationsEditViewModel(ILocationRepository locationRepository, INavigationService navigationService)
        : base(locationRepository, navigationService, "Location", "Locations") // Enhanced constructor!
    {
        _locationRepository = locationRepository;
        this.LogInfo("Initialized - using enhanced base functionality");
    }

    #endregion

    #region Navigation Parameter Handling - IGUAL ao Family

    /// <summary>
    /// Enhanced navigation parameter handling for location-specific scenarios
    /// </summary>
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

    #region Initialization Methods - IGUAL ao Family

    /// <summary>
    /// Initialize for creating new location
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
    /// Initialize for editing existing location
    /// </summary>
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

    #region Location-Specific Delete with Validation - IGUAL ao Family

    /// <summary>
    /// Override the base delete with simple confirmation
    /// </summary>
    [RelayCommand]
    public async Task DeleteWithValidationAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            var confirmed = await this.ShowConfirmation("Delete Location",
                "Are you sure you want to delete this location?", "Delete", "Cancel");
            if (!confirmed) return;

            var success = await _locationRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Location deleted successfully");
                await _navigationService.GoBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete location");
            }
        }, "Delete Location");
    }

    #endregion
}