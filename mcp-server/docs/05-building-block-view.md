# 5. Building Block View

## Overview

PersonaMcpServer follows a layered architecture with clear separation of concerns. The decomposition uses four main abstraction levels: the entry point and hosting layer, MCP protocol tool handlers (not resources), business logic services, and configuration management. This hierarchical approach enables maintainability, testability, and extensibility while supporting the provider-agnostic requirement and complete instruction file lifecycle management (discovery, creation, validation).

## Level 1: Overall System (White-box) **MANDATORY**

### Overview Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    PersonaMcpServer                              │
├─────────────────┬──────────────────┬─────────────────┬─────────────┤
│   Application   │   MCP Tools &    │  Business Logic │ Configuration│
│    Host         │   Prompts        │   Services      │             │
├─────────────────┼──────────────────┼─────────────────┼─────────────┤
│  Program.cs     │ PersonaMcpTools  │ PersonaInstruct │ PersonaServer│
│  DI Container   │ ProjectMcpTools  │ ProjectInstruct │ Config       │
│  Startup        │ ValidationPrompts│ Validation      │ Environment  │
│                 │ TemplateMcpTools │ TemplateService │              │
│                 │                  │ PromptService   │              │
└─────────────────┴──────────────────┴─────────────────┴─────────────┘

External Interfaces:
│
├─ MCP Clients (Claude, Copilot, OpenAI, Custom)
│  └─ STDIO transport (Model Context Protocol)
│
└─ File System
   ├─ ../templates/ (for template discovery)
   ├─ ../prompts/ (for validation prompts)
   └─ ./personas/ & ./projects/ (instruction file storage)
```

**Legend:**
- [Component] = Building block (layer)
- ↔ = Bidirectional dependency/communication
- → = Unidirectional dependency

### Contained Building Blocks

| Name | Responsibility | Key Interfaces |
|------|---------------|----------------|
| **Application Host** | Entry point, DI container setup, tool/prompt registration, application lifecycle | IF-01 (DI Registration), IF-02 (Hosting) |
| **MCP Tools & Prompts** | MCP protocol tool handlers and prompt generation for instruction management, creation, and validation | IF-03 (MCP Tools), IF-04 (MCP Prompts) |
| **Business Logic Services** | Instruction file CRUD, template-based creation, validation logic, caching | IF-05 (Service API) |
| **Configuration** | Settings management, environment variable handling, startup configuration | IF-06 (Config Access) |

### Decomposition Rationale

This layered decomposition was chosen to:

1. **Support Quality Goals**: Clear layers enable testing (#reliable), maintainability (#flexible), and performance optimization (#efficient)
2. **Align with .NET Conventions**: Uses Microsoft.Extensions.Hosting and dependency injection patterns familiar to .NET developers
3. **Enable Provider Agnosticism**: MCP protocol tool layer isolates transport concerns from business logic
4. **Facilitate Extensibility**: New MCP tools and prompts can be added without modifying existing services
5. **Separate Concerns**: Configuration, business logic, MCP tool handling, and hosting have distinct responsibilities
6. **Support Complete Workflows**: Template discovery → File creation → Validation → Prompt-based review

**Important Note on Design**: This implementation uses MCP **Tools** (not Resources) for instruction file access, enabling more flexible query patterns and better client-side control over tool invocation.

Links to Section 4 (Solution Strategy): Implements the layered architecture strategy with tool-based MCP implementation and dependency injection pattern.

---

## Building Block: Application Host (Black-box)

### Purpose/Responsibility
Provides application entry point, dependency injection container setup, and application lifecycle management. Hosts the MCP server and manages transport selection (HTTP/SSE vs STDIO).

### Interfaces

| Interface | Description | Type | Technology |
|-----------|-------------|------|-----------|
| IF-01 | Dependency Registration | Provided | Microsoft.Extensions.DI |
| IF-02 | Application Hosting | Provided | Microsoft.Extensions.Hosting |
| IF-08 | MCP Transport Selection | Required | ModelContextProtocol.AspNetCore |

### Quality Attributes
- **Reliability**: Graceful startup/shutdown, proper resource cleanup
- **Usability**: Clear startup logging, configuration validation
- **Flexibility**: Support for different transport modes via configuration

### Directory/File Location
- Source: `/Program.cs`, `/Startup.cs` (if used)
- Configuration: `/appsettings.json`, `/appsettings.{Environment}.json`

### Fulfilled Requirements
- **NFR-012**: Dependency injection container setup
- **FR-001**: Server initialization with proper DI container
- **FR-003**: Transport selection (HTTP/SSE vs STDIO)
- **NFR-071**: Startup logging with configuration details

### Open Issues
- None identified

---

## Building Block: MCP Tools & Prompts (Black-box)

### Purpose/Responsibility
Implements MCP protocol tools for instruction file management and prompts for validation guidance. Tools provide queryable access to instruction files with parameters (discovery, retrieval, creation, validation). Prompts enable validation workflows with optional content injection.

### Interfaces

| Interface | Description | Type | Technology |
|-----------|-------------|------|-----------|
| IF-03 | MCP Tools | Provided | ModelContextProtocol (18 tools total) |
| IF-04 | MCP Prompts | Provided | ModelContextProtocol (2 prompts) |
| IF-05 | Business Logic Access | Required | Internal service calls |

### Quality Attributes
- **Performance**: Fast tool execution, async operations throughout
- **Security**: Input validation, parameter sanitization, path validation
- **Flexibility**: Provider-agnostic MCP protocol compliance, parameter-based queries

### Directory/File Location
- Tools: `/Server/PersonaMcpTools.cs`, `/Server/ProjectMcpTools.cs`, `/Server/TemplateMcpTools.cs`
- Prompts: `/Server/ValidationPrompts.cs`, `/Prompts/*.txt`
- Tests: `/Tests/Server/`

### MCP Tools Overview (18 total)

**Template Discovery (3 tools):**
- `template_list` - Discover available templates
- `template_get_persona` - Get persona template structure
- `template_get_project` - Get project template structure

**Instruction Management (14 tools):**
- `persona_list`, `persona_get`, `persona_exists`, `persona_get_current`, `persona_set_current`, `persona_invalidate_cache`, `persona_refresh_cache`
- `project_list`, `project_get`, `project_exists`, `project_get_current`, `project_set_current`, `project_invalidate_cache`, `project_refresh_cache`

**Instruction Creation (2 tools):**
- `persona_create_from_template` - Create persona from template
- `project_create_from_template` - Create project from template

**Instruction Validation (2 tools):**
- `persona_validate` - Validate persona file
- `project_validate` - Validate project file

### MCP Prompts Overview (2 total)

**Validation Prompts:**
- `validate_persona_prompt` - Guidance for persona validation with optional content injection
- `validate_project_prompt` - Guidance for project validation with optional content injection

### Fulfilled Requirements
- **FR-020-036**: All tool and prompt requirements
- **NFR-013**: MCP tool registration and auto-discovery via [McpServerTool] attributes
- **NFR-014**: Prompt functionality
- **NFR-010**: Provider-agnostic implementation

### Design Decision: Tools vs Resources
This implementation uses MCP **Tools** instead of **Resources** for instruction file access. **Rationale:**
- Tools provide better query flexibility (parameters for filtering, options)
- Clients have explicit control over tool invocation
- Supports diverse operations (create, validate, list with filters)
- More aligned with MCP best practices for active operations

### Open Issues
- None identified

---

## Building Block: Business Logic Services (Black-box)

### Purpose/Responsibility
Core instruction file management including CRUD operations, template-based file creation, validation logic, caching strategy, and service coordination. Provides IPersonaInstructionService, IProjectInstructionService, ITemplateService, and IPromptService for all business operations.

### Interfaces

| Interface | Description | Type | Technology |
|-----------|-------------|------|-----------|
| IF-05 | Service API | Provided | IPersonaInstructionService, IProjectInstructionService, ITemplateService, IPromptService |
| IF-07 | File Access | Required | System.IO (async) |

### Quality Attributes
- **Performance**: <50ms cached responses, <200ms fresh reads, cache hit optimization with TTL
- **Security**: Path validation, directory traversal prevention, file existence checks
- **Reliability**: Error handling, graceful degradation, thread-safe caching with SemaphoreSlim
- **Maintainability**: Clean service layer separation, testable interfaces

### Directory/File Location
- Interfaces: `/Services/IPersonaInstructionService.cs`, `/Services/IProjectInstructionService.cs`, `/Services/ITemplateService.cs`, `/Services/IPromptService.cs`
- Implementation: `/Services/PersonaInstructionService.cs`, `/Services/ProjectInstructionService.cs`, `/Services/TemplateService.cs`, `/Services/PromptService.cs`
- Tests: `/Tests/Services/`

### Service Responsibilities

**Instruction Services (IPersonaInstructionService, IProjectInstructionService):**
- List available instruction files
- Get specific instruction by name
- Check instruction existence
- Get/set current active instruction
- Create instruction from template
- Validate instruction file
- Cache management (invalidate, refresh)

**Template Service (ITemplateService):**
- Discover available templates
- Load persona template
- Load project template
- Cache template content

**Prompt Service (IPromptService):**
- Load validation prompts
- Support optional content injection

### Fulfilled Requirements
- **FR-010-036**: All instruction management, creation, and validation requirements
- **NFR-020-024**: Caching with TTL, asynchronous operations, memory efficiency
- **NFR-030**: Path validation and security

### Open Issues
- None identified

---

## Building Block: Configuration (Black-box)

### Purpose/Responsibility
Manages application configuration from appsettings.json and environment variables. Provides strongly-typed PersonaServerConfig with validation and supports environment-specific overrides via PERSONA_* environment variable prefix.

### Interfaces

| Interface | Description | Type | Technology |
|-----------|-------------|------|-----------|
| IF-06 | Configuration Access | Provided | Microsoft.Extensions.Configuration, IOptions<PersonaServerConfig> |
| IF-08 | Environment Variables | Required | System.Environment (PERSONA_ prefix) |

### Quality Attributes
- **Usability**: Clear configuration options, sensible defaults, validation on startup
- **Flexibility**: Environment-specific configuration (Development/Production), Docker/cloud compatible
- **Security**: Environment variable support for sensitive settings, no hardcoded values

### Directory/File Location
- Configuration Model: `/PersonaServerConfig.cs`
- Configuration Files: `/appsettings.json`, `/appsettings.Development.json`, `/appsettings.Production.json`
- Tests: `/Tests/ConfigurationTests.cs`

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| PersonaRepoPath | string | "./personas" | Directory containing instruction files |
| CacheTtlSeconds | int | 300 | Time-to-live for cached files (seconds) |
| MaxCacheSizeBytes | long | 10485760 | Maximum total cache size (10MB default) |
| CurrentPersona | string? | null | Name of currently active persona |
| CurrentProject | string? | null | Name of currently active project |

### Environment Variable Overrides

Prefix with `PERSONA_` and use double underscore for nested properties:
- `PERSONA_PersonaServer__PersonaRepoPath`
- `PERSONA_PersonaServer__CacheTtlSeconds`

### Fulfilled Requirements
- **FR-002**: Configuration loading from appsettings.json and environment
- **NFR-032**: Environment-based configuration management
- **NFR-071**: Configuration validation on startup

### Open Issues
- None identified

---

## Level 2: MCP Tools & Prompts Internal Structure (White-box)

### Refinement Motivation
The MCP Tools & Prompts layer is refined to Level-2 because it contains multiple distinct tool classes that handle different functional domains (template discovery, persona management, project management, validation, etc.) with different responsibilities and interaction patterns.

### Internal Structure Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    MCP Tools & Prompts Layer                    │
├─────────────────┬────────────────┬──────────────┬──────────────┤
│ TemplateMcpTools│ PersonaMcpTools│ProjectMcpTool│ValidationPrompt
│                 │                │   s          │   s           │
├─────────────────┼────────────────┼──────────────┼──────────────┤
│[McpServerTool]  │[McpServerTool] │[McpServerTool
  [McpServerPrompt│
│                 │                │              │Type]          │
│ 3 template tools│ 7 persona tools│ 7 project    │ 2 validation  │
│ discovery tools │ CRUD & cache   │ CRUD & cache │ prompts w/    │
│                 │ operations     │ operations   │ content inject│
└─────────────────┴────────────────┴──────────────┴──────────────┘
           │                │                │              │
           └────────────────┼────────────────┼──────────────┘
                           │
                           ↓
        ┌──────────────────────────────────────────┐
        │ Business Logic Services Layer            │
        ├──────────────────────────────────────────┤
        │ IPersonaInstructionService               │
        │ IProjectInstructionService               │
        │ ITemplateService                         │
        │ IPromptService                           │
        └──────────────────────────────────────────┘
```

### Internal Building Blocks

#### TemplateMcpTools (Black-box)
- **Purpose**: Provides MCP tools for discovering and retrieving instruction templates
- **Tools**: `template_list`, `template_get_persona`, `template_get_project`
- **Dependencies**: ITemplateService for template loading
- **Interaction**: Delegates template discovery to ITemplateService, returns JSON responses

#### PersonaMcpTools (Black-box)
- **Purpose**: Provides MCP tools for persona instruction management and creation
- **Tools**: `persona_list`, `persona_get`, `persona_exists`, `persona_get_current`, `persona_set_current`, `persona_invalidate_cache`, `persona_refresh_cache`, `persona_create_from_template`, `persona_validate`
- **Dependencies**: IPersonaInstructionService for instruction operations, ITemplateService for creation
- **Interaction**: Delegates file operations to service layer, returns JSON responses

#### ProjectMcpTools (Black-box)
- **Purpose**: Provides MCP tools for project instruction management and creation
- **Tools**: `project_list`, `project_get`, `project_exists`, `project_get_current`, `project_set_current`, `project_invalidate_cache`, `project_refresh_cache`, `project_create_from_template`, `project_validate`
- **Dependencies**: IProjectInstructionService for instruction operations, ITemplateService for creation
- **Interaction**: Mirrors PersonaMcpTools structure for consistency

#### ValidationPrompts (Black-box)
- **Purpose**: Provides MCP prompts for validation workflows
- **Prompts**: `validate_persona_prompt`, `validate_project_prompt`
- **Features**: Optional content injection via HTML comment placeholders
- **Dependencies**: IPromptService for prompt loading
- **Interaction**: Supports base prompt retrieval and content-injected variant generation

### Internal Interfaces
All tool classes are registered with [McpServerToolType] attribute for auto-discovery. All tool methods follow consistent patterns:
- Async operations with CancellationToken support
- JSON serialization for responses
- Parameter validation
- Error handling with JSON error responses
- Dependency injection for service access

Prompts are registered with [McpServerPromptType] attribute and support optional parameter injection.
