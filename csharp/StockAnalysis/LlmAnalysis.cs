using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace StockAnalysis;

/// <summary>
/// Sends all analysis data to an LLM (Claude or OpenRouter) for an independent recommendation.
/// </summary>
public class LlmAnalysis
{
    public record LlmConfig(string Provider, string Model, string ApiKey);

    public record LlmResult(string Provider, string Model, string Response);

    private static readonly string ConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".stock-analysis.json");

    // ── Model definitions ────────────────────────────────────────────────

    private static readonly (string Id, string Label)[] ClaudeModels =
    {
        ("claude-sonnet-4-5-20250929", "Claude Sonnet 4.5 (Recommended)"),
        ("claude-opus-4-6", "Claude Opus 4.6"),
        ("claude-haiku-4-5-20251001", "Claude Haiku 4.5"),
    };

    private static readonly (string Id, string Label)[] OpenRouterModels =
    {
        ("anthropic/claude-sonnet-4-5-20250929", "Claude Sonnet 4.5"),
        ("google/gemini-2.5-pro-preview", "Gemini 2.5 Pro"),
        ("google/gemini-2.0-flash-001", "Gemini 2.0 Flash"),
        ("openai/gpt-4o", "GPT-4o"),
        ("openai/o3-mini", "OpenAI o3-mini"),
        ("meta-llama/llama-4-maverick", "Llama 4 Maverick"),
        ("deepseek/deepseek-r1", "DeepSeek R1"),
        ("mistralai/mistral-large-2411", "Mistral Large"),
    };

    // ── Config file management ───────────────────────────────────────────

    private static Dictionary<string, string> LoadConfig()
    {
        if (!File.Exists(ConfigPath)) return new();
        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch { return new(); }
    }

    private static void SaveConfig(Dictionary<string, string> config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json + "\n");
    }

    private static string PromptApiKey(string provider, Dictionary<string, string> config)
    {
        var key = $"{provider}_api_key";
        if (config.TryGetValue(key, out var existing) && !string.IsNullOrEmpty(existing))
            return existing;

        Console.WriteLine($"\n  No {provider.ToUpper()} API key found in {ConfigPath}");
        Console.Write($"  Enter your {provider.ToUpper()} API key: ");
        var apiKey = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("  Error: API key is required.");
            Environment.Exit(1);
        }

        config[key] = apiKey;
        SaveConfig(config);
        Console.WriteLine($"  Key saved to {ConfigPath}");
        return apiKey;
    }

    // ── Interactive model selection ──────────────────────────────────────

    public static LlmConfig SelectModelInteractive(Dictionary<string, string> config)
    {
        Console.WriteLine("\n  Select LLM provider:");
        Console.WriteLine("    1. Claude (Anthropic API)");
        Console.WriteLine("    2. OpenRouter (GPT-4, Gemini, Llama, etc.)");

        Console.Write("\n  Provider [1]: ");
        var choice = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(choice)) choice = "1";

        string provider;
        (string Id, string Label)[] models;
        if (choice == "2")
        {
            provider = "openrouter";
            models = OpenRouterModels;
        }
        else
        {
            provider = "claude";
            models = ClaudeModels;
        }

        Console.WriteLine("\n  Select model:");
        for (int i = 0; i < models.Length; i++)
            Console.WriteLine($"    {i + 1}. {models[i].Label}  ({models[i].Id})");

        Console.Write("\n  Model [1]: ");
        var modelChoice = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(modelChoice)) modelChoice = "1";

        string modelId;
        if (int.TryParse(modelChoice, out int idx) && idx >= 1 && idx <= models.Length)
            modelId = models[idx - 1].Id;
        else
            modelId = modelChoice; // allow typing a model ID directly

        var apiKey = PromptApiKey(provider, config);
        return new LlmConfig(provider, modelId, apiKey);
    }

    public static LlmConfig ResolveModel(string? modelStr, Dictionary<string, string> config)
    {
        if (string.IsNullOrEmpty(modelStr))
            return SelectModelInteractive(config);

        string provider, modelId;
        if (modelStr.StartsWith("openrouter/"))
        {
            provider = "openrouter";
            modelId = modelStr["openrouter/".Length..];
        }
        else if (modelStr.StartsWith("claude-"))
        {
            provider = "claude";
            modelId = modelStr;
        }
        else if (modelStr.Contains('/'))
        {
            provider = "openrouter";
            modelId = modelStr;
        }
        else
        {
            provider = "claude";
            modelId = modelStr;
        }

        var apiKey = PromptApiKey(provider, config);
        return new LlmConfig(provider, modelId, apiKey);
    }

    // ── Prompt builder ──────────────────────────────────────────────────

    public static string BuildPrompt(
        AnalysisResult taResult,
        AnalysisResult faResult,
        NewsResult newsResult,
        DecisionResult decision,
        string symbol,
        string companyName,
        string currencyCode)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"You are a senior stock analyst. Analyze the following data for {symbol} " +
            $"({companyName}, currency: {currencyCode}) and provide your independent trading recommendation.\n");

        // Technical indicators
        sb.AppendLine("## Technical Indicators");
        foreach (var (k, v) in taResult.Metrics)
        {
            if (!k.StartsWith("_"))
                sb.AppendLine($"  {k}: {v}");
        }

        // Technical signals
        sb.AppendLine("\n## Technical Signals");
        foreach (var (k, signal) in taResult.Signals)
        {
            if (!k.StartsWith("_"))
                sb.AppendLine($"  {k}: {signal.Direction} — {signal.Reason}");
        }

        // Fundamental metrics
        sb.AppendLine("\n## Fundamental Metrics");
        foreach (var (k, v) in faResult.Metrics)
        {
            if (!k.StartsWith("_"))
                sb.AppendLine($"  {k}: {v}");
        }

        // Fundamental signals
        sb.AppendLine("\n## Fundamental Signals");
        foreach (var (k, signal) in faResult.Signals)
        {
            if (!k.StartsWith("_"))
                sb.AppendLine($"  {k}: {signal.Direction} — {signal.Reason}");
        }

        // News headlines
        sb.AppendLine("\n## Recent News Headlines");
        foreach (var h in newsResult.Headlines.Take(15))
            sb.AppendLine($"  [{h.SentimentScore:+0.000;-0.000}] {h.Title}");

        // Algorithmic result
        sb.AppendLine("\n## Algorithmic System Result (for reference)");
        sb.AppendLine($"  Recommendation: {decision.Recommendation}");
        foreach (var (k, v) in decision.Scores)
            sb.AppendLine($"  {k}: {v:+0.00;-0.00}");

        sb.AppendLine(
            """

            ---
            Based on ALL the data above, provide your independent analysis:
            1. **Recommendation**: STRONG BUY, BUY, HOLD, SELL, or STRONG SELL
            2. **Confidence**: High, Moderate, or Low
            3. **Reasoning**: Detailed analysis covering technical, fundamental, and sentiment factors (3-5 paragraphs)
            4. **Key Risks**: Top 3 risks to watch

            Be specific, reference the actual numbers, and be direct about your view.
            """);

        return sb.ToString();
    }

    // ── API callers ─────────────────────────────────────────────────────

    private static readonly HttpClient Http = new();

    private static async Task<string> QueryClaude(LlmConfig config, string prompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", config.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = config.Model,
            max_tokens = 2048,
            messages = new[] { new { role = "user", content = prompt } },
        };
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await Http.SendAsync(request);
        resp.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
        var content = json?["content"]?.AsArray();
        if (content == null) return "";

        var sb = new StringBuilder();
        foreach (var block in content)
        {
            if (block?["type"]?.GetValue<string>() == "text")
                sb.Append(block["text"]?.GetValue<string>() ?? "");
        }
        return sb.ToString();
    }

    private static async Task<string> QueryOpenRouter(LlmConfig config, string prompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

        var body = new
        {
            model = config.Model,
            max_tokens = 2048,
            messages = new[] { new { role = "user", content = prompt } },
        };
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await Http.SendAsync(request);
        resp.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
        return json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";
    }

    public static async Task<string> QueryLlm(LlmConfig config, string prompt)
    {
        return config.Provider switch
        {
            "claude" => await QueryClaude(config, prompt),
            "openrouter" => await QueryOpenRouter(config, prompt),
            _ => throw new ArgumentException($"Unknown provider: {config.Provider}"),
        };
    }

    // ── Orchestrator ────────────────────────────────────────────────────

    public static async Task<LlmResult> RunAsync(
        AnalysisResult taResult,
        AnalysisResult faResult,
        NewsResult newsResult,
        DecisionResult decision,
        string symbol,
        string companyName,
        string currencyCode,
        string? model = null)
    {
        var config = LoadConfig();
        var llmConfig = ResolveModel(model, config);

        var prompt = BuildPrompt(taResult, faResult, newsResult, decision,
            symbol, companyName, currencyCode);

        var response = await QueryLlm(llmConfig, prompt);

        return new LlmResult(llmConfig.Provider, llmConfig.Model, response);
    }
}
