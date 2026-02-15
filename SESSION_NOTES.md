# Session Notes — 2026-02-15

## What Was Built

### 1. Worldwide Stock Exchange Support
Added support for 60+ stock exchanges across all regions. Both Python and C# implementations updated.

**New files:**
- `python/exchange_registry.py` — Exchange map, ticker resolver, exchange listing
- `csharp/StockAnalysis/ExchangeRegistry.cs` — C# mirror

**Modified files:**
- `python/main.py` — New CLI args (`--exchange`, `--list-exchanges`), dynamic currency display
- `python/fundamental_analysis.py` — Dynamic currency symbol (replaced hardcoded `$`)
- `csharp/StockAnalysis/Program.cs` — Same changes as Python main
- `csharp/StockAnalysis/FundamentalAnalysis.cs` — Dynamic currency symbol
- `csharp/StockAnalysis/YahooFinanceClient.cs` — Extract `currency` field from Yahoo response

**Usage:**
```bash
# Direct suffix
python main.py TEVA.TA
python main.py 7203.T

# Exchange flag
python main.py TEVA --exchange TLV
python main.py VOD --exchange LSE

# List all exchanges
python main.py --list-exchanges
```

**Supported regions:** Americas (8), Europe (20), Asia-Pacific (15), Middle East & South Asia (6), Africa (1)

### 2. LLM-Based Analysis (Claude + OpenRouter)
Added optional LLM analysis that sends all raw data to an LLM for an independent Buy/Hold/Sell recommendation.

**New files:**
- `python/llm_analysis.py` — Config management, model selection, prompt builder, API callers
- `csharp/StockAnalysis/LlmAnalysis.cs` — C# mirror

**Modified files:**
- `python/main.py` — New CLI args (`--llm`, `--model`), LLM section after algorithmic decision
- `csharp/StockAnalysis/Program.cs` — Same changes

**Usage:**
```bash
# Interactive model selection
python main.py AAPL --llm

# Skip interactive selection
python main.py AAPL --llm --model claude-sonnet-4-5-20250929

# Use OpenRouter models
python main.py MSFT --llm --model google/gemini-2.0-flash-001
```

**Supported providers:**
- **Claude** (Anthropic API): Sonnet 4.5, Opus 4.6, Haiku 4.5
- **OpenRouter**: GPT-4o, Gemini 2.5 Pro, Gemini 2.0 Flash, o3-mini, Llama 4, DeepSeek R1, Mistral Large

**Config file:** `~/.stock-analysis.json` — API keys persist across runs

## Known Issues
- **C# Yahoo Finance REST API returns 401** — Yahoo changed their API auth requirements. The C# implementation compiles and the exchange/LLM code is structurally correct, but market data fetching fails for all tickers. Python (via yfinance library) is unaffected.

## Architecture Decisions
- No new pip dependencies for LLM — uses existing `requests` library with raw HTTP calls
- Exchange registry is a standalone module reusable by all components
- News fetching uses base ticker (without exchange suffix) for better search results
- Config file shared between Python and C# at `~/.stock-analysis.json`
- LLM prompt includes ALL raw data: 20 technical indicators, all fundamentals, news headlines with sentiment scores, plus the algorithmic result for reference
