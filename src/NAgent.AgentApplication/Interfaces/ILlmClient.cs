namespace NAgent.AgentApplication.Interfaces;

/// <summary>
/// LLM 客户端接口 - 抽象不同 LLM 提供商和模型
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// 生成文本回复
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="modelId">模型 ID（可选，使用默认模型）</param>
    /// <param name="temperature">温度参数（0-2）</param>
    /// <param name="maxTokens">最大输出 token 数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<string> GenerateAsync(
        string prompt, 
        string? modelId = null,
        double temperature = 0.7,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default,
        string? systemPrompt = null);

    /// <summary>
    /// 流式生成文本
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="modelId">模型 ID（可选，使用默认模型）</param>
    /// <param name="temperature">温度参数（0-2）</param>
    /// <param name="maxTokens">最大输出 token 数</param>
    /// <param name="cancellationToken">取消令牌</param>
    IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        string? modelId = null,
        double temperature = 0.7,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default,
        string? systemPrompt = null);

    /// <summary>
    /// 获取可用的模型列表
    /// </summary>
    Task<List<AvailableModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置当前使用的模型
    /// </summary>
    Task SetCurrentModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前使用的模型
    /// </summary>
    Task<string> GetCurrentModelAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 可用模型信息
/// </summary>
public record AvailableModel(
    string ModelId,
    string DisplayName,
    string ProviderName,
    int ContextWindowSize,
    bool IsDefault);