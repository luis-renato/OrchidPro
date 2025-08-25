using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Locations;

public partial class LocationsListViewModel : BaseListViewModel<PlantLocation, LocationItemViewModel>
{
    private readonly ILocationRepository _locationRepository;

    public override string EntityName => "Location";
    public override string EntityNamePlural => "Locations";
    public override string EditRoute => "locationedit";

    public LocationsListViewModel(ILocationRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _locationRepository = repository;
        this.LogInfo("🚀 ULTRA CLEAN LocationsListViewModel - base does everything!");
    }

    protected override LocationItemViewModel CreateItemViewModel(PlantLocation entity)
    {
        return new LocationItemViewModel(entity);
    }

    public IAsyncRelayCommand<LocationItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;

    // REMOVED: LoadByLocationTypeAsync - não usar LoadFilteredAsync que não existe
    // Se necessário, implementar usando métodos que existem na BaseListViewModel
}
