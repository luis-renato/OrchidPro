-- ======================================
-- Substrates Table Schema for OrchidPro - STANDARDIZED
-- Growing substrates and media mixtures
-- Apenas campos base padronizados + campos específicos de substrato
-- ======================================

-- Create substrates table
CREATE TABLE IF NOT EXISTS public.substrates (
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
    
    -- Substrate-specific fields
    components TEXT NULL, -- 'Pine Bark 70%, Perlite 20%, Moss 10%'
    ph_range VARCHAR(50) NULL, -- '5.5-6.5', '6.0-7.0'
    drainage_level VARCHAR(50) NULL, -- 'High', 'Medium', 'Low'
    supplier VARCHAR(255) NULL, -- Supplier or brand name
    
    CONSTRAINT substrates_pkey PRIMARY KEY (id),
    CONSTRAINT substrates_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT substrates_name_not_empty CHECK (length(trim(name)) > 0),
    CONSTRAINT substrates_name_user_key UNIQUE (name, user_id)
) TABLESPACE pg_default;

-- ÍNDICES DE PERFORMANCE (campos base - padrão das outras tabelas)
CREATE INDEX IF NOT EXISTS idx_substrates_user_id ON public.substrates USING btree (user_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_substrates_name ON public.substrates USING btree (name) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_substrates_active ON public.substrates USING btree (is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_substrates_is_favorite ON public.substrates USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE (is_favorite = true);
CREATE INDEX IF NOT EXISTS idx_substrates_user_active ON public.substrates USING btree (user_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_substrates_favorites ON public.substrates USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE ((is_favorite = true) AND (user_id IS NOT NULL));

-- ÍNDICES ESPECÍFICOS DE SUBSTRATES
CREATE INDEX IF NOT EXISTS idx_substrates_supplier ON public.substrates USING btree (supplier) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_substrates_drainage ON public.substrates USING btree (drainage_level) TABLESPACE pg_default;

-- ÍNDICE DE BUSCA TEXTUAL (name + description + components)
CREATE INDEX IF NOT EXISTS idx_substrates_search ON public.substrates USING gin (
    to_tsvector('english'::regconfig, (((name)::text || ' '::text) || COALESCE(description, ''::text) || ' '::text || COALESCE(components, ''::text)))
) TABLESPACE pg_default;

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.substrates IS 'Growing substrates and media mixtures for orchid cultivation. Contains base fields plus substrate-specific properties following standardized pattern.';

-- CAMPOS BASE (padrão exato de families/genera)
COMMENT ON COLUMN public.substrates.id IS 'Unique identifier for the substrate';
COMMENT ON COLUMN public.substrates.user_id IS 'User owner (null = system default)';
COMMENT ON COLUMN public.substrates.name IS 'Substrate name';
COMMENT ON COLUMN public.substrates.description IS 'General description of the substrate';
COMMENT ON COLUMN public.substrates.is_active IS 'Active status flag';
COMMENT ON COLUMN public.substrates.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.substrates.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.substrates.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.substrates.sync_hash IS 'Synchronization hash for offline sync';

-- CAMPOS ESPECÍFICOS DE SUBSTRATES
COMMENT ON COLUMN public.substrates.components IS 'Substrate composition (e.g., Pine Bark 70%, Perlite 20%, Moss 10%)';
COMMENT ON COLUMN public.substrates.ph_range IS 'pH range specification (e.g., 5.5-6.5, 6.0-7.0)';
COMMENT ON COLUMN public.substrates.drainage_level IS 'Drainage level classification (High, Medium, Low)';
COMMENT ON COLUMN public.substrates.supplier IS 'Supplier or brand name';

-- Enable Row Level Security
ALTER TABLE public.substrates ENABLE ROW LEVEL SECURITY;

-- Create RLS policy for substrates (same pattern as other tables)
CREATE POLICY substrates_policy ON public.substrates
    FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);

-- Grant permissions (following same pattern as other tables)
GRANT ALL ON public.substrates TO authenticated;
GRANT ALL ON public.substrates TO service_role;