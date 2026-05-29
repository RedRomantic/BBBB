using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PanoramaFuturesAI.Services;

/// <summary>
/// 日志服务 - 记录和管理 AI 输出及错误日志
/// </summary>
public class LogService : INotifyPropertyChanged
{
    private static readonly Lazy<LogService> _instance = new(() => new LogService());
    public static LogService Instance => _instance.Value;

    private readonly string _logFilePath;
    private readonly object _lock = new();

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<LogEntry>? LogAdded;

    private LogService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PanoramaFuturesAI");
        Directory.CreateDirectory(appDataPath);
        _logFilePath = Path.Combine(appDataPath, "ai_error_logs.txt");
    }

    /// <summary>
    /// 添加错误日志
    /// </summary>
    public void AddError(string source, string errorMessage, string? details = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Error,
            Source = source,
            Message = errorMessage,
            Details = details
        };

        AddEntry(entry);
    }

    /// <summary>
    /// 添加信息日志
    /// </summary>
    public void AddInfo(string source, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Info,
            Source = source,
            Message = message
        };

        AddEntry(entry);
    }

    /// <summary>
    /// 添加 API 错误日志（专门处理 AI API 错误）
    /// </summary>
    public void AddApiError(string provider, string errorMessage, string? responseBody = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Error,
            Source = $"{provider} API",
            Message = errorMessage,
            Details = responseBody
        };

        AddEntry(entry);
    }

    private void AddEntry(LogEntry entry)
    {
        lock (_lock)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, entry);
                // 保持最多 500 条日志
                while (Logs.Count > 500)
                {
                    Logs.RemoveAt(Logs.Count - 1);
                }
            });

            // 写入文件
            try
            {
                var logLine = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] [{entry.Source}]\n  {entry.Message}";
                if (!string.IsNullOrEmpty(entry.Details))
                {
                    logLine += $"\n  详情: {entry.Details}";
                }
                logLine += "\n---\n";
                File.AppendAllText(_logFilePath, logLine);
            }
            catch { }

            LogAdded?.Invoke(this, entry);
            OnPropertyChanged(nameof(Logs));
            OnPropertyChanged(nameof(ErrorCount));
        }
    }

    /// <summary>
    /// 清空当前日志
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => Logs.Clear());
            OnPropertyChanged(nameof(Logs));
            OnPropertyChanged(nameof(ErrorCount));
        }
    }

    /// <summary>
    /// 获取所有日志的文本格式（用于复制）
    /// </summary>
    public string GetAllLogsAsText()
    {
        lock (_lock)
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var entry in Logs)
            {
                lines.Add($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] [{entry.Source}]");
                lines.Add($"  {entry.Message}");
                if (!string.IsNullOrEmpty(entry.Details))
                {
                    lines.Add($"  详情: {entry.Details}");
                }
                lines.Add("");
            }
            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// 获取错误日志的数量
    /// </summary>
    public int ErrorCount
    {
        get
        {
            lock (_lock)
            {
                return Logs.Count;
            }
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Source { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Details { get; set; }

    public string LevelDisplay => Level switch
    {
        LogLevel.Error => "错误",
        LogLevel.Warning => "警告",
        LogLevel.Info => "信息",
        _ => "未知"
    };

    public string LevelColor => Level switch
    {
        LogLevel.Error => "#F85149",
        LogLevel.Warning => "#D29922",
        LogLevel.Info => "#58A6FF",
        _ => "#8B949E"
    };
}

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error
}
