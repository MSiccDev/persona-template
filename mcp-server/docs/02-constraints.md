# 2. Constraints

This section documents constraints that restrict design, implementation, or process freedom. All architectural decisions must work within these boundaries.

## 2.1 Technical Constraints

| Constraint | Rationale | Authority | Negotiable |
|-----------|-----------|-----------|-----------|
| **C# 13 and .NET 8+** | Repository standardization on modern C# features, nullable reference types, file-scoped namespaces | Architecture Guidelines (csharp.instructions.md) | No |
| **ModelContextProtocol SDK (prerelease)** | Standard MCP protocol implementation, HTTP/SSE and STDIO transport support | MCP Specification Requirement | No |
| **Microsoft.Extensions.Hosting** | Dependency injection container, configuration management, application lifecycle | .NET Ecosystem Standard | No |
| **xUnit Testing Framework** | Unit testing with >80% coverage requirement | Repository Testing Standards | Yes (with justification) |
| **File-Based Only (No Database)** | Simplicity, deployment flexibility, avoid external dependencies | System Design Requirement | No |
| **Provider-Agnostic Implementation** | Must work identically with Claude, OpenAI, GitHub Copilot, custom MCP clients | Business Requirement | No |
| **Cross-Platform Support** | Windows, Linux, macOS compatibility for diverse development environments | User Base Requirements | No |
| **Docker Containerization** | Standardized deployment, cloud platform compatibility | DevOps Requirements | No |

## 2.2 Organizational Constraints

| Constraint | Rationale | Authority | Negotiable |
|-----------|-----------|-----------|-----------|
| **Spec-Driven Workflow v1** | Repository methodology: ANALYZE→DESIGN→IMPLEMENT→VALIDATE→REFLECT→HANDOFF | Repository Standards (spec-driven-workflow-v1.instructions.md) | No |
| **Phase-by-Phase Development** | User requirement for explicit review and approval of each phase | User Constraint | No |
| **Solo Development Project** | Single developer implementation with community contributions | Project Scope | Yes (if contributors available) |
| **Open Source Licensing** | Public repository, community access and contributions | Repository Policy | No |
| **Arc42 Documentation Standard** | Complete 12-section architecture documentation required | Documentation Standards | No |
| **English Documentation** | All technical documentation in English, German only for conversation | Language Policy | No |

## 2.3 Political Constraints

| Constraint | Rationale | Authority | Negotiable |
|-----------|-----------|-----------|-----------|
| **OWASP Security Standards** | Security-first approach, input validation, no hardcoded secrets | Security Policy (security-and-owasp.instructions.md) | No |
| **No Vendor Lock-in** | Avoid proprietary dependencies, maintain provider independence | Strategic Decision | No |
| **MIT/Apache Compatible Licensing** | Open source license compatibility for community adoption | Legal Requirement | No |
| **Privacy-by-Design** | No data collection, file-based operation only | Privacy Policy | No |

## 2.4 Conventions

| Convention | Rationale | Authority | Negotiable |
|-----------|-----------|-----------|-----------|
| **PascalCase/camelCase Naming** | C# language conventions, public members PascalCase, private fields camelCase | Language Standards (csharp.instructions.md) | No |
| **XML Documentation Comments** | All public APIs must have XML doc comments with examples | Code Quality Standards | No |
| **EARS Notation for Requirements** | Structured requirements syntax for clarity and testability | Requirements Standards | No |
| **Nullable Reference Types** | Use nullable reference types, validate inputs at entry points | Modern C# Practices | No |
| **File-Scoped Namespaces** | C# 10+ feature for cleaner code organization | Code Organization Standard | No |
| **Async/Await Pattern** | All I/O operations must be asynchronous with CancellationToken support | Performance Standards | No |
| **Dependency Injection** | Constructor injection for all services, no service locator pattern | Architecture Pattern | No |
| **Attribute-Based Tool Discovery** | Use `[McpServerTool]` and `[McpServerPrompt]` for auto-discovery of MCP tools and prompts | Framework Convention | No |

## 2.5 Constraint Impact Analysis

### High Impact Technical Constraints

**ModelContextProtocol SDK (Prerelease)**
- **Impact**: Potential API changes, limited documentation, community support
- **Mitigation**: Pin to specific version, implement abstraction layer for SDK interactions
- **Risk**: Medium - SDK is actively developed by Anthropic

**Provider-Agnostic Requirement**  
- **Impact**: Cannot use provider-specific optimizations or extensions
- **Mitigation**: Focus on MCP standard compliance, thorough testing with multiple clients
- **Risk**: Low - MCP standard is well-defined

**File-Based Only Constraint**
- **Impact**: No advanced querying, relationships, or transactions
- **Mitigation**: Design for simple file operations, implement efficient caching
- **Risk**: Low - Fits use case well

### High Impact Organizational Constraints

**Phase-by-Phase Development**
- **Impact**: Cannot proceed without explicit user approval, may slow development
- **Mitigation**: Prepare comprehensive documentation for each phase review
- **Risk**: Low - User engagement ensures quality

**Arc42 Documentation Standard**
- **Impact**: Significant documentation effort before implementation
- **Mitigation**: Use LEAN variant, focus on essential architectural decisions
- **Risk**: Low - Improves code quality and maintainability

### Negotiable Constraints

The following constraints may be challenged with proper justification:
- **xUnit Testing Framework** → Could use NUnit if team preference
- **Solo Development** → Open to community contributors
- **Some Code Conventions** → With architectural board approval

### Non-Negotiable Constraints

These constraints are fixed and must be accepted:
- **C# 13 and .NET 8+** → Repository standard
- **ModelContextProtocol SDK** → Protocol requirement  
- **Provider-Agnostic Design** → Business requirement
- **Security Standards** → Legal/safety requirement
- **Phase-by-Phase Process** → User requirement