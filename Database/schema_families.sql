-- ======================================
-- Families Table Schema for OrchidPro - STANDARDIZED
-- Botanical families (root level of hierarchy)
-- Apenas campos base padronizados
-- ======================================

-- Create families table (root of botanical hierarchy)
CREATE TABLE IF NOT EXISTS public.families (
    -- CAMPOS BASE (padrão exato de species/variants) - TODOS OBRIGATÓRIOS
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    
    CONSTRAINT families_pkey PRIMARY KEY (id),
    CONSTRAINT families_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT families_name_user_key UNIQUE (name, user_id)
) TABLESPACE pg_default;

-- ÍNDICES DE PERFORMANCE (campos base - padrão das outras tabelas)
CREATE INDEX IF NOT EXISTS idx_families_user_id ON public.families USING btree (user_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_families_name ON public.families USING btree (name) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_families_active ON public.families USING btree (is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_families_is_favorite ON public.families USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE (is_favorite = true);
CREATE INDEX IF NOT EXISTS idx_families_user_active ON public.families USING btree (user_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_families_favorites ON public.families USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE ((is_favorite = true) AND (user_id IS NOT NULL));

-- ÍNDICE DE BUSCA TEXTUAL (name + description)
CREATE INDEX IF NOT EXISTS idx_families_search ON public.families USING gin (
    to_tsvector('english'::regconfig, (((name)::text || ' '::text) || COALESCE(description, ''::text)))
) TABLESPACE pg_default;

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.families IS 'Botanical families (root level of hierarchy). Contains only base fields following standardized pattern.';

-- CAMPOS BASE (padrão exato de species/variants)
COMMENT ON COLUMN public.families.id IS 'Unique identifier for the family';
COMMENT ON COLUMN public.families.user_id IS 'User owner (null = system default)';
COMMENT ON COLUMN public.families.name IS 'Family name';
COMMENT ON COLUMN public.families.description IS 'General description of the family';
COMMENT ON COLUMN public.families.is_active IS 'Active status flag';
COMMENT ON COLUMN public.families.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.families.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.families.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.families.sync_hash IS 'Synchronization hash for offline sync';

-- Enable Row Level Security
ALTER TABLE public.families ENABLE ROW LEVEL SECURITY;

-- Create RLS policy for families (same pattern as other tables)
CREATE POLICY families_policy ON public.families
    FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);

-- Grant permissions (following same pattern as other tables)
GRANT ALL ON public.families TO authenticated;
GRANT ALL ON public.families TO service_role;