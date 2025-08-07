using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// MINIMAL Species edit ViewModel - following exact pattern of GenusEditViewModel.
/// Uses BaseEditViewModel for ALL common functionality + genus relationship management.
/// Only species-specific logic implemented here.
/// </summary>
public partial class SpeciesEditViewModel : BaseEditViewModel<Models.Species>
{
    #region Private Fields

    private readonly ISpeciesRepository _speciesRepository;
    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private Guid? selectedGenusId;

    [ObservableProperty]
    private string selectedGenusName = string.Empty;

    [ObservableProperty]
    private bool isGenusSelectionVisible = true;

    [ObservableProperty]
    private List<Genus> availableGenera = new();

    [ObservableProperty]
    private bool isLoadingGenera;

    [ObservableProperty]
    private string saveAndContinueButtonText = "Save & Add Another";

    [ObservableProperty]
    private bool showSaveAndContinue = true;

    /// <summary>
    /// Currently selected genus object for ComboBox binding
    /// </summary>
    public Genus? SelectedGenus
    {
        get => AvailableGenera?.FirstOrDefault(g => g.Id == SelectedGenusId);
        set
        {
            if (value != null)
            {
                SetSelectedGenus(value);
            }
        }
    }

    #region ComboBox Options Properties (Simplified - only basic ones)

    // Removed complex ComboBox options to match the simplified layout
    // Focusing on essential fields: Genus, Name, Scientific Name, Common Name, Description

    #endregion

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Species";
    public override string EntityNamePlural => "Species";

    #endregion

    #region Page Title Management

    /// <summary>
    /// Dynamic page title based on edit mode state and genus context
    /// </summary>
    public new string PageTitle => IsEditMode ? "Edit Species" : "New Species";

    /// <summary>
    /// Genus context for display
    /// </summary>
    public string GenusContext =>
        !string.IsNullOrEmpty(SelectedGenusName) ? $"in {SelectedGenusName}" : "";

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize species edit ViewModel with enhanced base functionality and genus management
    /// </summary>
    public SpeciesEditViewModel(ISpeciesRepository speciesRepository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(speciesRepository, navigationService)
    {
        _speciesRepository = speciesRepository;
        _genusRepository = genusRepository;

        SaveAndContinueCommand = new AsyncRelayCommand(SaveAndCreateAnotherAsync, () => CanCreateAnother);
        DeleteCommand = new AsyncRelayCommand(DeleteSpeciesAsync, () => CanDelete);

        // Subscribe to genus created message (if we add genus creation later)
        WeakReferenceMessenger.Default.Register<GenusCreatedMessage>(this, OnGenusCreated);

        this.LogInfo("Initialized - using base functionality with genus relationship management");

        // Load available genera for selection
        _ = Task.Run(LoadAvailableGeneraAsync);
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand SaveAndContinueCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }

    #endregion

    #region Genus Management

    /// <summary>
    /// Load available genera for selection dropdown
    /// </summary>
    private async Task LoadAvailableGeneraAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            IsLoadingGenera = true;
            this.LogInfo("Loading available genera for species selection");

            var genera = await _genusRepository.GetAllAsync(false); // Only active genera
            AvailableGenera = genera.OrderBy(g => g.Name).ToList();

            this.LogSuccess($"Loaded {AvailableGenera.Count} genera for selection");

            // Notify SelectedGenus after genera are loaded
            OnPropertyChanged(nameof(SelectedGenus));

        }, "Failed to load available genera");

        IsLoadingGenera = false;
    }

    /// <summary>
    /// Set selected genus for species
    /// </summary>
    [RelayCommand]
    private void SetSelectedGenus(Genus? genus)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Setting selected genus: {genus?.Name ?? "None"}");

            SelectedGenusId = genus?.Id;
            SelectedGenusName = genus?.Name ?? string.Empty;

            // Update entity genus if we're in edit mode and have entity
            if (genus != null && EntityId.HasValue)
            {
                // The entity will be updated in validation before save
                OnPropertyChanged(nameof(GenusContext));
                this.LogInfo($"Will update species genus to: {genus.Name}");
            }

        }, "SetSelectedGenus");
    }

    /// <summary>
    /// Handle genus created message
    /// </summary>
    private void OnGenusCreated(object recipient, GenusCreatedMessage message)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Received GenusCreatedMessage: {message.GenusName}");

            // Reload genera to include the new one
            _ = Task.Run(LoadAvailableGeneraAsync);

        }, "OnGenusCreated");
    }

    #endregion

    #region Override Save Success

    /// <summary>
    /// Override OnSaveSuccessAsync to handle genus relationship and messaging
    /// </summary>
    protected override async Task OnSaveSuccessAsync(Models.Species savedEntity)
    {
        await base.OnSaveSuccessAsync(savedEntity);

        // Update genus relationship if needed
        if (SelectedGenusId.HasValue && savedEntity.GenusId != SelectedGenusId.Value)
        {
            savedEntity.GenusId = SelectedGenusId.Value;
        }

        // Send species created message if it was a new species
        if (!IsEditMode)
        {
            SendSpeciesCreatedMessage(savedEntity.Name);
        }
    }

    /// <summary>
    /// Send species created message for other ViewModels
    /// </summary>
    private void SendSpeciesCreatedMessage(string speciesName)
    {
        this.SafeExecute(() =>
        {
            var message = new SpeciesCreatedMessage { SpeciesName = speciesName };
            WeakReferenceMessenger.Default.Send(message);
            this.LogInfo($"Sent SpeciesCreatedMessage: {speciesName}");
        }, "SendSpeciesCreatedMessage");
    }

    #endregion

    #region Entity Loading Override

    /// <summary>
    /// Override ApplyQueryAttributes to set genus relationship when entity is loaded
    /// </summary>
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        base.ApplyQueryAttributes(query);

        // After base loads the entity, set the genus selection
        if (EntityId.HasValue)
        {
            _ = Task.Run(async () =>
            {
                await LoadAvailableGeneraAsync();
                await SetGenusSelectionFromEntity();
            });
        }
    }

    /// <summary>
    /// Set genus selection based on loaded entity
    /// </summary>
    private async Task SetGenusSelectionFromEntity()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!EntityId.HasValue) return;

            // Get the current entity
            var entity = await _speciesRepository.GetByIdAsync(EntityId.Value);
            if (entity != null && entity.GenusId != Guid.Empty)
            {
                this.LogInfo($"Setting genus selection from entity: {entity.Name}");

                var genus = AvailableGenera.FirstOrDefault(g => g.Id == entity.GenusId);
                if (genus != null)
                {
                    SetSelectedGenus(genus);
                    this.LogSuccess($"Set genus: {genus.Name}");
                }
                else
                {
                    this.LogWarning($"Genus with ID {entity.GenusId} not found in available genera");
                }
            }

        }, "SetGenusSelectionFromEntity");
    }

    #endregion

    #region Additional Commands

    /// <summary>
    /// Save current species and create another one
    /// </summary>
    private async Task SaveAndCreateAnotherAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Save and continue workflow started");

            // Ensure genus is set before saving
            if (SelectedGenusId.HasValue && !string.IsNullOrWhiteSpace(Name))
            {
                // Create new entity with current form data
                var newSpecies = new Models.Species
                {
                    Name = Name,
                    Description = Description,
                    GenusId = SelectedGenusId.Value,
                    IsActive = IsActive,
                    IsFavorite = IsFavorite
                };

                var saved = await _speciesRepository.CreateAsync(newSpecies);
                if (saved != null)
                {
                    await this.ShowSuccessToast($"Species '{saved.Name}' saved successfully");

                    // Clear form for new entry but keep genus selection
                    var currentGenus = SelectedGenus;
                    ClearFormForNewEntry();

                    // Restore genus selection
                    if (currentGenus != null)
                    {
                        SetSelectedGenus(currentGenus);
                    }

                    this.LogSuccess("Ready for new species entry with same genus");
                }
            }
            else
            {
                await ShowErrorAsync("Validation Error", "Please select a genus and enter a species name.");
            }

        }, "Save and Continue");
    }

    /// <summary>
    /// Clear form for new entry
    /// </summary>
    private void ClearFormForNewEntry()
    {
        Name = string.Empty;
        Description = string.Empty;
        IsActive = true;
        IsFavorite = false;
        _isEditMode = false;
        EntityId = null;

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }

    /// <summary>
    /// Delete species with confirmation
    /// </summary>
    private async Task DeleteSpeciesAsync()
    {
        if (!EntityId.HasValue) return;

        await this.SafeExecuteAsync(async () =>
        {
            var confirmTitle = $"Delete {Name}?";
            var confirmMessage = "Are you sure you want to delete this species?";

            var confirmed = await ShowConfirmAsync(confirmTitle, confirmMessage);
            if (!confirmed) return;

            var success = await _speciesRepository.DeleteAsync(EntityId.Value);
            if (success)
            {
                await this.ShowSuccessToast($"Species '{Name}' deleted successfully");
                await _navigationService.GoBackAsync();
                this.LogSuccess($"Species deleted: {Name}");
            }
            else
            {
                await ShowErrorAsync("Delete Failed", "Unable to delete the species. Please try again.");
                this.LogError($"Failed to delete species: {Name}");
            }

        }, $"Delete Species: {Name}");
    }

    #endregion

    #region Can Execute Properties

    /// <summary>
    /// Can create another species (needs genus selection)
    /// </summary>
    public bool CanCreateAnother => !IsBusy && SelectedGenusId.HasValue;

    #endregion
}

/// <summary>
/// Message for genus created events
/// </summary>
public class GenusCreatedMessage
{
    public string GenusName { get; set; } = string.Empty;
}

/// <summary>
/// Message for species created events
/// </summary>
public class SpeciesCreatedMessage
{
    public string SpeciesName { get; set; } = string.Empty;
}