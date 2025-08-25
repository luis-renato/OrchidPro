-- ===================
-- SOURCES TABLE
-- ===================
CREATE TABLE IF NOT EXISTS public.sources (
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
    supplier_type VARCHAR(100) NULL, -- 'Nursery', 'Private', 'Exchange', 'Wild'
    contact_info TEXT NULL,
    website VARCHAR(255) NULL,
    
    CONSTRAINT sources_pkey PRIMARY KEY (id),
    CONSTRAINT sources_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT sources_name_user_key UNIQUE (name, user_id)
) TABLESPACE pg_default;

-- ÍNDICES DE PERFORMANCE (padrão estabelecido)
CREATE INDEX IF NOT EXISTS idx_sources_user_id ON public.sources USING btree (user_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_sources_name ON public.sources USING btree (name) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_sources_active ON public.sources USING btree (is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_sources_is_favorite ON public.sources USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE (is_favorite = true);
CREATE INDEX IF NOT EXISTS idx_sources_user_active ON public.sources USING btree (user_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_sources_favorites ON public.sources USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE ((is_favorite = true) AND (user_id IS NOT NULL));

-- ÍNDICE DE BUSCA TEXTUAL (padrão estabelecido)
CREATE INDEX IF NOT EXISTS idx_sources_search ON public.sources USING gin (
    to_tsvector('english'::regconfig, (((name)::text || ' '::text) || COALESCE(description, ''::text)))
) TABLESPACE pg_default;

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.sources IS 'Plant acquisition sources and suppliers. Contains base fields plus supplier-specific information.';
COMMENT ON COLUMN public.sources.id IS 'Unique identifier for the source';
COMMENT ON COLUMN public.sources.user_id IS 'User owner (null = system default)';
COMMENT ON COLUMN public.sources.name IS 'Source name';
COMMENT ON COLUMN public.sources.description IS 'General description of the source';
COMMENT ON COLUMN public.sources.supplier_type IS 'Type of supplier (Nursery, Private, Exchange, Wild)';
COMMENT ON COLUMN public.sources.contact_info IS 'Contact information for the source';
COMMENT ON COLUMN public.sources.website IS 'Website URL if available';

-- Enable Row Level Security
ALTER TABLE public.sources ENABLE ROW LEVEL SECURITY;

-- Create RLS policy (padrão estabelecido)
CREATE POLICY sources_policy ON public.sources
    FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);

-- Grant permissions (padrão estabelecido)
GRANT ALL ON public.sources TO authenticated;
GRANT ALL ON public.sources TO service_role;