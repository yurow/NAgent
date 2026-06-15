using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NAgent.Application.Interfaces;
using NAgent.Domain.Repositories;
using NAgent.AgentDomain.Repositories;
using NAgent.Infrastructure.Persistence;
using NAgent.Infrastructure.Repositories;
using NAgent.Infrastructure.Services;
using SqlSugar;

namespace NAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ⭐ 配置 SQLite 数据库连接
        var dbPath = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=nagent.db";

        // 注册 SqlSugar 客户端（Scoped，供所有层共享）
        services.AddScoped<ISqlSugarClient>(sp =>
        {
            var config = new ConnectionConfig()
            {
                ConnectionString = dbPath,
                DbType = DbType.Sqlite,
                InitKeyType = InitKeyType.Attribute, // 从特性读取主键和表名
                IsAutoCloseConnection = true, // 自动释放连接
                MoreSettings = new ConnMoreSettings()
                {
                    SqlServerCodeFirstNvarchar = true // SQLite 使用 NVARCHAR
                }
            };

            var logger = sp.GetService<ILogger<AppDbContext>>();
            var context = new AppDbContext(config, logger);
            
            // 初始化数据库表结构
            context.InitializeDatabase();
            
            return context;
        });

        // 注册数据库上下文（Scoped）
        services.AddScoped<AppDbContext>(sp => 
            (AppDbContext)sp.GetRequiredService<ISqlSugarClient>());

        // 注册仓储
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, SqliteProjectRepository>();

        // 注册初始化服务（Scoped，因为依赖了 Scoped 的 UserRepository）
        services.AddScoped<IInitializationService, InitializationService>();

        // 注册 JWT Token 服务（Singleton）
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // 注册工作空间管理器（Singleton）
        services.AddSingleton<IWorkspaceManager>(sp =>
        {
            var workspacePath = configuration["Workspace:BasePath"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NAgent", "workspace");
            return new WorkspaceManager(workspacePath);
        });

        // 注册密码哈希服务（Singleton）
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();

        return services;
    }
}