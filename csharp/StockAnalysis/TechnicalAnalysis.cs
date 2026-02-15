namespace StockAnalysis;

/// <summary>
/// Computes technical indicators and generates trading signals from price/volume data.
/// </summary>
public class TechnicalAnalysis
{
    private readonly List<StockBar> _bars;
    public Dictionary<string, object> Indicators { get; } = new();
    public Dictionary<string, Signal> Signals { get; } = new();

    public TechnicalAnalysis(List<StockBar> bars) => _bars = bars;

    public AnalysisResult ComputeAll()
    {
        var closes = _bars.Select(b => b.Close).ToArray();
        var highs = _bars.Select(b => b.High).ToArray();
        var lows = _bars.Select(b => b.Low).ToArray();
        var volumes = _bars.Select(b => (double)b.Volume).ToArray();

        MovingAverages(closes);
        Rsi(closes);
        Macd(closes);
        BollingerBands(closes);
        Stochastic(highs, lows, closes);
        Atr(highs, lows, closes);
        Obv(closes, volumes);
        Adx(highs, lows, closes);
        WilliamsR(highs, lows, closes);
        Cci(highs, lows, closes);
        AggregateSignal();

        return new AnalysisResult { Metrics = Indicators, Signals = Signals };
    }

    private void MovingAverages(double[] closes)
    {
        double sma20 = Sma(closes, 20);
        double sma50 = Sma(closes, 50);
        double sma200 = Sma(closes, 200);
        double ema12 = Ema(closes, 12);
        double ema26 = Ema(closes, 26);
        double last = closes[^1];

        Indicators["SMA_20"] = Math.Round(sma20, 2);
        Indicators["SMA_50"] = Math.Round(sma50, 2);
        Indicators["SMA_200"] = Math.Round(sma200, 2);
        Indicators["EMA_12"] = Math.Round(ema12, 2);
        Indicators["EMA_26"] = Math.Round(ema26, 2);

        Signals["MA_Cross"] = sma50 > sma200
            ? new Signal("Bullish", "Golden Cross (SMA50 > SMA200)")
            : new Signal("Bearish", "Death Cross (SMA50 < SMA200)");

        Signals["Price_vs_SMA200"] = last > sma200
            ? new Signal("Bullish", $"Price {last:F2} above SMA200 {sma200:F2}")
            : new Signal("Bearish", $"Price {last:F2} below SMA200 {sma200:F2}");
    }

    private void Rsi(double[] closes, int period = 14)
    {
        double val = CalcRsi(closes, period);
        Indicators["RSI_14"] = Math.Round(val, 2);

        Signals["RSI"] = val > 70 ? new Signal("Bearish", $"RSI {val:F2} – overbought")
            : val < 30 ? new Signal("Bullish", $"RSI {val:F2} – oversold")
            : new Signal("Neutral", $"RSI {val:F2} – neutral zone");
    }

    private void Macd(double[] closes)
    {
        double ema12 = Ema(closes, 12);
        double ema26 = Ema(closes, 26);
        double[] macdLine = new double[closes.Length];
        double[] ema12Arr = EmaArray(closes, 12);
        double[] ema26Arr = EmaArray(closes, 26);

        for (int i = 0; i < closes.Length; i++)
            macdLine[i] = ema12Arr[i] - ema26Arr[i];

        double[] signalLine = EmaArray(macdLine, 9);
        double macdVal = macdLine[^1];
        double signalVal = signalLine[^1];
        double hist = macdVal - signalVal;

        Indicators["MACD_Line"] = Math.Round(macdVal, 4);
        Indicators["MACD_Signal"] = Math.Round(signalVal, 4);
        Indicators["MACD_Histogram"] = Math.Round(hist, 4);

        Signals["MACD"] = macdVal > signalVal
            ? new Signal("Bullish", "MACD line above signal line")
            : new Signal("Bearish", "MACD line below signal line");
    }

    private void BollingerBands(double[] closes, int period = 20, double mult = 2.0)
    {
        double mid = Sma(closes, period);
        double std = StdDev(closes, period);
        double upper = mid + mult * std;
        double lower = mid - mult * std;
        double last = closes[^1];

        Indicators["BB_Upper"] = Math.Round(upper, 2);
        Indicators["BB_Middle"] = Math.Round(mid, 2);
        Indicators["BB_Lower"] = Math.Round(lower, 2);

        Signals["Bollinger"] = last >= upper ? new Signal("Bearish", $"Price at/above upper band ({upper:F2})")
            : last <= lower ? new Signal("Bullish", $"Price at/below lower band ({lower:F2})")
            : new Signal("Neutral", $"Price within bands ({lower:F2} – {upper:F2})");
    }

    private void Stochastic(double[] highs, double[] lows, double[] closes, int period = 14)
    {
        int n = closes.Length;
        double highest = highs.Skip(n - period).Max();
        double lowest = lows.Skip(n - period).Min();
        double k = (closes[^1] - lowest) / (highest - lowest) * 100;

        Indicators["Stoch_K"] = Math.Round(k, 2);

        Signals["Stochastic"] = k < 20 ? new Signal("Bullish", $"Stochastic %K={k:F2} – oversold")
            : k > 80 ? new Signal("Bearish", $"Stochastic %K={k:F2} – overbought")
            : new Signal("Neutral", $"Stochastic %K={k:F2} – neutral");
    }

    private void Atr(double[] highs, double[] lows, double[] closes, int period = 14)
    {
        int n = closes.Length;
        var trs = new double[n];
        trs[0] = highs[0] - lows[0];
        for (int i = 1; i < n; i++)
        {
            double hl = highs[i] - lows[i];
            double hc = Math.Abs(highs[i] - closes[i - 1]);
            double lc = Math.Abs(lows[i] - closes[i - 1]);
            trs[i] = Math.Max(hl, Math.Max(hc, lc));
        }
        double atr = trs.Skip(n - period).Average();
        double pct = atr / closes[^1] * 100;
        Indicators["ATR_14"] = Math.Round(atr, 2);
        Indicators["ATR_%"] = Math.Round(pct, 2);
    }

    private void Obv(double[] closes, double[] volumes)
    {
        double obv = 0;
        double obv10Ago = 0;
        for (int i = 1; i < closes.Length; i++)
        {
            if (closes[i] > closes[i - 1]) obv += volumes[i];
            else if (closes[i] < closes[i - 1]) obv -= volumes[i];

            if (i == closes.Length - 11) obv10Ago = obv;
        }
        Indicators["OBV"] = (long)obv;

        Signals["OBV"] = obv > obv10Ago
            ? new Signal("Bullish", "OBV rising – volume confirming trend")
            : new Signal("Bearish", "OBV falling – volume diverging");
    }

    private void Adx(double[] highs, double[] lows, double[] closes, int period = 14)
    {
        int n = closes.Length;
        if (n < period + 1)
        {
            Indicators["ADX"] = 0.0;
            Signals["ADX"] = new Signal("Neutral", "Insufficient data");
            return;
        }

        var plusDm = new double[n];
        var minusDm = new double[n];
        var tr = new double[n];

        for (int i = 1; i < n; i++)
        {
            double upMove = highs[i] - highs[i - 1];
            double downMove = lows[i - 1] - lows[i];
            plusDm[i] = upMove > downMove && upMove > 0 ? upMove : 0;
            minusDm[i] = downMove > upMove && downMove > 0 ? downMove : 0;
            double hl = highs[i] - lows[i];
            double hc = Math.Abs(highs[i] - closes[i - 1]);
            double lc = Math.Abs(lows[i] - closes[i - 1]);
            tr[i] = Math.Max(hl, Math.Max(hc, lc));
        }

        double smoothTr = tr.Skip(1).Take(period).Sum();
        double smoothPlusDm = plusDm.Skip(1).Take(period).Sum();
        double smoothMinusDm = minusDm.Skip(1).Take(period).Sum();

        var dx = new List<double>();
        for (int i = period + 1; i < n; i++)
        {
            smoothTr = smoothTr - smoothTr / period + tr[i];
            smoothPlusDm = smoothPlusDm - smoothPlusDm / period + plusDm[i];
            smoothMinusDm = smoothMinusDm - smoothMinusDm / period + minusDm[i];

            double plusDi = smoothTr > 0 ? smoothPlusDm / smoothTr * 100 : 0;
            double minusDi = smoothTr > 0 ? smoothMinusDm / smoothTr * 100 : 0;
            double diSum = plusDi + minusDi;
            double dxVal = diSum > 0 ? Math.Abs(plusDi - minusDi) / diSum * 100 : 0;
            dx.Add(dxVal);
        }

        double adx = dx.Count >= period ? dx.Skip(dx.Count - period).Average() : dx.LastOrDefault();
        Indicators["ADX"] = Math.Round(adx, 2);

        Signals["ADX"] = adx > 25
            ? new Signal("Neutral", $"ADX {adx:F2} – strong trend")
            : new Signal("Neutral", $"ADX {adx:F2} – weak/no trend");
    }

    private void WilliamsR(double[] highs, double[] lows, double[] closes, int period = 14)
    {
        int n = closes.Length;
        double highest = highs.Skip(n - period).Max();
        double lowest = lows.Skip(n - period).Min();
        double wr = (highest - closes[^1]) / (highest - lowest) * -100;

        Indicators["Williams_%R"] = Math.Round(wr, 2);

        Signals["Williams_R"] = wr < -80 ? new Signal("Bullish", $"Williams %R {wr:F2} – oversold")
            : wr > -20 ? new Signal("Bearish", $"Williams %R {wr:F2} – overbought")
            : new Signal("Neutral", $"Williams %R {wr:F2} – neutral");
    }

    private void Cci(double[] highs, double[] lows, double[] closes, int period = 20)
    {
        int n = closes.Length;
        var tp = new double[n];
        for (int i = 0; i < n; i++)
            tp[i] = (highs[i] + lows[i] + closes[i]) / 3;

        double mean = tp.Skip(n - period).Average();
        double meanDev = tp.Skip(n - period).Select(x => Math.Abs(x - mean)).Average();
        double cci = meanDev > 0 ? (tp[^1] - mean) / (0.015 * meanDev) : 0;

        Indicators["CCI"] = Math.Round(cci, 2);

        Signals["CCI"] = cci > 100 ? new Signal("Bearish", $"CCI {cci:F2} – overbought")
            : cci < -100 ? new Signal("Bullish", $"CCI {cci:F2} – oversold")
            : new Signal("Neutral", $"CCI {cci:F2} – neutral");
    }

    private void AggregateSignal()
    {
        int bullish = Signals.Values.Count(s => s.Direction == "Bullish");
        int bearish = Signals.Values.Count(s => s.Direction == "Bearish");
        int total = Signals.Count;

        Indicators["_ta_bullish"] = bullish;
        Indicators["_ta_bearish"] = bearish;
        Indicators["_ta_neutral"] = total - bullish - bearish;

        Signals["_TA_Overall"] = bullish > bearish + 2
            ? new Signal("Bullish", $"{bullish}/{total} signals bullish")
            : bearish > bullish + 2
                ? new Signal("Bearish", $"{bearish}/{total} signals bearish")
                : new Signal("Neutral", $"Mixed – {bullish}B/{bearish}S/{total - bullish - bearish}N");
    }

    // ── Math helpers ─────────────────────────────────────────────────────

    private static double Sma(double[] data, int period)
    {
        int start = Math.Max(0, data.Length - period);
        return data.Skip(start).Average();
    }

    private static double Ema(double[] data, int period)
    {
        double k = 2.0 / (period + 1);
        double ema = data[0];
        for (int i = 1; i < data.Length; i++)
            ema = data[i] * k + ema * (1 - k);
        return ema;
    }

    private static double[] EmaArray(double[] data, int period)
    {
        double k = 2.0 / (period + 1);
        var result = new double[data.Length];
        result[0] = data[0];
        for (int i = 1; i < data.Length; i++)
            result[i] = data[i] * k + result[i - 1] * (1 - k);
        return result;
    }

    private static double StdDev(double[] data, int period)
    {
        int start = Math.Max(0, data.Length - period);
        var slice = data.Skip(start).ToArray();
        double mean = slice.Average();
        double variance = slice.Select(x => (x - mean) * (x - mean)).Average();
        return Math.Sqrt(variance);
    }

    private static double CalcRsi(double[] closes, int period)
    {
        double gainSum = 0, lossSum = 0;
        for (int i = closes.Length - period; i < closes.Length; i++)
        {
            double change = closes[i] - closes[i - 1];
            if (change > 0) gainSum += change; else lossSum -= change;
        }
        double avgGain = gainSum / period;
        double avgLoss = lossSum / period;
        if (avgLoss == 0) return 100;
        double rs = avgGain / avgLoss;
        return 100 - 100 / (1 + rs);
    }
}
