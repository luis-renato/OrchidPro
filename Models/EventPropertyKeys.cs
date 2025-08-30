namespace OrchidPro.Models
{
    /// <summary>
    /// Standardized keys for event properties
    /// </summary>
    public static class EventPropertyKeys
    {
        // ACQUISITION PROPERTIES
        public const string PricePaid = "price_paid";
        public const string SellerNotes = "seller_notes";
        public const string AcquisitionMethod = "acquisition_method";
        public const string InvoiceNumber = "invoice_number";

        // CARE PROPERTIES
        public const string WaterType = "water_type";
        public const string WaterAmount = "water_amount";
        public const string FertilizerBrand = "fertilizer_brand";
        public const string FertilizerConcentration = "fertilizer_concentration";
        public const string FertilizerType = "fertilizer_type";

        // HEALTH PROPERTIES
        public const string Severity = "severity";
        public const string Symptoms = "symptoms";
        public const string TreatmentNotes = "treatment_notes";
        public const string RecoveryNotes = "recovery_notes";

        // FLOWERING PROPERTIES
        public const string SpikeCount = "spike_count";
        public const string FlowerCount = "flower_count";
        public const string BloomColor = "bloom_color";
        public const string Fragrance = "fragrance";
        public const string BloomSize = "bloom_size";

        // GROWTH PROPERTIES
        public const string MeasurementValue = "measurement_value";
        public const string MeasurementUnit = "measurement_unit";
        public const string MeasurementType = "measurement_type";
        public const string GrowthNotes = "growth_notes";

        // LOCATION PROPERTIES
        public const string PreviousLocationId = "previous_location_id";
        public const string NewLocationId = "new_location_id";
        public const string RelocationReason = "relocation_reason";

        // CONTAINER PROPERTIES
        public const string PreviousContainerId = "previous_container_id";
        public const string NewContainerId = "new_container_id";
        public const string SubstrateType = "substrate_type";
        public const string RepottingReason = "repotting_reason";
    }
}