# NAgent - .NET DDD 架构示例项目

这是一个基于领域驱动设计（DDD）架构的 .NET 8.0 示例项目，展示了如何构建可扩展、可维护的企业级应用程序。

## 📁 项目结构

```
NAgent/
├── src/
│   ├── NAgent.Domain/              # 领域层（核心业务逻辑）
│   │   ├── Entities/               # 实体
│   │   ├── ValueObjects/           # 值对象
│   │   ├── Repositories/           # 仓储接口
│   │   ├── Services/               # 领域服务
│   │   ├── Events/                 # 领域事件
│   │   ├── Exceptions/             # 领域异常
│   │   └── Common/                 # 通用基类
│   │
│   ├── NAgent.Application/         # 应用层（CQRS、DTO、服务协调）
│   │   ├── Features/               # 功能模块（按业务领域分组）
│   │   │   └── Users/
│   │   │       ├── Commands/       # 命令
│   │   │       ├── Queries/        # 查询
│   │   │       └── EventHandlers/  # 事件处理器
│   │   ├── DTOs/                   # 数据传输对象
│   │   ├── Mappings/               # AutoMapper配置
│   │   ├── Validators/             # FluentValidation验证器
│   │   └── Interfaces/             # 应用服务接口
│   │
│   ├── NAgent.Infrastructure/      # 基础设施层（数据访问、外部服务）
│   │   ├── Persistence/            # 数据库上下文
│   │   ├── Repositories/           # 仓储实现
│   │   ├── Configurations/         # EF Core配置
│   │   └── Services/               # 外部服务实现
│   │
│   ├── NAgent.Api/                 # API层（Controllers、中间件）
│   │   ├── Controllers/            # API控制器
│   │   ├── Middlewares/            # 中间件
│   │   └── Program.cs              # 应用入口
│   │
│   └── NAgent.Shared/              # 共享层（通用类型）
│       ├── Exceptions/             # 通用异常
│       └── Responses/              # 统一响应模型
│
└── tests/
    ├── NAgent.UnitTests/           # 单元测试
    └── NAgent.IntegrationTests/    # 集成测试
```

## 🏗️ 架构说明

### DDD 分层架构

1. **Domain Layer（领域层）**
   - 包含核心业务逻辑和领域模型
   - 不依赖任何外部库（除了MediatR用于领域事件）
   - 定义仓储接口，由Infrastructure层实现

2. **Application Layer（应用层）**
   - 使用CQRS模式（Command Query Responsibility Segregation）
   - 通过MediatR处理命令和查询
   - 协调领域对象完成用例
   - 不包含业务逻辑，只做流程编排

3. **Infrastructure Layer（基础设施层）**
   - 实现Domain层定义的接口
   - 数据持久化（EF Core + PostgreSQL）
   - 外部服务集成

4. **API Layer（API层）**
   - RESTful API端点
   - 请求验证和响应格式化
   - 全局异常处理

5. **Shared Layer（共享层）**
   - 跨层使用的通用类型
   - 异常类、响应模型等

## 🚀 技术栈

- **.NET 8.0** - 运行时框架
- **ASP.NET Core** - Web框架
- **Entity Framework Core 8.0** - ORM
- **PostgreSQL** - 数据库
- **MediatR 14.1.0** - CQRS实现
- **AutoMapper 16.1.1** - 对象映射
- **FluentValidation 12.1.1** - 验证框架
- **Serilog 4.3.1** - 结构化日志
- **Swagger/OpenAPI** - API文档

### 测试
- **xUnit** - 测试框架
- **Moq 4.20.72** - Mock框架
- **FluentAssertions 8.10.0** - 断言库
- **Testcontainers** - 容器化集成测试

## 📦 快速开始

### 前置要求

- .NET 8.0 SDK
- PostgreSQL 12+
- Docker（可选，用于集成测试）

### 安装步骤

1. **克隆仓库**
   ```bash
   git clone <repository-url>
   cd NAgent
   ```

2. **还原依赖**
   ```bash
   dotnet restore
   ```

3. **配置数据库**
   
   修改 `src/NAgent.Api/appsettings.json` 中的连接字符串：
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=nagent_db;Username=postgres;Password=postgres"
     }
   }
   ```

4. **创建数据库**
   ```bash
   # 使用psql或其他工具创建数据库
   createdb nagent_db
   ```

5. **运行迁移**
   ```bash
   cd src/NAgent.Api
   dotnet ef migrations add InitialCreate -o ../NAgent.Infrastructure/Migrations
   dotnet ef database update
   ```

6. **运行应用**
   ```bash
   dotnet run
   ```

7. **访问API文档**
   
   打开浏览器访问: `https://localhost:5001/swagger`

## 🔧 开发指南

### 添加新功能

以添加"订单"功能为例：

1. **在Domain层创建实体**
   ```csharp
   // src/NAgent.Domain/Entities/Order.cs
   public class Order : EntityBase
   {
       // 实体定义
   }
   ```

2. **定义仓储接口**
   ```csharp
   // src/NAgent.Domain/Repositories/IOrderRepository.cs
   public interface IOrderRepository : IRepository<Order>
   {
       // 自定义查询方法
   }
   ```

3. **在Application层创建命令/查询**
   ```csharp
   // src/NAgent.Application/Features/Orders/Commands/CreateOrderCommand.cs
   public record CreateOrderCommand(...) : IRequest<Guid>;
   
   // src/NAgent.Application/Features/Orders/Commands/CreateOrderCommandHandler.cs
   public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
   {
       // 实现
   }
   ```

4. **在Infrastructure层实现仓储**
   ```csharp
   // src/NAgent.Infrastructure/Repositories/OrderRepository.cs
   public class OrderRepository : IOrderRepository
   {
       // 实现
   }
   ```

5. **注册依赖**
   ```csharp
   // src/NAgent.Infrastructure/DependencyInjection.cs
   services.AddScoped<IOrderRepository, OrderRepository>();
   ```

6. **创建API控制器**
   ```csharp
   // src/NAgent.Api/Controllers/OrdersController.cs
   [ApiController]
   [Route("api/[controller]")]
   public class OrdersController : ControllerBase
   {
       // API端点
   }
   ```

### 代码规范

- ✅ 遵循 SOLID 原则
- ✅ 使用依赖注入
- ✅ 领域实体使用工厂方法创建
- ✅ 使用值对象封装复杂类型
- ✅ 通过领域事件解耦业务逻辑
- ✅ 命令和查询分离（CQRS）
- ✅ 使用DTO进行层间数据传输
- ✅ 全局异常处理

## 🧪 测试

### 运行单元测试
```bash
cd tests/NAgent.UnitTests
dotnet test
```

### 运行集成测试
```bash
cd tests/NAgent.IntegrationTests
dotnet test
```

## 📝 API 示例

### 创建用户
```bash
curl -X POST https://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "username": "john_doe",
    "email": "john@example.com"
  }'
```

### 获取用户
```bash
curl -X GET https://localhost:5001/api/users/{userId}
```

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License
