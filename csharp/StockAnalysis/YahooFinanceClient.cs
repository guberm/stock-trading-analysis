using Newtonsoft.Json.Linq;

namespace StockAnalysis;

/// <summary>
/// Fetches historical price data and fundamental info from Yahoo Finance API.
/// </summary>
public class YahooFinanceClient
{
    private readonly HttpClient _http;

    public YahooFinanceClient()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    }

    public async Task<List<StockBar>> GetHistoryAsync(string symbol, string range = "1y", string interval = "1d")
    {
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?range={range}&interval={interval}";
        var resp = await _http.GetStringAsync(url);
        var json = JObject.Parse(resp);
        var result = json["chart"]?["result"]?[0];
        if (result == null) return new List<StockBar>();

        var timestamps = result["timestamp"]?.ToObject<long[]>() ?? Array.Empty<long>();
        var quote = result["indicators"]?["quote"]?[0];
        var opens = quote?["open"]?.ToObject<double?[]>() ?? Array.Empty<double?>();
        var highs = quote?["high"]?.ToObject<double?[]>() ?? Array.Empty<double?>();
        var lows = quote?["low"]?.ToObject<double?[]>() ?? Array.Empty<double?>();
        var closes = quote?["close"]?.ToObject<double?[]>() ?? Array.Empty<double?>();
        var volumes = quote?["volume"]?.ToObject<long?[]>() ?? Array.Empty<long?>();

        var bars = new List<StockBar>();
        for (int i = 0; i < timestamps.Length; i++)
        {
            if (closes[i] == null) continue;
            bars.Add(new StockBar(
                DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).DateTime,
                opens[i] ?? 0, highs[i] ?? 0, lows[i] ?? 0, closes[i] ?? 0, volumes[i] ?? 0
            ));
        }
        return bars;
    }

    public async Task<Dictionary<string, object?>> GetFundamentalsAsync(string symbol)
    {
        var url = $"https://query1.finance.yahoo.com/v10/finance/quoteSummary/{symbol}"
            + "?modules=defaultKeyStatistics,financialData,summaryDetail,earningsTrend,summaryProfile";
        var resp = await _http.GetStringAsync(url);
        var json = JObject.Parse(resp);
        var result = json["quoteSummary"]?["result"]?[0];

        var dict = new Dictionary<string, object?>();
        if (result == null) return dict;

        var stats = result["defaultKeyStatistics"];
        var fin = result["financialData"];
        var summary = result["summaryDetail"];
        var profile = result["summaryProfile"];

        dict["shortName"] = profile?["longName"]?.Value<string>() ?? symbol;
        dict["trailingPE"] = Raw(summary?["trailingPE"]);
        dict["forwardPE"] = Raw(summary?["forwardPE"]);
        dict["pegRatio"] = Raw(stats?["pegRatio"]);
        dict["priceToBook"] = Raw(stats?["priceToBook"]);
        dict["priceToSalesTrailing12Months"] = Raw(summary?["priceToSalesTrailing12Months"]);
        dict["enterpriseToEbitda"] = Raw(stats?["enterpriseToEbitda"]);
        dict["profitMargins"] = Raw(stats?["profitMargins"]);
        dict["operatingMargins"] = Raw(fin?["operatingMargins"]);
        dict["returnOnEquity"] = Raw(fin?["returnOnEquity"]);
        dict["returnOnAssets"] = Raw(fin?["returnOnAssets"]);
        dict["revenueGrowth"] = Raw(fin?["revenueGrowth"]);
        dict["earningsGrowth"] = Raw(fin?["earningsGrowth"]);
        dict["debtToEquity"] = Raw(fin?["debtToEquity"]);
        dict["currentRatio"] = Raw(fin?["currentRatio"]);
        dict["quickRatio"] = Raw(fin?["quickRatio"]);
        dict["freeCashflow"] = Raw(fin?["freeCashflow"]);
        dict["totalCash"] = Raw(fin?["totalCash"]);
        dict["totalDebt"] = Raw(fin?["totalDebt"]);
        dict["dividendYield"] = Raw(summary?["dividendYield"]);
        dict["payoutRatio"] = Raw(summary?["payoutRatio"]);
        dict["targetMeanPrice"] = Raw(fin?["targetMeanPrice"]);
        dict["currentPrice"] = Raw(fin?["currentPrice"]);
        dict["recommendationKey"] = fin?["recommendationKey"]?.Value<string>();
        dict["numberOfAnalystOpinions"] = Raw(fin?["numberOfAnalystOpinions"]);

        return dict;
    }

    private static double? Raw(JToken? token)
    {
        if (token == null) return null;
        var raw = token["raw"];
        return raw?.Value<double>();
    }
}
