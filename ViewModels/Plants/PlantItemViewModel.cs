// ViewModels/Plants/PlantItemViewModel.cs
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Plants;

public partial class PlantItemViewModel(Plant plant) : BaseItemViewModel<Plant>(plant)
{
    #region Required Base Class Override
    public override string EntityName => "Plant";
    #endregion

    #region Public Entity Access
    // Add public accessor for Entity that uses ToModel() like your BaseItemViewModel pattern
    public Plant Entity => ToModel();
    #endregion

    #region Plant-Specific Properties (READ-ONLY)
    public bool IsFavorite => Entity?.IsFavorite ?? false;
    public string PlantCode => Entity?.PlantCode ?? string.Empty;
    public string? CommonName => Entity?.CommonName;
    public Guid SpeciesId => Entity?.SpeciesId ?? Guid.Empty;
    public Guid? VariantId => Entity?.VariantId;
    public Species? Species => Entity?.Species;
    public Variant? Variant => Entity?.Variant;
    #endregion

    #region Event-Sourced Computed Properties (READ-ONLY)
    public string HealthStatus => Entity?.HealthStatus ?? "Unknown";
    public string HealthStatusColor => Entity?.HealthStatusColor ?? "#9E9E9E";
    public bool HasHealthIssues => Entity?.HasHealthIssues ?? false;
    public bool NeedsWatering => Entity?.NeedsWatering ?? false;
    public bool NeedsFertilizing => Entity?.NeedsFertilizing ?? false;
    public bool IsCurrentlyBlooming => Entity?.IsCurrentlyBlooming ?? false;
    public int DaysSinceLastWatering => Entity?.DaysSinceLastWatering ?? int.MaxValue;
    public int DaysSinceLastFertilizing => Entity?.DaysSinceLastFertilizing ?? int.MaxValue;
    public DateTime? LastWatered => Entity?.LastWatered;
    public DateTime? LastFertilized => Entity?.LastFertilized;
    public DateTime? AcquisitionDate => Entity?.AcquisitionDate;
    public decimal? AcquisitionPrice => Entity?.AcquisitionPrice;
    #endregion

    #region Display Properties
    public string DisplayName => !string.IsNullOrWhiteSpace(CommonName)
        ? $"{PlantCode} ({CommonName})"
        : PlantCode;
    public string SpeciesDisplay => Species?.Name ?? "Unknown Species";
    public string VariantDisplay => Variant?.Name ?? "No variant";
    public string StatusSummary => GenerateStatusSummary();
    public string CareStatusSummary => GenerateCareStatusSummary();
    public string HealthStatusDisplay => $"{HealthStatus}";
    public bool ShowWateringAlert => NeedsWatering;
    public bool ShowFertilizingAlert => NeedsFertilizing;
    public bool ShowHealthAlert => HasHealthIssues;
    public bool ShowBloomingIndicator => IsCurrentlyBlooming;
    public string WateringStatusText => LastWatered.HasValue
        ? $"Watered {DaysSinceLastWatering}d ago"
        : "Never watered";
    public string FertilizingStatusText => LastFertilized.HasValue
        ? $"Fertilized {DaysSinceLastFertilizing}d ago"
        : "Never fertilized";
    public bool HasDetails => !string.IsNullOrWhiteSpace(CommonName) ||
                             AcquisitionDate.HasValue ||
                             IsCurrentlyBlooming;
    #endregion

    #region Private Helper Methods
    private string GenerateStatusSummary()
    {
        var status = new List<string>();
        if (HasHealthIssues)
            status.Add($"Health: {HealthStatus}");
        if (NeedsWatering)
            status.Add($"Water: {DaysSinceLastWatering}d ago");
        if (NeedsFertilizing)
            status.Add($"Fertilizer: {DaysSinceLastFertilizing}d ago");
        if (IsCurrentlyBlooming)
            status.Add("🌸 Blooming");
        return status.Any() ? string.Join(" • ", status) : "All good";
    }

    private string GenerateCareStatusSummary()
    {
        var careItems = new List<string>
        {
            WateringStatusText,
            FertilizingStatusText
        };
        return string.Join(" • ", careItems);
    }
    #endregion
}