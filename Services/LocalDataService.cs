using OrchidPro.Models;

namespace OrchidPro.Services;

/// <summary>
/// Temporary implementation of ILocalDataService for demonstration
/// In a real app, this would use SQLite with Entity Framework Core
/// </summary>
public class LocalDataService : ILocalDataService
{
    private readonly List<Family> _families = new();

    public Task<List<Family>> GetAllFamiliesAsync()
    {
        return Task.FromResult(_families.ToList());
    }

    public Task SaveFamilyAsync(Family family)
    {
        var existingIndex = _families.FindIndex(f => f.Id == family.Id);
        if (existingIndex >= 0)
        {
            _families[existingIndex] = family;
        }
        else
        {
            _families.Add(family);
        }
        return Task.CompletedTask;
    }

    public Task DeleteFamilyAsync(Guid id)
    {
        _families.RemoveAll(f => f.Id == id);
        return Task.CompletedTask;
    }
}