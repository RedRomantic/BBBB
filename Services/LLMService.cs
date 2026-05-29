using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PanoramaFuturesAI.Models;

namespace PanoramaFuturesAI.Services;

/// <summary>
/// LLM 服务 - 负责与 OpenAI 兼容 API 交互
/// 支持流式输出
/// </summary>
public class LLMService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public string CurrentModel { get; set; } = Constants.DefaultLLMModel;
    public string Endpoint { get; set; } = Constants.DefaultLLMEndpoint;

    public LLMService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
    }

    /// <summary>
    /// 生成策略（流式输出 + 结构化解析）
    /// </summary>
    public async Task<AiStrategyResult> StreamGenerateStrategyWithResultAsync(
        LLMRequest request,
        Action<string> onChunkReceived,
        CancellationToken cancellationToken = default)
    {
        var result = new AiStrategyResult();
        var fullResponse = new System.Text.StringBuilder();

        try
        {
            // 使用 JSON 约束的 System Prompt
            var systemPromptWithJson = GetJsonSystemPrompt(request.SystemPrompt);

            // 手动构建 JSON 以确保正确序列化
            var json = $@"{{
  ""model"": ""{EscapeJsonString(request.Model)}"",
  ""messages"": [
    {{""role"": ""system"", ""content"": ""{EscapeJsonString(systemPromptWithJson)}""}},
    {{""role"": ""user"", ""content"": ""{EscapeJsonString(request.UserMessage)}""}}
  ],
  ""max_tokens"": {request.MaxTokens},
  ""temperature"": {request.Temperature},
  ""stream"": true
}}";

            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

            // 确保 Endpoint 包含 /chat/completions 路径
            var endpoint = request.Endpoint.TrimEnd('/');
            if (!endpoint.Contains("/chat/completions"))
            {
                endpoint += "/chat/completions";
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = httpContent };
            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                LogService.Instance.AddApiError("LLM", $"HTTP {(int)response.StatusCode}", errorContent);
                result.RawReasoning = $"[错误: HTTP {(int)response.StatusCode}]\n{errorContent}";
                onChunkReceived(result.RawReasoning);
                return result;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;
                try
                {
                    var chunk = JsonSerializer.Deserialize<StreamChunk>(data);
                    var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                    if (!string.IsNullOrEmpty(content))
                    {
                        fullResponse.Append(content);
                        onChunkReceived(content);
                    }
                }
                catch { }
            }

            // 步骤A: 强制清洗 JSON
            var rawText = fullResponse.ToString();
            var cleanJson = SanitizeJsonString(rawText);

            System.Diagnostics.Debug.WriteLine($"[LLM] 原始响应长度: {rawText.Length}");
            System.Diagnostics.Debug.WriteLine($"[LLM] 清洗后JSON: {cleanJson}");

            // 步骤B: 解析清洗后的 JSON
            result = ParseStrategyResult(cleanJson, rawText);
        }
        catch (OperationCanceledException)
        {
            result.RawReasoning = "[生成已取消]";
            onChunkReceived("\n\n[生成已取消]");
        }
        catch (HttpRequestException ex)
        {
            var statusCode = ex.StatusCode.HasValue ? $"HTTP {(int)ex.StatusCode}" : "HTTP错误";
            var errorDetail = $"{statusCode}: {ex.Message}";
            LogService.Instance.AddApiError("LLM", errorDetail);
            result.RawReasoning = $"[错误: {errorDetail}]";
            onChunkReceived($"\n\n[错误: {errorDetail}]");
        }
        catch (Exception ex)
        {
            LogService.Instance.AddApiError("LLM", ex.Message);
            result.RawReasoning = $"[错误: {ex.Message}]";
            onChunkReceived($"\n\n[错误: {ex.Message}]");
        }

        return result;
    }

    /// <summary>
    /// 清洗 JSON 字符串 - 去除 Markdown 代码块和前后废话，只保留 JSON 对象
    /// </summary>
    /// <param name="input">原始 LLM 输出</param>
    /// <returns>清洗后的纯 JSON 字符串</returns>
    private static string SanitizeJsonString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "{}";

        var text = input.Trim();

        // 去除 markdown 代码块标记 ```json 或 ```
        if (text.StartsWith("```json"))
            text = text.Substring(7);
        else if (text.StartsWith("```"))
            text = text.Substring(3);
        else if (text.StartsWith("`"))
        {
            // 处理单个反引号开头的情况
            var firstTick = text.IndexOf('`');
            if (firstTick >= 0) text = text.Substring(firstTick + 1);
        }

        // 去除结尾的代码块标记
        if (text.EndsWith("```"))
            text = text.Substring(0, text.Length - 3);

        text = text.Trim();

        // 使用正则表达式查找第一个 { 和最后一个 }
        var firstBrace = text.IndexOf('{');
        var lastBrace = text.LastIndexOf('}');

        // 如果找不到有效的 JSON 对象，返回空对象
        if (firstBrace < 0 || lastBrace < 0 || firstBrace >= lastBrace)
        {
            // 尝试去除可能的前缀文字
            var jsonStart = Regex.Match(text, @"\{.*", RegexOptions.Singleline);
            if (jsonStart.Success)
            {
                var potential = jsonStart.Value;
                var lastB = potential.LastIndexOf('}');
                if (lastB > 0)
                    return potential.Substring(0, lastB + 1);
            }
            return "{}";
        }

        // 提取 JSON 对象
        return text.Substring(firstBrace, lastBrace - firstBrace + 1);
    }

    /// <summary>
    /// 解析策略结果 JSON
    /// </summary>
    /// <param name="cleanJson">清洗后的 JSON 字符串</param>
    /// <param name="rawText">原始文本（用于 raw_reasoning）</param>
    private static AiStrategyResult ParseStrategyResult(string cleanJson, string rawText)
    {
        var result = new AiStrategyResult { RawReasoning = rawText };

        try
        {
            if (string.IsNullOrWhiteSpace(cleanJson) || cleanJson == "{}")
                return result;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new TradeActionConverter() }
            };

            var parsed = JsonSerializer.Deserialize<AiStrategyResult>(cleanJson, options);
            if (parsed != null)
            {
                result = parsed;
                result.RawReasoning = rawText;
                result.IsParsed = true;
                result.UpdateGauges(); // 更新仪表盘
            }
        }
        catch (JsonException ex)
        {
            // JSON 解析失败，保持原始文本
            LogService.Instance.AddApiError("LLM", $"JSON解析失败: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LLM] JSON解析异常: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LLM] 解析中的JSON: {cleanJson}");
        }

        return result;
    }

    /// <summary>
    /// 获取 JSON 约束的系统提示词
    /// </summary>
    private static string GetJsonSystemPrompt(string basePrompt)
    {
        return $@"{basePrompt}

## 【终极约束】输出格式要求
⚠️ 你是一个 JSON 数据生成器，严禁输出任何 JSON 格式以外的文字，违者将被系统拦截。
你必须且只能返回一个有效的 JSON 对象，禁止输出任何前缀说明、后缀解释或 markdown 格式标记（如 ```json）。

JSON 结构必须严格遵循以下格式：
{{
    ""action"": ""Long""|""Short""|""Hold"",
    ""risk_level"": 0-100 的整数,
    ""suggested_position"": 0.0-100.0 的浮点数,
    ""confidence"": 0-100 的整数,
    ""strategy_summary"": ""一句话核心策略总结（不超过30字）"",
    ""raw_reasoning"": ""AI 完整的逻辑分析原文""
}}

字段说明：
- action: Long=做多, Short=做空, Hold=观望
- risk_level: 风险等级评分，0最低100最高
- suggested_position: 建议仓位百分比（0-100）
- confidence: 置信度评分，0-100
- strategy_summary: 简洁有力的策略总结
- raw_reasoning: 详细的推理过程和分析内容";
    }

    /// <summary>
    /// 生成策略（仅流式输出，保留兼容性）
    /// </summary>
    public async Task StreamGenerateStrategyAsync(LLMRequest request, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用 JSON 约束的 System Prompt
            var systemPromptWithJson = GetJsonSystemPrompt(request.SystemPrompt);

            // 手动构建 JSON 以确保正确序列化
            var json = $@"{{
  ""model"": ""{EscapeJsonString(request.Model)}"",
  ""messages"": [
    {{""role"": ""system"", ""content"": ""{EscapeJsonString(systemPromptWithJson)}""}},
    {{""role"": ""user"", ""content"": ""{EscapeJsonString(request.UserMessage)}""}}
  ],
  ""max_tokens"": {request.MaxTokens},
  ""temperature"": {request.Temperature},
  ""stream"": true
}}";

            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

            // 确保 Endpoint 包含 /chat/completions 路径
            var endpoint = request.Endpoint.TrimEnd('/');
            if (!endpoint.Contains("/chat/completions"))
            {
                endpoint += "/chat/completions";
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = httpContent };
            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                LogService.Instance.AddApiError("LLM", $"HTTP {(int)response.StatusCode}", errorContent);
                onChunkReceived($"\n\n[错误: HTTP {(int)response.StatusCode}]\n{errorContent}");
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;
                try
                {
                    var chunk = JsonSerializer.Deserialize<StreamChunk>(data);
                    var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                    if (!string.IsNullOrEmpty(content)) onChunkReceived(content);
                }
                catch { }
            }
        }
        catch (OperationCanceledException) { onChunkReceived("\n\n[生成已取消]"); }
        catch (HttpRequestException ex)
        {
            var statusCode = ex.StatusCode.HasValue ? $"HTTP {(int)ex.StatusCode}" : "HTTP错误";
            var errorDetail = $"{statusCode}: {ex.Message}";
            LogService.Instance.AddApiError("LLM", errorDetail);
            onChunkReceived($"\n\n[错误: {errorDetail}]");
        }
        catch (Exception ex) { LogService.Instance.AddApiError("LLM", ex.Message); onChunkReceived($"\n\n[错误: {ex.Message}]"); }
    }

    /// <summary>
    /// 构建市场分析 Prompt
    /// </summary>
    public static string BuildMarketAnalysisPrompt(MarketOverview overview, List<ContractData> contracts)
    {
        var phaseName = overview.CurrentPhase.GetDescription();
        var biasText = overview.MarketBias switch { 1 => "多头", -1 => "空头", _ => "中性" };
        var fundingDir = overview.AverageFundingRate > 0 ? "多方支付" : "空方支付";

        var prompt = $@"# 全景合约市场 AI 决策分析

## 当前市场阶段
- **宏观阶段**: {phaseName}（匹配度: {overview.PhaseMatchScore:F0}%）
- **市场偏向**: {biasText}
- **风险等级**: {overview.RiskLevel}/4
- **分析合约数量**: {overview.AnalyzedContractCount}
- **分析时间**: {overview.AnalysisTime:yyyy-MM-dd HH:mm:ss}

## 关键指标汇总

### 波动性分析
- 全市场平均布林带宽度: {overview.AverageBBW:P2}
- 布林带宽度历史百分位: {overview.BBWPercentile:F1}%
- 全市场平均 ATR: {overview.AverageATR:F4}

### 趋势分析
- 全市场平均 ADX: {overview.AverageADX:F1}
- 突破 EMA20 的合约占比: {overview.PercentContractsAboveEMA20:F1}%
- 多头排列合约占比: {overview.PercentBullishAlignment:F1}%
- 空头排列合约占比: {overview.PercentBearishAlignment:F1}%

### 动量分析
- RSI 极值合约占比: {overview.PercentRSIExtreme:F1}%
- 严重偏离 EMA20 合约占比: {overview.PercentContractsFarFromEMA20:F1}%

### 资金费率
- 全市场平均资金费率: {overview.AverageFundingRate * 100:F3}%
- 费率方向: {fundingDir}

## 阶段证据

";

        foreach (var evidence in overview.EvidenceList)
        {
            prompt += $"- **{evidence.Type}**: {evidence.Description}（数值: {evidence.Value}，强度: {evidence.Strength}/10）\n";
        }

        prompt += $@"

## 当前市场评述
{overview.MarketCommentary}

---

## 请基于以上量化数据，生成以下内容：

1. **市场解读**: 简要说明当前市场状态和潜在机会
2. **仓位管理建议**: 建议仓位比例（占总资金%）、入场条件、最大可承受亏损
3. **风险控制**: 止损位置建议、止盈策略、预警信号
4. **操作建议**: 建议方向（做多/做空/观望）、推荐时间框架、关键支撑/阻力位

请用专业的量化交易视角进行分析，语言简洁专业。";

        return prompt;
    }

    /// <summary>
    /// 系统提示词
    /// </summary>
    public static string GetSystemPrompt()
    {
        return @"你是一位专业的加密货币量化交易分析师，专注于币安 USD-M 永续合约市场分析。
你的分析原则：
1. 始终基于数据说话，避免空洞的描述
2. 风险控制优先，给出具体的止损止盈建议
3. 结合市场阶段给出针对性的策略
4. 策略要具体、可执行
5. 适当提示风险，特别是高风险阶段
请用专业但易懂的语言输出分析结果。";
    }

    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    protected virtual void Dispose(bool disposing) { if (!_disposed) { if (disposing) { _httpClient.Dispose(); } _disposed = true; } }

    /// <summary>
    /// 转义 JSON 字符串中的特殊字符
    /// </summary>
    private static string EscapeJsonString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

internal class StreamChunk
{
    [JsonPropertyName("choices")] public List<StreamChoice> Choices { get; set; } = new();
}

internal class StreamChoice
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("delta")] public StreamDelta Delta { get; set; } = new();
    [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
}

internal class StreamDelta
{
    [JsonPropertyName("role")] public string? Role { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }
}
