using System.Threading;
using System.Threading.Tasks;

namespace PersonaMcpServer.Services;

/// <summary>
/// Service for loading validation prompt files.
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Retrieves the persona validation prompt content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full content of the persona validation prompt file including YAML frontmatter.</returns>
    Task<string> GetPersonaValidationPromptAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the project validation prompt content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full content of the project validation prompt file including YAML frontmatter.</returns>
    Task<string> GetProjectValidationPromptAsync(CancellationToken cancellationToken = default);
}
