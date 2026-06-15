# 快速开始指南

本指南将帮助您在几分钟内启动并运行 NAgent。

## 前置条件

在开始之前，请确保已安装以下软件：

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本
- 现代 Web 浏览器（Chrome、Firefox、Edge、Safari）
- （可选）[Git](https://git-scm.com/) 用于克隆仓库

## 第一步：获取代码

### 选项 A：从 Git 克隆

```bash
git clone https://github.com/your-org/nagent.git
cd nagent
```

### 选项 B：下载 ZIP

下载最新的发布版 ZIP 文件并解压到您喜欢的位置。

## 第二步：构建项目

```bash
# 还原 NuGet 包
dotnet restore

# 构建整个解决方案
dotnet build
```

如果构建成功，您将看到类似输出：
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## 第三步：运行应用

```bash
dotnet run --project src/NAgent.Api
```

应用将启动并监听：
- HTTP: `http://localhost:9527`

您将看到类似输出：
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:9527
```

## 第四步：初始设置

1. 打开浏览器并访问 `http://localhost:9527`
2. 您将被重定向到**系统初始化**页面
3. 填写表单：
   - **用户名**：选择管理员用户名（例如 `admin`）
   - **邮箱**：您的邮箱地址
   - **密码**：强密码（至少 6 个字符）
4. 点击**初始化系统**
5. 系统将创建管理员账户并初始化数据库

## 第五步：登录

1. 初始化后，您将被重定向到**登录**页面
2. 输入您的管理员凭据
3. 点击**登录**
4. 您将进入**仪表盘**

## 第六步：配置 LLM 提供商

在使用 AI Agent 之前，您需要配置至少一个 LLM 提供商：

1. 在仪表盘侧边栏中，点击**模型管理**（仅管理员）
2. 点击**添加提供商**
3. 填写提供商详情：
   - **名称**：例如 "OpenAI"
   - **协议类型**：OpenAI 或 Anthropic
   - **基础 URL**：例如 `https://api.openai.com`
   - **API 密钥**：从提供商获取的 API 密钥
4. 点击**保存**
5. 系统将自动从提供商获取可用模型

### 支持的提供商

| 提供商 | 协议 | 基础 URL 示例 |
|--------|------|--------------|
| OpenAI | OpenAI | `https://api.openai.com` |
| Anthropic | Anthropic | `https://api.anthropic.com` |
| Ollama（本地） | OpenAI | `http://localhost:11434` |
| Azure OpenAI | OpenAI | `https://your-resource.openai.azure.com` |

## 第七步：创建您的第一个项目

1. 在仪表盘侧边栏中，点击**项目管理**
2. 点击**创建新项目**
3. 填写：
   - **项目名称**：例如 "我的第一个项目"
   - **描述**：可选的项目描述
4. 点击**创建**
5. 项目将被创建并拥有自己的工作空间目录

## 第八步：与 AI Agent 聊天

1. 在侧边栏中，点击**AI Agent 聊天**
2. 从下拉菜单中选择您的项目
3. 在输入框中输入消息并按回车或点击**发送**
4. AI 将根据选定的 LLM 模型进行回复

### 流式模式

对于实时响应，聊天界面使用服务器发送事件（SSE）逐字流式传输 AI 的回复。

## 第九步：添加 Skills（可选）

Skills 扩展了 AI Agent 的能力：

1. 在 `skills/` 目录中创建 Markdown 文件：

```markdown
---
name: code-review
description: 审查代码质量和错误
category: development
version: 1.0.0
---

# 代码审查 Skill

## 概述
本 Skill 帮助审查代码的质量问题、错误和改进点。

## Tools
- code_linter
- security_scanner

## 示例
#### 示例 1：审查 Python 代码
Input: ```python
def add(a, b):
    return a + b
```
Output: 代码简单且正确。建议添加类型提示。
```

2. 在仪表盘中进入 **Skills 管理**
3. 点击**重新加载所有 Skills**
4. 新 Skill 将被加载并可供 Agent 使用

## 第十步：添加 Tools（可选）

Tools 为 Agent 提供可执行的能力：

1. 在 `tools/` 目录中创建 YAML 文件：

```yaml
name: weather_lookup
description: 获取某地的当前天气
category: utility
security_level: low

parameters:
  - name: city
    description: 城市名称
    type: string
    required: true

execution:
  type: http
  endpoint: "https://api.weather.com/v1/current"
  http_method: GET
  timeout_seconds: 10
```

2. 重启应用以加载新 Tools

## 常见任务

### 切换 LLM 模型

1. 进入**模型管理**
2. 找到您想使用的模型
3. 点击**设为默认**或在聊天界面使用模型切换器

### 管理用户（管理员）

1. 在侧边栏中进入**用户管理**
2. 查看所有用户、创建新账户或管理现有账户
3. 切换管理员角色、启用/禁用账户或重置密码

### 清除项目记忆

1. 进入**项目管理**
2. 找到您的项目
3. 记忆可以通过 API 清除或根据过期设置自动管理

## 故障排除

### 端口已被占用

如果端口 9527 已被占用，您可以在 `src/NAgent.Api/Properties/launchSettings.json` 中更改：

```json
"applicationUrl": "http://localhost:9527"
```

### 数据库问题

如果遇到数据库错误：

```bash
# 删除数据库文件以重新开始
rm nagent.db

# 重新运行应用
dotnet run --project src/NAgent.Api
```

### LLM 提供商连接失败

- 验证您的 API 密钥是否正确
- 检查基础 URL 是否可从您的网络访问
- 对于本地提供商（Ollama），确保服务正在运行

### 构建错误

```bash
# 清理并重新构建
dotnet clean
dotnet restore
dotnet build
```

## 下一步

- 阅读 [项目结构](PROJECT_STRUCTURE_CN.md) 了解代码库
- 查看 [API 参考](README_CN.md#api-参考) 进行编程访问
- 探索 [开发报告](GENERATION_REPORT_CN.md) 了解设计决策

## 获取帮助

- 查看 [Issues](https://github.com/your-org/nagent/issues) 页面
- 查看 `logs/` 目录中的日志
- 在 `appsettings.json` 中设置 `"Default": "Debug"` 启用详细日志
