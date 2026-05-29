using System;
using System.Collections.Generic;

namespace PanoramaFuturesAI.Models;

/// <summary>
/// K线数据结构（对应币安API返回的K线数据）
/// </summary>
public class KLine
{
    /// <summary>
    /// K线开盘时间（Unix时间戳，毫秒）
    /// </summary>
    public long OpenTime { get; set; }

    /// <summary>
    /// K线开盘价
    /// </summary>
    public decimal Open { get; set; }

    /// <summary>
    /// K线最高价
    /// </summary>
    public decimal High { get; set; }

    /// <summary>
    /// K线最低价
    /// </summary>
    public decimal Low { get; set; }

    /// <summary>
    /// K线收盘价
    /// </summary>
    public decimal Close { get; set; }

    /// <summary>
    /// K线成交量
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// K线收盘时间（Unix时间戳，毫秒）
    /// </summary>
    public long CloseTime { get; set; }

    /// <summary>
    /// 成交额（Quote Asset Volume）
    /// </summary>
    public decimal QuoteVolume { get; set; }

    /// <summary>
    /// 交易次数
    /// </summary>
    public int TradeCount { get; set; }

    /// <summary>
    /// Taker买入量
    /// </summary>
    public decimal TakerBuyVolume { get; set; }

    /// <summary>
    /// Taker卖出量
    /// </summary>
    public decimal TakerSellVolume { get; set; }

    /// <summary>
    /// 转换为 DateTime 格式的开盘时间
    /// </summary>
    public DateTime OpenTimeDate => DateTimeOffset.FromUnixTimeMilliseconds(OpenTime).DateTime;

    /// <summary>
    /// 转换为 DateTime 格式的收盘时间
    /// </summary>
    public DateTime CloseTimeDate => DateTimeOffset.FromUnixTimeMilliseconds(CloseTime).DateTime;
}

/// <summary>
/// K线时间周期枚举
/// </summary>
public enum KlineInterval
{
    /// <summary>1分钟</summary>
    m1 = 1,
    /// <summary>5分钟</summary>
    m5 = 5,
    /// <summary>15分钟</summary>
    m15 = 15,
    /// <summary>30分钟</summary>
    m30 = 30,
    /// <summary>1小时</summary>
    h1 = 60,
    /// <summary>4小时</summary>
    h4 = 240,
    /// <summary>1天</summary>
    d1 = 1440
}

/// <summary>
/// K线周期扩展方法
/// </summary>
public static class KlineIntervalExtensions
{
    /// <summary>
    /// 获取周期字符串（用于币安API）
    /// </summary>
    public static string ToBinanceString(this KlineInterval interval)
    {
        return interval switch
        {
            KlineInterval.m1 => "1m",
            KlineInterval.m5 => "5m",
            KlineInterval.m15 => "15m",
            KlineInterval.m30 => "30m",
            KlineInterval.h1 => "1h",
            KlineInterval.h4 => "4h",
            KlineInterval.d1 => "1d",
            _ => "1h"
        };
    }

    /// <summary>
    /// 获取周期中文名称
    /// </summary>
    public static string GetChineseName(this KlineInterval interval)
    {
        return interval switch
        {
            KlineInterval.m1 => "1分钟",
            KlineInterval.m5 => "5分钟",
            KlineInterval.m15 => "15分钟",
            KlineInterval.m30 => "30分钟",
            KlineInterval.h1 => "1小时",
            KlineInterval.h4 => "4小时",
            KlineInterval.d1 => "1天",
            _ => "未知"
        };
    }
}
