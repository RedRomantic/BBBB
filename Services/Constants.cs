namespace PanoramaFuturesAI.Services;

/// <summary>
/// 全局常量定义
/// </summary>
public static class Constants
{
    // ==================== API 配置 ====================

    /// <summary>
    /// 是否使用测试网（默认为 false 使用主网）
    /// </summary>
    public const bool UseTestnet = false;

    /// <summary>
    /// 币安 USD-M 永续合约 API 基础地址（主网）
    /// </summary>
    public const string BinanceFuturesApiBaseMainnet = "https://fapi.binance.com";

    /// <summary>
    /// 币安 USD-M 永续合约 API 基础地址（测试网）
    /// </summary>
    public const string BinanceFuturesApiBaseTestnet = "https://testnet.binancefuture.com";

    /// <summary>
    /// 根据是否使用测试网获取 API 基础地址
    /// </summary>
    public static string BinanceFuturesApiBase => UseTestnet ? BinanceFuturesApiBaseTestnet : BinanceFuturesApiBaseMainnet;

    /// <summary>
    /// 币安现货 API 基础地址（主网）
    /// </summary>
    public const string BinanceSpotApiBase = "https://api.binance.com";

    /// <summary>
    /// 获取合约信息的接口
    /// </summary>
    public const string ExchangeInfoEndpoint = "/fapi/v1/exchangeInfo";

    /// <summary>
    /// 获取K线数据的接口
    /// </summary>
    public const string KlinesEndpoint = "/fapi/v1/klines";

    /// <summary>
    /// 获取24小时行情接口
    /// </summary>
    public const string Ticker24hEndpoint = "/fapi/v1/ticker/24hr";

    /// <summary>
    /// 获取顶部合约接口
    /// </summary>
    public const string TopLongShortAccountEndpoint = "/fapi/v1/topLongShortAccountRatio";

    /// <summary>
    /// 获取资金费率接口
    /// </summary>
    public const string PremiumIndexEndpoint = "/fapi/v1/premiumIndex";

    /// <summary>
    /// 获取资金费率历史接口
    /// </summary>
    public const string FundingRateEndpoint = "/fapi/v1/fundingRate";

    // ==================== 数据配置 ====================

    /// <summary>
    /// 默认获取的K线数量
    /// </summary>
    public const int DefaultKlineLimit = 200;

    /// <summary>
    /// 用于分析的最少K线数量
    /// </summary>
    public const int MinKlinesForAnalysis = 50;

    /// <summary>
    /// 获取的合约数量上限（按成交量排名）
    /// </summary>
    public const int MaxContractsToAnalyze = 100;

    /// <summary>
    /// 布林带计算周期
    /// </summary>
    public const int BollingerPeriod = 20;

    /// <summary>
    /// 布林带标准差倍数
    /// </summary>
    public const decimal BollingerStdDev = 2.0m;

    /// <summary>
    /// EMA 短期周期
    /// </summary>
    public const int EmaShortPeriod = 20;

    /// <summary>
    /// EMA 中期周期
    /// </summary>
    public const int EmaMediumPeriod = 50;

    /// <summary>
    /// EMA 长期周期
    /// </summary>
    public const int EmaLongPeriod = 200;

    /// <summary>
    /// ATR 计算周期
    /// </summary>
    public const int AtrPeriod = 14;

    /// <summary>
    /// ADX 计算周期
    /// </summary>
    public const int AdxPeriod = 14;

    /// <summary>
    /// RSI 计算周期
    /// </summary>
    public const int RsiPeriod = 14;

    // ==================== 阶段判断阈值 ====================

    /// <summary>
    /// 阶段一：ADX低于此值认为是无趋势
    /// </summary>
    public const decimal ADXWeakTrendThreshold = 20m;

    /// <summary>
    /// 阶段一：BBW历史百分位低于此值认为是低波
    /// </summary>
    public const decimal BBWLowPercentileThreshold = 20m;

    /// <summary>
    /// 阶段二：成交量倍数超过此值认为是激增
    /// </summary>
    public const decimal VolumeSurgeThreshold = 1.5m;

    /// <summary>
    /// 阶段三：ADX高于此值认为是强趋势
    /// </summary>
    public const decimal ADXStrongTrendThreshold = 25m;

    /// <summary>
    /// 阶段四：价格偏离EMA20超过此百分比认为是严重偏离
    /// </summary>
    public const decimal PriceDeviationThreshold = 5m;

    /// <summary>
    /// 阶段四：资金费率超过此值（绝对值）认为是极端
    /// </summary>
    public const decimal FundingRateExtremeThreshold = 0.0005m;

    // ==================== HTTP 配置 ====================

    /// <summary>
    /// HTTP 请求超时时间（毫秒）
    /// </summary>
    public const int HttpTimeoutMs = 60000;  // 改为 60 秒，适应网络较慢的环境

    /// <summary>
    /// 请求间隔（毫秒），用于避免触发 API 限流
    /// </summary>
    public const int RequestIntervalMs = 50;

    /// <summary>
    /// 最大并发请求数
    /// </summary>
    public const int MaxConcurrentRequests = 10;

    // ==================== UI 配置 ====================

    /// <summary>
    /// 图表刷新间隔（毫秒）
    /// </summary>
    public const int ChartRefreshIntervalMs = 1000;

    /// <summary>
    /// 自动刷新间隔（毫秒），0表示禁用
    /// </summary>
    public const int AutoRefreshIntervalMs = 0;

    // ==================== LLM 配置 ====================

    /// <summary>
    /// 默认 LLM 端点
    /// </summary>
    public const string DefaultLLMEndpoint = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// 默认 LLM 模型
    /// </summary>
    public const string DefaultLLMModel = "gpt-4";

    /// <summary>
    /// 最大流式输出 token 数
    /// </summary>
    public const int MaxStreamingTokens = 4000;

    /// <summary>
    /// 默认 LLM 温度
    /// </summary>
    public const double DefaultLLMTemperature = 0.7;
}
