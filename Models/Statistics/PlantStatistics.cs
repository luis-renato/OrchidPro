// Models/Statistics/PlantStatistics.cs
using OrchidPro.Models.Base;

namespace OrchidPro.Models.Statistics;

public class PlantStatistics : BaseStatistics
{
    public int HealthyPlantsCount { get; set; }
    public int PlantsWithIssuesCount { get; set; }
    public int PlantsNeedingWaterCount { get; set; }
    public int PlantsNeedingFertilizerCount { get; set; }
    public int BloomingPlantsCount { get; set; }
}