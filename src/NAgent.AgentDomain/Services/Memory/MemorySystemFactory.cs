using Microsoft.Extensions.DependencyInjection;

namespace NAgent.AgentDomain.Services.Memory;

/// <summary>
/// 记忆系统工厂实现
/// </summary>
public class MemorySystemFactory : IMemorySystemFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _memorySystemTypes;

    public MemorySystemFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _memorySystemTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "default", typeof(DefaultMemorySystem) },
            { "file", typeof(DefaultMemorySystem) }
        };
    }

    public IMemorySystem CreateMemorySystem(string memoryType = "default")
    {
        if (!_memorySystemTypes.TryGetValue(memoryType, out var systemType))
        {
            throw new ArgumentException($"不支持的记忆系统类型: {memoryType}", nameof(memoryType));
        }

        var storage = _serviceProvider.GetRequiredService<IMemoryStorage>();
        var projectMemoryRepo = _serviceProvider.GetRequiredService<NAgent.AgentDomain.Repositories.IProjectMemoryRepository>();
        return (IMemorySystem)Activator.CreateInstance(systemType, storage, projectMemoryRepo)!;
    }

    public IEnumerable<string> GetAvailableMemoryTypes()
    {
        return _memorySystemTypes.Keys;
    }

    /// <summary>
    /// 注册自定义记忆系统
    /// </summary>
    public void RegisterMemorySystem(string memoryType, Type systemType)
    {
        if (!typeof(IMemorySystem).IsAssignableFrom(systemType))
        {
            throw new ArgumentException($"类型 {systemType.Name} 必须实现 IMemorySystem 接口", nameof(systemType));
        }

        _memorySystemTypes[memoryType] = systemType;
    }
}
