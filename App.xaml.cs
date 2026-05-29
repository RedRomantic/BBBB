using System;
using System.Windows;

namespace PanoramaFuturesAI;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex, "AppDomain.UnhandledException");
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "Dispatcher.UnhandledException");
        e.Handled = true;
        MessageBox.Show($"发生错误: {e.Exception.Message}\n\n程序将继续运行。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void LogException(Exception ex, string source)
    {
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PanoramaFuturesAI_Error.log");
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n来源: {source}\n异常: {ex.GetType().Name}\n消息: {ex.Message}\n堆栈: {ex.StackTrace}\n----------------------------------------\n";
            System.IO.File.AppendAllText(logPath, logMessage);
        }
        catch { }
    }
}
