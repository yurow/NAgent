using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Enums;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Services;

/// <summary>
/// LLM 配置初始化服务 - 从配置文件加载提供商和模型数据
/// </summary>
public class LlmConfigurationInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ILlmProviderRepository _providerRepository;
    private readonly ILlmModelRepository _modelRepository;
    private readonly ILogger<LlmConfigurationInitializer> _logger;

    public LlmConfigurationInitializer(
        IConfiguration configuration,
        ILlmProviderRepository providerRepository,
        ILlmModelRepository modelRepository,
        ILogger<LlmConfigurationInitializer> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 初始化 LLM 配置（从配置文件加载到数据库）
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var providersSection = _configuration.GetSection("LlmProviders");
            if (!providersSection.Exists())
            {
                _logger.LogInformation("未找到 LlmProviders 配置，跳过初始化");
                return;
            }

            var providerConfigs = providersSection.Get<List<LlmProviderConfig>>();
            if (providerConfigs == null || providerConfigs.Count == 0)
            {
                _logger.LogInformation("LlmProviders 配置为空，跳过初始化");
                return;
            }

            _logger.LogInformation("开始初始化 LLM 配置，共 {Count} 个提供商", providerConfigs.Count);

            foreach (var providerConfig in providerConfigs)
            {
                await InitializeProviderAsync(providerConfig, cancellationToken);
            }

            _logger.LogInformation("LLM 配置初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM 配置初始化失败");
            throw;
        }
    }

    private async Task InitializeProviderAsync(LlmProviderConfig config, CancellationToken cancellationToken)
    {
        try
        {
            var protocolType = ParseProtocolType(config.ProtocolType);
            
            var existingProvider = await _providerRepository.GetByNameAsync(config.Name, cancellationToken);
            
            LlmProvider provider;
            if (existingProvider != null)
            {
                _logger.LogInformation("提供商 {Name} 已存在，更新配置", config.Name);
                existingProvider.UpdateBaseUrl(config.BaseUrl);
                existingProvider.UpdateApiKey(config.ApiKey);
                await _providerRepository.UpdateAsync(existingProvider, cancellationToken);
                provider = existingProvider;
            }
            else
            {
                _logger.LogInformation("创建新提供商 {Name}", config.Name);
                provider = new LlmProvider(config.Name, protocolType, config.BaseUrl, config.ApiKey);
                await _providerRepository.AddAsync(provider, cancellationToken);
            }

            await InitializeModelsAsync(provider, config.Models, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化提供商 {Name} 失败", config.Name);
            throw;
        }
    }

    private async Task InitializeModelsAsync(LlmProvider provider, List<LlmModelConfig> modelConfigs, CancellationToken cancellationToken)
    {
        if (modelConfigs == null || modelConfigs.Count == 0)
            return;

        _logger.LogInformation("为提供商 {ProviderName} 初始化 {Count} 个模型", provider.Name, modelConfigs.Count);

        foreach (var modelConfig in modelConfigs)
        {
            try
            {
                var existingModel = await _modelRepository.GetByModelIdAsync(modelConfig.ModelId, provider.Id, cancellationToken);
                
                if (existingModel != null)
                {
                    _logger.LogDebug("模型 {ModelId} 已存在，跳过", modelConfig.ModelId);
                    continue;
                }

                var model = new LlmModel(
                    modelConfig.ModelId,
                    modelConfig.DisplayName,
                    modelConfig.ContextWindowSize,
                    modelConfig.MaxOutputTokens,
                    modelConfig.DefaultTemperature,
                    provider.Id
                );

                if (modelConfig.IsDefault)
                {
                    model.SetAsDefault();
                }

                await _modelRepository.AddAsync(model, cancellationToken);
                
                provider.AddModel(model);
                await _providerRepository.UpdateAsync(provider, cancellationToken);

                _logger.LogInformation("添加模型 {ModelId} 到提供商 {ProviderName}", modelConfig.ModelId, provider.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化模型 {ModelId} 失败", modelConfig.ModelId);
            }
        }
    }

    private LlmProtocolType ParseProtocolType(string protocolType)
    {
        return protocolType.ToLowerInvariant() switch
        {
            "openai" => LlmProtocolType.OpenAI,
            "anthropic" => LlmProtocolType.Anthropic,
            _ => throw new ArgumentException($"不支持的协议类型: {protocolType}")
        };
    }
}

/// <summary>
/// LLM 提供商配置
/// </summary>
public class LlmProviderConfig
{
    public string Name { get; set; } = string.Empty;
    public string ProtocolType { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public List<LlmModelConfig> Models { get; set; } = new();
}

/// <summary>
/// LLM 模型配置
/// </summary>
public class LlmModelConfig
{
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ContextWindowSize { get; set; }
    public int MaxOutputTokens { get; set; } = 2048;
    public double DefaultTemperature { get; set; } = 0.7;
    public bool IsDefault { get; set; } = false;
}