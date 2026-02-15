using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StockAnalysis;

/// <summary>
/// Fetches recent news headlines and performs VADER-style sentiment analysis.
/// </summary>
public class NewsAnalysis
{
    private readonly string _symbol;
    private readonly HttpClient _http;
    public List<NewsHeadline> Headlines { get; } = new();
    public Dictionary<string, object> Metrics { get; } = new();
    public Dictionary<string, Signal> Signals { get; } = new();

    // Simplified VADER-inspired word lists
    private static readonly Dictionary<string, double> PositiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["surge"] = 0.6, ["surges"] = 0.6, ["soar"] = 0.7, ["soars"] = 0.7, ["rally"] = 0.5,
        ["gain"] = 0.4, ["gains"] = 0.4, ["rise"] = 0.3, ["rises"] = 0.3, ["rising"] = 0.3,
        ["up"] = 0.2, ["high"] = 0.3, ["higher"] = 0.3, ["bull"] = 0.5, ["bullish"] = 0.6,
        ["buy"] = 0.4, ["upgrade"] = 0.5, ["upgrades"] = 0.5, ["outperform"] = 0.5,
        ["beat"] = 0.5, ["beats"] = 0.5, ["strong"] = 0.4, ["positive"] = 0.4,
        ["profit"] = 0.4, ["profits"] = 0.4, ["growth"] = 0.4, ["record"] = 0.3,
        ["boost"] = 0.4, ["boosts"] = 0.4, ["optimistic"] = 0.5, ["recovery"] = 0.4,
        ["breakthrough"] = 0.6, ["best"] = 0.4, ["top"] = 0.2, ["win"] = 0.4,
        ["exceeds"] = 0.5, ["exceeded"] = 0.5, ["above"] = 0.2, ["jumps"] = 0.5,
        ["jump"] = 0.5, ["improve"] = 0.3, ["improved"] = 0.3, ["innovation"] = 0.4,
    };

    private static readonly Dictionary<string, double> NegativeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["crash"] = -0.8, ["crashes"] = -0.8, ["plunge"] = -0.7, ["plunges"] = -0.7,
        ["drop"] = -0.4, ["drops"] = -0.4, ["fall"] = -0.4, ["falls"] = -0.4, ["falling"] = -0.4,
        ["decline"] = -0.5, ["declines"] = -0.5, ["down"] = -0.3, ["low"] = -0.3, ["lower"] = -0.3,
        ["bear"] = -0.5, ["bearish"] = -0.6, ["sell"] = -0.4, ["downgrade"] = -0.5,
        ["downgrades"] = -0.5, ["underperform"] = -0.5, ["miss"] = -0.5, ["misses"] = -0.5,
        ["weak"] = -0.4, ["negative"] = -0.4, ["loss"] = -0.5, ["losses"] = -0.5,
        ["risk"] = -0.3, ["risks"] = -0.3, ["fear"] = -0.5, ["fears"] = -0.5,
        ["concern"] = -0.3, ["concerns"] = -0.3, ["warning"] = -0.5, ["warns"] = -0.5,
        ["worst"] = -0.6, ["crisis"] = -0.7, ["recession"] = -0.6, ["layoff"] = -0.5,
        ["layoffs"] = -0.5, ["cut"] = -0.3, ["cuts"] = -0.3, ["below"] = -0.2,
        ["slump"] = -0.5, ["slumps"] = -0.5, ["tumble"] = -0.5, ["tumbles"] = -0.5,
        ["fraud"] = -0.8, ["scandal"] = -0.7, ["lawsuit"] = -0.4, ["investigate"] = -0.4,
    };

    public NewsAnalysis(string symbol)
    {
        _symbol = symbol;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    }

    public async Task<NewsResult> ComputeAllAsync()
    {
        await FetchHeadlines();
        ScoreSentiment();
        AggregateSignal();
        return new NewsResult { Headlines = Headlines, Metrics = Metrics, Signals = Signals };
    }

    private async Task FetchHeadlines()
    {
        try { await FetchGoogleNews(); } catch { /* continue */ }

        // Deduplicate
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<NewsHeadline>();
        foreach (var h in Headlines)
        {
            if (seen.Add(h.Title.Trim()))
                unique.Add(h);
        }
        Headlines.Clear();
        Headlines.AddRange(unique.Take(20));
    }

    private async Task FetchGoogleNews()
    {
        var url = $"https://news.google.com/rss/search?q={_symbol}+stock&hl=en-US&gl=US&ceid=US:en";
        var resp = await _http.GetStringAsync(url);
        var doc = XDocument.Parse(resp);

        foreach (var item in doc.Descendants("item").Take(15))
        {
            Headlines.Add(new NewsHeadline
            {
                Title = item.Element("title")?.Value?.Trim() ?? "",
                Source = item.Element("source")?.Value?.Trim() ?? "Google News",
                Date = item.Element("pubDate")?.Value?.Trim() ?? "",
            });
        }
    }

    private void ScoreSentiment()
    {
        if (Headlines.Count == 0)
        {
            Metrics["News Count"] = 0;
            Metrics["Avg Sentiment"] = "N/A";
            return;
        }

        var scores = new List<double>();
        foreach (var h in Headlines)
        {
            double score = AnalyzeSentiment(h.Title);
            h.SentimentScore = Math.Round(score, 3);
            h.Sentiment = score > 0.05 ? "Positive" : score < -0.05 ? "Negative" : "Neutral";
            scores.Add(score);
        }

        double avg = scores.Average();
        int positive = scores.Count(s => s > 0.05);
        int negative = scores.Count(s => s < -0.05);

        Metrics["News Count"] = scores.Count;
        Metrics["Avg Sentiment"] = Math.Round(avg, 3);
        Metrics["Positive Headlines"] = positive;
        Metrics["Negative Headlines"] = negative;
        Metrics["Neutral Headlines"] = scores.Count - positive - negative;
    }

    private static double AnalyzeSentiment(string text)
    {
        var words = Regex.Split(text.ToLower(), @"[^a-z']+").Where(w => w.Length > 0);
        double total = 0;
        int count = 0;
        foreach (var word in words)
        {
            if (PositiveWords.TryGetValue(word, out double pos))
            {
                total += pos;
                count++;
            }
            else if (NegativeWords.TryGetValue(word, out double neg))
            {
                total += neg;
                count++;
            }
        }
        return count > 0 ? Math.Clamp(total / count, -1.0, 1.0) : 0.0;
    }

    private void AggregateSignal()
    {
        if (Metrics.GetValueOrDefault("Avg Sentiment") is not double avg)
        {
            Signals["_News_Overall"] = new Signal("Neutral", "No news data available");
            return;
        }

        int pos = Metrics.GetValueOrDefault("Positive Headlines") is int p ? p : 0;
        int neg = Metrics.GetValueOrDefault("Negative Headlines") is int n ? n : 0;
        int total = Metrics.GetValueOrDefault("News Count") is int t ? t : 0;

        Signals["_News_Overall"] = avg > 0.15 && pos > neg
            ? new Signal("Bullish", $"Avg sentiment {avg:F3}, {pos}/{total} positive")
            : avg < -0.15 && neg > pos
                ? new Signal("Bearish", $"Avg sentiment {avg:F3}, {neg}/{total} negative")
                : avg > 0.05
                    ? new Signal("Bullish", $"Slightly positive sentiment {avg:F3}")
                    : avg < -0.05
                        ? new Signal("Bearish", $"Slightly negative sentiment {avg:F3}")
                        : new Signal("Neutral", $"Mixed sentiment {avg:F3}");
    }
}
