create table public.families (
  id uuid not null default gen_random_uuid (),
  user_id uuid null,
  name character varying(255) not null,
  description text null,
  is_active boolean null default true,
  created_at timestamp with time zone null default now(),
  updated_at timestamp with time zone null default now(),
  sync_hash character varying(255) null,
  is_favorite boolean null default false,
  constraint families_pkey primary key (id),
  constraint families_user_id_fkey foreign KEY (user_id) references auth.users (id) on delete CASCADE
) TABLESPACE pg_default;
create index IF not exists idx_families_user_id on public.families using btree (user_id) TABLESPACE pg_default;
create index IF not exists idx_families_name on public.families using btree (name) TABLESPACE pg_default;
create index IF not exists idx_families_active on public.families using btree (is_active) TABLESPACE pg_default;
create index IF not exists idx_families_is_favorite on public.families using btree (user_id, is_favorite) TABLESPACE pg_default
where
  (is_favorite = true);
create index IF not exists idx_families_favorites on public.families using btree (user_id, is_favorite) TABLESPACE pg_default
where
  (
    (is_favorite = true)
    and (user_id is not null)
  );
create index IF not exists idx_families_name_text on public.families using gin (to_tsvector('english'::regconfig, (name)::text)) TABLESPACE pg_default;
create index IF not exists idx_families_user_active_name on public.families using btree (user_id, is_active, name) TABLESPACE pg_default
where
  (user_id is not null);
create unique INDEX IF not exists families_name_user_id_unique on public.families using btree (
  name,
  COALESCE(
    user_id,
    '00000000-0000-0000-0000-000000000000'::uuid
  )
) TABLESPACE pg_default;
create table public.genera (
  id uuid not null default gen_random_uuid (),
  family_id uuid not null,
  user_id uuid null,
  name character varying(255) not null,
  description text null,
  is_active boolean null default true,
  is_favorite boolean null default false,
  created_at timestamp with time zone null default now(),
  updated_at timestamp with time zone null default now(),
  sync_hash character varying(255) null,
  constraint genera_pkey primary key (id),
  constraint genera_name_family_user_key unique (name, family_id, user_id),
  constraint genera_family_id_fkey foreign KEY (family_id) references families (id) on delete CASCADE,
  constraint genera_user_id_fkey foreign KEY (user_id) references auth.users (id) on delete CASCADE
) TABLESPACE pg_default;
create index IF not exists idx_genera_user_id on public.genera using btree (user_id) TABLESPACE pg_default;
create index IF not exists idx_genera_family_id on public.genera using btree (family_id) TABLESPACE pg_default;
create index IF not exists idx_genera_name on public.genera using btree (name) TABLESPACE pg_default;
create index IF not exists idx_genera_active on public.genera using btree (is_active) TABLESPACE pg_default;
create index IF not exists idx_genera_is_favorite on public.genera using btree (user_id, is_favorite) TABLESPACE pg_default
where
  (is_favorite = true);
create index IF not exists idx_genera_user_active on public.genera using btree (user_id, is_active) TABLESPACE pg_default;
create index IF not exists idx_genera_name_search on public.genera using gin (
  to_tsvector(
    'english'::regconfig,
    (
      ((name)::text || ' '::text) || COALESCE(description, ''::text)
    )
  )
) TABLESPACE pg_default;


-- ======================================
-- Species Table Schema for OrchidPro - VERSÃO COMPLETA
-- Hierarchical relationship: Family -> Genus -> Species
-- Campos base (obrigatórios): id, genus_id, user_id, name, description, is_active, is_favorite, created_at, updated_at, sync_hash
-- Campos específicos (opcionais): scientific_name, common_name, cultivation_notes, habitat_info, flowering_season, flower_colors, size_category, rarity_status
-- ======================================

-- Create species table with hierarchical relationship to genera
CREATE TABLE IF NOT EXISTS public.species (
    -- CAMPOS BASE (seguem padrão families/genera) - OBRIGATÓRIOS
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    genus_id UUID NOT NULL,
    user_id UUID NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    is_active BOOLEAN NULL DEFAULT true,
    is_favorite BOOLEAN NULL DEFAULT false,
    created_at TIMESTAMPTZ NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL DEFAULT NOW(),
    sync_hash VARCHAR(255) NULL,
    
    -- CAMPOS ESPECÍFICOS PARA SPECIES - OPCIONAIS (você decide quais manter)
    scientific_name VARCHAR(500) NULL COMMENT 'Nome científico completo (ex: Cattleya mossiae)',
    common_name VARCHAR(255) NULL COMMENT 'Nome popular ou comercial',
    cultivation_notes TEXT NULL COMMENT 'Notas detalhadas de cultivo',
    habitat_info VARCHAR(1000) NULL COMMENT 'Informações sobre habitat natural',
    flowering_season VARCHAR(100) NULL COMMENT 'Época de floração (ex: Primavera, Verão)',
    flower_colors VARCHAR(200) NULL COMMENT 'Cores das flores (separadas por vírgula)',
    size_category VARCHAR(50) DEFAULT 'Medium' COMMENT 'Categoria de tamanho: Miniature, Small, Medium, Large, Giant',
    rarity_status VARCHAR(50) DEFAULT 'Common' COMMENT 'Status de raridade: Common, Uncommon, Rare, Very Rare, Extinct',
    
    -- CAMPOS EXTRAS QUE PODEM SER ÚTEIS - OPCIONAIS
    fragrance BOOLEAN NULL DEFAULT false COMMENT 'Se a espécie tem fragrância',
    temperature_preference VARCHAR(50) NULL COMMENT 'Preferência de temperatura: Cool, Intermediate, Warm',
    light_requirements VARCHAR(50) NULL COMMENT 'Necessidades de luz: Low, Medium, High, Very High',
    humidity_preference VARCHAR(50) NULL COMMENT 'Preferência de umidade: Low, Medium, High',
    growth_habit VARCHAR(50) NULL COMMENT 'Hábito de crescimento: Epiphyte, Terrestrial, Lithophyte',
    bloom_duration VARCHAR(50) NULL COMMENT 'Duração da floração em dias/semanas',
    
    CONSTRAINT species_pkey PRIMARY KEY (id),
    CONSTRAINT species_genus_id_fkey FOREIGN KEY (genus_id) REFERENCES public.genera(id) ON DELETE CASCADE,
    CONSTRAINT species_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
    CONSTRAINT species_name_genus_user_key UNIQUE (name, genus_id, user_id)
) TABLESPACE pg_default;

-- ÍNDICES DE PERFORMANCE (campos base - sempre necessários)
CREATE INDEX IF NOT EXISTS idx_species_user_id ON public.species USING btree (user_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_species_genus_id ON public.species USING btree (genus_id) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_species_name ON public.species USING btree (name) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_species_active ON public.species USING btree (is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_species_is_favorite ON public.species USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE (is_favorite = true);
CREATE INDEX IF NOT EXISTS idx_species_user_active ON public.species USING btree (user_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_species_user_genus_active ON public.species USING btree (user_id, genus_id, is_active) TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_species_favorites ON public.species USING btree (user_id, is_favorite) TABLESPACE pg_default WHERE ((is_favorite = true) AND (user_id IS NOT NULL));

-- ÍNDICES PARA CAMPOS ESPECÍFICOS (apenas se você mantiver os campos)
CREATE INDEX IF NOT EXISTS idx_species_scientific_name ON public.species USING btree (scientific_name) TABLESPACE pg_default WHERE scientific_name IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_species_rarity ON public.species USING btree (rarity_status) TABLESPACE pg_default WHERE rarity_status IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_species_size ON public.species USING btree (size_category) TABLESPACE pg_default WHERE size_category IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_species_flowering ON public.species USING btree (flowering_season) TABLESPACE pg_default WHERE flowering_season IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_species_temperature ON public.species USING btree (temperature_preference) TABLESPACE pg_default WHERE temperature_preference IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_species_growth_habit ON public.species USING btree (growth_habit) TABLESPACE pg_default WHERE growth_habit IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_species_fragrance ON public.species USING btree (fragrance) TABLESPACE pg_default WHERE fragrance = true;

-- ÍNDICES DE BUSCA TEXTUAL
-- Busca básica (name + description) - sempre necessário
CREATE INDEX IF NOT EXISTS idx_species_basic_search ON public.species USING gin (
    to_tsvector('english'::regconfig, (((name)::text || ' '::text) || COALESCE(description, ''::text)))
) TABLESPACE pg_default;

-- Busca completa (todos os campos de texto) - apenas se mantiver os campos
CREATE INDEX IF NOT EXISTS idx_species_full_search ON public.species USING gin (
    to_tsvector('english'::regconfig, (
        ((name)::text || ' '::text) || 
        COALESCE(scientific_name, ''::text) || ' '::text ||
        COALESCE(common_name, ''::text) || ' '::text ||
        COALESCE(description, ''::text)
    ))
) TABLESPACE pg_default;

-- Busca em notas de cultivo - apenas se mantiver cultivation_notes e habitat_info
CREATE INDEX IF NOT EXISTS idx_species_cultivation_search ON public.species USING gin (
    to_tsvector('english'::regconfig, (
        COALESCE(cultivation_notes, ''::text) || ' '::text ||
        COALESCE(habitat_info, ''::text)
    ))
) TABLESPACE pg_default;

-- COMENTÁRIOS DETALHADOS PARA ANÁLISE
COMMENT ON TABLE public.species IS 'Botanical species with hierarchical relationship to genera. Contains base fields (required) and species-specific fields (optional for analysis).';

-- CAMPOS BASE (obrigatórios - seguem padrão families/genera)
COMMENT ON COLUMN public.species.id IS 'Unique identifier for the species';
COMMENT ON COLUMN public.species.genus_id IS 'Foreign key linking to genus (hierarchical relationship)';
COMMENT ON COLUMN public.species.user_id IS 'User owner (null = system default)';
COMMENT ON COLUMN public.species.name IS 'Species name (simplified, follows families/genera pattern)';
COMMENT ON COLUMN public.species.description IS 'General description of the species';
COMMENT ON COLUMN public.species.is_active IS 'Active status flag';
COMMENT ON COLUMN public.species.is_favorite IS 'User favorite flag';
COMMENT ON COLUMN public.species.created_at IS 'Creation timestamp';
COMMENT ON COLUMN public.species.updated_at IS 'Last update timestamp';
COMMENT ON COLUMN public.species.sync_hash IS 'Synchronization hash for offline sync';

-- CAMPOS ESPECÍFICOS PARA SPECIES (opcionais - para sua análise)
COMMENT ON COLUMN public.species.scientific_name IS 'OPCIONAL: Nome científico completo (ex: Cattleya mossiae) - útil para precisão botânica';
COMMENT ON COLUMN public.species.common_name IS 'OPCIONAL: Nome popular/comercial - útil para usuários leigos';
COMMENT ON COLUMN public.species.cultivation_notes IS 'OPCIONAL: Notas detalhadas de cultivo - muito útil para orquidófilos';
COMMENT ON COLUMN public.species.habitat_info IS 'OPCIONAL: Informações sobre habitat natural - educacional';
COMMENT ON COLUMN public.species.flowering_season IS 'OPCIONAL: Época de floração - útil para planejamento';
COMMENT ON COLUMN public.species.flower_colors IS 'OPCIONAL: Cores das flores - visual/estético';
COMMENT ON COLUMN public.species.size_category IS 'OPCIONAL: Categoria de tamanho (Miniature/Small/Medium/Large/Giant) - útil para espaço';
COMMENT ON COLUMN public.species.rarity_status IS 'OPCIONAL: Status de raridade - útil para colecionadores';

-- CAMPOS EXTRAS DE CULTIVO (opcionais - mais especializados)
COMMENT ON COLUMN public.species.fragrance IS 'OPCIONAL: Se tem fragrância - característica importante';
COMMENT ON COLUMN public.species.temperature_preference IS 'OPCIONAL: Preferência temperatura (Cool/Intermediate/Warm) - cultivo';
COMMENT ON COLUMN public.species.light_requirements IS 'OPCIONAL: Necessidades de luz - cultivo';
COMMENT ON COLUMN public.species.humidity_preference IS 'OPCIONAL: Preferência umidade - cultivo';
COMMENT ON COLUMN public.species.growth_habit IS 'OPCIONAL: Hábito crescimento (Epiphyte/Terrestrial/Lithophyte) - botânico';
COMMENT ON COLUMN public.species.bloom_duration IS 'OPCIONAL: Duração da floração - planejamento';

-- Enable Row Level Security
ALTER TABLE public.species ENABLE ROW LEVEL SECURITY;

-- Create RLS policy for species (same pattern as families and genera)
CREATE POLICY species_policy ON public.species
    FOR ALL USING (user_id = auth.uid() OR user_id IS NULL);

-- Grant permissions (following same pattern as other tables)
GRANT ALL ON public.species TO authenticated;
GRANT ALL ON public.species TO service_role;