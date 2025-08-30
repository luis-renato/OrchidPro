CREATE TABLE IF NOT EXISTS public.events (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    plant_id UUID NOT NULL,
    event_type_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT NULL,
    scheduled_date DATE NOT NULL,
    actual_date DATE NULL,
    notes TEXT NULL,
    photos_count INTEGER NULL DEFAULT 0,
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    
    CONSTRAINT events_pkey PRIMARY KEY (id),
    CONSTRAINT events_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT events_plant_id_fkey FOREIGN KEY (plant_id) REFERENCES plants(id) ON DELETE CASCADE,
    CONSTRAINT events_event_type_id_fkey FOREIGN KEY (event_type_id) REFERENCES event_types(id) ON DELETE RESTRICT
);

-- Índices otimizados para Event-Sourcing
CREATE INDEX IF NOT EXISTS idx_events_plant_id_date ON public.events USING btree (plant_id, actual_date DESC NULLS LAST, scheduled_date DESC);
CREATE INDEX IF NOT EXISTS idx_events_plant_type ON public.events USING btree (plant_id, event_type_id);
CREATE INDEX IF NOT EXISTS idx_events_user_date ON public.events USING btree (user_id, actual_date DESC NULLS LAST);

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.events IS 'Individual events for plant care tracking. Core of the event-sourcing system for computing plant status and history.';

-- CAMPOS BASE PADRÃO
COMMENT ON COLUMN public.events.id IS 'Unique identifier for the event';
COMMENT ON COLUMN public.events.user_id IS 'User owner of this event (required)';
COMMENT ON COLUMN public.events.is_active IS 'Active status flag';
COMMENT ON COLUMN public.events.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.events.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.events.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.events.sync_hash IS 'Synchronization hash for offline sync';

-- CAMPOS ESPECÍFICOS DE EVENTS
COMMENT ON COLUMN public.events.plant_id IS 'Plant instance this event applies to';
COMMENT ON COLUMN public.events.event_type_id IS 'Type of event being recorded';
COMMENT ON COLUMN public.events.title IS 'Event title or summary';
COMMENT ON COLUMN public.events.description IS 'Detailed event description';
COMMENT ON COLUMN public.events.scheduled_date IS 'When this event was/is scheduled to occur';
COMMENT ON COLUMN public.events.actual_date IS 'When this event actually occurred (null = not yet completed)';
COMMENT ON COLUMN public.events.notes IS 'Additional notes or observations';
COMMENT ON COLUMN public.events.photos_count IS 'Number of photos associated with this event';

-- RLS
ALTER TABLE public.events ENABLE ROW LEVEL SECURITY;
CREATE POLICY events_policy ON public.events FOR ALL USING (user_id = auth.uid());
GRANT ALL ON public.events TO authenticated;
GRANT ALL ON public.events TO service_role;
