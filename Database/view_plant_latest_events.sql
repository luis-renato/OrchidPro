CREATE OR REPLACE VIEW public.plant_latest_events AS
WITH latest_events AS (
    SELECT DISTINCT ON (e.plant_id, et.category_key)
        e.plant_id,
        et.category_key,
        e.id as event_id,
        e.actual_date,
        e.scheduled_date,
        et.name_key as event_type_name_key
    FROM public.events e
    JOIN public.event_types et ON e.event_type_id = et.id
    WHERE e.is_active = true 
    AND e.actual_date IS NOT NULL
    ORDER BY e.plant_id, et.category_key, e.actual_date DESC
)
SELECT * FROM latest_events;

-- View para plant status computado
CREATE OR REPLACE VIEW public.plants_with_computed_status AS
SELECT 
    p.*,
    -- Aquisição
    acq.actual_date as acquisition_date,
    -- Última rega
    care_water.actual_date as last_watered,
    -- Última fertilização  
    care_fert.actual_date as last_fertilized,
    -- Status de saúde (simplified)
    COALESCE(health.event_type_name_key, 'Healthy') as health_status_key
FROM public.plants p
LEFT JOIN public.plant_latest_events acq ON p.id = acq.plant_id 
    AND acq.category_key = 'EventCategory.Acquisition'
LEFT JOIN public.plant_latest_events care_water ON p.id = care_water.plant_id 
    AND care_water.event_type_name_key = 'EventType.Watered'
LEFT JOIN public.plant_latest_events care_fert ON p.id = care_fert.plant_id 
    AND care_fert.event_type_name_key = 'EventType.Fertilized'  
LEFT JOIN public.plant_latest_events health ON p.id = health.plant_id 
    AND health.category_key = 'EventCategory.Health'
WHERE p.is_active = true;

-- COMENTÁRIOS PARA VIEWS
COMMENT ON VIEW public.plant_latest_events IS 'Optimized view for retrieving the most recent event of each category per plant. Used for computing current plant status.';
COMMENT ON VIEW public.plants_with_computed_status IS 'Plants enriched with computed status from event history. Provides current state without scanning all events.';

GRANT ALL ON public.plant_latest_events TO authenticated;
GRANT ALL ON public.plants_with_computed_status TO authenticated;