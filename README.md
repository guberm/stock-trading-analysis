# Stock Trading Decision System

A multi-factor stock analysis tool that combines **Technical Analysis**, **Fundamental Analysis**, and **News Sentiment Analysis** to produce a Buy/Hold/Sell recommendation. Implemented in both **Python** and **C#**.

## How It Works

The system takes a stock ticker as input (e.g., `MSFT` or `NYSE:MSFT`) and runs three independent analysis engines:

### 1. Technical Analysis (Weight: 40%)
Computes indicators from historical price/volume data:
- **Trend**: SMA (20/50/200), EMA (12/26), Golden/Death Cross
- **Momentum**: RSI (14), MACD, Stochastic Oscillator, Williams %R, CCI
- **Volatility**: Bollinger Bands, ATR
- **Volume**: On-Balance Volume (OBV), ADX

Each indicator generates a Bullish/Bearish/Neutral signal.

### 2. Fundamental Analysis (Weight: 35%)
Evaluates financial health using Yahoo Finance data:
- **Valuation**: P/E, Forward P/E, PEG, P/B, P/S, EV/EBITDA
- **Profitability**: Profit Margin, Operating Margin, ROE, ROA
- **Growth**: Revenue Growth, Earnings Growth
- **Financial Health**: Debt/Equity, Current Ratio, Quick Ratio, Free Cash Flow
- **Dividends**: Yield, Payout Ratio
- **Analyst Targets**: Price target, upside/downside, consensus recommendation

### 3. News Sentiment Analysis (Weight: 25%)
Scrapes recent headlines and scores sentiment:
- Fetches from Google News RSS and Finviz (Python) / Google News RSS (C#)
- Python uses VADER sentiment analyzer
- C# uses a built-in lexicon-based sentiment scorer
- Computes average sentiment and positive/negative headline ratio

### Final Decision
A weighted scoring engine combines all three analyses:
```
Score = (TA × 0.40) + (FA × 0.35) + (News × 0.25)
```

| Score Range | Recommendation |
|-------------|---------------|
| >= +0.40    | STRONG BUY    |
| >= +0.15    | BUY           |
| -0.15 to +0.15 | HOLD      |
| <= -0.15    | SELL          |
| <= -0.40    | STRONG SELL   |

## Python Version

### Prerequisites
- Python 3.10+
- pip

### Setup & Run
```bash
cd python
pip install -r requirements.txt
python main.py MSFT
python main.py NYSE:MSFT --period 6mo
```

### Dependencies
- `yfinance` - market data and fundamentals
- `pandas` / `numpy` - data processing
- `ta` - technical indicator library
- `vaderSentiment` - sentiment analysis
- `requests` / `beautifulsoup4` - news scraping
- `colorama` - colored terminal output

## C# Version

### Prerequisites
- .NET 8.0 SDK

### Setup & Run
```bash
cd csharp/StockAnalysis
dotnet restore
dotnet run -- MSFT
dotnet run -- NYSE:MSFT --period 6mo
```

### Dependencies
- `Newtonsoft.Json` - JSON parsing for Yahoo Finance API
- All other functionality uses built-in .NET libraries

## Example Output

```
Stock Trading Decision System
Analyzing: MSFT
========================================================================

Fetching market data...
  Company : Microsoft Corporation
  Price   : $420.55
  Period  : 1y (252 trading days)

========================================================================
  TECHNICAL ANALYSIS
========================================================================
  Indicators:
  SMA_20                       418.23
  RSI_14                       55.30
  MACD_Line                    2.3456
  ...

  Signals:
  MA_Cross             [ Bullish]  Golden Cross (SMA50 > SMA200)
  RSI                  [ Neutral]  RSI 55.30 – neutral zone
  ...

========================================================================
  FINAL DECISION
========================================================================
  RECOMMENDATION:  BUY  (Confidence: Moderate)
```

## Architecture

```
stock-trading-analysis/
├── python/
│   ├── main.py                 # Entry point & display
│   ├── technical_analysis.py   # TA indicators & signals
│   ├── fundamental_analysis.py # FA metrics & signals
│   ├── news_analysis.py        # News scraping & sentiment
│   ├── decision_engine.py      # Weighted scoring & recommendation
│   └── requirements.txt
├── csharp/
│   └── StockAnalysis/
│       ├── Program.cs              # Entry point & display
│       ├── Models.cs               # Shared data models
│       ├── YahooFinanceClient.cs   # Yahoo Finance API client
│       ├── TechnicalAnalysis.cs    # TA indicators & signals
│       ├── FundamentalAnalysis.cs  # FA metrics & signals
│       ├── NewsAnalysis.cs         # News & sentiment (built-in lexicon)
│       ├── DecisionEngine.cs       # Weighted scoring & recommendation
│       └── StockAnalysis.csproj
├── README.md
└── CLAUDE.md
```

## Disclaimer

**This tool is for educational and informational purposes only. It does not constitute financial advice. Always do your own research and consult a qualified financial advisor before making investment decisions.**

## License

MIT
