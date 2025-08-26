namespace OrchidPro.Services.Localization;

public static class FieldOptionsTranslator
{
    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            // pH Ranges
            ["PhRange.VeryAcidic_40_45"] = "4.0-4.5 (Very Acidic)",
            ["PhRange.Acidic_45_50"] = "4.5-5.0 (Acidic)",
            ["PhRange.SlightlyAcidic_50_55"] = "5.0-5.5 (Slightly Acidic)",
            ["PhRange.MildlyAcidic_55_60"] = "5.5-6.0 (Mildly Acidic)",
            ["PhRange.NearNeutral_60_65"] = "6.0-6.5 (Near Neutral)",
            ["PhRange.Neutral_65_70"] = "6.5-7.0 (Neutral)",
            ["PhRange.SlightlyAlkaline_70_75"] = "7.0-7.5 (Slightly Alkaline)",

            // Drainage Levels
            ["DrainageLevel.VeryHigh"] = "Very High",
            ["DrainageLevel.High"] = "High",
            ["DrainageLevel.Medium"] = "Medium",
            ["DrainageLevel.Low"] = "Low",
            ["DrainageLevel.VeryLow"] = "Very Low",

            // Mount Materials
            ["MountMaterial.Plastic"] = "Plastic",
            ["MountMaterial.Clay"] = "Clay",
            ["MountMaterial.Wood"] = "Wood",
            ["MountMaterial.Ceramic"] = "Ceramic",
            ["MountMaterial.Cork"] = "Cork",
            ["MountMaterial.Metal"] = "Metal",
            ["MountMaterial.Terracotta"] = "Terracotta",
            ["MountMaterial.Other"] = "Other",

            // Mount Sizes
            ["MountSize.Small_2Inch"] = "2 inch (5cm)",
            ["MountSize.Medium_3Inch"] = "3 inch (7.5cm)",
            ["MountSize.Large_4Inch"] = "4 inch (10cm)",
            ["MountSize.XLarge_5Inch"] = "5 inch (12.5cm)",
            ["MountSize.XXLarge_6Inch"] = "6 inch (15cm)",
            ["MountSize.Jumbo_8Inch"] = "8 inch (20cm)",
            ["MountSize.Custom"] = "Custom Size",

            // Drainage Types
            ["DrainageType.MultipleHoles"] = "Multiple Holes",
            ["DrainageType.SlottedSides"] = "Slotted Sides",
            ["DrainageType.BasketWeave"] = "Basket Weave",
            ["DrainageType.MeshBottom"] = "Mesh Bottom",
            ["DrainageType.Solid"] = "Solid",

            // Supplier Types
            ["SupplierType.LocalNursery"] = "Local Nursery",
            ["SupplierType.OnlineStore"] = "Online Store",
            ["SupplierType.SpecialtyGrower"] = "Specialty Grower",
            ["SupplierType.TradeShow"] = "Trade Show",
            ["SupplierType.FriendExchange"] = "Friend Exchange",
            ["SupplierType.PrivateCollection"] = "Private Collection",
            ["SupplierType.Other"] = "Other",

            // Location Types
            ["LocationType.IndoorWindowsill"] = "Indoor Windowsill",
            ["LocationType.IndoorGrowLight"] = "Indoor Grow Light",
            ["LocationType.Greenhouse"] = "Greenhouse",
            ["LocationType.OutdoorGarden"] = "Outdoor Garden",
            ["LocationType.ShadeHouse"] = "Shade House",
            ["LocationType.BalconyPatio"] = "Balcony/Patio",
            ["LocationType.Other"] = "Other"
        },

        ["pt"] = new Dictionary<string, string>
        {
            // pH Ranges
            ["PhRange.VeryAcidic_40_45"] = "4.0-4.5 (Muito Ácido)",
            ["PhRange.Acidic_45_50"] = "4.5-5.0 (Ácido)",
            ["PhRange.SlightlyAcidic_50_55"] = "5.0-5.5 (Levemente Ácido)",
            ["PhRange.MildlyAcidic_55_60"] = "5.5-6.0 (Pouco Ácido)",
            ["PhRange.NearNeutral_60_65"] = "6.0-6.5 (Quase Neutro)",
            ["PhRange.Neutral_65_70"] = "6.5-7.0 (Neutro)",
            ["PhRange.SlightlyAlkaline_70_75"] = "7.0-7.5 (Levemente Alcalino)",

            // Drainage Levels
            ["DrainageLevel.VeryHigh"] = "Muito Alta",
            ["DrainageLevel.High"] = "Alta",
            ["DrainageLevel.Medium"] = "Média",
            ["DrainageLevel.Low"] = "Baixa",
            ["DrainageLevel.VeryLow"] = "Muito Baixa",

            // Mount Materials
            ["MountMaterial.Plastic"] = "Plástico",
            ["MountMaterial.Clay"] = "Barro",
            ["MountMaterial.Wood"] = "Madeira",
            ["MountMaterial.Ceramic"] = "Cerâmica",
            ["MountMaterial.Cork"] = "Cortiça",
            ["MountMaterial.Metal"] = "Metal",
            ["MountMaterial.Terracotta"] = "Terracota",
            ["MountMaterial.Other"] = "Outro",

            // Mount Sizes
            ["MountSize.Small_2Inch"] = "2 pol (5cm)",
            ["MountSize.Medium_3Inch"] = "3 pol (7.5cm)",
            ["MountSize.Large_4Inch"] = "4 pol (10cm)",
            ["MountSize.XLarge_5Inch"] = "5 pol (12.5cm)",
            ["MountSize.XXLarge_6Inch"] = "6 pol (15cm)",
            ["MountSize.Jumbo_8Inch"] = "8 pol (20cm)",
            ["MountSize.Custom"] = "Tamanho Personalizado",

            // Drainage Types
            ["DrainageType.MultipleHoles"] = "Múltiplos Furos",
            ["DrainageType.SlottedSides"] = "Laterais Fendadas",
            ["DrainageType.BasketWeave"] = "Trama de Cesto",
            ["DrainageType.MeshBottom"] = "Fundo de Tela",
            ["DrainageType.Solid"] = "Sólido",

            // Supplier Types
            ["SupplierType.LocalNursery"] = "Viveiro Local",
            ["SupplierType.OnlineStore"] = "Loja Online",
            ["SupplierType.SpecialtyGrower"] = "Produtor Especializado",
            ["SupplierType.TradeShow"] = "Feira Comercial",
            ["SupplierType.FriendExchange"] = "Troca com Amigo",
            ["SupplierType.PrivateCollection"] = "Coleção Particular",
            ["SupplierType.Other"] = "Outro",

            // Location Types
            ["LocationType.IndoorWindowsill"] = "Parapeito Interno",
            ["LocationType.IndoorGrowLight"] = "Luz Artificial Interna",
            ["LocationType.Greenhouse"] = "Estufa",
            ["LocationType.OutdoorGarden"] = "Jardim Externo",
            ["LocationType.ShadeHouse"] = "Casa de Sombra",
            ["LocationType.BalconyPatio"] = "Varanda/Pátio",
            ["LocationType.Other"] = "Outro"
        }
    };

    public static string GetDisplayText<T>(T enumValue, string language = "en") where T : Enum
    {
        var key = $"{typeof(T).Name}.{enumValue}";

        if (_translations.TryGetValue(language, out var languageDict) &&
            languageDict.TryGetValue(key, out var translation))
        {
            return translation;
        }

        // Fallback to English
        if (language != "en" && _translations["en"].TryGetValue(key, out var englishTranslation))
        {
            return englishTranslation;
        }

        // Last resort: enum name
        return enumValue.ToString();
    }
}