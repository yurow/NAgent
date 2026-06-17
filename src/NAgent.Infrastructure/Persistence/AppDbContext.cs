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

        // 知识图谱表：检查 Description 列是否允许 NULL，如果不允许则删除重建
        EnsureKgTablesCorrect();
    }

    /// <summary>
    /// 确保知识图谱表结构正确（Description 列可空）
    /// </summary>
    private void EnsureKgTablesCorrect()
    {
        var nodeTableName = this.EntityMaintenance.GetEntityInfo<NAgent.AgentDomain.Entities.KnowledgeGraphNode>().DbTableName;
        var edgeTableName = this.EntityMaintenance.GetEntityInfo<NAgent.AgentDomain.Entities.KnowledgeGraphEdge>().DbTableName;

        // 检查 Node 表的 Description 列是否可空
        var nodeColumns = this.DbMaintenance.GetColumnInfosByTableName(nodeTableName, false);
        var descColumn = nodeColumns.FirstOrDefault(c => c.DbColumnName.Equals("Description", StringComparison.OrdinalIgnoreCase));

        bool needRebuild = false;
        if (descColumn != null && descColumn.IsNullable == false)
        {
            needRebuild = true;
        }

        // 检查 Edge 表的 Description 列是否可空
        var edgeColumns = this.DbMaintenance.GetColumnInfosByTableName(edgeTableName, false);
        var edgeDescColumn = edgeColumns.FirstOrDefault(c => c.DbColumnName.Equals("Description", StringComparison.OrdinalIgnoreCase));
        if (edgeDescColumn != null && edgeDescColumn.IsNullable == false)
        {
            needRebuild = true;
        }

        if (needRebuild)
        {
            _logger?.LogWarning("[KG] 知识图谱表结构需要重建（Description 列不可空），正在删除旧表...");
            this.DbMaintenance.DropTable<NAgent.AgentDomain.Entities.KnowledgeGraphEdge>();
            this.DbMaintenance.DropTable<NAgent.AgentDomain.Entities.KnowledgeGraphNode>();
        }

        // 创建表（如果不存在则创建，已删除则重建）
        CodeFirst.InitTables(typeof(NAgent.AgentDomain.Entities.KnowledgeGraphNode));
        CodeFirst.InitTables(typeof(NAgent.AgentDomain.Entities.KnowledgeGraphEdge));
    }
}
