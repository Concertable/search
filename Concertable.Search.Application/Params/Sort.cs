using System.ComponentModel;
using System.Globalization;

namespace Concertable.Search.Application.Params;

public enum SortField { Name, Date }

public enum SortDirection { Asc, Desc }

[TypeConverter(typeof(Converter))]
public readonly record struct Sort(SortField Field, SortDirection Direction = SortDirection.Asc)
{
    public static bool TryParse(string? value, out Sort sort)
    {
        sort = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var segments = value.Split('_');
        if (segments.Length > 2 || !Enum.TryParse<SortField>(segments[0], ignoreCase: true, out var field))
            return false;

        if (segments.Length == 1)
        {
            sort = new Sort(field);
            return true;
        }

        if (!Enum.TryParse<SortDirection>(segments[1], ignoreCase: true, out var direction))
            return false;

        sort = new Sort(field, direction);
        return true;
    }

    public sealed class Converter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            TryParse(value as string, out var sort)
                ? sort
                : throw new FormatException($"'{value}' is not a valid sort.");
    }
}
