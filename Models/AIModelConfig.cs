using System;
using System.Collections.Generic;

namespace PanoramaFuturesAI.Models;

/// <summary>
/// AI模型配置
/// </summary>
public class AIModelConfig
{
    /// <summary>唯一标识</summary>
    public int Id { get; set; }

    /// <summary>内部名称（如 deepseek_v3）</summary>
    public string Name { get; set; } = "";

    /// <summary>显示名称（如 DeepSeek-V3）</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>API地址（如 https://api.deepseek.com）</summary>
    public string ApiUrl { get; set; } = "";

    /// <summary>API密钥</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>默认模型ID（如 deepseek-chat）</summary>
    public string DefaultModel { get; set; } = "";

    /// <summary>提供商（OpenAI/Google/Anthropic/DeepSeek/Qwen/Volcano/Other）</summary>
    public string Provider { get; set; } = "";

    /// <summary>是否默认模型</summary>
    public bool IsDefault { get; set; }

    /// <summary>用途：Summary(摘要生成) / DeepResearch(深度研究)</summary>
    public string Purpose { get; set; } = "Summary";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取提供商的默认API地址
    /// </summary>
    public static string GetDefaultApiUrl(string provider)
    {
        return provider switch
        {
            "Google" => "https://generativelanguage.googleapis.com/v1beta/openai/",
            "DeepSeek" => "https://api.deepseek.com/v1",
            "Qwen" => "https://dashscope.aliyuncs.com/api/v1",
            "Volcano" => "https://ark.cn-beijing.volces.com/api/v3",
            "Anthropic" => "https://api.anthropic.com/v1",
            "OpenAI" => "https://api.openai.com/v1",
            _ => "https://api.openai.com/v1"
        };
    }

    /// <summary>
    /// 获取提供商的默认模型
    /// </summary>
    public static string GetDefaultModel(string provider)
    {
        return provider switch
        {
            "Google" => "gemini-2.0-flash",
            "DeepSeek" => "deepseek-chat",
            "Qwen" => "qwen-plus",
            "Volcano" => "doubao-seed-2-0-mini-260215",
            "Anthropic" => "claude-3-sonnet-20240229",
            "OpenAI" => "gpt-4o",
            _ => "gpt-4o"
        };
    }
}

/// <summary>
/// 模型配置集合（用于JSON序列化）
/// </summary>
public class AIModelConfigCollection
{
    public List<AIModelConfig> Models { get; set; } = new();
    public int NextId { get; set; } = 1;
}
