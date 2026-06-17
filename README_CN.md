# NAgent - AI Agent 平台

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

NAgent 是一个基于 .NET 8 构建的综合性 AI Agent 平台，采用领域驱动设计（DDD）架构。它提供了完整的解决方案，用于构建、管理和部署 AI Agent，支持多 LLM、项目隔离、可扩展的 Skills/Tools 系统以及项目级记忆管理。
项目并未为多用户，并发设计。但可以满足个人或少量用户一起使用。

## 目录

- [功能特性](#功能特性)
- [系统架构](#系统架构)
- [技术栈](#技术栈)
- [快速开始](#快速开始)
- [文档](#文档)
- [项目结构](#项目结构)
- [API 参考](#api-参考)
- [配置说明](#配置说明)
- [安全机制](#安全机制)
- [免责声明](#免责声明)
- [贡献指南](#贡献指南)
- [许可证](#许可证)

## 功能特性

### 核心功能

- **用户认证与授权**
  - 基于 JWT 的认证，支持角色访问控制（管理员/普通用户）
  - 可配置算法的安全密码哈希
  - Token 验证和自动过期处理

- **项目管理**
  - 创建、管理和组织项目，支持工作空间隔离
  - 每个项目拥有独立的工作空间目录和隔离的记忆
  - 项目级访问控制和管理

- **多 LLM 提供商管理**
  - 支持多个 LLM 提供商（OpenAI、Anthropic、Ollama 等）
  - 通过 API 或配置文件动态配置提供商
  - 模型切换和使用统计跟踪
  - 协议抽象，支持 OpenAI 兼容和 Anthropic API

- **AI Agent 执行**
  - 非流式和服务器发送事件（SSE）流式执行
  - 基于 LangChain 的 Agent 引擎，支持可扩展架构
  - Semantic Kernel 引擎支持（计划中）
  - 会话管理和对话历史
  - **思维链显示** - 在聊天界面展示 Agent 的思考过程

- **Skills 系统**
  - 通过 Markdown 文档和 YAML Front Matter 定义可扩展的 Skills
  - 从 `skills/` 目录自动加载
  - Skill 分类和版本管理
  - 与 Tools 关联，实现能力组合
  - **Skill 包装 Tool 执行** - 模型选择 Skill，代码处理 Tool 编排

- **Tools 系统**
  - 通过 YAML 配置文件定义 Tools
  - 安全等级分类（低/中/高）
  - 多种执行类型：本地、沙箱、HTTP、命令
  - 参数验证，支持类型检查和枚举
  - **内置工具**：文件读写、文件列表、网页搜索

- **项目级记忆系统**
  - 短期记忆：最近的对话上下文（最近 20 条消息）
  - 长期记忆：跨会话的持久化知识
  - 记忆重要性评分和自动过期
  - 记忆分类：用户偏好、项目知识、代码模式、错误解决方案、决策记录
  - 完整的项目记忆隔离——每个项目的记忆完全独立

- **用户管理（管理员功能）**
  - 用户增删改查和角色分配
  - 账户启用/禁用
  密码重置功能
  - 管理员专属的系统管理功能

### 前端功能

- 基于 jQuery 的单页应用体验
- 实时流式聊天界面
- 项目管理仪表盘
- LLM 提供商和模型配置界面
- 用户管理界面（管理员）
- Skills 和 Tools 管理面板
- **思维链可视化** 在聊天消息中展示

## 系统架构

NAgent 采用**领域驱动设计（DDD）分层架构**，包含两个平行的子系统：

### 核心系统（基础 DDD）

```
+-----------------------------------------+
|              NAgent.Api                  |  <- REST API、中间件、静态文件
+-----------------------------------------+
|         NAgent.Application               |  <- CQRS 命令/查询、DTO、验证器
+-----------------------------------------+
|           NAgent.Domain                  |  <- 实体、领域事件、仓储接口
+-----------------------------------------+
|        NAgent.Infrastructure             |  <- SqlSugar + SQLite、仓储实现
+-----------------------------------------+
|           NAgent.Shared                  |  <- 通用响应模型、异常类
+-----------------------------------------+
```

### Agent 子系统（AI 扩展）

```
+-----------------------------------------+
|              NAgent.Api                  |  <- 共享 API 层
+-----------------------------------------+
|      NAgent.AgentApplication             |  <- Agent CQRS、LLM 管理
+-----------------------------------------+
|        NAgent.AgentDomain                |  <- Agent 实体、记忆、Skills/Tools
+-----------------------------------------+
|     NAgent.AgentInfrastructure           |  <- LangChain/SK 引擎、多模型 LLM
+-----------------------------------------+
|         NAgent.AgentCore                 |  <- Agent 运行器、工具分发器、安全
+-----------------------------------------+
```

### 依赖流向

```
Api -> Application/AgentApplication -> Domain/AgentDomain -> Infrastructure/AgentInfrastructure
Shared 被所有层引用
```

### 关键架构模式

- **CQRS（命令查询职责分离）**：使用 MediatR 分离读写操作
- **仓储模式**：在领域层定义接口，在基础设施层实现
- **依赖注入**：完整的 DI 容器支持，包括 Scoped、Transient 和 Singleton 生命周期
- **中间件管道**：自定义 JWT 认证中间件和全局异常处理
- **Options 模式**：强类型配置绑定

## 技术栈

| 类别 | 技术 | 版本 |
|------|------|------|
| 运行时 | .NET | 8.0 |
| Web 框架 | ASP.NET Core | 8.0 |
| ORM | SqlSugar | 5.x |
| 数据库 | SQLite | 3.x |
| CQRS | MediatR | 14.x |
| 对象映射 | AutoMapper | 16.x |
| 验证 | FluentValidation | 12.x |
| 认证 | JWT (System.IdentityModel.Tokens.Jwt) | 7.x |
| 日志 | Serilog | 4.x |
| API 文档 | Swashbuckle (Swagger) | 6.x |
| LLM 集成 | LangChain.Core | 0.x |
| YAML 解析 | YamlDotNet | 16.x |
| Markdown 解析 | Markdig | 0.40.x |
| HTML 解析 | HtmlAgilityPack | 1.12.x |
| 前端 | jQuery | 3.7.1 |
| 测试 | xUnit | 2.x |

## 快速开始

### 前置条件

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQLite](https://www.sqlite.org/download.html)（可选，嵌入式）

### 安装

```bash
# 克隆仓库
git clone https://github.com/your-org/nagent.git
cd nagent

# 还原依赖
dotnet restore

# 构建解决方案
dotnet build

# 运行应用
dotnet run --project src/NAgent.Api
```

### 初始设置

1. 在浏览器中打开 `http://localhost:9527`
2. 首次运行时会重定向到初始化页面
3. 创建第一个管理员账号
4. 登录并开始使用 NAgent

### 默认配置

应用使用 `appsettings.json` 进行配置。关键设置：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=nagent.db"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKey...",
    "Issuer": "NAgent",
    "Audience": "NAgent.Api",
    "ExpirationMinutes": 60
  }
}
```

## 文档

- [快速开始指南](QUICKSTART_CN.md) - 新用户分步教程
- [项目结构](PROJECT_STRUCTURE_CN.md) - 详细的项目组织说明
- [开发报告](GENERATION_REPORT_CN.md) - 开发历史和设计决策
- [英文文档](README.md) - English documentation

## 项目结构

```
g:\NAgent/
├── src/                          # 源代码（9 个项目）
│   ├── NAgent.Api/               # REST API 层
│   ├── NAgent.Application/       # 核心应用层
│   ├── NAgent.Domain/            # 核心领域层
│   ├── NAgent.Infrastructure/    # 核心基础设施层
│   ├── NAgent.AgentApplication/  # Agent 应用层
│   ├── NAgent.AgentDomain/       # Agent 领域层
│   ├── NAgent.AgentInfrastructure/# Agent 基础设施层
│   ├── NAgent.AgentCore/         # Agent 核心运行时
│   └── NAgent.Shared/            # 共享组件
├── tests/                        # 测试项目
├── skills/                       # Skill Markdown 文件
├── tools/                        # Tool YAML 配置
└── docs/                         # 文档
```

## API 参考

### 认证

| 方法 | 端点 | 认证 | 说明 |
|------|------|------|------|
| POST | `/api/auth/login` | 公开 | 用户登录 |
| GET | `/api/auth/validate` | Bearer | 验证 Token |

### 项目

| 方法 | 端点 | 认证 | 说明 |
|------|------|------|------|
| GET | `/api/projects/user/{userId}` | Bearer | 用户项目列表 |
| POST | `/api/projects` | Bearer | 创建项目 |
| DELETE | `/api/projects/{id}` | Bearer | 删除项目 |

### LLM 管理

| 方法 | 端点 | 认证 | 说明 |
|------|------|------|------|
| GET | `/api/llm/providers` | Bearer | 提供商列表 |
| POST | `/api/llm/providers` | Bearer | 添加提供商 |
| GET | `/api/llm/models` | Bearer | 模型列表 |
| POST | `/api/llm/models/switch` | Bearer | 切换模型 |

### Agent 执行

| 方法 | 端点 | 认证 | 说明 |
|------|------|------|------|
| POST | `/api/agent/execute` | Bearer | 执行 Agent |
| POST | `/api/agent/execute-stream` | Bearer | 流式执行 |

### Skills & Tools

| 方法 | 端点 | 认证 | 说明 |
|------|------|------|------|
| GET | `/api/skills` | Bearer | Skills 列表 |
| POST | `/api/skills/reload` | Admin | 重新加载 Skills |
| GET | `/api/tools` | Bearer | Tools 列表 |

### 记忆

| 方法 | 端点 | 认证 | 说明 |
|------|------|------|------|
| GET | `/api/memory/project/{id}/summary` | Bearer | 记忆摘要 |
| POST | `/api/memory/project/{id}/long-term` | Bearer | 保存记忆 |

## 配置说明

### 环境变量

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `ASPNETCORE_ENVIRONMENT` | 环境名称 | `Production` |
| `JWT_SECRET` | JWT 签名密钥 | 来自 appsettings |
| `DATABASE_PATH` | SQLite 数据库路径 | `nagent.db` |

### Skills 目录

将 Markdown 文件放在 `skills/` 目录：

```markdown
---
name: my-skill
description: Skill 描述
category: development
version: 1.0.0
---

# My Skill

## Tools
- tool_name

## Examples
#### Example 1
Input: ...
Output: ...
```

### Tools 目录

将 YAML 文件放在 `tools/` 目录：

```yaml
name: my_tool
description: Tool 描述
category: development
security_level: low

parameters:
  - name: param1
    type: string
    required: true

execution:
  type: local
  command: "my-command"
```

## 安全机制

- **JWT 认证**：基于状态 Token 的认证，支持可配置过期时间
- **角色授权**：管理员端点使用 `[Authorize(Roles = "Admin")]`
- **密码哈希**：SHA256，支持迁移至 BCrypt/PBKDF2
- **输入验证**：所有输入 DTO 使用 FluentValidation
- **XSS 防护**：前端 HTML 转义
- **提示词注入过滤**：检测并阻止恶意提示词模式
- **沙箱执行**：高风险工具在隔离环境中执行

## 免责声明

### 搜索结果使用声明

NAgent 内置了网页搜索功能（百度、Bing），该功能仅用于**研究和学习目的**。搜索结果通过程序方式获取，使用时请遵守相应搜索引擎的服务条款。

- **仅限研究用途**：搜索结果仅用于研究、学习和信息收集目的。
- **禁止批量抓取**：本工具不适用于大规模数据收集、自动化抓取或商业用途。
- **遵守频率限制**：系统实现了频率限制（搜索间隔 5 秒），以避免对搜索引擎服务器造成过大负载。
- **不保证准确性**：搜索结果的准确性、完整性或可用性不做任何保证。
- **用户责任**：用户使用搜索功能时需遵守适用的法律法规。

## 贡献指南

欢迎贡献！请阅读我们的 [贡献指南](CONTRIBUTING.md) 了解详情。

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。

## 致谢

- 基于 .NET 8 和优秀的 .NET 生态系统构建
- LLM 集成由 LangChain 提供支持
- 前端基于 jQuery 和现代 CSS
