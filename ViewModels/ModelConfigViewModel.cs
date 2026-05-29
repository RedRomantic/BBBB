using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using PanoramaFuturesAI.Models;
using PanoramaFuturesAI.Commands;
using PanoramaFuturesAI.Services;

namespace PanoramaFuturesAI.ViewModels;

/// <summary>
/// 模型配置窗口视图模型
/// </summary>
public class ModelConfigViewModel : INotifyPropertyChanged
{
    private readonly ModelConfigService _configService;

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

    #region 属性

    private ObservableCollection<ModelListItem> _modelList = new();
    public ObservableCollection<ModelListItem> ModelList
    {
        get => _modelList;
        set => SetProperty(ref _modelList, value);
    }

    private ModelListItem? _selectedModelItem;
    public ModelListItem? SelectedModelItem
    {
        get => _selectedModelItem;
        set
        {
            if (SetProperty(ref _selectedModelItem, value) && value != null)
            {
                LoadModelToEditor(value.Model);
                IsEditing = true;
                IsNewModel = false;
            }
        }
    }

    private AIModelConfig _currentModel = new();
    public AIModelConfig CurrentModel
    {
        get => _currentModel;
        set => SetProperty(ref _currentModel, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    private bool _isNewModel;
    public bool IsNewModel
    {
        get => _isNewModel;
        set => SetProperty(ref _isNewModel, value);
    }

    private bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    private string _testResult = "";
    public string TestResult
    {
        get => _testResult;
        set => SetProperty(ref _testResult, value);
    }

    public ObservableCollection<string> AvailableProviders { get; } = new()
    {
        "OpenAI", "Google", "Anthropic", "DeepSeek", "Qwen", "Volcano", "Other"
    };

    public ObservableCollection<string> AvailablePurposes { get; } = new()
    {
        "Summary", "DeepResearch"
    };

    private string _selectedProvider = "OpenAI";
    public string SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetProperty(ref _selectedProvider, value))
            {
                if (IsNewModel || string.IsNullOrEmpty(CurrentModel.ApiUrl) || IsDefaultProviderUrl())
                {
                    CurrentModel.ApiUrl = AIModelConfig.GetDefaultApiUrl(value);
                }
                if (IsNewModel || string.IsNullOrEmpty(CurrentModel.DefaultModel) || IsDefaultProviderModel())
                {
                    CurrentModel.DefaultModel = AIModelConfig.GetDefaultModel(value);
                }
            }
        }
    }

    private string _selectedPurpose = "Summary";
    public string SelectedPurpose
    {
        get => _selectedPurpose;
        set => SetProperty(ref _selectedPurpose, value);
    }

    private bool _isDefault;
    public bool IsDefault
    {
        get => _isDefault;
        set => SetProperty(ref _isDefault, value);
    }

    #endregion

    #region 命令

    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    #endregion

    #region 构造

    public ModelConfigViewModel()
    {
        _configService = new ModelConfigService();

        AddCommand = new RelayCommand(() => AddNewModel());
        DeleteCommand = new RelayCommand(() => DeleteCurrentModel(), () => SelectedModelItem != null && ModelList.Count > 1);
        TestConnectionCommand = new AsyncRelayCommand(async () => await TestConnectionAsync());
        SaveCommand = new AsyncRelayCommand(async () => await SaveModelAsync());
        CancelCommand = new RelayCommand(() => CancelEdit());

        LoadModels();
    }

    private bool CanTestConnection()
    {
        return !IsTesting && !string.IsNullOrWhiteSpace(CurrentModel.ApiUrl) && !string.IsNullOrWhiteSpace(CurrentModel.ApiKey);
    }

    private bool CanSaveModel()
    {
        return IsEditing && !string.IsNullOrWhiteSpace(CurrentModel.Name) && !string.IsNullOrWhiteSpace(CurrentModel.DisplayName);
    }

    #endregion

    #region 方法

    private void LoadModels()
    {
        var models = _configService.GetAllModels();
        ModelList.Clear();
        foreach (var model in models)
        {
            ModelList.Add(new ModelListItem { Model = model });
        }

        if (ModelList.Any())
        {
            SelectedModelItem = ModelList.First();
        }
    }

    private void LoadModelToEditor(AIModelConfig model)
    {
        CurrentModel = new AIModelConfig
        {
            Id = model.Id,
            Name = model.Name,
            DisplayName = model.DisplayName,
            ApiUrl = model.ApiUrl,
            ApiKey = model.ApiKey,
            DefaultModel = model.DefaultModel,
            Provider = model.Provider,
            Purpose = model.Purpose,
            IsDefault = model.IsDefault,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
        _selectedProvider = model.Provider;
        _selectedPurpose = model.Purpose;
        _isDefault = model.IsDefault;
        OnPropertyChanged(nameof(SelectedProvider));
        OnPropertyChanged(nameof(SelectedPurpose));
        OnPropertyChanged(nameof(IsDefault));
        TestResult = "";
    }

    private bool IsDefaultProviderUrl()
    {
        var defaultUrl = AIModelConfig.GetDefaultApiUrl(_selectedProvider);
        return CurrentModel.ApiUrl == defaultUrl || string.IsNullOrEmpty(CurrentModel.ApiUrl);
    }

    private bool IsDefaultProviderModel()
    {
        var defaultModel = AIModelConfig.GetDefaultModel(_selectedProvider);
        return CurrentModel.DefaultModel == defaultModel || string.IsNullOrEmpty(CurrentModel.DefaultModel);
    }

    private void AddNewModel()
    {
        var newModel = new AIModelConfig
        {
            Name = "new_model",
            DisplayName = "新模型",
            ApiUrl = AIModelConfig.GetDefaultApiUrl("OpenAI"),
            DefaultModel = AIModelConfig.GetDefaultModel("OpenAI"),
            Provider = "OpenAI",
            Purpose = "Summary",
            IsDefault = false
        };

        CurrentModel = newModel;
        _selectedProvider = "OpenAI";
        _selectedPurpose = "Summary";
        _isDefault = false;
        _isEditing = true;
        _isNewModel = true;
        
        OnPropertyChanged(nameof(CurrentModel));
        OnPropertyChanged(nameof(SelectedProvider));
        OnPropertyChanged(nameof(SelectedPurpose));
        OnPropertyChanged(nameof(IsDefault));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(IsNewModel));
        TestResult = "";
    }

    private void DeleteCurrentModel()
    {
        if (SelectedModelItem == null) return;

        _configService.DeleteModel(SelectedModelItem.Model.Id);
        LoadModels();
        
        if (ModelList.Any())
        {
            SelectedModelItem = ModelList.First();
        }
    }

    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestResult = "正在测试连接...";

        try
        {
            var (success, message) = await _configService.TestModelAsync(CurrentModel);
            TestResult = success ? $"✓ 测试成功！{message}" : $"✗ 测试失败: {message}";
        }
        catch (Exception ex)
        {
            TestResult = $"✗ 测试异常: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task SaveModelAsync()
    {
        CurrentModel.Provider = SelectedProvider;
        CurrentModel.Purpose = SelectedPurpose;
        CurrentModel.IsDefault = IsDefault;

        if (IsNewModel)
        {
            var added = _configService.AddModel(CurrentModel);
            if (IsDefault)
            {
                _configService.SetDefaultModel(added.Id, SelectedPurpose);
            }
        }
        else
        {
            _configService.UpdateModel(CurrentModel);
            if (IsDefault)
            {
                _configService.SetDefaultModel(CurrentModel.Id, CurrentModel.Purpose);
            }
        }

        LoadModels();
        IsNewModel = false;
        IsEditing = false;

        await Task.CompletedTask;
    }

    private void CancelEdit()
    {
        if (SelectedModelItem != null)
        {
            LoadModelToEditor(SelectedModelItem.Model);
        }
        IsEditing = false;
        IsNewModel = false;
        TestResult = "";
    }

    #endregion
}

/// <summary>
/// 模型列表项（用于显示星号标记）
/// </summary>
public class ModelListItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private AIModelConfig _model = new();
    public AIModelConfig Model
    {
        get => _model;
        set
        {
            _model = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Model)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDefault)));
        }
    }

    public string DisplayText => Model.IsDefault ? $"★ {Model.DisplayName}" : Model.DisplayName;
    public bool HasDefault => Model.IsDefault;
}
