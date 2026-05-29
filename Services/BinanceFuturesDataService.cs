using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using PanoramaFuturesAI.Models;

namespace PanoramaFuturesAI.Services;

/// <summary>
/// 币安 USD-M 永续合约数据服务
/// 提供并发获取K线、行情、资金费率等功能
/// </summary>
public class BinanceFuturesDataService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _rateLimiter;
    private bool _disposed;

    /// <summary>
    /// 当前加载状态
    /// </summary>
    public string LoadingStatus { get; set; } = "准备就绪";

    /// <summary>
    /// 构造函数
    /// </summary>
    public BinanceFuturesDataService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(Constants.BinanceFuturesApiBase),
            Timeout = TimeSpan.FromMilliseconds(Constants.HttpTimeoutMs)
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PanoramaFuturesAI/1.0");
        _rateLimiter = new SemaphoreSlim(Constants.MaxConcurrentRequests);
    }

    /// <summary>
    /// 获取成交量最高的合约列表
    /// </summary>
    public async Task<List<string>> GetTopContractsAsync(int limit = Constants.MaxContractsToAnalyze, CancellationToken cancellationToken = default)
    {
        try
        {
            LoadingStatus = "正在获取合约列表...";
            
            // 当 limit <= 0 时获取所有合约，否则获取成交量最高的前 limit 个
            string endpoint = Constants.Ticker24hEndpoint;
            if (limit > 0)
            {
                endpoint += $"?limit={limit}";
            }
            
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
            var tickers = await response.Content.ReadFromJsonAsync<List<Binance24hTicker>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
            if (tickers == null || tickers.Count == 0) return new List<string>();
            
            // 过滤出 USDT 永续合约并按成交量排序
            var filtered = tickers.Where(t => t.Symbol.EndsWith("USDT") && !t.Symbol.Contains("_"))
                          .OrderByDescending(t => decimal.TryParse(t.QuoteVolume, out var qv) ? qv : 0)
                          .Select(t => t.Symbol)
                          .ToList();
            
            // 如果指定了 limit，则取前 limit 个
            if (limit > 0 && filtered.Count > limit)
            {
                return filtered.Take(limit).ToList();
            }
            
            LoadingStatus = $"已获取 {filtered.Count} 个合约";
            return filtered;
        }
        catch (Exception ex)
        {
            LoadingStatus = $"获取合约列表失败: {ex.Message}";
            throw;
        }
    }

    /// <summary>
    /// 并行获取多个合约的K线数据
    /// </summary>
    public async Task<Dictionary<string, List<KLine>>> GetMultiSymbolKlinesAsync(
        List<string> symbols, KlineInterval interval, int limit = Constants.DefaultKlineLimit,
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, List<KLine>>();
        var completed = 0;
        var total = symbols.Count;
        LoadingStatus = $"正在获取 {total} 个合约的K线数据...";

        var tasks = symbols.Select(async symbol =>
        {
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var klines = await GetKlinesAsync(symbol, interval, limit, cancellationToken);
                var current = Interlocked.Increment(ref completed);
                progress?.Report($"获取K线数据... {current}/{total} ({symbol})");
                if (klines.Count >= Constants.MinKlinesForAnalysis)
                {
                    lock (result) { result[symbol] = klines; }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                var current = Interlocked.Increment(ref completed);
                progress?.Report($"获取K线数据... {current}/{total} ({symbol})");
            }
            finally { _rateLimiter.Release(); }
        });
        await Task.WhenAll(tasks);
        LoadingStatus = $"获取完成，有效数据: {result.Count}/{total} 个合约";
        return result;
    }

    /// <summary>
    /// 获取单个合约的K线数据
    /// </summary>
    public async Task<List<KLine>> GetKlinesAsync(string symbol, KlineInterval interval, int limit = Constants.DefaultKlineLimit, CancellationToken cancellationToken = default)
    {
        var intervalStr = interval.ToBinanceString();
        var url = $"{Constants.KlinesEndpoint}?symbol={symbol}&interval={intervalStr}&limit={limit}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var jsonElements = await response.Content.ReadFromJsonAsync<JsonElement[]>(cancellationToken);
        if (jsonElements == null || jsonElements.Length == 0) return new List<KLine>();
        return ParseKlines(jsonElements, symbol);
    }

    /// <summary>
    /// 获取所有合约的24小时行情数据
    /// </summary>
    public async Task<Dictionary<string, ContractInfo>> GetAll24hTickersAsync(CancellationToken cancellationToken = default)
    {
        LoadingStatus = "正在获取市场行情...";
        var response = await _httpClient.GetAsync(Constants.Ticker24hEndpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        var tickers = await response.Content.ReadFromJsonAsync<List<Binance24hTicker>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        var result = new Dictionary<string, ContractInfo>();
        if (tickers == null) return result;
        foreach (var ticker in tickers.Where(t => t.Symbol.EndsWith("USDT") && !t.Symbol.Contains("_")))
        {
            result[ticker.Symbol] = new ContractInfo
            {
                Symbol = ticker.Symbol,
                LastPrice = decimal.TryParse(ticker.LastPrice, out var lp) ? lp : 0,
                PriceChange = decimal.TryParse(ticker.PriceChange, out var pc) ? pc : 0,
                PriceChangePercent = decimal.TryParse(ticker.PriceChangePercent, out var pcp) ? pcp : 0,
                Volume = decimal.TryParse(ticker.Volume, out var v) ? v : 0,
                QuoteVolume = decimal.TryParse(ticker.QuoteVolume, out var qv) ? qv : 0,
                HighPrice = decimal.TryParse(ticker.HighPrice, out var hp) ? hp : 0,
                LowPrice = decimal.TryParse(ticker.LowPrice, out var lp2) ? lp2 : 0,
                FundingRate = 0
            };
        }
        return result;
    }

    /// <summary>
    /// 获取资金费率
    /// </summary>
    public async Task<decimal> GetFundingRateAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{Constants.PremiumIndexEndpoint}?symbol={symbol}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // 尝试备用接口：fundingRate 接口
                return await GetFundingRateFromHistoryAsync(symbol, cancellationToken);
            }
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content)) return 0;
            
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            // 尝试直接解析 lastFundingRate
            if (root.TryGetProperty("lastFundingRate", out var rateElement))
            {
                var rateStr = rateElement.GetString();
                if (decimal.TryParse(rateStr, out var rate)) return rate;
            }
            
            // 尝试解析 lastFundingRate 作为数字
            if (root.TryGetProperty("lastFundingRate", out var rateNum))
            {
                return rateNum.GetDecimal();
            }
            
            return 0;
        }
        catch { return 0; }
    }

    /// <summary>
    /// 从资金费率历史接口获取
    /// </summary>
    private async Task<decimal> GetFundingRateFromHistoryAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{Constants.FundingRateEndpoint}?symbol={symbol}&limit=1";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return 0;
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content)) return 0;
            
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            // 解析数组格式
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var firstItem = root[0];
                if (firstItem.TryGetProperty("fundingRate", out var rateElement))
                {
                    var rateStr = rateElement.GetString();
                    if (decimal.TryParse(rateStr, out var rate)) return rate;
                }
            }
            
            return 0;
        }
        catch { return 0; }
    }

    /// <summary>
    /// 并行获取多个合约的资金费率
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetMultiSymbolFundingRatesAsync(List<string> symbols, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, decimal>();
        var tasks = symbols.Select(async symbol =>
        {
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var rate = await GetFundingRateAsync(symbol, cancellationToken);
                lock (result) { result[symbol] = rate; }
            }
            catch { }
            finally { _rateLimiter.Release(); }
        });
        await Task.WhenAll(tasks);
        return result;
    }

    private static List<KLine> ParseKlines(JsonElement[] elements, string symbol)
    {
        var klines = new List<KLine>();
        foreach (var item in elements)
        {
            try
            {
                klines.Add(new KLine
                {
                    OpenTime = item[0].GetInt64(),
                    Open = decimal.Parse(item[1].GetString() ?? "0"),
                    High = decimal.Parse(item[2].GetString() ?? "0"),
                    Low = decimal.Parse(item[3].GetString() ?? "0"),
                    Close = decimal.Parse(item[4].GetString() ?? "0"),
                    Volume = decimal.Parse(item[5].GetString() ?? "0"),
                    CloseTime = item[6].GetInt64(),
                    QuoteVolume = decimal.Parse(item[7].GetString() ?? "0"),
                    TradeCount = item[8].GetInt32(),
                    TakerBuyVolume = decimal.Parse(item[9].GetString() ?? "0"),
                    TakerSellVolume = decimal.Parse(item[10].GetString() ?? "0")
                });
            }
            catch { }
        }
        return klines;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing) { _httpClient.Dispose(); _rateLimiter.Dispose(); }
            _disposed = true;
        }
    }
}

internal class Binance24hTicker
{
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
    [JsonPropertyName("lastPrice")] public string LastPrice { get; set; } = "0";
    [JsonPropertyName("priceChange")] public string PriceChange { get; set; } = "0";
    [JsonPropertyName("priceChangePercent")] public string PriceChangePercent { get; set; } = "0";
    [JsonPropertyName("volume")] public string Volume { get; set; } = "0";
    [JsonPropertyName("quoteVolume")] public string QuoteVolume { get; set; } = "0";
    [JsonPropertyName("highPrice")] public string HighPrice { get; set; } = "0";
    [JsonPropertyName("lowPrice")] public string LowPrice { get; set; } = "0";
}

internal class BinancePremiumIndex
{
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
    [JsonPropertyName("lastFundingRate")] public decimal LastFundingRate { get; set; }
    [JsonPropertyName("nextFundingTime")] public long NextFundingTime { get; set; }
}
