using System;

namespace PanoramaFuturesAI.Utils;

/// <summary>
/// 工具类 - 提供各种辅助方法
/// </summary>
public static class Helpers
{
    /// <summary>
    /// 格式化数字为千分位格式
    /// </summary>
    public static string FormatNumber(decimal value, int decimals = 2)
    {
        return value.ToString($"N{decimals}");
    }

    /// <summary>
    /// 格式化价格（根据价格大小自动选择精度）
    /// </summary>
    public static string FormatPrice(decimal price)
    {
        if (price >= 10000) return price.ToString("N0");
        if (price >= 100) return price.ToString("N2");
        if (price >= 1) return price.ToString("N4");
        return price.ToString("N6");
    }

    /// <summary>
    /// 格式化百分比
    /// </summary>
    public static string FormatPercent(decimal value, bool showSign = true)
    {
        var sign = showSign && value > 0 ? "+" : "";
        return $"{sign}{value:F2}%";
    }

    /// <summary>
    /// 格式化成交量
    /// </summary>
    public static string FormatVolume(decimal volume)
    {
        if (volume >= 1_000_000_000) return $"{volume / 1_000_000_000:F2}B";
        if (volume >= 1_000_000) return $"{volume / 1_000_000:F2}M";
        if (volume >= 1_000) return $"{volume / 1_000:F2}K";
        return volume.ToString("N0");
    }

    /// <summary>
    /// 获取颜色代码（上涨/下跌）
    /// </summary>
    public static string GetPriceChangeColor(decimal change)
    {
        return change >= 0 ? "#3FB950" : "#F85149";
    }

    /// <summary>
    /// 获取风险等级文本
    /// </summary>
    public static string GetRiskLevelText(int riskLevel)
    {
        return riskLevel switch { 1 => "低风险", 2 => "中等风险", 3 => "较高风险", 4 => "高风险", _ => "未知" };
    }

    /// <summary>
    /// 获取风险等级颜色
    /// </summary>
    public static string GetRiskLevelColor(int riskLevel)
    {
        return riskLevel switch { 1 => "#3FB950", 2 => "#F0883E", 3 => "#F85149", 4 => "#FF0000", _ => "#8B949E" };
    }

    /// <summary>
    /// 获取市场偏向文本
    /// </summary>
    public static string GetMarketBiasText(int bias)
    {
        return bias switch { 1 => "多头", -1 => "空头", _ => "中性" };
    }

    /// <summary>
    /// 获取市场偏向颜色
    /// </summary>
    public static string GetMarketBiasColor(int bias)
    {
        return bias switch { 1 => "#3FB950", -1 => "#F85149", _ => "#8B949E" };
    }
}
