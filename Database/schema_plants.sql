CREATE TABLE IF NOT EXISTS public.plants (
    -- IDENTIFICAÇÃO ESSENCIAL APENAS
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    species_id UUID NOT NULL,
    variant_id UUID NULL,
    plant_code VARCHAR(50) NOT NULL,
    common_name VARCHAR(255) NULL,
    
    -- CAMPOS BASE PADRÃO
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    
    CONSTRAINT plants_pkey PRIMARY KEY (id),
    CONSTRAINT plants_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT plants_species_id_fkey FOREIGN KEY (species_id) REFERENCES species(id) ON DELETE RESTRICT,
    CONSTRAINT plants_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES variants(id) ON DELETE SET NULL,
    CONSTRAINT plants_user_code_unique UNIQUE (user_id, plant_code)
) TABLESPACE pg_default;

-- Índices
CREATE INDEX IF NOT EXISTS idx_plants_user_id ON public.plants USING btree (user_id);
CREATE INDEX IF NOT EXISTS idx_plants_species_id ON public.plants USING btree (species_id);
CREATE INDEX IF NOT EXISTS idx_plants_plant_code ON public.plants USING btree (plant_code);

-- COMENTÁRIOS PARA DOCUMENTAÇÃO
COMMENT ON TABLE public.plants IS 'Individual plant instances owned by users. Minimalist event-sourced design with essential identification fields only.';

-- CAMPOS DE IDENTIFICAÇÃO
COMMENT ON COLUMN public.plants.id IS 'Unique identifier for the plant instance';
COMMENT ON COLUMN public.plants.user_id IS 'User owner of this plant (required)';
COMMENT ON COLUMN public.plants.species_id IS 'Species reference for botanical classification';
COMMENT ON COLUMN public.plants.variant_id IS 'Optional variant specification for cultivars/clones';
COMMENT ON COLUMN public.plants.plant_code IS 'User-defined identification code (unique per user)';
COMMENT ON COLUMN public.plants.common_name IS 'User-given common name for this specific plant';

-- CAMPOS BASE PADRÃO
COMMENT ON COLUMN public.plants.is_active IS 'Active status flag';
COMMENT ON COLUMN public.plants.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.plants.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.plants.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.plants.sync_hash IS 'Synchronization hash for offline sync';

-- RLS
ALTER TABLE public.plants ENABLE ROW LEVEL SECURITY;
CREATE POLICY plants_policy ON public.plants FOR ALL USING (user_id = auth.uid());
GRANT ALL ON public.plants TO authenticated;
GRANT ALL ON public.plants TO service_role;