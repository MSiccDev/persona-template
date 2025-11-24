using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PersonaMcpServer.Services;

/// <summary>
/// Service for loading and managing instruction file templates.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Retrieves the persona template content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full content of the persona template file.</returns>
    Task<string> GetPersonaTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the project template content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full content of the project template file.</returns>
    Task<string> GetProjectTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available template names.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of template names (e.g., "persona_template", "project_template").</returns>
    Task<List<string>> ListAvailableTemplatesAsync(CancellationToken cancellationToken = default);
}
