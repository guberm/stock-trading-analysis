namespace StockAnalysis;

public record StockBar(DateTime Date, double Open, double High, double Low, double Close, long Volume);

public record Signal(string Direction, string Reason);

public record AnalysisResult
{
    public Dictionary<string, object> Metrics { get; init; } = new();
    public Dictionary<string, Signal> Signals { get; init; } = new();
}

public record NewsHeadline
{
    public string Title { get; init; } = "";
    public string Source { get; init; } = "";
    public string Date { get; init; } = "";
    public double SentimentScore { get; set; }
    public string Sentiment { get; set; } = "";
}

public record NewsResult
{
    public List<NewsHeadline> Headlines { get; init; } = new();
    public Dictionary<string, object> Metrics { get; init; } = new();
    public Dictionary<string, Signal> Signals { get; init; } = new();
}

public record DecisionResult
{
    public Dictionary<string, double> Scores { get; init; } = new();
    public Dictionary<string, double> Weights { get; init; } = new();
    public string Recommendation { get; init; } = "";
    public string Confidence { get; init; } = "";
    public string Explanation { get; init; } = "";
}
