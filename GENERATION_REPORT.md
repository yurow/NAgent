# Generation Report

This document records the development history, design decisions, and architectural evolution of the NAgent project.

## Project Genesis

NAgent was conceived as a comprehensive AI Agent platform built on .NET 8, following Domain-Driven Design (DDD) principles. The goal was to create a system that could manage AI agents with multi-LLM support, project isolation, extensible capabilities, and persistent memory.

## Development Timeline

### Phase 1: Foundation (Initial Development)

**Core System Setup**
- Established the DDD layered architecture with 5 core projects: Api, Application, Domain, Infrastructure, Shared
- Implemented user authentication with JWT tokens and role-based access control
- Created SQLite database schema with SqlSugar ORM
- Built system initialization flow for first-time setup

**Key Decisions:**
- Chose SqlSugar over Entity Framework for better SQLite performance and simpler configuration
- Implemented JWT authentication with custom middleware for token validation
- Created `ApiResponse<T>` wrapper for standardized API responses

### Phase 2: Agent Subsystem

**AI Agent Architecture**
- Added parallel Agent subsystem: AgentApplication, AgentDomain, AgentInfrastructure, AgentCore
- Designed `IAgentEngine` abstraction supporting multiple agent frameworks (LangChain, Semantic Kernel)
- Implemented `ExecuteAgentCommand` with full execution pipeline: validation -> security filter -> session management -> engine execution -> result validation -> persistence

**LLM Management**
- Created multi-provider LLM support (OpenAI, Anthropic, Ollama)
- Implemented protocol abstraction (`LlmProtocolType`) for OpenAI-compatible and Anthropic APIs
- Built model switching and usage tracking system
- Added daily usage statistics with `LlmModelDailyUsage`

**Key Decisions:**
- Used LangChain.Core for agent orchestration due to its maturity and ecosystem
- Implemented in-memory repositories for agent sessions and tools (can be migrated to persistent storage)
- Created `MultiModelLlmClient` to abstract multiple LLM providers behind a single interface

### Phase 3: Project Management

**Project System**
- Implemented project CRUD operations with workspace isolation
- Each project gets its own workspace directory on the filesystem
- Added project activation/deactivation lifecycle
- Created `IWorkspaceManager` interface to abstract filesystem operations

**Key Decisions:**
- Projects are owned by users and isolated by user ID
- Workspace paths are generated deterministically: `{basePath}/{userId}/{projectId}`
- Used `IWorkspaceManager` interface to decouple Application layer from Infrastructure

### Phase 4: Skills and Tools System

**Skills System**
- Designed Skills as Markdown documents with YAML Front Matter
- Created `Skill` entity with metadata (name, description, category, version, author)
- Implemented `SkillMarkdownParser` for parsing Markdown files
- Added skill loading from `skills/` directory with file system watching

**Tools System**
- Designed Tools as YAML configuration files
- Created `ToolDefinition` entity with parameters and execution configuration
- Implemented `ToolYamlParser` for YAML parsing
- Added security level classification (Low/Medium/High)
- Supported multiple execution types: local, sandbox, HTTP, command

**Key Decisions:**
- Skills describe "what the agent knows" (knowledge)
- Tools describe "what the agent can do" (capabilities)
- Both systems use file-based configuration for easy extensibility
- Security levels determine execution environment (local vs sandbox)

### Phase 5: Memory System

**Project-Level Memory**
- Implemented dual-memory architecture: short-term and long-term
- Short-term memory: Recent conversation context (20 messages, auto-overflow to long-term)
- Long-term memory: Persistent `ProjectMemory` entities with importance scoring
- Memory categories: General, UserPreference, ProjectKnowledge, CodePattern, ErrorSolution, Decision
- Memory importance levels: Low, Medium, High, Critical

**Memory Scoring and Expiration**
- Implemented `CalculateScore()` method: `Importance + AccessCount * 0.1 + RecencyBonus`
- Added expiration mechanism with automatic cleanup
- Memory isolation: Each project's memories are completely separate

**Key Decisions:**
- Memory is scoped to projects, not users (a user can have multiple projects with separate memories)
- In-memory storage for project memories (can be migrated to SQLite)
- File-based storage for session memories using JSON files

### Phase 6: User Management Enhancement

**Admin Features**
- Added user list endpoint (`GET /api/users`)
- Implemented user status toggle (active/inactive)
- Added role management (admin/user toggle)
- Created password reset functionality
- Added confirmation dialogs for destructive operations

**Frontend Improvements**
- Updated UserDto to use camelCase properties for JavaScript compatibility
- Added admin-only menu items with visibility control
- Implemented user action buttons (role toggle, status toggle, password reset)

### Phase 7: Frontend Dashboard Enhancement

**Navigation and UI**
- Added Tools Management and Skills Management navigation items
- Created content areas for Tools and Skills management
- Implemented admin-only visibility for management menus

**Project Management Tab**
- Removed activate/deactivate buttons from project cards
- Added "Chat" button for direct project access
- Implemented admin-only delete with double confirmation
- Added secondary confirmation for project deletion

**Key Decisions:**
- Used jQuery + vanilla JavaScript for frontend (no framework) to keep it lightweight
- Stored JWT token and user info in localStorage
- Used camelCase property names in DTOs to match JavaScript conventions

## Bug Fixes and Refinements

### Authentication Fixes
- Fixed `UserInfo` property naming from PascalCase to camelCase for JSON serialization compatibility
- Resolved `undefined` userId issue by ensuring consistent property names between backend and frontend

### Project Management Fixes
- Fixed missing `Project` table creation in database initialization
- Added SqlSugar attributes (`[SugarTable]`, `[SugarColumn]`) to `Project` entity
- Fixed `SqliteProjectRepository` query methods to use correct SqlSugar API
- Resolved workspace path inconsistency in `CreateProjectCommandHandler`

### Dependency Injection Fixes
- Fixed `ReloadAllSkillsCommandHandler` constructor to use `IConfiguration` instead of raw `string`
- Resolved circular dependency between `AgentApplication` and `Infrastructure` by moving `IWorkspaceManager` to `AgentDomain`

### Code Cleanup
- Removed empty `Class1.cs` files from Domain, Application, Infrastructure, Shared layers
- Replaced `Console.WriteLine` in `AppDbContext` with `ILogger`
- Extracted `IPasswordHasher` interface to decouple password hashing
- Converted project commands from mutable classes to immutable records
- Removed temporary `FixInvalidProtocolTypes` endpoint from `LlmController`

## Architecture Decisions

### Why DDD?
Domain-Driven Design was chosen because:
- Complex business logic (agent execution, memory management, tool dispatch)
- Need for clear boundaries between core system and AI subsystem
- Requirement for extensibility (new LLM providers, new tools, new skills)

### Why SqlSugar over EF Core?
- Better SQLite support with simpler configuration
- Performance advantages for the expected workload
- Smaller dependency footprint

### Why In-Memory Repositories for Agent Components?
- Agent sessions and memories are transient by nature
- Can be easily migrated to persistent storage later
- Simplifies initial development and testing

### Why File-Based Skills/Tools?
- Easy to version control (Markdown and YAML are text-based)
- Simple to edit and extend without database migrations
- Hot-reload capability for rapid iteration

### Why jQuery for Frontend?
- Lightweight, no build step required
- Sufficient for the dashboard's requirements
- Easy to maintain for a small team

## Technical Debt and Future Improvements

### Known Issues
1. **Password Hashing**: Currently uses SHA256, should migrate to BCrypt or PBKDF2 for production
2. **In-Memory Storage**: Agent sessions, skills, tools, and memories use in-memory storage
3. **AgentCore Duplication**: Some components in AgentCore overlap with AgentInfrastructure
4. **Semantic Kernel**: Engine implementation is placeholder (throws NotImplementedException)

### Planned Enhancements
1. **Persistent Memory Storage**: Migrate project memories to SQLite
2. **Vector Search**: Add vector-based memory search for semantic retrieval
3. **Plugin System**: Allow dynamic loading of .NET assemblies as tools
4. **Multi-Agent Collaboration**: Support for multiple agents working together
5. **WebSocket Support**: Real-time bidirectional communication for chat
6. **React/Vue Frontend**: Migrate to modern frontend framework

## Performance Considerations

### Current Optimizations
- In-memory caching for LLM model configurations
- Short-term memory limit (20 messages) with automatic overflow
- Memory scoring for intelligent retention

### Bottlenecks
- File-based session memory I/O
- In-memory repository search (linear scan)
- LLM API call latency (external dependency)

## Security Considerations

### Implemented
- JWT authentication with configurable expiration
- Role-based authorization
- Password hashing (SHA256, with migration path)
- Prompt injection filtering
- Sandbox execution for high-risk tools
- HTML escaping in frontend

### Needs Improvement
- Password hashing algorithm (migrate to BCrypt)
- Rate limiting on API endpoints
- Input sanitization for file uploads
- CSRF protection for state-changing operations

## Conclusion

NAgent has evolved from a simple concept to a comprehensive AI Agent platform with:
- Clean DDD architecture
- Multi-LLM support
- Extensible Skills/Tools system
- Project-level memory management
- Role-based access control

The architecture supports future extensions while maintaining clean separation of concerns. The file-based configuration approach for Skills and Tools enables rapid iteration and easy customization.
