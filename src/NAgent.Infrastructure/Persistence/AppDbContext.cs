using SqlSugar;

namespace NAgent.Infrastructure.Persistence;

/// <summary>
/// SqlSugar 数据库上下文
/// </summary>
public class AppDbContext : SqlSugarScope
{
    public AppDbContext(ConnectionConfig config) : base(config)
    {
        // 配置全局设置
        this.Aop.OnLogExecuting = (sql, pars) =>
        {
            Console.WriteLine($"[SqlSugar] SQL: {sql}");
        };

        this.Aop.OnError = (exp) =>
        {
            Console.WriteLine($"[SqlSugar] Error: {exp.Message}");
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
    }
}
