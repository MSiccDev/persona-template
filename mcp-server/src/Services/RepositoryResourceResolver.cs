using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace PersonaMcpServer.Services;

internal static class RepositoryResourceResolver
{
    public static string Resolve(string repoPath, string folderName, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(repoPath))
        {
            throw new ArgumentException("repoPath must be a non-empty string", nameof(repoPath));
        }

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(repoPath, folderName)),
            Path.GetFullPath(Path.Combine(repoPath, "..", folderName))
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                logger.LogDebug("Resolved {FolderName} directory at {Path}", folderName, candidate);
                return candidate;
            }
        }

        logger.LogWarning(
            "Could not locate {FolderName} directory under {RepoPath}; defaulting to {DefaultPath}",
            folderName,
            repoPath,
            candidates[0]);

        return candidates[0];
    }
}
