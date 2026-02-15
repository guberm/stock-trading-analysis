"""Exchange Registry — maps user-friendly exchange codes to Yahoo Finance suffixes."""

from dataclasses import dataclass


@dataclass(frozen=True)
class ExchangeInfo:
    code: str            # User-friendly code, e.g. "TLV"
    name: str            # Full name, e.g. "Tel Aviv Stock Exchange"
    suffix: str          # Yahoo Finance suffix, e.g. ".TA"
    country: str         # Country name
    currency: str        # ISO currency code, e.g. "ILS"
    currency_symbol: str # Display symbol, e.g. "₪"


@dataclass
class TickerInfo:
    base_ticker: str              # e.g. "TEVA" (for news searches)
    yahoo_symbol: str             # e.g. "TEVA.TA" (for yfinance API)
    exchange: ExchangeInfo | None # None for bare US tickers


# ── Exchange map ──────────────────────────────────────────────────────────────

EXCHANGES: dict[str, ExchangeInfo] = {
    # Americas
    "NYSE":   ExchangeInfo("NYSE",   "New York Stock Exchange",     "",    "United States", "USD", "$"),
    "NASDAQ": ExchangeInfo("NASDAQ", "NASDAQ",                      "",    "United States", "USD", "$"),
    "AMEX":   ExchangeInfo("AMEX",   "NYSE American",               "",    "United States", "USD", "$"),
    "TSX":    ExchangeInfo("TSX",    "Toronto Stock Exchange",       ".TO", "Canada",        "CAD", "CA$"),
    "TSXV":   ExchangeInfo("TSXV",   "TSX Venture Exchange",        ".V",  "Canada",        "CAD", "CA$"),
    "BCBA":   ExchangeInfo("BCBA",   "Buenos Aires Stock Exchange", ".BA", "Argentina",     "ARS", "AR$"),
    "BVSP":   ExchangeInfo("BVSP",   "B3 - Brasil Bolsa Balcão",   ".SA", "Brazil",        "BRL", "R$"),
    "BMV":    ExchangeInfo("BMV",    "Bolsa Mexicana de Valores",   ".MX", "Mexico",        "MXN", "MX$"),

    # Europe
    "LSE":   ExchangeInfo("LSE",   "London Stock Exchange",     ".L",  "United Kingdom", "GBp", "£"),
    "IOB":   ExchangeInfo("IOB",   "London Intl Order Book",    ".IL", "United Kingdom", "USD", "$"),
    "EPA":   ExchangeInfo("EPA",   "Euronext Paris",            ".PA", "France",         "EUR", "€"),
    "FRA":   ExchangeInfo("FRA",   "Frankfurt Stock Exchange",  ".F",  "Germany",        "EUR", "€"),
    "XETRA": ExchangeInfo("XETRA", "XETRA",                    ".DE", "Germany",        "EUR", "€"),
    "MUN":   ExchangeInfo("MUN",   "Munich Stock Exchange",     ".MU", "Germany",        "EUR", "€"),
    "STU":   ExchangeInfo("STU",   "Stuttgart Stock Exchange",  ".SG", "Germany",        "EUR", "€"),
    "HAM":   ExchangeInfo("HAM",   "Hamburg Stock Exchange",    ".HM", "Germany",        "EUR", "€"),
    "BER":   ExchangeInfo("BER",   "Berlin Stock Exchange",     ".BE", "Germany",        "EUR", "€"),
    "DUS":   ExchangeInfo("DUS",   "Düsseldorf Stock Exchange", ".DU", "Germany",        "EUR", "€"),
    "AMS":   ExchangeInfo("AMS",   "Euronext Amsterdam",        ".AS", "Netherlands",    "EUR", "€"),
    "BIT":   ExchangeInfo("BIT",   "Borsa Italiana",            ".MI", "Italy",          "EUR", "€"),
    "BME":   ExchangeInfo("BME",   "Bolsa de Madrid",           ".MC", "Spain",          "EUR", "€"),
    "SWX":   ExchangeInfo("SWX",   "SIX Swiss Exchange",        ".SW", "Switzerland",    "CHF", "CHF"),
    "VTX":   ExchangeInfo("VTX",   "SIX Swiss (Virt-X)",        ".VX", "Switzerland",    "CHF", "CHF"),
    "STO":   ExchangeInfo("STO",   "Stockholm Stock Exchange",  ".ST", "Sweden",         "SEK", "kr"),
    "CPH":   ExchangeInfo("CPH",   "Copenhagen Stock Exchange", ".CO", "Denmark",        "DKK", "kr"),
    "OSL":   ExchangeInfo("OSL",   "Oslo Stock Exchange",       ".OL", "Norway",         "NOK", "kr"),
    "HEL":   ExchangeInfo("HEL",   "Helsinki Stock Exchange",   ".HE", "Finland",        "EUR", "€"),
    "VIE":   ExchangeInfo("VIE",   "Vienna Stock Exchange",     ".VI", "Austria",        "EUR", "€"),
    "EBR":   ExchangeInfo("EBR",   "Euronext Brussels",         ".BR", "Belgium",        "EUR", "€"),
    "ELI":   ExchangeInfo("ELI",   "Euronext Lisbon",           ".LS", "Portugal",       "EUR", "€"),
    "ATH":   ExchangeInfo("ATH",   "Athens Stock Exchange",     ".AT", "Greece",         "EUR", "€"),
    "IST":   ExchangeInfo("IST",   "Borsa Istanbul",            ".IS", "Turkey",         "TRY", "₺"),
    "WSE":   ExchangeInfo("WSE",   "Warsaw Stock Exchange",     ".WA", "Poland",         "PLN", "zł"),
    "PSE":   ExchangeInfo("PSE",   "Prague Stock Exchange",     ".PR", "Czech Republic", "CZK", "Kč"),
    "BUD":   ExchangeInfo("BUD",   "Budapest Stock Exchange",   ".BD", "Hungary",        "HUF", "Ft"),
    "MCX":   ExchangeInfo("MCX",   "Moscow Exchange",           ".ME", "Russia",         "RUB", "₽"),

    # Asia-Pacific
    "TSE":    ExchangeInfo("TSE",    "Tokyo Stock Exchange",       ".T",   "Japan",      "JPY", "¥"),
    "HKEX":   ExchangeInfo("HKEX",   "Hong Kong Stock Exchange",   ".HK",  "Hong Kong",  "HKD", "HK$"),
    "SSE":    ExchangeInfo("SSE",    "Shanghai Stock Exchange",    ".SS",  "China",       "CNY", "¥"),
    "SZSE":   ExchangeInfo("SZSE",   "Shenzhen Stock Exchange",   ".SZ",  "China",       "CNY", "¥"),
    "KRX":    ExchangeInfo("KRX",    "Korea Exchange",            ".KS",  "South Korea", "KRW", "₩"),
    "KOSDAQ": ExchangeInfo("KOSDAQ", "KOSDAQ",                    ".KQ",  "South Korea", "KRW", "₩"),
    "ASX":    ExchangeInfo("ASX",    "Australian Securities Exch", ".AX", "Australia",   "AUD", "A$"),
    "NZX":    ExchangeInfo("NZX",    "New Zealand Exchange",       ".NZ", "New Zealand",  "NZD", "NZ$"),
    "SGX":    ExchangeInfo("SGX",    "Singapore Exchange",         ".SI", "Singapore",    "SGD", "S$"),
    "KLSE":   ExchangeInfo("KLSE",   "Bursa Malaysia",            ".KL",  "Malaysia",    "MYR", "RM"),
    "IDX":    ExchangeInfo("IDX",    "Indonesia Stock Exchange",   ".JK", "Indonesia",    "IDR", "Rp"),
    "SET":    ExchangeInfo("SET",    "Stock Exch of Thailand",     ".BK", "Thailand",     "THB", "฿"),
    "TWSE":   ExchangeInfo("TWSE",   "Taiwan Stock Exchange",     ".TW",  "Taiwan",      "TWD", "NT$"),
    "TPEX":   ExchangeInfo("TPEX",   "Taipei Exchange",           ".TWO", "Taiwan",      "TWD", "NT$"),
    "PSEi":   ExchangeInfo("PSEi",   "Philippine Stock Exchange", ".PS",  "Philippines", "PHP", "₱"),

    # Middle East & South Asia
    "TLV":  ExchangeInfo("TLV",  "Tel Aviv Stock Exchange",     ".TA", "Israel",      "ILS", "₪"),
    "BSE":  ExchangeInfo("BSE",  "Bombay Stock Exchange",       ".BO", "India",       "INR", "₹"),
    "NSE":  ExchangeInfo("NSE",  "National Stock Exch India",   ".NS", "India",       "INR", "₹"),
    "IREX": ExchangeInfo("IREX", "Iran Fara Bourse",            ".IR", "Iran",        "IRR", "IRR"),
    "TADAWUL": ExchangeInfo("TADAWUL", "Saudi Stock Exchange",  ".SR", "Saudi Arabia","SAR", "﷼"),
    "QSE":  ExchangeInfo("QSE",  "Qatar Stock Exchange",        ".QA", "Qatar",       "QAR", "QR"),

    # Africa
    "JSE": ExchangeInfo("JSE", "Johannesburg Stock Exchange", ".JO", "South Africa", "ZAR", "R"),
}

# Reverse lookup: Yahoo suffix -> ExchangeInfo
_SUFFIX_MAP: dict[str, ExchangeInfo] = {
    info.suffix: info for info in EXCHANGES.values() if info.suffix
}

# Set of known suffixes for detection (including the dot)
_KNOWN_SUFFIXES: set[str] = set(_SUFFIX_MAP.keys())


def resolve_ticker(raw_ticker: str, exchange_code: str | None = None) -> TickerInfo:
    """
    Resolve a raw ticker input into a TickerInfo.

    Handles:
      TEVA.TA              → detect known suffix → base=TEVA, yahoo=TEVA.TA
      TEVA --exchange TLV  → lookup suffix       → base=TEVA, yahoo=TEVA.TA
      NYSE:TEVA            → strip prefix         → base=TEVA, yahoo=TEVA
      TEVA                 → bare                 → base=TEVA, yahoo=TEVA
    """
    raw = raw_ticker.strip()

    # Strip exchange prefix (legacy: NYSE:MSFT)
    if ":" in raw:
        raw = raw.split(":")[-1].strip()

    raw = raw.upper()

    # If --exchange flag is provided, it takes priority
    if exchange_code:
        code = exchange_code.upper()
        if code not in EXCHANGES:
            raise ValueError(
                f"Unknown exchange '{code}'. Use --list-exchanges to see all supported exchanges."
            )
        info = EXCHANGES[code]
        # Strip any existing suffix from the ticker before applying the new one
        base = _strip_known_suffix(raw)
        yahoo = base + info.suffix
        return TickerInfo(base_ticker=base, yahoo_symbol=yahoo, exchange=info)

    # Check if the ticker already has a known Yahoo suffix (e.g. TEVA.TA)
    dot_pos = raw.rfind(".")
    if dot_pos > 0:
        candidate_suffix = raw[dot_pos:]
        if candidate_suffix in _KNOWN_SUFFIXES:
            base = raw[:dot_pos]
            info = _SUFFIX_MAP[candidate_suffix]
            return TickerInfo(base_ticker=base, yahoo_symbol=raw, exchange=info)

    # Bare ticker — assume US market
    return TickerInfo(base_ticker=raw, yahoo_symbol=raw, exchange=None)


def _strip_known_suffix(ticker: str) -> str:
    """Remove a known exchange suffix from a ticker if present."""
    dot_pos = ticker.rfind(".")
    if dot_pos > 0 and ticker[dot_pos:] in _KNOWN_SUFFIXES:
        return ticker[:dot_pos]
    return ticker


def list_exchanges() -> str:
    """Return a formatted table of all supported exchanges."""
    lines = [
        "Supported Stock Exchanges",
        "=" * 80,
        f"  {'Code':<10} {'Exchange':<32} {'Suffix':<8} {'Country':<16} {'Currency'}",
        "-" * 80,
    ]

    regions = {
        "Americas": ["NYSE", "NASDAQ", "AMEX", "TSX", "TSXV", "BCBA", "BVSP", "BMV"],
        "Europe": [
            "LSE", "IOB", "EPA", "FRA", "XETRA", "MUN", "STU", "HAM", "BER", "DUS",
            "AMS", "BIT", "BME", "SWX", "VTX", "STO", "CPH", "OSL", "HEL",
            "VIE", "EBR", "ELI", "ATH", "IST", "WSE", "PSE", "BUD", "MCX",
        ],
        "Asia-Pacific": [
            "TSE", "HKEX", "SSE", "SZSE", "KRX", "KOSDAQ", "ASX", "NZX", "SGX",
            "KLSE", "IDX", "SET", "TWSE", "TPEX", "PSEi",
        ],
        "Middle East & South Asia": ["TLV", "BSE", "NSE", "IREX", "TADAWUL", "QSE"],
        "Africa": ["JSE"],
    }

    for region, codes in regions.items():
        lines.append(f"\n  {region}:")
        for code in codes:
            info = EXCHANGES[code]
            suffix = info.suffix or "(none)"
            lines.append(
                f"  {info.code:<10} {info.name:<32} {suffix:<8} {info.country:<16} {info.currency_symbol} {info.currency}"
            )

    lines.append("")
    lines.append("Usage:  python main.py TEVA.TA")
    lines.append("        python main.py TEVA --exchange TLV")
    lines.append("        python main.py 7203 --exchange TSE")
    return "\n".join(lines)
