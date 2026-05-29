# AI服务调用文档

## 一、概述

本系统提供两种AI调用方式：

| 模式 | 服务类 | 输出方式 | 适用场景 |
|------|--------|---------|----------|
| **异步轮询模式** | DeepResearchService | 非流式（轮询获取） | 深度研究、长任务 |
| **流式输出模式** | UnifiedAIService | SSE流式输出 | 摘要生成、快速响应 |

---

## 二、Deep Research 服务（异步轮询模式）

### 2.1 服务类

**文件位置**: `Services/DeepResearchService.cs`

### 2.2 核心方法

```csharp
/// <summary>
/// 执行深度研究
/// </summary>
/// <param name="topic">研究主题/提示词</param>
/// <param name="agentName">Agent名称，默认使用 deep-research-pro-preview-12-2025</param>
/// <param name="cancellationToken">取消令牌，用于中断任务</param>
/// <returns>研究结果文本，失败返回null</returns>
public async Task<string?> DeepResearchAsync(
    string topic,
    string agentName = "deep-research-pro-preview-12-2025",
    CancellationToken cancellationToken = default
)
```

### 2.3 工作流程

Deep Research 采用**异步轮询模式**，流程如下：

```
┌─────────────────────────────────────────────────────────────┐
│                    1. 启动任务（StartResearchAsync）        │
│   POST /v1alpha/interactions?key={API_KEY}                 │
│   Body: { "input": "主题", "agent": "xxx", "background": true }│
│   返回: { "name": "interactions/ABC123" }                  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    2. 轮询结果（PollForResultsAsync）        │
│   GET /v1alpha/interactions/{id}?key={API_KEY}             │
│   轮询间隔: 10秒                                            │
│   最大等待: 1200秒（20分钟）                                │
│   状态: in_progress → completed / failed                  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    3. 获取结果                              │
│   从 responses.outputs[0].text 获取研究结果                 │
└─────────────────────────────────────────────────────────────┘
```

### 2.4 调用示例

```csharp
// 在 WorkflowViewModel 中调用
var _deepResearchService = new DeepResearchService();

// 基本调用
var result = await _deepResearchService.DeepResearchAsync("研究螺纹钢期货走势");

// 带取消令牌的调用
var cts = new CancellationTokenSource();
try
{
    var result = await _deepResearchService.DeepResearchAsync(
        "研究主题",
        cancellationToken: cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("用户取��了研究任务");
}
finally
{
    cts.Dispose();
}

// 设置自动取消（30秒后）
var cts2 = new CancellationTokenSource();
cts2.CancelAfter(TimeSpan.FromSeconds(30));
```

### 2.5 配置细节

#### 2.5.1 Agent 配置

| Agent名称 | 说明 | 适用场景 |
|-----------|------|----------|
| `deep-research-pro-preview-12-2025` | **推荐**，最新深度研究Agent | 专业研究任务 |
| `deep-research` | 基础版本 | 简单研究任务 |

**修改方法**（在 `DeepResearchService.cs` 第41行）：

```csharp
public async Task<string?> DeepResearchAsync(
    string topic,
    string agentName = "deep-research-pro-preview-12-2025",  // 修改这里
    CancellationToken cancellationToken = default
)
```

#### 2.5.2 超时设置

**轮询超时配置**（在 `PollForResultsAsync` 方法，第176行）：

```csharp
private async Task<string?> PollForResultsAsync(
    string interactionId,
    string apiKey,
    CancellationToken cancellationToken,
    int maxWaitSeconds = 1200  // 默认20分钟
)
```

**常用超时配置**：

| 超时时间 | maxWaitSeconds值 | 适用场景 |
|----------|------------------|----------|
| 10分钟 | 600 | 快速研究 |
| 20分钟 | 1200 | 默认 |
| 30分钟 | 1800 | 复杂研究 |
| 60分钟 | 3600 | 超长任务 |

#### 2.5.3 轮询间隔设置

在 `PollForResultsAsync` 方法中修改（第260行）：

```csharp
Thread.Sleep(10000); // 10秒轮询间隔（毫秒）
```

**常用间隔配置**：

| 间隔 | 毫秒值 | 适用场景 |
|------|--------|----------|
| 快速 | 5000 | 需要快速响应 |
| 标准 | 10000 | 默认，推荐 |
| 慢速 | 30000 | 减少API调用 |

#### 2.5.4 HTTP超时设置

在构造函数中设置（第30-32行）：

```csharp
_httpClient = new HttpClient(handler)
{
    Timeout = TimeSpan.FromMinutes(5)  // 5分钟
};
```

### 2.6 API端点详解

#### 启动任务

```
POST https://generativelanguage.googleapis.com/v1alpha/interactions?key={API_KEY}
Content-Type: application/json
```

**Request Body**:
```json
{
  "input": "研究主题内容，可以是任意文本...",
  "agent": "deep-research-pro-preview-12-2025",
  "background": true
}
```

**Response**:
```json
{
  "name": "interactions/ABC123XYZ789",
  "id": "ABC123XYZ789",
  "status": "in_progress"
}
```

#### 轮询结果

```
GET https://generativelanguage.googleapis.com/v1alpha/interactions/{interactionId}?key={API_KEY}
```

**Response (进行中)**:
```json
{
  "status": "in_progress"
}
```

**Response (完成)**:
```json
{
  "status": "completed",
  "outputs": [
    {
      "text": "研究结果内容..."
    }
  ]
}
```

**Response (失败)**:
```json
{
  "status": "failed",
  "error": "错误描述信息"
}
```

### 2.7 状态事件

订阅状态变化以获取实时进度：

```csharp
var service = new DeepResearchService();

service.StatusChanged += (sender, e) =>
{
    Console.WriteLine($"[{e.Status}] {e.Message}");
    
    // 可用状态值：
    // - starting: 正在启动任务
    // - polling: 等待中（显示轮询次数和已等待时间）
    // - completed: 任务完成
    // - failed: 任务失败
    // - cancelled: 用户取消
    // - timeout: 超时
};

var result = await service.DeepResearchAsync("研究主题");
```

### 2.8 完整示例

```csharp
public async Task<string?> ExecuteDeepResearch(string topic)
{
    var service = new DeepResearchService();
    
    // 监听状态变化
    service.StatusChanged += (s, e) =>
    {
        Console.WriteLine($"[{e.Status}] {e.Message}");
    };
    
    try
    {
        // 执行深度研究（默认20分钟超时）
        var result = await service.DeepResearchAsync(topic);
        
        if (!string.IsNullOrEmpty(result))
        {
            Console.WriteLine($"研究完成，结果长度: {result.Length} 字符");
            return result;
        }
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("401"))
    {
        Console.WriteLine("API Key无效或已过期");
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("超时"))
    {
        Console.WriteLine("研究超时");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("任务已取消");
    }
    
    return null;
}
```

---

## 三、向量AI服务（流式输出模式）

### 3.1 服务类

**文件位置**: `Services/UnifiedAIService.cs`

向量AI服务是一个统一的AI接口，支持多种AI提供商（如Google Gemini、火山引擎、向量聚合平台等），通过OpenAI兼容格式调用。

### 3.2 核心方法

| 方法 | 说明 | 返回值 |
|------|------|--------|
| `SendPromptAsync(prompt, modelName?)` | 发送单次提示请求 | `Task<string?>` |
| `SendMessagesAsync(messages, modelName?)` | 发送多轮对话消息 | `Task<string?>` |
| `GetCurrentModel()` | 获取当前默认模型配置 | `AIModelConfig?` |
| `GetModelByName(name)` | 根据名称获取模型 | `AIModelConfig?` |
| `GetAllModels()` | 获取所有已配置的模型 | `List<AIModelConfig>` |

### 3.3 工作流程

向量AI采用**流式输出模式（SSE）**：

```
┌─────────────────────────────────────────────────────────────┐
│                    发送请求                                 │
│   POST {ApiUrl}/chat/completions                           │
│   Header: Authorization: Bearer {ApiKey}                   │
│   Body: { "model": "xxx", "messages": [...], "stream": true }│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    接收SSE流                               │
│   data: {"choices":[{"delta":{"content":"部分内容"}}]}     │
│   data: {"choices":[{"delta":{"content":"更多内容"}}]}     │
│   ...                                                      │
│   data: [DONE]                                             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    合并结果                                 │
│   从每个chunk中提取content，拼接成完整响应                   │
└─────────────────────────────────────────────────────────────┘
```

### 3.4 调用示例

```csharp
var _unifiedAIService = new UnifiedAIService();

// 使用默认模型发送提示
var response = await _unifiedAIService.SendPromptAsync("请总结以下内容：...");

// 使用指定模型发送提示
var response = await _unifiedAIService.SendPromptAsync("你的问题", "gemini");

// 多轮对话示例
var messages = new object[]
{
    new { role = "system", content = "你是一个助手" },
    new { role = "user", content = "你好" },
    new { role = "assistant", content = "你好！有什么可以帮助你的吗？" },
    new { role = "user", content = "再见" }
};
var response = await _unifiedAIService.SendMessagesAsync(messages);
```

### 3.5 各模型配置详解

向量AI支持多种模型，以下是各模型的详细配置：

#### 3.5.1 Google Gemini（推荐）

| 配置项 | 值 |
|--------|-----|
| ApiUrl | `https://generativelanguage.googleapis.com/v1beta/openai/` |
| DefaultModel | `gemini-2.0-flash` 或 `gemini-1.5-flash` |
| 格式 | OpenAI Chat Completions |
| 支持流式 | 是 |

**可用模型列表**：
- `gemini-2.0-flash` - 最新快速模型（推荐）
- `gemini-1.5-flash` - 稳定快速模型
- `gemini-1.5-pro` - 高性能模型
- `gemini-pro` - 标准模型

#### 3.5.2 火山引擎（豆包）

| 配置项 | 值 |
|--------|-----|
| ApiUrl | `https://ark.cn-beijing.volces.com/api/v3` |
| DefaultModel | `doubao-seed-2-0-mini-260215` |
| 格式 | OpenAI Chat Completions |
| 支持流式 | 是 |

**可用模型列表**：
- `doubao-seed-2-0-mini-260215` - 豆包最新模型
- `doubao-pro-32k` - 专业版32K上下文
- `doubao-general-32k` - 通用版32K上下文

#### 3.5.3 向量聚合平台

| 配置项 | 值 |
|--------|-----|
| ApiUrl | `https://api.vectorengine.ai` |
| DefaultModel | `gpt-4o` 或 `gpt-4o-mini` |
| 格式 | OpenAI Chat Completions |
| 支持流式 | 是 |

**可用模型列表**：
- `gpt-4o` - OpenAI最新大模型
- `gpt-4o-mini` - 经济实惠版本
- `gpt-4-turbo` - 高性能版本
- `claude-3-opus` - Anthropic高性能模型
- `claude-3-sonnet` - Anthropic均衡模型

### 3.6 配置文件格式

向量AI的模型配置存储在 `ai_models_config.json` 文件中：

```json
{
  "Models": [
    {
      "Id": 1,
      "Name": "gemini",
      "DisplayName": "Google Gemini",
      "ApiUrl": "https://generativelanguage.googleapis.com/v1beta/openai/",
      "ApiKey": "YOUR_API_KEY",
      "DefaultModel": "gemini-2.0-flash",
      "Provider": "Google",
      "IsDefault": true,
      "Purpose": "Summary"
    },
    {
      "Id": 2,
      "Name": "volcano",
      "DisplayName": "火山引擎（豆包）",
      "ApiUrl": "https://ark.cn-beijing.volces.com/api/v3",
      "ApiKey": "YOUR_API_KEY",
      "DefaultModel": "doubao-seed-2-0-mini-260215",
      "Provider": "Volcano",
      "Purpose": "Summary"
    },
    {
      "Id": 3,
      "Name": "vector",
      "DisplayName": "向量聚合平台",
      "ApiUrl": "https://api.vectorengine.ai",
      "ApiKey": "YOUR_API_KEY",
      "DefaultModel": "gpt-4o",
      "Provider": "OpenAI",
      "Purpose": "Summary"
    }
  ],
  "NextId": 4
}
```

### 3.7 配置细节

#### 3.7.1 超时设置

在 `SendStreamRequestAsync` 方法中设置（第236行）：

```csharp
// 120秒超时
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
var resp = await _httpClient.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, cts.Token);
```

**常用超时配置**：

| 超时时间 | 修改值 | 适用场景 |
|----------|--------|----------|
| 60秒 | 60 | 快速请求 |
| 120秒 | 120 | 默认，推荐 |
| 300秒 | 300 | 长请求 |

#### 3.7.2 重试配置

在 `SendStreamRequestAsync` 方法中（第221-224行）：

```csharp
const int maxRetries = 3;  // 最大重试次数
var delayMs = 1000;       // 初始延迟（毫秒）
```

**指数退避**：
```csharp
// 每次重试后延迟翻倍
await Task.Delay(delayMs);
delayMs *= 2;  // 1s → 2s → 4s
```

#### 3.7.3 流式输出配置

在 `SendPromptToModelAsync` 方法中（第152行）：

```csharp
var requestBody = new
{
    model = modelId,
    messages = new[]
    {
        new { role = "user", content = prompt }
    },
    stream = true  // 开启流式输出
};
```

**关闭流式输出**（改为非流式）：
```csharp
stream = false
```

#### 3.7.4 设置默认模型

将某个模型设为默认，需要设置 `IsDefault = true`：

```json
{
  "Name": "vector",
  "IsDefault": true,
  "Purpose": "Summary"
}
```

**注意**：
- `IsDefault = true` 表示该模型用于 Summary（摘要生成）
- Deep Research 专用的 `Purpose = "DeepResearch"` 模型不受此影响

### 3.8 API格式

所有模型统一使用 OpenAI Chat Completions 格式：

```
POST {ApiUrl}/chat/completions
Headers:
  Authorization: Bearer {ApiKey}
  Content-Type: application/json
  Accept: text/event-stream

Body:
{
  "model": "{DefaultModel}",
  "messages": [
    { "role": "user", "content": "提示词内容" }
  ],
  "stream": true
}

Response (SSE):
data: {"id":"chatcmpl-xxx","choices":[{"delta":{"content":"部分内容"}}]}
data: {"id":"chatcmpl-xxx","choices":[{"delta":{"content":"更多内容"}}]}
data: [DONE]
```

### 3.9 错误处理

```csharp
try
{
    var response = await _unifiedAIService.SendPromptAsync(prompt);
    if (response == null)
    {
        Console.WriteLine("请求失败，未获取到响应");
    }
    else
    {
        Console.WriteLine($"响应长度: {response.Length}");
    }
}
catch (Exception ex)
{
    // 处理常见错误
    if (ex.Message.Contains("401"))
    {
        Console.WriteLine("API Key无效或已过期");
    }
    else if (ex.Message.Contains("429"))
    {
        Console.WriteLine("请求频率超限，请稍后重试");
    }
    else if (ex.Message.Contains("超时"))
    {
        Console.WriteLine("请求超时，请尝试更短的内容或更长的超时设置");
    }
    else if (ex.Message.Contains("rate limit"))
    {
        Console.WriteLine("触发了速率限制，增加重试间隔");
    }
    else
    {
        Console.WriteLine($"请求失败: {ex.Message}");
    }
}
```

### 3.10 最佳实践

#### 3.10.1 选择合适的模型

| 场景 | 推荐模型 | 原因 |
|------|----------|------|
| 快速摘要 | Gemini 2.0 Flash | 速度快，成本低 |
| 高质量摘要 | GPT-4o / Claude-3 | 理解能力强 |
| 中文优化 | 豆包 | 国内优化 |
| 成本优先 | GPT-4o-mini | 性价比高 |

#### 3.10.2 优化提示词

```csharp
// 好的示例
var prompt = $"请用简洁的语言总结以下内容，突出关键信息：\n\n{MarkdownInput}";

// 避免过长的上下文
var truncatedInput = MarkdownInput.Length > 10000 
    ? MarkdownInput.Substring(0, 10000) + "..." 
    : MarkdownInput;
```

#### 3.10.3 批量处理

```csharp
public async Task<List<string>> ProcessBatchAsync(List<string> prompts)
{
    var results = new List<string>();
    foreach (var prompt in prompts)
    {
        try
        {
            var result = await _unifiedAIService.SendPromptAsync(prompt);
            results.Add(result ?? "");
        }
        catch
        {
            results.Add(""); // 单个失败不影响其他
        }
        // 避免请求过于频繁
        await Task.Delay(1000);
    }
    return results;
}
```

### 3.11 在 UI 中调用

在 WPF/WinForms 中调用时，建议使用异步方式避免阻塞UI：

```csharp
// ViewModel 示例
public async Task<string?> GenerateSummaryAsync(string content)
{
    try
    {
        // 显示加载状态
        IsLoading = true;
        StatusMessage = "正在生成摘要...";

        var prompt = $"请总结以下内容：\n\n{content}";
        var result = await _unifiedAIService.SendPromptAsync(prompt);

        StatusMessage = "摘要生成完成";
        return result;
    }
    catch (Exception ex)
    {
        StatusMessage = $"生成失败: {ex.Message}";
        return null;
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

## 四、模型配置服务

### 4.1 服务类

**文件位置**: `Services/ModelConfigService.cs`

### 4.2 核心方法

```csharp
// 获取所有模型
public List<AIModelConfig> GetAllModels()

// 获取默认模型
public AIModelConfig? GetDefaultModel()

// 获取 Deep Research 专用模型
public AIModelConfig? GetDeepResearchModel()

// 获取模型配置
public AIModelConfig? GetModelByName(string name)

// 测试模型连接
public async Task<(bool Success, string Message)> TestModelAsync(AIModelConfig model)
```

### 4.3 配置文件

**配置文件位置**: `ai_models_config.json`

**配置模型结构** (`Models/AIModelConfig.cs`):

```csharp
public class AIModelConfig
{
    public int Id { get; set; }                    // 唯一标识
    public string Name { get; set; }               // 内部名称
    public string DisplayName { get; set; }        // 显示名称
    public string ApiUrl { get; set; }             // API地址
    public string ApiKey { get; set; }             // API密钥
    public string DefaultModel { get; set; }       // 默认模型
    public string Provider { get; set; }           // 提供商
    public bool IsDefault { get; set; }           // 是否默认
    public string Purpose { get; set; }           // 用途: Summary/DeepResearch
    public DateTime CreatedAt { get; set; }        // 创建时间
    public DateTime UpdatedAt { get; set; }       // 更新时间
}
```

### 4.4 Purpose 字段说明

| Purpose值 | 用途 | 使用的服务 |
|-----------|------|-----------|
| `Summary` | 摘要生成 | UnifiedAIService |
| `DeepResearch` | 深度研究 | DeepResearchService |

---

## 五、预置模型配置

系统预置了以下默认模型配置：

| 名称 | 显示名 | API地址 | 默认模型 | 用途 |
|------|--------|---------|----------|------|
| gemini | Google Gemini | `https://generativelanguage.googleapis.com/v1beta/openai/` | gemini-2.0-flash | Summary |
| gemini-deep | Google Gemini (深度研究) | `https://generativelanguage.googleapis.com/v1beta/openai/` | gemini-2.0-flash | DeepResearch |
| volcano | 火山引擎（豆包） | `https://ark.cn-beijing.volces.com/api/v3` | doubao-seed-2-0-mini-260215 | Summary |
| vector | 向量聚合平台 | `https://api.vectorengine.ai` | gpt-4o | Summary |

---

## 六、完整调用流程示例

### 6.1 在 WorkflowViewModel 中的完整流程

```csharp
// 1. 初始化服务
private readonly DeepResearchService _deepResearchService;
private readonly UnifiedAIService _unifiedAIService;
private readonly ModelConfigService _modelConfigService;

public WorkflowViewModel()
{
    _deepResearchService = new DeepResearchService();
    _unifiedAIService = new UnifiedAIService();
    _modelConfigService = new ModelConfigService();
}

// 2. 执行深度研究
public async Task ExecuteDeepResearch(PromptGroup group)
{
    // 获取 Deep Research 模型
    var deepModel = _modelConfigService.GetDeepResearchModel();
    if (deepModel == null || string.IsNullOrWhiteSpace(deepModel.ApiKey))
    {
        throw new Exception("Deep Research 模型未配置");
    }

    // 监听状态
    _deepResearchService.StatusChanged += (s, e) =>
    {
        AppendExecutionLog($"[深度研究] {e.Message}");
    };

    // 执行深度研究
    var researchResult = await _deepResearchService.DeepResearchAsync(group.Prompt);

    if (!string.IsNullOrWhiteSpace(researchResult))
    {
        // 去除Sources部分
        MarkdownInput = RemoveSourcesSection(researchResult);
    }
}

// 3. 生成AI摘要
public async Task ExecuteGenerateSummary()
{
    // 获取当前配置的模型
    var modelConfig = _unifiedAIService.GetCurrentModel();
    if (modelConfig == null)
    {
        throw new Exception("未找到AI模型配置");
    }

    var prompt = AppSettings.Default.IntegratedPromptTemplate
                      .Replace("{markdown_content}", MarkdownInput);

    var aiResponse = await _unifiedAIService.SendPromptAsync(prompt);

    if (!string.IsNullOrWhiteSpace(aiResponse))
    {
        var (summary, html) = ParseIntegratedResponse(aiResponse);
        GeneratedSummary = summary;
    }
}
```

---

## 七、配置参数速查表

### 7.1 Deep Research 可调参数

| 参数 | 位置 | 默认值 | 说明 |
|------|------|--------|------|
| Agent名称 | DeepResearchAsync 第41行 | `deep-research-pro-preview-12-2025` | 研究Agent |
| 轮询间隔 | PollForResultsAsync 第260行 | 10000ms | 每次轮询间隔 |
| 最大等待 | PollForResultsAsync 第176行 | 1200秒 | 超时时间 |
| HTTP超时 | 构造函数第32行 | 5分钟 | 请求超时 |

### 7.2 UnifiedAIService 可调参数

| 参数 | 位置 | 默认值 | 说明 |
|------|------|--------|------|
| 请求超时 | SendStreamRequestAsync 第236行 | 120秒 | 响应超时 |
| 最大重试 | SendStreamRequestAsync 第221行 | 3次 | 重试次数 |
| 初始延迟 | SendStreamRequestAsync 第222行 | 1000ms | 重试间隔 |
| 流式开关 | SendPromptToModelAsync 第152行 | true | 是否流式 |

---

## 九、模型连接测试

系统提供了模型连接测试功能，用于验证配置的AI模型是否可用。

### 9.1 测试入口

**ViewModel 位置**: `ViewModels/ModelConfigViewModel.cs`

**测试方法**: `TestConnection()` (第162-186行)

### 9.2 测试流程

```
┌─────────────────────────────────────────────────────────────┐
│                    1. 前置检查                              │
│   验证 ApiUrl 和 ApiKey 不为空                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    2. 构建请求                              │
│   POST {ApiUrl}/chat/completions                           │
│   测试问题: "中国的首都是哪里"                              │
│   超时: 30秒                                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    3. 发送请求                              │
│   Authorization: Bearer {ApiKey}                           │
│   Content-Type: application/json                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    4. 返回结果                              │
│   成功: (true, "连接成功！响应预览...")                     │
│   失败: (false, "HTTP xxx: 错误信息")                      │
└─────────────────────────────────────────────────────────────┘
```

### 9.3 测试方法源码

**ModelConfigService.cs 中的 TestModelAsync 方法**：

```csharp
/// <summary>
/// 测试模型连接
/// </summary>
/// <param name="model">要测试的模型配置</param>
/// <returns>(成功标志, 消息)</returns>
public async Task<(bool Success, string Message)> TestModelAsync(AIModelConfig model)
{
    try
    {
        // 1. 前置检查
        if (string.IsNullOrWhiteSpace(model.ApiUrl) || string.IsNullOrWhiteSpace(model.ApiKey))
        {
            return (false, "API地址和API Key不能为空");
        }

        // 2. 配置HttpClient（30秒超时）
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        // 3. 构建请求URL
        var baseUrl = model.ApiUrl.TrimEnd('/');
        var url = baseUrl.EndsWith("/chat/completions")
            ? baseUrl
            : $"{baseUrl}/chat/completions";

        // 4. 构建请求体
        var requestBody = new
        {
            model = string.IsNullOrWhiteSpace(model.DefaultModel) ? "gpt-4o" : model.DefaultModel,
            messages = new[]
            {
                new { role = "user", content = "中国的首都是哪里" }
            }
        };

        var requestJson = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        });

        // 5. 发送请求
        var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json")
        };
        httpReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", model.ApiKey);

        var resp = await httpClient.SendAsync(httpReq);

        // 6. 处理响应
        if (resp.IsSuccessStatusCode)
        {
            var json = await resp.Content.ReadAsStringAsync();
            var preview = json.Length > 500
                ? json.Substring(0, 500) + "...(已截断)"
                : json;
            return (true, $"连接成功！响应预览:\n{preview}");
        }
        else
        {
            var errorContent = await resp.Content.ReadAsStringAsync();
            var preview = errorContent.Length > 500
                ? errorContent.Substring(0, 500) + "...(已截断)"
                : errorContent;
            return (false, $"HTTP {(int)resp.StatusCode}: {preview}");
        }
    }
    catch (Exception ex)
    {
        return (false, $"连接失败: {ex.Message}");
    }
}
```

### 9.4 ViewModel 中的调用

**ModelConfigViewModel.cs 中的 TestConnection 命令**：

```csharp
[RelayCommand]
private async Task TestConnection()
{
    // 1. 前置检查
    if (string.IsNullOrWhiteSpace(CurrentModel.ApiUrl) || string.IsNullOrWhiteSpace(CurrentModel.ApiKey))
    {
        TestResult = "错误: API地址和API Key不能为空";
        return;
    }

    // 2. 显示测试状态
    IsTesting = true;
    TestResult = "正在测试连接...";

    try
    {
        // 3. 调用测试方法
        var (success, message) = await _configService.TestModelAsync(CurrentModel);

        // 4. 显示结果
        TestResult = success ? $"✓ 测试成功！{message}" : $"✗ 测试失败: {message}";
    }
    catch (Exception ex)
    {
        TestResult = $"✗ 测试异常: {ex.Message}";
    }
    finally
    {
        IsTesting = false;
    }
}
```

### 9.5 测试参数说明

| 参数 | 值 | 说明 |
|------|-----|------|
| 测试问题 | "中国的首都是哪里" | 简单的中文测试问题 |
| 超时时间 | 30秒 | 避免长时间等待 |
| 默认模型 | gpt-4o | 如果未配置DefaultModel时使用 |
| 响应预览 | 最多500字符 | 避免过长输出 |

### 9.6 调用示例

```csharp
var configService = new ModelConfigService();

// 测试已配置的模型
var model = configService.GetModelByName("gemini");
if (model != null)
{
    var (success, message) = await configService.TestModelAsync(model);
    Console.WriteLine(success ? $"✓ {message}" : $"✗ {message}");
}

// 测试所有模型
var allModels = configService.GetAllModels();
foreach (var m in allModels)
{
    var (success, message) = await configService.TestModelAsync(m);
    Console.WriteLine($"[{m.DisplayName}] {(success ? "✓" : "✗")}");
}
```

### 9.7 常见测试结果

| 状态码 | 含义 | 解决方法 |
|--------|------|----------|
| 200 | 连接成功 | 模型配置正确 |
| 401 | 认证失败 | 检查API Key是否正确 |
| 403 | 权限不足 | 检查API Key权限 |
| 404 | 接口不存在 | 检查ApiUrl是否正确 |
| 429 | 请求过于频繁 | 稍后重试 |
| 500 | 服务器错误 | 检查服务提供商状态 |
| 超时 | 请求超时 | 检查网络或API服务 |

---

## 十、注意事项

1. **API Key 安全**: 不要将包含真实API Key的配置文件提交到代码仓库
2. **超时设置**: 根据任务复杂度合理设置超时时间
3. **取消支持**: 建议使用 CancellationToken 支持取消操作
4. **重试机制**: UnifiedAIService 内置3次重试机制（指数退避）
5. **流式输出**: 向量AI默认使用流式输出获取响应
6. **轮询开销**: Deep Research 会持续轮询直到完成，注意API配额消耗
7. **测试建议**: 添加新模型后，建议先测试连接再使用

向量AI的，调用示例：官方

var client = new RestClient("https://api.vectorengine.ai/v1/chat/completions");
client.Timeout = -1;
var request = new RestRequest(Method.POST);
request.AddHeader("Accept", "application/json");
request.AddHeader("Authorization", "Bearer <token>");
request.AddHeader("Content-Type", "application/json");
var body = @"{" + "\n" +
@"  ""model"": ""qwen-mt-turbo""," + "\n" +
@"  ""messages"": [" + "\n" +
@"    {" + "\n" +
@"      ""role"": ""user""," + "\n" +
@"      ""content"": ""看完这个视频我没有笑""" + "\n" +
@"    }" + "\n" +
@"  ]," + "\n" +
@"  ""translation_options"": {" + "\n" +
@"    ""source_lang"": ""auto""," + "\n" +
@"    ""target_lang"": ""English""" + "\n" +
@"  }" + "\n" +
@"}";
request.AddParameter("application/json", body,  ParameterType.RequestBody);
IRestResponse response = client.Execute(request);
Console.WriteLine(response.Content);

向量AI调用qwen的网址：
https://vectorengine.apifox.cn/api-349239091


向量ai 生图

var client = new RestClient("https://api.vectorengine.ai/v1beta/models/gemini-2.5-flash-image-preview:generateContent?key=sk-TNd5Ot40Hw4cVB65tBsRPJEpxRh4t1tZz3LrPlYzpkUINgzB");
client.Timeout = -1;
var request = new RestRequest(Method.POST);
request.AddHeader("Authorization", "Bearer <token>");
request.AddHeader("Content-Type", "application/json");
var body = @"{
" + "\n" +
@"    ""contents"": [
" + "\n" +
@"        {
" + "\n" +
@"            ""role"": ""user"",
" + "\n" +
@"            ""parts"": [
" + "\n" +
@"                {
" + "\n" +
@"                    ""text"": ""HA [style] sticker of a [subject], featuring [key characteristics] and a [color palette]. The design should have [line style] and [shading style]. The background must be transparent.""
" + "\n" +
@"                }
" + "\n" +
@"            ]
" + "\n" +
@"        }
" + "\n" +
@"    ],
" + "\n" +
@"    ""generationConfig"": {
" + "\n" +
@"        ""responseModalities"": [
" + "\n" +
@"            ""IMAGE""
" + "\n" +
@"        ],
" + "\n" +
@"        ""imageConfig"": {
" + "\n" +
@"            ""aspectRatio"": ""9:16"",
" + "\n" +
@"            ""imageSize"": ""1K""
" + "\n" +
@"        }
" + "\n" +
@"    }
" + "\n" +
@"}";
request.AddParameter("application/json", body,  ParameterType.RequestBody);
IRestResponse response = client.Execute(request);
Console.WriteLine(response.Content);