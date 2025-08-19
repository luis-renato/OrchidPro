using System.Globalization;

namespace OrchidPro.Converters;

/// <summary>
/// XAML converter that transforms sort option strings into compact display text.
/// Used primarily in UI controls where space is limited (dropdowns, buttons).
/// </summary>
/// <remarks>
/// Converts verbose sort descriptions to abbreviated versions:
/// - "Name A→Z" becomes "A→Z"
/// - "Recent First" becomes "Recent"
/// - Provides fallback to "A→Z" for unknown values
/// </remarks>
public class SortDisplayConverter : IValueConverter
{
    /// <summary>
    /// Converts sort order string to compact display format
    /// </summary>
    /// <param name="value">Sort order string from SortOption enum or ViewModel</param>
    /// <param name="targetType">Target type (typically string)</param>
    /// <param name="parameter">Optional converter parameter (unused)</param>
    /// <param name="culture">Culture info for localization (unused)</param>
    /// <returns>Abbreviated sort display text or "A→Z" as fallback</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string sortOrder)
        {
            return sortOrder switch
            {
                "Name A→Z" => "A→Z",
                "Name Z→A" => "Z→A",
                "Recent First" => "Recent",
                "Oldest First" => "Oldest",
                _ => "A→Z" // Default fallback for unknown sort options
            };
        }

        return "A→Z"; // Fallback when value is not a string
    }

    /// <summary>
    /// Reverse conversion not implemented as this is a one-way display converter
    /// </summary>
    /// <exception cref="NotImplementedException">Always thrown - converter is display-only</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("SortDisplayConverter is one-way only");
    }
}