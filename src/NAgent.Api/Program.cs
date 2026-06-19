using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NAgent.Api.Middlewares;
using NAgent.Application.Mappings;
using NAgent.Infrastructure;
using NAgent.AgentApplication;
using NAgent.AgentInfrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ⭐ 添加内存缓存服务
builder.Services.AddMemoryCache();

// ⭐ 配置 JWT 认证服务
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
// 优先从环境变量读取 SecretKey，其次从配置读取
var secretKey = Environment.GetEnvironmentVariable("NAGENT_JWT_SECRET_KEY")
    ?? jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey 未配置。请设置环境变量 NAGENT_JWT_SECRET_KEY 或在配置中指定。");
var issuer = jwtSettings["Issuer"] ?? "NAgent";
var audience = jwtSettings["Audience"] ?? "NAgent.Api";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 添加Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NAgent API", Version = "v1" });
    c.SwaggerDoc("agent", new() { Title = "NAgent AI Agent API", Version = "v1" });
});

// 注册原有 Infrastructure 层
builder.Services.AddInfrastructure(builder.Configuration);

// 注册 Agent Application 层（CQRS）
var agentApplicationAssembly = typeof(NAgent.AgentApplication.Features.ExecuteAgent.Commands.ExecuteAgentCommand).Assembly;
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(agentApplicationAssembly);
});

// 注册 Agent Infrastructure 层（具体实现）
builder.Services.AddAgentInfrastructure(builder.Configuration);

// 注册原有 Application 层
var applicationAssembly = typeof(NAgent.Application.DTOs.UserDto).Assembly;
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(applicationAssembly);
});

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

// 添加CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandler();

// ⭐ 添加初始化检查中间件（在认证授权之前）
app.UseInitializationCheck();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// ⭐ 使用标准认证中间件（必须在使用授权之前）
app.UseAuthentication();

app.UseAuthorization();

// ⭐ 添加静态文件支持（用于前端页面，必须在 MapControllers 之前）
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// ⭐ 初始化 LLM 配置（使用 Scoped 服务）
using (var scope = app.Services.CreateScope())
{
    var initializer = new NAgent.AgentInfrastructure.Services.LlmConfigurationInitializer(
        builder.Configuration,
        scope.ServiceProvider.GetRequiredService<NAgent.AgentDomain.Repositories.ILlmProviderRepository>(),
        scope.ServiceProvider.GetRequiredService<NAgent.AgentDomain.Repositories.ILlmModelRepository>(),
        scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NAgent.AgentInfrastructure.Services.LlmConfigurationInitializer>>()
    );
    await initializer.InitializeAsync();
}

// ⭐ 自动加载 Skills 和 Tools 文件
using (var scope = app.Services.CreateScope())
{
    var autoLoader = scope.ServiceProvider.GetRequiredService<NAgent.AgentInfrastructure.Services.SkillsAndToolsAutoLoader>();
    await autoLoader.LoadAsync();
}

app.Run();