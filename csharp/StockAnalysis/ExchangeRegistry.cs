namespace StockAnalysis;

/// <summary>
/// Maps user-friendly exchange codes to Yahoo Finance suffixes.
/// </summary>
public static class ExchangeRegistry
{
    public record ExchangeInfo(
        string Code,
        string Name,
        string Suffix,
        string Country,
        string Currency,
        string CurrencySymbol
    );

    public record TickerInfo(
        string BaseTicker,
        string YahooSymbol,
        ExchangeInfo? Exchange
    );

    private static readonly Dictionary<string, ExchangeInfo> Exchanges =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // Americas
        ["NYSE"]   = new("NYSE",   "New York Stock Exchange",     "",    "United States", "USD", "$"),
        ["NASDAQ"] = new("NASDAQ", "NASDAQ",                      "",    "United States", "USD", "$"),
        ["AMEX"]   = new("AMEX",   "NYSE American",               "",    "United States", "USD", "$"),
        ["TSX"]    = new("TSX",    "Toronto Stock Exchange",       ".TO", "Canada",        "CAD", "CA$"),
        ["TSXV"]   = new("TSXV",   "TSX Venture Exchange",        ".V",  "Canada",        "CAD", "CA$"),
        ["BCBA"]   = new("BCBA",   "Buenos Aires Stock Exchange", ".BA", "Argentina",     "ARS", "AR$"),
        ["BVSP"]   = new("BVSP",   "B3 - Brasil Bolsa Balcão",   ".SA", "Brazil",        "BRL", "R$"),
        ["BMV"]    = new("BMV",    "Bolsa Mexicana de Valores",   ".MX", "Mexico",        "MXN", "MX$"),

        // Europe
        ["LSE"]   = new("LSE",   "London Stock Exchange",     ".L",  "United Kingdom", "GBp", "£"),
        ["IOB"]   = new("IOB",   "London Intl Order Book",    ".IL", "United Kingdom", "USD", "$"),
        ["EPA"]   = new("EPA",   "Euronext Paris",            ".PA", "France",         "EUR", "€"),
        ["FRA"]   = new("FRA",   "Frankfurt Stock Exchange",  ".F",  "Germany",        "EUR", "€"),
        ["XETRA"] = new("XETRA", "XETRA",                    ".DE", "Germany",        "EUR", "€"),
        ["MUN"]   = new("MUN",   "Munich Stock Exchange",     ".MU", "Germany",        "EUR", "€"),
        ["STU"]   = new("STU",   "Stuttgart Stock Exchange",  ".SG", "Germany",        "EUR", "€"),
        ["HAM"]   = new("HAM",   "Hamburg Stock Exchange",    ".HM", "Germany",        "EUR", "€"),
        ["BER"]   = new("BER",   "Berlin Stock Exchange",     ".BE", "Germany",        "EUR", "€"),
        ["DUS"]   = new("DUS",   "Düsseldorf Stock Exchange", ".DU", "Germany",        "EUR", "€"),
        ["AMS"]   = new("AMS",   "Euronext Amsterdam",        ".AS", "Netherlands",    "EUR", "€"),
        ["BIT"]   = new("BIT",   "Borsa Italiana",            ".MI", "Italy",          "EUR", "€"),
        ["BME"]   = new("BME",   "Bolsa de Madrid",           ".MC", "Spain",          "EUR", "€"),
        ["SWX"]   = new("SWX",   "SIX Swiss Exchange",        ".SW", "Switzerland",    "CHF", "CHF"),
        ["VTX"]   = new("VTX",   "SIX Swiss (Virt-X)",        ".VX", "Switzerland",    "CHF", "CHF"),
        ["STO"]   = new("STO",   "Stockholm Stock Exchange",  ".ST", "Sweden",         "SEK", "kr"),
        ["CPH"]   = new("CPH",   "Copenhagen Stock Exchange", ".CO", "Denmark",        "DKK", "kr"),
        ["OSL"]   = new("OSL",   "Oslo Stock Exchange",       ".OL", "Norway",         "NOK", "kr"),
        ["HEL"]   = new("HEL",   "Helsinki Stock Exchange",   ".HE", "Finland",        "EUR", "€"),
        ["VIE"]   = new("VIE",   "Vienna Stock Exchange",     ".VI", "Austria",        "EUR", "€"),
        ["EBR"]   = new("EBR",   "Euronext Brussels",         ".BR", "Belgium",        "EUR", "€"),
        ["ELI"]   = new("ELI",   "Euronext Lisbon",           ".LS", "Portugal",       "EUR", "€"),
        ["ATH"]   = new("ATH",   "Athens Stock Exchange",     ".AT", "Greece",         "EUR", "€"),
        ["IST"]   = new("IST",   "Borsa Istanbul",            ".IS", "Turkey",         "TRY", "₺"),
        ["WSE"]   = new("WSE",   "Warsaw Stock Exchange",     ".WA", "Poland",         "PLN", "zł"),
        ["PSE"]   = new("PSE",   "Prague Stock Exchange",     ".PR", "Czech Republic", "CZK", "Kč"),
        ["BUD"]   = new("BUD",   "Budapest Stock Exchange",   ".BD", "Hungary",        "HUF", "Ft"),
        ["MCX"]   = new("MCX",   "Moscow Exchange",           ".ME", "Russia",         "RUB", "₽"),

        // Asia-Pacific
        ["TSE"]    = new("TSE",    "Tokyo Stock Exchange",       ".T",   "Japan",       "JPY", "¥"),
        ["HKEX"]   = new("HKEX",   "Hong Kong Stock Exchange",   ".HK",  "Hong Kong",   "HKD", "HK$"),
        ["SSE"]    = new("SSE",    "Shanghai Stock Exchange",    ".SS",  "China",        "CNY", "¥"),
        ["SZSE"]   = new("SZSE",   "Shenzhen Stock Exchange",   ".SZ",  "China",        "CNY", "¥"),
        ["KRX"]    = new("KRX",    "Korea Exchange",            ".KS",  "South Korea",  "KRW", "₩"),
        ["KOSDAQ"] = new("KOSDAQ", "KOSDAQ",                    ".KQ",  "South Korea",  "KRW", "₩"),
        ["ASX"]    = new("ASX",    "Australian Securities Exch", ".AX", "Australia",    "AUD", "A$"),
        ["NZX"]    = new("NZX",    "New Zealand Exchange",       ".NZ", "New Zealand",   "NZD", "NZ$"),
        ["SGX"]    = new("SGX",    "Singapore Exchange",         ".SI", "Singapore",     "SGD", "S$"),
        ["KLSE"]   = new("KLSE",   "Bursa Malaysia",            ".KL",  "Malaysia",     "MYR", "RM"),
        ["IDX"]    = new("IDX",    "Indonesia Stock Exchange",   ".JK", "Indonesia",     "IDR", "Rp"),
        ["SET"]    = new("SET",    "Stock Exch of Thailand",     ".BK", "Thailand",      "THB", "฿"),
        ["TWSE"]   = new("TWSE",   "Taiwan Stock Exchange",     ".TW",  "Taiwan",       "TWD", "NT$"),
        ["TPEX"]   = new("TPEX",   "Taipei Exchange",           ".TWO", "Taiwan",       "TWD", "NT$"),
        ["PSEi"]   = new("PSEi",   "Philippine Stock Exchange", ".PS",  "Philippines",  "PHP", "₱"),

        // Middle East & South Asia
        ["TLV"]     = new("TLV",     "Tel Aviv Stock Exchange",   ".TA", "Israel",       "ILS", "₪"),
        ["BSE"]     = new("BSE",     "Bombay Stock Exchange",     ".BO", "India",        "INR", "₹"),
        ["NSE"]     = new("NSE",     "National Stock Exch India", ".NS", "India",        "INR", "₹"),
        ["IREX"]    = new("IREX",    "Iran Fara Bourse",          ".IR", "Iran",         "IRR", "IRR"),
        ["TADAWUL"] = new("TADAWUL", "Saudi Stock Exchange",      ".SR", "Saudi Arabia", "SAR", "﷼"),
        ["QSE"]     = new("QSE",     "Qatar Stock Exchange",      ".QA", "Qatar",        "QAR", "QR"),

        // Africa
        ["JSE"] = new("JSE", "Johannesburg Stock Exchange", ".JO", "South Africa", "ZAR", "R"),
    };

    private static readonly Dictionary<string, ExchangeInfo> SuffixMap;
    private static readonly HashSet<string> KnownSuffixes;

    static ExchangeRegistry()
    {
        SuffixMap = new(StringComparer.OrdinalIgnoreCase);
        foreach (var info in Exchanges.Values)
        {
            if (!string.IsNullOrEmpty(info.Suffix) && !SuffixMap.ContainsKey(info.Suffix))
                SuffixMap[info.Suffix] = info;
        }
        KnownSuffixes = new HashSet<string>(SuffixMap.Keys, StringComparer.OrdinalIgnoreCase);
    }

    public static TickerInfo ResolveTicker(string rawTicker, string? exchangeCode = null)
    {
        var raw = rawTicker.Trim();

        // Strip exchange prefix (legacy: NYSE:MSFT)
        if (raw.Contains(':'))
            raw = raw.Split(':').Last().Trim();

        raw = raw.ToUpper();

        // --exchange flag takes priority
        if (!string.IsNullOrEmpty(exchangeCode))
        {
            if (!Exchanges.TryGetValue(exchangeCode, out var info))
                throw new ArgumentException(
                    $"Unknown exchange '{exchangeCode}'. Use --list-exchanges to see all supported exchanges.");

            var baseTicker = StripKnownSuffix(raw);
            return new TickerInfo(baseTicker, baseTicker + info.Suffix, info);
        }

        // Check for known Yahoo suffix (e.g. TEVA.TA)
        int dotPos = raw.LastIndexOf('.');
        if (dotPos > 0)
        {
            var candidateSuffix = raw[dotPos..];
            if (SuffixMap.TryGetValue(candidateSuffix, out var info))
                return new TickerInfo(raw[..dotPos], raw, info);
        }

        // Bare ticker — assume US market
        return new TickerInfo(raw, raw, null);
    }

    private static string StripKnownSuffix(string ticker)
    {
        int dotPos = ticker.LastIndexOf('.');
        if (dotPos > 0 && KnownSuffixes.Contains(ticker[dotPos..]))
            return ticker[..dotPos];
        return ticker;
    }

    public static string ListExchanges()
    {
        var lines = new List<string>
        {
            "Supported Stock Exchanges",
            new string('=', 80),
            $"  {"Code",-10} {"Exchange",-32} {"Suffix",-8} {"Country",-16} Currency",
            new string('-', 80),
        };

        var regions = new (string Name, string[] Codes)[]
        {
            ("Americas", new[] { "NYSE", "NASDAQ", "AMEX", "TSX", "TSXV", "BCBA", "BVSP", "BMV" }),
            ("Europe", new[] {
                "LSE", "IOB", "EPA", "FRA", "XETRA", "MUN", "STU", "HAM", "BER", "DUS",
                "AMS", "BIT", "BME", "SWX", "VTX", "STO", "CPH", "OSL", "HEL",
                "VIE", "EBR", "ELI", "ATH", "IST", "WSE", "PSE", "BUD", "MCX"
            }),
            ("Asia-Pacific", new[] {
                "TSE", "HKEX", "SSE", "SZSE", "KRX", "KOSDAQ", "ASX", "NZX", "SGX",
                "KLSE", "IDX", "SET", "TWSE", "TPEX", "PSEi"
            }),
            ("Middle East & South Asia", new[] { "TLV", "BSE", "NSE", "IREX", "TADAWUL", "QSE" }),
            ("Africa", new[] { "JSE" }),
        };

        foreach (var (name, codes) in regions)
        {
            lines.Add($"\n  {name}:");
            foreach (var code in codes)
            {
                if (!Exchanges.TryGetValue(code, out var info)) continue;
                var suffix = string.IsNullOrEmpty(info.Suffix) ? "(none)" : info.Suffix;
                lines.Add($"  {info.Code,-10} {info.Name,-32} {suffix,-8} {info.Country,-16} {info.CurrencySymbol} {info.Currency}");
            }
        }

        lines.Add("");
        lines.Add("Usage:  StockAnalysis TEVA.TA");
        lines.Add("        StockAnalysis TEVA --exchange TLV");
        lines.Add("        StockAnalysis 7203 --exchange TSE");
        return string.Join(Environment.NewLine, lines);
    }
}
