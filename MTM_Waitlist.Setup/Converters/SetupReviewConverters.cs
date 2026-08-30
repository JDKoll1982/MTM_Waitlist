using System;

using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MTM_Waitlist.Module_Setup.Converters;

public sealed class CategoryAccentBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var key = value as string ?? string.Empty;

        return key switch
        {
            "Coil" => CreateBrush(0x00, 0x78, 0xD4),
            "Die" => CreateBrush(0xC2, 0x39, 0x2B),
            "Component" => CreateBrush(0x10, 0x7C, 0x10),
            "Flatstock" => CreateBrush(0x86, 0x5E, 0x00),
            _ => CreateBrush(0x5C, 0x5C, 0x5C)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        return new SolidColorBrush(ColorHelper.FromArgb(255, r, g, b));
    }
}

public sealed class StockStateBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var key = value as string ?? string.Empty;
        var mode = parameter as string ?? "Foreground";

        if (string.Equals(mode, "Background", StringComparison.OrdinalIgnoreCase))
        {
            return key switch
            {
                "OutOfStock" => CreateBrush(0xFD, 0xE7, 0xE9),
                "LowStock" => CreateBrush(0xFF, 0xF4, 0xCE),
                _ => CreateBrush(0xE8, 0xF5, 0xE9)
            };
        }

        return key switch
        {
            "OutOfStock" => CreateBrush(0xC2, 0x39, 0x2B),
            "LowStock" => CreateBrush(0x98, 0x60, 0x00),
            _ => CreateBrush(0x10, 0x7C, 0x10)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        return new SolidColorBrush(ColorHelper.FromArgb(255, r, g, b));
    }
}
