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

## Complete MCP Tools Reference

PersonaMcpServer exposes 16 MCP tools across creation, retrieval, and validation operations:

### Template Discovery Tools (Step 4)

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|-----------|---------|
| `template_list` | Lists all available instruction templates | none | `string[]` |
| `template_get_persona` | Retrieves the persona template | none | JSON template content |
| `template_get_project` | Retrieves the project template | none | JSON template content |

### Legacy Retrieval Tools (Step 1-2, Maintained for Compatibility)

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|-----------|---------|
| `persona_list` | Lists all available persona instruction files | none | `string[]` |
| `persona_get` | Retrieves a specific persona by name | `name: string` | JSON with full content |
| `persona_exists` | Checks if a persona exists | `name: string` | `boolean` |
| `persona_get_current` | Gets the currently active persona | none | JSON or null |
| `persona_set_current` | Sets the current active persona | `name: string` | Success/error JSON |
| `persona_invalidate_cache` | Invalidates cache for a persona (or all) | `name?: string` | Success JSON |
| `persona_refresh_cache` | Preloads all personas into cache | none | Success JSON |
| `project_list` | Lists all available project instruction files | none | `string[]` |
| `project_get` | Retrieves a specific project by name | `name: string` | JSON with full content |
| `project_exists` | Checks if a project exists | `name: string` | `boolean` |
| `project_get_current` | Gets the currently active project | none | JSON or null |
| `project_set_current` | Sets the current active project | `name: string` | Success/error JSON |
| `project_invalidate_cache` | Invalidates cache for a project (or all) | `name?: string` | Success JSON |
| `project_refresh_cache` | Preloads all projects into cache | none | Success JSON |

### Creation Tools (Step 5)

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|-----------|---------|
| `persona_create_from_template` | Creates new persona from template | `name: string`, `description?: string`, `customFields?: object` | JSON with success, filePath, and validation status |
| `project_create_from_template` | Creates new project from template | `name: string`, `description?: string`, `customFields?: object` | JSON with success, filePath, and validation status |

### Validation Tools (Step 8)

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|-----------|---------|
| `persona_validate` | Validates a persona instruction file | `filePath: string` | JSON with validation status, error/warning/info counts, and issue details |
| `project_validate` | Validates a project instruction file | `filePath: string` | JSON with validation status, error/warning/info counts, and issue details |

### Validation Response Format (Step 8)

```json
{
  "success": true,
  "isValid": true,
  "errorCount": 0,
  "warningCount": 1,
  "infoCount": 2,
  "issues": [
    {
      "severity": "Warning",
      "section": "metadata",
      "message": "Optional description field not found",
      "lineNumber": 3
    },
    {
      "severity": "Info",
      "section": "structure",
      "message": "File follows recommended format",
      "lineNumber": 1
    }
  ]
}
```

---

## Complete MCP Prompts Reference

PersonaMcpServer exposes 2 MCP prompts for validation workflows:

### Validation Prompts (Step 9)

| Prompt Name | Description | Parameters | Features |
|-------------|-------------|-----------|----------|
| `validate_persona_prompt` | Prompt for persona validation review | `personaContent?: string` | Optional content injection via HTML comment placeholder |
| `validate_project_prompt` | Prompt for project validation review | `projectContent?: string` | Optional content injection via HTML comment placeholder |

### Prompt Usage

**Without Content (Returns base prompt):**
```
User: Get the persona validation prompt
LLM: [calls validate_persona_prompt] → Returns validation instructions prompt
```

**With Content (Injects content into prompt):**
```
User: Validate this persona content (provides full content)
LLM: [calls validate_persona_prompt with personaContent] → Returns prompt with injected content
```

**Prompt Response Format:**
```json
{
  "success": true,
  "prompt": "## Persona Validation Guide\n\nReview the following persona for completeness and quality...\n\n<!-- INSERT_PERSONA_CONTENT -->\n\nValidation checklist:\n- [ ] Frontmatter is valid YAML\n- [ ] All required sections present\n..."
}
```

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

### With VS Code (GitHub Copilot)

**Step 1:** Enable MCP in your workspace by creating `.vscode/settings.json`:

```json
{
  "github.copilot.chat.mcp.enabled": true
}
```

**Step 2:** Configure the MCP server in `.vscode/mcp.json`:

```json
{
  "servers": {
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

**Step 3:** Reload VS Code (`Cmd+Shift+P` → `Developer: Reload Window`)

**Step 4:** Verify the server is running:
- Open Output panel (`Cmd+Shift+U`)
- Select "MCP (persona-server)" from the dropdown
- You should see the server starting

### Workflow Examples

#### Workflow 1: Template Discovery & Persona Creation (Steps 4-5)

**User Query:**
```
Create a new persona called 'tech_lead' based on the template
```

**LLM Actions:**
```
1. Call template_get_persona → Get template structure
2. Call persona_create_from_template with:
   - name: "tech_lead"
   - description: "Technical leadership focused on architecture and team mentoring"
3. Receive JSON response:
   {
     "success": true,
     "filePath": "/personas/tech_lead.instructions.md",
     "createdAt": "2025-11-24T10:30:00Z",
     "isValid": true,
     "validationStatus": {
       "errorCount": 0,
       "warningCount": 0,
       "infoCount": 2
     }
   }
4. Confirm creation with user
```

#### Workflow 2: Validation Review (Steps 8-9)

**User Query:**
```
Validate my developer persona and suggest improvements
```

**LLM Actions:**
```
1. Call persona_list → Find "developer.instructions.md"
2. Call persona_validate with filePath → Get validation results:
   {
     "success": true,
     "isValid": true,
     "errorCount": 0,
     "warningCount": 1,
     "infoCount": 3,
     "issues": [
       {
         "severity": "Warning",
         "section": "skills",
         "message": "Backend skills list is shorter than industry average",
         "lineNumber": 24
       }
     ]
   }
3. Call validate_persona_prompt with personaContent → Get review prompt
4. Use prompt to guide detailed review of persona content
5. Present findings and suggestions to user
```

#### Workflow 3: End-to-End Project Setup (Steps 1-10)

**User Query:**
```
Create a new web application project with developer and architect personas
```

**LLM Actions:**
```
1. Call template_get_project → Understand project structure
2. Call project_create_from_template with:
   - name: "webapp"
   - description: "Modern web application with React frontend and .NET backend"
   - customFields: {
       "frontend": "React 18 + TypeScript",
       "backend": ".NET 8",
       "database": "PostgreSQL"
     }
3. Receive project creation response with validation
4. Create developer persona: persona_create_from_template
5. Create architect persona: persona_create_from_template
6. Validate both personas: persona_validate (both should return 0 errors)
7. List all artifacts:
   - project_list → ["webapp"]
   - persona_list → ["developer", "architect"]
8. Set active context:
   - persona_set_current("developer")
   - project_set_current("webapp")
9. Begin coding session with full context
```

#### Workflow 4: Batch Validation (Step 8-9)

**User Query:**
```
Audit all instruction files in my project for quality and completeness
```

**LLM Actions:**
```
1. Call persona_list → Get all personas
2. Call project_list → Get all projects
3. For each persona: Call persona_validate → Collect results
4. For each project: Call project_validate → Collect results
5. Summarize findings:
   - Total files: 5
   - Valid: 5
   - Errors: 0
   - Warnings: 2
   - Info messages: 8
6. Call validate_persona_prompt + validate_project_prompt for each file
7. Generate comprehensive audit report with improvement suggestions
```

### Usage in Claude Desktop

Add to your Claude Desktop MCP configuration:

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

**Then in Claude:**
```
User: List available personas
Claude: [calls persona_list tool] The available personas are: developer, architect, reviewer.

User: Create a new senior developer persona
Claude: [calls template_get_persona, then persona_create_from_template] 
Created new persona "senior_developer" with validation successful.

User: Validate my developer persona
Claude: [calls persona_validate] 
Your developer persona is valid with 0 errors and 2 info messages.

User: Show validation suggestions for the developer persona
Claude: [calls validate_persona_prompt with content]
[Uses prompt to analyze and provide suggestions]
```

### Usage in VS Code (GitHub Copilot Chat)

Enable MCP in `.vscode/settings.json` and configure in `.vscode/mcp.json` (see Configuration section).

**Then in Copilot Chat:**
```
@<#> What personas do I have?
Copilot: [Uses persona_list tool] Your available personas are...

@<#> Create a project called 'backend-api'
Copilot: [Uses template_get_project, then project_create_from_template]
Successfully created the backend-api project.

@<#> Validate all my instruction files
Copilot: [Uses persona_list, project_list, then validate tools]
Here's your validation report...

@<#> Set developer as my current persona
Copilot: [Uses persona_set_current]
Your current persona is now set to developer.
```

---

## Testing & Quality Assurance

PersonaMcpServer includes **196 comprehensive tests** (100% passing) organized by component:

- **Configuration Tests:** 9 tests validating configuration options and environment variable overrides
- **Service Tests:** 70 tests for PersonaInstructionService and ProjectInstructionService
- **Tool Tests:** 56 tests for MCP tool handlers (PersonaMcpTools, ProjectMcpTools)
- **Validation Tests:** 43 tests for validation models, logic, and tools
- **Prompt Tests:** 8 tests for validation prompts
- **Integration Tests:** 11 end-to-end tests covering complete workflows

Run tests:

```bash
cd mcp-server
dotnet test
```

Run tests with coverage:

```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

Run tests with detailed output:

```bash
dotnet test --verbosity detailed
```

**Quality Metrics:**
- **Pass Rate:** 196/196 (100%)
- **Build Warnings:** 0
- **Performance:** Cache hit < 1ms, cold load ~5ms
- **Thread Safety:** SemaphoreSlim for concurrent access

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
