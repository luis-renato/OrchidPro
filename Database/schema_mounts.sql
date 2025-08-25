-- Create mounts table
CREATE TABLE mounts (
    -- Base fields (padrão estabelecido)
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES auth.users(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT true,
    is_favorite BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    sync_hash VARCHAR(255),
    
    -- Mount-specific fields
    material VARCHAR(100), -- 'Plastic', 'Clay', 'Wood', 'Ceramic'
    size VARCHAR(50), -- '4 inch', '6 inch', 'Large', 'Small'
    drainage_type VARCHAR(100), -- 'Multiple holes', 'Slotted', 'Basket weave'
    
    -- Constraints
    CONSTRAINT mounts_name_not_empty CHECK (length(trim(name)) > 0),
    CONSTRAINT mounts_name_user_unique UNIQUE(name, user_id)
);

-- Create indexes for performance
CREATE INDEX idx_mounts_user_id ON mounts(user_id);
CREATE INDEX idx_mounts_name ON mounts(name);
CREATE INDEX idx_mounts_active ON mounts(is_active);
CREATE INDEX idx_mounts_favorite ON mounts(is_favorite);
CREATE INDEX idx_mounts_material ON mounts(material);

-- Enable Row Level Security
ALTER TABLE mounts ENABLE ROW LEVEL SECURITY;

-- RLS Policies for mounts
CREATE POLICY "Users can view own mounts" ON mounts
    FOR SELECT USING (
        auth.uid() = user_id OR 
        user_id IS NULL -- System data visible to all
    );

CREATE POLICY "Users can insert own mounts" ON mounts
    FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own mounts" ON mounts
    FOR UPDATE USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own mounts" ON mounts
    FOR DELETE USING (auth.uid() = user_id);