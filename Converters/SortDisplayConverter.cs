using System.Globalization;

namespace OrchidPro.Converters;

/// <summary>
/// ✅ NOVO: Converter para exibir texto de ordenação compacto
/// </summary>
public class SortDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string sortOrder)
        {
            return sortOrder switch
            {
                "Name A→Z" => "A→Z",
                "Name Z→A" => "Z→A",
                "Recent First" => "Recent",
                "Oldest First" => "Oldest",
                _ => "A→Z"
            };
        }
        return "A→Z";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}