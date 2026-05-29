using System;
using System.Collections.Generic;
using System.Linq;
using PanoramaFuturesAI.Models;

namespace PanoramaFuturesAI.Services;

/// <summary>
/// 技术指标计算服务
/// 手动实现各种技术指标以确保兼容性
/// </summary>
public class IndicatorCalculationService
{
    /// <summary>
    /// 计算单个合约的所有技术指标
    /// </summary>
    public ContractIndicators CalculateIndicators(string symbol, List<KLine> klines, decimal fundingRate = 0)
    {
        var indicators = new ContractIndicators { Symbol = symbol, KLines = klines };

        if (klines == null || klines.Count < Constants.MinKlinesForAnalysis) return indicators;

        var closes = klines.Select(k => (double)k.Close).ToList();
        var highs = klines.Select(k => (double)k.High).ToList();
        var lows = klines.Select(k => (double)k.Low).ToList();
        var volumes = klines.Select(k => (double)k.Volume).ToList();

        // 计算布林带
        var (bbUpper, bbLower, bbw, bbwPercentile) = CalculateBollingerBands(closes);
        indicators.BBUpper = (decimal)bbUpper;
        indicators.BBLower = (decimal)bbLower;
        indicators.BBW = (decimal)bbw;
        indicators.BBWPercentile = (decimal)bbwPercentile;

        // 计算 EMA
        indicators.EMA20 = (decimal)CalculateEMA(closes, Constants.EmaShortPeriod);
        indicators.EMA50 = (decimal)CalculateEMA(closes, Constants.EmaMediumPeriod);
        indicators.EMA200 = (decimal)CalculateEMA(closes, Constants.EmaLongPeriod);

        var lastClose = (double)klines.Last().Close;
        if (indicators.EMA20 > 0)
        {
            indicators.PriceDeviationFromEMA20 = (decimal)((lastClose - (double)indicators.EMA20) / (double)indicators.EMA20 * 100);
        }
        indicators.IsAboveEMA20 = lastClose > (double)indicators.EMA20;
        indicators.IsBullishAlignment = indicators.EMA20 > indicators.EMA50 && indicators.EMA50 > indicators.EMA200 && indicators.EMA20 > 0;
        indicators.IsBearishAlignment = indicators.EMA20 < indicators.EMA50 && indicators.EMA50 < indicators.EMA200 && indicators.EMA20 > 0;

        // 计算 RSI
        indicators.RSI = (decimal)CalculateRSI(closes, Constants.RsiPeriod);

        // 计算 ATR
        indicators.ATR = (decimal)CalculateATR(highs, lows, closes, Constants.AtrPeriod);

        // 计算 ADX
        var (adx, plusDI, minusDI) = CalculateADX(highs, lows, closes, Constants.AdxPeriod);
        indicators.ADX = (decimal)adx;
        indicators.PlusDI = (decimal)plusDI;
        indicators.MinusDI = (decimal)minusDI;

        // 成交量指标
        if (klines.Count >= 10)
        {
            indicators.AverageVolume = (decimal)volumes.TakeLast(10).Average();
            indicators.CurrentVolume = (decimal)volumes.Last();
        }

        indicators.FundingRate = fundingRate;
        return indicators;
    }

    private (double upper, double lower, double bbw, double percentile) CalculateBollingerBands(List<double> closes)
    {
        var period = Constants.BollingerPeriod;
        if (closes.Count < period) return (0, 0, 0, 50);

        var sma = closes.TakeLast(period).Average();
        var stdDev = CalculateStdDev(closes.TakeLast(period).ToList(), sma);
        var upper = sma + (double)Constants.BollingerStdDev * stdDev;
        var lower = sma - (double)Constants.BollingerStdDev * stdDev;
        var bbw = (upper - lower) / sma;

        // 计算历史百分位
        var bbwHistory = new List<double>();
        for (int i = period; i <= closes.Count; i++)
        {
            var slice = closes.Skip(i - period).Take(period).ToList();
            var sliceAvg = slice.Average();
            var sliceStd = CalculateStdDev(slice, sliceAvg);
            if (sliceAvg > 0)
                bbwHistory.Add(((sliceAvg + (double)Constants.BollingerStdDev * sliceStd) - (sliceAvg - (double)Constants.BollingerStdDev * sliceStd)) / sliceAvg);
        }

        var belowCount = bbwHistory.Count(v => v < bbw);
        var percentile = bbwHistory.Count > 1 ? (belowCount * 100.0 / (bbwHistory.Count - 1)) : 50;

        return (upper, lower, bbw, percentile);
    }

    private double CalculateStdDev(List<double> values, double mean)
    {
        if (values.Count < 2) return 0;
        var sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }

    private double CalculateEMA(List<double> values, int period)
    {
        if (values.Count < period) return 0;
        var multiplier = 2.0 / (period + 1);
        var ema = values.Take(period).Average();
        for (int i = period; i < values.Count; i++)
        {
            ema = (values[i] - ema) * multiplier + ema;
        }
        return ema;
    }

    private double CalculateRSI(List<double> closes, int period)
    {
        if (closes.Count < period + 1) return 50;

        var gains = new List<double>();
        var losses = new List<double>();

        for (int i = closes.Count - period; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? -change : 0);
        }

        var avgGain = gains.Average();
        var avgLoss = losses.Average();

        if (avgLoss == 0) return 100;
        var rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }

    private double CalculateATR(List<double> highs, List<double> lows, List<double> closes, int period)
    {
        if (closes.Count < period + 1) return 0;

        var trList = new List<double>();
        for (int i = closes.Count - period; i < closes.Count; i++)
        {
            var tr = Math.Max(
                highs[i] - lows[i],
                Math.Max(
                    Math.Abs(highs[i] - closes[i - 1]),
                    Math.Abs(lows[i] - closes[i - 1])
                )
            );
            trList.Add(tr);
        }
        return trList.Average();
    }

    private (double adx, double plusDI, double minusDI) CalculateADX(List<double> highs, List<double> lows, List<double> closes, int period)
    {
        if (closes.Count < period + 1) return (0, 0, 0);

        var plusDMList = new List<double>();
        var minusDMList = new List<double>();
        var trList = new List<double>();

        for (int i = closes.Count - period; i < closes.Count; i++)
        {
            var highDiff = highs[i] - highs[i - 1];
            var lowDiff = lows[i - 1] - lows[i];

            var plusDM = (highDiff > lowDiff && highDiff > 0) ? highDiff : 0;
            var minusDM = (lowDiff > highDiff && lowDiff > 0) ? lowDiff : 0;

            var tr = Math.Max(highs[i] - lows[i], Math.Max(Math.Abs(highs[i] - closes[i - 1]), Math.Abs(lows[i] - closes[i - 1])));

            plusDMList.Add(plusDM);
            minusDMList.Add(minusDM);
            trList.Add(tr);
        }

        var atr = trList.Average();
        if (atr == 0) return (0, 0, 0);

        var plusDI = plusDMList.Average() / atr * 100;
        var minusDI = minusDMList.Average() / atr * 100;

        var sumDI = plusDI + minusDI;
        var dx = sumDI > 0 ? Math.Abs(plusDI - minusDI) / sumDI * 100 : 0;

        return (dx, plusDI, minusDI);
    }
}
