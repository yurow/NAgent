using Microsoft.Extensions.Logging;
using SqlSugar;

namespace NAgent.Infrastructure.Persistence;

/// <summary>
/// SqlSugar 数据库上下文
/// </summary>
public class AppDbContext : SqlSugarScope
{
    private readonly ILogger<AppDbContext>? _logger;

    public AppDbContext(ConnectionConfig config, ILogger<AppDbContext>? logger = null) : base(config)
    {
        _logger = logger;

        // 配置全局设置
        this.Aop.OnLogExecuting = (sql, pars) =>
        {
            _logger?.LogDebug("[SqlSugar] SQL: {Sql}", sql);
        };

        this.Aop.OnError = (exp) =>
        {
            _logger?.LogError(exp, "[SqlSugar] 执行 SQL 时发生错误");
        };
    }

    /// <summary>
    /// 用户表查询对象
    /// </summary>
    public ISugarQueryable<NAgent.Domain.Entities.User> Users => Queryable<NAgent.Domain.Entities.User>();

    /// <summary>
    /// 初始化数据库表结构
    /// </summary>
    public void InitializeDatabase()
    {
        // 创建表（如果不存在）
        CodeFirst.InitTables(typeof(NAgent.Domain.Entities.User));
        CodeFirst.InitTables(typeof(NAgent.AgentDomain.Entities.Project));
    }
}
