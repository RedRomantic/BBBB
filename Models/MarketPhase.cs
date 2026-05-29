using System;

namespace PanoramaFuturesAI.Models;

/// <summary>
/// 宏观市场阶段枚举
/// </summary>
public enum MarketPhase
{
    /// <summary>未知/计算中</summary>
    Unknown = 0,

    /// <summary>
    /// 阶段一：低波蓄势期
    /// 市场波动率处于历史低位，成交量低迷，为后续突破蓄力
    /// </summary>
    Phase1_LowVolatilityAccumulation = 1,

    /// <summary>
    /// 阶段二：高波突破期
    /// 波动率放大，成交量激增，价格开始突破关键位置
    /// </summary>
    Phase2_HighVolatilityBreakout = 2,

    /// <summary>
    /// 阶段三：趋势主升/主跌期
    /// ADX 确认趋势，EMA 呈现多头/空头排列，趋势明确
    /// </summary>
    Phase3_TrendContinuation = 3,

    /// <summary>
    /// 阶段四：趋势衰竭期
    /// 价格严重偏离均线，RSI 极值，费率极端，市场可能反转
    /// </summary>
    Phase4_TrendExhaustion = 4
}

/// <summary>
/// 市场阶段扩展方法
/// </summary>
public static class MarketPhaseExtensions
{
    /// <summary>
    /// 获取阶段中文名称
    /// </summary>
    public static string GetChineseName(this MarketPhase phase)
    {
        return phase switch
        {
            MarketPhase.Unknown => "计算中...",
            MarketPhase.Phase1_LowVolatilityAccumulation => "阶段一",
            MarketPhase.Phase2_HighVolatilityBreakout => "阶段二",
            MarketPhase.Phase3_TrendContinuation => "阶段三",
            MarketPhase.Phase4_TrendExhaustion => "阶段四",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取阶段详细描述
    /// </summary>
    public static string GetDescription(this MarketPhase phase)
    {
        return phase switch
        {
            MarketPhase.Unknown => "正在分析市场数据...",
            MarketPhase.Phase1_LowVolatilityAccumulation => "低波蓄势期",
            MarketPhase.Phase2_HighVolatilityBreakout => "高波突破期",
            MarketPhase.Phase3_TrendContinuation => "趋势主升/主跌期",
            MarketPhase.Phase4_TrendExhaustion => "趋势衰竭期",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取阶段图标
    /// </summary>
    public static string GetIcon(this MarketPhase phase)
    {
        return phase switch
        {
            MarketPhase.Unknown => "\u23F3",
            MarketPhase.Phase1_LowVolatilityAccumulation => "\uD83C\uDF1F",
            MarketPhase.Phase2_HighVolatilityBreakout => "\uD83D\uDCA5",
            MarketPhase.Phase3_TrendContinuation => "\uD83D\uDE80",
            MarketPhase.Phase4_TrendExhaustion => "\u26A0\uFE0F",
            _ => "\u2753"
        };
    }

    /// <summary>
    /// 获取阶段颜色（用于UI显示）
    /// </summary>
    public static string GetColor(this MarketPhase phase)
    {
        return phase switch
        {
            MarketPhase.Unknown => "#8B949E",
            MarketPhase.Phase1_LowVolatilityAccumulation => "#58A6FF",
            MarketPhase.Phase2_HighVolatilityBreakout => "#F0883E",
            MarketPhase.Phase3_TrendContinuation => "#3FB950",
            MarketPhase.Phase4_TrendExhaustion => "#F85149",
            _ => "#8B949E"
        };
    }
}
