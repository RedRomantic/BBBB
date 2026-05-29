using System.Text.Json.Serialization;

namespace PanoramaFuturesAI.Models;

/// <summary>
/// LLM API 请求模型
/// </summary>
public class LLMRequest
{
    /// <summary>
    /// API 密钥
    /// </summary>
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// API 端点
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "";

    /// <summary>
    /// 模型名称
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    /// <summary>
    /// 系统提示词
    /// </summary>
    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; set; } = "";

    /// <summary>
    /// 用户消息
    /// </summary>
    [JsonPropertyName("user_message")]
    public string UserMessage { get; set; } = "";

    /// <summary>
    /// 最大 token 数
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// 温度参数 (0-2)
    /// </summary>
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
}
