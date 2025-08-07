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