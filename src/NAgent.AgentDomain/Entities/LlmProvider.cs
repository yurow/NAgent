using SqlSugar;
using NAgent.AgentDomain.Enums;

namespace NAgent.AgentDomain.Entities;

/// <summary>
/// LLM 提供商配置实体 - 一个 API Key 和端点可以对应多个模型
/// </summary>
[SugarTable("LlmProviders")]
public class LlmProvider
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public Guid Id { get; private set; }
    
    [SugarColumn(Length = 100)]
    public string Name { get; private set; }
    
    public LlmProtocolType ProtocolType { get; private set; }
    
    [SugarColumn(Length = 500)]
    public string BaseUrl { get; private set; }
    
    [SugarColumn(Length = 200)]
    public string ApiKey { get; private set; }
    
    public bool IsEnabled { get; private set; }
    
    [SugarColumn(IsIgnore = true)]
    public List<LlmModel> Models { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAt { get; private set; }

    public LlmProvider() 
    {
        Models = new List<LlmModel>();
    }

    public LlmProvider(string name, LlmProtocolType protocolType, string baseUrl, string apiKey)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ProtocolType = protocolType;
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        IsEnabled = true;
        Models = new List<LlmModel>();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加模型到此提供商
    /// </summary>
    public void AddModel(LlmModel model)
    {
        if (Models.Any(m => m.ModelId == model.ModelId))
        {
            throw new InvalidOperationException($"模型 {model.ModelId} 已存在");
        }
        
        Models.Add(model);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 移除模型
    /// </summary>
    public void RemoveModel(string modelId)
    {
        var model = Models.FirstOrDefault(m => m.ModelId == modelId);
        if (model != null)
        {
            Models.Remove(model);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 获取指定模型
    /// </summary>
    public LlmModel? GetModel(string modelId)
    {
        return Models.FirstOrDefault(m => m.ModelId == modelId);
    }

    /// <summary>
    /// 启用/禁用提供商
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新 API Key
    /// </summary>
    public void UpdateApiKey(string apiKey)
    {
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新基础 URL
    /// </summary>
    public void UpdateBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新名称
    /// </summary>
    public void UpdateName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新协议类型
    /// </summary>
    public void UpdateProtocolType(LlmProtocolType protocolType)
    {
        ProtocolType = protocolType;
        UpdatedAt = DateTime.UtcNow;
    }
}