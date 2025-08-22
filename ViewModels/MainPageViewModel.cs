using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Services.Contracts;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels;

public partial class MainPageViewModel : BaseViewModel
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IGenusRepository _genusRepository;
    private readonly ISpeciesRepository _speciesRepository;
    private readonly IVariantRepository _variantRepository;

    [ObservableProperty]
    private int familyCount;

    [ObservableProperty]
    private int genusCount;

    [ObservableProperty]
    private int speciesCount;

    [ObservableProperty]
    private int variantCount;

    [ObservableProperty]
    private int totalEntries;

    [ObservableProperty]
    private int favoriteEntries;

    public MainPageViewModel(
        IFamilyRepository familyRepository,
        IGenusRepository genusRepository,
        ISpeciesRepository speciesRepository,
        IVariantRepository variantRepository)
    {
        _familyRepository = familyRepository;
        _genusRepository = genusRepository;
        _speciesRepository = speciesRepository;
        _variantRepository = variantRepository;
    }

    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LoadDashboardData starting...");
            IsBusy = true;

            System.Diagnostics.Debug.WriteLine("Calling repositories...");
            var familyTask = _familyRepository.GetAllAsync();
            var genusTask = _genusRepository.GetAllAsync();
            var speciesTask = _speciesRepository.GetAllAsync();
            var variantTask = _variantRepository.GetAllAsync();

            await Task.WhenAll(familyTask, genusTask, speciesTask, variantTask);
            System.Diagnostics.Debug.WriteLine("All repository calls completed");

            var families = await familyTask;
            var genera = await genusTask;
            var species = await speciesTask;
            var variants = await variantTask;

            var familiesList = families.ToList();
            var generaList = genera.ToList();
            var speciesList = species.ToList();
            var variantsList = variants.ToList();

            FamilyCount = familiesList.Count;
            GenusCount = generaList.Count;
            SpeciesCount = speciesList.Count;
            VariantCount = variantsList.Count;

            System.Diagnostics.Debug.WriteLine($"Counts: F={FamilyCount}, G={GenusCount}, S={SpeciesCount}, V={VariantCount}");

            TotalEntries = FamilyCount + GenusCount + SpeciesCount + VariantCount;
            FavoriteEntries = familiesList.Count(f => f.IsFavorite) +
                            generaList.Count(g => g.IsFavorite) +
                            speciesList.Count(s => s.IsFavorite) +
                            variantsList.Count(v => v.IsFavorite);

            System.Diagnostics.Debug.WriteLine($"Total: {TotalEntries}, Favorites: {FavoriteEntries}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadDashboardData: {ex}");
            this.LogError(ex, "Error loading dashboard data");
        }
        finally
        {
            IsBusy = false;
            System.Diagnostics.Debug.WriteLine("LoadDashboardData finished");
        }
    }
}