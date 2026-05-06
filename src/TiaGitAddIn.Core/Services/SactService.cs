using System;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services.SimaticMl;

namespace TiaGitAddIn.Services
{
    public sealed class SactService(IAddInLogger? logger = null) : ISactService
    {
        public bool IsAvailable => true;

        public async Task<SactCompareResult?> CompareAsync(string? leftXmlPath, string? rightXmlPath, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger?.Info($"SactService: Parsing SimaticML files.\n  Left: {leftXmlPath ?? "null"}\n  Right: {rightXmlPath ?? "null"}");

                    var leftFile = leftXmlPath != null ? SimaticMlParser.Parse(leftXmlPath) : null;
                    var rightFile = rightXmlPath != null ? SimaticMlParser.Parse(rightXmlPath) : null;

                    logger?.Info("SactService: Comparing models.");
                    var result = SimaticMlComparer.Compare(leftFile, rightFile);

                    int networkCount = result.Content?.Networks?.Count ?? 0;
                    logger?.Info($"SactService: Comparison completed. Networks found: {networkCount}");

                    return result;
                }
                catch (Exception ex)
                {
                    logger?.Error("SactService: Failed to parse or compare SimaticML files.", ex);
                    return null;
                }
            }, ct).ConfigureAwait(false);
        }
    }
}