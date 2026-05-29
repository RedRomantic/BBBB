using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PanoramaFuturesAI.Services;

namespace PanoramaFuturesAI.Views;

/// <summary>
/// 通知推送设置窗口
/// </summary>
public partial class NotificationSettingsWindow : Window
{
    private readonly NotificationViewModel _viewModel;

    public NotificationSettingsWindow()
    {
        InitializeComponent();

        _viewModel = new NotificationViewModel();
        DataContext = _viewModel;

        // 初始化下拉框
        InitializeComboBoxes();

        // 加载配置
        LoadConfig();

        UpdateStatusDisplay();
    }

    private void InitializeComboBoxes()
    {
        // 小时下拉框 (0-23)
        var hours = new List<string>();
        for (int i = 0; i < 24; i++)
        {
            hours.Add(i.ToString("D2"));
        }
        HourComboBox.ItemsSource = hours;
        HourComboBox.SelectedIndex = 9; // 默认 09:00

        // 周期下拉框
        IntervalComboBox.ItemsSource = new List<string>
        {
            "每小时",
            "每4小时",
            "每6小时",
            "每12小时",
            "每天"
        };
        IntervalComboBox.SelectedIndex = 4; // 默认每天
    }

    private void LoadConfig()
    {
        var config = NotificationService.Instance.Config;
        _viewModel.WebhookUrl = config.FeishuWebhookUrl;
        _viewModel.Enabled = config.FeishuWebhookEnabled;
        _viewModel.IsScheduledMode = config.PushMode == "Scheduled";
        _viewModel.IsManualMode = !_viewModel.IsScheduledMode;

        // 加载图片推送配置
        _viewModel.ImageApiKey = config.ImageApiKey;
        _viewModel.ImageSecretKey = config.ImageSecretKey;
        _viewModel.ImageReceiverId = config.ImageReceiverId;
        _viewModel.ImageReceiverIdType = config.ImageReceiverIdType;
        _viewModel.ImagePushEnabled = config.ImagePushEnabled;

        // 同步 UI 绑定
        ImageApiKeyBox.Text = _viewModel.ImageApiKey;
        ImageSecretKeyBox.Text = _viewModel.ImageSecretKey;
        ImageReceiverIdBox.Text = _viewModel.ImageReceiverId;
        ImagePushEnabledCheckBox.IsChecked = _viewModel.ImagePushEnabled;

        // 解析定时设置
        if (!string.IsNullOrEmpty(config.ScheduledTime))
        {
            var parts = config.ScheduledTime.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[0], out int hour))
            {
                HourComboBox.SelectedIndex = hour;
            }
        }

        if (!string.IsNullOrEmpty(config.PushInterval))
        {
            var intervalMap = new Dictionary<string, int>
            {
                { "Hourly", 0 },
                { "Every4Hours", 1 },
                { "Every6Hours", 2 },
                { "Every12Hours", 3 },
                { "Daily", 4 }
            };
            if (intervalMap.TryGetValue(config.PushInterval, out int index))
            {
                IntervalComboBox.SelectedIndex = index;
            }
        }

        UpdateStatusDisplay();
    }

    private void UpdateStatusDisplay()
    {
        var config = NotificationService.Instance.Config;

        if (config.FeishuWebhookEnabled)
        {
            StatusText.Text = config.PushMode == "Scheduled"
                ? $"定时推送模式 - 每 {GetIntervalText(config.PushInterval)} 推送"
                : "手动推送模式 - 生成策略后自动推送";
        }
        else
        {
            StatusText.Text = "通知服务已停止";
        }

        if (config.LastPushTime.HasValue)
        {
            LastPushText.Text = $"上次推送: {config.LastPushTime.Value:yyyy-MM-dd HH:mm:ss}";
        }
        else
        {
            LastPushText.Text = "上次推送: 未推送";
        }

        // 计算下次推送时间
        if (config.FeishuWebhookEnabled && config.PushMode == "Scheduled")
        {
            var nextPush = CalculateNextPushTime(config);
            NextPushText.Text = $"下次推送: {nextPush:yyyy-MM-dd HH:mm}";
        }
        else
        {
            NextPushText.Text = "";
        }
    }

    private DateTime CalculateNextPushTime(NotificationConfig config)
    {
        var now = DateTime.Now;
        var scheduledHour = 9;

        if (!string.IsNullOrEmpty(config.ScheduledTime))
        {
            var parts = config.ScheduledTime.Split(':');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int hour))
            {
                scheduledHour = hour;
            }
        }

        var baseTime = new DateTime(now.Year, now.Month, now.Day, scheduledHour, 0, 0);

        switch (config.PushInterval)
        {
            case "Hourly":
                return now.AddHours(1);
            case "Every4Hours":
                return baseTime.AddHours(((now.Hour / 4) + 1) * 4);
            case "Every6Hours":
                return baseTime.AddHours(((now.Hour / 6) + 1) * 6);
            case "Every12Hours":
                return baseTime.AddHours(now.Hour < 12 ? 12 : 24);
            default:
                // Daily
                if (now < baseTime)
                    return baseTime;
                return baseTime.AddDays(1);
        }
    }

    private string GetIntervalText(string interval)
    {
        return interval switch
        {
            "Hourly" => "每小时",
            "Every4Hours" => "每4小时",
            "Every6Hours" => "每6小时",
            "Every12Hours" => "每12小时",
            _ => "每天"
        };
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.WebhookUrl))
        {
            TestResultText.Text = "请先输入 Webhook URL";
            TestResultText.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }

        TestButton.IsEnabled = false;
        TestResultText.Text = "正在测试...";
        TestResultText.Foreground = System.Windows.Media.Brushes.Gray;

        try
        {
            var (success, message) = await NotificationService.Instance.TestFeishuWebhookAsync(_viewModel.WebhookUrl);
            TestResultText.Text = message;
            TestResultText.Foreground = success
                ? System.Windows.Media.Brushes.LimeGreen
                : System.Windows.Media.Brushes.OrangeRed;
        }
        catch (Exception ex)
        {
            TestResultText.Text = $"测试异常: {ex.Message}";
            TestResultText.Foreground = System.Windows.Media.Brushes.OrangeRed;
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private void TestScreenshot_Click(object sender, RoutedEventArgs e)
    {
        TestScreenshotButton.IsEnabled = false;
        ScreenshotPathText.Text = "正在截图...";

        try
        {
            // 获取主窗口
            var mainWindow = App.MainWindowInstance;

            // 获取策略内容面板
            FrameworkElement? elementToCapture = null;
            if (mainWindow?.StrategyContentPanelElement != null)
            {
                elementToCapture = mainWindow.StrategyContentPanelElement;
            }

            if (elementToCapture == null)
            {
                ScreenshotPathText.Text = "错误：无法获取策略看板控件，请先生成一次策略";
                ScreenshotPathText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            // 生成截图（使用实际尺寸）
            var imagePath = PanoramaFuturesAI.Utils.ScreenCapture.GetTempImagePath();
            PanoramaFuturesAI.Utils.ScreenCapture.CaptureToFile(elementToCapture, imagePath, 2);

            ScreenshotPathText.Text = $"截图已保存:\n{imagePath}";
            ScreenshotPathText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(63, 185, 80));
        }
        catch (Exception ex)
        {
            ScreenshotPathText.Text = $"截图失败: {ex.Message}";
            ScreenshotPathText.Foreground = System.Windows.Media.Brushes.OrangeRed;
        }
        finally
        {
            TestScreenshotButton.IsEnabled = true;
        }
    }

    private async void TestImageMessage_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.ImageApiKey) ||
            string.IsNullOrWhiteSpace(_viewModel.ImageSecretKey) ||
            string.IsNullOrWhiteSpace(_viewModel.ImageReceiverId))
        {
            TestResultText.Text = "请先填写 App ID、App Secret 和接收者 ID";
            TestResultText.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }

        TestImageButton.IsEnabled = false;
        TestResultText.Text = "正在测试...";
        TestResultText.Foreground = System.Windows.Media.Brushes.Gray;

        try
        {
            // 保存配置
            NotificationService.Instance.Config.ImageApiKey = _viewModel.ImageApiKey;
            NotificationService.Instance.Config.ImageSecretKey = _viewModel.ImageSecretKey;
            NotificationService.Instance.Config.ImageReceiverId = _viewModel.ImageReceiverId;

            var (success, message) = await NotificationService.Instance.TestImageMessageAsync();

            TestResultText.Text = message;
            TestResultText.Foreground = success
                ? System.Windows.Media.Brushes.LimeGreen
                : System.Windows.Media.Brushes.OrangeRed;
        }
        catch (Exception ex)
        {
            TestResultText.Text = $"测试异常: {ex.Message}";
            TestResultText.Foreground = System.Windows.Media.Brushes.OrangeRed;
        }
        finally
        {
            TestImageButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // 获取选择的值
        var selectedHour = HourComboBox.SelectedItem?.ToString() ?? "09";
        var selectedIntervalIndex = IntervalComboBox.SelectedIndex;

        var intervalMap = new Dictionary<int, string>
        {
            { 0, "Hourly" },
            { 1, "Every4Hours" },
            { 2, "Every6Hours" },
            { 3, "Every12Hours" },
            { 4, "Daily" }
        };

        var pushInterval = intervalMap.TryGetValue(selectedIntervalIndex, out var interval) ? interval : "Daily";

        // 保存配置
        var config = NotificationService.Instance.Config;
        config.FeishuWebhookUrl = _viewModel.WebhookUrl;
        config.FeishuWebhookEnabled = _viewModel.Enabled;
        config.PushMode = _viewModel.IsScheduledMode ? "Scheduled" : "Manual";
        config.ScheduledTime = $"{selectedHour}:00";
        config.PushInterval = pushInterval;

        // 保存图片推送配置
        config.ImageApiKey = ImageApiKeyBox.Text;
        config.ImageSecretKey = ImageSecretKeyBox.Text;
        config.ImageReceiverId = ImageReceiverIdBox.Text;
        config.ImageReceiverIdType = ImageReceiverIdTypeCombo.SelectedValue?.ToString() ?? "open_id";
        config.ImagePushEnabled = ImagePushEnabledCheckBox.IsChecked ?? false;

        NotificationService.Instance.SaveConfig();

        // 启动或停止定时器
        if (config.FeishuWebhookEnabled && config.PushMode == "Scheduled")
        {
            NotificationService.Instance.StartScheduledPush();
        }
        else
        {
            NotificationService.Instance.StopScheduledPush();
        }

        DialogResult = true;
        Close();
    }
}

/// <summary>
/// 通知设置视图模型
/// </summary>
public class NotificationViewModel
{
    public string WebhookUrl { get; set; } = "";
    public bool Enabled { get; set; }
    public bool IsManualMode { get; set; } = true;
    public bool IsScheduledMode { get; set; }
    public string SelectedHour { get; set; } = "09";
    public string SelectedInterval { get; set; } = "每天";

    // 图片推送配置
    public string ImageApiKey { get; set; } = "";
    public string ImageSecretKey { get; set; } = "";
    public string ImageReceiverId { get; set; } = "";
    public string ImageReceiverIdType { get; set; } = "open_id";
    public bool ImagePushEnabled { get; set; }
}
