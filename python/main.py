#!/usr/bin/env python3
"""
Stock Trading Decision System
Combines Technical Analysis, Fundamental Analysis, and News Sentiment
to produce a Buy/Hold/Sell recommendation.

Usage:
    python main.py MSFT
    python main.py NYSE:MSFT
    python main.py AAPL --period 1y
"""

import argparse
import sys

import yfinance as yf
from colorama import Fore, Style, init as colorama_init

from technical_analysis import TechnicalAnalysis
from fundamental_analysis import FundamentalAnalysis
from news_analysis import NewsAnalysis
from decision_engine import DecisionEngine

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

def parse_ticker(raw: str) -> str:
    """Strip exchange prefix like NYSE: or NASDAQ:."""
    if ":" in raw:
        return raw.split(":")[-1].strip().upper()
    return raw.strip().upper()


def main():
    parser = argparse.ArgumentParser(description="Stock Trading Decision System")
    parser.add_argument("ticker", help="Stock ticker, e.g. MSFT or NYSE:MSFT")
    parser.add_argument("--period", default="1y", help="History period (default: 1y)")
    args = parser.parse_args()

    symbol = parse_ticker(args.ticker)

    print(f"\n{Fore.WHITE}{Style.BRIGHT}Stock Trading Decision System")
    print(f"Analyzing: {Fore.YELLOW}{symbol}{Style.RESET_ALL}")
    print(SEPARATOR)

    # ── Fetch data ────────────────────────────────────────────────────────
    print(f"\n{Fore.WHITE}Fetching market data...{Style.RESET_ALL}")
    ticker = yf.Ticker(symbol)
    hist = ticker.history(period=args.period)

    if hist.empty:
        print(f"{Fore.RED}Error: No data found for '{symbol}'. Check the ticker.{Style.RESET_ALL}")
        sys.exit(1)

    company_name = ticker.info.get("shortName", symbol)
    current_price = hist["Close"].iloc[-1]
    print(f"  Company : {company_name}")
    print(f"  Price   : ${current_price:.2f}")
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
    fa_engine = FundamentalAnalysis(ticker)
    fa_result = fa_engine.compute_all()

    print(f"\n{Fore.WHITE}  Metrics:{Style.RESET_ALL}")
    print_metrics(fa_result["metrics"])
    print(f"\n{Fore.WHITE}  Signals:{Style.RESET_ALL}")
    print_signals(fa_result["signals"])
    print_overall(fa_result["signals"], "Fundamental")

    # ── News Sentiment ────────────────────────────────────────────────────
    print_header("NEWS SENTIMENT ANALYSIS")
    news_engine = NewsAnalysis(symbol, company_name)
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


if __name__ == "__main__":
    main()
