#!/usr/bin/env python3
"""
Stock Trading Decision System
Combines Technical Analysis, Fundamental Analysis, and News Sentiment
to produce a Buy/Hold/Sell recommendation.

Usage:
    python main.py MSFT
    python main.py TEVA.TA
    python main.py TEVA --exchange TLV
    python main.py AAPL --period 1y
    python main.py AAPL --llm
    python main.py AAPL --llm --model claude-sonnet-4-5-20250929
    python main.py --list-exchanges
"""

import argparse
import sys

import yfinance as yf
from colorama import Fore, Style, init as colorama_init

from technical_analysis import TechnicalAnalysis
from fundamental_analysis import FundamentalAnalysis
from news_analysis import NewsAnalysis
from decision_engine import DecisionEngine
from exchange_registry import resolve_ticker, list_exchanges

colorama_init(autoreset=True)

# ── Display helpers ──────────────────────────────────────────────────────────

SEPARATOR = "=" * 72
THIN_SEP = "-" * 72


def color_signal(direction: str) -> str:
    if direction == "Bullish":
        return Fore.GREEN + direction + Style.RESET_ALL
    if direction == "Bearish":
        return Fore.RED + direction + Style.RESET_ALL
    return Fore.YELLOW + direction + Style.RESET_ALL


def print_header(title: str):
    print(f"\n{Fore.CYAN}{SEPARATOR}")
    print(f"  {title}")
    print(f"{SEPARATOR}{Style.RESET_ALL}")


def print_metrics(metrics: dict):
    for k, v in metrics.items():
        if k.startswith("_"):
            continue
        print(f"  {k:<28} {v}")


def print_signals(signals: dict):
    for k, (direction, reason) in signals.items():
        if k.startswith("_"):
            continue
        print(f"  {k:<20} [{color_signal(direction):>18}]  {reason}")


def print_overall(signals: dict, label: str):
    for k, (direction, reason) in signals.items():
        if k.endswith("_Overall"):
            print(f"\n  >> {label} Overall: [{color_signal(direction)}]  {reason}")


# ── Main ─────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Stock Trading Decision System")
    parser.add_argument("ticker", nargs="?", help="Stock ticker, e.g. MSFT, TEVA.TA, or NYSE:MSFT")
    parser.add_argument("--exchange", "-e", default=None,
                        help="Exchange code, e.g. TLV, LSE, TSE (use --list-exchanges to see all)")
    parser.add_argument("--period", default="1y", help="History period (default: 1y)")
    parser.add_argument("--list-exchanges", action="store_true",
                        help="List all supported stock exchanges and exit")
    parser.add_argument("--llm", action="store_true",
                        help="Enable LLM analysis (Claude or OpenRouter)")
    parser.add_argument("--model", default=None,
                        help="LLM model ID (skip interactive selection), e.g. claude-sonnet-4-5-20250929")
    args = parser.parse_args()

    if args.list_exchanges:
        print(list_exchanges())
        sys.exit(0)

    if not args.ticker:
        parser.error("ticker is required (unless using --list-exchanges)")

    try:
        ticker_info = resolve_ticker(args.ticker, args.exchange)
    except ValueError as exc:
        print(f"{Fore.RED}Error: {exc}{Style.RESET_ALL}")
        sys.exit(1)

    symbol = ticker_info.yahoo_symbol
    base_ticker = ticker_info.base_ticker
    exchange_label = f" ({ticker_info.exchange.name})" if ticker_info.exchange else ""
    currency_sym = ticker_info.exchange.currency_symbol if ticker_info.exchange else "$"
    currency_code = ticker_info.exchange.currency if ticker_info.exchange else "USD"

    print(f"\n{Fore.WHITE}{Style.BRIGHT}Stock Trading Decision System")
    print(f"Analyzing: {Fore.YELLOW}{symbol}{exchange_label}{Style.RESET_ALL}")
    print(SEPARATOR)

    # ── Fetch data ────────────────────────────────────────────────────────
    print(f"\n{Fore.WHITE}Fetching market data...{Style.RESET_ALL}")
    ticker = yf.Ticker(symbol)
    hist = ticker.history(period=args.period)

    if hist.empty:
        print(f"{Fore.RED}Error: No data found for '{symbol}'. Check the ticker.{Style.RESET_ALL}")
        sys.exit(1)

    # Use Yahoo Finance's reported currency as fallback
    yf_currency = ticker.info.get("currency", "")
    if yf_currency:
        currency_code = yf_currency

    company_name = ticker.info.get("shortName", symbol)
    current_price = hist["Close"].iloc[-1]
    print(f"  Company : {company_name}")
    print(f"  Price   : {currency_sym}{current_price:.2f} {currency_code}")
    print(f"  Period  : {args.period} ({len(hist)} trading days)")

    # ── Technical Analysis ────────────────────────────────────────────────
    print_header("TECHNICAL ANALYSIS")
    ta_engine = TechnicalAnalysis(hist)
    ta_result = ta_engine.compute_all()

    print(f"\n{Fore.WHITE}  Indicators:{Style.RESET_ALL}")
    print_metrics(ta_result["indicators"])
    print(f"\n{Fore.WHITE}  Signals:{Style.RESET_ALL}")
    print_signals(ta_result["signals"])
    print_overall(ta_result["signals"], "Technical")

    # ── Fundamental Analysis ──────────────────────────────────────────────
    print_header("FUNDAMENTAL ANALYSIS")
    fa_engine = FundamentalAnalysis(ticker, currency_sym=currency_sym)
    fa_result = fa_engine.compute_all()

    print(f"\n{Fore.WHITE}  Metrics:{Style.RESET_ALL}")
    print_metrics(fa_result["metrics"])
    print(f"\n{Fore.WHITE}  Signals:{Style.RESET_ALL}")
    print_signals(fa_result["signals"])
    print_overall(fa_result["signals"], "Fundamental")

    # ── News Sentiment ────────────────────────────────────────────────────
    print_header("NEWS SENTIMENT ANALYSIS")
    news_engine = NewsAnalysis(base_ticker, company_name)
    news_result = news_engine.compute_all()

    print(f"\n{Fore.WHITE}  Metrics:{Style.RESET_ALL}")
    print_metrics(news_result["metrics"])

    if news_result["headlines"]:
        print(f"\n{Fore.WHITE}  Recent Headlines:{Style.RESET_ALL}")
        for h in news_result["headlines"][:10]:
            sent = h.get("sentiment", "")
            score = h.get("sentiment_score", 0)
            clr = Fore.GREEN if sent == "Positive" else Fore.RED if sent == "Negative" else Fore.YELLOW
            print(f"  {clr}[{score:+.3f}]{Style.RESET_ALL} {h['title'][:80]}")
            if h.get("source"):
                print(f"          — {h['source']}  {h.get('date', '')}")

    print_overall(news_result["signals"], "News Sentiment")

    # ── Final Decision ────────────────────────────────────────────────────
    print_header("FINAL DECISION")
    engine = DecisionEngine(ta_result, fa_result, news_result)
    decision = engine.decide()

    print(f"\n{Fore.WHITE}  Component Scores:{Style.RESET_ALL}")
    for k, v in decision["scores"].items():
        print(f"    {k:<24} {v:+.2f}")

    rec = decision["recommendation"]
    conf = decision["confidence"]
    if "BUY" in rec:
        rec_color = Fore.GREEN
    elif "SELL" in rec:
        rec_color = Fore.RED
    else:
        rec_color = Fore.YELLOW

    print(f"\n{SEPARATOR}")
    print(f"  {Fore.WHITE}{Style.BRIGHT}RECOMMENDATION:  {rec_color}{Style.BRIGHT}{rec}{Style.RESET_ALL}  (Confidence: {conf})")
    print(f"{SEPARATOR}")

    print(f"\n{Fore.WHITE}  Explanation:{Style.RESET_ALL}")
    for line in decision["explanation"].split("\n"):
        print(f"    {line}")

    print(f"\n{Fore.YELLOW}  Disclaimer: This is for educational purposes only. Not financial advice.{Style.RESET_ALL}\n")

    # ── LLM Analysis (optional) ──────────────────────────────────────────
    if args.llm or args.model:
        from llm_analysis import run_llm_analysis

        print_header("LLM ANALYSIS")
        print(f"\n{Fore.WHITE}  Querying LLM...{Style.RESET_ALL}")

        try:
            llm_result = run_llm_analysis(
                ta_result=ta_result,
                fa_result=fa_result,
                news_result=news_result,
                decision=decision,
                symbol=symbol,
                company_name=company_name,
                currency_code=currency_code,
                model=args.model,
            )

            print(f"\n  {Fore.CYAN}Model: {llm_result['model']} ({llm_result['provider']}){Style.RESET_ALL}")
            print(f"{THIN_SEP}")
            # Print LLM response with indentation
            for line in llm_result["response"].split("\n"):
                print(f"  {line}")
            print(f"\n{THIN_SEP}")

        except Exception as exc:
            print(f"\n  {Fore.RED}LLM analysis failed: {exc}{Style.RESET_ALL}")


if __name__ == "__main__":
    main()
