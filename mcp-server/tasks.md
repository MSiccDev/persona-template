---
description: 'Implementation Plan - Granular tasks for Phase 3'
---

# Implementation Tasks (Phase 3: IMPLEMENT)

**Total Estimated Effort:** 30-35 hours (6-8 days of development)

**Task Dependencies:** Complete in order; respect dependency groups.

**Architecture Foundation:** ✅ Arc42 Core Documentation Complete (Sections 01-06)
- **Design Decisions:** Available in `/docs/arc42/` for architectural reference
- **Quality Goals:** #reliable #efficient #usable #secure #flexible (measurable targets)
- **Technology Stack:** Validated in Arc42 Section 04 (Solution Strategy)
- **Building Blocks:** Defined in Arc42 Section 05 (Level-1 + Level-2 architecture)

**Key Arc42 Integration Points:**
- **Section 02 (Constraints):** .NET 8+, C# 13, ModelContextProtocol.AspNetCore
- **Section 04 (Solution Strategy):** HTTP/SSE primary, STDIO fallback, file-based resources
- **Section 05 (Building Blocks):** Service Layer → MCP Handlers → External Interface
- **Section 06 (Runtime View):** Resource access, tool invocation, caching, error scenarios

---

## GROUP 1: Project Setup & Configuration (3 tasks, ~3 hours)

### Task 1.1: Create Project Structure
**Duration:** 30 min | **Files:** Project files  
**Objective:** Set up .NET 8 solution with correct folder structure

**Steps:**
1. Create `/mcp-server` directory structure:
   ```
   mcp-server/
   ├── src/
   │   ├── Program.cs
   │   ├── PersonaServerConfig.cs
   │   ├── Handlers/
   │   │   ├── PersonaResourceHandler.cs
   │   │   ├── ProjectResourceHandler.cs
   │   │   └── InstructionToolHandler.cs
   │   └── Services/
   │       └── PersonaInstructionService.cs
   ├── tests/
   │   ├── PersonaInstructionServiceTests.cs
   │   ├── PathValidationTests.cs
   │   └── ConfigurationTests.cs
   ├── docs/ (already created)
   ├── mcp-server.csproj
   ├── mcp-server.tests.csproj
   ├── appsettings.json
   ├── appsettings.Development.json
   ├── appsettings.Production.json
   ├── Dockerfile
   ├── docker-compose.yml
   └── .gitignore
   ```

2. Run: `dotnet new globaljson --sdk-version 8.0.0 --roll-forward latestFeature`
3. Run: `dotnet new sln -n PersonaMcpServer`
4. Verify all directories exist and structure matches above

**Acceptance Criteria:**
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` runs (no tests yet, but infrastructure works)
- [ ] All project files at correct paths

---

### Task 1.2: Create Project Files (.csproj)
**Duration:** 30 min | **Files:** `mcp-server.csproj`, `mcp-server.tests.csproj`  
**Objective:** Define NuGet dependencies and build configuration

**Main Project (.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <PropertyGroup>
    <OutputType>Exe</OutputType>
    <StartupObject>Program</StartupObject>
    <TrimMode>partial</TrimMode>
  </PropertyGroup>

  <ItemGroup>
    <!-- MCP SDK -->
    <PackageReference Include="ModelContextProtocol.AspNetCore" Version="1.0.0" />
    <PackageReference Include="ModelContextProtocol" Version="1.0.0" />
    
    <!-- Framework -->
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    
    <!-- Utilities -->
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
</Project>
```

**Test Project (.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="AwesomeAssertions" Version="9.3.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../src/mcp-server.csproj" />
  </ItemGroup>
</Project>
```

**Acceptance Criteria:**
- [ ] `dotnet restore` succeeds
- [ ] All NuGet packages resolve to correct versions
- [ ] Build succeeds with `dotnet build`

---

### Task 1.3: Create Configuration Model
**Duration:** 45 min | **File:** `src/PersonaServerConfig.cs`  
**Objective:** Define configuration schema and defaults

**PersonaServerConfig.cs:**
```csharp
public class PersonaServerConfig
{
    /// <summary>Host to listen on (default: localhost)</summary>
    public string Host { get; set; } = "localhost";
    
    /// <summary>Port to listen on (default: 3000)</summary>
    public int Port { get; set; } = 3000;
    
    /// <summary>Transport mechanism (SSE or STDIO)</summary>
    public string Transport { get; set; } = "SSE";
    
    /// <summary>Path to persona-template repository (default: ../)</summary>
    public string PersonaRepoPath { get; set; } = "../";
    
    /// <summary>Name of current persona file</summary>
    public string? CurrentPersona { get; set; }
    
    /// <summary>Name of current project file</summary>
    public string? CurrentProject { get; set; }
    
    /// <summary>Cache TTL in seconds (default: 300)</summary>
    public int CacheTtlSeconds { get; set; } = 300;
    
    /// <summary>Max cache size in bytes (default: 100MB)</summary>
    public long MaxCacheSizeBytes { get; set; } = 100 * 1024 * 1024;
    
    /// <summary>Validation: Ensure configuration is valid</summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new();
        
        if (Port <= 0 || Port > 65535)
            errors.Add("Port must be between 1 and 65535");
        if (CacheTtlSeconds < 1)
            errors.Add("CacheTtlSeconds must be >= 1");
        if (MaxCacheSizeBytes < 1024)
            errors.Add("MaxCacheSizeBytes must be >= 1024 (1KB)");
        if (Transport != "SSE" && Transport != "STDIO")
            errors.Add("Transport must be 'SSE' or 'STDIO'");
        
        return errors.Count == 0;
    }
}
```

**appsettings.json:**
```json
{
  "Mcp": {
    "Host": "localhost",
    "Port": 3000,
    "Transport": "SSE",
    "PersonaRepoPath": "../",
    "CacheTtlSeconds": 300,
    "MaxCacheSizeBytes": 104857600
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Acceptance Criteria:**
- [ ] Configuration loads from appsettings.json
- [ ] Environment variables override appsettings (e.g., `Mcp__Port=4000`)
- [ ] Validation catches invalid config
- [ ] All defaults documented in XML comments

---

## GROUP 2: Core Service Layer (2 tasks, ~4 hours)

### Task 2.1: Implement PersonaInstructionService
**Duration:** 2 hours | **File:** `src/Services/PersonaInstructionService.cs`  
**Objective:** Core service with caching, path validation, file I/O

**Arc42 Reference:** Section 05 (Building Blocks) - Service Layer architecture  
**Quality Goals:** #efficient (caching), #secure (path validation), #reliable (error handling)

**Key Methods:**
```csharp
public class PersonaInstructionService : IDisposable
{
    // GetPersonaContent(string personaName) → cached file read
    // GetProjectContent(string projectName) → cached file read
    // ListAvailablePersonasAsync(CancellationToken) → scan directory
    // ListAvailableProjectsAsync(CancellationToken) → scan directory
    // ValidatePath(string filePath) → security validation
}
```

**Implementation Checklist:**
- [ ] In-memory cache with Dictionary<string, CacheEntry>
- [ ] ReaderWriterLockSlim for concurrent access
- [ ] TTL-based expiration logic
- [ ] Path validation (no traversal, absolute paths, file extension)
- [ ] Directory scanning for list operations
- [ ] Error handling (FileNotFoundException, UnauthorizedAccessException, etc.)
- [ ] Structured logging with request context
- [ ] IDisposable cleanup for ReaderWriterLockSlim
- [ ] Unit tests for all public methods

**Acceptance Criteria:**
- [ ] Cache hit returns in <10ms
- [ ] Cache miss reads file and caches for TTL
- [ ] Path validation rejects `..`, `/`, absolute paths
- [ ] ListAsync operations return sorted results
- [ ] All error cases handled gracefully

---

### Task 2.2: Implement Cache Invalidation & Monitoring
**Duration:** 2 hours | **File:** `src/Services/PersonaInstructionService.cs` (continued)  
**Objective:** Add TTL checking, max size limit, metrics

**Features:**
- [ ] Check TTL on cache read (remove if expired)
- [ ] Max cache size enforcement (configurable)
- [ ] LRU eviction when size exceeded
- [ ] Cache statistics (hit/miss counts, size in bytes)
- [ ] Logging for cache events (hit, miss, eviction, expiration)

**Unit Tests:**
- [ ] Verify expired cache entries removed
- [ ] Verify cache size never exceeds max
- [ ] Verify LRU eviction removes least-recent

**Acceptance Criteria:**
- [ ] Cache never exceeds MaxCacheSizeBytes
- [ ] Expired entries removed on next access
- [ ] Hit/miss statistics accurate
- [ ] No memory leaks (cache doesn't grow after reads)

---

## GROUP 3: Handler Implementation (3 tasks, ~5 hours)

### Task 3.1: Implement PersonaResourceHandler
**Duration:** 1.5 hours | **File:** `src/Handlers/PersonaResourceHandler.cs`  
**Objective:** Expose persona files as MCP resources

**Required Methods:**
```csharp
[McpServerResourceType("persona://")]
public class PersonaResourceHandler
{
    [McpServerResourceDescription]
    public ResourceDescription[] GetResourceDescriptions()
    {
        return new[] {
            new ResourceDescription {
                Type = "persona://current",
                Description = "Current persona instruction set"
            }
        };
    }
    
    [McpServerResource]
    public async Task<ResourceContents> GetResourceContents(
        string resourceName,
        CancellationToken cancellationToken)
    {
        // Get persona content; return as ResourceContents
    }
}
```

**Implementation Checklist:**
- [ ] Parse resource name (extract persona identifier)
- [ ] Call PersonaInstructionService.GetPersonaContent()
- [ ] Handle errors (FileNotFoundException → ResourceNotFound)
- [ ] Return ResourceContents with MimeType="text/markdown"
- [ ] Structured logging
- [ ] Unit tests with mocked service

**Acceptance Criteria:**
- [ ] Resource discoverable via MCP protocol
- [ ] Returns correct content for existing personas
- [ ] Returns MCP error for missing personas
- [ ] Response time <50ms for cached content

---

### Task 3.2: Implement ProjectResourceHandler
**Duration:** 1.5 hours | **File:** `src/Handlers/ProjectResourceHandler.cs`  
**Objective:** Expose project files as MCP resources

**Arc42 Reference:** Section 05 (Building Blocks) - Level-2 MCP Handlers  
**Quality Goals:** #usable (consistent interface), #reliable (error handling)

**Identical to PersonaResourceHandler but for `project://` URI scheme.**

**Implementation Checklist:**
- [ ] Resource type: `project://current`
- [ ] Call PersonaInstructionService.GetProjectContent()
- [ ] Same error handling as PersonaResourceHandler
- [ ] Structured logging
- [ ] Unit tests

**Acceptance Criteria:**
- [ ] Project resources discoverable
- [ ] Returns correct content for projects
- [ ] Error handling matches PersonaResourceHandler

---

### Task 3.3: Implement InstructionToolHandler
**Duration:** 2 hours | **File:** `src/Handlers/InstructionToolHandler.cs`  
**Objective:** Expose list operations as MCP tools

**Arc42 Reference:** Section 06 (Runtime View) - Tool invocation scenarios  
**Quality Goals:** #usable (discovery), #efficient (caching)

**Required Tools:**
```csharp
[McpServerToolType]
public class InstructionToolHandler
{
    [McpServerTool]
    public async Task<string> ListAvailableInstructions(
        string type,  // "persona" or "project"
        CancellationToken cancellationToken)
    {
        // Return JSON array of available items
    }
}
```

**Implementation Checklist:**
- [ ] List personas with descriptions (from first line of file)
- [ ] List projects with descriptions
- [ ] Validate type parameter (only "persona" or "project")
- [ ] Return JSON with array of {name, description}
- [ ] Sort results alphabetically
- [ ] Structured logging
- [ ] Unit tests

**Acceptance Criteria:**
- [ ] Tool callable from MCP protocol
- [ ] Returns valid JSON with correct schema
- [ ] All available items listed (complete inventory)
- [ ] Descriptions extracted from file (first line after YAML front matter)

---

## GROUP 4: Program Initialization (1 task, ~1.5 hours)

### Task 4.1: Implement Program.cs & Startup
**Duration:** 1.5 hours | **File:** `src/Program.cs`  
**Objective:** Set up dependency injection, host builder, transport

**Arc42 Reference:** Section 04 (Solution Strategy) - Technology stack decisions  
**Quality Goals:** #flexible (configuration-driven), #reliable (graceful startup/shutdown)

**Program.cs Structure:**
```csharp
var builder = Host.CreateApplicationBuilder(args);

// 1. Load configuration
var config = new PersonaServerConfig();
builder.Configuration.GetSection("Mcp").Bind(config);
config.Validate(); // Throws if invalid

// 2. Set up logging
builder.Services.AddLogging(options => {
    options.AddConsole();
});

// 3. Register services
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<PersonaInstructionService>();

// 4. Register MCP handlers (auto-discovery via attributes)
var assembly = Assembly.GetExecutingAssembly();
foreach (var type in assembly.GetTypes()
    .Where(t => t.GetCustomAttribute<McpServerResourceTypeAttribute>() != null))
{
    builder.Services.AddSingleton(type);
}
foreach (var type in assembly.GetTypes()
    .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null))
{
    builder.Services.AddSingleton(type);
}

// 5. Configure transport (HTTP/SSE or STDIO)
if (config.Transport == "SSE")
{
    builder.WebHost.ConfigureKestrel(options => {
        options.ListenAnyIP(config.Port);
    });
}

var host = builder.Build();
host.Run();
```

**Implementation Checklist:**
- [ ] Configuration loaded and validated
- [ ] All services registered in DI container
- [ ] Logging configured for console output
- [ ] Handler auto-discovery via reflection
- [ ] HTTP server (for SSE) or STDIO (fallback)
- [ ] Server starts and logs: "MCP Server starting on {Host}:{Port}"
- [ ] Server responds to health check endpoints

**Acceptance Criteria:**
- [ ] `dotnet run` starts server without errors
- [ ] Server logs startup information
- [ ] Handlers auto-discovered (log count of handlers registered)
- [ ] Ctrl+C gracefully shuts down

---

## GROUP 5: Error Handling & Validation (2 tasks, ~3 hours)

### Task 5.1: Implement Comprehensive Path Validation
**Duration:** 1.5 hours | **File:** `src/Services/PersonaInstructionService.cs` (ValidatePath method)  
**Objective:** Security: prevent directory traversal, absolute paths, etc.

**Validation Rules:**
- [ ] No `..` sequences (directory traversal)
- [ ] No leading `/` (absolute paths on Unix)
- [ ] No leading drive letter (absolute paths on Windows)
- [ ] Must resolve within PersonaRepoPath
- [ ] Must end with `.instructions.md`
- [ ] No null bytes
- [ ] No suspicious characters (*, ?, :, etc. except `.` and `-`)

**Security Tests:**
```csharp
[Theory]
[InlineData("../../etc/passwd")]  // Traversal
[InlineData("/etc/passwd")]       // Absolute
[InlineData("C:\\Windows\\System32")]  // Windows absolute
[InlineData("test\0injection")]   // Null byte
[InlineData("../marco_persona.instructions.md")]  // Traversal
public void ValidatePath_RejectsInvalidPaths(string path)
{
    // Assert: throws ArgumentException
}

[Theory]
[InlineData("marco_persona.instructions.md")]
[InlineData("projects/project1_project.instructions.md")]
public void ValidatePath_AcceptsValidPaths(string path)
{
    // Assert: does not throw
}
```

**Acceptance Criteria:**
- [ ] All security test cases pass
- [ ] No false negatives (valid paths accepted)
- [ ] No false positives (invalid paths rejected)
- [ ] Logging for rejected paths (potential attack indicator)

---

### Task 5.2: Implement Error Handling & Exceptions
**Duration:** 1.5 hours | **File:** `src/Services/PersonaInstructionService.cs` + Handlers  
**Objective:** Graceful error handling, meaningful error messages

**Error Scenarios:**
- [ ] FileNotFoundException (file not found) → ResourceNotFound
- [ ] UnauthorizedAccessException (permission denied) → InternalError + logged
- [ ] DirectoryNotFoundException → InternalError + logged
- [ ] Invalid path format → InvalidParams
- [ ] Null/empty parameters → InvalidParams
- [ ] Unhandled exceptions → InternalError + logged

**Implementation:**
```csharp
try { /* operation */ }
catch (FileNotFoundException ex)
{
    _logger.LogWarning(ex, "File not found: {Path}", path);
    throw new McpProtocolException(
        McpErrorCode.ResourceNotFound, 
        $"Resource not found: {path}");
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogError(ex, "Permission denied: {Path}", path);
    throw new McpProtocolException(
        McpErrorCode.InternalError, 
        "Permission denied accessing resource");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    throw new McpProtocolException(
        McpErrorCode.InternalError, 
        "An unexpected error occurred");
}
```

**Acceptance Criteria:**
- [ ] All error types return MCP protocol-compliant errors
- [ ] Error messages are meaningful but not revealing
- [ ] Stack traces logged server-side (not sent to client)
- [ ] No unhandled exceptions crash server

---

## GROUP 6: Testing (3 tasks, ~8 hours)

### Task 6.1: Unit Tests - PersonaInstructionService
**Duration:** 3 hours | **File:** `tests/PersonaInstructionServiceTests.cs`  
**Objective:** >90% coverage for service layer

**Test Classes:**
- [ ] CacheTests (hit, miss, TTL expiration, eviction)
- [ ] FileOperationTests (read, list directories, encodings)
- [ ] PathValidationTests (see Task 5.1)
- [ ] ErrorHandlingTests (all exception types)
- [ ] ConcurrencyTests (concurrent reads, lock behavior)

**Sample Test:**
```csharp
[Fact]
public async Task GetPersonaContent_CachesResult()
{
    var service = new PersonaInstructionService(config);
    
    var content1 = await service.GetPersonaContent("marco_persona.instructions.md");
    var content2 = await service.GetPersonaContent("marco_persona.instructions.md");
    
    Assert.Equal(content1, content2);
    Assert.True(content1.Length > 0);
}
```

**Acceptance Criteria:**
- [ ] >90% code coverage
- [ ] All cache scenarios tested
- [ ] Path validation thoroughly tested
- [ ] Concurrent access tested (no race conditions)

---

### Task 6.2: Unit Tests - Handlers
**Duration:** 2.5 hours | **File:** `tests/HandlersTests.cs`  
**Objective:** >85% coverage for handlers

**Test Classes:**
- [ ] PersonaResourceHandlerTests (get resource, missing resource)
- [ ] ProjectResourceHandlerTests (get resource, missing resource)
- [ ] InstructionToolHandlerTests (list personas, list projects, invalid type)

**Sample Test:**
```csharp
[Fact]
public async Task ListAvailableInstructions_ReturnsSortedList()
{
    var handler = new InstructionToolHandler(service);
    
    var result = await handler.ListAvailableInstructions("persona", CancellationToken.None);
    var personas = JsonSerializer.Deserialize<List<string>>(result);
    
    Assert.NotEmpty(personas);
    var sorted = personas.OrderBy(p => p).ToList();
    Assert.Equal(sorted, personas);  // Verify alphabetical order
}
```

**Acceptance Criteria:**
- [ ] >85% handler code coverage
- [ ] All resource types tested
- [ ] Missing resources handled correctly
- [ ] JSON responses valid

---

### Task 6.3: Integration Tests & E2E Scenarios
**Duration:** 2.5 hours | **File:** `tests/IntegrationTests.cs`  
**Objective:** >75% coverage; test full request/response cycle

**Test Scenarios:**
- [ ] Server startup + health check
- [ ] Complete request: GetPersonaResource → Response
- [ ] Complete request: ListInstructions → Response
- [ ] Concurrent requests don't interfere
- [ ] Cache persists across multiple requests
- [ ] Configuration changes reflected in behavior

**Sample Test:**
```csharp
[Fact]
public async Task FullRequestCycle_ReturnsValidResponse()
{
    // 1. Start server in test mode
    var host = await StartTestServer();
    
    // 2. Make MCP request
    var response = await GetPersonaResource("marco_persona.instructions.md");
    
    // 3. Verify response
    Assert.NotNull(response.Content);
    Assert.Contains("# Marco Persona", response.Content);
    
    // 4. Cleanup
    await host.StopAsync();
}
```

**Acceptance Criteria:**
- [ ] All major workflows tested
- [ ] >75% overall code coverage
- [ ] No flaky tests
- [ ] Tests run in <30 seconds

---

## GROUP 7: Configuration & Deployment (2 tasks, ~3 hours)

### Task 7.1: Create Dockerfile & Docker Compose
**Duration:** 1 hour | **Files:** `Dockerfile`, `docker-compose.yml`  
**Objective:** Containerized deployment

**Dockerfile (multi-stage):**
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/", "."]
RUN dotnet build -c Release

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=publish /app .
EXPOSE 3000
ENTRYPOINT ["dotnet", "mcp-server.dll"]
```

**docker-compose.yml:**
```yaml
version: '3.8'
services:
  mcp-server:
    build: .
    ports:
      - "3000:3000"
    environment:
      Mcp__Host: 0.0.0.0
      Mcp__Port: 3000
      Mcp__Transport: SSE
    volumes:
      - ../:/data/persona-template:ro
    working_dir: /data
```

**Acceptance Criteria:**
- [ ] `docker build` succeeds
- [ ] `docker run` starts server
- [ ] Server accessible at http://localhost:3000
- [ ] `docker-compose up` works end-to-end

---

### Task 7.2: Create Kubernetes Manifests & Deployment Configs
**Duration:** 2 hours | **File:** `k8s/mcp-deployment.yaml`  
**Objective:** Cloud-ready deployment

**K8s Manifest:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcp-server
spec:
  replicas: 2
  selector:
    matchLabels:
      app: mcp-server
  template:
    metadata:
      labels:
        app: mcp-server
    spec:
      containers:
      - name: mcp-server
        image: mcp-server:latest
        ports:
        - containerPort: 3000
        env:
        - name: Mcp__Host
          value: "0.0.0.0"
        - name: Mcp__Port
          value: "3000"
        volumeMounts:
        - name: persona-template
          mountPath: /data
        livenessProbe:
          httpGet:
            path: /health
            port: 3000
          initialDelaySeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 3000
          initialDelaySeconds: 5
      volumes:
      - name: persona-template
        persistentVolumeClaim:
          claimName: persona-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: mcp-server-service
spec:
  selector:
    app: mcp-server
  ports:
  - port: 3000
    targetPort: 3000
  type: LoadBalancer
```

**Acceptance Criteria:**
- [ ] Manifests valid (`kubectl apply --dry-run`)
- [ ] HA configuration (2+ replicas)
- [ ] Health checks configured
- [ ] PersistentVolume for persona-template

---

## GROUP 8: Documentation & Polish (1 task, ~1 hour)

### Task 8.1: Create Installation & Usage README
**Duration:** 1 hour | **File:** `README.md`  
**Objective:** Developer onboarding guide

**Content:**
- [ ] Project overview (3-4 sentences)
- [ ] Prerequisites (OS, .NET 8, Docker)
- [ ] Quick start (3 commands to run)
- [ ] Configuration documentation
- [ ] Development vs Production setup
- [ ] Testing instructions
- [ ] Deployment instructions (local, Docker, K8s)
- [ ] Troubleshooting section
- [ ] Contributing guidelines

**Acceptance Criteria:**
- [ ] README completeness verified
- [ ] Quick start tested by another developer
- [ ] All sections documented

**Note:** ✅ Arc42 architecture documentation complete (6 core sections) - Reference `/docs/arc42/` during implementation

---

## GROUP 9: Final Validation (1 task, ~1.5 hours)

### Task 9.1: End-to-End Validation
**Duration:** 1.5 hours | **Verification:**

**Manual Test Checklist:**
- [ ] Server starts locally (`dotnet run`)
- [ ] Resource endpoint works (persona://current)
- [ ] Tool endpoint works (list_available_instructions)
- [ ] Error handling graceful (missing file)
- [ ] Cache working (response fast on second request)
- [ ] Logging structured and useful
- [ ] Docker image builds and runs
- [ ] K8s deployment valid
- [ ] Documentation accurate and complete

**Performance Baseline:**
- [ ] Cache hit latency: <10ms (measure with stopwatch)
- [ ] Cache miss latency: <100ms (measure with stopwatch)
- [ ] Memory usage stable: <150MB
- [ ] No memory leaks (monitor for 5 minutes)

**Security Checklist:**
- [ ] Path traversal blocked
- [ ] Absolute paths blocked
- [ ] Invalid paths logged
- [ ] Error messages don't leak internal paths

**Acceptance Criteria:**
- [ ] All manual tests pass
- [ ] Performance baseline met
- [ ] Security checks pass
- [ ] Ready for Phase 4: VALIDATE

---

## Task Dependencies Summary

```
1.1 (Project Structure)
  ├─→ 1.2 (Project Files) ─→ 1.3 (Configuration)
        ├─→ 2.1 (Service Layer) ─→ 2.2 (Cache Invalidation)
        │      ├─→ 3.1 (PersonaResourceHandler)
        │      ├─→ 3.2 (ProjectResourceHandler)
        │      └─→ 3.3 (InstructionToolHandler)
        │             ├─→ 4.1 (Program.cs)
        │                  ├─→ 5.1 (Path Validation) ─→ 6.1 (Unit Tests)
        │                  ├─→ 5.2 (Error Handling) ─────→ 6.2 (Handler Tests)
        │                  │                              └→ 6.3 (Integration Tests)
        │                  ├─→ 7.1 (Dockerfile)
        │                  └─→ 7.2 (K8s Manifests)
        └─→ 8.1 (README)
        └─→ 8.2 (Contributing Guide)
        └─→ 9.1 (E2E Validation)
```

---

## Execution Notes

- **Daily Goals:** Complete 1-2 groups per day
- **Review Points:** After each group, verify acceptance criteria
- **Blockers:** If task blocked, escalate immediately (don't continue)
- **Refactoring:** If refactoring needed mid-task, document reason in commit message
- **Testing:** Write tests as you implement (TDD mindset)
- **Documentation:** Reference Arc42 sections for architectural decisions; update README if needed

---

## Success Criteria (Phase 3 Complete)

- ✅ All 9 groups (24 tasks) implemented
- ✅ >85% code coverage across all components
- ✅ Zero unhandled exceptions
- ✅ All MCP resources discoverable
- ✅ All MCP tools callable
- ✅ Security validation passes
- ✅ Performance baseline met
- ✅ Docker deployment works
- ✅ K8s deployment manifests valid
- ✅ README complete (Arc42 architecture docs already available)
