-- ===================
-- LOCATIONS TABLE
-- ===================
CREATE TABLE IF NOT EXISTS public.locations (
    -- CAMPOS BASE (padrão exato estabelecido)
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    
    -- CAMPOS ESPECÍFICOS
    location_type VARCHAR(100) NULL, -- 'Greenhouse', 'Window', 'Outdoor', 'Shelf'
    environment_notes TEXT NULL, -- Light, temperature, humidity info
    
    CONSTRAINT locations_pkey PRIMARY KEY (id),
    CONSTRAINT locations_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT locations_name_user_key UNIQUE (name, user_id)
) TABLESPACE pg_default;

-- ÍNDICES DE PERFORMANCE (padrão estabelecido)
CREATE INDEX IF NOT EXISTS idx_locations_user_id ON public.locations USING btree (user_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_locations_name ON public.locations USING btree (name) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_locations_active ON public.locations USING btree (is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_locations_is_favorite ON public.locations USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE (is_favorite = true);
CREATE INDEX IF NOT EXISTS idx_locations_user_active ON public.locations USING btree (user_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_locations_favorites ON public.locations USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE ((is_favorite = true) AND (user_id IS NOT NULL));

-- ÍNDICE DE BUSCA TEXTUAL (padrão estabelecido)
CREATE INDEX IF NOT EXISTS idx_locations_search ON public.locations USING gin (
    to_tsvector('english'::regconfig, (((name)::text || ' '::text) || COALESCE(description, ''::text)))
) TABLESPACE pg_default;

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.locations IS 'Physical locations within the collection. Contains base fields plus environment-specific information.';
COMMENT ON COLUMN public.locations.id IS 'Unique identifier for the location';
COMMENT ON COLUMN public.locations.user_id IS 'User owner (null = system default)';
COMMENT ON COLUMN public.locations.name IS 'Location name';
COMMENT ON COLUMN public.locations.description IS 'General description of the location';
COMMENT ON COLUMN public.locations.location_type IS 'Type of location (Greenhouse, Window, Outdoor, Shelf)';
COMMENT ON COLUMN public.locations.environment_notes IS 'Environmental conditions (light, temperature, humidity)';

-- Enable Row Level Security
ALTER TABLE public.locations ENABLE ROW LEVEL SECURITY;

-- Create RLS policy (padrão estabelecido)
CREATE POLICY locations_policy ON public.locations
    FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);

-- Grant permissions (padrão estabelecido)
GRANT ALL ON public.locations TO authenticated;
GRANT ALL ON public.locations TO service_role;