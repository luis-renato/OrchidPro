CREATE TABLE IF NOT EXISTS public.event_types (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NULL,
    name_key VARCHAR(100) NULL,
    description_key VARCHAR(100) NULL,
    display_name VARCHAR(255) NULL,
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    category_key VARCHAR(50) NOT NULL,
    icon VARCHAR(50) NULL,
    color VARCHAR(7) NULL,
    is_positive BOOLEAN NULL DEFAULT true,
    requires_future_date BOOLEAN NULL DEFAULT false,
    is_system_default BOOLEAN NULL DEFAULT false,
    
    CONSTRAINT event_types_pkey PRIMARY KEY (id),
    CONSTRAINT event_types_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE
);

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.event_types IS 'Event type definitions for plant care activities. Supports both system defaults and user-defined types.';

-- CAMPOS BASE PADRÃO
COMMENT ON COLUMN public.event_types.id IS 'Unique identifier for the event type';
COMMENT ON COLUMN public.event_types.user_id IS 'User owner (null = system default type)';
COMMENT ON COLUMN public.event_types.is_active IS 'Active status flag';
COMMENT ON COLUMN public.event_types.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.event_types.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.event_types.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.event_types.sync_hash IS 'Synchronization hash for offline sync';

-- CAMPOS ESPECÍFICOS DE EVENT TYPES
COMMENT ON COLUMN public.event_types.name_key IS 'Localization key for event type name';
COMMENT ON COLUMN public.event_types.description_key IS 'Localization key for event type description';
COMMENT ON COLUMN public.event_types.display_name IS 'User-defined display name (overrides localized name)';
COMMENT ON COLUMN public.event_types.category_key IS 'Event category for grouping (Acquisition, Care, Health, etc.)';
COMMENT ON COLUMN public.event_types.icon IS 'Material Design icon identifier';
COMMENT ON COLUMN public.event_types.color IS 'Hex color code for UI representation';
COMMENT ON COLUMN public.event_types.is_positive IS 'Whether this event type represents positive/negative occurrence';
COMMENT ON COLUMN public.event_types.requires_future_date IS 'Whether this event type requires future scheduling';
COMMENT ON COLUMN public.event_types.is_system_default IS 'Whether this is a system-provided default type';

-- RLS
ALTER TABLE public.event_types ENABLE ROW LEVEL SECURITY;
CREATE POLICY event_types_policy ON public.event_types FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);
GRANT ALL ON public.event_types TO authenticated;
GRANT ALL ON public.event_types TO service_role;

-- SEED DATA EXPANDIDO
INSERT INTO public.event_types (user_id, name_key, description_key, category_key, icon, color, is_positive, requires_future_date, is_system_default) VALUES

-- ACQUISITION
(NULL, 'EventType.Acquired', 'EventType.Acquired.Description', 'EventCategory.Acquisition', 'shopping_cart', '#4CAF50', true, false, true),
(NULL, 'EventType.Received', 'EventType.Received.Description', 'EventCategory.Acquisition', 'card_giftcard', '#4CAF50', true, false, true),
(NULL, 'EventType.Traded', 'EventType.Traded.Description', 'EventCategory.Acquisition', 'swap_horiz', '#4CAF50', true, false, true),

-- CARE
(NULL, 'EventType.Watered', 'EventType.Watered.Description', 'EventCategory.Care', 'water_drop', '#2196F3', true, false, true),
(NULL, 'EventType.Fertilized', 'EventType.Fertilized.Description', 'EventCategory.Care', 'eco', '#8BC34A', true, false, true),
(NULL, 'EventType.Repotted', 'EventType.Repotted.Description', 'EventCategory.Care', 'potted_plant', '#FF9800', true, false, true),
(NULL, 'EventType.Relocated', 'EventType.Relocated.Description', 'EventCategory.Care', 'place', '#795548', true, false, true),

-- HEALTH
(NULL, 'EventType.HealthIssue', 'EventType.HealthIssue.Description', 'EventCategory.Health', 'healing', '#F44336', false, false, true),
(NULL, 'EventType.TreatmentApplied', 'EventType.TreatmentApplied.Description', 'EventCategory.Health', 'medical_services', '#9C27B0', true, false, true),
(NULL, 'EventType.RecoveryNoted', 'EventType.RecoveryNoted.Description', 'EventCategory.Health', 'trending_up', '#4CAF50', true, false, true),
(NULL, 'EventType.HealthStatusChanged', 'EventType.HealthStatusChanged.Description', 'EventCategory.Health', 'healing', '#FF9800', true, false, true),

-- FLOWERING
(NULL, 'EventType.FirstBloom', 'EventType.FirstBloom.Description', 'EventCategory.Flowering', 'local_florist', '#E91E63', true, false, true),
(NULL, 'EventType.SpikeEmerged', 'EventType.SpikeEmerged.Description', 'EventCategory.Flowering', 'timeline', '#E91E63', true, false, true),
(NULL, 'EventType.BudsFormed', 'EventType.BudsFormed.Description', 'EventCategory.Flowering', 'radio_button_unchecked', '#E91E63', true, false, true),
(NULL, 'EventType.BloomingEnded', 'EventType.BloomingEnded.Description', 'EventCategory.Flowering', 'local_florist', '#9E9E9E', true, false, true),

-- GROWTH
(NULL, 'EventType.NewGrowth', 'EventType.NewGrowth.Description', 'EventCategory.Growth', 'trending_up', '#4CAF50', true, false, true),
(NULL, 'EventType.SizeMeasured', 'EventType.SizeMeasured.Description', 'EventCategory.Growth', 'straighten', '#607D8B', true, false, true),

-- DOCUMENTATION
(NULL, 'EventType.PhotoTaken', 'EventType.PhotoTaken.Description', 'EventCategory.Documentation', 'photo_camera', '#607D8B', true, false, true),
(NULL, 'EventType.NotesUpdated', 'EventType.NotesUpdated.Description', 'EventCategory.Documentation', 'edit_note', '#795548', true, false, true);
