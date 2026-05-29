using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PanoramaFuturesAI.Models;

namespace PanoramaFuturesAI.Services;

/// <summary>
/// AI模型配置服务
/// 负责模型的加载、保存、测试连接等操作
/// </summary>
public class ModelConfigService
{
    private readonly string _configFilePath;
    private AIModelConfigCollection _config;

    public ModelConfigService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PanoramaFuturesAI");
        
        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);
        
        _configFilePath = Path.Combine(appDataPath, "ai_models_config.json");
        _config = LoadConfig();
    }

    #region 配置加载与保存

    private AIModelConfigCollection LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<AIModelConfigCollection>(json);
                if (config != null)
                    return config;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
        }

        return CreateDefaultConfig();
    }

    private void SaveConfig()
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
            System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
        }
    }

    private AIModelConfigCollection CreateDefaultConfig()
    {
        var config = new AIModelConfigCollection
        {
            NextId = 1,
            Models = new List<AIModelConfig>
            {
                new AIModelConfig
                {
                    Id = 1,
                    Name = "gemini",
                    DisplayName = "Google Gemini",
                    ApiUrl = "https://generativelanguage.googleapis.com/v1beta/openai/",
                    DefaultModel = "gemini-2.0-flash",
                    Provider = "Google",
                    Purpose = "Summary",
                    IsDefault = true
                },
                new AIModelConfig
                {
                    Id = 2,
                    Name = "volcano",
                    DisplayName = "火山引擎（豆包）",
                    ApiUrl = "https://ark.cn-beijing.volces.com/api/v3",
                    DefaultModel = "doubao-seed-2-0-mini-260215",
                    Provider = "Volcano",
                    Purpose = "Summary"
                }
            }
        };

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"创建默认配置失败: {ex.Message}");
        }

        return config;
    }

    #endregion

    #region 模型管理

    /// <summary>
    /// 获取所有模型
    /// </summary>
    public List<AIModelConfig> GetAllModels()
    {
        return _config.Models.ToList();
    }

    /// <summary>
    /// 获取默认模型（用于Summary）
    /// </summary>
    public AIModelConfig? GetDefaultModel()
    {
        return _config.Models.FirstOrDefault(m => m.IsDefault && m.Purpose == "Summary");
    }

    /// <summary>
    /// 获取 Deep Research 专用模型
    /// </summary>
    public AIModelConfig? GetDeepResearchModel()
    {
        return _config.Models.FirstOrDefault(m => m.Purpose == "DeepResearch");
    }

    /// <summary>
    /// 获取模型配置
    /// </summary>
    public AIModelConfig? GetModelByName(string name)
    {
        return _config.Models.FirstOrDefault(m => m.Name == name);
    }

    /// <summary>
    /// 获取模型配置
    /// </summary>
    public AIModelConfig? GetModelById(int id)
    {
        return _config.Models.FirstOrDefault(m => m.Id == id);
    }

    /// <summary>
    /// 添加新模型
    /// </summary>
    public AIModelConfig AddModel(AIModelConfig model)
    {
        model.Id = _config.NextId++;
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;
        _config.Models.Add(model);
        SaveConfig();
        return model;
    }

    /// <summary>
    /// 更新模型
    /// </summary>
    public void UpdateModel(AIModelConfig model)
    {
        var existing = _config.Models.FirstOrDefault(m => m.Id == model.Id);
        if (existing != null)
        {
            var index = _config.Models.IndexOf(existing);
            model.UpdatedAt = DateTime.Now;
            _config.Models[index] = model;
            SaveConfig();
        }
    }

    /// <summary>
    /// 删除模型
    /// </summary>
    public void DeleteModel(int id)
    {
        var model = _config.Models.FirstOrDefault(m => m.Id == id);
        if (model != null)
        {
            _config.Models.Remove(model);
            SaveConfig();
        }
    }

    /// <summary>
    /// 设置默认模型
    /// </summary>
    public void SetDefaultModel(int id, string purpose)
    {
        foreach (var model in _config.Models.Where(m => m.Purpose == purpose))
        {
            model.IsDefault = model.Id == id;
        }
        SaveConfig();
    }

    #endregion

    #region 连接测试

    /// <summary>
    /// 测试模型连接
    /// </summary>
    public async Task<(bool Success, string Message)> TestModelAsync(AIModelConfig model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.ApiUrl) || string.IsNullOrWhiteSpace(model.ApiKey))
            {
                return (false, "API地址和API Key不能为空");
            }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            var baseUrl = model.ApiUrl.TrimEnd('/');
            var url = baseUrl.EndsWith("/chat/completions")
                ? baseUrl
                : $"{baseUrl}/chat/completions";

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

            var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json")
            };
            httpReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", model.ApiKey);

            var resp = await httpClient.SendAsync(httpReq);

            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        var answer = content.GetString() ?? "";
                        var preview = answer.Length > 100 ? answer.Substring(0, 100) + "..." : answer;
                        return (true, $"连接成功！响应: {preview}");
                    }
                }
                catch { }
                
                var responsePreview = json.Length > 200 ? json.Substring(0, 200) + "..." : json;
                return (true, $"连接成功！响应预览:\n{responsePreview}");
            }
            else
            {
                var errorContent = await resp.Content.ReadAsStringAsync();
                var preview = errorContent.Length > 200
                    ? errorContent.Substring(0, 200) + "..."
                    : errorContent;

                // 记录错误日志
                LogService.Instance.AddApiError(model.Provider, $"HTTP {(int)resp.StatusCode}", errorContent);

                return (false, $"HTTP {(int)resp.StatusCode}: {preview}");
            }
        }
        catch (TaskCanceledException)
        {
            LogService.Instance.AddApiError(model.Provider, "连接超时", "请求在30秒内未完成");
            return (false, "连接超时（30秒）");
        }
        catch (Exception ex)
        {
            LogService.Instance.AddApiError(model.Provider, ex.Message);
            return (false, $"连接失败: {ex.Message}");
        }
    }

    #endregion

    #region 用途分类

    /// <summary>
    /// 获取指定用途的模型列表
    /// </summary>
    public List<AIModelConfig> GetModelsByPurpose(string purpose)
    {
        return _config.Models.Where(m => m.Purpose == purpose).ToList();
    }

    /// <summary>
    /// 获取摘要模型列表
    /// </summary>
    public List<AIModelConfig> GetSummaryModels()
    {
        return GetModelsByPurpose("Summary");
    }

    /// <summary>
    /// 获取深度研究模型列表
    /// </summary>
    public List<AIModelConfig> GetDeepResearchModels()
    {
        return GetModelsByPurpose("DeepResearch");
    }

    #endregion
}
