using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using PanoramaFuturesAI.ViewModels;

namespace PanoramaFuturesAI.Models;

/// <summary>
/// 交易动作枚举
/// </summary>
[JsonConverter(typeof(TradeActionConverter))]
public enum TradeAction
{
    /// <summary>观望</summary>
    Hold = 0,
    /// <summary>做多</summary>
    Long = 1,
    /// <summary>做空</summary>
    Short = 2
}

/// <summary>
/// TradeAction 枚举的 JSON 转换器，支持中英文值
/// </summary>
public class TradeActionConverter : JsonConverter<TradeAction>
{
    public override TradeAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()?.Trim().ToLowerInvariant();

        return value switch
        {
            "long" or "做多" or "做多(看涨)" or "做多（看涨）" => TradeAction.Long,
            "short" or "做空" or "做空(看跌)" or "做空（看跌）" => TradeAction.Short,
            "hold" or "观望" or "等待" or "持仓" or "无操作" => TradeAction.Hold,
            _ => TradeAction.Hold
        };
    }

    public override void Write(Utf8JsonWriter writer, TradeAction value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            TradeAction.Long => "Long",
            TradeAction.Short => "Short",
            _ => "Hold"
        };
        writer.WriteStringValue(str);
    }
}

/// <summary>
/// AI 策略结果 - 用于反序列化 LLM 返回的结构化数据
/// 实现 INotifyPropertyChanged 以支持 UI 绑定更新
/// </summary>
public class AiStrategyResult : ViewModelBase
{
    private TradeAction _action = TradeAction.Hold;
    private int _riskLevel = 50;
    private double _suggestedPosition = 0;
    private int _confidence = 0;
    private string _strategySummary = "";
    private string _rawReasoning = "";
    private bool _isParsed = false;

    /// <summary>
    /// 交易动作：Long(做多), Short(做空), Hold(观望)
    /// </summary>
    [JsonPropertyName("action")]
    public TradeAction Action
    {
        get => _action;
        set { _action = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActionText)); OnPropertyChanged(nameof(ActionColor)); OnPropertyChanged(nameof(ActionIcon)); }
    }

    /// <summary>
    /// 风险等级：0-100
    /// </summary>
    [JsonPropertyName("risk_level")]
    public int RiskLevel
    {
        get => _riskLevel;
        set { _riskLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(RiskDescription)); OnPropertyChanged(nameof(RiskColor)); }
    }

    /// <summary>
    /// 建议仓位：0.0-100.0 (百分比)
    /// </summary>
    [JsonPropertyName("suggested_position")]
    public double SuggestedPosition
    {
        get => _suggestedPosition;
        set { _suggestedPosition = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 置信度：0-100
    /// </summary>
    [JsonPropertyName("confidence")]
    public int Confidence
    {
        get => _confidence;
        set { _confidence = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConfidenceDescription)); }
    }

    /// <summary>
    /// 策略总结：一句话核心策略
    /// </summary>
    [JsonPropertyName("strategy_summary")]
    public string StrategySummary
    {
        get => _strategySummary;
        set { _strategySummary = value ?? ""; OnPropertyChanged(); }
    }

    /// <summary>
    /// 原始推理：AI 完整的逻辑分析原文
    /// </summary>
    [JsonPropertyName("raw_reasoning")]
    public string RawReasoning
    {
        get => _rawReasoning;
        set { _rawReasoning = value ?? ""; OnPropertyChanged(); }
    }

    /// <summary>
    /// 是否解析成功
    /// </summary>
    [JsonIgnore]
    public bool IsParsed
    {
        get => _isParsed;
        set { _isParsed = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 获取动作的中文描述
    /// </summary>
    [JsonIgnore]
    public string ActionText => _action switch
    {
        TradeAction.Long => "做多",
        TradeAction.Short => "做空",
        _ => "观望"
    };

    /// <summary>
    /// 获取动作的颜色代码
    /// </summary>
    [JsonIgnore]
    public string ActionColor => _action switch
    {
        TradeAction.Long => "#3FB950",
        TradeAction.Short => "#F85149",
        _ => "#8B949E"
    };

    /// <summary>
    /// 获取动作图标
    /// </summary>
    [JsonIgnore]
    public string ActionIcon => _action switch
    {
        TradeAction.Long => "📈",
        TradeAction.Short => "📉",
        _ => "⏸️"
    };

    /// <summary>
    /// 获取风险等级描述
    /// </summary>
    [JsonIgnore]
    public string RiskDescription => _riskLevel switch
    {
        < 30 => "低风险",
        < 60 => "中等风险",
        < 80 => "较高风险",
        _ => "高风险"
    };

    /// <summary>
    /// 获取风险等级颜色
    /// </summary>
    [JsonIgnore]
    public string RiskColor => _riskLevel switch
    {
        < 30 => "#3FB950",
        < 60 => "#F0883E",
        < 80 => "#F85149",
        _ => "#DA3633"
    };

    /// <summary>
    /// 获取置信度描述
    /// </summary>
    [JsonIgnore]
    public string ConfidenceDescription => _confidence switch
    {
        < 30 => "低置信",
        < 60 => "中等置信",
        < 80 => "较高置信",
        _ => "高置信"
    };

    /// <summary>
    /// 风险等级仪表盘系列
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<ISeries> RiskGaugeSeries { get; private set; } = new();

    /// <summary>
    /// 仓位仪表盘系列
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<ISeries> PositionGaugeSeries { get; private set; } = new();

    private void CreateGauges()
    {
        RiskGaugeSeries.Clear();
        var riskColor = RiskLevel switch
        {
            < 30 => "#3FB950",
            < 60 => "#F0883E",
            < 80 => "#F85149",
            _ => "#DA3633"
        };
        RiskGaugeSeries.Add(new PieSeries<double>
        {
            Values = new double[] { RiskLevel },
            Fill = new SolidColorPaint(SKColor.Parse(riskColor)),
            MaxRadialColumnWidth = 20
        });
        RiskGaugeSeries.Add(new PieSeries<double>
        {
            Values = new double[] { 100 - RiskLevel },
            Fill = new SolidColorPaint(SKColor.Parse("#21262D")),
            MaxRadialColumnWidth = 20
        });

        PositionGaugeSeries.Clear();
        var positionColor = SuggestedPosition switch
        {
            <= 20 => "#3FB950",
            <= 50 => "#F0883E",
            _ => "#F85149"
        };
        PositionGaugeSeries.Add(new PieSeries<double>
        {
            Values = new double[] { SuggestedPosition },
            Fill = new SolidColorPaint(SKColor.Parse(positionColor)),
            MaxRadialColumnWidth = 20
        });
        PositionGaugeSeries.Add(new PieSeries<double>
        {
            Values = new double[] { 100 - SuggestedPosition },
            Fill = new SolidColorPaint(SKColor.Parse("#21262D")),
            MaxRadialColumnWidth = 20
        });
    }

    /// <summary>
    /// 更新仪表盘数据
    /// </summary>
    public void UpdateGauges()
    {
        CreateGauges();
        // 通知图表集合已更改
        OnPropertyChanged(nameof(RiskGaugeSeries));
        OnPropertyChanged(nameof(PositionGaugeSeries));
    }
}
