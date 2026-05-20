# NAgent 项目结构

```
NAgent/
│
├── .gitignore                          # Git忽略文件配置
├── README.md                           # 项目说明文档
├── NAgent.WebApi.slnx                  # 解决方案文件
├── csharp-dotnet-ddd.md                # C# DDD开发规范
│
├── src/                                # 源代码目录
│   │
│   ├── NAgent.Domain/                  # 🎯 领域层（核心业务逻辑）
│   │   ├── Common/
│   │   │   └── EntityBase.cs          # 实体基类
│   │   ├── Entities/
│   │   │   └── User.cs                # 用户实体
│   │   ├── Events/
│   │   │   ├── IDomainEvent.cs        # 领域事件接口
│   │   │   └── UserCreatedEvent.cs    # 用户创建事件
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs     # 领域异常
│   │   ├── Repositories/
│   │   │   ├── IRepository.cs         # 通用仓储接口
│   │   │   └── IUserRepository.cs     # 用户仓储接口
│   │   ├── Services/                   # 领域服务（待实现）
│   │   └── ValueObjects/               # 值对象（待实现）
│   │
│   ├── NAgent.Application/             # 📋 应用层（CQRS、DTO、服务协调）
│   │   ├── Common/                     # 应用层通用类
│   │   ├── DTOs/
│   │   │   └── UserDto.cs             # 用户DTO
│   │   ├── Features/                   # 功能模块（按业务领域分组）
│   │   │   └── Users/
│   │   │       ├── Commands/
│   │   │       │   ├── CreateUserCommand.cs
│   │   │       │   └── CreateUserCommandHandler.cs
│   │   │       ├── Queries/
│   │   │       │   ├── GetUserByIdQuery.cs
│   │   │       │   └── GetUserByIdQueryHandler.cs
│   │   │       └── EventHandlers/      # 领域事件处理器
│   │   ├── Interfaces/                 # 应用服务接口
│   │   ├── Mappings/
│   │   │   └── MappingProfile.cs      # AutoMapper配置
│   │   └── Validators/
│   │       └── CreateUserCommandValidator.cs  # FluentValidation验证器
│   │
│   ├── NAgent.Infrastructure/          # 🔧 基础设施层（数据访问、外部服务）
│   │   ├── Configurations/
│   │   │   └── UserConfiguration.cs   # EF Core实体配置
│   │   ├── Persistence/
│   │   │   └── AppDbContext.cs        # 数据库上下文
│   │   ├── Repositories/
│   │   │   └── UserRepository.cs      # 用户仓储实现
│   │   ├── Services/                   # 外部服务实现
│   │   └── DependencyInjection.cs     # 依赖注入配置
│   │
│   ├── NAgent.Api/                     # 🌐 API层（Controllers、中间件）
│   │   ├── Controllers/
│   │   │   └── UsersController.cs     # 用户API控制器
│   │   ├── Middlewares/
│   │   │   └── GlobalExceptionHandler.cs  # 全局异常处理中间件
│   │   ├── Program.cs                  # 应用入口和DI配置
│   │   ├── appsettings.json            # 应用配置
│   │   └── NAgent.Api.csproj           # API项目文件
│   │
│   └── NAgent.Shared/                  # 📦 共享层（通用类型）
│       ├── Exceptions/
│       │   ├── NotFoundException.cs   # 未找到异常
│       │   └── ValidationException.cs # 验证异常
│       └── Responses/
│           └── ApiResponse.cs         # 统一API响应模型
│
└── tests/                              # 🧪 测试目录
    │
    ├── NAgent.UnitTests/               # 单元测试
    │   ├── Domain/                     # 领域层测试
    │   ├── Application/                # 应用层测试
    │   └── NAgent.UnitTests.csproj
    │
    └── NAgent.IntegrationTests/        # 集成测试
        ├── Api/                        # API集成测试
        ├── Infrastructure/             # 基础设施测试
        └── NAgent.IntegrationTests.csproj
```

## 📊 层级依赖关系

```
┌─────────────────────────────────────┐
│         NAgent.Api (API层)          │
│  ┌──────────────────────────────┐   │
│  │ Controllers + Middlewares    │   │
│  └──────────────────────────────┘   │
└──────────┬──────────┬───────────────┘
           ↓          ↓
┌──────────────────┐ ┌──────────────────────┐
│  Application层   │ │   Shared层           │
│  ┌────────────┐  │ │  ┌────────────────┐  │
│  │ CQRS + DTO │  │ │  │ Exceptions     │  │
│  └────────────┘  │ │  │ Responses      │  │
└────────┬─────────┘ │  └────────────────┘  │
         ↓           └──────────┬───────────┘
┌──────────────────┐            ↓
│ Infrastructure层 │  ┌──────────────────────┐
│  ┌────────────┐  │  │   Domain层           │
│  │ EF Core    │  │  │  ┌────────────────┐  │
│  │ Repos      │  │  │  │ Entities       │  │
│  └────────────┘  │  │  │ Value Objects  │  │
└──────────────────┘  │  │ Domain Events  │  │
                      │  │ Repositories*  │  │
                      │  └────────────────┘  │
                      └──────────────────────┘
                      * 接口定义，Infrastructure实现
```

## 🔄 数据流示例：创建用户

```
Client Request
     ↓
[API Layer] UsersController.CreateUser()
     ↓
[Application Layer] CreateUserCommand → MediatR
     ↓
[Application Layer] CreateUserCommandHandler
     ├→ 验证用户名/邮箱唯一性 (IUserRepository)
     ├→ 调用领域实体 User.Create() 
     │    ├→ 业务规则验证
     │    └→ 触发 UserCreatedEvent
     └→ 持久化 (IUserRepository.AddAsync)
     ↓
[Infrastructure Layer] UserRepository.AddAsync()
     ↓
[Infrastructure Layer] AppDbContext.SaveChangesAsync()
     ↓
Database (PostgreSQL)
     ↓
Response: UserId
```

## 📁 关键文件说明

### Domain层
- **EntityBase.cs**: 所有实体的基类，包含Id、时间戳和领域事件
- **User.cs**: 用户实体，封装业务规则和状态变更
- **IDomainEvent.cs**: 领域事件标记接口
- **IUserRepository.cs**: 用户仓储接口（由Infrastructure实现）

### Application层
- **CreateUserCommand.cs**: 创建用户的命令
- **CreateUserCommandHandler.cs**: 命令处理器，协调领域对象
- **GetUserByIdQuery.cs**: 查询用户ById的查询
- **UserDto.cs**: 数据传输对象
- **MappingProfile.cs**: AutoMapper映射配置
- **CreateUserCommandValidator.cs**: FluentValidation验证器

### Infrastructure层
- **AppDbContext.cs**: EF Core数据库上下文
- **UserConfiguration.cs**: User实体的EF Core配置
- **UserRepository.cs**: IUserRepository的实现
- **DependencyInjection.cs**: 服务注册扩展方法

### API层
- **UsersController.cs**: RESTful API端点
- **GlobalExceptionHandler.cs**: 全局异常处理中间件
- **Program.cs**: 应用启动和DI配置

### Shared层
- **ApiResponse.cs**: 统一的API响应格式
- **NotFoundException.cs**: 资源未找到异常
- **ValidationException.cs**: 验证失败异常
