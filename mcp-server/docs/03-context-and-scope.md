# 3. Context and Scope

## 3.1 Business Context

**Overview:**
The C# MCP Server Template serves as a bridge between AI-powered development tools and project-specific development guidelines. It exposes persona and project instruction files from the persona-template repository to MCP-compatible clients, enabling consistent application of coding standards and architectural patterns across AI-assisted development workflows.

### Context Diagram

```
                    ┌─────────────────────────────────────┐
                    │                                     │
                    │        Development Teams            │
                    │     (Stakeholders & Users)          │
                    │                                     │
                    └──────────────┬──────────────────────┘
                                   │
                                   │ Configuration & Oversight
                                   │
   ┌─────────────────┐            ┌▼─────────────────────────────┐            ┌─────────────────┐
   │                 │   MCP      │                              │  File      │                 │
   │   AI/LLM Tools  │  Protocol  │     C# MCP Server           │  Access    │  Persona-Template│
   │                 ├───────────►│        Template             ├───────────►│   Repository    │
   │ • Claude Desktop│  Requests  │                              │            │                 │
   │ • GitHub Copilot│            │  (Persona & Project         │            │ • Instructions  │
   │ • OpenAI Tools  │◄───────────┤   Instructions Server)      │            │ • Guidelines    │
   │ • VS Code       │  Responses │                              │            │ • Standards     │
   │ • Custom Tools  │            │                              │            │ • Templates     │
   └─────────────────┘            └──────────────────────────────┘            └─────────────────┘
```

**Legend:**
- [System] = The C# MCP Server Template (system being documented)
- <External Entity> = AI tools, development teams, file repository
- ─► = Primary data flow direction
- ◄─ = Response/feedback data flow

### External Interfaces

| Interface | Partner | Description | Input to System | Output from System |
|-----------|---------|-------------|-----------------|-------------------|
| **IF-EXT-01** | AI/LLM Development Tools | MCP protocol communication for accessing development guidelines | MCP resource requests (`persona://current`, `project://current`), tool execution requests (`list_available_instructions`) | Instruction content (markdown), available file listings (JSON), error responses |
| **IF-EXT-02** | Persona-Template Repository | File system access to instruction and guideline files | File modification notifications (implicit via TTL), directory structure changes | Instruction file content, file metadata, directory listings |
| **IF-EXT-03** | Development Teams | Configuration and operational oversight | Configuration settings (appsettings.json, environment variables), deployment parameters | Server status logs, error notifications, performance metrics |

### Detailed Interface Descriptions

#### IF-EXT-01: AI/LLM Development Tools Integration
**Partners:** Claude Desktop, GitHub Copilot, OpenAI API clients, VS Code extensions, custom MCP implementations
**Purpose:** Enable AI tools to access current development guidelines and project-specific instructions during code generation and review
**Business Process:** AI tools query for relevant instructions before generating code, ensuring output adheres to team standards and architectural patterns
**Data Exchanged:**
- **Input:** Resource requests for current persona/project instructions, discovery requests for available instruction sets
- **Output:** Current instruction content, lists of available instruction files with descriptions, contextual development guidelines

#### IF-EXT-02: Persona-Template Repository Access
**Partners:** Local file system, Git repository (persona-template)
**Purpose:** Read instruction files and maintain current view of available development guidelines
**Business Process:** Server monitors and caches instruction files, refreshes content based on TTL to ensure current guidelines are served
**Data Exchanged:**
- **Input:** File system events, directory structure, file modification times
- **Output:** File access requests, cache refresh operations

#### IF-EXT-03: Development Teams Operations
**Partners:** DevOps teams, development team leads, system administrators
**Purpose:** Configure, monitor, and maintain the instruction delivery system
**Business Process:** Teams configure server for their environment, monitor system health, and ensure instruction files are current and accessible
**Data Exchanged:**
- **Input:** Configuration parameters, deployment settings, monitoring requests
- **Output:** System status, performance metrics, error diagnostics

---

## 3.2 Technical Context

**Note:** Technical context provided to clarify protocol and transport implementation details critical for integration.

### Technical Context Diagram

```
                         HTTP/SSE (Primary)
   ┌─────────────────┐   Port 3000, JSON    ┌─────────────────────────────┐   File I/O
   │   MCP Client    ├──────────────────────►│    C# MCP Server           ├──────────────┐
   │                 │                       │      Template              │              │
   │ • HTTP/SSE      │◄──────────────────────┤                            │              ▼
   │ • JSON over     │   MCP Responses       │  • ASP.NET Core Host       │   ┌─────────────────┐
   │   WebSocket     │                       │  • ModelContextProtocol    │   │   File System   │
   └─────────────────┘                       │    .AspNetCore            │   │                 │
                                             │  • Dependency Injection    │   │ ../persona-     │
   ┌─────────────────┐   STDIO (Fallback)    │  • Configuration Mgmt      │   │ template/       │
   │   Legacy MCP    │   stdin/stdout        │                            │   │                 │
   │     Client      ├──────────────────────►│                            │   │ • *.instructions│
   │                 │   JSON Lines          │                            │   │ • projects/     │
   │ • Command Line  │◄──────────────────────┤                            │   │ • templates/    │
   │ • STDIO         │   MCP Responses       │                            │   └─────────────────┘
   └─────────────────┘                       └─────────────────────────────┘
                                                         │
                                                         │ Configuration
                                                         ▼
                                             ┌─────────────────────────────┐
                                             │    Configuration Sources    │
                                             │                             │
                                             │ • appsettings.json         │
                                             │ • Environment Variables     │
                                             │ • Docker/K8s ConfigMaps    │
                                             └─────────────────────────────┘
```

### Technical Interfaces

| Interface | Technology | Protocol | Format | Endpoint/Channel | Security |
|-----------|-----------|----------|--------|-----------------|----------|
| **IF-EXT-01A** | HTTP/SSE | MCP over HTTP/Server-Sent Events | JSON | `http://localhost:3000/mcp` | None (local) |
| **IF-EXT-01B** | STDIO | MCP over Standard I/O | JSON Lines | stdin/stdout pipes | Process isolation |
| **IF-EXT-02** | File I/O | System.IO.File APIs | Direct file access | `../persona-template/**/*.md` | OS file permissions |
| **IF-EXT-03A** | JSON Config | File system | JSON | `appsettings.json` | File system ACL |
| **IF-EXT-03B** | Environment Variables | Process environment | Key-value pairs | `MCP_*` variables | Process isolation |

### Technology Stack Integration Points

**MCP Protocol Implementation:**
- Uses `ModelContextProtocol.AspNetCore` for HTTP/SSE transport
- Uses `ModelContextProtocol` base library for STDIO transport
- Implements standard MCP resource and tool patterns

**File System Integration:**
- Asynchronous file I/O using `System.IO` APIs
- Relative path resolution for `../persona-template/` access
- File caching with TTL-based invalidation

**Configuration Management:**
- Microsoft.Extensions.Configuration for structured settings
- Environment variable overrides for Docker/cloud deployment
- Strongly-typed configuration objects for type safety