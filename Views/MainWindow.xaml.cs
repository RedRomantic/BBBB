using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using PanoramaFuturesAI.ViewModels;
using PanoramaFuturesAI.Models;
using PanoramaFuturesAI.Views;

namespace PanoramaFuturesAI.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshDataAsync();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Dispose();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_viewModel, vm =>
        {
            _viewModel.SelectedInterval = vm.SelectedInterval;
            _viewModel.ContractCount = vm.ContractCount;
        });
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();

        // 刷新模型配置
        _viewModel.RefreshModelConfig();
    }

    private void OpenNotificationSettings_Click(object sender, RoutedEventArgs e)
    {
        var notificationWindow = new NotificationSettingsWindow();
        notificationWindow.Owner = this;
        notificationWindow.ShowDialog();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshDataAsync();
    }

    private void OpenLogWindow_Click(object sender, RoutedEventArgs e)
    {
        var logWindow = new LogWindow();
        logWindow.Owner = this;
        logWindow.ShowDialog();
    }
}
