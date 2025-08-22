using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Models;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using System.Collections.ObjectModel;

namespace OrchidPro.ViewModels.Botanical.Genera;

/// <summary>
/// MINIMAL Genus edit ViewModel - Copy of Species pattern that works
/// </summary>
public partial class GenusEditViewModel : BaseEditViewModel<Genus>
{
    #region Private Fields

    private readonly IGenusRepository _genusRepository;
    private readonly IFamilyRepository _familyRepository;

    #endregion

    #region Required Base Class Overrides

    protected override string GetEntityName() => "Genus";
    protected override string GetEntityNamePlural() => "Genera";

    #endregion

    #region 🔗 RELATIONSHIP MANAGEMENT - EXACTLY like Species

    /// <summary>
    /// Available families collection - EXACTLY like AvailableGenera in Species
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Family> availableFamilies = [];

    /// <summary>
    /// Currently selected family - EXACTLY like SelectedGenus in Species
    /// </summary>
    [ObservableProperty]
    private Family? selectedFamily;

    // Override virtual properties from base for relationship management - EXACTLY like Species
    public override Guid? ParentEntityId => SelectedFamily?.Id;
    public override string ParentDisplayName => SelectedFamily?.Name ?? string.Empty;

    /// <summary>
    /// Handle family selection changes - EXACTLY like Species
    /// </summary>
    partial void OnSelectedFamilyChanged(Family? value)
    {
        OnParentSelectionChanged(); // Call base method
    }

    #endregion

    // ADICIONAR ESTE MÉTODO AO GenusEditViewModel EXISTENTE:

    #region Navigation Parameter Handling - EXACTLY like Species

    /// <summary>
    /// Enhanced navigation parameter handling for genus-specific scenarios - EXACTLY like Species
    /// </summary>
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Applying query attributes for Genus edit");

            if (query.TryGetValue("GenusId", out var genusIdObj) &&
                Guid.TryParse(genusIdObj?.ToString(), out var genusId))
            {
                // Use genus-specific initialization for edit mode - EXACTLY like Species
                _ = InitializeForEditAsync(genusId);
            }
            else if (query.TryGetValue("FamilyId", out var familyIdObj) &&
                     Guid.TryParse(familyIdObj?.ToString(), out var familyId))
            {
                // Use genus-specific initialization for create mode with family preselection
                _ = InitializeForCreateAsync(familyId);
            }
            else
            {
                // Use genus-specific initialization for create mode
                _ = InitializeForCreateAsync();
            }
        }, "Apply Query Attributes");
    }

    #endregion

    #region Initialization Methods - EXACTLY like Species

    /// <summary>
    /// Initialize for creating new genus with optional family preselection - EXACTLY like Species
    /// </summary>
    public async Task InitializeForCreateAsync(Guid? familyId = null)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for create mode with family preselection: {familyId}");

            // Ensure families are loaded
            if (!AvailableFamilies.Any())
            {
                await LoadAvailableFamiliesAsync();
            }

            // Preselect family if provided using base method
            if (familyId.HasValue)
            {
                SelectParentById(AvailableFamilies, familyId.Value, family => SelectedFamily = family, "Family");
                this.LogInfo($"Preselected family: {ParentDisplayName}");
            }

            HasUnsavedChanges = false;
            this.LogInfo("Create mode initialization completed - form is clean");
        }, "Initialize for Create");
    }

    /// <summary>
    /// Initialize for editing existing genus - EXACTLY like Species
    /// </summary>
    public async Task InitializeForEditAsync(Guid genusId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {genusId}");

            // Ensure families are loaded first
            if (!AvailableFamilies.Any())
            {
                await LoadAvailableFamiliesAsync();
            }

            // Set entity ID and edit mode FIRST, then load data via base class
            EntityId = genusId;
            _isEditMode = true;

            // Force update of computed properties that depend on IsEditMode
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));

            // Load entity data using base class method
            await LoadEntityAsync();

            this.LogInfo("Edit mode initialization completed");
        }, "Initialize for Edit");
    }

    #endregion

    #region 🔧 UI Properties - EXACTLY like Species

    /// <summary>
    /// Override page title - EXACTLY like Species
    /// </summary>
    public override string PageTitle => IsEditMode ? "Edit Genus" :
        !string.IsNullOrEmpty(ParentDisplayName) ? $"New Genus in {ParentDisplayName}" : "New Genus";

    #endregion

    #region Constructor - EXACTLY like Species

    public GenusEditViewModel(IGenusRepository genusRepository, IFamilyRepository familyRepository, INavigationService navigationService)
        : base(genusRepository, navigationService, "Genus", "Genera", "Family") // Enhanced constructor!
    {
        _genusRepository = genusRepository;
        _familyRepository = familyRepository;

        // Subscribe to family creation messages - EXACTLY like Species with Genus
        SubscribeToParentCreatedMessages<FamilyCreatedMessage>(
            m => m.FamilyId,
            m => m.FamilyName,
            async (id, name) => await HandleFamilyCreatedAsync(id, name));

        this.LogInfo("Initialized - using enhanced base functionality with family relationship management");

        // Background load of available families - EXACTLY like Species
        _ = Task.Run(async () => await LoadAvailableFamiliesAsync());
    }

    #endregion

    #region Base Class Overrides - EXACTLY like Species (minimal needed)

    /// <summary>
    /// Override to include genus-specific properties - EXACTLY like Species
    /// </summary>
    protected override bool IsTrackedProperty(string? propertyName) =>
        base.IsTrackedProperty(propertyName) || propertyName is nameof(SelectedFamily);

    /// <summary>
    /// Override entity preparation - EXACTLY like Species
    /// </summary>
    protected override void PrepareEntitySpecificFields(Genus entity)
    {
        entity.FamilyId = ParentEntityId!.Value;
    }

    /// <summary>
    /// Override entity population - EXACTLY like Species
    /// </summary>
    protected override async Task PopulateEntitySpecificFieldsAsync(Genus entity)
    {
        await ExecuteWithAllSuppressionsEnabledAsync(async () =>
        {
            // Ensure families are loaded before setting selection
            if (!AvailableFamilies.Any())
            {
                await LoadAvailableFamiliesAsync();
            }

            // Select the family using base method - EXACTLY like Species
            SelectParentById(AvailableFamilies, entity.FamilyId, family => SelectedFamily = family, "Family");

            this.LogInfo($"Loaded genus data: {entity.Name} in family {ParentDisplayName}");
        });
    }

    /// <summary>
    /// Override save success - EXACTLY like Species
    /// </summary>
    protected override async Task OnEntitySavedAsync(Genus savedEntity)
    {
        if (!IsEditMode)
        {
            // Send genus created message for species auto-selection
            WeakReferenceMessenger.Default.Send(new GenusCreatedMessage(savedEntity.Id, savedEntity.Name));
            this.LogSuccess($"Genus created and message sent: {savedEntity.Name}");
        }

        // Always send updated message for list refresh
        WeakReferenceMessenger.Default.Send(new GenusUpdatedMessage());

        await base.OnEntitySavedAsync(savedEntity);
    }

    #endregion

    #region Family-Specific Operations - EXACTLY like Species with Genus

    /// <summary>
    /// Load available families - EXACTLY like Species LoadAvailableGeneraAsync
    /// </summary>
    private async Task LoadAvailableFamiliesAsync()
    {
        await LoadParentCollectionAsync(_familyRepository, AvailableFamilies, "Available Families");
    }

    /// <summary>
    /// Handle family creation - EXACTLY like Species HandleGenusCreatedAsync
    /// </summary>
    private async Task HandleFamilyCreatedAsync(Guid familyId, string familyName)
    {
        await HandleParentCreatedAsync(
            familyId,
            familyName,
            _familyRepository,
            AvailableFamilies,
            family => SelectedFamily = family,
            "Family");
    }

    #endregion

    #region Commands - EXACTLY like Species

    /// <summary>
    /// Create new family command - EXACTLY like Species CreateNewGenusCommand
    /// </summary>
    public IAsyncRelayCommand CreateNewFamilyCommand => NavigateToCreateParentCommand;

    #endregion
}