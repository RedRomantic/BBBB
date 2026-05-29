using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramaFuturesAI.Services;

/// <summary>
/// 通知服务 - 支持飞书 Webhook 等通知渠道
/// </summary>
public class NotificationService
{
    private readonly string _configFilePath;
    private NotificationConfig _config;
    private static NotificationService? _instance;
    private static readonly object _lock = new();
    private Timer? _scheduledTimer;
    private bool _isScheduledRunning;

    public static NotificationService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new NotificationService();
                }
            }
            return _instance;
        }
    }

    public NotificationService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PanoramaFuturesAI");

        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        _configFilePath = Path.Combine(appDataPath, "notification_config.json");
        _config = LoadConfig();

        // 自动启动定时推送（如果已启用）
        if (_config.FeishuWebhookEnabled && _config.PushMode == "Scheduled")
        {
            StartScheduledPush();
        }
    }

    /// <summary>
    /// 获取当前配置
    /// </summary>
    public NotificationConfig Config => _config;

    private NotificationConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<NotificationConfig>(json);
                if (config != null)
                    return config;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载通知配置失败: {ex.Message}");
        }

        return new NotificationConfig();
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public void SaveConfig()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存通知配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新飞书 Webhook 配置
    /// </summary>
    public void UpdateFeishuWebhook(string webhookUrl, bool enabled)
    {
        _config.FeishuWebhookUrl = webhookUrl;
        _config.FeishuWebhookEnabled = enabled;
        SaveConfig();
    }

    /// <summary>
    /// 启动定时推送
    /// </summary>
    public void StartScheduledPush()
    {
        if (_isScheduledRunning) return;

        var intervalMs = GetIntervalMilliseconds(_config.PushInterval);
        if (intervalMs <= 0) intervalMs = 86400000; // 默认24小时

        _scheduledTimer = new Timer(async _ =>
        {
            await ExecuteScheduledPushAsync();
        }, null, GetDelayToNextPush(), intervalMs);

        _isScheduledRunning = true;
        System.Diagnostics.Debug.WriteLine($"[Notification] 定时推送已启动，间隔: {_config.PushInterval}");
    }

    /// <summary>
    /// 停止定时推送
    /// </summary>
    public void StopScheduledPush()
    {
        _scheduledTimer?.Dispose();
        _scheduledTimer = null;
        _isScheduledRunning = false;
        System.Diagnostics.Debug.WriteLine("[Notification] 定时推送已停止");
    }

    private int GetDelayToNextPush()
    {
        var now = DateTime.Now;
        var scheduledHour = 9;

        if (!string.IsNullOrEmpty(_config.ScheduledTime))
        {
            var parts = _config.ScheduledTime.Split(':');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int hour))
            {
                scheduledHour = hour;
            }
        }

        var nextPush = new DateTime(now.Year, now.Month, now.Day, scheduledHour, 0, 0);
        if (nextPush <= now)
        {
            nextPush = nextPush.AddDays(1);
        }

        return (int)(nextPush - now).TotalMilliseconds;
    }

    private int GetIntervalMilliseconds(string interval)
    {
        return interval switch
        {
            "Hourly" => 3600000,
            "Every4Hours" => 14400000,
            "Every6Hours" => 21600000,
            "Every12Hours" => 43200000,
            "Daily" => 86400000,
            _ => 86400000
        };
    }

    private async Task ExecuteScheduledPushAsync()
    {
        if (!_config.FeishuWebhookEnabled || string.IsNullOrWhiteSpace(_config.FeishuWebhookUrl))
            return;

        try
        {
            var message = new FeishuWebhookMessage
            {
                MsgType = "interactive",
                Card = new FeishuCard
                {
                    Header = new FeishuCardHeader
                    {
                        Title = new FeishuCardTitle { Tag = "plain_text", Content = "定时推送通知" },
                        Template = "blue"
                    },
                    Elements = new FeishuCardElement[]
                    {
                        new FeishuCardElement
                        {
                            Tag = "div",
                            Content = $"**定时推送**: 系统运行正常\n请及时查看最新策略分析"
                        },
                        new FeishuCardElement
                        {
                            Tag = "hr"
                        },
                        new FeishuCardElement
                        {
                            Tag = "note",
                            Elements = new FeishuCardElement[]
                            {
                                new FeishuCardElement { Tag = "plain_text", Content = $"推送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(_config.FeishuWebhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _config.LastPushTime = DateTime.Now;
                SaveConfig();
                LogService.Instance.AddInfo("Feishu", "定时推送发送成功");
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.AddApiError("Feishu", $"定时推送失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 发送策略完成通知到飞书
    /// </summary>
    public async Task<bool> SendStrategyNotificationAsync(string action, int riskLevel, string summary)
    {
        if (!_config.FeishuWebhookEnabled || string.IsNullOrWhiteSpace(_config.FeishuWebhookUrl))
            return false;

        try
        {
            var message = new FeishuWebhookMessage
            {
                MsgType = "interactive",
                Card = new FeishuCard
                {
                    Header = new FeishuCardHeader
                    {
                        Title = new FeishuCardTitle { Tag = "plain_text", Content = "AI 策略生成通知" },
                        Template = action == "做多" ? "green" : action == "做空" ? "red" : "grey"
                    },
                    Elements = new FeishuCardElement[]
                    {
                        new FeishuCardElement
                        {
                            Tag = "div",
                            Content = $"**交易动作**: {action}\n**风险等级**: {riskLevel}/100\n**策略摘要**: {summary}"
                        },
                        new FeishuCardElement
                        {
                            Tag = "hr"
                        },
                        new FeishuCardElement
                        {
                            Tag = "note",
                            Elements = new FeishuCardElement[]
                            {
                                new FeishuCardElement { Tag = "plain_text", Content = $"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(_config.FeishuWebhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                LogService.Instance.AddInfo("Feishu", "飞书通知发送成功");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                LogService.Instance.AddApiError("Feishu", $"通知发送失败: {response.StatusCode}", error);
                return false;
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.AddApiError("Feishu", $"通知发送异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 测试飞书 Webhook
    /// </summary>
    public async Task<(bool Success, string Message)> TestFeishuWebhookAsync(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            return (false, "Webhook URL 不能为空");

        try
        {
            var message = new FeishuWebhookMessage
            {
                MsgType = "text",
                Content = new { text = "🔔 测试消息：全景合约市场AI决策系统通知测试成功！" }
            };

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(webhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                return (true, "测试消息发送成功！");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, $"发送失败 ({(int)response.StatusCode}): {error}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"发送异常: {ex.Message}");
        }
    }
}

/// <summary>
/// 通知配置
/// </summary>
public class NotificationConfig
{
    /// <summary>
    /// 飞书 Webhook URL
    /// </summary>
    public string FeishuWebhookUrl { get; set; } = "";

    /// <summary>
    /// 飞书 Webhook 是否启用
    /// </summary>
    public bool FeishuWebhookEnabled { get; set; } = false;

    /// <summary>
    /// 推送模式: Manual（手动）或 Scheduled（定时）
    /// </summary>
    public string PushMode { get; set; } = "Manual";

    /// <summary>
    /// 定时推送时间 (HH:mm 格式)
    /// </summary>
    public string ScheduledTime { get; set; } = "09:00";

    /// <summary>
    /// 推送间隔: Hourly, Every4Hours, Every6Hours, Every12Hours, Daily
    /// </summary>
    public string PushInterval { get; set; } = "Daily";

    /// <summary>
    /// 上次推送时间
    /// </summary>
    public DateTime? LastPushTime { get; set; }
}

/// <summary>
/// 飞书 Webhook 消息
/// </summary>
public class FeishuWebhookMessage
{
    [JsonPropertyName("msg_type")]
    public string MsgType { get; set; } = "text";

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("card")]
    public FeishuCard? Card { get; set; }
}

/// <summary>
/// 飞书卡片消息
/// </summary>
public class FeishuCard
{
    [JsonPropertyName("header")]
    public FeishuCardHeader? Header { get; set; }

    [JsonPropertyName("elements")]
    public FeishuCardElement[]? Elements { get; set; }
}

/// <summary>
/// 飞书卡片头部
/// </summary>
public class FeishuCardHeader
{
    [JsonPropertyName("title")]
    public FeishuCardTitle? Title { get; set; }

    [JsonPropertyName("template")]
    public string Template { get; set; } = "grey";
}

/// <summary>
/// 飞书卡片标题
/// </summary>
public class FeishuCardTitle
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "plain_text";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

/// <summary>
/// 飞书卡片元素
/// </summary>
public class FeishuCardElement
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("elements")]
    public FeishuCardElement[]? Elements { get; set; }
}
