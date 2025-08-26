-- ======================================
-- Mounts Table Schema for OrchidPro - STANDARDIZED
-- Mounting containers and pots for orchids
-- Apenas campos base padronizados + campos específicos de mount
-- ======================================

-- Create mounts table
CREATE TABLE IF NOT EXISTS public.mounts (
    -- CAMPOS BASE (padrão exato de families/genera) - TODOS OBRIGATÓRIOS
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    
    -- Mount-specific fields
    material VARCHAR(100) NULL, -- 'Plastic', 'Clay', 'Wood', 'Ceramic'
    size VARCHAR(50) NULL, -- '4 inch', '6 inch', 'Large', 'Small'
    drainage_type VARCHAR(100) NULL, -- 'Multiple holes', 'Slotted', 'Basket weave'
    
    CONSTRAINT mounts_pkey PRIMARY KEY (id),
    CONSTRAINT mounts_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT mounts_name_not_empty CHECK (length(trim(name)) > 0),
    CONSTRAINT mounts_name_user_key UNIQUE (name, user_id)
) TABLESPACE pg_default;

-- ÍNDICES DE PERFORMANCE (campos base - padrão das outras tabelas)
CREATE INDEX IF NOT EXISTS idx_mounts_user_id ON public.mounts USING btree (user_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_mounts_name ON public.mounts USING btree (name) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_mounts_active ON public.mounts USING btree (is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_mounts_is_favorite ON public.mounts USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE (is_favorite = true);
CREATE INDEX IF NOT EXISTS idx_mounts_user_active ON public.mounts USING btree (user_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_mounts_favorites ON public.mounts USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE ((is_favorite = true) AND (user_id IS NOT NULL));

-- ÍNDICES ESPECÍFICOS DE MOUNTS
CREATE INDEX IF NOT EXISTS idx_mounts_material ON public.mounts USING btree (material) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_mounts_size ON public.mounts USING btree (size) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_mounts_drainage ON public.mounts USING btree (drainage_type) TABLESPACE pg_default;

-- ÍNDICE DE BUSCA TEXTUAL (name + description + material)
CREATE INDEX IF NOT EXISTS idx_mounts_search ON public.mounts USING gin (
    to_tsvector('english'::regconfig, (((name)::text || ' '::text) || COALESCE(description, ''::text) || ' '::text || COALESCE(material, ''::text)))
) TABLESPACE pg_default;

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.mounts IS 'Mounting containers and pots for orchid cultivation. Contains base fields plus mount-specific properties following standardized pattern.';

-- CAMPOS BASE (padrão exato de families/genera)
COMMENT ON COLUMN public.mounts.id IS 'Unique identifier for the mount';
COMMENT ON COLUMN public.mounts.user_id IS 'User owner (null = system default)';
COMMENT ON COLUMN public.mounts.name IS 'Mount name';
COMMENT ON COLUMN public.mounts.description IS 'General description of the mount';
COMMENT ON COLUMN public.mounts.is_active IS 'Active status flag';
COMMENT ON COLUMN public.mounts.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.mounts.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.mounts.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.mounts.sync_hash IS 'Synchronization hash for offline sync';

-- CAMPOS ESPECÍFICOS DE MOUNTS
COMMENT ON COLUMN public.mounts.material IS 'Mount material (Plastic, Clay, Wood, Ceramic)';
COMMENT ON COLUMN public.mounts.size IS 'Mount size specification (4 inch, 6 inch, Large, Small)';
COMMENT ON COLUMN public.mounts.drainage_type IS 'Drainage system type (Multiple holes, Slotted, Basket weave)';

-- Enable Row Level Security
ALTER TABLE public.mounts ENABLE ROW LEVEL SECURITY;

-- Create RLS policy for mounts (same pattern as other tables)
CREATE POLICY mounts_policy ON public.mounts
    FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);

-- Grant permissions (following same pattern as other tables)
GRANT ALL ON public.mounts TO authenticated;
GRANT ALL ON public.mounts TO service_role;