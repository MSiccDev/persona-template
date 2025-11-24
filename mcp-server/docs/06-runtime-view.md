# 6. Runtime View

## Overview

This section documents 4 architecturally significant runtime scenarios that demonstrate how building blocks collaborate during system operation. These scenarios cover core MCP tool functionality (discovery, retrieval, creation, validation), demonstrate the caching strategy, and show error handling patterns. The scenarios use **tool-based interactions** (not resource-based) as implemented.

---

## Scenario 1: Instruction Discovery with Caching (Cache Hit)

### Overview
A client calls `persona_list` tool to discover available persona instructions. The current persona content is already cached, demonstrating the fast-path scenario that supports the #efficient quality goal.

### Step-by-Step

1. **MCP Client** → **MCP Tool Layer**: Invoke `persona_list` tool
   - Data: Tool invocation (no parameters)
   - Processing: Tool dispatch via [McpServerTool] attribute

2. **MCP Tool Layer (PersonaMcpTools)** → **IPersonaInstructionService**: Call ListAvailablePersonasAsync()
   - Processing: Service method invocation
   - Data: Request for persona file list

3. **IPersonaInstructionService** → **File System (via cache)**: List personas directory
   - Processing: Directory scan, filter *.instructions.md files
   - Data: Directory path

4. **File System** → **IPersonaInstructionService**: Return cached list
   - Processing: Cache hit (TTL not expired), no file I/O needed
   - Data: List of persona file names

5. **IPersonaInstructionService** → **PersonaMcpTools**: Return results
   - Processing: Format results as string array
   - Data: List of persona names

6. **PersonaMcpTools** → **MCP Client**: Tool response
   - Processing: Serialize to JSON
   - Data: `["developer", "architect", "reviewer"]`

### Quality Aspects
- **Performance:** Cache hit delivers results instantly, <1ms latency
- **Security:** Directory scan is pre-validated, cached results are safe
- **Scalability:** In-memory cache reduces file system load for repeated requests

---

## Scenario 2: Instruction Retrieval with File I/O (Cache Miss)

### Overview
A client calls `persona_get` tool to retrieve a specific persona instruction. The content is not cached or TTL has expired, demonstrating file I/O and cache update.

### Step-by-Step

1. **MCP Client** → **PersonaMcpTools**: Invoke `persona_get` tool
   - Parameters: name="developer"
   - Data: Tool invocation with name parameter

2. **PersonaMcpTools** → **IPersonaInstructionService**: Call GetPersonaAsync("developer")
   - Processing: Service method invocation
   - Data: Persona name request

3. **IPersonaInstructionService** → **Internal Cache**: Check cache
   - Processing: Generate cache key from file path
   - Data: Cache lookup with TTL validation

4. **Cache** → **IPersonaInstructionService**: Cache miss
   - Processing: Entry not found or TTL expired
   - Data: Cache miss indicator

5. **IPersonaInstructionService** → **File System**: Read developer_persona.instructions.md (async)
   - Processing: Path validation, then async file read
   - Data: Validated file path

6. **File System** → **IPersonaInstructionService**: File content
   - Processing: File I/O completed
   - Data: Raw markdown content

7. **IPersonaInstructionService** → **Internal Cache**: Store with TTL
   - Processing: Update cache, calculate expiration
   - Data: Content, TTL timestamp

8. **IPersonaInstructionService** → **PersonaMcpTools**: Return content
   - Processing: Format for response
   - Data: Persona markdown content

9. **PersonaMcpTools** → **MCP Client**: Tool response
   - Processing: Serialize to JSON with content
   - Data: JSON with persona content

### Quality Aspects
- **Performance:** <200ms response (fresh read), subsequent calls use cache
- **Security:** Path validation prevents directory traversal
- **Availability:** Graceful file I/O with async operations and error handling

---

## Scenario 3: Instruction Creation from Template

### Overview
A client calls `persona_create_from_template` tool to create a new persona from template, demonstrating complete workflow including template loading, file creation, and cache invalidation.

### Step-by-Step

1. **MCP Client** → **PersonaMcpTools**: Invoke `persona_create_from_template` tool
   - Parameters: name="tech_lead", description="...", customFields={...}
   - Data: Creation request

2. **PersonaMcpTools** → **ITemplateService**: Call GetPersonaTemplateAsync()
   - Processing: Load template for substitution
   - Data: Template request

3. **ITemplateService** → **File System**: Read ../templates/persona_template.instructions.md
   - Processing: Load template from disk
   - Data: Template file path

4. **File System** → **ITemplateService**: Template content
   - Processing: File read completed
   - Data: Template markdown

5. **ITemplateService** → **PersonaMcpTools**: Return template
   - Data: Template content

6. **PersonaMcpTools** → **IPersonaInstructionService**: Call CreatePersonaFromTemplateAsync(name, description, fields)
   - Processing: Delegate to service
   - Data: Name, description, custom fields

7. **IPersonaInstructionService** → **File System**: Write new persona file
   - Processing: Template substitution, path validation, write to ./personas/tech_lead_persona.instructions.md
   - Data: Formatted content, target path

8. **File System** → **IPersonaInstructionService**: File created
   - Processing: File write completed successfully
   - Data: Confirmation

9. **IPersonaInstructionService** → **Internal Cache**: Invalidate cache
   - Processing: Clear old entries, prepare for refresh
   - Data: Cache invalidation signal

10. **IPersonaInstructionService** → **IPersonaInstructionService**: Validate new file
    - Processing: Run validation logic
    - Data: Validation result

11. **IPersonaInstructionService** → **PersonaMcpTools**: Return creation result
    - Data: Success status, file path, validation result

12. **PersonaMcpTools** → **MCP Client**: Tool response
    - Processing: Serialize to JSON
    - Data: `{"success": true, "filePath": "./personas/tech_lead_persona.instructions.md", "isValid": true}`

### Quality Aspects
- **Performance:** Cache is invalidated so next reads are fresh from disk
- **Security:** Path validation prevents directory traversal in file creation
- **Reliability:** File creation error properly reported in JSON response

---

## Scenario 4: Instruction Validation Workflow

### Overview
A client uses the `persona_validate` tool combined with the `validate_persona_prompt` to validate a persona file, demonstrating prompt-based validation and error reporting.

### Step-by-Step

1. **MCP Client** → **PersonaMcpTools**: Invoke `persona_validate` tool
   - Parameters: name="developer"
   - Data: Validation request

2. **PersonaMcpTools** → **IPersonaInstructionService**: Call ValidatePersonaAsync("developer")
   - Processing: Service method invocation
   - Data: Persona name for validation

3. **IPersonaInstructionService** → **IPersonaInstructionService**: Read file
   - Processing: Load persona file (uses cache if available, or fresh read)
   - Data: File content

4. **IPersonaInstructionService** → **IPromptService**: Get `validate_persona_prompt`
   - Processing: Load validation prompt with content injection
   - Data: Prompt name

5. **IPromptService** → **File System**: Read ../prompts/validate_persona_prompt.md
   - Processing: Load prompt template
   - Data: Prompt file path

6. **File System** → **IPromptService**: Prompt content
   - Processing: File read completed
   - Data: Prompt template markdown

7. **IPromptService** → **IPromptService**: Inject persona content
   - Processing: Template substitution (e.g., `{{persona_content}}` → developer persona markdown)
   - Data: Persona content for injection

8. **IPromptService** → **IPersonaInstructionService**: Enriched prompt
   - Data: Prompt with persona content injected

9. **IPersonaInstructionService** → **Validation Engine**: Validate structure
   - Processing: Check for required EARS sections, valid markdown, proper metadata
   - Data: Persona content

10. **Validation Engine** → **IPersonaInstructionService**: Validation result
    - Processing: Validation complete, errors collected
    - Data: List of validation errors (empty if valid)

11. **IPersonaInstructionService** → **PersonaMcpTools**: Return validation result
    - Data: `{"isValid": true, "errors": []}`

12. **PersonaMcpTools** → **MCP Client**: Tool response
    - Processing: Serialize to JSON
    - Data: Validation status and error details

### Alternative Path: Validation Failure

When validation detects errors:

11b. **Validation Engine** → **IPersonaInstructionService**: Validation errors found
     - Processing: Collect all validation issues with context
     - Data: List of `ValidationError` objects with file locations and descriptions

12b. **IPersonaInstructionService** → **PersonaMcpTools**: Return failure result
     - Data: `{"isValid": false, "errors": [{"location": "line 5", "message": "Missing EARS section", "severity": "error"}]}`

13b. **PersonaMcpTools** → **MCP Client**: Tool response with errors
     - Processing: Serialize errors to JSON
     - Data: Detailed validation failure report

### Quality Aspects
- **Performance:** Validation completes in <50ms (structure checks), LLM validation deferred to client
- **Security:** Persona content is validated before use, preventing malformed instructions
- **Usability:** Clear validation errors help developers fix persona definitions
- **Maintainability:** Prompt-based validation allows easy updates to validation rules