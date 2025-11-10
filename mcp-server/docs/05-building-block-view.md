# 5. Building Block View

## Overview

The C# MCP Server Template follows a layered architecture with clear separation of concerns. The decomposition uses four main abstraction levels: the entry point and hosting layer, MCP protocol handlers, business logic services, and configuration management. This hierarchical approach enables maintainability, testability, and extensibility while supporting the provider-agnostic requirement.

## Level 1: Overall System (White-box) **MANDATORY**

### Overview Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    C# MCP Server Template                        │
├─────────────────┬──────────────────┬─────────────────┬─────────────┤
│   Application   │   MCP Handlers   │  Business Logic │ Configuration│
│    Host         │                  │   Services      │             │
├─────────────────┼──────────────────┼─────────────────┼─────────────┤
│  Program.cs     │ PersonaResource  │ PersonaInstruct │ PersonaServer│
│  Startup        │ ProjectResource  │ FileCache       │ Config       │
│  DI Container   │ InstructionTool  │ PathValidator   │ Environment  │
└─────────────────┴──────────────────┴─────────────────┴─────────────┘

External Interfaces:
│
├─ MCP Clients (Claude, Copilot, OpenAI, Custom)
│  └─ HTTP/SSE (port 3000) or STDIO transport
│
└─ File System
   └─ Persona-template repository (relative path ../)
```

**Legend:**
- [Component] = Building block (layer)
- ↔ = Bidirectional dependency/communication
- → = Unidirectional dependency

### Contained Building Blocks

| Name | Responsibility | Key Interfaces |
|------|---------------|----------------|
| **Application Host** | Entry point, DI container setup, application lifecycle management | IF-01 (DI Registration), IF-02 (Hosting) |
| **MCP Handlers** | MCP protocol implementation, resource and tool endpoints | IF-03 (MCP Resources), IF-04 (MCP Tools) |
| **Business Logic Services** | File operations, caching, validation, core business logic | IF-05 (File Access), IF-06 (Caching) |
| **Configuration** | Settings management, environment variable handling | IF-07 (Config Access) |

### Decomposition Rationale

This layered decomposition was chosen to:

1. **Support Quality Goals**: Clear layers enable testing (#reliable), maintainability (#flexible), and performance optimization (#efficient)
2. **Align with .NET Conventions**: Uses Microsoft.Extensions.Hosting patterns familiar to .NET developers
3. **Enable Provider Agnosticism**: MCP protocol layer isolates transport concerns from business logic
4. **Facilitate Extensibility**: New MCP resources/tools can be added without modifying existing layers
5. **Separate Concerns**: Configuration, business logic, protocol handling, and hosting have distinct responsibilities

Links to Section 4 (Solution Strategy): Implements the layered architecture strategy with dependency injection pattern.

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

## Building Block: MCP Handlers (Black-box)

### Purpose/Responsibility
Implements MCP protocol resources and tools. Handles client requests, validates inputs, and coordinates with business logic services. Provides the MCP interface layer between clients and internal services.

### Interfaces

| Interface | Description | Type | Technology |
|-----------|-------------|------|-----------|
| IF-03 | MCP Resources | Provided | ModelContextProtocol (persona://, project://) |
| IF-04 | MCP Tools | Provided | ModelContextProtocol (list_available_instructions) |
| IF-05 | Business Logic Access | Required | Internal service calls |

### Quality Attributes
- **Performance**: Fast resource/tool responses, async operations
- **Security**: Input validation, parameter sanitization
- **Flexibility**: Provider-agnostic MCP protocol compliance

### Directory/File Location
- Source: `/Handlers/PersonaResourceHandler.cs`, `/Handlers/ProjectResourceHandler.cs`, `/Handlers/InstructionToolHandler.cs`
- Tests: `/Tests/Handlers/`

### Fulfilled Requirements
- **FR-011**: Expose persona://current resource
- **FR-021**: Expose project://current resource  
- **FR-030-032**: Implement list_available_instructions tool
- **NFR-013**: Auto-discovery via attributes
- **NFR-010**: Provider-agnostic implementation

### Open Issues
- None identified

---

## Building Block: Business Logic Services (Black-box)

### Purpose/Responsibility
Core business logic including file operations, caching, path validation, and content processing. Provides reusable services for file access with performance and security considerations.

### Interfaces

| Interface | Description | Type | Technology |
|-----------|-------------|------|-----------|
| IF-05 | File Access Service | Provided | Internal API (async methods) |
| IF-06 | Caching Service | Provided | Internal API (TTL-based cache) |
| IF-09 | Path Validation | Provided | Internal API (security validation) |
| IF-10 | File System Access | Required | System.IO (async) |

### Quality Attributes
- **Performance**: Sub-50ms cached responses, sub-200ms fresh reads
- **Security**: Path validation, directory traversal prevention
- **Reliability**: Error handling, graceful degradation

### Directory/File Location
- Source: `/Services/PersonaInstructionService.cs`, `/Services/FileCache.cs`, `/Services/PathValidator.cs`
- Tests: `/Tests/Services/`

### Fulfilled Requirements
- **NFR-020-023**: File caching with TTL, asynchronous operations, memory efficiency
- **NFR-030**: Path validation and security
- **FR-040-043**: Cache management and TTL handling
- **FR-050-054**: Error handling and recovery

### Open Issues
- Cache memory usage monitoring needed for large deployments

---

## Building Block: Configuration (Black-box)

### Purpose/Responsibility
Manages application configuration from appsettings.json and environment variables. Provides strongly-typed configuration access and supports environment-specific overrides.

### Interfaces

| Interface | Description | Type | Technology |
|-----------|-------------|------|-----------|
| IF-07 | Configuration Access | Provided | Microsoft.Extensions.Configuration |
| IF-11 | Environment Variables | Required | System.Environment |

### Quality Attributes
- **Usability**: Clear configuration options, sensible defaults
- **Flexibility**: Environment-specific configuration, Docker/cloud compatibility
- **Security**: Environment variable support for sensitive settings

### Directory/File Location
- Source: `/Config/PersonaServerConfig.cs`
- Configuration: `/appsettings.json`, environment variables
- Tests: `/Tests/Config/`

### Fulfilled Requirements
- **FR-002**: Configuration loading from appsettings.json and environment
- **NFR-032**: Environment-based configuration management
- **DR-020-021**: Configuration file and environment variable support

### Open Issues
- None identified

---

## Level 2: MCP Handlers Internal Structure (White-box)

### Refinement Motivation
The MCP Handlers layer is refined to Level-2 because it contains the critical MCP protocol implementation with three distinct handler types that have different responsibilities and interaction patterns.

### Internal Structure Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         MCP Handlers                            │
├─────────────────┬─────────────────┬─────────────────────────────┤
│ PersonaResource │ ProjectResource │    InstructionTool         │
│    Handler      │    Handler      │       Handler              │
├─────────────────┼─────────────────┼─────────────────────────────┤
│[McpServerResource│[McpServerResource│   [McpServerToolType]      │
│Type("persona")] │Type("project")] │                            │
│                 │                 │                            │
│GetPersona()     │GetProject()     │ListAvailableInstructions() │
│                 │                 │                            │
└─────────────────┴─────────────────┴─────────────────────────────┘
           │                │                        │
           └────────────────┼────────────────────────┘
                           │
                           ↓
            ┌─────────────────────────────┐
            │   PersonaInstructionService │
            └─────────────────────────────┘
```

### Internal Building Blocks

#### PersonaResourceHandler (Black-box)
- **Purpose**: Handles `persona://current` resource requests
- **Interface**: MCP resource endpoint with persona URI scheme  
- **Implementation**: Single GetPersona() method with attribute-based registration

#### ProjectResourceHandler (Black-box)  
- **Purpose**: Handles `project://current` resource requests (when configured)
- **Interface**: MCP resource endpoint with project URI scheme
- **Implementation**: Single GetProject() method, conditional registration based on config

#### InstructionToolHandler (Black-box)
- **Purpose**: Implements `list_available_instructions` MCP tool
- **Interface**: MCP tool endpoint with parameter validation
- **Implementation**: ListAvailableInstructions() method with type parameter ("persona" | "project")

### Internal Interfaces
All handlers depend on PersonaInstructionService for file operations and caching. Communication is synchronous method calls within the same process, using dependency injection for service resolution.