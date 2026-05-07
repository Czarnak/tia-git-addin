using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TiaGitAddIn.Services.SimaticMl
{
    public sealed class SimaticMlSchemaLocator : ISimaticMlSchemaLocator
    {
        public const string SchemaDirectoryEnvironmentVariable = "TIA_PORTAL_SCHEMA_DIR";

        private static readonly string[] ExpectedSchemaFileNames =
        [
            "SW.InterfaceSections_v3.xsd",
            "SW.Interface.Snapshot.xsd",
            "SW.PlcBlocks.LADFBD_v3.xsd",
            "SW.PlcBlocks.Access_v3.xsd",
            "SW.Common_v2.xsd"
        ];

        public SimaticMlSchemaLocation Locate(string? explicitSchemaDirectory = null)
        {
            if (!string.IsNullOrWhiteSpace(explicitSchemaDirectory))
            {
                string schemaDirectory = explicitSchemaDirectory!;
                return FromCandidate(schemaDirectory, "explicit");
            }

            string? environmentPath = Environment.GetEnvironmentVariable(SchemaDirectoryEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentPath))
            {
                return FromCandidate(environmentPath, SchemaDirectoryEnvironmentVariable);
            }

            SimaticMlSchemaLocation? installedLocation = LocateInstalledPortalSchemas();
            return installedLocation ?? SimaticMlSchemaLocation.Unavailable(
                "auto",
                "No TIA Portal PublicAPI schema directory was found. Set TIA_PORTAL_SCHEMA_DIR to enable schema validation.");
        }

        private static SimaticMlSchemaLocation FromCandidate(string schemaDirectory, string source)
        {
            if (!Directory.Exists(schemaDirectory))
            {
                return SimaticMlSchemaLocation.Unavailable(source, "Schema directory does not exist.");
            }

            string[] schemaFiles = Directory.GetFiles(schemaDirectory, "*.xsd", SearchOption.TopDirectoryOnly);
            if (schemaFiles.Length == 0)
            {
                return SimaticMlSchemaLocation.Unavailable(source, "Schema directory does not contain .xsd files.");
            }

            string[] missingExpectedSchemas = ExpectedSchemaFileNames
                .Where(fileName => !File.Exists(Path.Combine(schemaDirectory, fileName)))
                .ToArray();

            return SimaticMlSchemaLocation.Available(
                schemaDirectory,
                source,
                InferPortalVersion(schemaDirectory),
                missingExpectedSchemas);
        }

        private static SimaticMlSchemaLocation? LocateInstalledPortalSchemas()
        {
            IEnumerable<string> automationRoots = GetProgramFilesRoots()
                .Select(root => Path.Combine(root, "Siemens", "Automation"))
                .Where(Directory.Exists);

            var candidates = new List<string>();
            foreach (string automationRoot in automationRoots)
            {
                foreach (string portalDirectory in Directory.GetDirectories(automationRoot, "Portal V*", SearchOption.TopDirectoryOnly))
                {
                    string publicApiDirectory = Path.Combine(portalDirectory, "PublicAPI");
                    if (!Directory.Exists(publicApiDirectory))
                    {
                        continue;
                    }

                    candidates.AddRange(
                        Directory.GetDirectories(publicApiDirectory, "V*", SearchOption.TopDirectoryOnly)
                            .Select(apiDirectory => Path.Combine(apiDirectory, "Schemas"))
                            .Where(Directory.Exists));
                }
            }

            return candidates
                .OrderByDescending(InferPortalVersionNumber)
                .Select(candidate => FromCandidate(candidate, "auto"))
                .FirstOrDefault(location => location.IsAvailable);
        }

        private static IEnumerable<string> GetProgramFilesRoots()
        {
            string? programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string? programW6432 = Environment.GetEnvironmentVariable("ProgramW6432");
            string? programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            return new[] { programFiles, programW6432, programFilesX86 }
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string? InferPortalVersion(string schemaDirectory)
        {
            DirectoryInfo? directory = new DirectoryInfo(schemaDirectory);
            while (directory != null)
            {
                if (directory.Name.StartsWith("V", StringComparison.OrdinalIgnoreCase) &&
                    directory.Name.Length > 1 &&
                    directory.Name.Skip(1).All(char.IsDigit))
                {
                    return directory.Name;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static int InferPortalVersionNumber(string schemaDirectory)
        {
            string? version = InferPortalVersion(schemaDirectory);
            return version != null && int.TryParse(version.Substring(1), out int number)
                ? number
                : 0;
        }
    }
}
