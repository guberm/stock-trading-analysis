namespace StockAnalysis;

/// <summary>
/// Scores a stock based on fundamental financial metrics from Yahoo Finance.
/// </summary>
public class FundamentalAnalysis
{
    private readonly Dictionary<string, object?> _info;
    public Dictionary<string, object> Metrics { get; } = new();
    public Dictionary<string, Signal> Signals { get; } = new();

    public FundamentalAnalysis(Dictionary<string, object?> info) => _info = info;

    public AnalysisResult ComputeAll()
    {
        Valuation();
        Profitability();
        Growth();
        FinancialHealth();
        Dividends();
        AnalystTargets();
        AggregateSignal();
        return new AnalysisResult { Metrics = Metrics, Signals = Signals };
    }

    private void Valuation()
    {
        double? pe = Dbl("trailingPE"), fwdPe = Dbl("forwardPE"), peg = Dbl("pegRatio");
        double? pb = Dbl("priceToBook"), ps = Dbl("priceToSalesTrailing12Months"), evEbitda = Dbl("enterpriseToEbitda");

        Metrics["P/E (TTM)"] = Fmt(pe);
        Metrics["Forward P/E"] = Fmt(fwdPe);
        Metrics["PEG Ratio"] = Fmt(peg);
        Metrics["P/B"] = Fmt(pb);
        Metrics["P/S"] = Fmt(ps);
        Metrics["EV/EBITDA"] = Fmt(evEbitda);

        int score = 0;
        var reasons = new List<string>();
        if (pe is < 20) { score++; reasons.Add($"P/E {pe:F1} < 20"); }
        else if (pe is > 35) { score--; reasons.Add($"P/E {pe:F1} > 35"); }
        if (peg is < 1.5) { score++; reasons.Add($"PEG {peg:F2} < 1.5"); }
        else if (peg is > 2.5) { score--; reasons.Add($"PEG {peg:F2} > 2.5"); }
        if (pb is < 3) { score++; reasons.Add($"P/B {pb:F2} < 3"); }

        Signals["Valuation"] = score > 0 ? new Signal("Bullish", string.Join("; ", reasons))
            : score < 0 ? new Signal("Bearish", string.Join("; ", reasons))
            : new Signal("Neutral", "Fair valuation");
    }

    private void Profitability()
    {
        double? margin = Dbl("profitMargins"), opMargin = Dbl("operatingMargins");
        double? roe = Dbl("returnOnEquity"), roa = Dbl("returnOnAssets");

        Metrics["Profit Margin"] = Pct(margin);
        Metrics["Operating Margin"] = Pct(opMargin);
        Metrics["ROE"] = Pct(roe);
        Metrics["ROA"] = Pct(roa);

        int score = 0;
        var reasons = new List<string>();
        if (margin is > 0.15) { score++; reasons.Add($"Profit margin {margin:P1}"); }
        if (roe is > 0.15) { score++; reasons.Add($"ROE {roe:P1}"); }
        if (roa is > 0.05) { score++; reasons.Add($"ROA {roa:P1}"); }

        Signals["Profitability"] = score >= 2 ? new Signal("Bullish", string.Join("; ", reasons))
            : score == 0 && margin is < 0 ? new Signal("Bearish", "Negative margins")
            : new Signal("Neutral", reasons.Count > 0 ? string.Join("; ", reasons) : "Average profitability");
    }

    private void Growth()
    {
        double? revGrowth = Dbl("revenueGrowth"), earnGrowth = Dbl("earningsGrowth");

        Metrics["Revenue Growth"] = Pct(revGrowth);
        Metrics["Earnings Growth"] = Pct(earnGrowth);

        int score = 0;
        var reasons = new List<string>();
        if (revGrowth is > 0.10) { score++; reasons.Add($"Revenue growth {revGrowth:P1}"); }
        if (earnGrowth is > 0.10) { score++; reasons.Add($"Earnings growth {earnGrowth:P1}"); }

        Signals["Growth"] = score >= 1 ? new Signal("Bullish", string.Join("; ", reasons))
            : revGrowth is < 0 ? new Signal("Bearish", $"Revenue declining {revGrowth:P1}")
            : new Signal("Neutral", "Moderate growth");
    }

    private void FinancialHealth()
    {
        double? de = Dbl("debtToEquity"), current = Dbl("currentRatio"), quick = Dbl("quickRatio");
        double? fcf = Dbl("freeCashflow"), cash = Dbl("totalCash"), debt = Dbl("totalDebt");

        Metrics["Debt/Equity"] = Fmt(de);
        Metrics["Current Ratio"] = Fmt(current);
        Metrics["Quick Ratio"] = Fmt(quick);
        Metrics["Free Cash Flow"] = HumanNumber(fcf);
        Metrics["Total Cash"] = HumanNumber(cash);
        Metrics["Total Debt"] = HumanNumber(debt);

        int score = 0;
        var reasons = new List<string>();
        if (de is < 100) { score++; reasons.Add($"D/E {de:F1} < 100"); }
        else if (de is > 200) { score--; reasons.Add($"D/E {de:F1} > 200"); }
        if (current is > 1.5) { score++; reasons.Add($"Current ratio {current:F2}"); }
        if (fcf is > 0) { score++; reasons.Add("Positive FCF"); }

        Signals["Financial Health"] = score >= 2 ? new Signal("Bullish", string.Join("; ", reasons))
            : score < 0 ? new Signal("Bearish", string.Join("; ", reasons))
            : new Signal("Neutral", reasons.Count > 0 ? string.Join("; ", reasons) : "Average health");
    }

    private void Dividends()
    {
        double? divYield = Dbl("dividendYield"), payout = Dbl("payoutRatio");

        Metrics["Dividend Yield"] = Pct(divYield);
        Metrics["Payout Ratio"] = Pct(payout);

        Signals["Dividends"] = divYield is > 0.02 ? new Signal("Bullish", $"Yield {divYield:P2}")
            : divYield is > 0 ? new Signal("Neutral", $"Yield {divYield:P2}")
            : new Signal("Neutral", "No dividend");
    }

    private void AnalystTargets()
    {
        double? target = Dbl("targetMeanPrice");
        double? currentPrice = Dbl("currentPrice");
        string? rec = _info.GetValueOrDefault("recommendationKey") as string;

        Metrics["Analyst Target Price"] = Fmt(target);
        Metrics["Current Price"] = Fmt(currentPrice);
        Metrics["Analyst Recommendation"] = rec ?? "N/A";

        if (target.HasValue && currentPrice.HasValue && currentPrice > 0)
        {
            double upside = (target.Value - currentPrice.Value) / currentPrice.Value;
            Metrics["Upside to Target"] = $"{upside:P1}";

            Signals["Analyst Target"] = upside > 0.15 ? new Signal("Bullish", $"{upside:P1} upside to ${target:F2}")
                : upside < -0.10 ? new Signal("Bearish", $"{upside:P1} downside to ${target:F2}")
                : new Signal("Neutral", $"{upside:P1} to target ${target:F2}");
        }
    }

    private void AggregateSignal()
    {
        int bullish = Signals.Values.Count(s => s.Direction == "Bullish");
        int bearish = Signals.Values.Count(s => s.Direction == "Bearish");
        int total = Signals.Count;

        Metrics["_fa_bullish"] = bullish;
        Metrics["_fa_bearish"] = bearish;
        Metrics["_fa_neutral"] = total - bullish - bearish;

        Signals["_FA_Overall"] = bullish > bearish + 1
            ? new Signal("Bullish", $"{bullish}/{total} fundamentals bullish")
            : bearish > bullish + 1
                ? new Signal("Bearish", $"{bearish}/{total} fundamentals bearish")
                : new Signal("Neutral", $"Mixed – {bullish}B/{bearish}S/{total - bullish - bearish}N");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private double? Dbl(string key)
    {
        if (_info.TryGetValue(key, out var val) && val is double d) return d;
        return null;
    }

    private static object Fmt(double? val) => val.HasValue ? Math.Round(val.Value, 2) : (object)"N/A";
    private static string Pct(double? val) => val.HasValue ? $"{val:P2}" : "N/A";

    private static string HumanNumber(double? val)
    {
        if (!val.HasValue) return "N/A";
        double abs = Math.Abs(val.Value);
        string sign = val < 0 ? "-" : "";
        if (abs >= 1_000_000_000_000) return $"{sign}${abs / 1_000_000_000_000:F2}T";
        if (abs >= 1_000_000_000) return $"{sign}${abs / 1_000_000_000:F2}B";
        if (abs >= 1_000_000) return $"{sign}${abs / 1_000_000:F2}M";
        return $"{sign}${abs:N0}";
    }
}
