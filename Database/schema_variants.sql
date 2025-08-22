-- ======================================
-- Variants Table Schema for OrchidPro - INDEPENDENT ENTITY
-- Variações independentes que podem ser aplicadas a qualquer planta
-- Apenas campos base (seguindo exato padrão de families/genera/species)
-- ======================================

-- Create variants table as independent entity (not related to species)
CREATE TABLE IF NOT EXISTS public.variants (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    
    CONSTRAINT variants_pkey PRIMARY KEY (id),
    CONSTRAINT variants_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT variants_name_user_key UNIQUE (name, user_id)
) TABLESPACE pg_default;

-- ÍNDICES DE PERFORMANCE (campos base - padrão das outras tabelas)
CREATE INDEX IF NOT EXISTS idx_variants_user_id ON public.variants USING btree (user_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_variants_name ON public.variants USING btree (name) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_variants_active ON public.variants USING btree (is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_variants_is_favorite ON public.variants USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE (is_favorite = true);
CREATE INDEX IF NOT EXISTS idx_variants_user_active ON public.variants USING btree (user_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_variants_favorites ON public.variants USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE ((is_favorite = true) AND (user_id IS NOT NULL));

-- ÍNDICE DE BUSCA TEXTUAL (name + description)
CREATE INDEX IF NOT EXISTS idx_variants_search ON public.variants USING gin (
    to_tsvector('english'::regconfig, (((name)::text || ' '::text) || COALESCE(description, ''::text)))
) TABLESPACE pg_default;

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.variants IS 'Independent plant variants that can be applied to any plant. Contains only base fields following exact pattern of families/genera/species.';

-- CAMPOS BASE (padrão exato de families/genera/species)
COMMENT ON COLUMN public.variants.id IS 'Unique identifier for the variant';
COMMENT ON COLUMN public.variants.user_id IS 'User owner (null = system default)';
COMMENT ON COLUMN public.variants.name IS 'Variant name (independent, can be used across different plants)';
COMMENT ON COLUMN public.variants.description IS 'General description of the variant';
COMMENT ON COLUMN public.variants.is_active IS 'Active status flag';
COMMENT ON COLUMN public.variants.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.variants.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.variants.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.variants.sync_hash IS 'Synchronization hash for offline sync';

-- Enable Row Level Security
ALTER TABLE public.variants ENABLE ROW LEVEL SECURITY;

-- Create RLS policy for variants (same pattern as other tables)
CREATE POLICY variants_policy ON public.variants
    FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);

-- Grant permissions (following same pattern as other tables)
GRANT ALL ON public.variants TO authenticated;
GRANT ALL ON public.variants TO service_role;