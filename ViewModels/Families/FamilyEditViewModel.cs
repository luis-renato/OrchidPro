using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ViewModel for editing and creating botanical family records.
/// Provides family-specific functionality while leveraging enhanced base edit operations.
/// </summary>
public partial class FamilyEditViewModel : BaseEditViewModel<Family>, IQueryAttributable
{
    #region Required Base Class Overrides

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";

    #endregion

    #region Page Title Management

    /// <summary>
    /// Dynamic page title based on edit mode state
    /// </summary>
    public new string PageTitle => IsEditMode ? "Edit Family" : "New Family";

    /// <summary>
    /// Current edit mode state for UI binding
    /// </summary>
    public bool IsEditMode => _isEditMode;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize family edit ViewModel with enhanced base functionality
    /// </summary>
    public FamilyEditViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        this.LogInfo("Initialized - using base functionality with corrections");
    }

    #endregion

    #region Save Operations Override

    /// <summary>
    /// Override save command to send messaging when family is created
    /// </summary>
    [RelayCommand]
    private async Task SaveWithMessagingAsync()
    {
        var wasCreateOperation = !IsEditMode;
        var familyName = Name;

        // Call base save command
        await SaveCommand.ExecuteAsync(null);

        // Send message only when creating new family (not editing)
        if (wasCreateOperation && EntityId.HasValue)
        {
            await this.SafeExecuteAsync(async () =>
            {
                var message = new FamilyCreatedMessage(EntityId.Value, familyName);
                WeakReferenceMessenger.Default.Send(message);
                this.LogSuccess($"Sent FamilyCreatedMessage for: {familyName}");
            }, "Send FamilyCreatedMessage");
        }
    }

    #endregion

    #region Query Attributes Handling

    /// <summary>
    /// Handle navigation parameters and update edit mode state
    /// </summary>
    public new void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ApplyQueryAttributes called with {query.Count} parameters");

            // Log all parameters for debugging
            foreach (var param in query)
            {
                this.LogInfo($"Parameter: {param.Key} = {param.Value} ({param.Value?.GetType().Name})");
            }

            // Call base implementation first
            base.ApplyQueryAttributes(query);

            // Notify UI of title and mode changes
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));

            this.LogSuccess($"Query attributes applied - IsEditMode: {IsEditMode}, PageTitle: {PageTitle}");

        }, "ApplyQueryAttributes");
    }

    #endregion

}