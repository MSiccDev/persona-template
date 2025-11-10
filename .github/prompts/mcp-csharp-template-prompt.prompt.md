# GitHub Copilot Prompt: Reusable C# MCP Server Template for Persona Instructions

## Goal
Create a minimal, reusable C# MCP server template that exposes persona and project-specific instructions from a persona-template repository as MCP resources to any LLM provider. This server acts as the universal bridge between your instruction repository and AI assistants (Claude, OpenAI, GitHub Copilot, VS Code Copilot, Semantic Kernel, etc.), allowing them all to automatically load and reference your professional context and project guidelines through a single standardized interface.

**Purpose**: Enable any LLM or AI platform to access your persona instructions (who you are, your skills, preferences) and project instructions (scope, tech stack, roles, phases) automatically via MCP, eliminating platform-specific configuration and the need to paste instructions into every session or for every provider.

## Provider-Agnostic Architecture

This MCP server is **provider-agnostic** and follows the philosophy of the persona-template repository: **one instruction source, many consumption methods**.

**How it works:**
1. Users create a new repository from the persona-template GitHub template and customize their persona and project instructions (single source of truth)
2. They deploy this MCP server locally or remotely
3. They configure it to point to their persona-template instance
4. Any LLM provider/platform can connect to this server via MCP (HTTP/SSE transport)
5. The provider accesses persona and project instructions via MCP resources
6. No more copy-pasting instructions—any provider loads them automatically

**Supported providers (examples):**
- Claude Desktop (local STDIO or remote HTTP)
- OpenAI API (via OpenAI Agents SDK with MCP support)
- GitHub Copilot (local or remote)
- VS Code Copilot (local or remote)
- Microsoft Copilot Studio
- Semantic Kernel (local or remote)
- Custom LLM applications using MCP clients
- Any tool implementing MCP protocol

**File structure expected from persona-template repo:**
```
persona-template/
├── yourname_persona.instructions.md     # Persona instructions (loaded by default)
├── projects/
│   ├── project1_project.instructions.md  # Project-specific instructions
│   └── project2_project.instructions.md
├── specs/
│   └── context_aware_ai_session_spec.md
```

This server exposes the markdown files from that repo as MCP resources that any provider can reference.

## Project Structure

This MCP server lives as a `/mcp-server` subdirectory within the persona-template repository template. When users create a new repository from the persona-template GitHub template, they get both the instructions and the MCP server together in their own instance.

```
persona-template/                     # Single template repo (users create instances)
├── README.md                          # Main repo docs
├── yourname_persona.instructions.md   # Persona instructions
├── projects/                          # Project instructions
│   ├── project1_project.instructions.md
│   └── project2_project.instructions.md
├── specs/
│   └── context_aware_ai_session_spec.md
# (arc42 docs handled via separate instructions/prompts)
└── mcp-server/                        # C# MCP server (subdirectory)
    ├── src/
    │   ├── Program.cs
    │   ├── MCP.PersonaServer.csproj
    │   ├── Config/
    │   │   ├── PersonaServerConfig.cs
    │   │   └── TransportOptions.cs
    │   ├── Handlers/
    │   │   ├── PersonaResourceHandler.cs
    │   │   ├── ProjectResourceHandler.cs
    │   │   └── InstructionToolHandler.cs
    │   └── Services/
    │       ├── PersonaInstructionService.cs
    │       └── ServiceRegistration.cs
    ├── tests/
    │   ├── MCP.PersonaServer.Tests.csproj
    │   ├── Handlers/
    │   │   ├── PersonaResourceHandlerTests.cs
    │   │   └── InstructionToolHandlerTests.cs
    │   └── Services/
    │       └── PersonaInstructionServiceTests.cs
    ├── appsettings.json               # Configuration (relative paths to parent repo)
    ├── Dockerfile
    ├── README.md                      # Quick start guide
    └── .gitignore
```

**Usage workflow**:
1. User creates a new repository from the persona-template GitHub template
2. User customizes persona and project instruction files in their instance
3. User runs the MCP server (from `/mcp-server` subdirectory), which automatically finds instructions using relative paths (`../`)

## Core Requirements

### 1. Program.cs Entry Point
- Use Microsoft.Extensions.Hosting for DI container
- Support two transport mechanisms for flexibility:
  - **HTTP/SSE with ASP.NET Core** (RECOMMENDED, default): Remote HTTP server accessible by any provider
  - **STDIO** (optional): Local console app for single-client scenarios (e.g., Claude Desktop local)
- Transport type configurable via `appsettings.json` (default: "SSE")
- Load MCP C# SDK packages: `ModelContextProtocol.AspNetCore` for SSE (primary), `ModelContextProtocol` for STDIO (optional)
- Register handlers using `ServiceRegistration.cs` helper
- Graceful startup/shutdown
- Log server URL/port on startup for easy client configuration

### 2. PersonaServerConfig.cs
- Model binding from `appsettings.json`
- Properties:
  - `string PersonaRepoPath` (path to persona instructions, defaults to `../` relative to mcp-server directory)
  - `string CurrentPersona` (which persona file to use, e.g., "marco_persona.instructions.md")
  - `string CurrentProject` (optional, which project file to expose, e.g., "projects/project1_project.instructions.md")
  - `string Transport` ("SSE" or "STDIO", default: "SSE")
  - `int Port` (for HTTP server, default: 3000)
  - `string Host` (for HTTP server, default: "localhost" for local, "0.0.0.0" for docker/remote)
  - `int CacheTtlSeconds` (how long to cache file contents, default: 300)

### 3. PersonaInstructionService.cs
- Service that reads from persona-template repo structure
- Methods:
  - `GetPersonaContent(personaName)`: Returns content of persona instructions file
  - `GetProjectContent(projectName)`: Returns content of project instructions file
  - `ListAvailablePersonas()`: Lists all `*_persona.instructions.md` files
  - `ListAvailableProjects()`: Lists all `*_project.instructions.md` files
  - Error handling for missing files, permission issues
- Caches file contents with TTL (refresh every N seconds)

### 4. PersonaResourceHandler.cs
- Exposes persona instructions as MCP resource with URI pattern: `persona://current`
- Resource returns the current persona instructions markdown as read-only text
- Includes resource description and MIME type
- Decorated with `[McpServerResourceType]` attribute

### 5. ProjectResourceHandler.cs
- Exposes project instructions as MCP resource with URI pattern: `project://current`
- Resource returns current project instructions markdown as read-only text
- Only available if CurrentProject is configured
- Includes resource description and MIME type
- Decorated with `[McpServerResourceType]` attribute

### 6. InstructionToolHandler.cs
- Single tool: `list_available_instructions`
- Parameters: `type` (persona or project)
- Returns: JSON list of available instruction files with descriptions
- Allows Claude to discover what personas/projects are available in the repo
- Decorated with `[McpServerToolType]` and `[McpServerTool]` attributes

### 7. ServiceRegistration.cs
- Static method: `public static IHostBuilder RegisterMcpServices(this IHostBuilder builder)`
- Auto-discovers and registers all classes with `[McpServerResourceType]` and `[McpServerToolType]` attributes
- Registers `PersonaInstructionService` as singleton
- Configures MCP server with resources and tools
- Both STDIO and SSE transport setup
- Logging configuration

### 8. Unit Tests
- **PersonaInstructionServiceTests**: 
  - File reading (existing file, missing file)
  - Caching behavior
  - List operations
- **PersonaResourceHandlerTests**: 
  - Resource URI resolution
  - Content returned correctly
- **InstructionToolHandlerTests**:
  - Tool lists available personas
  - Tool lists available projects
  - Invalid type parameter handled

### 9. appsettings.json
```json
{
  "Mcp": {
    "PersonaRepoPath": "../",
    "CurrentPersona": "yourname_persona.instructions.md",
    "CurrentProject": "projects/project1_project.instructions.md",
    "Transport": "SSE",
    "Port": 3000,
    "Host": "localhost",
    "CacheTtlSeconds": 300
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

**Configuration notes:**
- `PersonaRepoPath: "../"` - Points to repo root (one level up from mcp-server directory)
- `CurrentPersona` and `CurrentProject` - Relative to PersonaRepoPath
- For local development: `"Host": "localhost"` (only accessible from your machine)
- For Docker/remote deployment: `"Host": "0.0.0.0"` (accessible from other machines)
- Change `"Transport": "STDIO"` only if running single local client

### 10. README.md (in mcp-server/ subdirectory)
Should include:
- **Overview**: This server exposes your persona-template instructions to any LLM provider via MCP
- **Quick Start**: 
  - Prerequisites (dotnet SDK version)
  - Build and run the server locally: `dotnet run`
  - Server starts on `http://localhost:3000` by default
- **Configuration**: How to customize `appsettings.json` to point to different persona/project files
- **Deployment Options**:
  - Local HTTP: Run on localhost:3000 for single-machine access
  - Docker: Include Dockerfile for containerization
  - Cloud deployment: AWS, Azure, or other platforms
- **Integration with Providers** (step-by-step for each):
  - **Claude Desktop**: Add to claude_desktop_config.json with HTTP endpoint
  - **OpenAI API**: Use with OpenAI Agents SDK MCP support
  - **GitHub Copilot**: Configure in workspace or user settings
  - **VS Code Copilot**: Configure in VS Code settings
  - **Semantic Kernel**: Example C# code to connect as MCP client
  - **Custom applications**: Document HTTP/SSE API endpoints
- **API Documentation**: What resources are exposed (`/resources`) and what tools are available (`/tools`)
- **Testing**: How to verify server is working (health check endpoint, test with curl/Postman)
- **Running Unit Tests**: How to run test suite (`dotnet test`)
- **Troubleshooting**: Common issues (file not found, permission errors, connection issues)

### 11. Architecture Documentation (External Creation)
**Note:** Arc42 documentation will be created via dedicated instructions and prompts, not as part of this implementation.

The architecture documentation should include:
- System overview and quality goals
- Architecture constraints and design decisions  
- Building blocks and runtime behavior
- Deployment models and cross-cutting concerns
- Quality requirements and risk assessment

**Implementation scope:** This prompt focuses on the MCP server implementation only. Architecture documentation is handled separately.

## Technical Specifications

### NuGet Dependencies
- `Microsoft.Extensions.Hosting` (for DI/hosting)
- `Microsoft.Extensions.Configuration` (for config)
- `ModelContextProtocol` (main SDK for STDIO)
- `ModelContextProtocol.AspNetCore` (for SSE/HTTP)
- `Microsoft.Extensions.Logging` (for logging)
- `xUnit` or `NUnit` (for tests)
- `Moq` (for mocking in tests)

### .NET Target
- Target Framework: `net8.0` or `net9.0` minimum

### Transport Implementation
- **STDIO**: Use `StdioServerTransport` from SDK
- **SSE**: Use ASP.NET Core minimal APIs with `SseServerTransport`
- Configuration selects transport at startup based on `appsettings.json`

### Tool Naming Convention
- Use snake_case for tool names (e.g., `echo_message`, not `EchoMessage`)
- Provide clear descriptions and parameter names

### Error Handling
- All tools should handle errors gracefully
- Log errors with context (what went wrong, relevant parameters)
- Return meaningful error messages to clients
- Never expose stack traces to clients

## Constraints & Guidelines

1. **Provider-agnostic**: No code specific to any one LLM provider. The server should work identically whether accessed from Claude, OpenAI, GitHub Copilot, or any other MCP client.
2. **HTTP/SSE primary transport**: Default configuration uses HTTP/SSE so any provider (local or remote) can connect.
3. **Read-only resources**: Persona and project instructions are exposed as read-only resources (clients can read but not modify through MCP).
4. **File-based**: Read directly from persona-template repo file structure, no database or external service dependency.
5. **Configuration over code**: Repo path, persona name, project name all configurable in `appsettings.json` and environment variables.
6. **Caching**: Cache file contents to avoid constant disk reads (with TTL).
7. **Testing from day one**: Include unit tests for file reading, resource exposure, tool listing.
8. **No hardcoding**: All paths, names, and deployment config from configuration.
9. **Deployable**: Works locally, in Docker, or on cloud platforms without code changes.
10. **Single responsibility**: This server only exposes persona instructions. Don't add LLM-specific logic, provider-specific features, or application logic.

## Success Criteria

When complete, the server should:
- ✅ Read from a persona-template repo directory
- ✅ Expose current persona instructions as MCP resource (`persona://current`)
- ✅ Expose current project instructions as MCP resource (`project://current`)
- ✅ Provide tool to list available personas and projects
- ✅ Run as HTTP/SSE server accessible by any MCP client (STDIO as optional fallback)
- ✅ Work identically with Claude Desktop, OpenAI, GitHub Copilot, VS Code Copilot, Semantic Kernel, or any MCP-compliant provider
- ✅ Be deployable locally, in Docker, or on cloud platforms
- ✅ Have unit tests that pass for all handlers and services
- ✅ Cache file contents for performance
- ✅ Handle missing files and configuration errors gracefully
- ✅ Have provider-agnostic documentation with integration examples for major platforms
- ✅ Include no provider-specific code or dependencies

## Deployment Models & Provider Integration

The server should work identically in these scenarios without code changes:

### Local Development
- Run on localhost:3000 with HTTP/SSE transport
- Accessible only from your machine
- Configured with `"Host": "localhost"` in appsettings.json

### Docker/Container Deployment
- Include `Dockerfile` that packages server for cloud platforms
- Configured with `"Host": "0.0.0.0"` in appsettings.json
- Accessible from other machines within network

### Provider Integration (examples, not implementation-specific):
1. **Claude Desktop**: 
   - Add HTTP endpoint to `claude_desktop_config.json`
   - Server at `http://localhost:3000`

2. **OpenAI API (with MCP support)**:
   - Client connects to server URL
   - Calls MCP resources via OpenAI Agents SDK

3. **GitHub Copilot/VS Code**:
   - Configure in workspace settings
   - Points to local or remote server URL

4. **Semantic Kernel (C# app)**:
   - Create MCP client that connects to server URL
   - Retrieves resources programmatically

5. **Custom applications**:
   - Any application with MCP client support can connect via HTTP/SSE

**Key principle**: All integration examples should show the server URL/endpoint configuration, not server-side code changes.

## Output Deliverables

1. Complete project file structure with all files
2. Working code for all components (src/ with all handlers and services)
3. Unit tests that pass (tests/ with comprehensive coverage)
4. README.md (quick start guide for the MCP server)
5. Example `appsettings.json` (with both local and remote configurations)
6. `Dockerfile` for containerization and cloud deployment
7. `.gitignore` appropriate for C# projects

**Note:** Architecture documentation (arc42) will be created separately via dedicated instructions/prompts.

## Implementation Plan

Before writing any code, I need you to create a detailed **implementation plan** that breaks this project into small, easily committable steps.

**For each step, provide:**
1. **Step name**: Clear, concise title
2. **Description**: What will be created/modified
3. **Files affected**: Which files will be created, modified, or deleted
4. **Dependencies**: Any prior steps that must complete first
5. **Validation**: How to verify this step is complete (tests, file structure checks, etc.)
6. **Notes**: Any gotchas or important context

**Requirements for the plan:**
- Steps should be small enough to complete and commit in 15-30 minutes
- Each step should be independently reviewable
- Group related items logically (e.g., all config setup together, all handlers together)
- Order steps so that dependencies are respected (e.g., config types before handlers that use them)
- Include setup steps (project structure, .csproj files) before implementation steps
- Include test setup and test implementations alongside feature implementations
- Include documentation steps (Arc42 docs) as separate, reviewable chunks

**Output format:**
Present the plan as a numbered list with clear separations between steps. Include a summary at the top showing total step count and estimated time. After each step, include a brief checklist of what should be validated before committing.

**Do not write any code or create any files yet.** Just the plan. I will review it, ask questions, request changes if needed, and give approval before you proceed with implementation.

Ask clarifying questions if any requirements are unclear.

## Issue Creation & Agentic Implementation

Once the implementation plan is approved, I will ask you to create a GitHub issue for each step. Use this process:

**For each issue, create:**
1. **Title**: Step name from the plan (e.g., "Step 1: Initialize project structure and .csproj files")
2. **Description**: Include:
   - Brief description of what this step accomplishes
   - **Files affected** list (from the plan)
   - **Dependencies**: Which previous issues must be closed first (if any)
   - **Acceptance Criteria**: The validation checklist from the plan
   - **Notes**: Any gotchas or important implementation details
3. **Labels**: `setup`, `feature`, `test`, or `docs` (and optionally `mcp-server` to scope it)
4. **Milestone**: Optional, but groups related issues

**Important guidelines for implementation:**

- **One coherent commit per issue**: Commit once when the issue is complete, with message: `Fixes #[issue-number]: [brief description]`
- **Pull Requests recommended**: Open a PR for each issue (rather than committing directly) so I can review before merging
- **Test before closing**: Run any relevant tests within this issue before marking it done. Include test results in the PR description
- **State communication**: After each issue is merged, briefly report what was completed and what's ready for the next step
- **Validation checklist**: Run through the acceptance criteria before closing. If any fail, report and ask for guidance before proceeding
- **Rollback awareness**: If an issue introduces breaking changes, be explicit about it and ask before merging. Don't silently work around previous issues
- **Dependency blocking**: If an issue is blocked by a previous step not being complete, pause and report. Don't work around missing dependencies
- **No silent failures**: If tests fail or files don't compile, report immediately rather than continuing

**Expect the following from the user during implementation:**
- PRs will be reviewed and either approved or request changes
- If changes are requested, clear feedback will be provided about what needs adjustment
- Questions will be answered promptly to keep work moving forward
- The user may ask for local validation before merging (compile checks, test runs, file structure verification)
- If an issue needs mid-stream adjustment, the user will communicate this early and clearly
- After each PR is merged, you should be ready to proceed to the next issue

**When blocked or uncertain:**
- If a PR is rejected, read the feedback carefully and adjust accordingly
- If you need clarification before proceeding, ask clearly what needs to be done
- If a decision requires user input (e.g., architecture choice, rollback decision), ask rather than guessing
- Report immediately if tests fail, code won't compile, or acceptance criteria aren't met

This workflow keeps the user in control while you handle the detailed implementation work autonomously.
