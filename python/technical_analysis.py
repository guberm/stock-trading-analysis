"""Technical Analysis module - computes indicators and generates signals."""

import pandas as pd
import numpy as np
import ta


class TechnicalAnalysis:
    """Compute technical indicators and produce a consolidated signal."""

    def __init__(self, df: pd.DataFrame):
        """
        Args:
            df: OHLCV DataFrame with columns [Open, High, Low, Close, Volume].
        """
        self.df = df.copy()
        self.indicators: dict = {}
        self.signals: dict = {}

    def compute_all(self) -> dict:
        self._moving_averages()
        self._rsi()
        self._macd()
        self._bollinger_bands()
        self._stochastic()
        self._atr()
        self._obv()
        self._adx()
        self._cci()
        self._williams_r()
        self._aggregate_signal()
        return {"indicators": self.indicators, "signals": self.signals}

    # -- Trend indicators --------------------------------------------------

    def _moving_averages(self):
        close = self.df["Close"]
        sma_20 = ta.trend.sma_indicator(close, window=20)
        sma_50 = ta.trend.sma_indicator(close, window=50)
        sma_200 = ta.trend.sma_indicator(close, window=200)
        ema_12 = ta.trend.ema_indicator(close, window=12)
        ema_26 = ta.trend.ema_indicator(close, window=26)

        last = close.iloc[-1]
        self.indicators["SMA_20"] = round(sma_20.iloc[-1], 2)
        self.indicators["SMA_50"] = round(sma_50.iloc[-1], 2)
        self.indicators["SMA_200"] = round(sma_200.iloc[-1], 2)
        self.indicators["EMA_12"] = round(ema_12.iloc[-1], 2)
        self.indicators["EMA_26"] = round(ema_26.iloc[-1], 2)

        # Golden / death cross
        if sma_50.iloc[-1] > sma_200.iloc[-1]:
            self.signals["MA_Cross"] = ("Bullish", "Golden Cross (SMA50 > SMA200)")
        else:
            self.signals["MA_Cross"] = ("Bearish", "Death Cross (SMA50 < SMA200)")

        # Price vs SMA 200
        if last > sma_200.iloc[-1]:
            self.signals["Price_vs_SMA200"] = ("Bullish", f"Price {last:.2f} above SMA200 {sma_200.iloc[-1]:.2f}")
        else:
            self.signals["Price_vs_SMA200"] = ("Bearish", f"Price {last:.2f} below SMA200 {sma_200.iloc[-1]:.2f}")

    # -- Momentum indicators ------------------------------------------------

    def _rsi(self):
        rsi = ta.momentum.rsi(self.df["Close"], window=14)
        val = round(rsi.iloc[-1], 2)
        self.indicators["RSI_14"] = val
        if val > 70:
            self.signals["RSI"] = ("Bearish", f"RSI {val} – overbought")
        elif val < 30:
            self.signals["RSI"] = ("Bullish", f"RSI {val} – oversold")
        else:
            self.signals["RSI"] = ("Neutral", f"RSI {val} – neutral zone")

    def _macd(self):
        macd_line = ta.trend.macd(self.df["Close"])
        macd_signal = ta.trend.macd_signal(self.df["Close"])
        macd_hist = ta.trend.macd_diff(self.df["Close"])

        self.indicators["MACD_Line"] = round(macd_line.iloc[-1], 4)
        self.indicators["MACD_Signal"] = round(macd_signal.iloc[-1], 4)
        self.indicators["MACD_Histogram"] = round(macd_hist.iloc[-1], 4)

        if macd_line.iloc[-1] > macd_signal.iloc[-1]:
            self.signals["MACD"] = ("Bullish", "MACD line above signal line")
        else:
            self.signals["MACD"] = ("Bearish", "MACD line below signal line")

    def _stochastic(self):
        stoch_k = ta.momentum.stoch(self.df["High"], self.df["Low"], self.df["Close"])
        stoch_d = ta.momentum.stoch_signal(self.df["High"], self.df["Low"], self.df["Close"])
        k_val = round(stoch_k.iloc[-1], 2)
        d_val = round(stoch_d.iloc[-1], 2)
        self.indicators["Stoch_K"] = k_val
        self.indicators["Stoch_D"] = d_val

        if k_val < 20:
            self.signals["Stochastic"] = ("Bullish", f"Stochastic %K={k_val} – oversold")
        elif k_val > 80:
            self.signals["Stochastic"] = ("Bearish", f"Stochastic %K={k_val} – overbought")
        else:
            self.signals["Stochastic"] = ("Neutral", f"Stochastic %K={k_val} – neutral")

    def _williams_r(self):
        wr = ta.momentum.williams_r(self.df["High"], self.df["Low"], self.df["Close"])
        val = round(wr.iloc[-1], 2)
        self.indicators["Williams_%R"] = val
        if val < -80:
            self.signals["Williams_R"] = ("Bullish", f"Williams %R {val} – oversold")
        elif val > -20:
            self.signals["Williams_R"] = ("Bearish", f"Williams %R {val} – overbought")
        else:
            self.signals["Williams_R"] = ("Neutral", f"Williams %R {val} – neutral")

    def _cci(self):
        cci = ta.trend.cci(self.df["High"], self.df["Low"], self.df["Close"])
        val = round(cci.iloc[-1], 2)
        self.indicators["CCI"] = val
        if val > 100:
            self.signals["CCI"] = ("Bearish", f"CCI {val} – overbought")
        elif val < -100:
            self.signals["CCI"] = ("Bullish", f"CCI {val} – oversold")
        else:
            self.signals["CCI"] = ("Neutral", f"CCI {val} – neutral")

    # -- Volatility indicators ----------------------------------------------

    def _bollinger_bands(self):
        bb = ta.volatility.BollingerBands(self.df["Close"])
        upper = round(bb.bollinger_hband().iloc[-1], 2)
        mid = round(bb.bollinger_mavg().iloc[-1], 2)
        lower = round(bb.bollinger_lband().iloc[-1], 2)
        last = self.df["Close"].iloc[-1]

        self.indicators["BB_Upper"] = upper
        self.indicators["BB_Middle"] = mid
        self.indicators["BB_Lower"] = lower

        if last >= upper:
            self.signals["Bollinger"] = ("Bearish", f"Price at/above upper band ({upper})")
        elif last <= lower:
            self.signals["Bollinger"] = ("Bullish", f"Price at/below lower band ({lower})")
        else:
            self.signals["Bollinger"] = ("Neutral", f"Price within bands ({lower} – {upper})")

    def _atr(self):
        atr = ta.volatility.average_true_range(self.df["High"], self.df["Low"], self.df["Close"])
        val = round(atr.iloc[-1], 2)
        self.indicators["ATR_14"] = val
        pct = round(val / self.df["Close"].iloc[-1] * 100, 2)
        self.indicators["ATR_%"] = pct

    # -- Volume indicators --------------------------------------------------

    def _obv(self):
        obv = ta.volume.on_balance_volume(self.df["Close"], self.df["Volume"])
        self.indicators["OBV"] = int(obv.iloc[-1])
        # OBV trend (rising / falling over last 10 days)
        if obv.iloc[-1] > obv.iloc[-10]:
            self.signals["OBV"] = ("Bullish", "OBV rising – volume confirming trend")
        else:
            self.signals["OBV"] = ("Bearish", "OBV falling – volume diverging")

    def _adx(self):
        adx = ta.trend.adx(self.df["High"], self.df["Low"], self.df["Close"])
        val = round(adx.iloc[-1], 2)
        self.indicators["ADX"] = val
        if val > 25:
            self.signals["ADX"] = ("Neutral", f"ADX {val} – strong trend")
        else:
            self.signals["ADX"] = ("Neutral", f"ADX {val} – weak/no trend")

    # -- Aggregation --------------------------------------------------------

    def _aggregate_signal(self):
        bullish = sum(1 for s, _ in self.signals.values() if s == "Bullish")
        bearish = sum(1 for s, _ in self.signals.values() if s == "Bearish")
        total = len(self.signals)
        self.indicators["_ta_bullish"] = bullish
        self.indicators["_ta_bearish"] = bearish
        self.indicators["_ta_neutral"] = total - bullish - bearish

        if bullish > bearish + 2:
            self.signals["_TA_Overall"] = ("Bullish", f"{bullish}/{total} signals bullish")
        elif bearish > bullish + 2:
            self.signals["_TA_Overall"] = ("Bearish", f"{bearish}/{total} signals bearish")
        else:
            self.signals["_TA_Overall"] = ("Neutral", f"Mixed – {bullish}B/{bearish}S/{total - bullish - bearish}N")
