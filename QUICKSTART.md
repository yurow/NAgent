# 🚀 NAgent 快速开始指南

## 前置要求

在开始之前，请确保您的系统已安装以下软件：

- ✅ [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ [PostgreSQL 12+](https://www.postgresql.org/download/)
- ✅ [Git](https://git-scm.com/downloads)
- ✅ IDE推荐: [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [VS Code](https://code.visualstudio.com/) + [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

## 📥 第一步：克隆项目

```bash
git clone <repository-url>
cd NAgent
```

## 🗄️ 第二步：设置数据库

### 选项A：使用本地PostgreSQL

1. 安装PostgreSQL（如果尚未安装）

2. 创建数据库：
   ```bash
   # Windows (使用pgAdmin或psql)
   psql -U postgres
   CREATE DATABASE nagent_db;
   \q
   ```

3. 修改连接字符串：
   
   编辑 `src/NAgent.Api/appsettings.json`：
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=nagent_db;Username=postgres;Password=your_password"
     }
   }
   ```

### 选项B：使用Docker（推荐用于开发环境）

```bash
# 启动PostgreSQL容器
docker run --name nagent-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=nagent_db \
  -p 5432:5432 \
  -d postgres:16-alpine

# 验证容器运行
docker ps
```

连接字符串保持不变（默认密码为`postgres`）。

## 🔨 第三步：还原依赖并构建

```bash
# 还原NuGet包
dotnet restore

# 构建项目
dotnet build
```

## 📊 第四步：创建数据库迁移

```bash
# 进入API项目目录
cd src/NAgent.Api

# 添加初始迁移
dotnet ef migrations add InitialCreate -o ../NAgent.Infrastructure/Migrations

# 应用迁移到数据库
dotnet ef database update
```

如果看到类似输出，说明成功：
```
Done.
```

## ▶️ 第五步：运行应用

```bash
# 仍在 src/NAgent.Api 目录下
dotnet run
```

您应该看到类似输出：
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

## 🧪 第六步：测试API

### 方法1：使用Swagger UI

打开浏览器访问：
- **HTTPS**: https://localhost:5001/swagger
- **HTTP**: http://localhost:5000/swagger

### 方法2：使用cURL

#### 创建用户
```bash
curl -X POST https://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "username": "john_doe",
    "email": "john@example.com"
  }'
```

响应示例：
```json
{
  "success": true,
  "message": "用户创建成功",
  "data": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

#### 获取用户
```bash
# 替换 {userId} 为上一步返回的ID
curl -X GET https://localhost:5001/api/users/{userId} \
  -k
```

响应示例：
```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "username": "john_doe",
    "email": "john@example.com",
    "isActive": true,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": null
  }
}
```

### 方法3：使用Postman

1. 导入集合或手动创建请求
2. 设置SSL证书验证为关闭（或使用自签名证书）
3. 发送请求

## 🧪 第七步：运行测试

### 单元测试
```bash
cd tests/NAgent.UnitTests
dotnet test
```

### 集成测试
```bash
cd tests/NAgent.IntegrationTests
dotnet test
```

## 📝 常见问题

### Q1: 遇到 "无法连接到数据库" 错误

**解决方案：**
1. 确认PostgreSQL服务正在运行
2. 检查连接字符串中的主机、端口、用户名和密码
3. 确认数据库 `nagent_db` 已创建

### Q2: HTTPS证书警告

**解决方案（仅开发环境）：**
```bash
# 信任ASP.NET Core HTTPS开发证书
dotnet dev-certs https --trust
```

或在浏览器中点击"高级" → "继续访问"

### Q3: 迁移失败

**解决方案：**
```bash
# 删除现有迁移
rm -rf src/NAgent.Infrastructure/Migrations

# 重新创建迁移
cd src/NAgent.Api
dotnet ef migrations add InitialCreate -o ../NAgent.Infrastructure/Migrations
dotnet ef database update
```

### Q4: 端口被占用

**解决方案：**
编辑 `src/NAgent.Api/Properties/launchSettings.json`，更改端口号。

## 🎯 下一步

- 📖 阅读 [README.md](README.md) 了解架构设计
- 📂 查看 [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) 了解项目结构
- 📜 阅读 [csharp-dotnet-ddd.md](csharp-dotnet-ddd.md) 了解编码规范
- 🔍 探索代码示例，理解DDD模式实现

## 💡 开发提示

### 添加新功能的标准流程

1. **Domain层**: 创建实体、值对象、领域事件
2. **Application层**: 创建Command/Query、Handler、DTO、Validator
3. **Infrastructure层**: 实现仓储、配置EF Core
4. **API层**: 创建Controller端点

详细步骤请参考 [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) 中的"添加新功能"章节。

## 🤝 需要帮助？

- 提交 [Issue](../../issues)
- 查看 [Discussions](../../discussions)
- 阅读文档

---

**祝您开发愉快！** 🎉
