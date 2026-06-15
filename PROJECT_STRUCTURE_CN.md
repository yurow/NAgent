# 项目结构

本文档全面概述 NAgent 项目的组织结构，解释每个目录和关键文件的用途。

## 根目录

```
NAgent/
├── src/                          # 源代码
├── tests/                        # 测试项目
├── docs/                         # 文档
├── skills/                       # Skill Markdown 文件
├── tools/                        # Tool YAML 配置文件
├── nagent.db                     # SQLite 数据库（运行时生成）
├── NAgent.WebApi.slnx            # 解决方案文件
├── README.md                     # 项目概述
├── QUICKSTART.md                 # 快速开始指南
├── PROJECT_STRUCTURE.md          # 本文档
└── GENERATION_REPORT.md          # 开发历史
```

## 源代码组织（`src/`）

源代码按照领域驱动设计原则组织成两个平行的子系统。

### 核心系统

#### 1. NAgent.Api

**用途**：REST API 层、中间件、静态文件服务

```
NAgent.Api/
├── Controllers/                  # API 控制器
│   ├── AuthController.cs         # 认证端点
│   ├── UsersController.cs        # 用户管理
│   ├── ProjectsController.cs     # 项目管理
│   ├── LlmController.cs          # LLM 提供商/模型管理
│   ├── AgentController.cs        # Agent 执行
│   ├── SkillsController.cs       # Skills 管理
│   ├── MemoryController.cs       # 记忆管理
│   └── InitializationController.cs # 系统初始化
├── Middleware/
│   ├── JwtAuthenticationMiddleware.cs  # JWT Token 验证
│   └── GlobalExceptionHandler.cs       # 全局异常处理
├── wwwroot/                      # 静态文件
│   ├── index.html               # 入口页面
│   ├── login.html               # 登录页面
│   ├── init.html                # 系统初始化页面
│   ├── dashboard.html           # 主仪表盘（SPA）
│   ├── css/                     # 样式表
│   │   ├── login.css
│   │   ├── init.css
│   │   └── dashboard.css
│   └── js/                      # JavaScript 文件
│       ├── login.js             # 登录逻辑
│       ├── init.js              # 初始化逻辑
│       └── dashboard.js         # 主应用逻辑
├── appsettings.json             # 应用配置
├── appsettings.Development.json # 开发配置
└── Program.cs                   # 应用入口点
```

#### 2. NAgent.Application

**用途**：应用层，包含 CQRS 命令/查询、DTO、验证器和映射配置

```
NAgent.Application/
├── DTOs/
│   └── UserDto.cs               # 用户数据传输对象
├── Features/                    # 按功能组织的 CQRS
│   ├── Auth/
│   │   └── Queries/
│   │       ├── LoginQuery.cs           # 登录查询
│   │       └── LoginQueryHandler.cs    # 登录处理器
│   ├── Users/
│   │   ├── Commands/
│   │   │   ├── CreateUserCommand.cs           # 创建用户
│   │   │   ├── CreateUserCommandHandler.cs
│   │   │   ├── UpdateUserStatusCommand.cs     # 更新状态
│   │   │   ├── UpdateUserRoleCommand.cs       # 更新角色
│   │   │   ├── ResetUserPasswordCommand.cs    # 重置密码
│   │   │   └── UserManagementCommandHandlers.cs
│   │   └── Queries/
│   │       ├── GetUserByIdQuery.cs
│   │       ├── GetUserByIdQueryHandler.cs
│   │       ├── GetAllUsersQuery.cs
│   │       └── GetAllUsersQueryHandler.cs
│   └── ... (其他功能)
├── Mappings/
│   └── MappingProfile.cs        # AutoMapper 配置
├── Interfaces/
│   ├── IJwtTokenService.cs      # JWT Token 生成
│   ├── IPasswordHasher.cs       # 密码哈希抽象
│   └── IDateTimeService.cs      # 日期时间抽象
└── Validators/                  # FluentValidation 验证器
```

#### 3. NAgent.Domain

**用途**：领域层，包含核心业务实体、领域事件、仓储接口和领域异常

```
NAgent.Domain/
├── Entities/
│   └── User.cs                  # 用户实体
├── Enums/
├── Events/                      # 领域事件
├── Exceptions/
│   └── DomainException.cs       # 基础领域异常
├── Repositories/
│   ├── IRepository.cs           # 通用仓储接口
│   └── IUserRepository.cs       # 用户仓储接口
└── ValueObjects/                # 值对象
```

#### 4. NAgent.Infrastructure

**用途**：基础设施层，包含数据持久化和外部服务实现

```
NAgent.Infrastructure/
├── Persistence/
│   └── AppDbContext.cs          # SqlSugar 数据库上下文
├── Repositories/
│   ├── SqliteRepository.cs      # 通用 SQLite 仓储
│   ├── SqliteUserRepository.cs  # 用户仓储实现
│   └── ...
├── Services/
│   ├── JwtTokenService.cs       # JWT 实现
│   ├── Sha256PasswordHasher.cs  # SHA256 密码哈希
│   └── InitializationService.cs # 系统初始化
└── DependencyInjection.cs       # DI 注册
```

#### 5. NAgent.Shared

**用途**：被所有层共享的组件

```
NAgent.Shared/
└── Responses/
    └── ApiResponse.cs           # 标准 API 响应包装器
```

### Agent 子系统

#### 6. NAgent.AgentApplication

**用途**：AI Agent 子系统的应用层

```
NAgent.AgentApplication/
├── Features/
│   ├── ExecuteAgent/
│   │   └── Commands/
│   │       ├── ExecuteAgentCommand.cs
│   │       └── ExecuteAgentCommandHandler.cs
│   ├── LlmManagement/
│   │   ├── Commands/            # LLM 提供商/模型命令
│   │   └── Queries/             # LLM 提供商/模型查询
│   ├── Projects/
│   │   ├── Commands/            # 项目命令
│   │   └── Queries/             # 项目查询
│   ├── Skills/
│   │   ├── Commands/            # Skill 命令
│   │   └── Queries/             # Skill 查询
│   └── Memory/
│       ├── Commands/            # 记忆命令
│       └── Queries/             # 记忆查询
└── Interfaces/
    ├── IAgentEngine.cs          # Agent 引擎抽象
    ├── ILlmClient.cs            # LLM 客户端接口
    ├── ISandboxExecutor.cs      # 沙箱执行器接口
    └── ISecurity.cs             # 安全组件接口
```

#### 7. NAgent.AgentDomain

**用途**：AI Agent 实体和服务领域层

```
NAgent.AgentDomain/
├── Entities/
│   ├── AgentSession.cs          # Agent 会话实体
│   ├── AgentTool.cs             # Agent 工具实体
│   ├── Project.cs               # 项目实体
│   ├── ProjectMemory.cs         # 项目记忆实体
│   ├── Skill.cs                 # Skill 实体
│   └── ToolDefinition.cs        # Tool 定义实体
├── Enums/
│   ├── LlmProtocolType.cs       # LLM 协议类型
│   ├── ToolSecurityLevel.cs     # 工具安全等级
│   ├── MemoryCategory.cs        # 记忆分类
│   └── MemoryImportance.cs      # 记忆重要性等级
├── Repositories/
│   ├── IAgentSessionRepository.cs
│   ├── IAgentToolRepository.cs
│   ├── IProjectRepository.cs
│   ├── IProjectMemoryRepository.cs
│   ├── ISkillRepository.cs
│   └── IToolDefinitionRepository.cs
└── Services/
    └── Memory/
        ├── IMemorySystem.cs           # 记忆系统接口
        ├── IMemoryStorage.cs          # 记忆存储接口
        ├── DefaultMemorySystem.cs     # 默认实现
        ├── FileMemoryStorage.cs       # 基于文件的存储
        └── MemorySystemFactory.cs     # 记忆系统工厂
```

#### 8. NAgent.AgentInfrastructure

**用途**：AI Agent 执行基础设施

```
NAgent.AgentInfrastructure/
├── Agents/
│   ├── AgentEngineFactory.cs          # 引擎工厂
│   ├── LangChain/
│   │   └── LangChainAgentEngine.cs    # LangChain 实现
│   └── SemanticKernel/
│       └── SemanticKernelAgentEngine.cs # SK 实现
├── Llm/
│   └── MultiModelLlmClient.cs         # 多模型 LLM 客户端
├── Parsers/
│   ├── SkillMarkdownParser.cs         # Skill MD 解析器
│   └── ToolYamlParser.cs              # Tool YAML 解析器
├── Repositories/
│   ├── InMemoryAgentSessionRepository.cs
│   ├── InMemoryAgentToolRepository.cs
│   ├── InMemoryProjectMemoryRepository.cs
│   ├── InMemorySkillRepository.cs
│   ├── InMemoryToolDefinitionRepository.cs
│   ├── SqliteLlmModelRepository.cs
│   └── SqliteLlmProviderRepository.cs
├── Sandbox/
│   └── CubeSandboxExecutorImpl.cs     # 沙箱执行器
├── Security/
│   ├── PromptFilterImpl.cs            # 提示词注入过滤器
│   └── SandboxResultValidatorImpl.cs  # 沙箱结果验证器
└── DependencyInjection.cs             # DI 注册
```

#### 9. NAgent.AgentCore

**用途**：核心 Agent 运行时组件

```
NAgent.AgentCore/
├── Agent/
│   ├── AgentRunner.cs           # 主 Agent 运行器
│   ├── ToolDispatcher.cs        # 工具分发逻辑
│   └── MemoryManager.cs         # 记忆管理
├── LLm/
│   └── LocalLlmClient.cs        # 本地 LLM 客户端
├── Sandbox/
│   └── CubeSandboxClient.cs     # 沙箱客户端
├── Security/
│   ├── PromptFilter.cs          # 提示词过滤器
│   ├── SandboxResultCheck.cs    # 沙箱结果检查器
│   └── ToolLevelConfig.cs       # 工具安全配置
├── Tools/
│   ├── LocalTools/              # 本地工具实现
│   │   └── CalculatorTool.cs
│   └── SandboxTools/            # 沙箱工具实现
│       └── CodeExecutorTool.cs
└── DependencyInjection.cs       # DI 注册
```

## 测试项目（`tests/`）

```
tests/
└── NAgent.Api.Tests/
    ├── Controllers/             # 控制器测试
    └── Integration/             # 集成测试
```

## 配置文件

### appsettings.json

主应用配置：
- 数据库连接字符串
- JWT 设置
- Agent 配置
- LLM 提供商预设
- 日志配置

### launchSettings.json

开发环境设置：
- 应用 URL
- 环境变量
- 启动配置文件

## 数据库架构

### SQLite 表

1. **Users** - 用户账户
2. **projects** - 项目
3. **LlmProviders** - LLM 提供商
4. **LlmModels** - LLM 模型
5. **LlmModelDailyUsages** - 每日使用统计

### 内存存储

以下使用内存仓储（可迁移到持久化存储）：
- Agent 会话
- Agent 工具
- Skills
- Tool 定义
- 项目记忆

## 依赖图

```
                    ┌─────────────┐
                    │  NAgent.Api │
                    └──────┬──────┘
           ┌─────────────┼─────────────┐
           ▼             ▼             ▼
    ┌────────────┐ ┌────────────┐ ┌────────────┐
    │NAgent.App  │ │NAgent.Agent│ │NAgent.Agent│
    │lication    │ │Application │ │Domain      │
    └──────┬─────┘ └──────┬─────┘ └──────┬─────┘
           │              │              │
           ▼              ▼              ▼
    ┌────────────┐ ┌────────────┐ ┌────────────┐
    │NAgent.     │ │NAgent.Agent│ │NAgent.Agent│
    │Domain      │ │Infrastructure│ │Core      │
    └──────┬─────┘ └──────┬─────┘ └────────────┘
           │              │
           ▼              ▼
    ┌────────────┐ ┌────────────┐
    │NAgent.     │ │NAgent.     │
    │Infrastructure│ │Shared     │
    └────────────┘ └────────────┘
```

## 命名规范

### 文件
- **控制器**：`*Controller.cs`
- **命令**：`*Command.cs`, `*CommandHandler.cs`
- **查询**：`*Query.cs`, `*QueryHandler.cs`
- **实体**：PascalCase，单数（例如 `User.cs`）
- **仓储**：`I*Repository.cs`（接口）, `*Repository.cs`（实现）
- **服务**：`I*Service.cs`（接口）, `*Service.cs`（实现）

### 类/接口
- **接口**：PascalCase，带 `I` 前缀（例如 `IUserRepository`）
- **实体**：PascalCase（例如 `User`, `Project`）
- **DTO**：PascalCase，JSON 序列化使用 camelCase 属性
- **枚举**：PascalCase（例如 `ToolSecurityLevel`）

### 方法
- **异步方法**：后缀 `Async`（例如 `GetByIdAsync`）
- **命令处理器**：`Handle(*Command, CancellationToken)`
- **查询处理器**：`Handle(*Query, CancellationToken)`

## 添加新功能

添加新功能的步骤：

1. **领域层**：在 `NAgent.AgentDomain` 中定义实体和仓储接口
2. **应用层**：在 `NAgent.AgentApplication` 中创建命令/查询和处理器
3. **基础设施层**：在 `NAgent.AgentInfrastructure` 中实现仓储
4. **API 层**：在 `NAgent.Api` 中添加控制器端点
5. **前端**：更新 `dashboard.html` 和 `dashboard.js`

## 构建配置

### 解决方案文件

`NAgent.WebApi.slnx` 定义解决方案结构和项目引用。

### 项目文件

每个 `.csproj` 文件定义：
- 目标框架（`net8.0`）
- 包引用
- 项目引用
- 输出设置

### 关键 NuGet 包

| 包 | 用途 |
|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT 认证 |
| `SqlSugarCore` | SQLite ORM |
| `MediatR` | CQRS 实现 |
| `AutoMapper` | 对象映射 |
| `FluentValidation` | 输入验证 |
| `Serilog` | 结构化日志 |
| `Swashbuckle.AspNetCore` | Swagger/OpenAPI |
| `YamlDotNet` | YAML 解析 |
| `Markdig` | Markdown 解析 |
| `LangChain.Core` | LLM 集成 |
