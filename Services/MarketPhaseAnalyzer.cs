using System;
using System.Collections.Generic;
using System.Linq;
using PanoramaFuturesAI.Models;

namespace PanoramaFuturesAI.Services;

/// <summary>
/// 宏观市场阶段分析服务
/// 将单个合约的指标进行全市场汇总，判断当前宏观市场阶段
/// </summary>
public class MarketPhaseAnalyzer
{
    private readonly IndicatorCalculationService _indicatorService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MarketPhaseAnalyzer()
    {
        _indicatorService = new IndicatorCalculationService();
    }

    /// <summary>
    /// 分析全市场宏观阶段
    /// </summary>
    public MarketOverview AnalyzeMarketPhase(List<ContractData> contractDataList)
    {
        var overview = new MarketOverview
        {
            AnalysisTime = DateTime.Now,
            AnalyzedContractCount = contractDataList.Count
        };

        if (contractDataList.Count == 0)
        {
            overview.MarketCommentary = "未能获取到有效的市场数据";
            return overview;
        }

        CalculateMarketAverages(contractDataList, overview);
        var phaseScores = CalculatePhaseScores(contractDataList, overview);
        var (currentPhase, matchScore) = phaseScores.MaxBy(x => x.Value);
        overview.CurrentPhase = currentPhase;
        overview.PhaseMatchScore = matchScore;
        GenerateEvidence(contractDataList, overview);
        DetermineMarketBias(contractDataList, overview);
        GenerateMarketCommentary(overview);

        return overview;
    }

    private void CalculateMarketAverages(List<ContractData> contracts, MarketOverview overview)
    {
        overview.AverageBBW = contracts.Average(c => c.Indicators.BBW);
        overview.BBWPercentile = contracts.Average(c => c.Indicators.BBWPercentile);
        overview.AverageATR = contracts.Average(c => c.Indicators.ATR);
        overview.PercentContractsAboveEMA20 = (decimal)contracts.Count(c => c.Indicators.IsAboveEMA20) / contracts.Count * 100;
        var totalVolumeRatio = contracts.Sum(c => c.Indicators.VolumeRatio);
        overview.VolumeSurgeMultiplier = totalVolumeRatio / contracts.Count;
        overview.AverageADX = contracts.Average(c => c.Indicators.ADX);
        overview.PercentBullishAlignment = (decimal)contracts.Count(c => c.Indicators.IsBullishAlignment) / contracts.Count * 100;
        overview.PercentBearishAlignment = (decimal)contracts.Count(c => c.Indicators.IsBearishAlignment) / contracts.Count * 100;
        overview.PercentContractsFarFromEMA20 = (decimal)contracts.Count(c => Math.Abs(c.Indicators.PriceDeviationFromEMA20) > Constants.PriceDeviationThreshold) / contracts.Count * 100;
        overview.PercentRSIExtreme = (decimal)contracts.Count(c => c.Indicators.IsRSIExtreme) / contracts.Count * 100;
        overview.AverageFundingRate = contracts.Average(c => c.Indicators.FundingRate);
        overview.PercentContractsADXBelow20 = (decimal)contracts.Count(c => c.Indicators.ADX < Constants.ADXWeakTrendThreshold) / contracts.Count * 100;

        // 计算 EMA 聚合指标
        var lastPrices = contracts.Where(c => c.KLines.Count > 0).Select(c => c.KLines.Last().Close).ToList();
        var validContracts = contracts.Where(c => c.Indicators.EMA20 > 0).ToList();

        if (validContracts.Count > 0)
        {
            overview.PercentAboveEMA20 = (decimal)validContracts.Count(c => c.Indicators.IsAboveEMA20) / validContracts.Count * 100;
            overview.PercentAboveEMA50 = (decimal)validContracts.Count(c => c.KLines.Last().Close > c.Indicators.EMA50) / validContracts.Count * 100;
            overview.PercentAboveEMA200 = (decimal)validContracts.Count(c => c.KLines.Last().Close > c.Indicators.EMA200) / validContracts.Count * 100;
            overview.AveragePriceDeviationFromEMA20 = validContracts.Average(c => c.Indicators.PriceDeviationFromEMA20);
        }
    }

    private Dictionary<MarketPhase, decimal> CalculatePhaseScores(List<ContractData> contracts, MarketOverview overview)
    {
        return new Dictionary<MarketPhase, decimal>
        {
            [MarketPhase.Phase1_LowVolatilityAccumulation] = CalculatePhase1Score(overview),
            [MarketPhase.Phase2_HighVolatilityBreakout] = CalculatePhase2Score(overview),
            [MarketPhase.Phase3_TrendContinuation] = CalculatePhase3Score(overview),
            [MarketPhase.Phase4_TrendExhaustion] = CalculatePhase4Score(overview)
        };
    }

    private decimal CalculatePhase1Score(MarketOverview overview)
    {
        decimal score = 0;
        if (overview.BBWPercentile < Constants.BBWLowPercentileThreshold) score += 30;
        if (overview.PercentContractsADXBelow20 > 60) score += 30;
        else if (overview.PercentContractsADXBelow20 > 40) score += 15;
        if (overview.AverageBBW < 0.03m) score += 20;
        if (!overview.IsVolumeSpiking) score += 20;
        return Math.Min(100, score);
    }

    private decimal CalculatePhase2Score(MarketOverview overview)
    {
        decimal score = 0;
        if (overview.BBWPercentile > 50) score += 25;
        if (overview.IsVolumeSpiking) score += 30;
        if (overview.PercentContractsAboveEMA20 > 50) score += 25;
        if (overview.AverageADX > 15 && overview.AverageADX < 30) score += 20;
        return Math.Min(100, score);
    }

    private decimal CalculatePhase3Score(MarketOverview overview)
    {
        decimal score = 0;
        if (overview.AverageADX > Constants.ADXStrongTrendThreshold) score += 35;
        else if (overview.AverageADX > 20) score += 20;
        var alignmentRatio = overview.PercentBullishAlignment + overview.PercentBearishAlignment;
        if (alignmentRatio > 60) score += 35;
        else if (alignmentRatio > 40) score += 20;
        if (overview.PercentBullishAlignment > 40 || overview.PercentBearishAlignment > 40) score += 30;
        return Math.Min(100, score);
    }

    private decimal CalculatePhase4Score(MarketOverview overview)
    {
        decimal score = 0;
        if (overview.PercentContractsFarFromEMA20 > 30) score += 25;
        if (overview.PercentRSIExtreme > 30) score += 25;
        if (overview.IsFundingRateExtreme) score += 30;
        if (overview.AverageADX > 30 && overview.AverageADX < 40) score += 20;
        return Math.Min(100, score);
    }

    private void GenerateEvidence(List<ContractData> contracts, MarketOverview overview)
    {
        overview.EvidenceList.Clear();
        switch (overview.CurrentPhase)
        {
            case MarketPhase.Phase1_LowVolatilityAccumulation:
                AddPhase1Evidence(contracts, overview); break;
            case MarketPhase.Phase2_HighVolatilityBreakout:
                AddPhase2Evidence(contracts, overview); break;
            case MarketPhase.Phase3_TrendContinuation:
                AddPhase3Evidence(contracts, overview); break;
            case MarketPhase.Phase4_TrendExhaustion:
                AddPhase4Evidence(contracts, overview); break;
        }
    }

    private void AddPhase1Evidence(List<ContractData> contracts, MarketOverview overview)
    {
        // 波动率证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "波动率",
            Description = "全市场平均布林带宽度处于历史极低水平",
            Value = $"{overview.BBWPercentile:F1}%",
            Strength = overview.BBWPercentile < 15 ? 10 : 7,
            IsPositive = true
        });

        // 趋势强度证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "趋势强度",
            Description = $"{(int)overview.PercentContractsADXBelow20}% 的合约 ADX < 20，市场无明显趋势",
            Value = $"{overview.AverageADX:F1}",
            Strength = (int)(overview.PercentContractsADXBelow20 / 10),
            IsPositive = true
        });

        // 成交量证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "成交量",
            Description = "成交量处于低位，市场观望情绪浓厚",
            Value = $"{overview.VolumeSurgeMultiplier:F2}x",
            Strength = overview.VolumeSurgeMultiplier < 1.2m ? 8 : 5,
            IsPositive = true
        });

        // 均线排列证据 - 积累阶段均线往往混乱
        var alignmentRatio = overview.PercentBullishAlignment + overview.PercentBearishAlignment;
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "均线状态",
            Description = $"仅 {(int)alignmentRatio}% 合约呈现有序均线排列，大部分处于混沌状态",
            Value = $"{alignmentRatio:F0}%",
            Strength = alignmentRatio < 30 ? 9 : 6,
            IsPositive = true
        });

        // 资金费率证据 - 低波动期费率通常较低
        var fundingSeverity = Math.Abs(overview.AverageFundingRate) * 10000;
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "资金费率",
            Description = fundingSeverity < 5 ? "资金费率平稳，多空博弈均衡" : "资金费率略有波动",
            Value = $"{overview.AverageFundingRate * 100:F4}%",
            Strength = fundingSeverity < 5 ? 7 : 4,
            IsPositive = true
        });

        // 价格偏离证据 - 低波动期价格贴近均线
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "价格偏离",
            Description = $"平均价格偏离 EMA20 仅 {overview.AveragePriceDeviationFromEMA20:F2}%，价格走势平稳",
            Value = $"{overview.AveragePriceDeviationFromEMA20:+0.00;-0.00;0.00}%",
            Strength = Math.Abs(overview.AveragePriceDeviationFromEMA20) < 2 ? 8 : 5,
            IsPositive = true
        });
    }

    private void AddPhase2Evidence(List<ContractData> contracts, MarketOverview overview)
    {
        // 波动率证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "波动率",
            Description = "布林带宽度扩大，市场波动加剧",
            Value = $"{overview.BBWPercentile:F1}%",
            Strength = overview.BBWPercentile > 60 ? 9 : 6,
            IsPositive = true
        });

        // EMA20突破证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "EMA20突破",
            Description = $"{(int)overview.PercentContractsAboveEMA20}% 的合约突破 EMA20，短期动能增强",
            Value = $"{overview.PercentContractsAboveEMA20:F0}%",
            Strength = overview.PercentContractsAboveEMA20 > 50 ? 9 : 6,
            IsPositive = true
        });

        // EMA50突破证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "EMA50突破",
            Description = $"{(int)overview.PercentAboveEMA50}% 的合约突破 EMA50，中期趋势启动",
            Value = $"{overview.PercentAboveEMA50:F0}%",
            Strength = overview.PercentAboveEMA50 > 40 ? 8 : 5,
            IsPositive = true
        });

        // 成交量证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "成交量",
            Description = $"成交量激增至 {overview.VolumeSurgeMultiplier:F2}x 均线水平",
            Value = $"{overview.VolumeSurgeMultiplier:F2}x",
            Strength = overview.IsVolumeSpiking ? 10 : 5,
            IsPositive = true
        });

        // ADX证据 - 突破阶段ADX开始上升但还未进入强趋势
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "ADX变化",
            Description = overview.AverageADX > 15 && overview.AverageADX < 30 ? "ADX开始上升，趋势正在形成" : "ADX处于中等水平",
            Value = $"{overview.AverageADX:F1}",
            Strength = overview.AverageADX > 15 && overview.AverageADX < 30 ? 8 : 5,
            IsPositive = true
        });

        // 价格偏离证据 - 突破时价格开始远离均线
        var priceDeviation = Math.Abs(overview.AveragePriceDeviationFromEMA20);
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "价格偏离",
            Description = $"平均价格偏离 EMA20 {overview.AveragePriceDeviationFromEMA20:+0.00;-0.00;0.00}%，突破动能明显",
            Value = $"{overview.AveragePriceDeviationFromEMA20:+0.00;-0.00;0.00}%",
            Strength = priceDeviation > 2 ? 7 : 4,
            IsPositive = true
        });

        // 资金费率证据 - 突破期费率开始波动
        var fundingDirection = overview.AverageFundingRate > 0 ? "多方" : "空方";
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "资金费率",
            Description = $"资金费率偏向 {fundingDirection}，套利资金开始活跃",
            Value = $"{overview.AverageFundingRate * 100:F4}%",
            Strength = overview.IsFundingRateExtreme ? 6 : 3,
            IsPositive = overview.AverageFundingRate > 0
        });
    }

    private void AddPhase3Evidence(List<ContractData> contracts, MarketOverview overview)
    {
        var trendDirection = overview.PercentBullishAlignment > overview.PercentBearishAlignment ? "多头" : "空头";
        var biasSymbol = overview.PercentBullishAlignment > overview.PercentBearishAlignment ? "📈" : "📉";

        // ADX趋势证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "趋势强度",
            Description = $"全市场平均 ADX = {overview.AverageADX:F1}，{trendDirection}趋势强劲",
            Value = $"{overview.AverageADX:F1}",
            Strength = overview.AverageADX > 30 ? 10 : 7,
            IsPositive = true
        });

        // 均线排列证据
        var alignmentRatio = overview.PercentBullishAlignment + overview.PercentBearishAlignment;
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "均线排列",
            Description = $"{biasSymbol} {(int)alignmentRatio}% 合约呈现{trendDirection}排列",
            Value = $"多头 {overview.PercentBullishAlignment:F0}% / 空头 {overview.PercentBearishAlignment:F0}%",
            Strength = 8,
            IsPositive = true
        });

        // EMA20突破证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "EMA20突破",
            Description = $"{(int)overview.PercentAboveEMA20}% 合约价格站稳 EMA20，趋势得到确认",
            Value = $"{overview.PercentAboveEMA20:F0}%",
            Strength = overview.PercentAboveEMA20 > 60 ? 9 : 6,
            IsPositive = true
        });

        // EMA50突破证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "EMA50突破",
            Description = $"{(int)overview.PercentAboveEMA50}% 合约价格突破 EMA50，中期趋势确认",
            Value = $"{overview.PercentAboveEMA50:F0}%",
            Strength = overview.PercentAboveEMA50 > 50 ? 8 : 5,
            IsPositive = true
        });

        // EMA200突破证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "EMA200突破",
            Description = overview.PercentAboveEMA200 > 50 ? "超过半数合约站上 EMA200，长期趋势看涨" : "EMA200突破率较低，长期趋势未确认",
            Value = $"{overview.PercentAboveEMA200:F0}%",
            Strength = overview.PercentAboveEMA200 > 50 ? 9 : 5,
            IsPositive = overview.PercentAboveEMA200 > 50
        });

        // 成交量证据 - 趋势延续期成交量稳定或温和放大
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "成交量",
            Description = overview.VolumeSurgeMultiplier > 1.2m ? "成交量持续放大，趋势动能充足" : "成交量维持稳定，趋势健康延续",
            Value = $"{overview.VolumeSurgeMultiplier:F2}x",
            Strength = overview.VolumeSurgeMultiplier > 1.5m ? 9 : 6,
            IsPositive = true
        });

        // 价格偏离证据 - 趋势期价格平稳偏离均线
        var priceDeviation = overview.AveragePriceDeviationFromEMA20;
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "价格偏离",
            Description = $"平均价格偏离 EMA20 {priceDeviation:+0.00;-0.00;0.00}%，趋势走势健康",
            Value = $"{priceDeviation:+0.00;-0.00;0.00}%",
            Strength = Math.Abs(priceDeviation) > 3 ? 7 : 4,
            IsPositive = true
        });

        // 资金费率证据
        var fundingDirection = overview.AverageFundingRate > 0 ? "多方" : "空方";
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "资金费率",
            Description = overview.AverageFundingRate > 0 ? $"资金费率支持{trendDirection}，{fundingDirection}持续入场" : "资金费率偏空，但整体可控",
            Value = $"{overview.AverageFundingRate * 100:F4}%",
            Strength = overview.IsFundingRateExtreme ? 7 : 4,
            IsPositive = overview.AverageFundingRate > 0
        });
    }

    private void AddPhase4Evidence(List<ContractData> contracts, MarketOverview overview)
    {
        var fundingDirection = overview.AverageFundingRate > 0 ? "多方" : "空方";
        var trendDirection = overview.PercentBullishAlignment > overview.PercentBearishAlignment ? "多头" : "空头";

        // 价格偏离EMA20证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "价格偏离",
            Description = $"{(int)overview.PercentContractsFarFromEMA20}% 的合约严重偏离 EMA20，涨势过热",
            Value = $"{overview.PercentContractsFarFromEMA20:F0}%",
            Strength = overview.PercentContractsFarFromEMA20 > 40 ? 9 : 6,
            IsPositive = false
        });

        // RSI极值证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "RSI极值",
            Description = $"{(int)overview.PercentRSIExtreme}% 的合约 RSI 处于超买/超卖区域",
            Value = $"{overview.PercentRSIExtreme:F0}%",
            Strength = overview.PercentRSIExtreme > 30 ? 8 : 5,
            IsPositive = false
        });

        // 资金费率证据
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "资金费率",
            Description = $"全市场平均费率 {fundingDirection} 方支付 {Math.Abs(overview.AverageFundingRate) * 100:F3}%，{(overview.IsFundingRateExtreme ? "费率极端，市场过热" : "费率处于高位")}",
            Value = $"{overview.AverageFundingRate * 100:F3}%",
            Strength = overview.IsFundingRateExtreme ? 9 : 5,
            IsPositive = false
        });

        // ADX证据 - 衰竭期ADX可能开始下降
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "ADX变化",
            Description = overview.AverageADX > 30 && overview.AverageADX < 40 ? "ADX处于高位但未继续上升，上涨动能可能衰竭" : $"ADX = {overview.AverageADX:F1}",
            Value = $"{overview.AverageADX:F1}",
            Strength = overview.AverageADX > 30 && overview.AverageADX < 40 ? 7 : 4,
            IsPositive = false
        });

        // EMA200突破证据 - 衰竭期可能突破200日均线
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "EMA200突破",
            Description = overview.PercentAboveEMA200 > 70 ? "大量合约突破 EMA200，长期支撑变为阻力位" : "EMA200突破率较高，但需警惕反转风险",
            Value = $"{overview.PercentAboveEMA200:F0}%",
            Strength = overview.PercentAboveEMA200 > 70 ? 9 : 5,
            IsPositive = false
        });

        // 均线排列证据 - 衰竭期排列过于完美可能是陷阱
        var alignmentRatio = overview.PercentBullishAlignment + overview.PercentBearishAlignment;
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "均线排列",
            Description = $"{(int)alignmentRatio}% 合约呈现{trendDirection}排列，极度有序暗示反转风险",
            Value = $"多头 {overview.PercentBullishAlignment:F0}% / 空头 {overview.PercentBearishAlignment:F0}%",
            Strength = alignmentRatio > 70 ? 8 : 5,
            IsPositive = false
        });

        // 成交量证据 - 衰竭期成交量可能放大但价格停滞
        overview.EvidenceList.Add(new PhaseEvidence
        {
            Type = "成交量背离",
            Description = overview.VolumeSurgeMultiplier > 1.8m ? "成交量异常放大但涨势趋缓，警惕顶部形成" : "成交量维持在高位，需密切关注",
            Value = $"{overview.VolumeSurgeMultiplier:F2}x",
            Strength = overview.VolumeSurgeMultiplier > 1.8m ? 9 : 5,
            IsPositive = false
        });
    }

    private void DetermineMarketBias(List<ContractData> contracts, MarketOverview overview)
    {
        if (overview.PercentBullishAlignment > overview.PercentBearishAlignment + 20)
        {
            overview.MarketBias = 1;
            overview.RiskLevel = overview.CurrentPhase == MarketPhase.Phase4_TrendExhaustion ? 4 : 2;
        }
        else if (overview.PercentBearishAlignment > overview.PercentBullishAlignment + 20)
        {
            overview.MarketBias = -1;
            overview.RiskLevel = overview.CurrentPhase == MarketPhase.Phase4_TrendExhaustion ? 4 : 2;
        }
        else
        {
            overview.MarketBias = 0;
            overview.RiskLevel = overview.CurrentPhase == MarketPhase.Phase4_TrendExhaustion ? 3 : 1;
        }
    }

    private void GenerateMarketCommentary(MarketOverview overview)
    {
        var phaseName = overview.CurrentPhase.GetDescription();
        var biasText = overview.MarketBias switch
        {
            1 => "整体偏向多头",
            -1 => "整体偏向空头",
            _ => "市场整体中性"
        };
        var riskText = overview.RiskLevel switch
        {
            1 => "低风险",
            2 => "中等风险",
            3 => "较高风险",
            _ => "高风险"
        };
        overview.MarketCommentary = $"当前市场处于【{phaseName}】，{biasText}，风险等级：{riskText}。";
    }
}
