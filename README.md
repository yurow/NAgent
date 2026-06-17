# NAgent - AI Agent Platform

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

NAgent is a comprehensive AI Agent platform built on .NET 8 with Domain-Driven Design (DDD) architecture. It provides a complete solution for building, managing, and deploying AI agents with multi-LLM support, project isolation, extensible Skills/Tools system, and project-level memory management.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [Project Structure](#project-structure)
- [API Reference](#api-reference)
- [Configuration](#configuration)
- [Security](#security)
- [Disclaimer](#disclaimer)
- [Contributing](#contributing)
- [License](#license)

## Features

### Core Features

- **User Authentication & Authorization**
  - JWT-based authentication with role-based access control (Admin/User)
  - Secure password hashing with configurable algorithms
  - Token validation and automatic expiration handling

- **Project Management**
  - Create, manage, and organize projects with workspace isolation
  - Each project has its own workspace directory and isolated memory
  - Project-level access control and management

- **Multi-LLM Provider Management**
  - Support for multiple LLM providers (OpenAI, Anthropic, Ollama, and more)
  - Dynamic provider configuration via API or configuration files
  - Model switching and usage statistics tracking
  - Protocol abstraction supporting OpenAI-compatible and Anthropic APIs

- **AI Agent Execution**
  - Non-streaming and Server-Sent Events (SSE) streaming execution
  - LangChain-based agent engine with extensible architecture
  - Semantic Kernel engine support (planned)
  - Session management with conversation history
  - **Reasoning chain display** - Shows the agent's thought process in the chat UI

- **Skills System**
  - Extensible Skills defined via Markdown documents with YAML Front Matter
  - Automatic loading from `skills/` directory
  - Skill categorization and version management
  - Association with Tools for capability composition
  - **Skill wraps Tool execution** - Model selects Skills, code handles Tool orchestration

- **Tools System**
  - Tool definitions via YAML configuration files
  - Security level classification (Low/Medium/High)
  - Multiple execution types: local, sandbox, HTTP, command
  - Parameter validation with type checking and enum support
  - **Built-in Tools**: file read/write, file listing, web search

- **Project-Level Memory System**
  - Short-term memory: Recent conversation context (last 20 messages)
  - Long-term memory: Persistent knowledge across sessions
  - Memory importance scoring and automatic expiration
  - Memory categories: UserPreference, ProjectKnowledge, CodePattern, ErrorSolution, Decision
  - Full project memory isolation - each project's memories are completely separate

- **User Management (Admin)**
  - User CRUD operations with role assignment
  - Account activation/deactivation
  - Password reset functionality
  - Admin-only access to system management features

### Frontend Features

- Single-page application experience with jQuery
- Real-time streaming chat interface
- Project management dashboard
- LLM provider and model configuration UI
- User management interface (admin)
- Skills and Tools management panels
- **Reasoning chain visualization** in chat messages

## Architecture

NAgent adopts a **Domain-Driven Design (DDD) layered architecture** with two parallel subsystems:

### Core System (Base DDD)

```
+-----------------------------------------+
|              NAgent.Api                  |  <- REST API, Middleware, Static Files
+-----------------------------------------+
|         NAgent.Application               |  <- CQRS Commands/Queries, DTOs, Validators
+-----------------------------------------+
|           NAgent.Domain                  |  <- Entities, Domain Events, Repository Interfaces
+-----------------------------------------+
|        NAgent.Infrastructure             |  <- SqlSugar + SQLite, Repository Implementations
+-----------------------------------------+
|           NAgent.Shared                  |  <- Common Response Models, Exceptions
+-----------------------------------------+
```

### Agent Subsystem (AI Extension)

```
+-----------------------------------------+
|              NAgent.Api                  |  <- Shared API Layer
+-----------------------------------------+
|      NAgent.AgentApplication             |  <- Agent CQRS, LLM Management
+-----------------------------------------+
|        NAgent.AgentDomain                |  <- Agent Entities, Memory, Skills/Tools
+-----------------------------------------+
|     NAgent.AgentInfrastructure           |  <- LangChain/SK Engines, Multi-Model LLM
+-----------------------------------------+
|         NAgent.AgentCore                 |  <- Agent Runner, Tool Dispatcher, Security
+-----------------------------------------+
```

### Dependency Flow

```
Api -> Application/AgentApplication -> Domain/AgentDomain -> Infrastructure/AgentInfrastructure
Shared is referenced by all layers
```

### Key Architectural Patterns

- **CQRS (Command Query Responsibility Segregation)**: Separates read and write operations using MediatR
- **Repository Pattern**: Abstracts data access with interfaces in Domain layer and implementations in Infrastructure
- **Dependency Injection**: Full DI container support with scoped, transient, and singleton lifetimes
- **Middleware Pipeline**: Custom JWT authentication middleware and global exception handling
- **Options Pattern**: Strongly-typed configuration binding

## Technology Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Runtime | .NET | 8.0 |
| Web Framework | ASP.NET Core | 8.0 |
| ORM | SqlSugar | 5.x |
| Database | SQLite | 3.x |
| CQRS | MediatR | 14.x |
| Object Mapping | AutoMapper | 16.x |
| Validation | FluentValidation | 12.x |
| Authentication | JWT (System.IdentityModel.Tokens.Jwt) | 7.x |
| Logging | Serilog | 4.x |
| API Documentation | Swashbuckle (Swagger) | 6.x |
| LLM Integration | LangChain.Core | 0.x |
| YAML Parsing | YamlDotNet | 16.x |
| Markdown Parsing | Markdig | 0.40.x |
| HTML Parsing | HtmlAgilityPack | 1.12.x |
| Frontend | jQuery | 3.7.1 |
| Testing | xUnit | 2.x |

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQLite](https://www.sqlite.org/download.html) (optional, embedded)

### Installation

```bash
# Clone the repository
git clone https://github.com/your-org/nagent.git
cd nagent

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project src/NAgent.Api
```

### Initial Setup

1. Open `http://localhost:9527` in your browser
2. The system will redirect to the initialization page on first run
3. Create the first admin account
4. Log in and start using NAgent

### Default Configuration

The application uses `appsettings.json` for configuration. Key settings:

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

## Documentation

- [Quick Start Guide](QUICKSTART.md) - Step-by-step tutorial for new users
- [Project Structure](PROJECT_STRUCTURE.md) - Detailed project organization
- [Generation Report](GENERATION_REPORT.md) - Development history and design decisions
- [Chinese Docs](README_CN.md) - Chinese documentation

## Project Structure

```
g:\NAgent/
├── src/                          # Source code (9 projects)
│   ├── NAgent.Api/               # REST API layer
│   ├── NAgent.Application/       # Core application layer
│   ├── NAgent.Domain/            # Core domain layer
│   ├── NAgent.Infrastructure/    # Core infrastructure layer
│   ├── NAgent.AgentApplication/  # Agent application layer
│   ├── NAgent.AgentDomain/       # Agent domain layer
│   ├── NAgent.AgentInfrastructure/# Agent infrastructure layer
│   ├── NAgent.AgentCore/         # Agent core runtime
│   └── NAgent.Shared/            # Shared components
├── tests/                        # Test projects
├── skills/                       # Skill Markdown files
├── tools/                        # Tool YAML configurations
└── docs/                         # Documentation
```

## API Reference

### Authentication

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | Public | User login |
| GET | `/api/auth/validate` | Bearer | Validate token |

### Projects

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/projects/user/{userId}` | Bearer | List user projects |
| POST | `/api/projects` | Bearer | Create project |
| DELETE | `/api/projects/{id}` | Bearer | Delete project |

### LLM Management

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/llm/providers` | Bearer | List providers |
| POST | `/api/llm/providers` | Bearer | Add provider |
| GET | `/api/llm/models` | Bearer | List models |
| POST | `/api/llm/models/switch` | Bearer | Switch model |

### Agent Execution

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/agent/execute` | Bearer | Execute agent |
| POST | `/api/agent/execute-stream` | Bearer | Stream execution |

### Skills & Tools

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/skills` | Bearer | List skills |
| POST | `/api/skills/reload` | Admin | Reload skills |
| GET | `/api/tools` | Bearer | List tools |

### Memory

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/memory/project/{id}/summary` | Bearer | Memory summary |
| POST | `/api/memory/project/{id}/long-term` | Bearer | Save memory |

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `JWT_SECRET` | JWT signing key | From appsettings |
| `DATABASE_PATH` | SQLite database path | `nagent.db` |

### Skills Directory

Place Markdown files in the `skills/` directory:

```markdown
---
name: my-skill
description: Skill description
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

### Tools Directory

Place YAML files in the `tools/` directory:

```yaml
name: my_tool
description: Tool description
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

## Security

- **JWT Authentication**: Stateless token-based auth with configurable expiration
- **Role-Based Authorization**: `[Authorize(Roles = "Admin")]` for admin endpoints
- **Password Hashing**: SHA256 with migration path to BCrypt/PBKDF2
- **Input Validation**: FluentValidation for all input DTOs
- **XSS Prevention**: HTML escaping in frontend
- **Prompt Injection Filter**: Detects and blocks malicious prompt patterns
- **Sandbox Execution**: High-risk tools execute in isolated environments

## Disclaimer

### Search Results Usage

NAgent includes built-in web search functionality (Baidu, Bing) for research and educational purposes only. The search results are retrieved programmatically and should be used in compliance with the respective search engines' Terms of Service.

- **Research Purpose Only**: Search results are intended for research, learning, and information gathering purposes.
- **Not for Bulk Scraping**: This tool is not designed for large-scale data collection, automated scraping, or commercial use.
- **Respect Rate Limits**: The system implements rate limiting (5-second intervals between searches) to avoid overloading search engine servers.
- **No Warranty**: The accuracy, completeness, or availability of search results is not guaranteed.
- **User Responsibility**: Users are responsible for complying with applicable laws and regulations when using search functionality.

## Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with .NET 8 and the amazing .NET ecosystem
- LLM integration powered by LangChain
- Frontend powered by jQuery and modern CSS
