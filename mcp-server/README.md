# PersonaMcpServer

A Model Context Protocol (MCP) server that provides instruction file management for context-aware AI sessions.

## Overview

PersonaMcpServer implements **Layer 1 (Content Provider)** of the [Context-Aware AI Session Flow Specification](../specs/context_aware_ai_session_spec.md). It serves as the foundation for managing and distributing persona and project instruction files to AI clients, enabling consistent, context-aware collaboration across different LLM providers.

### What This Server Does

- **Loads and caches** persona and project instruction files (`.instructions.md`)
- **Parses YAML frontmatter** for metadata (`applyTo`, `description`)
- **Exposes MCP tools** for listing, retrieving, and managing instructions
- **Manages current context** (active persona and project)
- **Provides high-performance caching** with TTL and LRU eviction

### What This Server Does NOT Do (Yet)

This is v1.0 focused on content delivery. Session state management (Role, Phase, Output Style) is planned for v2.0. See [Future Enhancements](#future-enhancements) below.

---

## Architecture

```
PersonaMcpServer/
├── src/
│   ├── Models/               # Data models (PersonaInstruction, ProjectInstruction)
│   ├── Services/             # Business logic (file I/O, caching)
│   ├── Server/               # MCP tool handlers (PersonaMcpTools, ProjectMcpTools)
│   ├── PersonaServerConfig.cs # Configuration with validation
│   ├── Program.cs            # Host application entry point
│   └── appsettings.json      # Configuration files
└── tests/
    ├── Models/               # Model tests
    ├── Services/             # Service unit tests
    ├── Server/               # Tool handler tests
    └── Integration/          # End-to-end integration tests
```

**Key Technologies:**
- .NET 8 with Microsoft.Extensions.Hosting
- ModelContextProtocol SDK (v0.4.0-preview.3)
- ConcurrentDictionary-based caching with SemaphoreSlim
- xUnit + NSubstitute + AwesomeAssertions for testing

---

## Installation

### Prerequisites

- .NET 8 SDK or later
- An MCP-compatible client (Claude Desktop, VS Code with MCP extension, etc.)

### Building from Source

```bash
cd mcp-server
dotnet restore
dotnet build
dotnet test
```

### Running the Server

```bash
cd mcp-server/src
dotnet run
```

The server runs as a stdio-based MCP server, communicating via stdin/stdout according to the MCP protocol.

---

## Configuration

Configuration is loaded from `appsettings.json` and can be overridden via environment variables with the `PERSONA_` prefix.

### appsettings.json

```json
{
  "PersonaServer": {
    "PersonaRepoPath": "./personas",
    "CacheTtlSeconds": 300,
    "MaxCacheSizeBytes": 10485760,
    "CurrentPersona": null,
    "CurrentProject": null
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ModelContextProtocol": "Information"
    }
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `PersonaRepoPath` | string | `"./personas"` | Path to the directory containing instruction files |
| `CacheTtlSeconds` | int | `300` | Time-to-live for cached instruction files (seconds) |
| `MaxCacheSizeBytes` | long | `10485760` | Maximum total size of cached files (bytes) |
| `CurrentPersona` | string? | `null` | Name of the currently active persona |
| `CurrentProject` | string? | `null` | Name of the currently active project |

### Environment Variable Overrides

Prefix configuration keys with `PERSONA_`:

```bash
export PERSONA_PersonaServer__PersonaRepoPath="/path/to/personas"
export PERSONA_PersonaServer__CacheTtlSeconds=600
dotnet run
```

---

## MCP Tools

PersonaMcpServer exposes 14 MCP tools (7 for personas + 7 for projects):

### Persona Tools

| Tool Name | Description | Returns |
|-----------|-------------|---------|
| `persona_list` | Lists all available persona instruction files | `string[]` |
| `persona_get` | Retrieves a specific persona by name | JSON with full content |
| `persona_exists` | Checks if a persona exists | `boolean` |
| `persona_get_current` | Gets the currently active persona | JSON or null |
| `persona_set_current` | Sets the current active persona | Success/error JSON |
| `persona_invalidate_cache` | Invalidates cache for a persona (or all) | Success JSON |
| `persona_refresh_cache` | Preloads all personas into cache | Success JSON |

### Project Tools

| Tool Name | Description | Returns |
|-----------|-------------|---------|
| `project_list` | Lists all available project instruction files | `string[]` |
| `project_get` | Retrieves a specific project by name | JSON with full content |
| `project_exists` | Checks if a project exists | `boolean` |
| `project_get_current` | Gets the currently active project | JSON or null |
| `project_set_current` | Sets the current active project | Success/error JSON |
| `project_invalidate_cache` | Invalidates cache for a project (or all) | Success JSON |
| `project_refresh_cache` | Preloads all projects into cache | Success JSON |

---

## File Format

Instruction files use Markdown with optional YAML frontmatter:

### Persona File Example (`developer_persona.instructions.md`)

```markdown
---
applyTo: '**/*'
description: 'Senior developer persona with technical expertise'
---

# Developer Persona

## About
Experienced software engineer with expertise in .NET, C#, and cloud architecture.

## Working Style
- Prefers structured, test-driven development
- Values code quality and maintainability
- Uses clear commit messages and documentation

## Technical Skills
- Backend: .NET 8, ASP.NET Core, Entity Framework
- Frontend: React, TypeScript
- DevOps: Docker, Kubernetes, GitHub Actions
```

### Project File Example (`webapp_project.instructions.md`)

```markdown
---
applyTo: 'src/**'
description: 'Web application project using React and .NET'
---

# Web Application Project

## Tech Stack
- Frontend: React 18 + TypeScript + Vite
- Backend: ASP.NET Core 8 Web API
- Database: PostgreSQL 15

## Architecture
- Clean Architecture with CQRS
- Repository pattern for data access
- JWT authentication with refresh tokens

## Development Guidelines
- Follow TDD practices
- Maintain 80%+ code coverage
- Use conventional commits
```

---

## Usage Example

### With Claude Desktop

Add to your Claude Desktop MCP configuration (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

```json
{
  "mcpServers": {
    "persona-server": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/persona-template/mcp-server/src"],
      "env": {
        "PERSONA_PersonaServer__PersonaRepoPath": "/path/to/your/personas"
      }
    }
  }
}
```

### Querying Tools

Once connected, you can use the tools:

```
User: List available personas
AI: [calls persona_list tool] → ["developer", "architect", "reviewer"]

User: Load the developer persona
AI: [calls persona_get with name="developer"] → Returns full persona content

User: Set developer as current persona
AI: [calls persona_set_current with name="developer"] → Success
```

---

## Testing

The project includes 101 comprehensive tests:

- **9 tests** - Configuration validation
- **11 tests** - Persona service interface contracts
- **17 tests** - Persona service implementation
- **11 tests** - Project service interface contracts
- **17 tests** - Project service implementation
- **12 tests** - Persona MCP tool handlers
- **12 tests** - Project MCP tool handlers
- **12 tests** - Integration tests (end-to-end)

Run tests:

```bash
cd mcp-server
dotnet test
```

Run tests with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Performance

- **Caching:** ConcurrentDictionary with thread-safe operations
- **TTL-based expiration:** Configurable per-file cache lifetime
- **LRU eviction:** Automatic cleanup when cache size limits are reached
- **Async I/O:** Non-blocking file operations throughout
- **Benchmarks:**
  - Cache hit: < 1ms
  - Cold load (1KB file): ~5ms
  - Refresh cache (10 files): ~50ms

---

## Future Enhancements

PersonaMcpServer v1.0 implements **Layer 1: Content Provider** of the Context-Aware AI Session specification. Future versions may explore:

### v2.0 - Session State Manager (Planned)
- **Role/Phase/Output Style tracking** - Manage active session state beyond just current persona/project
- **State transition logic** - Handle role switches (Architect → Developer) with validation
- **Context initialization flows** - Automated session setup with confirmation summaries
- **Deterministic behavior control** - Role-dependent reasoning and response formatting

### Additional Ideas
- **JSON metadata extraction** - Parse structured data from persona files (skills, projects, goals)
- **Role-based tool filtering** - Expose different tools based on active role
- **Multi-provider sync** - Synchronize instruction files across different LLM platforms
- **Version control integration** - Track instruction file changes with git

See [`specs/context_aware_ai_session_spec.md`](../specs/context_aware_ai_session_spec.md) for the complete vision of context-aware AI sessions.

**Contributions welcome!** If you're interested in implementing any of these features, please open an issue to discuss.

---

## Project Structure Reference

```
persona-template/
├── specs/                           # Specification documents
│   └── context_aware_ai_session_spec.md
├── templates/                       # Instruction file templates
│   ├── persona_template.instructions.md
│   └── project_template.instructions.md
└── mcp-server/                      # This MCP server implementation
    ├── src/                         # Source code
    ├── tests/                       # Test suites
    └── README.md                    # This file
```

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'feat: add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

---

## License

This project is part of the [persona-template](https://github.com/MSiccDev/persona-template) repository.

---

## Acknowledgments

- [Model Context Protocol](https://modelcontextprotocol.io/) - Protocol specification
- [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) - Official C# implementation
- [Context-Aware AI Session Flow Specification](../specs/context_aware_ai_session_spec.md) - Design foundation

---

**Version:** 1.0.0  
**Last Updated:** November 2025  
**Maintainer:** [MSiccDev](https://github.com/MSiccDev)
