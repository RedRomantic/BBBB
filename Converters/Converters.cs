using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PanoramaFuturesAI.Models;

namespace PanoramaFuturesAI.Converters;

/// <summary>
/// 布尔值转可见性
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = value is bool b && b;

        // 支持 Inverse 参数：反转布尔值
        if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 布尔值取反转换器
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 字符串非空转可见性
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 市场阶段转图标
/// </summary>
public class PhaseToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is MarketPhase p ? p.GetIcon() : "?";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 市场阶段转中文名称
/// </summary>
public class PhaseToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is MarketPhase p ? p.GetChineseName() : "未知";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 市场阶段转描述
/// </summary>
public class PhaseToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is MarketPhase p ? p.GetDescription() : "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 市场阶段转颜色
/// </summary>
public class PhaseToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MarketPhase p)
        {
            var hex = p.GetColor();
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        return new SolidColorBrush(Colors.Gray);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 市场偏向转文本
/// </summary>
public class BiasToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int b) return b switch { 1 => "多头", -1 => "空头", _ => "中性" };
        return "中性";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 市场偏向转颜色
/// </summary>
public class BiasToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int b)
        {
            return b switch
            {
                1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950")),
                -1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E"))
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 风险等级转文本
/// </summary>
public class RiskToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int r) return r switch { 1 => "低风险", 2 => "中等风险", 3 => "较高风险", 4 => "高风险", _ => "未知" };
        return "未知";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// 风险等级转颜色
/// </summary>
public class RiskToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int r)
        {
            var hex = r switch { 1 => "#3FB950", 2 => "#F0883E", 3 => "#F85149", 4 => "#FF0000", _ => "#8B949E" };
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        return new SolidColorBrush(Colors.Gray);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
