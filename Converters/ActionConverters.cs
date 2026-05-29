using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PanoramaFuturesAI.Models;

namespace PanoramaFuturesAI.Converters;

/// <summary>
/// 交易动作转换为颜色
/// </summary>
public class ActionToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TradeAction action)
        {
            return action switch
            {
                TradeAction.Long => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950")),
                TradeAction.Short => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F85149")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 交易动作转换为文字
/// </summary>
public class ActionToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TradeAction action)
        {
            return action switch
            {
                TradeAction.Long => "做多 LONG",
                TradeAction.Short => "做空 SHORT",
                _ => "观望 HOLD"
            };
        }
        return "观望 HOLD";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 交易动作转换为图标
/// </summary>
public class ActionToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TradeAction action)
        {
            return action switch
            {
                TradeAction.Long => "📈",
                TradeAction.Short => "📉",
                _ => "⏸️"
            };
        }
        return "⏸️";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
