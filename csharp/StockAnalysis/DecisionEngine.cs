namespace StockAnalysis;

/// <summary>
/// Weighted scoring system that merges TA, FA, and News signals into a final recommendation.
/// </summary>
public class DecisionEngine
{
    private static readonly Dictionary<string, double> Weights = new()
    {
        ["Technical"] = 0.40,
        ["Fundamental"] = 0.35,
        ["News"] = 0.25,
    };

    private readonly AnalysisResult _ta;
    private readonly AnalysisResult _fa;
    private readonly NewsResult _news;

    public DecisionEngine(AnalysisResult ta, AnalysisResult fa, NewsResult news)
    {
        _ta = ta;
        _fa = fa;
        _news = news;
    }

    public DecisionResult Decide()
    {
        double taScore = CategoryScore(_ta.Signals);
        double faScore = CategoryScore(_fa.Signals);
        double newsScore = CategoryScore(_news.Signals);

        double weighted = taScore * Weights["Technical"]
                        + faScore * Weights["Fundamental"]
                        + newsScore * Weights["News"];

        var (rec, conf) = MapScore(weighted);

        return new DecisionResult
        {
            Scores = new Dictionary<string, double>
            {
                ["Technical Score"] = Math.Round(taScore, 2),
                ["Fundamental Score"] = Math.Round(faScore, 2),
                ["News Score"] = Math.Round(newsScore, 2),
                ["Weighted Score"] = Math.Round(weighted, 2),
            },
            Weights = Weights,
            Recommendation = rec,
            Confidence = conf,
            Explanation = Explain(taScore, faScore, newsScore, weighted, rec),
        };
    }

    private static double CategoryScore(Dictionary<string, Signal> signals)
    {
        if (signals.Count == 0) return 0.0;
        int total = 0, count = 0;
        foreach (var (key, signal) in signals)
        {
            if (key.StartsWith("_") && key.EndsWith("Overall")) continue;
            count++;
            if (signal.Direction == "Bullish") total++;
            else if (signal.Direction == "Bearish") total--;
        }
        return count > 0 ? (double)total / count : 0.0;
    }

    private static (string Recommendation, string Confidence) MapScore(double score) => score switch
    {
        >= 0.4 => ("STRONG BUY", "High"),
        >= 0.15 => ("BUY", "Moderate"),
        > -0.15 => ("HOLD", "—"),
        > -0.4 => ("SELL", "Moderate"),
        _ => ("STRONG SELL", "High"),
    };

    private static string Explain(double ta, double fa, double news, double weighted, string rec)
    {
        var lines = new[]
        {
            $"Technical analysis score: {ta:+0.00;-0.00} (weight 40%)",
            $"Fundamental analysis score: {fa:+0.00;-0.00} (weight 35%)",
            $"News sentiment score: {news:+0.00;-0.00} (weight 25%)",
            $"Combined weighted score: {weighted:+0.00;-0.00}",
            $"Recommendation: {rec}",
            "",
            "Score interpretation: -1.0 (Strong Sell) to +1.0 (Strong Buy)",
            "  >= +0.40  STRONG BUY  | >= +0.15  BUY  | -0.15 to +0.15  HOLD",
            "  <= -0.15  SELL        | <= -0.40  STRONG SELL",
        };
        return string.Join(Environment.NewLine, lines);
    }
}
