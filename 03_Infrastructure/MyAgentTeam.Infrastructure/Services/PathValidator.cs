using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyAgentTeam.Infrastructure.Services
{
    public static class PathValidator
    {
        /// <summary>
        /// Validates if the given path is safe to use as a project directory.
        /// Blocks root directories, system directories, and other sensitive locations.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the path is considered safe; otherwise, false.</returns>
        public static bool IsSafePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                // Resolve full path to handle relative paths and separators
                string fullPath = Path.GetFullPath(path);

                // 1. Block root directories (e.g., C:\, /)
                string? root = Path.GetPathRoot(fullPath);
                // Normalize by trimming trailing separator for comparison
                string normalizedFullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedRoot = root?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? "";

                if (string.Equals(normalizedFullPath, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 2. Define forbidden system directories
                var forbiddenPaths = new List<string>();

                if (OperatingSystem.IsWindows())
                {
                    forbiddenPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.Windows));       // C:\Windows
                    forbiddenPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));        // C:\Windows\System32
                    forbiddenPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));  // C:\Program Files
                    forbiddenPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)); // C:\Program Files (x86)
                    // CommonApplicationData is usually C:\ProgramData
                    forbiddenPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
                }
                else
                {
                    // Common Linux/Unix sensitive directories
                    forbiddenPaths.AddRange(new[]
                    {
                        "/bin", "/boot", "/dev", "/etc", "/lib", "/lib64",
                        "/proc", "/root", "/run", "/sbin", "/sys", "/usr", "/var"
                    });
                }

                // 3. Check if the path is within any forbidden directory
                foreach (var forbidden in forbiddenPaths)
                {
                    if (string.IsNullOrEmpty(forbidden)) continue;

                    string forbiddenFull = Path.GetFullPath(forbidden);
                    string normalizedForbidden = forbiddenFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    // Use appropriate comparison based on OS
                    StringComparison comparison = OperatingSystem.IsWindows()
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal;

                    // Check exact match
                    if (string.Equals(normalizedFullPath, normalizedForbidden, comparison))
                    {
                        return false;
                    }

                    // Check if it is a subdirectory
                    if (normalizedFullPath.StartsWith(normalizedForbidden + Path.DirectorySeparatorChar, comparison))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                // If path is invalid or throws exception during processing, consider it unsafe
                return false;
            }
        }
    }
}
