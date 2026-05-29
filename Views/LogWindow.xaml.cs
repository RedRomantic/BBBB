using System;
using System.Windows;
using PanoramaFuturesAI.Services;

namespace PanoramaFuturesAI.Views;

/// <summary>
/// 日志窗口的交互逻辑
/// </summary>
public partial class LogWindow : Window
{
    private readonly LogService _logService;

    public LogWindow()
    {
        InitializeComponent();
        _logService = LogService.Instance;

        LogList.ItemsSource = _logService.Logs;
        UpdateErrorCount();

        _logService.LogAdded += (s, e) =>
        {
            Dispatcher.Invoke(UpdateErrorCount);
        };
    }

    private void UpdateErrorCount()
    {
        var count = _logService.Logs.Count;
        ErrorCountText.Text = count.ToString();
        StatusText.Text = count > 0 ? $"共 {count} 条日志" : "暂无日志";
    }

    private void CopyAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = _logService.GetAllLogsAsText();
            if (string.IsNullOrEmpty(text))
            {
                StatusText.Text = "没有日志可复制";
                return;
            }

            Clipboard.SetText(text);
            StatusText.Text = "已复制到剪贴板";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"复制失败: {ex.Message}";
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _logService.Clear();
        StatusText.Text = "日志已清空";
    }
}
