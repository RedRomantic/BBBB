using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using PanoramaFuturesAI.Models;
using PanoramaFuturesAI.Services;

namespace PanoramaFuturesAI.ViewModels;

/// <summary>
/// 设置页面视图模型
/// </summary>
public class SettingsViewModel : INotifyPropertyChanged
{
    #region 事件

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region 构造函数

    public SettingsViewModel()
    {
        // 从通知服务加载配置
        var config = NotificationService.Instance.Config;
        _feishuWebhookUrl = config.FeishuWebhookUrl;
        _feishuWebhookEnabled = config.FeishuWebhookEnabled;

        TestFeishuCommand = new RelayCommand(async () => await TestFeishuWebhookAsync());
    }

    #endregion

    #region 模型显示

    private string _currentModelName = "未配置";
    public string CurrentModelName
    {
        get => _currentModelName;
        set => SetProperty(ref _currentModelName, value);
    }

    private bool _hasDefaultModel;
    public bool HasDefaultModel
    {
        get => _hasDefaultModel;
        set => SetProperty(ref _hasDefaultModel, value);
    }

    #endregion

    #region 参数设置

    private KlineInterval _selectedInterval = KlineInterval.h1;
    public KlineInterval SelectedInterval
    {
        get => _selectedInterval;
        set => SetProperty(ref _selectedInterval, value);
    }

    public ObservableCollection<KlineInterval> AvailableIntervals { get; } = new()
    {
        KlineInterval.m15, KlineInterval.h1, KlineInterval.h4, KlineInterval.d1
    };

    private int _contractCount = 100;
    public int ContractCount
    {
        get => _contractCount;
        set => SetProperty(ref _contractCount, Math.Clamp(value, 10, 200));
    }

    #endregion

    #region 飞书通知设置

    private string _feishuWebhookUrl = "";
    public string FeishuWebhookUrl
    {
        get => _feishuWebhookUrl;
        set => SetProperty(ref _feishuWebhookUrl, value);
    }

    private bool _feishuWebhookEnabled;
    public bool FeishuWebhookEnabled
    {
        get => _feishuWebhookEnabled;
        set => SetProperty(ref _feishuWebhookEnabled, value);
    }

    private bool _isTestingFeishu;
    public bool IsTestingFeishu
    {
        get => _isTestingFeishu;
        set => SetProperty(ref _isTestingFeishu, value);
    }

    private string _feishuTestResult = "";
    public string FeishuTestResult
    {
        get => _feishuTestResult;
        set => SetProperty(ref _feishuTestResult, value);
    }

    public ICommand TestFeishuCommand { get; }

    private async Task TestFeishuWebhookAsync()
    {
        if (string.IsNullOrWhiteSpace(FeishuWebhookUrl))
        {
            FeishuTestResult = "请先输入 Webhook URL";
            return;
        }

        IsTestingFeishu = true;
        FeishuTestResult = "正在测试...";

        try
        {
            var (success, message) = await NotificationService.Instance.TestFeishuWebhookAsync(FeishuWebhookUrl);
            FeishuTestResult = message;
        }
        catch (Exception ex)
        {
            FeishuTestResult = $"测试异常: {ex.Message}";
        }
        finally
        {
            IsTestingFeishu = false;
        }
    }

    /// <summary>
    /// 保存飞书通知设置
    /// </summary>
    public void SaveFeishuSettings()
    {
        NotificationService.Instance.UpdateFeishuWebhook(FeishuWebhookUrl, FeishuWebhookEnabled);
    }

    #endregion
}

/// <summary>
/// 简单的 RelayCommand 实现
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action? _execute;
    private readonly Func<Task>? _executeAsync;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (_executeAsync != null)
        {
            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try
            {
                await _executeAsync();
            }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            _execute?.Invoke();
        }
    }
}
