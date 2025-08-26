using OrchidPro.Services.Localization;
using OrchidPro.Models.Enums;

namespace OrchidPro.Extensions;

public static partial class EnumExtensions
{
    public static string ToDisplayText(this PhRange value, string language = "en")
        => FieldOptionsTranslator.GetDisplayText(value, language);

    public static string ToDisplayText(this DrainageLevel value, string language = "en")
        => FieldOptionsTranslator.GetDisplayText(value, language);

    public static string ToDisplayText(this MountMaterial value, string language = "en")
        => FieldOptionsTranslator.GetDisplayText(value, language);

    public static string ToDisplayText(this MountSize value, string language = "en")
        => FieldOptionsTranslator.GetDisplayText(value, language);

    public static string ToDisplayText(this DrainageType value, string language = "en")
        => FieldOptionsTranslator.GetDisplayText(value, language);

    public static string ToDisplayText(this SupplierType value, string language = "en")
        => FieldOptionsTranslator.GetDisplayText(value, language);

    public static string ToDisplayText(this LocationType value, string language = "en")
        => FieldOptionsTranslator.GetDisplayText(value, language);
}
