CREATE TABLE IF NOT EXISTS public.event_properties (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL,
    property_key VARCHAR(50) NOT NULL,
    property_value TEXT NOT NULL,
    data_type VARCHAR(20) NOT NULL DEFAULT 'text',
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    
    CONSTRAINT event_properties_pkey PRIMARY KEY (id),
    CONSTRAINT event_properties_event_fkey FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE CASCADE,
    CONSTRAINT event_properties_unique UNIQUE (event_id, property_key),
    CONSTRAINT event_properties_data_type_check CHECK (data_type IN ('text', 'integer', 'decimal', 'boolean', 'date'))
);

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.event_properties IS 'Additional key-value properties for events. Provides flexibility for storing event-specific data without schema changes.';

-- CAMPOS ESPECÍFICOS DE EVENT PROPERTIES
COMMENT ON COLUMN public.event_properties.id IS 'Unique identifier for the property';
COMMENT ON COLUMN public.event_properties.event_id IS 'Parent event this property belongs to';
COMMENT ON COLUMN public.event_properties.property_key IS 'Property name/key (e.g., amount, concentration, ph_level)';
COMMENT ON COLUMN public.event_properties.property_value IS 'Property value stored as text (parsed according to data_type)';
COMMENT ON COLUMN public.event_properties.data_type IS 'Data type for proper parsing (text, integer, decimal, boolean, date)';
COMMENT ON COLUMN public.event_properties.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.event_properties.updated_at IS 'Last update timestamp';

-- RLS
ALTER TABLE public.event_properties ENABLE ROW LEVEL SECURITY;
CREATE POLICY event_properties_policy ON public.event_properties
    FOR ALL USING (
        EXISTS (
            SELECT 1 FROM public.events e 
            WHERE e.id = event_properties.event_id 
            AND e.user_id = auth.uid()
        )
    );

GRANT ALL ON public.event_properties TO authenticated;
GRANT ALL ON public.event_properties TO service_role;