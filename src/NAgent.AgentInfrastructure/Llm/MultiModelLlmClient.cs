using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Enums;
using NAgent.AgentDomain.Services;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentInfrastructure.Llm;

/// <summary>
/// 多模型 LLM 客户端 - 支持 OpenAI 和 Anthropic 协议，支持多个提供商和模型
/// </summary>
public class MultiModelLlmClient : ILlmClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly LlmModelCacheService _cacheService;

    public MultiModelLlmClient(LlmModelCacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        // ⭐ 复用全局共享 HttpClient，避免每个 Scoped 实例创建独立 Handler 导致内存泄漏
        _httpClient = HttpClientManager.Default;
    }

    public async Task<string> GenerateAsync(
        string prompt, 
        string? modelId = null,
        double temperature = 0.7,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default,
        string? systemPrompt = null)
    {
        var (provider, model) = await ResolveModelAsync(modelId, cancellationToken);

        return provider.ProtocolType switch
        {
            LlmProtocolType.OpenAI => await CallOpenAiApiAsync(provider, model, prompt, temperature, maxTokens, cancellationToken, systemPrompt),
            LlmProtocolType.Anthropic => await CallAnthropicApiAsync(provider, model, prompt, temperature, maxTokens, cancellationToken, systemPrompt),
            _ => throw new NotSupportedException($"不支持的协议类型: {provider.ProtocolType}")
        };
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        string? modelId = null,
        double temperature = 0.7,
        int maxTokens = 2048,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default,
        string? systemPrompt = null)
    {
        var (provider, model) = await ResolveModelAsync(modelId, cancellationToken);

        var stream = provider.ProtocolType switch
        {
            LlmProtocolType.OpenAI => CallOpenAiStreamAsync(provider, model, prompt, temperature, maxTokens, cancellationToken, systemPrompt),
            LlmProtocolType.Anthropic => CallAnthropicStreamAsync(provider, model, prompt, temperature, maxTokens, cancellationToken, systemPrompt),
            _ => throw new NotSupportedException($"不支持的协议类型: {provider.ProtocolType}")
        };

        await foreach (var chunk in stream)
        {
            yield return chunk;
        }
    }

    public async Task<List<AvailableModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var providers = await _cacheService.GetAllEnabledProvidersAsync(cancellationToken);
        var availableModels = new List<AvailableModel>();

        foreach (var provider in providers)
        {
            foreach (var model in provider.Models.Where(m => m.IsEnabled))
            {
                availableModels.Add(new AvailableModel(
                    model.ModelId,
                    model.DisplayName,
                    provider.Name,
                    model.ContextWindowSize,
                    model.IsDefault
                ));
            }
        }

        return availableModels;
    }

    public async Task SetCurrentModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        await _cacheService.SetCurrentModelAsync(modelId, cancellationToken);
    }

    public async Task<string> GetCurrentModelAsync(CancellationToken cancellationToken = default)
    {
        var currentModel = await _cacheService.GetCurrentModelAsync(cancellationToken);
        return currentModel?.Model.ModelId ?? "default";
    }

    public void Dispose()
    {
        // ⭐ 不 Dispose 共享 HttpClient，它由 HttpClientManager 全局管理
    }

    #region Private Methods

    /// <summary>
    /// 解析模型配置
    /// </summary>
    private async Task<(LlmProvider Provider, LlmModel Model)> ResolveModelAsync(
        string? modelId, 
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(modelId))
        {
            var result = await _cacheService.ResolveModelAsync(modelId, cancellationToken);
            if (result.HasValue)
            {
                return result.Value;
            }
        }

        var currentModel = await _cacheService.GetCurrentModelAsync(cancellationToken);
        if (currentModel.HasValue)
        {
            return currentModel.Value;
        }

        var defaultResult = await _cacheService.ResolveModelAsync(null, cancellationToken);
        return defaultResult ?? throw new InvalidOperationException("没有找到可用的模型");
    }

    /// <summary>
    /// 调用 OpenAI 兼容 API
    /// </summary>
    private async Task<string> CallOpenAiApiAsync(
        LlmProvider provider,
        LlmModel model,
        string prompt,
        double temperature,
        int maxTokens,
        CancellationToken cancellationToken,
        string? systemPrompt = null)
    {
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });
        messages.Add(new { role = "user", content = prompt });

        var request = new
        {
            model = model.ModelId,
            messages = messages.ToArray(),
            temperature = temperature,
            max_tokens = Math.Min(maxTokens, model.MaxOutputTokens)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // OpenAI 兼容协议的 endpoint
        var url = $"{provider.BaseUrl.TrimEnd('/')}/chat/completions";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        requestMessage.Content = content;

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonDocument.Parse(responseBody);

        // 解析 OpenAI 响应格式
        return result.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    /// <summary>
    /// 调用 Anthropic Claude API
    /// </summary>
    private async Task<string> CallAnthropicApiAsync(
        LlmProvider provider,
        LlmModel model,
        string prompt,
        double temperature,
        int maxTokens,
        CancellationToken cancellationToken,
        string? systemPrompt = null)
    {
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = model.ModelId,
            ["messages"] = new[] { new { role = "user", content = prompt } },
            ["temperature"] = temperature,
            ["max_tokens"] = Math.Min(maxTokens, model.MaxOutputTokens)
        };
        if (!string.IsNullOrEmpty(systemPrompt))
            requestBody["system"] = systemPrompt;

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{provider.BaseUrl.TrimEnd('/')}/v1/messages";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.TryAddWithoutValidation("x-api-key", provider.ApiKey);
        requestMessage.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        requestMessage.Content = content;

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonDocument.Parse(responseBody);

        // 解析 Anthropic 响应格式
        return result.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    /// <summary>
    /// OpenAI 流式调用
    /// </summary>
    private async IAsyncEnumerable<string> CallOpenAiStreamAsync(
        LlmProvider provider,
        LlmModel model,
        string prompt,
        double temperature,
        int maxTokens,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        string? systemPrompt = null)
    {
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });
        messages.Add(new { role = "user", content = prompt });

        var request = new
        {
            model = model.ModelId,
            messages = messages.ToArray(),
            temperature = temperature,
            max_tokens = Math.Min(maxTokens, model.MaxOutputTokens),
            stream = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{provider.BaseUrl.TrimEnd('/')}/chat/completions";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        requestMessage.Content = content;

        var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                
                if (data == "[DONE]")
                    break;

                JsonDocument? jsonDoc = null;
                try
                {
                    jsonDoc = JsonDocument.Parse(data);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (jsonDoc != null)
                {
                    var choices = jsonDoc.RootElement.GetProperty("choices");
                    
                    if (choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentElement))
                        {
                            var contentText = contentElement.GetString();
                            if (!string.IsNullOrEmpty(contentText))
                            {
                                yield return contentText;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Anthropic 流式调用
    /// </summary>
    private async IAsyncEnumerable<string> CallAnthropicStreamAsync(
        LlmProvider provider,
        LlmModel model,
        string prompt,
        double temperature,
        int maxTokens,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        string? systemPrompt = null)
    {
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = model.ModelId,
            ["messages"] = new[] { new { role = "user", content = prompt } },
            ["temperature"] = temperature,
            ["max_tokens"] = Math.Min(maxTokens, model.MaxOutputTokens),
            ["stream"] = true
        };
        if (!string.IsNullOrEmpty(systemPrompt))
            requestBody["system"] = systemPrompt;

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{provider.BaseUrl.TrimEnd('/')}/v1/messages";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.TryAddWithoutValidation("x-api-key", provider.ApiKey);
        requestMessage.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        requestMessage.Content = content;

        var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                
                JsonDocument? jsonDoc = null;
                try
                {
                    jsonDoc = JsonDocument.Parse(data);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (jsonDoc != null)
                {
                    var type = jsonDoc.RootElement.GetProperty("type").GetString();
                    
                    if (type == "content_block_delta")
                    {
                        var delta = jsonDoc.RootElement.GetProperty("delta");
                        if (delta.TryGetProperty("text", out var textElement))
                        {
                            var text = textElement.GetString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                yield return text;
                            }
                        }
                    }
                    else if (type == "message_stop")
                    {
                        break;
                    }
                }
            }
        }
    }

    #endregion
}