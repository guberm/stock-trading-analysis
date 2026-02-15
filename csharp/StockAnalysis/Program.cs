using StockAnalysis;
using static StockAnalysis.ExchangeRegistry;

// ── Parse arguments ──────────────────────────────────────────────────────────
string? rawTicker = null;
string period = "1y";
string? exchangeCode = null;
bool listExchanges = false;
bool useLlm = false;
string? llmModel = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--period" && i + 1 < args.Length)
        period = args[++i];
    else if (args[i] is "--exchange" or "-e" && i + 1 < args.Length)
        exchangeCode = args[++i];
    else if (args[i] == "--list-exchanges")
        listExchanges = true;
    else if (args[i] == "--llm")
        useLlm = true;
    else if (args[i] == "--model" && i + 1 < args.Length)
        llmModel = args[++i];
    else if (rawTicker == null)
        rawTicker = args[i];
}

if (listExchanges)
{
    Console.WriteLine(ExchangeRegistry.ListExchanges());
    return;
}

if (rawTicker == null)
{
    Console.WriteLine("Usage: StockAnalysis <TICKER> [--exchange TLV] [--period 1y] [--llm] [--model MODEL]");
    Console.WriteLine("       StockAnalysis --list-exchanges");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  StockAnalysis MSFT");
    Console.WriteLine("  StockAnalysis TEVA.TA");
    Console.WriteLine("  StockAnalysis TEVA --exchange TLV");
    Console.WriteLine("  StockAnalysis 7203 --exchange TSE");
    Console.WriteLine("  StockAnalysis AAPL --llm");
    Console.WriteLine("  StockAnalysis AAPL --llm --model claude-sonnet-4-5-20250929");
    return;
}

TickerInfo tickerInfo;
try
{
    tickerInfo = ExchangeRegistry.ResolveTicker(rawTicker, exchangeCode);
}
catch (ArgumentException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {ex.Message}");
    Console.ResetColor();
    return;
}

string symbol = tickerInfo.YahooSymbol;
string baseTicker = tickerInfo.BaseTicker;
string currencySym = tickerInfo.Exchange?.CurrencySymbol ?? "$";
string currencyCode = tickerInfo.Exchange?.Currency ?? "USD";
string exchangeLabel = tickerInfo.Exchange != null ? $" ({tickerInfo.Exchange.Name})" : "";

var sep = new string('=', 72);
var thinSep = new string('-', 72);

Console.WriteLine();
Console.WriteLine("Stock Trading Decision System");
Console.WriteLine($"Analyzing: {symbol}{exchangeLabel}");
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

// Use Yahoo Finance's reported currency as fallback
if (fundamentals.GetValueOrDefault("currency") is string yfCurrency && !string.IsNullOrEmpty(yfCurrency))
    currencyCode = yfCurrency;

string companyName = fundamentals.GetValueOrDefault("shortName") as string ?? symbol;
double currentPrice = history[^1].Close;

Console.WriteLine($"  Company : {companyName}");
Console.WriteLine($"  Price   : {currencySym}{currentPrice:F2} {currencyCode}");
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
var fa = new FundamentalAnalysis(fundamentals, currencySym);
var faResult = fa.ComputeAll();

Console.WriteLine("\n  Metrics:");
PrintMetrics(faResult.Metrics);
Console.WriteLine("\n  Signals:");
PrintSignals(faResult.Signals);
PrintOverall(faResult.Signals, "Fundamental");

// ── News Sentiment ───────────────────────────────────────────────────────────
PrintHeader("NEWS SENTIMENT ANALYSIS");
var news = new NewsAnalysis(baseTicker);
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

// ── LLM Analysis (optional) ─────────────────────────────────────────────────
if (useLlm || llmModel != null)
{
    PrintHeader("LLM ANALYSIS");
    Console.WriteLine("\n  Querying LLM...");

    try
    {
        var llmResult = await LlmAnalysis.RunAsync(
            taResult, faResult, newsResult, decision,
            symbol, companyName, currencyCode, llmModel);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  Model: {llmResult.Model} ({llmResult.Provider})");
        Console.ResetColor();
        Console.WriteLine(thinSep);
        foreach (var line in llmResult.Response.Split('\n'))
            Console.WriteLine($"  {line}");
        Console.WriteLine();
        Console.WriteLine(thinSep);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n  LLM analysis failed: {ex.Message}");
        Console.ResetColor();
    }
}

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
