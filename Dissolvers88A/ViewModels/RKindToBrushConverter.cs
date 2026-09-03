using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Dissolvers88A.R;

namespace Dissolvers88A.ViewModels;

/// <summary>Colours a console line by its stream kind (dark-terminal palette).</summary>
public sealed class RKindToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Output = Frozen("#DDE7F5");
    private static readonly SolidColorBrush Error  = Frozen("#F87171");
    private static readonly SolidColorBrush Echo   = Frozen("#7DA9FF");
    private static readonly SolidColorBrush System = Frozen("#5B6E90");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            RStreamKind.Error => Error,
            RStreamKind.Echo  => Echo,
            RStreamKind.System => System,
            _ => Output,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush Frozen(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }
}
