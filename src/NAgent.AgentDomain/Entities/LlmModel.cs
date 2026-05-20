using SqlSugar;

namespace NAgent.AgentDomain.Entities;

/// <summary>
/// LLM 模型实体 - 属于某个提供商的具体模型
/// </summary>
[SugarTable("LlmModels")]
public class LlmModel
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public Guid Id { get; private set; }
    
    [SugarColumn(Length = 100)]
    public string ModelId { get; private set; }
    
    [SugarColumn(Length = 200)]
    public string DisplayName { get; private set; }
    
    public int ContextWindowSize { get; private set; }
    public int MaxOutputTokens { get; private set; }
    public double DefaultTemperature { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsEnabled { get; private set; }
    public Guid ProviderId { get; private set; }
    public long TotalTokenUsage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAt { get; private set; }

    public LlmModel() { }

    public LlmModel(
        string modelId, 
        string displayName, 
        int contextWindowSize,
        int maxOutputTokens = 2048,
        double defaultTemperature = 0.7,
        Guid providerId = default)
    {
        Id = Guid.NewGuid();
        ModelId = modelId ?? throw new ArgumentNullException(nameof(modelId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ContextWindowSize = contextWindowSize;
        MaxOutputTokens = maxOutputTokens;
        DefaultTemperature = defaultTemperature;
        IsDefault = false;
        IsEnabled = true;
        TotalTokenUsage = 0;
        ProviderId = providerId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 增加 Token 使用量
    /// </summary>
    public void AddTokenUsage(long tokens)
    {
        if (tokens < 0)
        {
            throw new ArgumentException("Token 使用量不能为负数", nameof(tokens));
        }
        
        TotalTokenUsage += tokens;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置为默认模型
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 取消默认设置
    /// </summary>
    public void UnsetAsDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用/禁用模型
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新上下文窗口大小
    /// </summary>
    public void UpdateContextWindowSize(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentException("上下文窗口大小必须大于 0", nameof(size));
        }
        
        ContextWindowSize = size;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新最大输出 token 数
    /// </summary>
    public void UpdateMaxOutputTokens(int tokens)
    {
        if (tokens <= 0)
        {
            throw new ArgumentException("最大输出 token 数必须大于 0", nameof(tokens));
        }
        
        MaxOutputTokens = tokens;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新默认温度参数
    /// </summary>
    public void UpdateDefaultTemperature(double temperature)
    {
        if (temperature < 0 || temperature > 2)
        {
            throw new ArgumentException("温度参数必须在 0-2 之间", nameof(temperature));
        }
        
        DefaultTemperature = temperature;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// LLM 模型每日使用统计实体
/// </summary>
[SugarTable("LlmModelDailyUsages")]
public class LlmModelDailyUsage
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public Guid Id { get; private set; }
    public Guid ModelId { get; private set; }
    public DateTime UsageDate { get; private set; }
    public long TotalTokens { get; private set; }
    public int RequestCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAt { get; private set; }

    public LlmModelDailyUsage() { }

    public LlmModelDailyUsage(Guid modelId, DateTime usageDate)
    {
        Id = Guid.NewGuid();
        ModelId = modelId;
        UsageDate = usageDate.Date;
        TotalTokens = 0;
        RequestCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 增加 Token 使用量和请求次数
    /// </summary>
    public void AddUsage(long tokens)
    {
        if (tokens < 0)
        {
            throw new ArgumentException("Token 使用量不能为负数", nameof(tokens));
        }
        
        TotalTokens += tokens;
        RequestCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}