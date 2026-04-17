using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;

namespace HyundaiTransys.VisionInspection.UI.Converters;

public sealed class ResultToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            InspectionResult.Ok => new SolidColorBrush(Color.FromRgb(0x1E, 0x88, 0x45)),
            InspectionResult.Ng => new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
            _                   => new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
        };

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

public sealed class ResultToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            InspectionResult.Ok => "OK",
            InspectionResult.Ng => "NG",
            _                   => "—"
        };

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}

public sealed class BoolToConnectionTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "ONLINE" : "OFFLINE";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}
