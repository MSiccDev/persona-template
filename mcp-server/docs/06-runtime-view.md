# 6. Runtime View

## Overview

This section documents the 4 most architecturally significant runtime scenarios that demonstrate how building blocks collaborate during system operation. These scenarios were selected because they cover the core MCP functionality (resource access, tool execution), demonstrate the caching strategy, and show critical error handling patterns.

---

## Scenario 1: MCP Client Requests Persona Resource (Cache Hit)

### Overview
A client (Claude, Copilot, etc.) requests the `persona://current` resource and the content is already cached, demonstrating the fast-path scenario that supports the #efficient quality goal.

### Step-by-Step

1. **MCP Client** → **MCP Protocol Layer**: Request `persona://current` resource
   - Data: Resource URI, client headers

2. **MCP Protocol Layer** → **PersonaResourceHandler**: Route resource request
   - Processing: Protocol parsing, request validation
   - Data: Resource identifier, request context

3. **PersonaResourceHandler** → **PersonaInstructionService**: Get persona content
   - Processing: Handler validates resource type
   - Data: Current persona file path from config

4. **PersonaInstructionService** → **FileCache**: Check cache for content
   - Processing: Generate cache key from file path
   - Data: File path, cache lookup

5. **FileCache** → **PersonaInstructionService**: Return cached content
   - Processing: TTL validation (not expired), cache hit
   - Data: Cached markdown content, metadata

6. **PersonaInstructionService** → **PersonaResourceHandler**: Content with metadata
   - Processing: No file I/O needed, content ready
   - Data: Markdown text, MIME type (text/markdown)

7. **PersonaResourceHandler** → **MCP Protocol Layer**: MCP resource response
   - Processing: Wrap content in MCP response format
   - Data: Resource content, resource metadata

8. **MCP Protocol Layer** → **MCP Client**: HTTP/SSE or STDIO response
   - Processing: Protocol serialization, transport selection
   - Data: JSON-formatted MCP response

### Quality Aspects
- **Performance:** <50ms response time (cached), meets #efficient goal
- **Security:** No file path validation needed (cached content pre-validated)
- **Availability:** Cache provides fast response even if file system is slow

---

## Scenario 2: MCP Client Requests Persona Resource (Cache Miss)

### Overview
A client requests the `persona://current` resource but content is not cached or TTL has expired, demonstrating the file I/O path and caching strategy.

### Step-by-Step

1. **MCP Client** → **MCP Protocol Layer**: Request `persona://current` resource
   - Data: Resource URI, client headers

2. **MCP Protocol Layer** → **PersonaResourceHandler**: Route resource request
   - Processing: Protocol parsing, request validation
   - Data: Resource identifier, request context

3. **PersonaResourceHandler** → **PersonaInstructionService**: Get persona content
   - Processing: Handler validates resource type
   - Data: Current persona file path from config

4. **PersonaInstructionService** → **FileCache**: Check cache for content
   - Processing: Generate cache key from file path
   - Data: File path, cache lookup

5. **FileCache** → **PersonaInstructionService**: Cache miss (expired or not found)
   - Processing: TTL expired or entry not found
   - Data: Cache miss indicator

6. **PersonaInstructionService** → **PathValidator**: Validate file path
   - Processing: Security validation, prevent directory traversal
   - Data: File path for validation

7. **PathValidator** → **PersonaInstructionService**: Path validation result
   - Processing: Path sanitization, security checks
   - Data: Validated path or validation error

8. **PersonaInstructionService** → **File System**: Read persona file (async)
   - Processing: Async file read with CancellationToken
   - Data: Validated file path

9. **File System** → **PersonaInstructionService**: File content
   - Processing: File I/O operation completed
   - Data: Raw markdown content, file metadata

10. **PersonaInstructionService** → **FileCache**: Store content with TTL
    - Processing: Update cache with fresh content
    - Data: File content, cache key, TTL timestamp

11. **PersonaInstructionService** → **PersonaResourceHandler**: Content with metadata
    - Processing: Content ready for response
    - Data: Markdown text, MIME type (text/markdown)

12. **PersonaResourceHandler** → **MCP Protocol Layer**: MCP resource response
    - Processing: Wrap content in MCP response format
    - Data: Resource content, resource metadata

13. **MCP Protocol Layer** → **MCP Client**: HTTP/SSE or STDIO response
    - Processing: Protocol serialization, transport selection
    - Data: JSON-formatted MCP response

### Quality Aspects
- **Performance:** <200ms response time (fresh read), meets #efficient goal
- **Security:** Path validation prevents directory traversal attacks (#secure goal)
- **Availability:** Graceful handling of file I/O, proper async operations

---

## Scenario 3: MCP Client Calls list_available_instructions Tool

### Overview
A client executes the `list_available_instructions` tool to discover available persona or project files, demonstrating tool execution and directory scanning.

### Step-by-Step

1. **MCP Client** → **MCP Protocol Layer**: Execute `list_available_instructions` tool
   - Data: Tool name, parameters (`type="persona"`)

2. **MCP Protocol Layer** → **InstructionToolHandler**: Route tool execution
   - Processing: Tool discovery via attributes, parameter validation
   - Data: Tool parameters, execution context

3. **InstructionToolHandler** → **InstructionToolHandler**: Validate parameters
   - Processing: Check `type` parameter ("persona" or "project")
   - Data: Tool parameters

4. **InstructionToolHandler** → **PersonaInstructionService**: Get available files
   - Processing: Delegate directory scanning to service
   - Data: File type filter, repository path from config

5. **PersonaInstructionService** → **PathValidator**: Validate directory path
   - Processing: Ensure repository path is safe
   - Data: Directory path for validation

6. **PathValidator** → **PersonaInstructionService**: Path validation result
   - Processing: Directory path validation
   - Data: Validated directory path

7. **PersonaInstructionService** → **File System**: Scan directory (async)
   - Processing: Directory enumeration, pattern matching (`*_persona.instructions.md`)
   - Data: Directory path, file pattern filter

8. **File System** → **PersonaInstructionService**: File list
   - Processing: File system enumeration completed
   - Data: List of matching file names

9. **PersonaInstructionService** → **InstructionToolHandler**: Available instructions
   - Processing: Format file list for tool response
   - Data: File names, descriptions (first line of each file)

10. **InstructionToolHandler** → **MCP Protocol Layer**: Tool execution result
    - Processing: Format as MCP tool response
    - Data: JSON array of available instructions

11. **MCP Protocol Layer** → **MCP Client**: HTTP/SSE or STDIO response
    - Processing: Protocol serialization
    - Data: JSON-formatted tool result

### Quality Aspects
- **Performance:** Directory scan completes in seconds even for large directories
- **Security:** Directory path validation prevents traversal outside repository
- **Usability:** Clear tool response format helps clients understand available options

---

## Scenario 4: File Not Found Error (Failure Scenario)

### Overview
A client requests a persona resource but the configured file does not exist, demonstrating error handling and recovery mechanisms.

### Step-by-Step

1. **MCP Client** → **MCP Protocol Layer**: Request `persona://current` resource
   - Data: Resource URI, client headers

2. **MCP Protocol Layer** → **PersonaResourceHandler**: Route resource request
   - Processing: Protocol parsing, request validation
   - Data: Resource identifier, request context

3. **PersonaResourceHandler** → **PersonaInstructionService**: Get persona content
   - Processing: Handler validates resource type
   - Data: Current persona file path from config

4. **PersonaInstructionService** → **FileCache**: Check cache for content
   - Processing: Generate cache key from file path
   - Data: File path, cache lookup

5. **FileCache** → **PersonaInstructionService**: Cache miss
   - Processing: File never cached or TTL expired
   - Data: Cache miss indicator

6. **PersonaInstructionService** → **PathValidator**: Validate file path
   - Processing: Security validation passes
   - Data: File path validation successful

7. **PersonaInstructionService** → **File System**: Attempt file read (async)
   - Processing: Async file read operation
   - Data: Validated file path

8. **File System** → **PersonaInstructionService**: FileNotFoundException
   - Processing: File does not exist
   - Data: Exception with file path details

### Error Handling

#### Error: File Not Found
**Condition:** Configured persona file does not exist on disk
**Handling:** PersonaInstructionService catches FileNotFoundException, logs error with full context, creates McpProtocolException with InvalidParams error code
**Recovery:** Return meaningful error message to client, system remains operational for other requests

9. **PersonaInstructionService** → **Microsoft.Extensions.Logging**: Log error
   - Processing: Structured logging with full context
   - Data: Error level, file path, stack trace, correlation ID

10. **PersonaInstructionService** → **PersonaResourceHandler**: McpProtocolException
    - Processing: Wrap file system error in MCP protocol exception
    - Data: Error code (InvalidParams), user-friendly message

11. **PersonaResourceHandler** → **MCP Protocol Layer**: Error response
    - Processing: Convert exception to MCP error response
    - Data: MCP error format with code and message

12. **MCP Protocol Layer** → **MCP Client**: HTTP/SSE error response
    - Processing: Protocol error serialization
    - Data: JSON error response with diagnostic information

### Quality Aspects
- **Reliability:** System does not crash, remains available for other requests (#reliable goal)
- **Security:** No sensitive path information leaked to client, full details logged server-side
- **Usability:** Clear error message helps developers diagnose configuration issues (#usable goal)