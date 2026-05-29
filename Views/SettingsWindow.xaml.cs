using System;
using System.Windows;
using System.Windows.Controls;
using PanoramaFuturesAI.ViewModels;
using PanoramaFuturesAI.Services;

namespace PanoramaFuturesAI.Views;

/// <summary>
/// 设置窗口
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly MainViewModel _mainViewModel;
    private readonly Action<SettingsViewModel> _onSave;
    private readonly ModelConfigService _modelConfigService;

    public SettingsWindow(MainViewModel mainViewModel, Action<SettingsViewModel> onSave)
    {
        InitializeComponent();

        _mainViewModel = mainViewModel;
        _onSave = onSave;
        _modelConfigService = new ModelConfigService();

        var viewModel = new SettingsViewModel
        {
            SelectedInterval = _mainViewModel.SelectedInterval,
            ContractCount = _mainViewModel.ContractCount
        };

        DataContext = viewModel;

        UpdateCurrentModelDisplay();
    }

    private void UpdateCurrentModelDisplay()
    {
        var defaultModel = _modelConfigService.GetDefaultModel();
        if (DataContext is SettingsViewModel vm && defaultModel != null)
        {
            vm.CurrentModelName = defaultModel.DisplayName;
            vm.HasDefaultModel = true;
        }
    }

    private void OpenModelConfig_Click(object sender, RoutedEventArgs e)
    {
        var configWindow = new ModelConfigWindow();
        configWindow.Owner = this;
        configWindow.ShowDialog();

        UpdateCurrentModelDisplay();

        // 刷新主窗口的模型配置
        _mainViewModel.RefreshModelConfig();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            _mainViewModel.SelectedInterval = viewModel.SelectedInterval;
            _mainViewModel.ContractCount = viewModel.ContractCount;
            _onSave?.Invoke(viewModel);
        }

        DialogResult = true;
        Close();
    }
}
