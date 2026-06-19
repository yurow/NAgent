using NAgent.AgentApplication.Interfaces;

namespace NAgent.AgentInfrastructure.Llm;

/// <summary>
/// 基于 LLamaSharp 的本地 LLM 客户端实现（已废弃，使用 MultiModelLlmClient）
/// </summary>
[Obsolete("请使用 MultiModelLlmClient")]
public class LocalLlmClientImpl : ILlmClient
{
    private readonly string _modelPath;
    private string _currentModelId = "local-llama";
    // TODO: LLamaSharp 模型实例

    public LocalLlmClientImpl(string modelPath)
    {
        _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
        // TODO: 加载模型
    }

    public async Task<string> GenerateAsync(
        string prompt, 
        string? modelId = null,
        double temperature = 0.7,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default,
        string? systemPrompt = null)
    {
        // TODO: 使用 LLamaSharp 生成文本
        await Task.Delay(100, cancellationToken);
        return "这是来自本地模型的回复示例";
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        string? modelId = null,
        double temperature = 0.7,
        int maxTokens = 2048,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default,
        string? systemPrompt = null)
    {
        // TODO: 实现流式生成
        yield return "流式";
        yield return "生成";
        yield return "示例";
    }

    public Task<List<AvailableModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<AvailableModel>
        {
            new AvailableModel("local-llama", "Local Llama", "Local", 4096, true)
        });
    }

    public Task SetCurrentModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        _currentModelId = modelId;
        return Task.CompletedTask;
    }

    public Task<string> GetCurrentModelAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentModelId);
    }
}