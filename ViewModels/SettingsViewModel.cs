using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PanoramaFuturesAI.Models;

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
}
