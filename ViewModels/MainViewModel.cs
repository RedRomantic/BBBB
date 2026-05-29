using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using PanoramaFuturesAI.Models;
using PanoramaFuturesAI.Services;
using PanoramaFuturesAI.Commands;

namespace PanoramaFuturesAI.ViewModels;

/// <summary>
/// 主视图模型 - MVVM架构的核心
/// 负责协调视图与业务逻辑，处理数据加载、阶段分析、AI策略生成等
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
    #region 私有字段

    private readonly BinanceFuturesDataService _binanceService;
    private readonly MarketPhaseAnalyzer _phaseAnalyzer;
    private readonly IndicatorCalculationService _indicatorService;
    private readonly LLMService _llmService;
    private readonly ModelConfigService _modelConfigService;
    private AIModelConfig? _currentModelConfig;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    #endregion

    #region 属性

    private MarketOverview _marketOverview = new();
    public MarketOverview MarketOverview
    {
        get => _marketOverview;
        set { _marketOverview = value; OnPropertyChanged(); }
    }

    private List<ContractData> _contractDataList = new();
    public List<ContractData> ContractDataList
    {
        get => _contractDataList;
        set { _contractDataList = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); }
    }

    public bool IsNotLoading => !_isLoading;

    private string _loadingStatus = "准备就绪";
    public string LoadingStatus
    {
        get => _loadingStatus;
        set { _loadingStatus = value; OnPropertyChanged(); }
    }

    private string _aiStrategyOutput = "";
    public string AIStrategyOutput
    {
        get => _aiStrategyOutput;
        set { _aiStrategyOutput = value; OnPropertyChanged(); }
    }

    private AiStrategyResult _strategyResult = new();
    public AiStrategyResult StrategyResult
    {
        get => _strategyResult;
        set { _strategyResult = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStrategyResult)); }
    }

    public bool HasStrategyResult => StrategyResult.IsParsed;

    private bool _showRawOutput = false;
    public bool ShowRawOutput
    {
        get => _showRawOutput;
        set { _showRawOutput = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 主窗口引用（用于截图）
    /// </summary>
    public Views.MainWindow? MainView { get; set; }

    /// <summary>
    /// 策略看板内容面板引用（用于截图）
    /// </summary>
    public FrameworkElement? StrategyContentPanel => MainView?.StrategyContentPanelElement;

    /// <summary>
    /// TabControl 当前选中的 Tab 索引
    /// 0 = 量化策略看板
    /// 1 = AI推演原文
    /// </summary>
    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    private string _rawReasoning = "";
    public string RawReasoning
    {
        get => _rawReasoning;
        set { _rawReasoning = value; OnPropertyChanged(); }
    }

    private string _apiKey = "";
    public string ApiKey
    {
        get
        {
            if (string.IsNullOrEmpty(_apiKey) && _currentModelConfig != null)
                return _currentModelConfig.ApiKey;
            return _apiKey;
        }
        set { _apiKey = value; OnPropertyChanged(); }
    }

    private string _selectedModel = "";
    public string SelectedModel
    {
        get
        {
            if (string.IsNullOrEmpty(_selectedModel) && _currentModelConfig != null)
                return _currentModelConfig.DefaultModel;
            return _selectedModel;
        }
        set { _selectedModel = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> AvailableModels { get; } = new()
    {
        "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo",
        "claude-3-opus", "claude-3-sonnet", "claude-3-haiku"
    };

    private KlineInterval _selectedInterval = KlineInterval.h1;
    public KlineInterval SelectedInterval
    {
        get => _selectedInterval;
        set { _selectedInterval = value; OnPropertyChanged(); }
    }

    public ObservableCollection<KlineInterval> AvailableIntervals { get; } = new()
    {
        KlineInterval.m15, KlineInterval.h1, KlineInterval.h4, KlineInterval.d1
    };

    private int _contractCount = 100;
    public int ContractCount
    {
        get => _contractCount;
        set { _contractCount = System.Math.Clamp(value, 10, 200); OnPropertyChanged(); }
    }

    #endregion

    #region 命令

    public ICommand RefreshCommand { get; }
    public ICommand GenerateStrategyCommand { get; }

    #endregion

    #region 构造

    public MainViewModel()
    {
        _binanceService = new BinanceFuturesDataService();
        _phaseAnalyzer = new MarketPhaseAnalyzer();
        _indicatorService = new IndicatorCalculationService();
        _llmService = new LLMService();
        _modelConfigService = new ModelConfigService();

        LoadDefaultModelConfig();

        RefreshCommand = new AsyncRelayCommand(RefreshDataAsync, () => IsNotLoading);
        GenerateStrategyCommand = new AsyncRelayCommand(GenerateStrategyAsync, () => IsNotLoading && !string.IsNullOrWhiteSpace(ApiKey));
    }

    private void LoadDefaultModelConfig()
    {
        _currentModelConfig = _modelConfigService.GetDefaultModel();
        if (_currentModelConfig != null)
        {
            _apiKey = _currentModelConfig.ApiKey;
            _selectedModel = _currentModelConfig.DefaultModel;
            OnPropertyChanged(nameof(ApiKey));
            OnPropertyChanged(nameof(SelectedModel));
        }
    }

    /// <summary>
    /// 刷新模型配置（从配置文件重新加载）
    /// </summary>
    public void RefreshModelConfig()
    {
        LoadDefaultModelConfig();
        OnPropertyChanged(nameof(ApiKey));
        OnPropertyChanged(nameof(SelectedModel));
    }

    #endregion

    #region 数据加载

    public async Task RefreshDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        _cancellationTokenSource = new CancellationTokenSource();
        AIStrategyOutput = "";

        try
        {
            LoadingStatus = "正在获取合约列表...";

            var symbols = await _binanceService.GetTopContractsAsync(ContractCount, _cancellationTokenSource.Token);
            if (symbols.Count == 0) { LoadingStatus = "获取合约列表失败"; return; }

            LoadingStatus = $"正在获取 {symbols.Count} 个合约的数据...";

            var klinesDict = await _binanceService.GetMultiSymbolKlinesAsync(
                symbols, SelectedInterval, Constants.DefaultKlineLimit,
                new Progress<string>(s => LoadingStatus = s),
                _cancellationTokenSource.Token);

            if (klinesDict.Count == 0) { LoadingStatus = "获取K线数据失败"; return; }

            LoadingStatus = "正在获取资金费率...";

            var fundingRates = await _binanceService.GetMultiSymbolFundingRatesAsync(
                klinesDict.Keys.ToList(), _cancellationTokenSource.Token);

            LoadingStatus = "正在计算技术指标...";

            var contractDataList = new List<ContractData>();
            foreach (var (symbol, klines) in klinesDict)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested) break;

                var indicators = _indicatorService.CalculateIndicators(symbol, klines,
                    fundingRates.TryGetValue(symbol, out var rate) ? rate : 0);

                contractDataList.Add(new ContractData
                {
                    Info = new ContractInfo { Symbol = symbol },
                    KLines = klines,
                    Indicators = indicators,
                    FetchTime = System.DateTime.Now
                });
            }

            ContractDataList = contractDataList;
            LoadingStatus = "正在分析市场阶段...";

            MarketOverview = _phaseAnalyzer.AnalyzeMarketPhase(contractDataList);
            LoadingStatus = "数据加载完成";
        }
        catch (System.OperationCanceledException) { LoadingStatus = "操作已取消"; }
        catch (System.Exception ex) { LoadingStatus = $"错误: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    #endregion

    #region AI策略生成

    public async Task GenerateStrategyAsync()
    {
        // 如果没有设置 API Key，尝试从配置文件获取
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            _currentModelConfig = _modelConfigService.GetDefaultModel();
            if (_currentModelConfig != null)
            {
                ApiKey = _currentModelConfig.ApiKey;
                SelectedModel = _currentModelConfig.DefaultModel;
            }
        }

        if (string.IsNullOrWhiteSpace(ApiKey)) { AIStrategyOutput = "请先配置 API Key（在设置中配置模型）"; return; }
        if (MarketOverview.CurrentPhase == MarketPhase.Unknown) { AIStrategyOutput = "请先加载市场数据"; return; }

        IsLoading = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            LoadingStatus = "正在生成策略...";
            AIStrategyOutput = "";

            // 重置策略结果
            StrategyResult = new AiStrategyResult();
            RawReasoning = "";

            var endpoint = _currentModelConfig?.ApiUrl ?? Constants.DefaultLLMEndpoint;
            var model = string.IsNullOrWhiteSpace(SelectedModel) ? "gpt-4o" : SelectedModel;

            System.Diagnostics.Debug.WriteLine($"[DEBUG] API Key 长度: {ApiKey?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Endpoint: {endpoint}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Model: {model}");

            var request = new LLMRequest
            {
                ApiKey = ApiKey,
                Endpoint = endpoint,
                Model = model,
                SystemPrompt = LLMService.GetSystemPrompt(),
                UserMessage = LLMService.BuildMarketAnalysisPrompt(MarketOverview, ContractDataList),
                MaxTokens = Constants.MaxStreamingTokens,
                Temperature = Constants.DefaultLLMTemperature
            };

            System.Diagnostics.Debug.WriteLine($"[DEBUG] 开始调用 LLM...");

            var result = await _llmService.StreamGenerateStrategyWithResultAsync(request, chunk =>
            {
                Application.Current?.Dispatcher.Invoke(() => { AIStrategyOutput += chunk; });
            }, _cancellationTokenSource.Token);

            System.Diagnostics.Debug.WriteLine($"[DEBUG] LLM 调用完成，IsParsed: {result.IsParsed}");

            // 在 UI 线程更新结果
            Application.Current?.Dispatcher.Invoke(() => {
                // 步骤C: 更新策略结果并通知 UI
                StrategyResult = result;
                RawReasoning = result.RawReasoning;

                if (result.IsParsed)
                {
                    // 解析成功，切换到 Tab 0（量化策略看板）
                    SelectedTabIndex = 0;
                    LoadingStatus = $"策略生成完成 (动作:{result.Action}, 风险:{result.RiskLevel}, 置信:{result.Confidence})";
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 策略解析成功 - Action:{result.Action}, RiskLevel:{result.RiskLevel}, Confidence:{result.Confidence}");

                    // 发送飞书通知（包含截图）
                    _ = Task.Run(async () =>
                    {
                        var imagePath = "";
                        try
                        {
                            // 等待 UI 渲染完成（图表需要时间渲染）
                            await Task.Delay(1000);

                            // 获取策略内容面板进行截图
                            var contentPanel = StrategyContentPanel;
                            if (contentPanel != null)
                            {
                                imagePath = PanoramaFuturesAI.Utils.ScreenCapture.GetTempImagePath();
                                PanoramaFuturesAI.Utils.ScreenCapture.CaptureToFile(contentPanel, imagePath, 2);
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] 策略看板截图已保存: {imagePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] 截图失败: {ex.Message}");
                        }

                        await Services.NotificationService.Instance.SendStrategyNotificationAsync(
                            result.ActionText,
                            result.RiskLevel,
                            result.StrategySummary,
                            imagePath);
                    });
                }
                else
                {
                    // 步骤D: 解析失败，切换到 Tab 1（AI推演原文）
                    RawReasoning = result.RawReasoning;
                    SelectedTabIndex = 1;
                    LoadingStatus = "策略生成完成（部分解析）";
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 策略解析失败，RawReasoning: {result.RawReasoning?.Substring(0, Math.Min(200, result.RawReasoning.Length))}");
                }
            });
        }
        catch (System.OperationCanceledException) { LoadingStatus = "策略生成已取消"; }
        catch (System.Exception ex) { AIStrategyOutput += $"\n\n[错误: {ex.Message}]"; LoadingStatus = "策略生成失败"; }
        finally { IsLoading = false; }
    }

    #endregion

    #region 资源释放

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _binanceService.Dispose();
                _llmService.Dispose();
            }
            _disposed = true;
        }
    }

    #endregion
}
