using System;
using System.IO;
using System.Reflection;

namespace TiaGitAddIn.Services
{
    public sealed class VciWorkspaceLocator : IVciWorkspaceLocator
    {
        public string? TryGetWorkspacePath(object projectContext)
        {
            if (projectContext == null)
            {
                return null;
            }

            foreach (string propertyName in new[] { "WorkspacePath", "Path", "Directory", "Location" })
            {
                object? value = TryReadProperty(projectContext, propertyName);
                string? path = ResolvePath(value);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static object? TryReadProperty(object source, string propertyName)
        {
            try
            {
                PropertyInfo? property = source.GetType().GetProperty(propertyName);
                return property?.GetValue(source, null);
            }
            catch (TargetInvocationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static string? ResolvePath(object? value)
        {
            switch (value)
            {
                case string path:
                    return ResolveStringPath(path);
                case DirectoryInfo directory:
                    return directory.Exists ? directory.FullName : null;
                case FileInfo file:
                    return file.Exists ? file.DirectoryName : null;
                default:
                    return null;
            }
        }

        private static string? ResolveStringPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (Directory.Exists(path))
            {
                return path;
            }

            if (File.Exists(path))
            {
                return Path.GetDirectoryName(path);
            }

            return null;
        }
    }
}
