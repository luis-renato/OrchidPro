using OrchidPro.Services.Localization;
using OrchidPro.Models.Enums;

namespace OrchidPro.Extensions;

public static class EnumExtensions
{
    public static string ToDisplayText<T>(this T enumValue, string language = "en") where T : Enum
    {
        return FieldOptionsTranslator.GetDisplayText(enumValue, language);
    }
}