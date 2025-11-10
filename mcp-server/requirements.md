---
description: 'Complete requirements specification for C# MCP Server Template in EARS notation'
applyTo: '**'
---

# C# MCP Server Template - Requirements Specification

## Overview

This document defines all functional and non-functional requirements for the C# MCP Server Template project in structured EARS (Easy Approach to Requirements Syntax) notation.

## Functional Requirements

### Core Server Functionality

#### FR-001: Server Initialization
- **WHEN** the application starts
- **THE SYSTEM SHALL** initialize the MCP server with proper DI container setup
- **AND** load configuration from `appsettings.json` and environment variables
- **AND** log server URL and port on startup for client configuration

#### FR-002: Configuration Loading
- **WHEN** the server initializes
- **THE SYSTEM SHALL** read `PersonaRepoPath`, `CurrentPersona`, `CurrentProject`, `Transport`, `Port`, `Host`, and `CacheTtlSeconds` from `appsettings.json`
- **AND** support environment variable overrides for all configuration values
- **AND** use sensible defaults if values are missing

#### FR-003: Transport Selection
- **WHEN** the server starts
- **THE SYSTEM SHALL** support HTTP/SSE transport as the default (primary)
- **AND** support STDIO transport as an optional fallback
- **AND** select transport based on `Transport` configuration setting ("SSE" or "STDIO")

#### FR-004: Graceful Shutdown
- **WHEN** the server receives a shutdown signal
- **THE SYSTEM SHALL** stop accepting new client connections
- **AND** allow existing connections to complete gracefully
- **AND** release all resources (file handles, network connections)

### Persona Instructions Resource

#### FR-010: Read Persona Instructions
- **WHEN** the server starts
- **THE SYSTEM SHALL** read the persona instructions file from the path specified in configuration
- **AND** use relative path `../` (parent of mcp-server directory) as default to access persona-template repo

#### FR-011: Expose Persona Resource
- **WHEN** a client requests the `persona://current` MCP resource
- **THE SYSTEM SHALL** return the current persona instructions markdown as read-only text
- **AND** include resource description and MIME type (text/markdown)
- **AND** cache the content according to TTL settings

#### FR-012: Handle Missing Persona File
- **IF** the configured persona file does not exist
- **THEN THE SYSTEM SHALL** return a meaningful error message via MCP protocol
- **AND** log the error with full file path context

#### FR-013: Persona File Path Validation
- **WHEN** reading the persona file
- **THE SYSTEM SHALL** validate the file path to prevent directory traversal attacks (e.g., `../../etc/passwd`)
- **AND** sanitize the path before accessing the file system

### Project Instructions Resource

#### FR-020: Read Project Instructions
- **WHEN** `CurrentProject` is configured
- **THE SYSTEM SHALL** read the project instructions file from the path relative to `PersonaRepoPath`

#### FR-021: Expose Project Resource
- **WHEN** a client requests the `project://current` MCP resource
- **AND** `CurrentProject` is configured
- **THE SYSTEM SHALL** return the project instructions markdown as read-only text
- **AND** include resource description and MIME type (text/markdown)

#### FR-022: Optional Project Resource
- **IF** `CurrentProject` is not configured
- **THEN** the `project://current` resource SHALL NOT be exposed
- **AND** clients requesting this resource SHALL receive an appropriate error

#### FR-023: Handle Missing Project File
- **IF** the configured project file does not exist
- **THEN THE SYSTEM SHALL** return a meaningful error message via MCP protocol
- **AND** log the error with full file path context

#### FR-024: Project File Path Validation
- **WHEN** reading the project file
- **THE SYSTEM SHALL** validate the file path to prevent directory traversal attacks
- **AND** sanitize the path before accessing the file system

### Instruction Discovery Tool

#### FR-030: List Available Personas
- **WHEN** a client calls the `list_available_instructions` tool with parameter `type="persona"`
- **THE SYSTEM SHALL** scan the `PersonaRepoPath` directory
- **AND** return a JSON list of all files matching pattern `*_persona.instructions.md`
- **AND** include file names and descriptions (first line of each file)

#### FR-031: List Available Projects
- **WHEN** a client calls the `list_available_instructions` tool with parameter `type="project"`
- **THE SYSTEM SHALL** scan the `PersonaRepoPath/projects/` directory
- **AND** return a JSON list of all files matching pattern `*_project.instructions.md`
- **AND** include file names and descriptions

#### FR-032: Tool Parameter Validation
- **WHEN** a client calls the `list_available_instructions` tool with invalid `type` parameter
- **THE SYSTEM SHALL** validate the parameter value (must be "persona" or "project")
- **AND** return an error via MCP protocol if validation fails
- **AND** NOT execute the tool with invalid parameters

#### FR-033: Tool Description
- **THE SYSTEM SHALL** expose the `list_available_instructions` tool with clear description
- **AND** document all parameters and expected return format
- **AND** help LLMs understand the tool's purpose and correct usage

### File Caching

#### FR-040: Cache File Contents
- **WHEN** a file is read from disk
- **THE SYSTEM SHALL** cache the content in memory
- **AND** use the cache for subsequent requests to the same file
- **AND** improve performance by avoiding repeated disk I/O

#### FR-041: Cache TTL Management
- **WHEN** cached content is retrieved
- **THE SYSTEM SHALL** check if the cache has expired based on configured TTL (default: 300 seconds)
- **AND** refresh the cache from disk if TTL has expired
- **AND** support configurable TTL via `CacheTtlSeconds` setting

#### FR-042: Cache Invalidation
- **WHEN** a file is modified on disk
- **THE SYSTEM SHALL** eventually detect the change (on next TTL expiration)
- **AND** re-read the file and update the cache
- **OR** IF explicit cache invalidation is needed, provide a method to clear the cache

#### FR-043: Cache Key Strategy
- **THE SYSTEM SHALL** use file path as cache key
- **AND** track file modification time to detect changes
- **AND** ensure cache entries are unique per file path

### Error Handling

#### FR-050: File Not Found Error
- **WHEN** a requested file does not exist
- **THE SYSTEM SHALL** return an `McpProtocolException` with appropriate error code
- **AND** include the file path in the error message for debugging

#### FR-051: File Permission Error
- **WHEN** the server lacks permission to read a file
- **THE SYSTEM SHALL** return an `McpProtocolException` with permission error
- **AND** log the error with full context (file path, error details)

#### FR-052: Path Traversal Prevention
- **WHEN** a file path contains directory traversal attempts (../)
- **THE SYSTEM SHALL** reject the path
- **AND** return a validation error via MCP protocol
- **AND** log the suspicious attempt for security auditing

#### FR-053: Invalid Parameter Error
- **WHEN** a tool receives invalid parameters
- **THE SYSTEM SHALL** validate all parameters before execution
- **AND** return `McpProtocolException` with `McpErrorCode.InvalidParams`
- **AND** include specific details about what is invalid

#### FR-054: Graceful Error Recovery
- **WHEN** an error occurs during processing
- **THE SYSTEM SHALL** handle the error gracefully without crashing
- **AND** return meaningful error messages to the client
- **AND** log the error for debugging

---

## Non-Functional Requirements

### Architecture & Design

#### NFR-010: Provider Agnostic
- **THE SYSTEM SHALL** contain no code specific to any particular LLM provider (Claude, OpenAI, GitHub Copilot, etc.)
- **AND** work identically regardless of which MCP client connects
- **AND** use only MCP protocol standard features, no proprietary extensions

#### NFR-011: Separation of Concerns
- **THE SYSTEM SHALL** organize code into logical layers:
  - `Config/` - Configuration management
  - `Handlers/` - MCP resource and tool handlers
  - `Services/` - Business logic (file reading, caching)
  - `Program.cs` - Entry point and DI setup

#### NFR-012: Dependency Injection
- **THE SYSTEM SHALL** use `Microsoft.Extensions.Hosting` for DI container
- **AND** register all services in the DI container
- **AND** support dependency injection in tool methods

#### NFR-013: Auto-Discovery of Handlers
- **THE SYSTEM SHALL** auto-discover all classes with `[McpServerResourceType]` and `[McpServerToolType]` attributes
- **AND** register them automatically without manual registration code
- **AND** enable extensibility by adding new handlers as new files

### Performance

#### NFR-020: File Caching
- **THE SYSTEM SHALL** cache file contents to minimize disk I/O
- **AND** support configurable cache TTL (Time-To-Live)
- **AND** refresh cache automatically when TTL expires

#### NFR-021: Asynchronous Operations
- **THE SYSTEM SHALL** use `async/await` for I/O operations
- **AND** avoid blocking threads during file reads
- **AND** support `CancellationToken` for proper cancellation

#### NFR-022: Memory Efficiency
- **THE SYSTEM SHALL** avoid loading entire large files into memory unnecessarily
- **AND** consider streaming for large instruction files (>10MB)
- **AND** clean up resources promptly (IDisposable pattern)

#### NFR-023: Response Time
- **THE SYSTEM SHALL** return cached resources within milliseconds
- **AND** return directory listings within seconds even for large directories
- **AND** log response times for monitoring

### Security

#### NFR-030: Path Validation
- **THE SYSTEM SHALL** validate all file paths
- **AND** prevent directory traversal attacks (../ patterns)
- **AND** use APIs that build paths securely (System.IO.Path.Combine, etc.)

#### NFR-031: No Hardcoded Secrets
- **THE SYSTEM SHALL** NEVER hardcode API keys, passwords, or connection strings
- **AND** read all configuration from environment variables or secure config
- **AND** validate that secrets are not logged

#### NFR-032: Environment-Based Configuration
- **THE SYSTEM SHALL** support environment variable overrides for all configuration
- **AND** use `appsettings.json` as default configuration file
- **AND** support environment-specific configurations (appsettings.Production.json)

#### NFR-033: Secure Error Messages
- **THE SYSTEM SHALL** return meaningful error messages to clients
- **AND** NEVER expose stack traces to clients
- **AND** log detailed error information server-side for debugging

#### NFR-034: Input Validation
- **THE SYSTEM SHALL** validate all inputs from MCP clients
- **AND** reject invalid parameters with appropriate errors
- **AND** sanitize user-provided values before use

### Code Quality

#### NFR-040: C# 13 Standards
- **THE SYSTEM SHALL** use C# 13 features and modern best practices
- **AND** follow PascalCase for public members, camelCase for private fields
- **AND** prefix interfaces with "I" (e.g., IPersonaService)

#### NFR-041: XML Documentation
- **THE SYSTEM SHALL** include XML doc comments for all public APIs
- **AND** document method purpose, parameters, return values, and exceptions
- **AND** include `<example>` tags where helpful

#### NFR-042: Meaningful Comments
- **THE SYSTEM SHALL** include comments explaining WHY decisions were made
- **AND** NOT include comments that simply repeat what the code says
- **AND** document performance-critical sections and non-obvious logic

#### NFR-043: Nullable Reference Types
- **THE SYSTEM SHALL** use nullable reference types (declare non-nullable by default)
- **AND** use `is null` and `is not null` instead of `== null`
- **AND** validate inputs at entry points

#### NFR-044: Code Organization
- **THE SYSTEM SHALL** use file-scoped namespaces
- **AND** organize using statements logically (System, external, internal)
- **AND** keep files focused on single responsibility

### Testing

#### NFR-050: Unit Test Coverage
- **THE SYSTEM SHALL** include comprehensive unit tests
- **AND** achieve minimum 80% code coverage
- **AND** test all public methods and error paths

#### NFR-051: Test Framework
- **THE SYSTEM SHALL** use xUnit as the test framework
- **AND** use NSubstitute for dependency mocking
- **AND** follow arrange-act-assert pattern (with explicit comments)

#### NFR-052: Test Organization
- **THE SYSTEM SHALL** organize tests to match source code structure
- **AND** use descriptive test method names that indicate what is being tested
- **AND** group related tests in test classes

#### NFR-053: Error Path Testing
- **THE SYSTEM SHALL** test all error scenarios
- **AND** verify error messages and exception types
- **AND** test edge cases and boundary conditions

### Deployability

#### NFR-060: Local Development
- **THE SYSTEM SHALL** run on localhost:3000 with HTTP/SSE transport
- **AND** be accessible only from the local machine by default
- **AND** support rapid development and testing cycles

#### NFR-061: Docker Deployment
- **THE SYSTEM SHALL** include a `Dockerfile` for containerization
- **AND** build and run successfully in Docker without code changes
- **AND** support configuration via environment variables in Docker

#### NFR-062: Cloud Deployment
- **THE SYSTEM SHALL** support deployment to cloud platforms (AWS, Azure, GCP)
- **AND** work unchanged when deployed to cloud environments
- **AND** support configuration management for different environments

#### NFR-063: Configuration Management
- **THE SYSTEM SHALL** read all configuration from `appsettings.json` and environment variables
- **AND** support different configurations for local/Docker/cloud without code changes
- **AND** document how to configure for each deployment model

#### NFR-064: No Code Changes for Deployment
- **THE SYSTEM SHALL** work identically in all deployment models
- **AND** require no code changes to move from local to Docker to cloud
- **AND** support this through environment-based configuration only

### Logging & Monitoring

#### NFR-070: Structured Logging
- **THE SYSTEM SHALL** use `Microsoft.Extensions.Logging` for structured logs
- **AND** log to stderr to avoid interfering with stdio transport
- **AND** use appropriate log levels (Trace, Debug, Information, Warning, Error)

#### NFR-071: Startup Logging
- **THE SYSTEM SHALL** log server startup information
- **AND** include configured transport type, port, host, and repository path
- **AND** help users verify correct startup configuration

#### NFR-072: Error Logging
- **THE SYSTEM SHALL** log all errors with full context
- **AND** include file paths, error messages, and stack traces server-side
- **AND** use structured logging for easy parsing and analysis

#### NFR-073: Performance Monitoring
- **THE SYSTEM SHALL** support performance monitoring hooks
- **AND** allow integration with monitoring systems like Prometheus or Application Insights
- **AND** track cache hit/miss ratios

---

## Deployment Requirements

### Docker

#### DR-010: Dockerfile
- **THE SYSTEM SHALL** include a `Dockerfile` for containerization
- **AND** support multi-stage builds for optimized image size
- **AND** use appropriate base image for .NET applications

#### DR-011: Container Configuration
- **THE SYSTEM SHALL** accept all configuration via environment variables in container
- **AND** default to `0.0.0.0` as host for accessibility from other machines
- **AND** expose port 3000 by default

### Configuration Files

#### DR-020: appsettings.json
- **THE SYSTEM SHALL** include example `appsettings.json` with sensible defaults
- **AND** document all configuration options
- **AND** include examples for local, Docker, and cloud deployments

#### DR-021: Environment Variables
- **THE SYSTEM SHALL** support environment variable overrides for all config values
- **AND** use naming convention `Mcp__*` for environment variables
- **AND** document required and optional environment variables

### Git

#### DR-030: .gitignore
- **THE SYSTEM SHALL** include `.gitignore` appropriate for C# projects
- **AND** exclude `bin/`, `obj/`, `*.user`, `appsettings.Production.json`, etc.
- **AND** follow best practices for .NET projects

---

## Documentation Requirements

### README

#### DOC-010: Quick Start Guide
- **THE SYSTEM SHALL** include README.md with clear quick start instructions
- **AND** document prerequisites (.NET SDK version)
- **AND** provide commands to build and run locally

#### DOC-011: Configuration Guide
- **THE SYSTEM SHALL** document how to configure the server
- **AND** explain all appsettings.json options
- **AND** provide examples for common scenarios

#### DOC-012: Provider Integration
- **THE SYSTEM SHALL** include integration guides for major LLM providers:
  - Claude Desktop
  - OpenAI API
  - GitHub Copilot
  - VS Code Copilot
  - Semantic Kernel
  - Custom applications
- **AND** provide step-by-step configuration instructions for each

#### DOC-013: Deployment Options
- **THE SYSTEM SHALL** document deployment models:
  - Local HTTP
  - Docker
  - Cloud platforms (AWS, Azure, GCP)
- **AND** provide specific instructions for each model

#### DOC-014: Troubleshooting
- **THE SYSTEM SHALL** include troubleshooting section
- **AND** document common issues and solutions
- **AND** provide debugging tips

### Arc42 Architecture Documentation

#### DOC-020: Arc42 Structure
- **THE SYSTEM SHALL** include complete arc42 documentation in `/docs/` directory
- **AND** organize into 12 separate markdown files (01-12)
- **AND** include `/docs/images/` folder for diagrams

#### DOC-021: System Context (Arc42 Section 03)
- **THE SYSTEM SHALL** include system context diagram (ASCII or image)
- **AND** show server, clients, and persona-template repository
- **AND** document external interfaces and dependencies

#### DOC-022: Building Blocks (Arc42 Section 05)
- **THE SYSTEM SHALL** document building block hierarchy
- **AND** show component relationships and data flows
- **AND** include white-box descriptions of each component

#### DOC-023: Runtime Scenarios (Arc42 Section 06)
- **THE SYSTEM SHALL** document key runtime scenarios
- **AND** include sequence diagrams for client interactions
- **AND** show data flow from request to response

#### DOC-024: Deployment View (Arc42 Section 07)
- **THE SYSTEM SHALL** document deployment architecture
- **AND** show local, Docker, and cloud deployment models
- **AND** document infrastructure elements and requirements

---

## Quality Attributes

### Reliability
- Server must reliably deliver current instructions from persona-template repo
- Cache must not serve stale data beyond TTL
- Error handling must prevent crashes and data corruption

### Maintainability
- Code structure must be clear and easy to navigate
- Tests must cover critical paths and error scenarios
- Documentation must be comprehensive and up-to-date

### Usability
- Configuration must be straightforward and well-documented
- Error messages must be helpful and actionable
- Integration with providers must be well-explained

### Performance
- Cached resources must be returned in milliseconds
- Directory listings must be returned in seconds
- No memory leaks or unbounded resource usage

### Security
- Path validation must prevent directory traversal
- No secrets in code or logs
- Input validation on all external inputs

### Scalability
- Server must handle reasonable concurrent connections
- Caching strategy must prevent resource exhaustion
- Suitable for local, Docker, and cloud deployments

---

## Success Criteria

All of the following must be true:

- ✅ All code compiles without errors or warnings
- ✅ All unit tests pass with >80% code coverage
- ✅ Server starts and logs startup information correctly
- ✅ `persona://current` resource returns persona markdown
- ✅ `project://current` resource returns project markdown (if configured)
- ✅ `list_available_instructions` tool lists personas and projects correctly
- ✅ File caching works with configurable TTL
- ✅ Path validation prevents directory traversal
- ✅ Error handling prevents crashes
- ✅ Works on localhost:3000 (HTTP/SSE)
- ✅ Docker image builds and runs successfully
- ✅ Configuration works via appsettings.json and environment variables
- ✅ Arc42 documentation complete (12 sections + diagrams)
- ✅ README with quick start and integration guides
- ✅ No provider-specific code
- ✅ Follows all C# conventions (C# 13, XML docs, PascalCase/camelCase)
- ✅ No security vulnerabilities (path validation, no hardcoded secrets)
- ✅ Can be deployed to local, Docker, and cloud unchanged

