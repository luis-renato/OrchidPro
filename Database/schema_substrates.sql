-- Create substrates table
CREATE TABLE substrates (
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
    
    -- Substrate-specific fields
    components TEXT, -- 'Pine Bark 70%, Perlite 20%, Moss 10%'
    ph_range VARCHAR(50), -- '5.5-6.5', '6.0-7.0'
    drainage_level VARCHAR(50), -- 'High', 'Medium', 'Low'
    supplier VARCHAR(255), -- Supplier or brand name
    
    -- Constraints
    CONSTRAINT substrates_name_not_empty CHECK (length(trim(name)) > 0),
    CONSTRAINT substrates_name_user_unique UNIQUE(name, user_id)
);

-- Create indexes for performance
CREATE INDEX idx_substrates_user_id ON substrates(user_id);
CREATE INDEX idx_substrates_name ON substrates(name);
CREATE INDEX idx_substrates_active ON substrates(is_active);
CREATE INDEX idx_substrates_favorite ON substrates(is_favorite);
CREATE INDEX idx_substrates_supplier ON substrates(supplier);

-- Enable Row Level Security
ALTER TABLE substrates ENABLE ROW LEVEL SECURITY;

-- RLS Policies for substrates  
CREATE POLICY "Users can view own substrates" ON substrates
    FOR SELECT USING (
        auth.uid() = user_id OR 
        user_id IS NULL -- System data visible to all
    );

CREATE POLICY "Users can insert own substrates" ON substrates
    FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own substrates" ON substrates
    FOR UPDATE USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own substrates" ON substrates
    FOR DELETE USING (auth.uid() = user_id);