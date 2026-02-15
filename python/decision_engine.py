"""Decision Engine - combines all analyses into a final Buy/Hold/Sell recommendation."""


class DecisionEngine:
    """Weighted scoring system that merges TA, FA, and News signals."""

    WEIGHTS = {
        "technical": 0.40,
        "fundamental": 0.35,
        "news": 0.25,
    }

    def __init__(self, ta_result: dict, fa_result: dict, news_result: dict):
        self.ta = ta_result
        self.fa = fa_result
        self.news = news_result

    def decide(self) -> dict:
        ta_score = self._category_score(self.ta.get("signals", {}))
        fa_score = self._category_score(self.fa.get("signals", {}))
        news_score = self._category_score(self.news.get("signals", {}))

        weighted = (
            ta_score * self.WEIGHTS["technical"]
            + fa_score * self.WEIGHTS["fundamental"]
            + news_score * self.WEIGHTS["news"]
        )

        recommendation, confidence = self._map_score(weighted)

        return {
            "scores": {
                "Technical Score": round(ta_score, 2),
                "Fundamental Score": round(fa_score, 2),
                "News Score": round(news_score, 2),
                "Weighted Score": round(weighted, 2),
            },
            "weights": self.WEIGHTS,
            "recommendation": recommendation,
            "confidence": confidence,
            "explanation": self._explain(ta_score, fa_score, news_score, weighted, recommendation),
        }

    @staticmethod
    def _category_score(signals: dict) -> float:
        """Convert signals dict to a -1..+1 score."""
        if not signals:
            return 0.0
        total = 0
        count = 0
        for key, (direction, _) in signals.items():
            if key.startswith("_") and key.endswith("Overall"):
                continue  # skip aggregate keys to avoid double-counting
            count += 1
            if direction == "Bullish":
                total += 1
            elif direction == "Bearish":
                total -= 1
        return total / count if count else 0.0

    @staticmethod
    def _map_score(score: float) -> tuple[str, str]:
        if score >= 0.4:
            return "STRONG BUY", "High"
        if score >= 0.15:
            return "BUY", "Moderate"
        if score > -0.15:
            return "HOLD", "—"
        if score > -0.4:
            return "SELL", "Moderate"
        return "STRONG SELL", "High"

    @staticmethod
    def _explain(ta: float, fa: float, news: float, weighted: float, rec: str) -> str:
        parts = []
        parts.append(f"Technical analysis score: {ta:+.2f} (weight 40%)")
        parts.append(f"Fundamental analysis score: {fa:+.2f} (weight 35%)")
        parts.append(f"News sentiment score: {news:+.2f} (weight 25%)")
        parts.append(f"Combined weighted score: {weighted:+.2f}")
        parts.append(f"Recommendation: {rec}")
        parts.append("")
        parts.append("Score interpretation: -1.0 (Strong Sell) to +1.0 (Strong Buy)")
        parts.append("  >= +0.40  STRONG BUY  | >= +0.15  BUY  | -0.15 to +0.15  HOLD")
        parts.append("  <= -0.15  SELL        | <= -0.40  STRONG SELL")
        return "\n".join(parts)
