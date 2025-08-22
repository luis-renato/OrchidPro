-- ======================================
-- Genera Table Schema for OrchidPro - STANDARDIZED
-- Botanical genera (intermediate level: Family -> Genus -> Species)
-- Apenas campos base padronizados + family_id para hierarquia
-- ======================================

-- Create genera table with hierarchical relationship to families
CREATE TABLE IF NOT EXISTS public.genera (
    -- CAMPOS BASE (padrão exato de species/variants) - TODOS OBRIGATÓRIOS
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    family_id UUID NOT NULL,
    user_id UUID NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    
    CONSTRAINT genera_pkey PRIMARY KEY (id),
    CONSTRAINT genera_family_id_fkey FOREIGN KEY (family_id) REFERENCES public.families(id) ON DELETE CASCADE,
    CONSTRAINT genera_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT genera_name_family_user_key UNIQUE (name, family_id, user_id)
) TABLESPACE pg_default;

-- ÍNDICES DE PERFORMANCE (campos base - padrão das outras tabelas)
CREATE INDEX IF NOT EXISTS idx_genera_user_id ON public.genera USING btree (user_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_genera_family_id ON public.genera USING btree (family_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_genera_name ON public.genera USING btree (name) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_genera_active ON public.genera USING btree (is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_genera_is_favorite ON public.genera USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE (is_favorite = true);
CREATE INDEX IF NOT EXISTS idx_genera_user_active ON public.genera USING btree (user_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_genera_user_family_active ON public.genera USING btree (user_id, family_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_genera_favorites ON public.genera USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE ((is_favorite = true) AND (user_id IS NOT NULL));

-- ÍNDICE DE BUSCA TEXTUAL (name + description)
CREATE INDEX IF NOT EXISTS idx_genera_search ON public.genera USING gin (
    to_tsvector('english'::regconfig, (((name)::text || ' '::text) || COALESCE(description, ''::text)))
) TABLESPACE pg_default;

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.genera IS 'Botanical genera with hierarchical relationship to families. Contains only base fields following standardized pattern.';

-- CAMPOS BASE (padrão exato de species/variants)
COMMENT ON COLUMN public.genera.id IS 'Unique identifier for the genus';
COMMENT ON COLUMN public.genera.family_id IS 'Foreign key linking to family (hierarchical relationship)';
COMMENT ON COLUMN public.genera.user_id IS 'User owner (null = system default)';
COMMENT ON COLUMN public.genera.name IS 'Genus name';
COMMENT ON COLUMN public.genera.description IS 'General description of the genus';
COMMENT ON COLUMN public.genera.is_active IS 'Active status flag';
COMMENT ON COLUMN public.genera.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.genera.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.genera.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.genera.sync_hash IS 'Synchronization hash for offline sync';

-- Enable Row Level Security
ALTER TABLE public.genera ENABLE ROW LEVEL SECURITY;

-- Create RLS policy for genera (same pattern as other tables)
CREATE POLICY genera_policy ON public.genera
    FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);

-- Grant permissions (following same pattern as other tables)
GRANT ALL ON public.genera TO authenticated;
GRANT ALL ON public.genera TO service_role;