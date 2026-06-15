# 开发报告

本文档记录 NAgent 项目的开发历史、设计决策和架构演进。

## 项目起源

NAgent 的构想是作为一个基于 .NET 8 的综合 AI Agent 平台，遵循领域驱动设计（DDD）原则。目标是创建一个能够管理 AI Agent 的系统，支持多 LLM、项目隔离、可扩展能力和持久记忆。

## 开发时间线

### 第一阶段：基础（初始开发）

**核心系统搭建**
- 建立 DDD 分层架构，包含 5 个核心项目：Api、Application、Domain、Infrastructure、Shared
- 实现基于 JWT Token 的用户认证和基于角色的访问控制
- 创建使用 SqlSugar ORM 的 SQLite 数据库架构
- 构建首次使用的系统初始化流程

**关键决策：**
- 选择 SqlSugar 而非 Entity Framework，以获得更好的 SQLite 性能和更简单的配置
- 实现带有自定义中间件的 JWT 认证用于 Token 验证
- 创建 `ApiResponse<T>` 包装器用于标准化 API 响应

### 第二阶段：Agent 子系统

**AI Agent 架构**
- 添加并行的 Agent 子系统：AgentApplication、AgentDomain、AgentInfrastructure、AgentCore
- 设计支持多 Agent 框架（LangChain、Semantic Kernel）的 `IAgentEngine` 抽象
- 实现 `ExecuteAgentCommand`，包含完整的执行流水线：验证 -> 安全过滤 -> 会话管理 -> 引擎执行 -> 结果验证 -> 持久化

**LLM 管理**
- 创建多提供商 LLM 支持（OpenAI、Anthropic、Ollama）
- 实现协议抽象（`LlmProtocolType`）用于 OpenAI 兼容和 Anthropic API
- 构建模型切换和使用跟踪系统
- 添加每日使用统计和 `LlmModelDailyUsage`

**关键决策：**
- 使用 LangChain.Core 进行 Agent 编排，因其成熟度和生态系统
- 为 Agent 会话和工具实现内存仓储（可迁移到持久化存储）
- 创建 `MultiModelLlmClient` 以在单一接口后抽象多个 LLM 提供商

### 第三阶段：项目管理

**项目系统**
- 实现带有工作空间隔离的项目增删改查操作
- 每个项目获得自己的文件系统工作空间目录
- 添加项目激活/停用生命周期
- 创建 `IWorkspaceManager` 接口以抽象文件系统操作

**关键决策：**
- 项目由用户拥有并按用户 ID 隔离
- 工作空间路径按确定性生成：`{basePath}/{userId}/{projectId}`
- 使用 `IWorkspaceManager` 接口解耦应用层和基础设施层

### 第四阶段：Skills 和 Tools 系统

**Skills 系统**
- 将 Skills 设计为带有 YAML Front Matter 的 Markdown 文档
- 创建带有元数据（名称、描述、分类、版本、作者）的 `Skill` 实体
- 实现 `SkillMarkdownParser` 用于解析 Markdown 文件
- 添加从 `skills/` 目录加载 Skills 并支持文件系统监视

**Tools 系统**
- 将 Tools 设计为 YAML 配置文件
- 创建带有参数和执行配置的 `ToolDefinition` 实体
- 实现 `ToolYamlParser` 用于 YAML 解析
- 添加安全等级分类（低/中/高）
- 支持多种执行类型：本地、沙箱、HTTP、命令

**关键决策：**
- Skills 描述"Agent 知道什么"（知识）
- Tools 描述"Agent 能做什么"（能力）
- 两个系统都使用基于文件的配置以便轻松扩展
- 安全等级决定执行环境（本地 vs 沙箱）

### 第五阶段：记忆系统

**项目级记忆**
- 实现双记忆架构：短期和长期
- 短期记忆：最近的对话上下文（20 条消息，自动溢出到长期）
- 长期记忆：带有重要性评分的持久化 `ProjectMemory` 实体
- 记忆分类：通用、用户偏好、项目知识、代码模式、错误解决方案、决策记录
- 记忆重要性等级：低、中、高、关键

**记忆评分和过期**
- 实现 `CalculateScore()` 方法：`重要性 + 访问次数 * 0.1 + 近期奖励`
- 添加带有自动清理的过期机制
- 记忆隔离：每个项目的记忆完全独立

**关键决策：**
- 记忆按项目而非用户划分（一个用户可以有多个项目，各自独立记忆）
- 项目记忆使用内存存储（可迁移到 SQLite）
- 会话记忆使用 JSON 文件存储

### 第六阶段：用户管理增强

**管理员功能**
- 添加用户列表端点（`GET /api/users`）
- 实现用户状态切换（活跃/禁用）
- 添加角色管理（管理员/用户切换）
- 创建密码重置功能
- 为破坏性操作添加确认对话框

**前端改进**
- 更新 UserDto 使用 camelCase 属性以兼容 JavaScript
- 添加仅管理员可见的菜单项
- 实现用户操作按钮（角色切换、状态切换、密码重置）

### 第七阶段：前端仪表盘增强

**导航和 UI**
- 添加 Tools 管理和 Skills 管理导航项
- 创建 Tools 和 Skills 管理的内容区域
- 实现管理菜单的仅管理员可见性

**项目管理 Tab**
- 从项目卡片中移除激活/停用按钮
- 添加"聊天"按钮用于直接项目访问
- 实现仅管理员删除并带二次确认
- 为项目删除添加二次确认

**关键决策：**
- 使用 jQuery + 原生 JavaScript 作为前端（无框架）以保持轻量
- 在 localStorage 中存储 JWT Token 和用户信息
- 在 DTO 中使用 camelCase 属性名以匹配 JavaScript 约定

## Bug 修复和优化

### 认证修复
- 修复 `UserInfo` 属性命名从 PascalCase 到 camelCase 以兼容 JSON 序列化
- 解决 `undefined` userId 问题，确保后端和前端属性名一致

### 项目管理修复
- 修复数据库初始化中缺失的 `Project` 表创建
- 为 `Project` 实体添加 SqlSugar 特性（`[SugarTable]`、`[SugarColumn]`）
- 修复 `SqliteProjectRepository` 查询方法以使用正确的 SqlSugar API
- 解决 `CreateProjectCommandHandler` 中的工作空间路径不一致

### 依赖注入修复
- 修复 `ReloadAllSkillsCommandHandler` 构造函数以使用 `IConfiguration` 而非原始 `string`
- 通过将 `IWorkspaceManager` 移至 `AgentDomain` 解决 `AgentApplication` 和 `Infrastructure` 之间的循环依赖

### 代码清理
- 从 Domain、Application、Infrastructure、Shared 层移除空的 `Class1.cs` 文件
- 在 `AppDbContext` 中将 `Console.WriteLine` 替换为 `ILogger`
- 提取 `IPasswordHasher` 接口以解耦密码哈希
- 将项目命令从可变类转换为不可变记录
- 从 `LlmController` 移除临时的 `FixInvalidProtocolTypes` 端点

## 架构决策

### 为什么选择 DDD？
选择领域驱动设计是因为：
- 复杂的业务逻辑（Agent 执行、记忆管理、工具分发）
- 核心系统和 AI 子系统之间需要清晰的边界
- 需要可扩展性（新的 LLM 提供商、新工具、新 Skills）

### 为什么选择 SqlSugar 而非 EF Core？
- 更好的 SQLite 支持和更简单的配置
- 预期工作负载的性能优势
- 更小的依赖占用

### 为什么 Agent 组件使用内存仓储？
- Agent 会话和记忆本质上是临时的
- 以后可以轻松迁移到持久化存储
- 简化初始开发和测试

### 为什么 Skills/Tools 使用基于文件的配置？
- 易于版本控制（Markdown 和 YAML 是基于文本的）
- 无需数据库迁移即可简单编辑和扩展
- 热重载能力用于快速迭代

### 为什么前端使用 jQuery？
- 轻量，无需构建步骤
- 足以满足仪表盘的需求
- 小团队易于维护

## 技术债务和未来改进

### 已知问题
1. **密码哈希**：当前使用 SHA256，生产环境应迁移到 BCrypt 或 PBKDF2
2. **内存存储**：Agent 会话、Skills、Tools 和记忆使用内存存储
3. **AgentCore 重复**：AgentCore 中的某些组件与 AgentInfrastructure 重叠
4. **Semantic Kernel**：引擎实现是占位符（抛出 NotImplementedException）

### 计划增强
1. **持久记忆存储**：将项目记忆迁移到 SQLite
2. **向量搜索**：添加基于向量的记忆搜索用于语义检索
3. **插件系统**：允许动态加载 .NET 程序集作为工具
4. **多 Agent 协作**：支持多个 Agent 协同工作
5. **WebSocket 支持**：用于聊天的实时双向通信
6. **React/Vue 前端**：迁移到现代前端框架

## 性能考虑

### 当前优化
- LLM 模型配置的内存缓存
- 短期记忆限制（20 条消息）和自动溢出
- 智能保留的记忆评分

### 瓶颈
- 基于文件的会话内存 I/O
- 内存仓储搜索（线性扫描）
- LLM API 调用延迟（外部依赖）

## 安全考虑

### 已实现
- 带有可配置过期的 JWT 认证
- 基于角色的授权
- 密码哈希（SHA256，带迁移路径）
- 提示词注入过滤
- 高风险工具的沙箱执行
- 前端 HTML 转义

### 需要改进
- 密码哈希算法（迁移到 BCrypt）
- API 端点的速率限制
- 文件上传的输入清理
- 状态变更操作的 CSRF 防护

## 结论

NAgent 已从一个简单概念演进为一个综合 AI Agent 平台，具备：
- 清晰的 DDD 架构
- 多 LLM 支持
- 可扩展的 Skills/Tools 系统
- 项目级记忆管理
- 基于角色的访问控制

该架构支持未来扩展，同时保持清晰的关注点分离。Skills 和 Tools 的基于文件配置方法支持快速迭代和轻松定制。
