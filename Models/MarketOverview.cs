using System;
using System.Collections.Generic;

namespace PanoramaFuturesAI.Models;

/// <summary>
/// 市场宏观分析结果
/// </summary>
public class MarketOverview
{
    /// <summary>
    /// 分析时间
    /// </summary>
    public DateTime AnalysisTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 当前宏观市场阶段
    /// </summary>
    public MarketPhase CurrentPhase { get; set; } = MarketPhase.Unknown;

    /// <summary>
    /// 阶段匹配分数 (0-100)
    /// </summary>
    public decimal PhaseMatchScore { get; set; }

    /// <summary>
    /// 分析的合约数量
    /// </summary>
    public int AnalyzedContractCount { get; set; }

    // ==================== 阶段一指标 ====================

    /// <summary>
    /// 全市场平均布林带宽度
    /// </summary>
    public decimal AverageBBW { get; set; }

    /// <summary>
    /// 布林带宽度历史百分位
    /// </summary>
    public decimal BBWPercentile { get; set; }

    /// <summary>
    /// ADX < 20 的合约占比
    /// </summary>
    public decimal PercentContractsADXBelow20 { get; set; }

    /// <summary>
    /// 全市场平均 ATR
    /// </summary>
    public decimal AverageATR { get; set; }

    /// <summary>
    /// ATR 是否走低（相比上一周期）
    /// </summary>
    public bool IsATRDeclining { get; set; }

    // ==================== 阶段二指标 ====================

    /// <summary>
    /// 布林带宽度是否扩大
    /// </summary>
    public bool IsBBWExpanding { get; set; }

    /// <summary>
    /// 突破 EMA20 的合约占比
    /// </summary>
    public decimal PercentContractsAboveEMA20 { get; set; }

    /// <summary>
    /// 全市场总成交量相对10周期均值的倍数
    /// </summary>
    public decimal VolumeSurgeMultiplier { get; set; }

    /// <summary>
    /// 成交量是否激增
    /// </summary>
    public bool IsVolumeSpiking => VolumeSurgeMultiplier > 1.5m;

    // ==================== 阶段三指标 ====================

    /// <summary>
    /// 全市场平均 ADX
    /// </summary>
    public decimal AverageADX { get; set; }

    /// <summary>
    /// ADX 是否上升
    /// </summary>
    public bool IsADXRising { get; set; }

    /// <summary>
    /// 均线多头排列的合约占比
    /// </summary>
    public decimal PercentBullishAlignment { get; set; }

    /// <summary>
    /// 均线空头排列的合约占比
    /// </summary>
    public decimal PercentBearishAlignment { get; set; }

    /// <summary>
    /// 趋势强度：1=强多头，-1=强空头，0=无趋势
    /// </summary>
    public int TrendStrength { get; set; }

    // ==================== EMA 聚合指标 ====================

    /// <summary>
    /// 突破 EMA20 的合约占比
    /// </summary>
    public decimal PercentAboveEMA20 { get; set; }

    /// <summary>
    /// 突破 EMA50 的合约占比
    /// </summary>
    public decimal PercentAboveEMA50 { get; set; }

    /// <summary>
    /// 突破 EMA200 的合约占比
    /// </summary>
    public decimal PercentAboveEMA200 { get; set; }

    /// <summary>
    /// 平均价格偏离 EMA20 百分比
    /// </summary>
    public decimal AveragePriceDeviationFromEMA20 { get; set; }

    // ==================== 阶段四指标 ====================

    /// <summary>
    /// 严重偏离 EMA20 的合约占比（偏离>5%）
    /// </summary>
    public decimal PercentContractsFarFromEMA20 { get; set; }

    /// <summary>
    /// RSI 处于极值的合约占比
    /// </summary>
    public decimal PercentRSIExtreme { get; set; }

    /// <summary>
    /// 全市场平均资金费率
    /// </summary>
    public decimal AverageFundingRate { get; set; }

    /// <summary>
    /// 资金费率是否极端
    /// </summary>
    public bool IsFundingRateExtreme => Math.Abs(AverageFundingRate) > 0.0005m;

    /// <summary>
    /// 费率极端程度：正=多方狂热，负=空方狂热
    /// </summary>
    public decimal FundingRateSeverity => AverageFundingRate * 10000;

    // ==================== 归因分析 ====================

    /// <summary>
    /// 支持当前阶段的证据列表
    /// </summary>
    public List<PhaseEvidence> EvidenceList { get; set; } = new();

    /// <summary>
    /// 市场主导方向：1=多头，-1=空头，0=中性
    /// </summary>
    public int MarketBias { get; set; }

    /// <summary>
    /// 市场风险等级：1=低，2=中，3=高
    /// </summary>
    public int RiskLevel { get; set; }

    /// <summary>
    /// 简短的市场评述
    /// </summary>
    public string MarketCommentary { get; set; } = string.Empty;
}

/// <summary>
/// 阶段证据（用于展示为什么处于该阶段）
/// </summary>
public class PhaseEvidence
{
    /// <summary>
    /// 证据类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 证据描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 证据数值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 证据强度：1-10
    /// </summary>
    public int Strength { get; set; }

    /// <summary>
    /// 是否是正向证据
    /// </summary>
    public bool IsPositive { get; set; }
}
