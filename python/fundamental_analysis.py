"""Fundamental Analysis module - retrieves and scores key financial metrics."""

import yfinance as yf


class FundamentalAnalysis:
    """Pull fundamental data from Yahoo Finance and score the stock."""

    def __init__(self, ticker: yf.Ticker, currency_sym: str = "$"):
        self.ticker = ticker
        self.info = ticker.info
        self.currency_sym = currency_sym
        self.metrics: dict = {}
        self.signals: dict = {}

    def compute_all(self) -> dict:
        self._valuation()
        self._profitability()
        self._growth()
        self._financial_health()
        self._dividends()
        self._analyst_targets()
        self._aggregate_signal()
        return {"metrics": self.metrics, "signals": self.signals}

    # -- Valuation ----------------------------------------------------------

    def _valuation(self):
        pe = self.info.get("trailingPE")
        fwd_pe = self.info.get("forwardPE")
        peg = self.info.get("pegRatio")
        pb = self.info.get("priceToBook")
        ps = self.info.get("priceToSalesTrailing12Months")
        ev_ebitda = self.info.get("enterpriseToEbitda")

        self.metrics["P/E (TTM)"] = _fmt(pe)
        self.metrics["Forward P/E"] = _fmt(fwd_pe)
        self.metrics["PEG Ratio"] = _fmt(peg)
        self.metrics["P/B"] = _fmt(pb)
        self.metrics["P/S"] = _fmt(ps)
        self.metrics["EV/EBITDA"] = _fmt(ev_ebitda)

        # Simple scoring
        score = 0
        reasons = []
        if pe and pe < 20:
            score += 1; reasons.append(f"P/E {pe:.1f} < 20")
        elif pe and pe > 35:
            score -= 1; reasons.append(f"P/E {pe:.1f} > 35")

        if peg and peg < 1.5:
            score += 1; reasons.append(f"PEG {peg:.2f} < 1.5")
        elif peg and peg > 2.5:
            score -= 1; reasons.append(f"PEG {peg:.2f} > 2.5")

        if pb and pb < 3:
            score += 1; reasons.append(f"P/B {pb:.2f} < 3")

        if score > 0:
            self.signals["Valuation"] = ("Bullish", "; ".join(reasons))
        elif score < 0:
            self.signals["Valuation"] = ("Bearish", "; ".join(reasons))
        else:
            self.signals["Valuation"] = ("Neutral", "Fair valuation")

    # -- Profitability ------------------------------------------------------

    def _profitability(self):
        margin = self.info.get("profitMargins")
        op_margin = self.info.get("operatingMargins")
        roe = self.info.get("returnOnEquity")
        roa = self.info.get("returnOnAssets")

        self.metrics["Profit Margin"] = _pct(margin)
        self.metrics["Operating Margin"] = _pct(op_margin)
        self.metrics["ROE"] = _pct(roe)
        self.metrics["ROA"] = _pct(roa)

        score = 0
        reasons = []
        if margin and margin > 0.15:
            score += 1; reasons.append(f"Profit margin {margin:.1%}")
        if roe and roe > 0.15:
            score += 1; reasons.append(f"ROE {roe:.1%}")
        if roa and roa > 0.05:
            score += 1; reasons.append(f"ROA {roa:.1%}")

        if score >= 2:
            self.signals["Profitability"] = ("Bullish", "; ".join(reasons))
        elif score == 0 and margin and margin < 0:
            self.signals["Profitability"] = ("Bearish", "Negative margins")
        else:
            self.signals["Profitability"] = ("Neutral", "; ".join(reasons) if reasons else "Average profitability")

    # -- Growth -------------------------------------------------------------

    def _growth(self):
        rev_growth = self.info.get("revenueGrowth")
        earn_growth = self.info.get("earningsGrowth")
        earn_qtr = self.info.get("earningsQuarterlyGrowth")

        self.metrics["Revenue Growth"] = _pct(rev_growth)
        self.metrics["Earnings Growth"] = _pct(earn_growth)
        self.metrics["Earnings Growth (QoQ)"] = _pct(earn_qtr)

        score = 0
        reasons = []
        if rev_growth and rev_growth > 0.10:
            score += 1; reasons.append(f"Revenue growth {rev_growth:.1%}")
        if earn_growth and earn_growth > 0.10:
            score += 1; reasons.append(f"Earnings growth {earn_growth:.1%}")

        if score >= 1:
            self.signals["Growth"] = ("Bullish", "; ".join(reasons))
        elif rev_growth and rev_growth < 0:
            self.signals["Growth"] = ("Bearish", f"Revenue declining {rev_growth:.1%}")
        else:
            self.signals["Growth"] = ("Neutral", "Moderate growth")

    # -- Financial Health ---------------------------------------------------

    def _financial_health(self):
        de = self.info.get("debtToEquity")
        current = self.info.get("currentRatio")
        quick = self.info.get("quickRatio")
        fcf = self.info.get("freeCashflow")
        total_cash = self.info.get("totalCash")
        total_debt = self.info.get("totalDebt")

        self.metrics["Debt/Equity"] = _fmt(de)
        self.metrics["Current Ratio"] = _fmt(current)
        self.metrics["Quick Ratio"] = _fmt(quick)
        self.metrics["Free Cash Flow"] = _human_number(fcf, self.currency_sym)
        self.metrics["Total Cash"] = _human_number(total_cash, self.currency_sym)
        self.metrics["Total Debt"] = _human_number(total_debt, self.currency_sym)

        score = 0
        reasons = []
        if de is not None and de < 100:
            score += 1; reasons.append(f"D/E {de:.1f} < 100")
        elif de is not None and de > 200:
            score -= 1; reasons.append(f"D/E {de:.1f} > 200")

        if current and current > 1.5:
            score += 1; reasons.append(f"Current ratio {current:.2f}")

        if fcf and fcf > 0:
            score += 1; reasons.append("Positive FCF")

        if score >= 2:
            self.signals["Financial Health"] = ("Bullish", "; ".join(reasons))
        elif score < 0:
            self.signals["Financial Health"] = ("Bearish", "; ".join(reasons))
        else:
            self.signals["Financial Health"] = ("Neutral", "; ".join(reasons) if reasons else "Average health")

    # -- Dividends ----------------------------------------------------------

    def _dividends(self):
        div_yield = self.info.get("dividendYield")
        payout = self.info.get("payoutRatio")

        self.metrics["Dividend Yield"] = _pct(div_yield)
        self.metrics["Payout Ratio"] = _pct(payout)

        if div_yield and div_yield > 0.02:
            self.signals["Dividends"] = ("Bullish", f"Yield {div_yield:.2%}")
        elif div_yield and div_yield > 0:
            self.signals["Dividends"] = ("Neutral", f"Yield {div_yield:.2%}")
        else:
            self.signals["Dividends"] = ("Neutral", "No dividend")

    # -- Analyst targets ----------------------------------------------------

    def _analyst_targets(self):
        target = self.info.get("targetMeanPrice")
        current_price = self.info.get("currentPrice") or self.info.get("regularMarketPrice")
        rec = self.info.get("recommendationKey")
        num_analysts = self.info.get("numberOfAnalystOpinions")

        self.metrics["Analyst Target Price"] = _fmt(target)
        self.metrics["Current Price"] = _fmt(current_price)
        self.metrics["Analyst Recommendation"] = rec or "N/A"
        self.metrics["Number of Analysts"] = num_analysts or "N/A"

        if target and current_price:
            upside = (target - current_price) / current_price
            self.metrics["Upside to Target"] = f"{upside:.1%}"
            sym = self.currency_sym
            if upside > 0.15:
                self.signals["Analyst Target"] = ("Bullish", f"{upside:.1%} upside to {sym}{target}")
            elif upside < -0.10:
                self.signals["Analyst Target"] = ("Bearish", f"{upside:.1%} downside to {sym}{target}")
            else:
                self.signals["Analyst Target"] = ("Neutral", f"{upside:.1%} to target {sym}{target}")

    # -- Aggregation --------------------------------------------------------

    def _aggregate_signal(self):
        bullish = sum(1 for s, _ in self.signals.values() if s == "Bullish")
        bearish = sum(1 for s, _ in self.signals.values() if s == "Bearish")
        total = len(self.signals)
        self.metrics["_fa_bullish"] = bullish
        self.metrics["_fa_bearish"] = bearish
        self.metrics["_fa_neutral"] = total - bullish - bearish

        if bullish > bearish + 1:
            self.signals["_FA_Overall"] = ("Bullish", f"{bullish}/{total} fundamentals bullish")
        elif bearish > bullish + 1:
            self.signals["_FA_Overall"] = ("Bearish", f"{bearish}/{total} fundamentals bearish")
        else:
            self.signals["_FA_Overall"] = ("Neutral", f"Mixed – {bullish}B/{bearish}S/{total - bullish - bearish}N")


# -- Helpers ----------------------------------------------------------------

def _fmt(val):
    if val is None:
        return "N/A"
    if isinstance(val, float):
        return round(val, 2)
    return val


def _pct(val):
    if val is None:
        return "N/A"
    return f"{val:.2%}"


def _human_number(val, currency_sym="$"):
    if val is None:
        return "N/A"
    abs_val = abs(val)
    sign = "-" if val < 0 else ""
    s = currency_sym
    if abs_val >= 1_000_000_000_000:
        return f"{sign}{s}{abs_val / 1_000_000_000_000:.2f}T"
    if abs_val >= 1_000_000_000:
        return f"{sign}{s}{abs_val / 1_000_000_000:.2f}B"
    if abs_val >= 1_000_000:
        return f"{sign}{s}{abs_val / 1_000_000:.2f}M"
    return f"{sign}{s}{abs_val:,.0f}"
