using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services
{
    public sealed class SactService : ISactService
    {
        private readonly ISactPathResolver pathResolver;
        private readonly ISactProcessRunner processRunner;
        private readonly IAddInLogger? logger;

        public SactService(ISactPathResolver pathResolver, ISactProcessRunner processRunner, IAddInLogger? logger = null)
        {
            this.pathResolver = pathResolver;
            this.processRunner = processRunner;
            this.logger = logger;
        }

        public bool IsAvailable => pathResolver.ResolveSiemensInstallPath() != null && pathResolver.ResolveNodePath() != null;

        public async Task<SactCompareResult?> CompareAsync(string leftXmlPath, string rightXmlPath, CancellationToken ct)
        {
            var siemensPath = pathResolver.ResolveSiemensInstallPath();
            var nodePath = pathResolver.ResolveNodePath();

            if (siemensPath == null || nodePath == null)
            {
                logger?.Info($"SACT: Missing prerequisites. SiemensPath: {siemensPath ?? "null"}, NodePath: {nodePath ?? "null"}");
                return null;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var scriptPath = Path.Combine(baseDir, "Scripts", "CompareBlocks.js");
            if (!File.Exists(scriptPath))
            {
                // Fallback for some test runners
                scriptPath = Path.Combine(Path.GetDirectoryName(typeof(SactService).Assembly.Location) ?? baseDir, "Scripts", "CompareBlocks.js");
            }

            if (!File.Exists(scriptPath))
            {
                logger?.Info($"SACT: Script not found at {scriptPath}");
                return null;
            }

            // node_modules path: <SiemensDir>\ACT-CLI\resources\app\node_modules
            var nodeModulesPath = Path.Combine(siemensPath, "ACT-CLI", "resources", "app", "node_modules");
            var environment = new Dictionary<string, string>
            {
                { "NODE_PATH", nodeModulesPath }
            };

            var arguments = $"\"{scriptPath}\" \"{leftXmlPath}\" \"{rightXmlPath}\"";

            logger?.Info($"SACT: Starting comparison via Node.js bridge.\n  Node: {nodePath}\n  Script: {scriptPath}\n  Left: {leftXmlPath}\n  Right: {rightXmlPath}\n  NODE_PATH: {nodeModulesPath}");
            
            var result = await processRunner.RunAsync(nodePath, arguments, ct, environment).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                logger?.Info($"SACT: Process failed. Exit code: {result.ExitCode}, TimedOut: {result.TimedOut}");
                if (!string.IsNullOrWhiteSpace(result.StandardError))
                {
                    logger?.Info($"SACT STDERR:\n{result.StandardError}");
                }
                return null;
            }

            if (string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                logger?.Info("SACT: Process succeeded but stdout is empty.");
                return null;
            }

            try
            {
                var parsedResult = SactJsonParser.ParseCompareResult(result.StandardOutput);
                if (parsedResult == null)
                {
                    logger?.Info($"SACT: SactJsonParser returned null. Raw JSON length: {result.StandardOutput.Length}");
                }
                return parsedResult;
            }
            catch (Exception ex)
            {
                logger?.Error("SACT: Exception thrown during JSON parsing.", ex);
                return null;
            }
        }
    }
}