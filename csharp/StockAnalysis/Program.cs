using StockAnalysis;

// ── Parse arguments ──────────────────────────────────────────────────────────
string symbol;
string period = "1y";

if (args.Length < 1)
{
    Console.WriteLine("Usage: StockAnalysis <TICKER> [--period 1y]");
    Console.WriteLine("  e.g. StockAnalysis MSFT");
    Console.WriteLine("  e.g. StockAnalysis NYSE:MSFT --period 6mo");
    return;
}

symbol = args[0].Contains(':') ? args[0].Split(':').Last().Trim().ToUpper() : args[0].Trim().ToUpper();

for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--period" && i + 1 < args.Length)
        period = args[++i];
}

var sep = new string('=', 72);
var thinSep = new string('-', 72);

Console.WriteLine();
Console.WriteLine("Stock Trading Decision System");
Console.WriteLine($"Analyzing: {symbol}");
Console.WriteLine(sep);

// ── Fetch data ───────────────────────────────────────────────────────────────
Console.WriteLine("\nFetching market data...");
var client = new YahooFinanceClient();

List<StockBar> history;
Dictionary<string, object?> fundamentals;
try
{
    history = await client.GetHistoryAsync(symbol, period);
    fundamentals = await client.GetFundamentalsAsync(symbol);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error fetching data for '{symbol}': {ex.Message}");
    Console.ResetColor();
    return;
}

if (history.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: No data found for '{symbol}'. Check the ticker.");
    Console.ResetColor();
    return;
}

string companyName = fundamentals.GetValueOrDefault("shortName") as string ?? symbol;
double currentPrice = history[^1].Close;

Console.WriteLine($"  Company : {companyName}");
Console.WriteLine($"  Price   : ${currentPrice:F2}");
Console.WriteLine($"  Period  : {period} ({history.Count} trading days)");

// ── Technical Analysis ───────────────────────────────────────────────────────
PrintHeader("TECHNICAL ANALYSIS");
var ta = new TechnicalAnalysis(history);
var taResult = ta.ComputeAll();

Console.WriteLine("\n  Indicators:");
PrintMetrics(taResult.Metrics);
Console.WriteLine("\n  Signals:");
PrintSignals(taResult.Signals);
PrintOverall(taResult.Signals, "Technical");

// ── Fundamental Analysis ─────────────────────────────────────────────────────
PrintHeader("FUNDAMENTAL ANALYSIS");
var fa = new FundamentalAnalysis(fundamentals);
var faResult = fa.ComputeAll();

Console.WriteLine("\n  Metrics:");
PrintMetrics(faResult.Metrics);
Console.WriteLine("\n  Signals:");
PrintSignals(faResult.Signals);
PrintOverall(faResult.Signals, "Fundamental");

// ── News Sentiment ───────────────────────────────────────────────────────────
PrintHeader("NEWS SENTIMENT ANALYSIS");
var news = new NewsAnalysis(symbol);
var newsResult = await news.ComputeAllAsync();

Console.WriteLine("\n  Metrics:");
PrintMetrics(newsResult.Metrics);

if (newsResult.Headlines.Count > 0)
{
    Console.WriteLine("\n  Recent Headlines:");
    foreach (var h in newsResult.Headlines.Take(10))
    {
        Console.ForegroundColor = h.Sentiment == "Positive" ? ConsoleColor.Green
            : h.Sentiment == "Negative" ? ConsoleColor.Red
            : ConsoleColor.Yellow;
        Console.Write($"  [{h.SentimentScore:+0.000;-0.000}]");
        Console.ResetColor();
        string title = h.Title.Length > 80 ? h.Title[..80] : h.Title;
        Console.WriteLine($" {title}");
        if (!string.IsNullOrEmpty(h.Source))
            Console.WriteLine($"          — {h.Source}  {h.Date}");
    }
}

PrintOverall(newsResult.Signals, "News Sentiment");

// ── Final Decision ───────────────────────────────────────────────────────────
PrintHeader("FINAL DECISION");
var engine = new DecisionEngine(taResult, faResult, newsResult);
var decision = engine.Decide();

Console.WriteLine("\n  Component Scores:");
foreach (var (k, v) in decision.Scores)
    Console.WriteLine($"    {k,-24} {v:+0.00;-0.00}");

Console.WriteLine();
Console.WriteLine(sep);
Console.ForegroundColor = decision.Recommendation.Contains("BUY") ? ConsoleColor.Green
    : decision.Recommendation.Contains("SELL") ? ConsoleColor.Red : ConsoleColor.Yellow;
Console.Write($"  RECOMMENDATION:  {decision.Recommendation}");
Console.ResetColor();
Console.WriteLine($"  (Confidence: {decision.Confidence})");
Console.WriteLine(sep);

Console.WriteLine("\n  Explanation:");
foreach (var line in decision.Explanation.Split(Environment.NewLine))
    Console.WriteLine($"    {line}");

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n  Disclaimer: This is for educational purposes only. Not financial advice.");
Console.ResetColor();
Console.WriteLine();

// ── Display helpers ──────────────────────────────────────────────────────────

void PrintHeader(string title)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n{sep}");
    Console.WriteLine($"  {title}");
    Console.WriteLine(sep);
    Console.ResetColor();
}

void PrintMetrics(Dictionary<string, object> metrics)
{
    foreach (var (k, v) in metrics)
    {
        if (k.StartsWith("_")) continue;
        Console.WriteLine($"  {k,-28} {v}");
    }
}

void PrintSignals(Dictionary<string, Signal> signals)
{
    foreach (var (k, signal) in signals)
    {
        if (k.StartsWith("_")) continue;
        Console.Write($"  {k,-20} [");
        Console.ForegroundColor = signal.Direction == "Bullish" ? ConsoleColor.Green
            : signal.Direction == "Bearish" ? ConsoleColor.Red : ConsoleColor.Yellow;
        Console.Write($"{signal.Direction,8}");
        Console.ResetColor();
        Console.WriteLine($"]  {signal.Reason}");
    }
}

void PrintOverall(Dictionary<string, Signal> signals, string label)
{
    foreach (var (k, signal) in signals)
    {
        if (!k.EndsWith("_Overall")) continue;
        Console.Write($"\n  >> {label} Overall: [");
        Console.ForegroundColor = signal.Direction == "Bullish" ? ConsoleColor.Green
            : signal.Direction == "Bearish" ? ConsoleColor.Red : ConsoleColor.Yellow;
        Console.Write(signal.Direction);
        Console.ResetColor();
        Console.WriteLine($"]  {signal.Reason}");
    }
}
