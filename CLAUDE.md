# CLAUDE.md - Project Guide for AI Assistants

## Project Overview
Stock Trading Decision System - a dual-implementation (Python + C#) tool that combines technical analysis, fundamental analysis, and news sentiment to generate stock trading recommendations.

## Repository Structure
- `python/` - Python implementation using yfinance, ta, vaderSentiment
- `csharp/StockAnalysis/` - C# (.NET 8) implementation using Yahoo Finance REST API

## Build & Run

### Python
```bash
cd python
pip install -r requirements.txt
python main.py <TICKER>           # e.g. python main.py MSFT
python main.py <TICKER> --period 6mo
```

### C#
```bash
cd csharp/StockAnalysis
dotnet restore
dotnet run -- <TICKER>            # e.g. dotnet run -- MSFT
dotnet run -- <TICKER> --period 6mo
```

## Key Design Decisions
- **Modular architecture**: Each analysis type (TA, FA, News) is a standalone module
- **Signal-based system**: Every indicator produces a Bullish/Bearish/Neutral signal
- **Weighted aggregation**: TA 40%, FA 35%, News 25% — these weights reflect typical quant models
- **No external API keys required**: Uses Yahoo Finance (free) and Google News RSS
- **C# implements its own sentiment lexicon** instead of requiring an external NLP library

## Conventions
- Python: snake_case, type hints, dataclass-style dicts
- C#: PascalCase, records for immutable models, async/await for HTTP calls
- Both versions produce identical output format for consistency

## Testing
Run against known tickers to verify:
```bash
# Python
python main.py AAPL
python main.py GOOGL
python main.py TSLA

# C#
dotnet run -- AAPL
dotnet run -- GOOGL
dotnet run -- TSLA
```

## Extending
- To add a new technical indicator: add method in `TechnicalAnalysis`, call it from `compute_all`/`ComputeAll`
- To adjust weights: modify `WEIGHTS`/`Weights` dict in `DecisionEngine`
- To add a news source: add a `_fetch_*`/`Fetch*` method in `NewsAnalysis`
