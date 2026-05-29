using System;
using System.Windows;
using System.Windows.Controls;
using PanoramaFuturesAI.ViewModels;

namespace PanoramaFuturesAI.Views;

/// <summary>
/// 模型配置窗口
/// </summary>
public partial class ModelConfigWindow : Window
{
    private readonly ModelConfigViewModel _viewModel;

    public ModelConfigWindow()
    {
        InitializeComponent();
        _viewModel = new ModelConfigViewModel();
        DataContext = _viewModel;

        Loaded += ModelConfigWindow_Loaded;
    }

    private void ModelConfigWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_viewModel.CurrentModel.ApiKey))
        {
            ApiKeyBox.Password = _viewModel.CurrentModel.ApiKey;
        }
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.CurrentModel.ApiKey = passwordBox.Password;
        }
    }
}
