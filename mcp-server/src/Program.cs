// Program.cs - MCP Server Host Application Entry Point
// Configures and runs the PersonaMcpServer with dependency injection and hosting

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PersonaMcpServer;
using PersonaMcpServer.Server;
using PersonaMcpServer.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr (MCP protocol requirement)
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Load configuration from appsettings.json and environment variables
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "PERSONA_");

// Register configuration with validation
builder.Services.AddOptions<PersonaServerConfig>()
    .Bind(builder.Configuration.GetSection("PersonaServer"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register services
builder.Services.AddSingleton<IPersonaInstructionService, PersonaInstructionService>();
builder.Services.AddSingleton<IProjectInstructionService, ProjectInstructionService>();
builder.Services.AddSingleton<ITemplateService, TemplateService>();

// Register MCP tool classes for dependency injection
builder.Services.AddSingleton<PersonaMcpTools>();
builder.Services.AddSingleton<ProjectMcpTools>();
builder.Services.AddSingleton<TemplateMcpTools>();

// Configure MCP server with stdio transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<PersonaMcpTools>()
    .WithTools<ProjectMcpTools>()
    .WithTools<TemplateMcpTools>();

// Build and run the host
var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("PersonaMcpServer starting...");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

// Validate configuration on startup
var config = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<PersonaServerConfig>>().Value;
logger.LogInformation("Persona repository path: {Path}", config.PersonaRepoPath);
logger.LogInformation("Cache TTL: {Ttl}s", config.CacheTtlSeconds);
logger.LogInformation("Max cache size: {Size} bytes", config.MaxCacheSizeBytes);

if (!string.IsNullOrWhiteSpace(config.CurrentPersona))
{
    logger.LogInformation("Current persona: {Persona}", config.CurrentPersona);
}

if (!string.IsNullOrWhiteSpace(config.CurrentProject))
{
    logger.LogInformation("Current project: {Project}", config.CurrentProject);
}

// Run the host
await host.RunAsync();