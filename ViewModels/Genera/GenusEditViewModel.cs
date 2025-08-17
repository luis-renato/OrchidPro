using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using System.Collections.ObjectModel;

namespace OrchidPro.ViewModels.Genera;

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