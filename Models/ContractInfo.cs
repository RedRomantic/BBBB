using System;

namespace PanoramaFuturesAI.Models;

/// <summary>
/// 合约基本信息（用于显示合约列表）
/// </summary>
public class ContractInfo
{
    /// <summary>
    /// 合约符号，如 BTCUSDT
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 合约全名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 当前价格
    /// </summary>
    public decimal LastPrice { get; set; }

    /// <summary>
    /// 24小时价格变化
    /// </summary>
    public decimal PriceChange { get; set; }

    /// <summary>
    /// 24小时价格变化百分比
    /// </summary>
    public decimal PriceChangePercent { get; set; }

    /// <summary>
    /// 24小时成交量（合约数）
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// 24小时成交额
    /// </summary>
    public decimal QuoteVolume { get; set; }

    /// <summary>
    /// 资金费率
    /// </summary>
    public decimal FundingRate { get; set; }

    /// <summary>
    /// 下次资金费率时间
    /// </summary>
    public DateTime? NextFundingTime { get; set; }

    /// <summary>
    /// 24小时最高价
    /// </summary>
    public decimal HighPrice { get; set; }

    /// <summary>
    /// 24小时最低价
    /// </summary>
    public decimal LowPrice { get; set; }

    /// <summary>
    /// 活跃做多用户数
    /// </summary>
    public int LongShortRatioAccounts { get; set; }

    /// <summary>
    /// 活跃做空用户数
    /// </summary>
    public int ShortAccounts { get; set; }

    /// <summary>
    /// 多空账户比率
    /// </summary>
    public decimal AccountLongShortRatio => ShortAccounts > 0 ? (decimal)LongShortRatioAccounts / ShortAccounts : 1;

    /// <summary>
    /// 价格方向：1=上涨，-1=下跌，0=持平
    /// </summary>
    public int PriceDirection => PriceChangePercent > 0 ? 1 : (PriceChangePercent < 0 ? -1 : 0);

    /// <summary>
    /// 价格变化颜色（用于UI）
    /// </summary>
    public string PriceChangeColor => PriceDirection switch
    {
        1 => "#3FB950",
        -1 => "#F85149",
        _ => "#8B949E"
    };
}

/// <summary>
/// 单个合约的完整数据（包含K线和指标）
/// </summary>
public class ContractData
{
    /// <summary>
    /// 合约基本信息
    /// </summary>
    public ContractInfo Info { get; set; } = new();

    /// <summary>
    /// K线数据
    /// </summary>
    public System.Collections.Generic.List<KLine> KLines { get; set; } = new();

    /// <summary>
    /// 技术指标
    /// </summary>
    public ContractIndicators Indicators { get; set; } = new();

    /// <summary>
    /// 数据获取时间
    /// </summary>
    public DateTime FetchTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否成功获取数据
    /// </summary>
    public bool IsValid => KLines.Count > 0 && Indicators.BBW > 0;
}
