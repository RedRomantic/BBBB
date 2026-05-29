using System;
using System.Collections.Generic;

namespace PanoramaFuturesAI.Models;

/// <summary>
/// 单个合约的技术指标数据
/// </summary>
public class ContractIndicators
{
    /// <summary>
    /// 合约符号，如 BTCUSDT
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// K线数据列表
    /// </summary>
    public List<KLine> KLines { get; set; } = new();

    // ==================== 布林带指标 ====================

    /// <summary>
    /// 布林带宽度 (Bollinger Band Width)
    /// </summary>
    public decimal BBW { get; set; }

    /// <summary>
    /// 布林带宽度历史百分位
    /// </summary>
    public decimal BBWPercentile { get; set; }

    /// <summary>
    /// 布林上轨
    /// </summary>
    public decimal BBUpper { get; set; }

    /// <summary>
    /// 布林下轨
    /// </summary>
    public decimal BBLower { get; set; }

    // ==================== 趋势指标 ====================

    /// <summary>
    /// ADX (平均方向指数)
    /// </summary>
    public decimal ADX { get; set; }

    /// <summary>
    /// +DI 方向指标
    /// </summary>
    public decimal PlusDI { get; set; }

    /// <summary>
    /// -DI 方向指标
    /// </summary>
    public decimal MinusDI { get; set; }

    // ==================== 均线指标 ====================

    /// <summary>
    /// EMA20 当前值
    /// </summary>
    public decimal EMA20 { get; set; }

    /// <summary>
    /// EMA50 当前值
    /// </summary>
    public decimal EMA50 { get; set; }

    /// <summary>
    /// EMA200 当前值
    /// </summary>
    public decimal EMA200 { get; set; }

    /// <summary>
    /// 价格与 EMA20 的偏离百分比
    /// </summary>
    public decimal PriceDeviationFromEMA20 { get; set; }

    /// <summary>
    /// 是否突破 EMA20（收盘价 > EMA20）
    /// </summary>
    public bool IsAboveEMA20 { get; set; }

    /// <summary>
    /// 是否呈现多头排列（EMA20 > EMA50 > EMA200）
    /// </summary>
    public bool IsBullishAlignment { get; set; }

    /// <summary>
    /// 是否呈现空头排列（EMA20 < EMA50 < EMA200）
    /// </summary>
    public bool IsBearishAlignment { get; set; }

    // ==================== 动量指标 ====================

    /// <summary>
    /// RSI (相对强弱指数)
    /// </summary>
    public decimal RSI { get; set; }

    /// <summary>
    /// ATR (平均真实波幅)
    /// </summary>
    public decimal ATR { get; set; }

    /// <summary>
    /// RSI 是否处于极值区域（>70 或 <30）
    /// </summary>
    public bool IsRSIExtreme => RSI > 70 || RSI < 30;

    /// <summary>
    /// RSI 是否超买（>70）
    /// </summary>
    public bool IsRSIOverbought => RSI > 70;

    /// <summary>
    /// RSI 是否超卖（<30）
    /// </summary>
    public bool IsRSIOversold => RSI < 30;

    // ==================== 成交量指标 ====================

    /// <summary>
    /// 当前成交量
    /// </summary>
    public decimal CurrentVolume { get; set; }

    /// <summary>
    /// 平均成交量（过去N周期）
    /// </summary>
    public decimal AverageVolume { get; set; }

    /// <summary>
    /// 成交量是否激增（超过平均的1.5倍）
    /// </summary>
    public bool IsVolumeSpike => AverageVolume > 0 && CurrentVolume > AverageVolume * 1.5m;

    /// <summary>
    /// 成交量倍数
    /// </summary>
    public decimal VolumeRatio => AverageVolume > 0 ? CurrentVolume / AverageVolume : 0;

    // ==================== 资金费率 ====================

    /// <summary>
    /// 资金费率
    /// </summary>
    public decimal FundingRate { get; set; }

    /// <summary>
    /// 资金费率是否极端（>0.05% 或 <-0.05%）
    /// </summary>
    public bool IsFundingRateExtreme => Math.Abs(FundingRate) > 0.0005m;

    /// <summary>
    /// 资金费率方向（1=正费率多方支付, -1=负费率空方支付）
    /// </summary>
    public int FundingRateDirection => FundingRate > 0 ? 1 : -1;
}
