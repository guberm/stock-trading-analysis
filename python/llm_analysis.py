"""LLM Analysis module - sends all analysis data to an LLM for an independent recommendation."""

import json
import os
import sys
from dataclasses import dataclass
from pathlib import Path

import requests

CONFIG_PATH = Path.home() / ".stock-analysis.json"

# ── Model definitions ────────────────────────────────────────────────────────

CLAUDE_MODELS = [
    ("claude-sonnet-4-5-20250929", "Claude Sonnet 4.5 (Recommended)"),
    ("claude-opus-4-6", "Claude Opus 4.6"),
    ("claude-haiku-4-5-20251001", "Claude Haiku 4.5"),
]

OPENROUTER_MODELS = [
    ("anthropic/claude-sonnet-4-5-20250929", "Claude Sonnet 4.5"),
    ("google/gemini-2.5-pro-preview", "Gemini 2.5 Pro"),
    ("google/gemini-2.0-flash-001", "Gemini 2.0 Flash"),
    ("openai/gpt-4o", "GPT-4o"),
    ("openai/o3-mini", "OpenAI o3-mini"),
    ("meta-llama/llama-4-maverick", "Llama 4 Maverick"),
    ("deepseek/deepseek-r1", "DeepSeek R1"),
    ("mistralai/mistral-large-2411", "Mistral Large"),
]


@dataclass
class LLMConfig:
    provider: str   # "claude" or "openrouter"
    model: str      # model ID
    api_key: str


# ── Config file management ───────────────────────────────────────────────────

def load_config() -> dict:
    """Load config from ~/.stock-analysis.json, return empty dict if missing."""
    if CONFIG_PATH.exists():
        try:
            return json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, OSError):
            return {}
    return {}


def save_config(config: dict):
    """Save config to ~/.stock-analysis.json."""
    CONFIG_PATH.write_text(json.dumps(config, indent=2) + "\n", encoding="utf-8")


def _prompt_api_key(provider: str, config: dict) -> str:
    """Prompt user for an API key if not already in config."""
    config_key = f"{provider}_api_key"
    existing = config.get(config_key, "")
    if existing:
        return existing

    print(f"\n  No {provider.upper()} API key found in {CONFIG_PATH}")
    key = input(f"  Enter your {provider.upper()} API key: ").strip()
    if not key:
        print("  Error: API key is required.")
        sys.exit(1)

    config[config_key] = key
    save_config(config)
    print(f"  Key saved to {CONFIG_PATH}")
    return key


# ── Interactive model selection ──────────────────────────────────────────────

def select_model_interactive(config: dict) -> LLMConfig:
    """Interactive menu to pick provider and model."""
    print("\n  Select LLM provider:")
    print("    1. Claude (Anthropic API)")
    print("    2. OpenRouter (GPT-4, Gemini, Llama, etc.)")

    choice = input("\n  Provider [1]: ").strip() or "1"
    if choice == "2":
        provider = "openrouter"
        models = OPENROUTER_MODELS
    else:
        provider = "claude"
        models = CLAUDE_MODELS

    print(f"\n  Select model:")
    for i, (model_id, label) in enumerate(models, 1):
        print(f"    {i}. {label}  ({model_id})")

    model_choice = input(f"\n  Model [1]: ").strip() or "1"
    try:
        idx = int(model_choice) - 1
        if 0 <= idx < len(models):
            model_id = models[idx][0]
        else:
            model_id = models[0][0]
    except ValueError:
        # Allow typing a model ID directly
        model_id = model_choice

    api_key = _prompt_api_key(provider, config)
    return LLMConfig(provider=provider, model=model_id, api_key=api_key)


def resolve_model(model_str: str | None, config: dict) -> LLMConfig:
    """
    Resolve a model from --model flag or interactive selection.

    --model flag formats:
      claude-sonnet-4-5-20250929         → Claude provider
      openrouter/google/gemini-2.0-flash → OpenRouter provider
    """
    if not model_str:
        return select_model_interactive(config)

    if model_str.startswith("openrouter/"):
        provider = "openrouter"
        model_id = model_str[len("openrouter/"):]
    else:
        # Check if it's an OpenRouter-style model with org/name
        # Claude models start with "claude-"
        if model_str.startswith("claude-"):
            provider = "claude"
            model_id = model_str
        elif "/" in model_str:
            provider = "openrouter"
            model_id = model_str
        else:
            provider = "claude"
            model_id = model_str

    api_key = _prompt_api_key(provider, config)
    return LLMConfig(provider=provider, model=model_id, api_key=api_key)


# ── Prompt builder ───────────────────────────────────────────────────────────

def build_prompt(
    ta_result: dict,
    fa_result: dict,
    news_result: dict,
    decision: dict,
    symbol: str,
    company_name: str,
    currency_code: str,
) -> str:
    """Build a structured analysis prompt from all collected data."""
    sections = []

    sections.append(
        f"You are a senior stock analyst. Analyze the following data for {symbol} "
        f"({company_name}, currency: {currency_code}) and provide your independent "
        f"trading recommendation.\n"
    )

    # Technical indicators
    sections.append("## Technical Indicators")
    for k, v in ta_result.get("indicators", {}).items():
        if not k.startswith("_"):
            sections.append(f"  {k}: {v}")

    # Technical signals
    sections.append("\n## Technical Signals")
    for k, (direction, reason) in ta_result.get("signals", {}).items():
        if not k.startswith("_"):
            sections.append(f"  {k}: {direction} — {reason}")

    # Fundamental metrics
    sections.append("\n## Fundamental Metrics")
    for k, v in fa_result.get("metrics", {}).items():
        if not k.startswith("_"):
            sections.append(f"  {k}: {v}")

    # Fundamental signals
    sections.append("\n## Fundamental Signals")
    for k, (direction, reason) in fa_result.get("signals", {}).items():
        if not k.startswith("_"):
            sections.append(f"  {k}: {direction} — {reason}")

    # News headlines
    sections.append("\n## Recent News Headlines")
    for h in news_result.get("headlines", [])[:15]:
        score = h.get("sentiment_score", 0)
        sections.append(f"  [{score:+.3f}] {h['title']}")

    # Algorithmic result for context
    sections.append("\n## Algorithmic System Result (for reference)")
    sections.append(f"  Recommendation: {decision.get('recommendation', 'N/A')}")
    for k, v in decision.get("scores", {}).items():
        sections.append(f"  {k}: {v:+.2f}")

    sections.append(
        "\n---\n"
        "Based on ALL the data above, provide your independent analysis:\n"
        "1. **Recommendation**: STRONG BUY, BUY, HOLD, SELL, or STRONG SELL\n"
        "2. **Confidence**: High, Moderate, or Low\n"
        "3. **Reasoning**: Detailed analysis covering technical, fundamental, "
        "and sentiment factors (3-5 paragraphs)\n"
        "4. **Key Risks**: Top 3 risks to watch\n"
        "\nBe specific, reference the actual numbers, and be direct about your view."
    )

    return "\n".join(sections)


# ── API callers ──────────────────────────────────────────────────────────────

def _query_claude(config: LLMConfig, prompt: str) -> str:
    """Call Anthropic Messages API."""
    resp = requests.post(
        "https://api.anthropic.com/v1/messages",
        headers={
            "x-api-key": config.api_key,
            "anthropic-version": "2023-06-01",
            "content-type": "application/json",
        },
        json={
            "model": config.model,
            "max_tokens": 2048,
            "messages": [{"role": "user", "content": prompt}],
        },
        timeout=120,
    )
    resp.raise_for_status()
    data = resp.json()
    # Extract text from content blocks
    content = data.get("content", [])
    return "".join(block.get("text", "") for block in content if block.get("type") == "text")


def _query_openrouter(config: LLMConfig, prompt: str) -> str:
    """Call OpenRouter Chat Completions API."""
    resp = requests.post(
        "https://openrouter.ai/api/v1/chat/completions",
        headers={
            "Authorization": f"Bearer {config.api_key}",
            "content-type": "application/json",
        },
        json={
            "model": config.model,
            "max_tokens": 2048,
            "messages": [{"role": "user", "content": prompt}],
        },
        timeout=120,
    )
    resp.raise_for_status()
    data = resp.json()
    return data.get("choices", [{}])[0].get("message", {}).get("content", "")


def query_llm(config: LLMConfig, prompt: str) -> str:
    """Route to the appropriate provider API."""
    if config.provider == "claude":
        return _query_claude(config, prompt)
    elif config.provider == "openrouter":
        return _query_openrouter(config, prompt)
    else:
        raise ValueError(f"Unknown provider: {config.provider}")


# ── Orchestrator ─────────────────────────────────────────────────────────────

def run_llm_analysis(
    ta_result: dict,
    fa_result: dict,
    news_result: dict,
    decision: dict,
    symbol: str,
    company_name: str,
    currency_code: str,
    model: str | None = None,
) -> dict:
    """
    Run full LLM analysis: load config, select model, build prompt, query LLM.
    Returns {"model": ..., "provider": ..., "response": ...}.
    """
    config = load_config()
    llm_config = resolve_model(model, config)

    prompt = build_prompt(
        ta_result, fa_result, news_result, decision,
        symbol, company_name, currency_code,
    )

    response = query_llm(llm_config, prompt)

    return {
        "provider": llm_config.provider,
        "model": llm_config.model,
        "response": response,
    }
