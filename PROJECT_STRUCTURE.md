# Project Structure

This document provides a comprehensive overview of the NAgent project organization, explaining the purpose of each directory and key files.

## Root Directory

```
NAgent/
├── src/                          # Source code
├── tests/                        # Test projects
├── docs/                         # Documentation
├── skills/                       # Skill markdown files
├── tools/                        # Tool YAML configuration files
├── nagent.db                     # SQLite database (generated at runtime)
├── NAgent.WebApi.slnx            # Solution file
├── README.md                     # Project overview
├── QUICKSTART.md                 # Getting started guide
├── PROJECT_STRUCTURE.md          # This file
└── GENERATION_REPORT.md          # Development history
```

## Source Code Organization (`src/`)

The source code is organized into two parallel subsystems following Domain-Driven Design principles.

### Core System

#### 1. NAgent.Api

**Purpose**: REST API layer, middleware, static file serving

```
NAgent.Api/
├── Controllers/                  # API Controllers
│   ├── AuthController.cs         # Authentication endpoints
│   ├── UsersController.cs        # User management
│   ├── ProjectsController.cs     # Project management
│   ├── LlmController.cs          # LLM provider/model management
│   ├── AgentController.cs        # Agent execution
│   ├── SkillsController.cs       # Skills management
│   ├── MemoryController.cs       # Memory management
│   └── InitializationController.cs # System initialization
├── Middleware/
│   ├── JwtAuthenticationMiddleware.cs  # JWT token validation
│   └── GlobalExceptionHandler.cs       # Global exception handling
├── wwwroot/                      # Static files
│   ├── index.html               # Entry point
│   ├── login.html               # Login page
│   ├── init.html                # System initialization page
│   ├── dashboard.html           # Main dashboard (SPA)
│   ├── css/                     # Stylesheets
│   │   ├── login.css
│   │   ├── init.css
│   │   └── dashboard.css
│   └── js/                      # JavaScript files
│       ├── login.js             # Login logic
│       ├── init.js              # Initialization logic
│       └── dashboard.js         # Main application logic
├── appsettings.json             # Application configuration
├── appsettings.Development.json # Development configuration
└── Program.cs                   # Application entry point
```

#### 2. NAgent.Application

**Purpose**: Application layer containing CQRS commands/queries, DTOs, validators, and mapping profiles

```
NAgent.Application/
├── DTOs/
│   └── UserDto.cs               # User data transfer object
├── Features/                    # Feature-organized CQRS
│   ├── Auth/
│   │   └── Queries/
│   │       ├── LoginQuery.cs           # Login query
│   │       └── LoginQueryHandler.cs    # Login handler
│   ├── Users/
│   │   ├── Commands/
│   │   │   ├── CreateUserCommand.cs           # Create user
│   │   │   ├── CreateUserCommandHandler.cs
│   │   │   ├── UpdateUserStatusCommand.cs     # Update status
│   │   │   ├── UpdateUserRoleCommand.cs       # Update role
│   │   │   ├── ResetUserPasswordCommand.cs    # Reset password
│   │   │   └── UserManagementCommandHandlers.cs
│   │   └── Queries/
│   │       ├── GetUserByIdQuery.cs
│   │       ├── GetUserByIdQueryHandler.cs
│   │       ├── GetAllUsersQuery.cs
│   │       └── GetAllUsersQueryHandler.cs
│   └── ... (other features)
├── Mappings/
│   └── MappingProfile.cs        # AutoMapper configuration
├── Interfaces/
│   ├── IJwtTokenService.cs      # JWT token generation
│   ├── IPasswordHasher.cs       # Password hashing abstraction
│   └── IDateTimeService.cs      # DateTime abstraction
└── Validators/                  # FluentValidation validators
```

#### 3. NAgent.Domain

**Purpose**: Domain layer containing core business entities, domain events, repository interfaces, and domain exceptions

```
NAgent.Domain/
├── Entities/
│   └── User.cs                  # User entity
├── Enums/
├── Events/                      # Domain events
├── Exceptions/
│   └── DomainException.cs       # Base domain exception
├── Repositories/
│   ├── IRepository.cs           # Generic repository interface
│   └── IUserRepository.cs       # User repository interface
└── ValueObjects/                # Value objects
```

#### 4. NAgent.Infrastructure

**Purpose**: Infrastructure layer containing data persistence, external service implementations

```
NAgent.Infrastructure/
├── Persistence/
│   └── AppDbContext.cs          # SqlSugar database context
├── Repositories/
│   ├── SqliteRepository.cs      # Generic SQLite repository
│   ├── SqliteUserRepository.cs  # User repository implementation
│   └── ...
├── Services/
│   ├── JwtTokenService.cs       # JWT implementation
│   ├── Sha256PasswordHasher.cs  # SHA256 password hasher
│   └── InitializationService.cs # System initialization
└── DependencyInjection.cs       # DI registration
```

#### 5. NAgent.Shared

**Purpose**: Shared components used across all layers

```
NAgent.Shared/
└── Responses/
    └── ApiResponse.cs           # Standard API response wrapper
```

### Agent Subsystem

#### 6. NAgent.AgentApplication

**Purpose**: Application layer for the AI Agent subsystem

```
NAgent.AgentApplication/
├── Features/
│   ├── ExecuteAgent/
│   │   └── Commands/
│   │       ├── ExecuteAgentCommand.cs
│   │       └── ExecuteAgentCommandHandler.cs
│   ├── LlmManagement/
│   │   ├── Commands/            # LLM provider/model commands
│   │   └── Queries/             # LLM provider/model queries
│   ├── Projects/
│   │   ├── Commands/            # Project commands
│   │   └── Queries/             # Project queries
│   ├── Skills/
│   │   ├── Commands/            # Skill commands
│   │   └── Queries/             # Skill queries
│   └── Memory/
│       ├── Commands/            # Memory commands
│       └── Queries/             # Memory queries
└── Interfaces/
    ├── IAgentEngine.cs          # Agent engine abstraction
    ├── ILlmClient.cs            # LLM client interface
    ├── ISandboxExecutor.cs      # Sandbox executor interface
    └── ISecurity.cs             # Security component interfaces
```

#### 7. NAgent.AgentDomain

**Purpose**: Domain layer for AI Agent entities and services

```
NAgent.AgentDomain/
├── Entities/
│   ├── AgentSession.cs          # Agent session entity
│   ├── AgentTool.cs             # Agent tool entity
│   ├── Project.cs               # Project entity
│   ├── ProjectMemory.cs         # Project memory entity
│   ├── Skill.cs                 # Skill entity
│   └── ToolDefinition.cs        # Tool definition entity
├── Enums/
│   ├── LlmProtocolType.cs       # LLM protocol types
│   ├── ToolSecurityLevel.cs     # Tool security levels
│   ├── MemoryCategory.cs        # Memory categories
│   └── MemoryImportance.cs      # Memory importance levels
├── Repositories/
│   ├── IAgentSessionRepository.cs
│   ├── IAgentToolRepository.cs
│   ├── IProjectRepository.cs
│   ├── IProjectMemoryRepository.cs
│   ├── ISkillRepository.cs
│   └── IToolDefinitionRepository.cs
└── Services/
    └── Memory/
        ├── IMemorySystem.cs           # Memory system interface
        ├── IMemoryStorage.cs          # Memory storage interface
        ├── DefaultMemorySystem.cs     # Default implementation
        ├── FileMemoryStorage.cs       # File-based storage
        └── MemorySystemFactory.cs     # Memory system factory
```

#### 8. NAgent.AgentInfrastructure

**Purpose**: Infrastructure for AI Agent execution

```
NAgent.AgentInfrastructure/
├── Agents/
│   ├── AgentEngineFactory.cs          # Engine factory
│   ├── LangChain/
│   │   └── LangChainAgentEngine.cs    # LangChain implementation
│   └── SemanticKernel/
│       └── SemanticKernelAgentEngine.cs # SK implementation
├── Llm/
│   └── MultiModelLlmClient.cs         # Multi-model LLM client
├── Parsers/
│   ├── SkillMarkdownParser.cs         # Skill MD parser
│   └── ToolYamlParser.cs              # Tool YAML parser
├── Repositories/
│   ├── InMemoryAgentSessionRepository.cs
│   ├── InMemoryAgentToolRepository.cs
│   ├── InMemoryProjectMemoryRepository.cs
│   ├── InMemorySkillRepository.cs
│   ├── InMemoryToolDefinitionRepository.cs
│   ├── SqliteLlmModelRepository.cs
│   └── SqliteLlmProviderRepository.cs
├── Sandbox/
│   └── CubeSandboxExecutorImpl.cs     # Sandbox executor
├── Security/
│   ├── PromptFilterImpl.cs            # Prompt injection filter
│   └── SandboxResultValidatorImpl.cs  # Sandbox result validator
└── DependencyInjection.cs             # DI registration
```

#### 9. NAgent.AgentCore

**Purpose**: Core agent runtime components

```
NAgent.AgentCore/
├── Agent/
│   ├── AgentRunner.cs           # Main agent runner
│   ├── ToolDispatcher.cs        # Tool dispatch logic
│   └── MemoryManager.cs         # Memory management
├── LLm/
│   └── LocalLlmClient.cs        # Local LLM client
├── Sandbox/
│   └── CubeSandboxClient.cs     # Sandbox client
├── Security/
│   ├── PromptFilter.cs          # Prompt filter
│   ├── SandboxResultCheck.cs    # Sandbox result checker
│   └── ToolLevelConfig.cs       # Tool security config
├── Tools/
│   ├── LocalTools/              # Local tool implementations
│   │   └── CalculatorTool.cs
│   └── SandboxTools/            # Sandbox tool implementations
│       └── CodeExecutorTool.cs
└── DependencyInjection.cs       # DI registration
```

## Test Projects (`tests/`)

```
tests/
└── NAgent.Api.Tests/
    ├── Controllers/             # Controller tests
    └── Integration/             # Integration tests
```

## Configuration Files

### appsettings.json

Main application configuration:
- Database connection strings
- JWT settings
- Agent configuration
- LLM provider presets
- Logging configuration

### launchSettings.json

Development environment settings:
- Application URLs
- Environment variables
- Launch profiles

## Database Schema

### SQLite Tables

1. **Users** - User accounts
2. **projects** - Projects
3. **LlmProviders** - LLM providers
4. **LlmModels** - LLM models
5. **LlmModelDailyUsages** - Daily usage statistics

### In-Memory Storage

The following use in-memory repositories (can be migrated to persistent storage):
- Agent Sessions
- Agent Tools
- Skills
- Tool Definitions
- Project Memories

## Dependency Graph

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

## Naming Conventions

### Files
- **Controllers**: `*Controller.cs`
- **Commands**: `*Command.cs`, `*CommandHandler.cs`
- **Queries**: `*Query.cs`, `*QueryHandler.cs`
- **Entities**: PascalCase, singular (e.g., `User.cs`)
- **Repositories**: `I*Repository.cs` (interface), `*Repository.cs` (implementation)
- **Services**: `I*Service.cs` (interface), `*Service.cs` (implementation)

### Classes/Interfaces
- **Interfaces**: PascalCase with `I` prefix (e.g., `IUserRepository`)
- **Entities**: PascalCase (e.g., `User`, `Project`)
- **DTOs**: PascalCase with camelCase properties for JSON serialization
- **Enums**: PascalCase (e.g., `ToolSecurityLevel`)

### Methods
- **Async methods**: Suffix with `Async` (e.g., `GetByIdAsync`)
- **Command handlers**: `Handle(*Command, CancellationToken)`
- **Query handlers**: `Handle(*Query, CancellationToken)`

## Adding New Features

To add a new feature:

1. **Domain Layer**: Define entities and repository interfaces in `NAgent.AgentDomain`
2. **Application Layer**: Create commands/queries and handlers in `NAgent.AgentApplication`
3. **Infrastructure Layer**: Implement repositories in `NAgent.AgentInfrastructure`
4. **API Layer**: Add controller endpoints in `NAgent.Api`
5. **Frontend**: Update `dashboard.html` and `dashboard.js`

## Build Configuration

### Solution File

`NAgent.WebApi.slnx` defines the solution structure and project references.

### Project Files

Each `.csproj` file defines:
- Target framework (`net8.0`)
- Package references
- Project references
- Output settings

### Key NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT authentication |
| `SqlSugarCore` | ORM for SQLite |
| `MediatR` | CQRS implementation |
| `AutoMapper` | Object mapping |
| `FluentValidation` | Input validation |
| `Serilog` | Structured logging |
| `Swashbuckle.AspNetCore` | Swagger/OpenAPI |
| `YamlDotNet` | YAML parsing |
| `Markdig` | Markdown parsing |
| `LangChain.Core` | LLM integration |
