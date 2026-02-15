"""News Analysis module - scrapes recent headlines and performs sentiment analysis."""

import requests
from bs4 import BeautifulSoup
from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer


class NewsAnalysis:
    """Fetch recent news headlines and score their sentiment."""

    def __init__(self, symbol: str, company_name: str = ""):
        self.symbol = symbol
        self.company_name = company_name
        self.headlines: list[dict] = []
        self.metrics: dict = {}
        self.signals: dict = {}
        self.analyzer = SentimentIntensityAnalyzer()

    def compute_all(self) -> dict:
        self._fetch_headlines()
        self._score_sentiment()
        self._aggregate_signal()
        return {
            "headlines": self.headlines,
            "metrics": self.metrics,
            "signals": self.signals,
        }

    def _fetch_headlines(self):
        """Fetch headlines from multiple free RSS/web sources."""
        sources = [
            self._fetch_google_news,
            self._fetch_finviz,
        ]
        for fetcher in sources:
            try:
                fetcher()
            except Exception:
                continue

        # Deduplicate by title
        seen = set()
        unique = []
        for h in self.headlines:
            key = h["title"].lower().strip()
            if key not in seen:
                seen.add(key)
                unique.append(h)
        self.headlines = unique[:20]  # keep top 20

    def _fetch_google_news(self):
        query = self.symbol
        url = f"https://news.google.com/rss/search?q={query}+stock&hl=en-US&gl=US&ceid=US:en"
        headers = {"User-Agent": "Mozilla/5.0"}
        resp = requests.get(url, headers=headers, timeout=10)
        if resp.status_code != 200:
            return
        soup = BeautifulSoup(resp.content, "xml")
        for item in soup.find_all("item")[:15]:
            title = item.find("title")
            source = item.find("source")
            pub_date = item.find("pubDate")
            if title:
                self.headlines.append({
                    "title": title.text.strip(),
                    "source": source.text.strip() if source else "Google News",
                    "date": pub_date.text.strip() if pub_date else "",
                })

    def _fetch_finviz(self):
        url = f"https://finviz.com/quote.ashx?t={self.symbol}"
        headers = {"User-Agent": "Mozilla/5.0"}
        resp = requests.get(url, headers=headers, timeout=10)
        if resp.status_code != 200:
            return
        soup = BeautifulSoup(resp.text, "html.parser")
        news_table = soup.find("table", {"id": "news-table"})
        if not news_table:
            return
        for row in news_table.find_all("tr")[:15]:
            link = row.find("a")
            date_cell = row.find("td")
            if link:
                self.headlines.append({
                    "title": link.text.strip(),
                    "source": "Finviz",
                    "date": date_cell.text.strip() if date_cell else "",
                })

    def _score_sentiment(self):
        if not self.headlines:
            self.metrics["News Count"] = 0
            self.metrics["Avg Sentiment"] = "N/A"
            return

        scores = []
        for h in self.headlines:
            vs = self.analyzer.polarity_scores(h["title"])
            h["sentiment_score"] = round(vs["compound"], 3)
            h["sentiment"] = _label(vs["compound"])
            scores.append(vs["compound"])

        avg = sum(scores) / len(scores)
        positive = sum(1 for s in scores if s > 0.05)
        negative = sum(1 for s in scores if s < -0.05)
        neutral = len(scores) - positive - negative

        self.metrics["News Count"] = len(scores)
        self.metrics["Avg Sentiment"] = round(avg, 3)
        self.metrics["Positive Headlines"] = positive
        self.metrics["Negative Headlines"] = negative
        self.metrics["Neutral Headlines"] = neutral

    def _aggregate_signal(self):
        avg = self.metrics.get("Avg Sentiment")
        if avg is None or avg == "N/A":
            self.signals["_News_Overall"] = ("Neutral", "No news data available")
            return

        pos = self.metrics.get("Positive Headlines", 0)
        neg = self.metrics.get("Negative Headlines", 0)
        total = self.metrics.get("News Count", 0)

        if avg > 0.15 and pos > neg:
            self.signals["_News_Overall"] = ("Bullish", f"Avg sentiment {avg:.3f}, {pos}/{total} positive")
        elif avg < -0.15 and neg > pos:
            self.signals["_News_Overall"] = ("Bearish", f"Avg sentiment {avg:.3f}, {neg}/{total} negative")
        elif avg > 0.05:
            self.signals["_News_Overall"] = ("Bullish", f"Slightly positive sentiment {avg:.3f}")
        elif avg < -0.05:
            self.signals["_News_Overall"] = ("Bearish", f"Slightly negative sentiment {avg:.3f}")
        else:
            self.signals["_News_Overall"] = ("Neutral", f"Mixed sentiment {avg:.3f}")


def _label(score: float) -> str:
    if score > 0.05:
        return "Positive"
    if score < -0.05:
        return "Negative"
    return "Neutral"
