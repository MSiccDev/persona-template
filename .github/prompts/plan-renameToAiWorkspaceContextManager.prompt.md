# Plan: Rename Project to "AI Workspace Context Manager"

This plan comprehensively renames the project from "persona" to "workspace context" terminology, including all assembly names and packages. The architecture follows: **Workspace = UserContext + ProjectContext**.

## Architecture

- **UserContext**: User's professional identity, skills, and preferences (files: `*_usercontext.instructions.md`)
- **ProjectContext**: Project-specific tech stack, scope, and guidelines (files: `*_project.instructions.md`, unchanged)
- **Workspace**: The combined context for complete AI collaboration

## Scope of Changes

### ✅ RENAME (User Identity References)
- File names: `*_persona.instructions.md` → `*_usercontext.instructions.md`
- Code symbols: `PersonaInstruction`, `PersonaService`, `persona_list`, etc.
- Documentation: "Personal persona instructions" → "Personal user context instructions"
- Templates: User identity template file and content

### ❌ PRESERVE (AI Role/Behavior References)
- AI assistant roles/modes in project templates (Architect, Developer, Designer modes)
- "Role" and "Mode" terminology when referring to AI behavior
- Session specification's use of "Persona" as a session state element (with clarification added)
- Any references to "AI assistant personas" or "recommended roles" for AI behavior

### Renaming Strategy
Files will be renamed directly and git will auto-detect renames based on content similarity.

## Steps

1. **GitHub repository rename** - Rename `MSiccDev/persona-template` to `MSiccDev/ai-workspace-context-manager` via GitHub settings, update remote URLs
2. **Configure git rename detection** - Set `git config diff.renames true` and `git config diff.renameLimit 999999` to ensure reliable rename detection across all commits
3. **Clean build artifacts** - Delete all `obj/` and `bin/` directories in `mcp-server` to prevent assembly confusion after project file renames
4. **Rename root directory** - Rename `mcp-server/` to `workspace-context-server/`, update all relative path references (`../`) in code and configuration
5. **Rename solution file** - Rename `PersonaMcpServer.sln` → `WorkspaceContextMcpServer.sln`, update internal project references to new `.csproj` paths
6. **Rename project files** - Rename `mcp-server.csproj` → `workspace-context-server.csproj` and `mcp-server.tests.csproj` → `workspace-context-server.tests.csproj`, update `<AssemblyName>` to `WorkspaceContextMcpServer`, `<RootNamespace>` to `WorkspaceContextMcpServer`
7. **Rename C# source files** - Rename 8 files: `PersonaServerConfig.cs` → `WorkspaceContextConfig.cs`, `PersonaInstruction.cs` → `UserContextInstruction.cs`, `IPersonaInstructionService.cs` → `IUserContextService.cs`, `PersonaInstructionService.cs` → `UserContextService.cs`, `PersonaMcpTools.cs` → `UserContextTools.cs`, `PersonaResourceHandler.cs` → `UserContextResourceHandler.cs`
8. **Rename test files** - Rename `PersonaInstructionServiceTests.cs` → `UserContextServiceTests.cs`, `PersonaServerConfigTests.cs` → `WorkspaceContextConfigTests.cs`, `PersonaMcpToolsTests.cs` → `UserContextToolsTests.cs`
9. **Rename template files** - Rename `persona_template.instructions.md` → `usercontext_template.instructions.md`, `test_persona.instructions.md` → `test_usercontext.instructions.md`
10. **Rename prompt files** - Rename `create-persona-instructions.prompt.md` → `create-usercontext-instructions.prompt.md`, `validate-persona-instructions.prompt.md` → `validate-usercontext-instructions.prompt.md`
11. **Update all namespaces** - Change `namespace PersonaMcpServer` → `namespace WorkspaceContextMcpServer` in 21 C# files, update all `using PersonaMcpServer.*` statements throughout codebase
12. **Update all classes and interfaces** - Rename `PersonaServerConfig` → `WorkspaceContextConfig`, `PersonaInstruction` → `UserContextInstruction`, `IPersonaInstructionService` → `IUserContextService`, `PersonaInstructionService` → `UserContextService`, `PersonaMcpTools` → `UserContextTools`, `PersonaResourceHandler` → `UserContextResourceHandler`, update all references, constructors, and XML documentation
13. **Update all methods and properties** - Rename `GetPersonaAsync` → `GetUserContextAsync`, `ListAvailablePersonasAsync` → `ListAvailableUserContextsAsync`, `PersonaRepoPath` → `UserContextRepoPath`, `CurrentPersona` → `CurrentUserContext`, `PersonaExistsAsync` → `UserContextExistsAsync`, update all 15+ code symbols and their callers
14. **Update MCP tool registrations** - Rename 9 tools in `UserContextTools.cs`: `persona_list` → `usercontext_list`, `persona_get` → `usercontext_get`, `persona_exists` → `usercontext_exists`, `persona_get_current` → `usercontext_get_current`, `persona_set_current` → `usercontext_set_current`, `persona_invalidate_cache` → `usercontext_invalidate_cache`, `persona_refresh_cache` → `usercontext_refresh_cache`, `persona_create_from_template` → `usercontext_create_from_template`, `persona_validate` → `usercontext_validate`
15. **Update MCP prompts** - Rename `validate_persona_prompt` → `validate_usercontext_prompt` in `ValidationPrompts.cs`, update prompt registration and handler references
16. **Update configuration files** - Change `PersonaServer` → `WorkspaceContext` section in `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`, rename keys (`PersonaRepoPath` → `UserContextRepoPath`, `CurrentPersona` → `CurrentUserContext`), update `Program.cs` configuration binding and section references
17. **Update environment variables** - Change prefix `PERSONA_` → `WORKSPACE_CONTEXT_` in `Dockerfile`, `docker-compose.yml`, update all variable names (`WORKSPACE_CONTEXT_WorkspaceContext__Host`, `WORKSPACE_CONTEXT_WorkspaceContext__Port`, etc.)
18. **Update Docker infrastructure** - Change image name to `workspace-context-mcp-server`, update service name in docker-compose, change volume mount path to `/data/ai-workspace-context-manager`, update `ENTRYPOINT` and `CMD` to reference new assembly name `WorkspaceContextMcpServer.dll`
19. **Update template content** - Update `usercontext_template.instructions.md` title to "Personal User Context Instructions", change "This file defines your complete AI persona" → "This file defines your complete user context", update file naming examples to `yourname_usercontext.instructions.md`, update frontmatter descriptions. **Preserve**: AI role/mode terminology in project templates (Architect Mode, Developer Mode, etc.)
20. **Update prompt content** - Update `create-usercontext-instructions.prompt.md` and `validate-usercontext-instructions.prompt.md` with new template paths, file patterns (`*_usercontext.instructions.md`), tool names, change "persona instruction file" → "user context instruction file", "Generate personal persona" → "Generate personal user context"
21. **Update main README** - Update `README.md` title to "AI Workspace Context Manager", subtitle to "A structured instruction framework for maintaining consistent, context-aware AI collaboration...", change "Personal persona instructions" → "Personal user context instructions", "yourname_persona.instructions.md" → "yourname_usercontext.instructions.md", update repository structure examples, GitHub URLs to `MSiccDev/ai-workspace-context-manager`, clarify Workspace = UserContext + ProjectContext architecture. **Preserve**: References to "AI assistant roles" when discussing AI behavior
22. **Update server README** - Update `workspace-context-server/README.md` title to "WorkspaceContextMcpServer", update description to "provides user context and project instruction file management", Docker examples with new image name, configuration examples, environment variables, tool names (`usercontext_list`, etc.), file patterns (`*_usercontext.instructions.md`), change "persona instruction files" → "user context instruction files"
23. **Update architecture documentation** - Update all 6 docs in `workspace-context-server/docs/` with new system name "WorkspaceContextMcpServer", update component names (UserContextService, UserContextTools), class/interface references, configuration keys (UserContextRepoPath), file patterns (`*_usercontext.instructions.md`), quality goals, replace all "persona" terminology with "user context"
24. **Update requirements** - Update `requirements.md` with new resource URIs (`usercontext://current`), tool names (`usercontext_list`, etc.), configuration section name (`WorkspaceContext`), configuration keys (UserContextRepoPath, CurrentUserContext), environment variable prefix, file patterns (`*_usercontext.instructions.md`), replace all 100+ "persona" references with "user context"
25. **Update tasks** - Update `tasks.md` with new component names, file references, task descriptions
26. **Update specification** - Update `context_aware_ai_session_spec.md` repository name/URLs to `MSiccDev/ai-workspace-context-manager`, add terminology clarification section distinguishing "user context instructions" (user identity files) from "Persona" session state element (which represents loaded user preferences), update file path examples from `yourname_persona.instructions.md` → `yourname_usercontext.instructions.md`. **Preserve**: "Persona" as session state element name with added clarification
27. **Update all test code** - Update 30+ test method names, class names, mock objects (`_mockPersonaService` → `_mockUserContextService`), test file paths (`*_usercontext.instructions.md`), assertion messages, XML docs in all test files
28. **Update all log messages** - Update all logger statements from "persona" → "user context" in services, tools, handlers, Program.cs, update startup message to "WorkspaceContextMcpServer starting...", update structured logging properties (e.g., "UserContext", "UserContextPath")
29. **Build and test validation** - Run `dotnet restore`, `dotnet build`, `dotnet test` to verify compilation and all tests pass with new naming
30. **Docker validation** - Run `docker build -t workspace-context-mcp-server .` and `docker-compose up` to verify Docker build and startup work correctly
31. **Git commit with rename detection** - Stage all changes, commit with message mentioning rename scope, verify git detects renames with `git log --follow` on key files
