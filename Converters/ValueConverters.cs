using System.Globalization;

namespace OrchidPro.Converters;

/// <summary>
/// Converts string values to boolean for UI binding purposes
/// Returns true if string is not null, empty, or whitespace
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts string to boolean - true if string has content
    /// </summary>
    /// <param name="value">String value to evaluate</param>
    /// <param name="targetType">Target type (bool)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>True if string is not null/empty/whitespace, false otherwise</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrWhiteSpace(value?.ToString());
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverted version of StringToBoolConverter
/// Returns true if string is null, empty, or whitespace
/// Used for showing/hiding empty state indicators
/// </summary>
public class InvertedStringToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts string to inverted boolean - true if string is empty
    /// </summary>
    /// <param name="value">String value to evaluate</param>
    /// <param name="targetType">Target type (bool)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>True if string is null/empty/whitespace, false if has content</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value?.ToString());
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts boolean values for UI binding
/// Commonly used for negating visibility or enabled states
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    /// <summary>
    /// Inverts boolean value
    /// </summary>
    /// <param name="value">Boolean value to invert</param>
    /// <param name="targetType">Target type (bool)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>Inverted boolean value, false if input is not boolean</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }

    /// <summary>
    /// Converts back by inverting the boolean again (two-way binding support)
    /// </summary>
    /// <param name="value">Boolean value to invert back</param>
    /// <param name="targetType">Target type (bool)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>Inverted boolean value, false if input is not boolean</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }
}

/// <summary>
/// Converts integer values to boolean for UI binding
/// Returns true if integer is greater than zero
/// Useful for count-based visibility logic
/// </summary>
public class IntToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts integer to boolean - true if value > 0
    /// </summary>
    /// <param name="value">Integer value to evaluate</param>
    /// <param name="targetType">Target type (bool)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>True if integer > 0, false otherwise</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0;
        }
        return false;
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean values to colors based on parameter configuration
/// Parameter format: "TrueColor|FalseColor" (hex values)
/// Example usage: ConverterParameter="#4CAF50|#F44336"
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    /// <summary>
    /// Converts boolean to color based on parameter mapping
    /// </summary>
    /// <param name="value">Boolean value to evaluate</param>
    /// <param name="targetType">Target type (Color)</param>
    /// <param name="parameter">Color mapping in format "TrueColor|FalseColor"</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>Color based on boolean value and parameter mapping</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colorParams)
        {
            var colors = colorParams.Split('|');
            if (colors.Length == 2)
            {
                var trueColor = colors[0].Trim();
                var falseColor = colors[1].Trim();

                try
                {
                    return boolValue ? Color.FromArgb(trueColor) : Color.FromArgb(falseColor);
                }
                catch
                {
                    // Return transparent if color parsing fails
                    return Colors.Transparent;
                }
            }
        }

        return Colors.Transparent;
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean values to integers based on parameter configuration
/// Parameter format: "TrueValue|FalseValue" (integer values)
/// Example usage: ConverterParameter="1|0" or "100|50"
/// </summary>
public class BoolToIntConverter : IValueConverter
{
    /// <summary>
    /// Converts boolean to integer based on parameter mapping
    /// </summary>
    /// <param name="value">Boolean value to evaluate</param>
    /// <param name="targetType">Target type (int)</param>
    /// <param name="parameter">Integer mapping in format "TrueValue|FalseValue"</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>Integer based on boolean value and parameter mapping</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string intParams)
        {
            var values = intParams.Split('|');
            if (values.Length == 2 &&
                int.TryParse(values[0], out var trueValue) &&
                int.TryParse(values[1], out var falseValue))
            {
                return boolValue ? trueValue : falseValue;
            }
        }

        return 0;
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean values to strings based on parameter configuration
/// Parameter format: "TrueString|FalseString"
/// Example usage: ConverterParameter="Active|Inactive" or "Yes|No"
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    /// <summary>
    /// Converts boolean to string based on parameter mapping
    /// </summary>
    /// <param name="value">Boolean value to evaluate</param>
    /// <param name="targetType">Target type (string)</param>
    /// <param name="parameter">String mapping in format "TrueString|FalseString"</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>String based on boolean value and parameter mapping</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string stringParams)
        {
            var strings = stringParams.Split('|');
            if (strings.Length == 2)
            {
                return boolValue ? strings[0] : strings[1];
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts null values to boolean for UI binding purposes
/// Returns true if value is not null, false if null
/// Useful for object existence checks in UI
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts null check to boolean - true if not null
    /// </summary>
    /// <param name="value">Value to check for null</param>
    /// <param name="targetType">Target type (bool)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>True if value is not null, false if null</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverted version of NullToBoolConverter
/// Returns true if value is null, false if not null
/// Useful for showing empty state indicators
/// </summary>
public class InvertedNullToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts null check to inverted boolean - true if null
    /// </summary>
    /// <param name="value">Value to check for null</param>
    /// <param name="targetType">Target type (bool)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>True if value is null, false if not null</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts collection count to boolean for UI binding
/// Returns true if collection has items, false if empty or null
/// Supports both ICollection and IEnumerable interfaces
/// </summary>
public class CollectionToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts collection to boolean - true if has items
    /// </summary>
    /// <param name="value">Collection to evaluate</param>
    /// <param name="targetType">Target type (bool)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>True if collection has items, false if empty or null</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Prefer ICollection for performance (O(1) Count access)
        if (value is System.Collections.ICollection collection)
        {
            return collection.Count > 0;
        }

        // Fallback to IEnumerable (requires enumeration)
        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object>().Any();
        }

        return false;
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts sort order strings to corresponding Unicode icons
/// Used for displaying sort direction indicators in UI
/// Supports common sorting patterns with intuitive icons
/// </summary>
public class SortToIconConverter : IValueConverter
{
    /// <summary>
    /// Converts sort order string to appropriate Unicode icon
    /// </summary>
    /// <param name="value">Sort order string</param>
    /// <param name="targetType">Target type (string)</param>
    /// <param name="parameter">Optional parameter (not used)</param>
    /// <param name="culture">Culture info for conversion</param>
    /// <returns>Unicode icon representing the sort order</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string sortOrder)
        {
            return sortOrder switch
            {
                "Name A→Z" => "🔤↑",      // Alphabetical ascending
                "Name Z→A" => "🔤↓",      // Alphabetical descending
                "Recent First" => "🕐↓",   // Chronological descending
                "Oldest First" => "🕐↑",   // Chronological ascending
                _ => "📊"                  // Default/unsorted icon
            };
        }
        return "📊";
    }

    /// <summary>
    /// ConvertBack not implemented - one-way binding only
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}