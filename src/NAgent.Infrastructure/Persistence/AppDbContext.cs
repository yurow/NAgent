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

        // 知识图谱表：如果表已存在但结构不对，先删除再重建
        try
        {
            CodeFirst.InitTables(typeof(NAgent.AgentDomain.Entities.KnowledgeGraphNode));
        }
        catch
        {
            // 表结构不对，删除后重建
            this.DbMaintenance.DropTable<NAgent.AgentDomain.Entities.KnowledgeGraphEdge>();
            this.DbMaintenance.DropTable<NAgent.AgentDomain.Entities.KnowledgeGraphNode>();
            CodeFirst.InitTables(typeof(NAgent.AgentDomain.Entities.KnowledgeGraphNode));
        }

        try
        {
            CodeFirst.InitTables(typeof(NAgent.AgentDomain.Entities.KnowledgeGraphEdge));
        }
        catch
        {
            this.DbMaintenance.DropTable<NAgent.AgentDomain.Entities.KnowledgeGraphEdge>();
            CodeFirst.InitTables(typeof(NAgent.AgentDomain.Entities.KnowledgeGraphEdge));
        }
    }
}
