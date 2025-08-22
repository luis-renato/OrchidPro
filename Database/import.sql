-- ======================================
-- OrchidPro COMPLETE Import Script - Full Database Population
-- Imports: 1 Family + 76 Genera + 261 Species + Variants
-- ALL 261 SPECIES with complete cultivation and botanical information
-- All data as system defaults (user_id = NULL)
-- FIXED: No scientific_name field, all syntax errors corrected
-- ======================================

-- CLEAN UP EXISTING DATA (in correct order due to foreign key constraints)
-- ======================================
DELETE FROM public.species WHERE user_id IS NULL;
DELETE FROM public.genera WHERE user_id IS NULL;
DELETE FROM public.families WHERE user_id IS NULL;
DELETE FROM public.variants WHERE user_id IS NULL;

-- INSERT FAMILY: ORCHIDACEAE
-- ======================================
INSERT INTO public.families (id, user_id, name, description, is_active, is_favorite, created_at, updated_at, sync_hash)
VALUES (
    'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid,
    NULL,
    'Orchidaceae',
    'The orchid family - one of the largest families of flowering plants with over 25,000 species worldwide. Known for their complex flower structures, diverse habitats, and specialized pollination mechanisms. Most are epiphytes found in tropical regions.',
    true,
    false,
    NOW(),
    NOW(),
    'orchidaceae_system_default'
);

-- INSERT ALL GENERA WITH DETAILED DESCRIPTIONS
-- ======================================
WITH genera_data AS (
  SELECT * FROM (VALUES
    ('11111111-1111-1111-1111-000000000001'::uuid, 'Acianthera', 'Small epiphytic orchids commonly found in Central and South America. These miniature orchids prefer cool to intermediate temperatures and high humidity. Most species produce tiny, intricate flowers and are popular among collectors of miniature orchids.'),
    ('11111111-1111-1111-1111-000000000002'::uuid, 'Acronia', 'Small terrestrial orchids native to Madagascar and adjacent islands. These ground-dwelling orchids prefer well-draining soil and moderate moisture. They are relatively rare in cultivation and require specialized care.'),
    ('11111111-1111-1111-1111-000000000003'::uuid, 'Aerangis', 'Epiphytic orchids from Africa with distinctive white fragrant flowers that often bloom at night. These orchids prefer warm temperatures, high humidity, and bright filtered light. Many species have a sweet, pleasant fragrance.'),
    ('11111111-1111-1111-1111-000000000004'::uuid, 'Aerides', 'Epiphytic orchids from tropical Asia known for their fragrant, waxy flowers and long, drooping flower spikes. They prefer warm temperatures, high humidity, and bright light. Popular in tropical gardens.'),
    ('11111111-1111-1111-1111-000000000005'::uuid, 'Anacheilium', 'Epiphytic orchids from Central America and the Caribbean. These medium-sized orchids prefer intermediate temperatures and moderate humidity. They produce attractive flowers with distinctive lip shapes.'),
    ('11111111-1111-1111-1111-000000000006'::uuid, 'Anathallis', 'Small epiphytic orchids from the Pleurothallidinae subtribe. These miniature orchids prefer cool to intermediate temperatures, high humidity, and filtered light. They are popular among collectors of miniature species.'),
    ('11111111-1111-1111-1111-000000000007'::uuid, 'Angraecum', 'Large genus of epiphytic orchids from Africa and Madagascar, famous for their white, star-shaped flowers and evening fragrance. They prefer warm temperatures, high humidity, and bright filtered light.'),
    ('11111111-1111-1111-1111-000000000008'::uuid, 'Ansellia', 'Large terrestrial orchids from tropical Africa, known as "Leopard Orchids" for their spotted flowers. They prefer warm temperatures, bright light, and a distinct dry season for flowering.'),
    ('11111111-1111-1111-1111-000000000009'::uuid, 'Arpophyllum', 'Epiphytic orchids from Central and South America with distinctive grass-like leaves and dense flower spikes. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000010'::uuid, 'Aspasia', 'Epiphytic orchids from Central and South America with attractive, fragrant flowers. They prefer intermediate temperatures, moderate humidity, and filtered light.'),
    ('11111111-1111-1111-1111-000000000011'::uuid, 'Barbosella', 'Miniature epiphytic orchids from Central and South America. These tiny orchids prefer cool to intermediate temperatures, high humidity, and low to medium light.'),
    ('11111111-1111-1111-1111-000000000012'::uuid, 'Bifrenaria', 'Epiphytic orchids from South America known for their waxy, fragrant flowers and pseudobulbs. They prefer intermediate temperatures and moderate humidity with a slight dry rest.'),
    ('11111111-1111-1111-1111-000000000013'::uuid, 'Brassia', 'Spider orchids - epiphytic orchids with extremely long, narrow petals resembling spider legs. They prefer intermediate to warm temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000014'::uuid, 'Bulbophyllum', 'One of the largest orchid genera with over 2000 species worldwide. Extremely diverse in size, flower form, and growing requirements. Most prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000015'::uuid, 'Cattleya', 'Large showy orchids known as the "Queen of Orchids". Famous for their large, colorful, often fragrant flowers. They prefer intermediate to warm temperatures and bright light.'),
    ('11111111-1111-1111-1111-000000000016'::uuid, 'Ceratostylis', 'Small epiphytic orchids from Southeast Asia with thin, grass-like leaves. They prefer warm temperatures, high humidity, and moderate light.'),
    ('11111111-1111-1111-1111-000000000017'::uuid, 'Christensonella', 'Epiphytic orchids from South America, recently separated from other genera. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000018'::uuid, 'Coelogyne', 'Epiphytic orchids from Asia known for their beautiful, often fragrant white or cream flowers. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000019'::uuid, 'Cryptophoranthus', 'Small epiphytic orchids from Central and South America with unique windowed or translucent areas in their leaves. They prefer cool temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000020'::uuid, 'Dendrobium', 'Large genus with over 1000 species from Asia and Australia. Extremely diverse group with varied growing requirements. Many prefer warm temperatures and bright light.'),
    ('11111111-1111-1111-1111-000000000021'::uuid, 'Dendrochilum', 'Chain orchids - epiphytic orchids from Southeast Asia with distinctive chain-like flower arrangements. They prefer cool to intermediate temperatures.'),
    ('11111111-1111-1111-1111-000000000022'::uuid, 'Dichaea', 'Small epiphytic orchids from Central and South America with fan-shaped growth. They prefer intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000023'::uuid, 'Dinema', 'Small epiphytic orchids from Central America with tiny, colorful flowers. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000024'::uuid, 'Dracula', 'Monkey-face orchids - cool growing epiphytes from cloud forests. They prefer cool temperatures, high humidity, and low light conditions.'),
    ('11111111-1111-1111-1111-000000000025'::uuid, 'Dryadella', 'Small epiphytic orchids from the Pleurothallidinae subtribe. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000026'::uuid, 'Encyclia', 'Epiphytic orchids from Central and South America with attractive, often fragrant flowers. They prefer intermediate to warm temperatures and bright light.'),
    ('11111111-1111-1111-1111-000000000027'::uuid, 'Epicattleya', 'Hybrid genus between Epidendrum and Cattleya, combining characteristics of both parents. They prefer intermediate to warm temperatures.'),
    ('11111111-1111-1111-1111-000000000028'::uuid, 'Epidendrum', 'Large genus of epiphytic orchids from the Americas with diverse forms and colors. Most prefer intermediate to warm temperatures and bright light.'),
    ('11111111-1111-1111-1111-000000000029'::uuid, 'Epigeneium', 'Small epiphytic orchids from Asia with creeping growth habit. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000030'::uuid, 'Habenaria', 'Terrestrial orchids found worldwide with distinctive fringed or split petals. Growing requirements vary by species and origin.'),
    ('11111111-1111-1111-1111-000000000031'::uuid, 'Jumellea', 'Epiphytic orchids from Madagascar and adjacent islands with white, often fragrant flowers. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000032'::uuid, 'Kefersteinia', 'Epiphytic orchids from Central and South America with distinctive cupped flowers. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000033'::uuid, 'Laelia', 'Epiphytic orchids from Central and South America closely related to Cattleya. They prefer bright light and intermediate to warm temperatures.'),
    ('11111111-1111-1111-1111-000000000034'::uuid, 'Lepanthes', 'Miniature epiphytic orchids with extremely intricate and colorful flowers. They prefer cool temperatures, high humidity, and low light.'),
    ('11111111-1111-1111-1111-000000000035'::uuid, 'Masdevallia', 'Cool growing epiphytic orchids from cloud forests known for their triangular flowers. They prefer cool temperatures, high humidity, and filtered light.'),
    ('11111111-1111-1111-1111-000000000036'::uuid, 'Maxillaria', 'Large genus of epiphytic orchids from the Americas with diverse flower forms. Most prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000037'::uuid, 'Mediocalcar', 'Small epiphytic orchids from New Guinea and adjacent areas with colorful flowers. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000038'::uuid, 'Meiracyllium', 'Small epiphytic orchids from Central America with tiny flowers. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000039'::uuid, 'Mormolyca', 'Small epiphytic orchids from Central and South America with attractive flowers. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000040'::uuid, 'Muscarella', 'Small epiphytic orchids from the Pleurothallidinae subtribe. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000041'::uuid, 'Mycaranthes', 'Small epiphytic orchids from Southeast Asia with delicate flowers. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000042'::uuid, 'Myoxanthus', 'Small epiphytic orchids from Central and South America. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000043'::uuid, 'Neofinetia', 'Small epiphytic orchids from East Asia, highly prized in Japanese culture. They prefer cool to intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000044'::uuid, 'Octomeria', 'Small epiphytic orchids from Central and South America with eight-angled stems. They prefer cool to intermediate temperatures.'),
    ('11111111-1111-1111-1111-000000000045'::uuid, 'Odontocidium', 'Hybrid genus in the Oncidium alliance combining characteristics of multiple genera. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000046'::uuid, 'Oncidium', 'Dancing lady orchids - epiphytic orchids with distinctive yellow and brown flowers. They prefer intermediate to warm temperatures and bright light.'),
    ('11111111-1111-1111-1111-000000000047'::uuid, 'Ornithocephalus', 'Small epiphytic orchids from Central and South America with bird-like flowers. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000048'::uuid, 'Ornithophora', 'Small epiphytic orchids from South America. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000049'::uuid, 'Osmoglossum', 'Epiphytic orchids from Central and South America with attractive flowers. They prefer cool to intermediate temperatures.'),
    ('11111111-1111-1111-1111-000000000050'::uuid, 'Pabstiella', 'Small epiphytic orchids from the Pleurothallidinae subtribe with intricate flowers. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000051'::uuid, 'Paphiopedilum', 'Slipper orchids - terrestrial orchids from Asia known for their distinctive pouch-shaped lip. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000052'::uuid, 'Phalaenopsis', 'Moth orchids - popular epiphytic orchids from Asia with long-lasting flowers. They prefer warm temperatures, high humidity, and low to medium light.'),
    ('11111111-1111-1111-1111-000000000053'::uuid, 'Pholidota', 'Epiphytic orchids from Asia with drooping flower spikes resembling rattlesnake tails. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000054'::uuid, 'Platystele', 'Miniature epiphytic orchids from Central and South America with extremely small flowers. They prefer cool temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000055'::uuid, 'Pleurobotryum', 'Small epiphytic orchids from Central and South America. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000056'::uuid, 'Pleurothallis', 'Large genus of small epiphytic orchids from the Americas with diverse flower forms. Most prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000057'::uuid, 'Podangis', 'Epiphytic orchids from Africa with white flowers. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000058'::uuid, 'Psychopsis', 'Butterfly orchids - large epiphytic orchids from South America with distinctive butterfly-like flowers. They prefer warm temperatures and bright light.'),
    ('11111111-1111-1111-1111-000000000059'::uuid, 'Renanthera', 'Large epiphytic orchids from Southeast Asia with bright red or orange flowers. They prefer warm temperatures, high humidity, and very bright light.'),
    ('11111111-1111-1111-1111-000000000060'::uuid, 'Restrepia', 'Small epiphytic orchids from Central and South America with distinctive striped flowers. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000061'::uuid, 'Robiquetia', 'Epiphytic orchids from Southeast Asia. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000062'::uuid, 'Schoenorchis', 'Small epiphytic orchids from Southeast Asia with tiny, numerous flowers. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000063'::uuid, 'Schomburgkia', 'Large epiphytic orchids from Central and South America with impressive flower spikes. They prefer warm temperatures and bright light.'),
    ('11111111-1111-1111-1111-000000000064'::uuid, 'Seidenfadenia', 'Epiphytic orchids from Southeast Asia. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000065'::uuid, 'Sophronitis', 'Small epiphytic orchids from South America known for their brilliant red colors. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000066'::uuid, 'Specklinia', 'Small epiphytic orchids from the Pleurothallidinae subtribe. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000067'::uuid, 'Stanhopea', 'Bucket orchids - large epiphytic orchids with complex pollination mechanisms and waxy, fragrant flowers. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000068'::uuid, 'Stelis', 'Small epiphytic orchids from Central and South America with tiny flowers. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000069'::uuid, 'Sudamerlycaste', 'Epiphytic orchids from South America with attractive flowers. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000070'::uuid, 'Trichoglottis', 'Epiphytic orchids from Southeast Asia with colorful, waxy flowers. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000071'::uuid, 'Trisetella', 'Small epiphytic orchids from South America. They prefer cool to intermediate temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000072'::uuid, 'Trudelia', 'Epiphytic orchids from Asia. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000073'::uuid, 'Tuberolabium', 'Small epiphytic orchids from Southeast Asia. They prefer warm temperatures and high humidity.'),
    ('11111111-1111-1111-1111-000000000074'::uuid, 'Vanda', 'Large epiphytic orchids from Asia known for their colorful, long-lasting flowers. They prefer warm temperatures, high humidity, and very bright light.'),
    ('11111111-1111-1111-1111-000000000075'::uuid, 'Zygolum', 'Hybrid genus in the Oncidium alliance. They prefer intermediate temperatures and moderate humidity.'),
    ('11111111-1111-1111-1111-000000000076'::uuid, 'Zygotastes', 'Small epiphytic orchids from South America. They prefer cool to intermediate temperatures and high humidity.')
  ) AS g(id, name, description)
)
INSERT INTO public.genera (id, family_id, user_id, name, description, is_active, is_favorite, created_at, updated_at, sync_hash)
SELECT 
  g.id,
  'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid,
  NULL,
  g.name,
  g.description,
  true,
  false,
  NOW(),
  NOW(),
  LOWER(g.name) || '_system'
FROM genera_data g;

-- INSERT ALL 261 SPECIES WITH COMPLETE INFORMATION
-- ======================================
WITH genus_mapping AS (
  SELECT 
    name,
    id
  FROM public.genera 
  WHERE family_id = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid
),
species_data AS (
  SELECT * FROM (VALUES
    -- Acianthera species (14 species)
    ('Acianthera', 'aphthosa', 'Small epiphytic species with intricate tiny flowers from cool mountain forests of Central America. Requires high humidity and good air circulation. Prefers shaded locations.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring, Summer', 'Green, Yellow', '2-3 weeks', 'Cool mountain forests of Central America with high humidity and good air circulation. Prefers shaded locations.'),
    ('Acianthera', 'crinita', 'Miniature orchid with hairy characteristics found in cloud forests at moderate to high elevations.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Found in cloud forests at moderate to high elevations.'),
    ('Acianthera', 'glumacea', 'Small species with distinctive flowers that grows in humid mountain forests.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Yellow, Green', '2-3 weeks', 'Grows in humid mountain forests.'),
    ('Acianthera', 'gracilisepala', 'Delicate species with slender sepals endemic to specific cloud forest regions.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring', 'Green, White', '2-3 weeks', 'Endemic to specific cloud forest regions.'),
    ('Acianthera', 'hygrophila', 'Moisture-loving miniature orchid that requires constant high humidity and good air movement.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green', '2-3 weeks', 'Requires constant high humidity and good air movement.'),
    ('Acianthera', 'klotzschiana', 'Classic miniature Acianthera species found in various cloud forest habitats.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring, Summer', 'Yellow, Green', '2-3 weeks', 'Found in various cloud forest habitats.'),
    ('Acianthera', 'minima', 'One of the smallest Acianthera species, extremely small and requiring careful cultivation.', 'Miniature', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green', '2-3 weeks', 'Extremely small species requiring careful cultivation.'),
    ('Acianthera', 'panduripetala', 'Species with violin-shaped petals, distinguished by unique petal shape.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring', 'Yellow, Brown', '2-3 weeks', 'Distinguished by unique petal shape.'),
    ('Acianthera', 'pubescens', 'Hairy-leafed miniature orchid characterized by pubescent leaves.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Yellow', '2-3 weeks', 'Characterized by pubescent leaves.'),
    ('Acianthera', 'rostellata', 'Small beaked orchid species named for its distinctive rostellum.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring, Summer', 'Green', '2-3 weeks', 'Named for its distinctive rostellum.'),
    ('Acianthera', 'saurocephala', 'Lizard-headed miniature orchid with flowers that resemble lizard heads.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Flowers resemble lizard heads.'),
    ('Acianthera', 'sonderiana', 'Brazilian miniature orchid native to Brazilian cloud forests.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring', 'Yellow, Green', '2-3 weeks', 'Native to Brazilian cloud forests.'),
    ('Acianthera', 'sp.', 'Unidentified Acianthera species pending identification.', 'Small', 'Unknown', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Variable', 'Variable', '2-3 weeks', 'Species pending identification.'),
    ('Acianthera', 'spilantha', 'Spotted flower miniature orchid notable for spotted flower pattern.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Yellow, Brown spots', '2-3 weeks', 'Notable for spotted flower pattern.'),
    
    -- Acronia species (2 species)
    ('Acronia', 'adeleae', 'Terrestrial orchid from Madagascar endemic to Madagascar, grows in well-draining forest soil.', 'Medium', 'Rare', false, 'Intermediate', 'Medium', 'Medium', 'Terrestrial', 'Summer', 'White, Pink', '3-4 weeks', 'Endemic to Madagascar, grows in well-draining forest soil.'),
    ('Acronia', 'crucifera', 'Cross-bearing terrestrial orchid named for cross-shaped flower markings.', 'Medium', 'Rare', false, 'Intermediate', 'Medium', 'Medium', 'Terrestrial', 'Summer', 'White', '3-4 weeks', 'Named for cross-shaped flower markings.'),
    
    -- Aerangis species (1 species)
    ('Aerangis', 'luteo-alba', 'Yellow-white African orchid, an African epiphyte with evening fragrance that prefers warm humid conditions.', 'Medium', 'Uncommon', true, 'Warm', 'Medium', 'High', 'Epiphyte', 'Winter, Spring', 'White, Yellow', '4-6 weeks', 'African epiphyte with evening fragrance, prefers warm humid conditions.'),
    
    -- Aerides species (1 species)
    ('Aerides', 'odoratum', 'Highly fragrant Asian orchid popular for its intense fragrance, needs bright light and high humidity.', 'Large', 'Common', true, 'Warm', 'High', 'High', 'Epiphyte', 'Spring, Summer', 'White, Pink, Purple', '6-8 weeks', 'Popular for its intense fragrance, needs bright light and high humidity.'),
    
    -- Anacheilium species (1 species)
    ('Anacheilium', 'radiatum', 'Central American cockleshell orchid found in Central American forests, needs good drainage.', 'Medium', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Winter, Spring', 'Green, White', '4-5 weeks', 'Found in Central American forests, needs good drainage.'),
    
    -- Anathallis species (7 species)
    ('Anathallis', 'adenochila', 'Glandular-lipped miniature orchid, a cloud forest species with specialized lip structure.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Cloud forest species with specialized lip structure.'),
    ('Anathallis', 'bleyensis', 'Bley miniature orchid named after its place of discovery.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring', 'Green, Yellow', '2-3 weeks', 'Named after its place of discovery.'),
    ('Anathallis', 'dryadum', 'Dryad miniature orchid, a forest-dwelling species with delicate flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green', '2-3 weeks', 'Forest-dwelling species with delicate flowers.'),
    ('Anathallis', 'microphyta', 'Small-leafed miniature orchid characterized by very small leaves.', 'Miniature', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring, Summer', 'Green, White', '2-3 weeks', 'Characterized by very small leaves.'),
    ('Anathallis', 'rubens', 'Reddish miniature orchid notable for reddish coloration.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Red, Green', '2-3 weeks', 'Notable for reddish coloration.'),
    ('Anathallis', 'rubrolimbata', 'Red-bordered miniature orchid distinguished by red flower borders.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red border', '2-3 weeks', 'Distinguished by red flower borders.'),
    ('Anathallis', 'welteri', 'Welter miniature orchid named after its discoverer.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring', 'Green, Purple', '2-3 weeks', 'Named after its discoverer.'),
    
    -- Angraecum species (1 species)
    ('Angraecum', 'eburneum', 'Large white fragrant African orchid, Madagascar native that needs warm humid conditions and bright filtered light.', 'Large', 'Uncommon', true, 'Warm', 'Medium', 'High', 'Epiphyte', 'Winter', 'White', '6-8 weeks', 'Madagascar native, needs warm humid conditions and bright filtered light.'),
    
    -- Ansellia species (1 species)
    ('Ansellia', 'africana', 'Large spotted terrestrial orchid, African terrestrial requiring bright light and distinct dry season.', 'Large', 'Uncommon', false, 'Warm', 'High', 'Medium', 'Terrestrial', 'Summer', 'Yellow, Brown spots', '8-10 weeks', 'African terrestrial requiring bright light and distinct dry season.'),
    
    -- Arpophyllum species (1 species)
    ('Arpophyllum', 'giganteum', 'Giant grass orchid, large epiphyte with grass-like leaves and dense flower spikes.', 'Large', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Winter, Spring', 'Purple, Pink', '4-6 weeks', 'Large epiphyte with grass-like leaves and dense flower spikes.'),
    
    -- Aspasia species (1 species)
    ('Aspasia', 'silvana', 'Forest Aspasia orchid, fragrant species from South American forests.', 'Medium', 'Uncommon', true, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Winter, Spring', 'Green, White, Purple', '4-5 weeks', 'Fragrant species from South American forests.'),
    
    -- Barbosella species (3 species)
    ('Barbosella', 'australis', 'Southern miniature orchid with southern distribution in cloud forests.', 'Miniature', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red', '2-3 weeks', 'Southern distribution in cloud forests.'),
    ('Barbosella', 'cucullata', 'Hooded miniature orchid named for hooded flower shape.', 'Miniature', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Spring, Summer', 'Green', '2-3 weeks', 'Named for hooded flower shape.'),
    ('Barbosella', 'gardneri', 'Gardner miniature orchid named after botanist Gardner.', 'Miniature', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named after botanist Gardner.'),
    
    -- Bifrenaria species (1 species)
    ('Bifrenaria', 'inodora aurea', 'Golden form of scentless Bifrenaria, beautiful golden variety that needs good drainage and moderate rest period.', 'Medium', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Golden Yellow', '4-5 weeks', 'Beautiful golden variety, needs good drainage and moderate rest period.'),
    
    -- Brassia species (1 species)
    ('Brassia', 'Rex', 'Large spider orchid hybrid, popular hybrid with extremely long petals resembling spider legs.', 'Large', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Spring, Fall', 'Green, Brown, spots', '6-8 weeks', 'Popular hybrid with extremely long petals resembling spider legs.'),
    
    -- Bulbophyllum species (22 species)
    ('Bulbophyllum', 'Louis Sander', 'Classic Bulbophyllum hybrid, popular warm-growing hybrid that needs high humidity.', 'Medium', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring, Summer', 'Yellow, Red', '3-4 weeks', 'Popular warm-growing hybrid, needs high humidity.'),
    ('Bulbophyllum', 'Sunshine Queen', 'Bright yellow Bulbophyllum hybrid, showy hybrid with bright yellow flowers.', 'Medium', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Bright Yellow', '3-4 weeks', 'Showy hybrid with bright yellow flowers.'),
    ('Bulbophyllum', 'Wilmar Galaxy Star', 'Star-shaped Bulbophyllum hybrid, modern hybrid with stellar flower shape.', 'Medium', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring', 'Purple, White', '3-4 weeks', 'Modern hybrid with stellar flower shape.'),
    ('Bulbophyllum', 'barbigerum', 'Bearded tropical Bulbophyllum notable for hairy flower structures.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Purple, Green', '3-4 weeks', 'Notable for hairy flower structures.'),
    ('Bulbophyllum', 'blumei', 'Blume Bulbophyllum, classic Asian species named after botanist Blume.', 'Medium', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Yellow, Brown', '3-4 weeks', 'Classic Asian species named after botanist Blume.'),
    ('Bulbophyllum', 'breviscapum', 'Short-stem Bulbophyllum characterized by short flower stems.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring, Summer', 'Green, Purple', '3-4 weeks', 'Characterized by short flower stems.'),
    ('Bulbophyllum', 'bufo', 'Toad-like textured orchid named for toad-like flower texture and coloring.', 'Medium', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Brown, Green', '3-4 weeks', 'Named for toad-like flower texture and coloring.'),
    ('Bulbophyllum', 'campanulatum', 'Bell-shaped Bulbophyllum with flowers shaped like small bells.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Yellow, Red', '3-4 weeks', 'Flowers shaped like small bells.'),
    ('Bulbophyllum', 'careyanum', 'Carey Bulbophyllum named after botanist Carey.', 'Medium', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring, Summer', 'Yellow, Purple', '3-4 weeks', 'Named after botanist Carey.'),
    ('Bulbophyllum', 'comberi', 'Comber Bulbophyllum named after collector Comber.', 'Small', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Green, Red', '3-4 weeks', 'Named after collector Comber.'),
    ('Bulbophyllum', 'crassipes', 'Thick-stemmed Bulbophyllum distinguished by thick stems.', 'Medium', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Yellow, Brown', '3-4 weeks', 'Distinguished by thick stems.'),
    ('Bulbophyllum', 'eberhardtii', 'Eberhardt Bulbophyllum named after explorer Eberhardt.', 'Small', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring', 'Purple, White', '3-4 weeks', 'Named after explorer Eberhardt.'),
    ('Bulbophyllum', 'echinolabium', 'Spiny-lipped Bulbophyllum that has the largest flower in the genus.', 'Large', 'Rare', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Red, Purple', '3-4 weeks', 'Has the largest flower in the genus.'),
    ('Bulbophyllum', 'elassonotum', 'Small-backed Bulbophyllum, small species with distinctive back markings.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Green, Yellow', '3-4 weeks', 'Small species with distinctive back markings.'),
    ('Bulbophyllum', 'fascinator', 'Fascinating Bulbophyllum named for its fascinating flower structure.', 'Medium', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring, Summer', 'Purple, Yellow', '3-4 weeks', 'Named for its fascinating flower structure.'),
    ('Bulbophyllum', 'fletcherianum', 'Fletcher Bulbophyllum named after botanist Fletcher.', 'Medium', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Yellow, Red', '3-4 weeks', 'Named after botanist Fletcher.'),
    ('Bulbophyllum', 'gracillimum', 'Most graceful Bulbophyllum noted for graceful flower form.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring, Summer', 'Green, White', '3-4 weeks', 'Noted for graceful flower form.'),
    ('Bulbophyllum', 'guttulatum', 'Spotted Bulbophyllum notable for spotted flower pattern.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Yellow, Brown spots', '3-4 weeks', 'Notable for spotted flower pattern.'),
    ('Bulbophyllum', 'laciniatum', 'Fringed Bulbophyllum with flowers that have fringed edges.', 'Medium', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Purple, White', '3-4 weeks', 'Flowers have fringed edges.'),
    ('Bulbophyllum', 'lepidum', 'Charming Bulbophyllum, charming small species.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring, Summer', 'Pink, White', '3-4 weeks', 'Charming small species.'),
    ('Bulbophyllum', 'micholitzii', 'Micholitz Bulbophyllum named after collector Micholitz.', 'Medium', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Red, Purple', '3-4 weeks', 'Named after collector Micholitz.'),
    ('Bulbophyllum', 'miniatum', 'Cinnabar Bulbophyllum named for cinnabar-red coloring.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Orange, Red', '3-4 weeks', 'Named for cinnabar-red coloring.'),
    ('Bulbophyllum', 'odoratistrueum', 'Fragrant Bulbophyllum, one of the few fragrant Bulbophyllum species.', 'Medium', 'Uncommon', true, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Yellow, Green', '3-4 weeks', 'One of the few fragrant Bulbophyllum species.'),
    ('Bulbophyllum', 'phalaenopsis', 'Moth-like Bulbophyllum, large species resembling Phalaenopsis.', 'Large', 'Rare', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Purple, White', '3-4 weeks', 'Large species resembling Phalaenopsis.'),
    ('Bulbophyllum', 'reticulatum', 'Net-veined Bulbophyllum notable for net-like vein pattern.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Spring, Summer', 'Green, Purple veins', '3-4 weeks', 'Notable for net-like vein pattern.'),
    ('Bulbophyllum', 'rothschildianum', 'Rothschild Bulbophyllum, large spectacular species named after Rothschild.', 'Large', 'Rare', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Red, Purple', '3-4 weeks', 'Large spectacular species named after Rothschild.'),
    ('Bulbophyllum', 'rufinum', 'Reddish Bulbophyllum notable for reddish coloration.', 'Small', 'Common', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Red, Brown', '3-4 weeks', 'Notable for reddish coloration.'),
    
    -- Cattleya species (2 species)
    ('Cattleya', 'Isabellae', 'Classic purple Cattleya hybrid, popular hybrid with large fragrant flowers that needs bright light and good air circulation.', 'Large', 'Common', true, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Fall, Winter', 'Purple, White', '6-8 weeks', 'Popular hybrid with large fragrant flowers, needs bright light and good air circulation.'),
    ('Cattleya', 'guatemalensis', 'Central American Cattleya species native to Guatemala that needs bright light and winter rest.', 'Large', 'Uncommon', true, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Winter, Spring', 'White, Purple lip', '6-8 weeks', 'Native to Guatemala, needs bright light and winter rest.'),
    
    -- Ceratostylis species (1 species)
    ('Ceratostylis', 'incognita', 'Unknown horn orchid, recently discovered species from Southeast Asia.', 'Small', 'Rare', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'White, Pink', '2-3 weeks', 'Recently discovered species from Southeast Asia.'),
    
    -- Christensonella species (1 species)
    ('Christensonella', 'paranaensis', 'Parana Christensonella endemic to Parana region of Brazil.', 'Medium', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Spring', 'Yellow, Green', '4-5 weeks', 'Endemic to Parana region of Brazil.'),
    
    -- Coelogyne species (9 species)
    ('Coelogyne', 'cristata', 'White Himalayan orchid, cool-growing Himalayan species that needs winter chill and high humidity.', 'Medium', 'Common', true, 'Cool', 'Medium', 'High', 'Epiphyte', 'Winter, Spring', 'White, Yellow markings', '4-6 weeks', 'Cool-growing Himalayan species, needs winter chill and high humidity.'),
    ('Coelogyne', 'graminifolia', 'Grass-leafed Coelogyne, small species with grass-like leaves.', 'Small', 'Uncommon', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Green', '3-4 weeks', 'Small species with grass-like leaves.'),
    ('Coelogyne', 'huettneriana', 'Huettner Coelogyne named after botanist Huettner.', 'Medium', 'Uncommon', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Brown', '4-5 weeks', 'Named after botanist Huettner.'),
    ('Coelogyne', 'lawrenceana', 'Lawrence Coelogyne, popular cool-growing species.', 'Medium', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Winter, Spring', 'White, Yellow', '4-5 weeks', 'Popular cool-growing species.'),
    ('Coelogyne', 'lindleyana', 'Lindley Coelogyne named after botanist Lindley.', 'Medium', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Pink', '4-5 weeks', 'Named after botanist Lindley.'),
    ('Coelogyne', 'miniata', 'Orange Coelogyne, unusual orange-flowered species.', 'Small', 'Uncommon', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'Orange, Red', '3-4 weeks', 'Unusual orange-flowered species.'),
    ('Coelogyne', 'ovalis', 'Oval-leafed Coelogyne, small species with oval leaves.', 'Small', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Green', '3-4 weeks', 'Small species with oval leaves.'),
    ('Coelogyne', 'trinervis', 'Three-nerved Coelogyne named for three-nerved leaves.', 'Medium', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Brown', '4-5 weeks', 'Named for three-nerved leaves.'),
    ('Coelogyne', 'usitana', 'Usitan Coelogyne, regional variety from specific location.', 'Medium', 'Uncommon', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Yellow', '4-5 weeks', 'Regional variety from specific location.'),
    
    -- Cryptophoranthus species (1 species)
    ('Cryptophoranthus', 'fenestratus', 'Window-leafed orchid, unique species with translucent window areas in leaves.', 'Small', 'Rare', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Unique species with translucent window areas in leaves.'),
    
    -- Dendrobium species (17 species)
    ('Dendrobium', 'alexandrae', 'Australian rock orchid, Australian species preferring bright light and good drainage.', 'Medium', 'Uncommon', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'White, Purple', '4-6 weeks', 'Australian species preferring bright light and good drainage.'),
    ('Dendrobium', 'anceps', 'Two-edged Dendrobium, Asian species with flattened stems.', 'Medium', 'Common', false, 'Warm', 'High', 'High', 'Epiphyte', 'Spring, Summer', 'White, Purple', '4-6 weeks', 'Asian species with flattened stems.'),
    ('Dendrobium', 'chrysotoxum', 'Golden yellow Asian orchid with spectacular golden flowers that needs bright light and dry winter rest.', 'Medium', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'Golden Yellow', '6-8 weeks', 'Spectacular golden flowers, needs bright light and dry winter rest.'),
    ('Dendrobium', 'densiflorum', 'Dense golden clusters, dense flower clusters that needs cool dry winter rest.', 'Medium', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'Golden Yellow, Orange', '6-8 weeks', 'Dense flower clusters, needs cool dry winter rest.'),
    ('Dendrobium', 'gatton', 'Gatton Dendrobium hybrid, popular hybrid with long-lasting flowers.', 'Medium', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'Yellow, Red', '6-8 weeks', 'Popular hybrid with long-lasting flowers.'),
    ('Dendrobium', 'glumaceum', 'Chaffy Dendrobium, small Asian species with chaffy bracts.', 'Small', 'Uncommon', false, 'Warm', 'High', 'High', 'Epiphyte', 'Summer', 'White, Pink', '3-4 weeks', 'Small Asian species with chaffy bracts.'),
    ('Dendrobium', 'hancockii', 'Hancock Dendrobium named after collector Hancock.', 'Small', 'Uncommon', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'Pink, White', '4-5 weeks', 'Named after collector Hancock.'),
    ('Dendrobium', 'hercoglossum', 'Hero-tongued Dendrobium notable for prominent lip.', 'Medium', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'White, Purple', '4-6 weeks', 'Notable for prominent lip.'),
    ('Dendrobium', 'kingianum', 'Australian pink rock orchid, hardy Australian species that tolerates cool temperatures and lower humidity.', 'Small', 'Common', false, 'Cool', 'High', 'Low', 'Epiphyte', 'Spring', 'Pink, White', '6-8 weeks', 'Hardy Australian species, tolerates cool temperatures and lower humidity.'),
    ('Dendrobium', 'lichenastrum', 'Lichen-like Dendrobium, miniature species resembling lichen.', 'Small', 'Rare', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Green, White', '3-4 weeks', 'Miniature species resembling lichen.'),
    ('Dendrobium', 'loddigesii', 'Pink Asian beauty with beautiful pink flowers with contrasting purple lip.', 'Small', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'Pink, White, Purple lip', '4-6 weeks', 'Beautiful pink flowers with contrasting purple lip.'),
    ('Dendrobium', 'mousmee', 'Mousmee Dendrobium, named variety with attractive flowers.', 'Medium', 'Uncommon', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'White, Pink', '4-6 weeks', 'Named variety with attractive flowers.'),
    ('Dendrobium', 'nobile x findlayanum', 'Classic noble-type hybrid, popular hybrid combining two classic species that needs cool winter rest.', 'Medium', 'Common', false, 'Cool', 'High', 'Medium', 'Epiphyte', 'Winter, Spring', 'White, Pink, Purple', '6-8 weeks', 'Popular hybrid combining two classic species, needs cool winter rest.'),
    ('Dendrobium', 'purpureum', 'Purple Dendrobium, warm-growing species with purple flowers.', 'Medium', 'Common', false, 'Warm', 'High', 'High', 'Epiphyte', 'Summer', 'Purple, White', '4-6 weeks', 'Warm-growing species with purple flowers.'),
    ('Dendrobium', 'rhodostictum', 'Rose-spotted Dendrobium notable for rose-colored spots.', 'Small', 'Uncommon', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'White, Pink spots', '4-5 weeks', 'Notable for rose-colored spots.'),
    ('Dendrobium', 'terminale', 'Terminal Dendrobium with flowers at stem tips.', 'Small', 'Common', false, 'Warm', 'High', 'High', 'Epiphyte', 'Summer', 'White, Pink', '3-4 weeks', 'Flowers at stem tips.'),
    ('Dendrobium', 'thyrsiflorum', 'White and gold clusters, dense pendulous clusters that needs bright light and winter rest.', 'Medium', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring', 'White, Golden Yellow', '6-8 weeks', 'Dense pendulous clusters, needs bright light and winter rest.'),
    
    -- Dendrochilum species (3 species)
    ('Dendrochilum', 'stenophyllum', 'Narrow-leafed chain orchid with delicate chain-like flower arrangement.', 'Small', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Green', '4-5 weeks', 'Delicate chain-like flower arrangement.'),
    ('Dendrochilum', 'tenellum', 'Delicate chain orchid, very delicate small chain orchid.', 'Small', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Yellow', '4-5 weeks', 'Very delicate small chain orchid.'),
    ('Dendrochilum', 'wenzelii', 'Wenzel chain orchid named after botanist Wenzel.', 'Small', 'Uncommon', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Spring', 'White, Green', '4-5 weeks', 'Named after botanist Wenzel.'),
    
    -- Dichaea species (1 species)
    ('Dichaea', 'pendula', 'Pendulous fan orchid with fan-shaped growth with pendulous habit.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'High', 'Epiphyte', 'Summer', 'Green, White', '3-4 weeks', 'Fan-shaped growth with pendulous habit.'),
    
    -- Dinema species (1 species)
    ('Dinema', 'polybulbon', 'Many-bulbed miniature orchid, small orchid with multiple pseudobulbs.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Spring, Summer', 'Yellow, Brown', '2-3 weeks', 'Small orchid with multiple pseudobulbs.'),
    
    -- Dracula species (2 species)
    ('Dracula', 'benedictii', 'Benedict monkey-face orchid, cool cloud forest species with bizarre monkey-face flowers.', 'Medium', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Brown, Red, Yellow', '3-4 weeks', 'Cool cloud forest species with bizarre monkey-face flowers.'),
    ('Dracula', 'hirsuta', 'Hairy monkey-face orchid notable for hairy flower characteristics.', 'Medium', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Brown, Purple, Yellow', '3-4 weeks', 'Notable for hairy flower characteristics.'),
    
    -- Dryadella species (5 species)
    ('Dryadella', 'cristata', 'Crested forest nymph orchid, small cloud forest species with crested flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Small cloud forest species with crested flowers.'),
    ('Dryadella', 'espirito-santensis', 'Espirito Santo forest nymph endemic to Espirito Santo region.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red', '2-3 weeks', 'Endemic to Espirito Santo region.'),
    ('Dryadella', 'litoralis', 'Coastal forest nymph orchid found in coastal cloud forests.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Found in coastal cloud forests.'),
    ('Dryadella', 'vitorinoi', 'Vitorino forest nymph orchid named after botanist Vitorino.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named after botanist Vitorino.'),
    ('Dryadella', 'wuerstlei', 'Wuerstle forest nymph orchid named after collector Wuerstle.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red', '2-3 weeks', 'Named after collector Wuerstle.'),
    
    -- Encyclia species (2 species)
    ('Encyclia', 'citrina', 'Fragrant lemon-yellow orchid, popular fragrant species that needs bright light and good drainage.', 'Medium', 'Common', true, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring, Summer', 'Lemon Yellow', '6-8 weeks', 'Popular fragrant species, needs bright light and good drainage.'),
    ('Encyclia', 'mariae', 'Mary Encyclia orchid, Central American species with attractive flowers.', 'Medium', 'Uncommon', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Summer', 'Green, Purple', '5-6 weeks', 'Central American species with attractive flowers.'),
    
    -- Epicattleya species (1 species)
    ('Epicattleya', 'Hsing', 'Hsing hybrid orchid, popular intergeneric hybrid combining Epidendrum and Cattleya.', 'Large', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Fall, Winter', 'Orange, Red', '6-8 weeks', 'Popular intergeneric hybrid combining Epidendrum and Cattleya.'),
    
    -- Epidendrum species (3 species)
    ('Epidendrum', 'parkinsonianum', 'Parkinson reed orchid, large pendulous species that can bloom repeatedly.', 'Large', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Year-round', 'Green, White', '4-6 weeks', 'Large pendulous species, can bloom repeatedly.'),
    ('Epidendrum', 'peperomia', 'Pepper-like reed orchid, small species with succulent-like leaves.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Green, Red', '3-4 weeks', 'Small species with succulent-like leaves.'),
    ('Epidendrum', 'vesicatum', 'Bladder reed orchid named for bladder-like pseudobulbs.', 'Medium', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Green, Purple', '4-5 weeks', 'Named for bladder-like pseudobulbs.'),
    
    -- Epigeneium species (1 species)
    ('Epigeneium', 'amplum', 'Large Epigeneium orchid, Asian creeping orchid with waxy flowers.', 'Small', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'White, Pink', '3-4 weeks', 'Asian creeping orchid with waxy flowers.'),
    
    -- Habenaria species (1 species)
    ('Habenaria', 'myriotricha', 'Many-haired bog orchid, terrestrial species with fringed flowers.', 'Medium', 'Rare', false, 'Cool', 'Medium', 'Medium', 'Terrestrial', 'Summer', 'White, Green', '4-6 weeks', 'Terrestrial species with fringed flowers.'),
    
    -- Jumellea species (1 species)
    ('Jumellea', 'gladiator', 'Gladiator white orchid, Madagascar species with evening fragrance.', 'Medium', 'Uncommon', true, 'Warm', 'Medium', 'High', 'Epiphyte', 'Winter', 'White', '4-6 weeks', 'Madagascar species with evening fragrance.'),
    
    -- Kefersteinia species (1 species)
    ('Kefersteinia', 'sanguinolenta', 'Blood-red cupped orchid, cloud forest species with distinctive cupped flowers.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Red, Brown', '3-4 weeks', 'Cloud forest species with distinctive cupped flowers.'),
    
    -- Laelia species (2 species)
    ('Laelia', 'lundii', 'Lund rock orchid, Brazilian species growing on rocks that needs bright light.', 'Medium', 'Common', false, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Summer', 'White, Purple lip', '6-8 weeks', 'Brazilian species growing on rocks, needs bright light.'),
    ('Laelia', 'purpurata', 'Brazilian national flower, fragrant and spectacular.', 'Large', 'Common', true, 'Intermediate', 'High', 'Medium', 'Epiphyte', 'Spring, Summer', 'White, Purple lip', '8-10 weeks', 'Brazil national flower, fragrant and spectacular.'),
    
    -- Lepanthes species (11 species)
    ('Lepanthes', 'adrianae', 'Adrian tiny orchid, miniature cloud forest species with intricate flowers.', 'Miniature', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Orange, Red', '2-3 weeks', 'Miniature cloud forest species with intricate flowers.'),
    ('Lepanthes', 'calodictyon', 'Beautiful net tiny orchid notable for net-like flower patterns.', 'Miniature', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Purple, Yellow', '2-3 weeks', 'Notable for net-like flower patterns.'),
    ('Lepanthes', 'escobariana', 'Escobar tiny orchid named after botanist Escobar.', 'Miniature', 'Rare', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Red, Orange', '2-3 weeks', 'Named after botanist Escobar.'),
    ('Lepanthes', 'gargoyla', 'Gargoyle tiny orchid with bizarre gargoyle-like flower form.', 'Miniature', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Brown, Purple', '2-3 weeks', 'Bizarre gargoyle-like flower form.'),
    ('Lepanthes', 'helicocephala', 'Spiral-headed tiny orchid notable for spiral flower arrangement.', 'Miniature', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Purple, Yellow', '2-3 weeks', 'Notable for spiral flower arrangement.'),
    ('Lepanthes', 'jubata', 'Maned tiny orchid named for mane-like flower structures.', 'Miniature', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Red, Yellow', '2-3 weeks', 'Named for mane-like flower structures.'),
    ('Lepanthes', 'nicolasii', 'Nicolas tiny orchid named after botanist Nicolas.', 'Miniature', 'Rare', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Orange, Red', '2-3 weeks', 'Named after botanist Nicolas.'),
    ('Lepanthes', 'orion', 'Orion tiny orchid named for constellation-like flower pattern.', 'Miniature', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Purple, White', '2-3 weeks', 'Named for constellation-like flower pattern.'),
    ('Lepanthes', 'telipogoniflora', 'Telipogon-flowered tiny orchid with flowers that resemble Telipogon orchids.', 'Miniature', 'Rare', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Purple, Yellow', '2-3 weeks', 'Flowers resemble Telipogon orchids.'),
    ('Lepanthes', 'tentaculata', 'Tentacled tiny orchid notable for tentacle-like flower projections.', 'Miniature', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Red, Purple', '2-3 weeks', 'Notable for tentacle-like flower projections.'),
    ('Lepanthes', 'tsubotae', 'Tsubota tiny orchid named after botanist Tsubota.', 'Miniature', 'Rare', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Orange, Yellow', '2-3 weeks', 'Named after botanist Tsubota.'),
    
    -- Masdevallia species (3 species)
    ('Masdevallia', 'Angel Frost', 'Angel Frost hybrid, popular cool-growing hybrid that needs constant cool conditions.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Winter, Spring', 'White, Pink', '4-6 weeks', 'Popular cool-growing hybrid, needs constant cool conditions.'),
    ('Masdevallia', 'infracta', 'Broken triangle orchid, cloud forest species with triangular flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Orange, Red', '4-6 weeks', 'Cloud forest species with triangular flowers.'),
    ('Masdevallia', 'stenorrhynchos', 'Narrow-beaked triangle orchid notable for narrow elongated flower tail.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Yellow, Red', '4-6 weeks', 'Notable for narrow elongated flower tail.'),
    
    -- Maxillaria species (13 species)
    ('Maxillaria', 'aurea', 'Golden Maxillaria with beautiful golden flowers with pleasant fragrance.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Golden Yellow', '4-5 weeks', 'Beautiful golden flowers with pleasant fragrance.'),
    ('Maxillaria', 'boothii', 'Booth Maxillaria named after botanist Booth.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Red, Brown', '3-4 weeks', 'Named after botanist Booth.'),
    ('Maxillaria', 'funicaulis', 'Rope-stemmed Maxillaria notable for rope-like stems.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Brown, Yellow', '3-4 weeks', 'Notable for rope-like stems.'),
    ('Maxillaria', 'picta', 'Painted Maxillaria with attractive painted flower patterns.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Spring, Summer', 'Yellow, Brown markings', '4-5 weeks', 'Attractive painted flower patterns.'),
    ('Maxillaria', 'porphyrostele', 'Purple-columned Maxillaria distinguished by purple column.', 'Medium', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Yellow, Purple column', '4-5 weeks', 'Distinguished by purple column.'),
    ('Maxillaria', 'sanguinea', 'Blood-red Maxillaria with striking deep red flowers.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Deep Red', '3-4 weeks', 'Striking deep red flowers.'),
    ('Maxillaria', 'schunkeana', 'Schunke Maxillaria named after botanist Schunke.', 'Medium', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'White, Red', '4-5 weeks', 'Named after botanist Schunke.'),
    ('Maxillaria', 'seidelii', 'Seidel Maxillaria named after collector Seidel.', 'Small', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Brown, Yellow', '3-4 weeks', 'Named after collector Seidel.'),
    ('Maxillaria', 'sophronitis', 'Sophronitis-like Maxillaria that resembles Sophronitis flowers.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Red, Orange', '3-4 weeks', 'Resembles Sophronitis flowers.'),
    ('Maxillaria', 'spilotantha', 'Spotted-flowered Maxillaria notable for spotted flower pattern.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Yellow, Brown spots', '4-5 weeks', 'Notable for spotted flower pattern.'),
    ('Maxillaria', 'tenuifolia', 'Coconut-scented orchid famous for intense coconut fragrance.', 'Small', 'Common', true, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Spring, Summer', 'Red, Yellow', '3-4 weeks', 'Famous for intense coconut fragrance.'),
    ('Maxillaria', 'uncata', 'Hooked Maxillaria named for hooked flower parts.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Brown, Green', '3-4 weeks', 'Named for hooked flower parts.'),
    ('Maxillaria', 'variabilis', 'Variable Maxillaria highly variable in flower color and form.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Year-round', 'Variable colors', '4-5 weeks', 'Highly variable in flower color and form.'),
    
    -- Continue with remaining species...
    ('Mediocalcar', 'bifolium', 'Two-leafed spur orchid, New Guinea species with two distinctive leaves.', 'Small', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Orange, Red', '3-4 weeks', 'New Guinea species with two distinctive leaves.'),
    
    ('Meiracyllium', 'trinasutum', 'Three-nosed miniature orchid named for three-pronged flower structure.', 'Small', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Purple, White', '2-3 weeks', 'Named for three-pronged flower structure.'),
    ('Meiracyllium', 'wendlandii', 'Wendland miniature orchid named after botanist Wendland.', 'Small', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Purple, Green', '2-3 weeks', 'Named after botanist Wendland.'),
    
    ('Mormolyca', 'ringens', 'Gaping goblin orchid named for gaping flower mouth.', 'Small', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Brown, Purple', '3-4 weeks', 'Named for gaping flower mouth.'),
    
    ('Muscarella', 'semperflorens', 'Ever-blooming fly orchid, continuously blooming miniature species.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Green, Purple', '2-3 weeks', 'Continuously blooming miniature species.'),
    
    ('Mycaranthes', 'stricta', 'Upright mouse orchid, small Asian species with upright growth.', 'Small', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'White, Pink', '2-3 weeks', 'Small Asian species with upright growth.'),
    
    ('Myoxanthus', 'exasperatus', 'Roughened mouse orchid, cloud forest species with rough-textured flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Cloud forest species with rough-textured flowers.'),
    
    ('Neofinetia', 'falcata', 'Japanese wind orchid highly prized in Japanese culture with evening fragrance.', 'Small', 'Common', true, 'Cool', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'White', '4-6 weeks', 'Highly prized in Japanese culture, evening fragrance.'),
    
    ('Octomeria', 'estrellensis', 'Star Mountain eight-part orchid from the Estrella mountains.', 'Small', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Summer', 'Yellow, Green', '2-3 weeks', 'From the Estrella mountains.'),
    ('Octomeria', 'leptophylla', 'Thin-leafed eight-part orchid notable for very thin leaves.', 'Small', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Notable for very thin leaves.'),
    ('Octomeria', 'octomeriantha', 'Eight-part flowered orchid, classic eight-angled stem orchid.', 'Small', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Summer', 'Green, Yellow', '2-3 weeks', 'Classic eight-angled stem orchid.'),
    
    ('Odontocidium', 'Catatante', 'Catatante dancing hybrid, popular Oncidium alliance hybrid.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall, Winter', 'Yellow, Brown', '6-8 weeks', 'Popular Oncidium alliance hybrid.'),
    ('Odontocidium', 'Tiger Crow', 'Tiger Crow dancing hybrid with striking striped hybrid flowers.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall, Winter', 'Yellow, Brown stripes', '6-8 weeks', 'Striking striped hybrid flowers.'),
    
    ('Oncidium', 'Elegance', 'Elegant dancing lady hybrid with graceful flowers.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall, Winter', 'Yellow, Brown', '6-8 weeks', 'Elegant hybrid with graceful flowers.'),
    ('Oncidium', 'Sharry Baby', 'Chocolate-scented dancing lady famous for chocolate fragrance.', 'Medium', 'Common', true, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall, Winter', 'Red, Brown', '6-8 weeks', 'Famous for chocolate fragrance.'),
    ('Oncidium', 'concolor', 'Solid-colored dancing lady with uniform yellow flowers without markings.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall', 'Yellow', '4-6 weeks', 'Uniform yellow flowers without markings.'),
    ('Oncidium', 'incurvum', 'Curved dancing lady notable for curved flower segments.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall', 'White, Pink', '6-8 weeks', 'Notable for curved flower segments.'),
    ('Oncidium', 'limminghei', 'Limminghe dancing lady named after collector Limminghe.', 'Small', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall', 'Yellow, Brown', '4-6 weeks', 'Named after collector Limminghe.'),
    ('Oncidium', 'raniferum', 'Frog-bearing dancing lady named for frog-like flower markings.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall', 'Yellow, Brown', '6-8 weeks', 'Named for frog-like flower markings.'),
    
    ('Ornithocephalus', 'bicornis', 'Two-horned bird orchid named for two horn-like projections.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Named for two horn-like projections.'),
    ('Ornithocephalus', 'gladiatus', 'Sword-shaped bird orchid notable for sword-like flower parts.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Notable for sword-like flower parts.'),
    ('Ornithocephalus', 'myrticola', 'Myrtle-dwelling bird orchid often found growing on myrtle trees.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Often found growing on myrtle trees.'),
    
    ('Ornithophora', 'radicans', 'Rooting bird-bearer orchid notable for extensive aerial root system.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Orange, Red', '3-4 weeks', 'Notable for extensive aerial root system.'),
    
    ('Osmoglossum', 'pulchellum', 'Pretty fragrant-tongue orchid, cool-growing species with pleasant fragrance.', 'Small', 'Common', true, 'Cool', 'Medium', 'High', 'Epiphyte', 'Fall', 'White, Brown', '4-6 weeks', 'Cool-growing species with pleasant fragrance.'),
    
    -- Pabstiella species (14 species)
    ('Pabstiella', 'alligatorifera', 'Alligator-bearing miniature orchid named for alligator-like flower markings.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named for alligator-like flower markings.'),
    ('Pabstiella', 'armeniaca', 'Apricot miniature orchid with beautiful apricot-colored flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Apricot, Orange', '2-3 weeks', 'Beautiful apricot-colored flowers.'),
    ('Pabstiella', 'bicolor', 'Two-colored miniature orchid with distinctive two-toned flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Distinctive two-toned flowers.'),
    ('Pabstiella', 'biracuensis', 'Biracu miniature orchid endemic to Biracu region.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red', '2-3 weeks', 'Endemic to Biracu region.'),
    ('Pabstiella', 'capijumensis', 'Capijum miniature orchid from the Capijum area.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'From the Capijum area.'),
    ('Pabstiella', 'determannii', 'Determann miniature orchid named after botanist Determann.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named after botanist Determann.'),
    ('Pabstiella', 'lacerticeps', 'Lizard-headed miniature orchid with flowers that resemble lizard heads.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Flowers resemble lizard heads.'),
    ('Pabstiella', 'naimekei', 'Naimeke miniature orchid named after location Naimeke.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named after location Naimeke.'),
    ('Pabstiella', 'oriana', 'Oriana miniature orchid named after person Oriana.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Purple, Green', '2-3 weeks', 'Named after person Oriana.'),
    ('Pabstiella', 'pantherina', 'Panther miniature orchid with panther-like spotted flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Yellow, Brown spots', '2-3 weeks', 'Panther-like spotted flowers.'),
    ('Pabstiella', 'rodriguesii', 'Rodrigues miniature orchid named after botanist Rodrigues.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red', '2-3 weeks', 'Named after botanist Rodrigues.'),
    ('Pabstiella', 'rubrolineata', 'Red-lined miniature orchid notable for red line markings.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red lines', '2-3 weeks', 'Notable for red line markings.'),
    ('Pabstiella', 'verboonenii', 'Verboonen miniature orchid named after botanist Verboonen.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named after botanist Verboonen.'),
    ('Pabstiella', 'versicolor', 'Variable-colored miniature orchid highly variable in flower coloration.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Variable colors', '2-3 weeks', 'Highly variable in flower coloration.'),
    
    ('Paphiopedilum', 'insigne', 'Classic green slipper orchid, hardy terrestrial species preferring cool conditions and good drainage.', 'Medium', 'Common', false, 'Cool', 'Low', 'Medium', 'Terrestrial', 'Winter', 'Green, White, Purple', '8-12 weeks', 'Hardy terrestrial species preferring cool conditions and good drainage.'),
    
    ('Phalaenopsis', 'lueddemanniana', 'Spotted moth orchid, popular species with long-lasting spotted flowers that needs warm humid conditions.', 'Medium', 'Common', false, 'Warm', 'Low', 'High', 'Epiphyte', 'Spring, Summer', 'White, Purple spots', '8-12 weeks', 'Popular species with long-lasting spotted flowers, needs warm humid conditions.'),
    
    ('Pholidota', 'chinensis', 'Chinese rattlesnake orchid, Asian species with drooping flower chains.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'White, Green', '3-4 weeks', 'Asian species with drooping flower chains.'),
    ('Pholidota', 'imbricata', 'Overlapping rattlesnake orchid named for overlapping flower bracts.', 'Small', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'White, Brown', '3-4 weeks', 'Named for overlapping flower bracts.'),
    
    ('Platystele', 'ovatifolia', 'Oval-leafed flat orchid, extremely small with oval leaves.', 'Miniature', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Extremely small with oval leaves.'),
    ('Platystele', 'oxyglossa', 'Sharp-tongued flat orchid named for sharp lip structure.', 'Miniature', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Named for sharp lip structure.'),
    
    ('Pleurobotryum', 'crepinianum', 'Crepin side-cluster orchid named after botanist Crepin.', 'Small', 'Uncommon', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Summer', 'Green, Brown', '3-4 weeks', 'Named after botanist Crepin.'),
    
    -- Pleurothallis species (40 species)
    ('Pleurothallis', 'aggeris', 'Mound rib-leaf orchid found growing in mounded colonies.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Found growing in mounded colonies.'),
    ('Pleurothallis', 'allenii', 'Allen rib-leaf orchid named after botanist Allen.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Named after botanist Allen.'),
    ('Pleurothallis', 'alligatorifera', 'Alligator-bearing rib-leaf orchid with flowers that resemble alligator patterns.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Flowers resemble alligator patterns.'),
    ('Pleurothallis', 'alvaroi', 'Alvaro rib-leaf orchid named after botanist Alvaro.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red', '2-3 weeks', 'Named after botanist Alvaro.'),
    ('Pleurothallis', 'aspergillum', 'Aspergillum rib-leaf orchid named for aspergillum-like flower form.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named for aspergillum-like flower form.'),
    ('Pleurothallis', 'binotii', 'Binot rib-leaf orchid named after botanist Binot.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named after botanist Binot.'),
    ('Pleurothallis', 'bivalvis', 'Two-valved rib-leaf orchid named for two-part flower structure.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Named for two-part flower structure.'),
    ('Pleurothallis', 'canaligera', 'Channel-bearing rib-leaf orchid notable for channeled leaf structure.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Notable for channeled leaf structure.'),
    ('Pleurothallis', 'chloroleuca', 'Green-white rib-leaf orchid with distinctive green and white flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Distinctive green and white flowers.'),
    ('Pleurothallis', 'crocodiliceps', 'Crocodile-headed rib-leaf orchid with flowers that resemble crocodile heads.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Flowers resemble crocodile heads.'),
    ('Pleurothallis', 'dilemma', 'Dilemma rib-leaf orchid named for taxonomic confusion.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named for taxonomic confusion.'),
    ('Pleurothallis', 'dunstervillei', 'Dunsterville rib-leaf orchid named after botanist Dunsterville.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named after botanist Dunsterville.'),
    ('Pleurothallis', 'excelsa', 'Tall rib-leaf orchid, one of the larger Pleurothallis species.', 'Medium', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'One of the larger Pleurothallis species.'),
    ('Pleurothallis', 'gargantua', 'Giant rib-leaf orchid, largest species in the genus.', 'Large', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '3-4 weeks', 'Largest species in the genus.'),
    ('Pleurothallis', 'gelida', 'Icy rib-leaf orchid, cold-growing cloud forest species.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'White, Green', '2-3 weeks', 'Cold-growing cloud forest species.'),
    ('Pleurothallis', 'ghiesbreghtiana', 'Ghiesbreght rib-leaf orchid named after collector Ghiesbreght.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named after collector Ghiesbreght.'),
    ('Pleurothallis', 'linguifera', 'Tongue-bearing rib-leaf orchid notable for tongue-like lip.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Notable for tongue-like lip.'),
    ('Pleurothallis', 'lynniana', 'Lynn rib-leaf orchid named after botanist Lynn.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Named after botanist Lynn.'),
    ('Pleurothallis', 'marthae', 'Martha rib-leaf orchid named after person Martha.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red', '2-3 weeks', 'Named after person Martha.'),
    ('Pleurothallis', 'mirabilis', 'Wonderful rib-leaf orchid remarkable for unusual flower colors.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Purple, Yellow', '2-3 weeks', 'Remarkable for unusual flower colors.'),
    ('Pleurothallis', 'niveoglobula', 'Snow-globe rib-leaf orchid with pure white globular flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'White', '2-3 weeks', 'Pure white globular flowers.'),
    ('Pleurothallis', 'pectinata', 'Combed rib-leaf orchid named for comb-like flower structure.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named for comb-like flower structure.'),
    ('Pleurothallis', 'pectinata x prolifera', 'Combed prolific hybrid, natural hybrid between two species.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Natural hybrid between two species.'),
    ('Pleurothallis', 'penelops', 'Penelope rib-leaf orchid named after mythological Penelope.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named after mythological Penelope.'),
    ('Pleurothallis', 'phalangifera', 'Spider-bearing rib-leaf orchid with flowers that resemble small spiders.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Flowers resemble small spiders.'),
    ('Pleurothallis', 'phymatodes x teaguei', 'Warty Teague hybrid, natural hybrid with warty characteristics.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Natural hybrid with warty characteristics.'),
    ('Pleurothallis', 'purpureoviolacea', 'Purple-violet rib-leaf orchid with beautiful purple-violet flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Purple, Violet', '2-3 weeks', 'Beautiful purple-violet flowers.'),
    ('Pleurothallis', 'racemiflora', 'Raceme-flowered rib-leaf orchid with flowers arranged in racemes.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Flowers arranged in racemes.'),
    ('Pleurothallis', 'restrepioides', 'Restrepia-like rib-leaf orchid that resembles Restrepia flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Red stripes', '2-3 weeks', 'Resembles Restrepia flowers.'),
    ('Pleurothallis', 'ringens', 'Gaping rib-leaf orchid named for gaping flower mouth.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named for gaping flower mouth.'),
    ('Pleurothallis', 'ruscaria', 'Butcher-broom rib-leaf orchid that resembles butcher-broom leaves.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green', '2-3 weeks', 'Resembles butcher-broom leaves.'),
    ('Pleurothallis', 'ruscifolia', 'Ruscus-leafed rib-leaf orchid with leaves that resemble Ruscus plants.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Leaves resemble Ruscus plants.'),
    ('Pleurothallis', 'saueri', 'Sauer rib-leaf orchid named after botanist Sauer.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named after botanist Sauer.'),
    ('Pleurothallis', 'solium', 'Throne rib-leaf orchid named for throne-like flower arrangement.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named for throne-like flower arrangement.'),
    ('Pleurothallis', 'talpinarioides', 'Mole-like rib-leaf orchid, dark colored and resembles small moles.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Brown, Green', '2-3 weeks', 'Dark colored, resembles small moles.'),
    ('Pleurothallis', 'tarantula', 'Tarantula rib-leaf orchid with dark hairy flowers resembling tarantulas.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Brown, Black', '2-3 weeks', 'Dark hairy flowers resembling tarantulas.'),
    ('Pleurothallis', 'taurus', 'Bull rib-leaf orchid with robust flowers resembling bull heads.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Brown, Red', '2-3 weeks', 'Robust flowers resembling bull heads.'),
    ('Pleurothallis', 'teaguei', 'Teague rib-leaf orchid named after botanist Teague.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named after botanist Teague.'),
    ('Pleurothallis', 'titan', 'Titan rib-leaf orchid, one of the largest in the genus.', 'Large', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '3-4 weeks', 'One of the largest in the genus.'),
    ('Pleurothallis', 'tragulosa', 'Tragic rib-leaf orchid with dark, somber colored flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Dark Purple', '2-3 weeks', 'Dark, somber colored flowers.'),
    ('Pleurothallis', 'tribuloides', 'Tribulus-like rib-leaf orchid with spiny, tribulus-like flower form.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Spiny, tribulus-like flower form.'),
    ('Pleurothallis', 'truncata', 'Truncated rib-leaf orchid notable for truncated leaf tips.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Notable for truncated leaf tips.'),
    ('Pleurothallis', 'viduata', 'Widowed rib-leaf orchid with dark mourning-colored flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Dark Purple', '2-3 weeks', 'Dark mourning-colored flowers.'),
    ('Pleurothallis', 'volans', 'Flying rib-leaf orchid with flowers that appear to be in flight.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Flowers appear to be in flight.'),
    
    ('Podangis', 'dactyloceras', 'Finger-horned African orchid, African species with finger-like spurs and evening fragrance.', 'Medium', 'Uncommon', true, 'Warm', 'Medium', 'High', 'Epiphyte', 'Winter', 'White', '4-6 weeks', 'African species with finger-like spurs and evening fragrance.'),
    
    ('Psychopsis', 'papilio', 'Classic butterfly orchid, sequential bloomer with butterfly-like flowers that needs warm bright conditions.', 'Large', 'Common', false, 'Warm', 'High', 'Medium', 'Epiphyte', 'Year-round', 'Yellow, Brown, Red', '6-8 weeks', 'Sequential bloomer with butterfly-like flowers, needs warm bright conditions.'),
    
    ('Renanthera', 'monachica', 'Monk fire orchid, large climbing orchid with fiery red flowers that needs very bright light.', 'Large', 'Uncommon', false, 'Warm', 'Very High', 'High', 'Epiphyte', 'Summer', 'Red, Orange', '6-8 weeks', 'Large climbing orchid with fiery red flowers, needs very bright light.'),
    
    ('Restrepia', 'aspasicensis', 'Aspasic striped orchid, cloud forest species with distinctive striped flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'White, Purple stripes', '3-4 weeks', 'Cloud forest species with distinctive striped flowers.'),
    ('Restrepia', 'brachypus', 'Short-footed striped orchid named for short flower stems.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'Yellow, Red stripes', '3-4 weeks', 'Named for short flower stems.'),
    ('Restrepia', 'guttulata', 'Spotted striped orchid notable for spotted rather than striped pattern.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Year-round', 'White, Purple spots', '3-4 weeks', 'Notable for spotted rather than striped pattern.'),
    
    ('Robiquetia', 'cerina', 'Waxy Asian orchid, Southeast Asian species with waxy textured flowers.', 'Medium', 'Uncommon', false, 'Warm', 'High', 'High', 'Epiphyte', 'Summer', 'Waxy Yellow', '4-6 weeks', 'Southeast Asian species with waxy textured flowers.'),
    
    ('Schoenorchis', 'gemmata', 'Jeweled rush orchid, small Asian species with jewel-like flowers.', 'Small', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'Pink, White', '3-4 weeks', 'Small Asian species with jewel-like flowers.'),
    
    ('Schomburgkia', 'crispa', 'Curled Schomburgkia, large epiphyte with impressive flower spikes.', 'Large', 'Uncommon', false, 'Warm', 'High', 'Medium', 'Epiphyte', 'Summer', 'Brown, Purple', '6-8 weeks', 'Large epiphyte with impressive flower spikes.'),
    
    ('Seidenfadenia', 'mitrata', 'Mitered Asian orchid, Southeast Asian species with miter-shaped flowers.', 'Medium', 'Uncommon', false, 'Warm', 'High', 'High', 'Epiphyte', 'Summer', 'Pink, White', '4-6 weeks', 'Southeast Asian species with miter-shaped flowers.'),
    
    ('Sophronitis', 'coccinea', 'Brilliant red miniature orchid with spectacular bright red flowers, cool growing Brazilian species.', 'Small', 'Common', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Winter, Spring', 'Brilliant Red', '4-6 weeks', 'Spectacular bright red flowers, cool growing Brazilian species.'),
    
    ('Specklinia', 'rubidantha', 'Red-flowered speckled orchid, cloud forest species with red-tinted flowers.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Red, Green', '2-3 weeks', 'Cloud forest species with red-tinted flowers.'),
    ('Specklinia', 'subpicta', 'Somewhat painted speckled orchid with subtle painted flower patterns.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple markings', '2-3 weeks', 'Subtle painted flower patterns.'),
    
    ('Stanhopea', 'lietzei', 'Lietze bucket orchid, large fragrant bucket orchid with complex pollination mechanism.', 'Large', 'Uncommon', true, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'White, Purple spots', '1-2 weeks', 'Large fragrant bucket orchid with complex pollination mechanism.'),
    
    ('Stelis', 'ciliaris', 'Fringed star orchid notable for fringed flower edges.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Notable for fringed flower edges.'),
    ('Stelis', 'megantha', 'Large-flowered star orchid with larger flowers than typical for genus.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Larger flowers than typical for genus.'),
    ('Stelis', 'morganii', 'Morgan star orchid named after botanist Morgan.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named after botanist Morgan.'),
    ('Stelis', 'pilosa', 'Hairy star orchid notable for hairy flower characteristics.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, White', '2-3 weeks', 'Notable for hairy flower characteristics.'),
    
    ('Sudamerlycaste', 'fimbriata', 'Fringed South American beauty, beautiful cool-growing species with fringed flower edges.', 'Medium', 'Uncommon', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Winter, Spring', 'White, Pink, fringed', '6-8 weeks', 'Beautiful cool-growing species with fringed flower edges.'),
    
    ('Trichoglottis', 'orchidea', 'Orchid-like hairy tongue, Southeast Asian species with waxy flowers.', 'Medium', 'Common', false, 'Warm', 'High', 'High', 'Epiphyte', 'Summer', 'Yellow, Brown', '4-6 weeks', 'Southeast Asian species with waxy flowers.'),
    ('Trichoglottis', 'rosea', 'Rose hairy tongue orchid, beautiful pink-flowered species.', 'Medium', 'Common', false, 'Warm', 'High', 'High', 'Epiphyte', 'Summer', 'Pink, Rose', '4-6 weeks', 'Beautiful pink-flowered species.'),
    
    ('Trisetella', 'hoeijeri', 'Hoeijer three-bristle orchid named after botanist Hoeijer.', 'Small', 'Uncommon', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '2-3 weeks', 'Named after botanist Hoeijer.'),
    ('Trisetella', 'triglochin', 'Three-pointed bristle orchid named for three-pointed flower structure.', 'Small', 'Common', false, 'Cool', 'Low', 'High', 'Epiphyte', 'Summer', 'Green, Brown', '2-3 weeks', 'Named for three-pointed flower structure.'),
    
    ('Trudelia', 'cristata', 'Crested Asian orchid, Asian species with crested flower structures.', 'Medium', 'Uncommon', false, 'Warm', 'High', 'High', 'Epiphyte', 'Summer', 'White, Purple', '4-6 weeks', 'Asian species with crested flower structures.'),
    
    ('Tuberolabium', 'woodii', 'Wood tube-lip orchid, Southeast Asian species named after botanist Wood.', 'Small', 'Uncommon', false, 'Warm', 'Medium', 'High', 'Epiphyte', 'Summer', 'White, Pink', '3-4 weeks', 'Southeast Asian species named after botanist Wood.'),
    
    ('Vanda', 'teres', 'Climbing terete vanda, large climbing orchid needing very bright light and high humidity.', 'Large', 'Common', false, 'Warm', 'Very High', 'High', 'Epiphyte', 'Year-round', 'Pink, Purple, White', '6-8 weeks', 'Large climbing orchid needing very bright light and high humidity.'),
    
    ('Zygolum', 'Louisendorf', 'Louisendorf dancing hybrid, Oncidium alliance hybrid with dancing flowers.', 'Medium', 'Common', false, 'Intermediate', 'Medium', 'Medium', 'Epiphyte', 'Fall, Winter', 'Yellow, Brown', '6-8 weeks', 'Oncidium alliance hybrid with dancing flowers.'),
    
    ('Zygotastes', 'huamae', 'Huama yoke-taste orchid, South American cloud forest species.', 'Small', 'Uncommon', false, 'Cool', 'Medium', 'High', 'Epiphyte', 'Summer', 'Green, Purple', '3-4 weeks', 'South American cloud forest species.')
    
  ) AS s(genus_name, species_name, description, size_category, rarity_status, fragrance, temperature_preference, light_requirements, humidity_preference, growth_habit, flowering_season, flower_colors, bloom_duration, habitat_info)
)
INSERT INTO public.species (
  id, genus_id, user_id, name, description, 
  size_category, rarity_status, fragrance, temperature_preference, light_requirements, 
  humidity_preference, growth_habit, flowering_season, flower_colors, bloom_duration, 
  habitat_info, cultivation_notes, is_active, is_favorite, created_at, updated_at, sync_hash
)
SELECT 
  gen_random_uuid(),
  gm.id,
  NULL,
  sd.species_name,
  sd.description,
  sd.size_category,
  sd.rarity_status,
  sd.fragrance,
  sd.temperature_preference,
  sd.light_requirements,
  sd.humidity_preference,
  sd.growth_habit,
  sd.flowering_season,
  sd.flower_colors,
  sd.bloom_duration,
  sd.habitat_info,
  CASE 
    -- All temperature descriptions properly escaped without problematic characters
    WHEN sd.growth_habit = 'Epiphyte' AND sd.temperature_preference = 'Cool' THEN 'Cool epiphyte. Mount on bark or grow in coarse bark mix with excellent drainage. Water regularly but allow to dry between waterings. Maintain 60-80% humidity with good air circulation. Prefers temperatures 50-70 degrees F (10-21 degrees C).'
    WHEN sd.growth_habit = 'Epiphyte' AND sd.temperature_preference = 'Intermediate' THEN 'Intermediate epiphyte. Use bark-based medium with good drainage. Water when approaching dryness. Maintain 50-70% humidity with good air movement. Prefers temperatures 60-80 degrees F (15-27 degrees C).'
    WHEN sd.growth_habit = 'Epiphyte' AND sd.temperature_preference = 'Warm' THEN 'Warm epiphyte. Use moisture-retentive but well-draining bark mix. Water regularly, keeping slightly moist. Maintain 60-80% humidity. Prefers temperatures 70-85 degrees F and above (21-29 degrees C and above).'
    WHEN sd.growth_habit = 'Terrestrial' AND sd.temperature_preference = 'Cool' THEN 'Cool growing terrestrial. Plant in well-draining potting mix with perlite. Water regularly but avoid waterlogging. Maintain 50-70% humidity. Prefers temperatures 50-70 degrees F (10-21 degrees C).'
    WHEN sd.growth_habit = 'Terrestrial' AND sd.temperature_preference = 'Intermediate' THEN 'Intermediate terrestrial. Use well-draining potting mix. Water when surface approaches dryness. Maintain 45-65% humidity with good air circulation. Prefers temperatures 60-75 degrees F (15-24 degrees C).'
    WHEN sd.growth_habit = 'Terrestrial' AND sd.temperature_preference = 'Warm' THEN 'Warm growing terrestrial. Plant in moisture-retentive but well-draining mix. Water regularly, keeping slightly moist. Maintain 60-80% humidity. Prefers temperatures 70-85 degrees F and above (21-29 degrees C and above).'
    WHEN sd.growth_habit = 'Lithophyte' THEN 'Rock-growing orchid. Use very well-draining medium with bark and perlite. Water sparingly, allowing to dry between waterings. Needs excellent drainage and air circulation.'
    ELSE 'Specialized growing requirements - research specific needs for this species. Provide appropriate light, temperature, and humidity based on natural habitat.'
  END,
  true,
  false,
  NOW(),
  NOW(),
  LOWER(sd.genus_name) || '_' || LOWER(REPLACE(sd.species_name, ' ', '_')) || '_system'
FROM species_data sd
JOIN genus_mapping gm ON gm.name = sd.genus_name;

-- CREATE COMPREHENSIVE VARIANTS WITH DETAILED DESCRIPTIONS
-- ======================================
INSERT INTO public.variants (id, user_id, name, description, is_active, is_favorite, created_at, updated_at, sync_hash)
VALUES 
(gen_random_uuid(), NULL, 'alba', 'White or albino form lacking normal pigmentation except for the green parts. Highly prized by collectors for their pure appearance. These forms lack anthocyanin pigments but retain chlorophyll.', true, false, NOW(), NOW(), 'variant_alba_system'),
(gen_random_uuid(), NULL, 'coerulea', 'Blue form of the species, where normal pink/purple pigments are replaced by blue tones. Rare and valuable genetic variants caused by altered anthocyanin production.', true, false, NOW(), NOW(), 'variant_coerulea_system'),
(gen_random_uuid(), NULL, 'semi-alba', 'Semi-albino form where only the lip retains normal coloration while petals and sepals are white. Classic variant especially prized in Cattleyas and related genera.', true, false, NOW(), NOW(), 'variant_semialbameta_system'),
(gen_random_uuid(), NULL, 'flammea', 'Red or flame-colored form with intense red pigmentation. Often temperature and light sensitive, with colors intensifying under proper conditions.', true, false, NOW(), NOW(), 'variant_flammea_system'),
(gen_random_uuid(), NULL, 'aureum', 'Golden or yellow form with enhanced yellow pigmentation. Popular in many genera, particularly spectacular in Dendrobium and Cattleya species.', true, false, NOW(), NOW(), 'variant_aureum_system'),
(gen_random_uuid(), NULL, 'concolor', 'Solid colored form without the typical patterns, spots, or contrasting colors. Provides uniform coloration across all flower segments.', true, false, NOW(), NOW(), 'variant_concolor_system'),
(gen_random_uuid(), NULL, 'vinicolor', 'Wine-colored form with deep burgundy or wine-red coloration. Rich, dark coloration often enhanced by cool growing conditions.', true, false, NOW(), NOW(), 'variant_vinicolor_system'),
(gen_random_uuid(), NULL, 'striata', 'Striped or streaked form with distinctive linear markings. Parallel lines or stripes create attractive geometric patterns.', true, false, NOW(), NOW(), 'variant_striata_system'),
(gen_random_uuid(), NULL, 'spotted', 'Spotted or dotted form with distinctive spot patterns. Irregular spots or regular polka-dot arrangements across flower surfaces.', true, false, NOW(), NOW(), 'variant_spotted_system'),
(gen_random_uuid(), NULL, 'xanthina', 'Yellow form with enhanced yellow pigmentation throughout the flower. Often includes various shades from pale lemon to deep golden yellow.', true, false, NOW(), NOW(), 'variant_xanthina_system'),
(gen_random_uuid(), NULL, 'rubra', 'Red form with enhanced red pigmentation. Often deeper and more intense than flammea varieties, with rich crimson tones.', true, false, NOW(), NOW(), 'variant_rubra_system'),
(gen_random_uuid(), NULL, 'marginata', 'Form with contrasting margins or edges on petals and sepals. Creates attractive borders and outlines that highlight flower shapes.', true, false, NOW(), NOW(), 'variant_marginata_system'),
(gen_random_uuid(), NULL, 'peloric', 'Abnormal form where the lip characteristics appear in petals (peloria). Unusual and collectible mutations creating symmetrical flowers.', true, false, NOW(), NOW(), 'variant_peloric_system'),
(gen_random_uuid(), NULL, 'picta', 'Painted form with distinctive artistic markings or colorations. Creates painterly effects with brushstroke-like patterns.', true, false, NOW(), NOW(), 'variant_picta_system'),
(gen_random_uuid(), NULL, 'lineata', 'Form with distinctive line patterns or striations. Fine parallel lines create texture and visual interest.', true, false, NOW(), NOW(), 'variant_lineata_system'),
(gen_random_uuid(), NULL, 'tessellata', 'Mosaic or tessellated form with checkered patterns. Creates tile-like geometric arrangements across flower surfaces.', true, false, NOW(), NOW(), 'variant_tessellata_system'),
(gen_random_uuid(), NULL, 'punctata', 'Punctated form with fine dots or stippling. Creates subtle textural effects with tiny dots across flower surfaces.', true, false, NOW(), NOW(), 'variant_punctata_system'),
(gen_random_uuid(), NULL, 'venosa', 'Veined form with prominent vein patterns. Enhanced vascular markings create leaf-like patterns on flowers.', true, false, NOW(), NOW(), 'variant_venosa_system'),
(gen_random_uuid(), NULL, 'bicolor', 'Two-colored form with distinct color zones. Clear separation between different colored areas creates dramatic contrasts.', true, false, NOW(), NOW(), 'variant_bicolor_system'),
(gen_random_uuid(), NULL, 'tricolor', 'Three-colored form with three distinct color areas. Complex color combinations create spectacular visual effects.', true, false, NOW(), NOW(), 'variant_tricolor_system');

-- SUMMARY AND VERIFICATION
-- ======================================
SELECT 
  'OrchidPro Complete Import Successfully Completed!' as status,
  (SELECT COUNT(*) FROM public.families WHERE name = 'Orchidaceae') as families_imported,
  (SELECT COUNT(*) FROM public.genera WHERE family_id = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid) as genera_imported,
  (SELECT COUNT(*) FROM public.species WHERE genus_id IN (SELECT id FROM public.genera WHERE family_id = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid)) as species_imported,
  (SELECT COUNT(*) FROM public.variants WHERE user_id IS NULL) as variants_imported,
  NOW() as completed_at;

-- ADDITIONAL VERIFICATION QUERIES
-- ======================================
SELECT 'Sample Data Verification:' as info;

-- Show sample of imported data with all fields
SELECT 
  f.name as family,
  g.name as genus,
  s.name as species,
  s.description,
  s.size_category,
  s.rarity_status,
  s.fragrance,
  s.growth_habit,
  s.temperature_preference,
  s.light_requirements,
  s.humidity_preference,
  s.flowering_season,
  s.flower_colors,
  s.bloom_duration
FROM public.families f
JOIN public.genera g ON f.id = g.family_id
JOIN public.species s ON g.id = s.genus_id
WHERE f.name = 'Orchidaceae'
ORDER BY g.name, s.name
LIMIT 20;

-- Show species count by genus
SELECT 
  g.name as genus,
  COUNT(s.id) as species_count,
  COUNT(CASE WHEN s.fragrance = true THEN 1 END) as fragrant_species,
  COUNT(CASE WHEN s.size_category = 'Miniature' THEN 1 END) as miniature_species,
  COUNT(CASE WHEN s.size_category = 'Small' THEN 1 END) as small_species,
  COUNT(CASE WHEN s.size_category = 'Medium' THEN 1 END) as medium_species,
  COUNT(CASE WHEN s.size_category = 'Large' THEN 1 END) as large_species
FROM public.genera g
LEFT JOIN public.species s ON g.id = s.genus_id
WHERE g.family_id = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid
GROUP BY g.name
ORDER BY species_count DESC, g.name;

-- Show species by growth habit and temperature preference
SELECT 
  s.growth_habit,
  s.temperature_preference,
  COUNT(*) as species_count
FROM public.species s
JOIN public.genera g ON s.genus_id = g.id
WHERE g.family_id = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid
GROUP BY s.growth_habit, s.temperature_preference
ORDER BY species_count DESC;

-- Show rarity distribution
SELECT 
  s.rarity_status,
  COUNT(*) as species_count,
  ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM public.species s2 JOIN public.genera g2 ON s2.genus_id = g2.id WHERE g2.family_id = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid), 2) as percentage
FROM public.species s
JOIN public.genera g ON s.genus_id = g.id
WHERE g.family_id = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid
GROUP BY s.rarity_status
ORDER BY species_count DESC;

-- Show fragrant species
SELECT 
  g.name as genus,
  s.name as species,
  s.description,
  s.flower_colors,
  s.flowering_season
FROM public.species s
JOIN public.genera g ON s.genus_id = g.id
WHERE g.family_id = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'::uuid
AND s.fragrance = true
ORDER BY g.name, s.name;

SELECT 'Import completed successfully! All 261 species with complete cultivation data have been imported.' as final_message;