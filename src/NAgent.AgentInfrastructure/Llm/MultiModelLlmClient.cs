using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Enums;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Llm;

/// <summary>
/// 多模型 LLM 客户端 - 支持 OpenAI 和 Anthropic 协议，支持多个提供商和模型
/// </summary>
public class MultiModelLlmClient : ILlmClient, IDisposable
{
    private readonly ILlmProviderRepository _providerRepository;
    private readonly ILlmModelRepository _modelRepository;
    private readonly HttpClient _httpClient;
    private string? _currentModelId;
    private LlmProvider? _currentProvider;
    private LlmModel? _currentModel;

    public MultiModelLlmClient(
        ILlmProviderRepository providerRepository,
        ILlmModelRepository modelRepository)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateAsync(
        string prompt, 
        string? modelId = null,
        double temperature = 0.7,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default)
    {
        // 1. 确定使用的模型
        var (provider, model) = await ResolveModelAsync(modelId, cancellationToken);

        // 2. 根据协议类型调用不同的 API
        return provider.ProtocolType switch
        {
            LlmProtocolType.OpenAI => await CallOpenAiApiAsync(provider, model, prompt, temperature, maxTokens, cancellationToken),
            LlmProtocolType.Anthropic => await CallAnthropicApiAsync(provider, model, prompt, temperature, maxTokens, cancellationToken),
            _ => throw new NotSupportedException($"不支持的协议类型: {provider.ProtocolType}")
        };
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        string? modelId = null,
        double temperature = 0.7,
        int maxTokens = 2048,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (provider, model) = await ResolveModelAsync(modelId, cancellationToken);

        var stream = provider.ProtocolType switch
        {
            LlmProtocolType.OpenAI => CallOpenAiStreamAsync(provider, model, prompt, temperature, maxTokens, cancellationToken),
            LlmProtocolType.Anthropic => CallAnthropicStreamAsync(provider, model, prompt, temperature, maxTokens, cancellationToken),
            _ => throw new NotSupportedException($"不支持的协议类型: {provider.ProtocolType}")
        };

        await foreach (var chunk in stream)
        {
            yield return chunk;
        }
    }

    public async Task<List<AvailableModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var providers = await _providerRepository.GetAllEnabledAsync(cancellationToken);
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

    public void SetCurrentModel(string modelId)
    {
        _currentModelId = modelId;
        _currentProvider = null;
        _currentModel = null;
    }

    public string GetCurrentModel()
    {
        return _currentModelId ?? "default";
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    #region Private Methods

    /// <summary>
    /// 解析模型配置
    /// </summary>
    private async Task<(LlmProvider Provider, LlmModel Model)> ResolveModelAsync(
        string? modelId, 
        CancellationToken cancellationToken)
    {
        // 如果指定了模型 ID，使用指定的
        if (!string.IsNullOrEmpty(modelId))
        {
            // 如果指定的模型ID与当前缓存的不同，清除缓存
            if (modelId != _currentModelId)
            {
                _currentModelId = modelId;
                _currentProvider = null;
                _currentModel = null;
            }
        }
        else
        {
            // 如果没有指定模型ID，清除缓存以重新查找默认模型
            _currentModelId = null;
            _currentProvider = null;
            _currentModel = null;
        }

        // 如果已经缓存了当前模型，直接返回
        if (_currentProvider != null && _currentModel != null)
        {
            return (_currentProvider, _currentModel);
        }

        // 获取所有启用的提供商
        var providers = await _providerRepository.GetAllEnabledAsync(cancellationToken);
        
        if (!providers.Any())
        {
            throw new InvalidOperationException("没有可用的 LLM 提供商");
        }

        // 查找模型
        LlmModel? model = null;
        LlmProvider? provider = null;

        if (!string.IsNullOrEmpty(_currentModelId))
        {
            // 查找指定的模型（跨所有提供商）
            foreach (var p in providers)
            {
                model = p.GetModel(_currentModelId);
                if (model != null && model.IsEnabled)
                {
                    provider = p;
                    break;
                }
            }
        }

        // 如果没有找到指定模型或没有指定，使用全局默认模型
        if (model == null)
        {
            // 在所有提供商中查找全局默认模型
            foreach (var p in providers)
            {
                model = p.Models.FirstOrDefault(m => m.IsDefault && m.IsEnabled);
                if (model != null)
                {
                    provider = p;
                    break;
                }
            }
        }

        // 如果还没有找到，使用第一个启用的模型
        if (model == null)
        {
            provider = providers.First();
            model = provider.Models.FirstOrDefault(m => m.IsEnabled);
        }

        if (model == null || provider == null)
        {
            throw new InvalidOperationException("没有找到可用的模型");
        }

        _currentProvider = provider;
        _currentModel = model;

        return (provider, model);
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
        CancellationToken cancellationToken)
    {
        var request = new
        {
            model = model.ModelId,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = temperature,
            max_tokens = Math.Min(maxTokens, model.MaxOutputTokens)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // OpenAI 兼容协议的 endpoint
        var url = $"{provider.BaseUrl.TrimEnd('/')}/v1/chat/completions";

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
        CancellationToken cancellationToken)
    {
        var request = new
        {
            model = model.ModelId,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = temperature,
            max_tokens = Math.Min(maxTokens, model.MaxOutputTokens)
        };

        var json = JsonSerializer.Serialize(request);
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
    /// OpenAI 流式调用（骨架实现）
    /// </summary>
    private async IAsyncEnumerable<string> CallOpenAiStreamAsync(
        LlmProvider provider,
        LlmModel model,
        string prompt,
        double temperature,
        int maxTokens,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // TODO: 实现 OpenAI SSE 流式响应解析
        yield return "流式响应示例";
    }

    /// <summary>
    /// Anthropic 流式调用（骨架实现）
    /// </summary>
    private async IAsyncEnumerable<string> CallAnthropicStreamAsync(
        LlmProvider provider,
        LlmModel model,
        string prompt,
        double temperature,
        int maxTokens,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // TODO: 实现 Anthropic SSE 流式响应解析
        yield return "流式响应示例";
    }

    #endregion
}