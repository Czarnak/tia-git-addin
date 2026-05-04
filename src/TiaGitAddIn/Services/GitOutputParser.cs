using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TiaGitAddIn.Models;

namespace TiaGitAddIn.Services
{
    public static class GitOutputParser
    {
        private const char FieldSeparator = '\u001f';

        public static GitStatus ParseStatus(string output)
        {
            string[] lines = SplitLines(output);
            GitStatus status = new GitStatus
            {
                Entries = lines
                    .Where(line => !line.StartsWith("##", StringComparison.Ordinal))
                    .Where(line => line.Length >= 3)
                    .Select(ParseStatusEntry)
                    .ToList()
            };

            string? branchLine = lines.FirstOrDefault(line => line.StartsWith("##", StringComparison.Ordinal));
            if (branchLine != null)
            {
                ApplyBranchStatus(status, branchLine);
            }

            return status;
        }

        public static IEnumerable<CommitInfo> ParseCommitLog(string output)
        {
            foreach (string line in SplitLines(output))
            {
                string[] fields = line.Split(FieldSeparator);
                if (fields.Length < 4)
                {
                    continue;
                }

                DateTimeOffset.TryParse(
                    fields[2],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTimeOffset authorDate);

                yield return new CommitInfo
                {
                    Hash = fields[0],
                    AuthorName = fields[1],
                    AuthorDate = authorDate,
                    Subject = fields[3],
                    ParentHash = fields.Length > 4 ? fields[4] : null
                };
            }
        }

        public static IEnumerable<BranchInfo> ParseBranches(string output)
        {
            foreach (string line in SplitLines(output))
            {
                string[] fields = line.Split(FieldSeparator);
                if (fields.Length < 2)
                {
                    continue;
                }

                yield return new BranchInfo
                {
                    Name = fields[1],
                    IsCurrent = fields[0] == "*",
                    TrackingBranch = fields.Length > 2 && fields[2].Length > 0 ? fields[2] : null
                };
            }
        }

        public static IEnumerable<RemoteInfo> ParseRemotes(string output)
        {
            foreach (string line in SplitLines(output))
            {
                int separatorIndex = line.IndexOf('\t');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                string name = line.Substring(0, separatorIndex);
                string remoteWithPurpose = line.Substring(separatorIndex + 1);
                int purposeIndex = remoteWithPurpose.LastIndexOf(" (", StringComparison.Ordinal);
                if (purposeIndex <= 0 || !remoteWithPurpose.EndsWith(")", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return new RemoteInfo
                {
                    Name = name,
                    Url = remoteWithPurpose.Substring(0, purposeIndex),
                    Purpose = remoteWithPurpose.Substring(
                        purposeIndex + 2,
                        remoteWithPurpose.Length - purposeIndex - 3)
                };
            }
        }

        private static FileStatusEntry ParseStatusEntry(string line)
        {
            char index = line[0];
            char workTree = line[1];
            string path = line.Substring(3);
            string? originalPath = null;

            string[] renameParts = path.Split(new[] { " -> " }, StringSplitOptions.None);
            if (renameParts.Length == 2)
            {
                originalPath = renameParts[0];
                path = renameParts[1];
            }

            return new FileStatusEntry
            {
                FilePath = path,
                OriginalPath = originalPath,
                IndexStatus = MapStatus(index, workTree),
                WorkTreeStatus = MapStatus(workTree, index)
            };
        }

        private static FileStatus MapStatus(char status, char pairedStatus)
        {
            if (status == 'U' || IsConflictPair(status, pairedStatus))
            {
                return FileStatus.Conflicted;
            }

            switch (status)
            {
                case ' ':
                    return FileStatus.Unmodified;
                case 'M':
                    return FileStatus.Modified;
                case 'A':
                    return FileStatus.Added;
                case 'D':
                    return FileStatus.Deleted;
                case 'R':
                    return FileStatus.Renamed;
                case 'C':
                    return FileStatus.Copied;
                case '?':
                    return FileStatus.Untracked;
                case '!':
                    return FileStatus.Ignored;
                default:
                    return FileStatus.Unmodified;
            }
        }

        private static bool IsConflictPair(char status, char pairedStatus) =>
            (status == 'A' && pairedStatus == 'A') ||
            (status == 'D' && pairedStatus == 'D');

        private static void ApplyBranchStatus(GitStatus status, string line)
        {
            string value = line.Substring(3);
            int trackingIndex = value.IndexOf("...", StringComparison.Ordinal);
            if (trackingIndex < 0)
            {
                status.CurrentBranch = ParseUntrackedBranchName(value);
                return;
            }

            status.CurrentBranch = value.Substring(0, trackingIndex);
            string tracking = value.Substring(trackingIndex + 3);
            int metadataIndex = tracking.IndexOf(" [", StringComparison.Ordinal);
            if (metadataIndex >= 0)
            {
                string metadata = tracking.Substring(metadataIndex + 2).TrimEnd(']');
                tracking = tracking.Substring(0, metadataIndex);
                ApplyAheadBehind(status, metadata);
            }

            status.TrackingBranch = tracking;
        }

        private static string ParseUntrackedBranchName(string value)
        {
            const string noCommitsPrefix = "No commits yet on ";
            return value.StartsWith(noCommitsPrefix, StringComparison.Ordinal)
                ? value.Substring(noCommitsPrefix.Length)
                : value;
        }

        private static void ApplyAheadBehind(GitStatus status, string metadata)
        {
            foreach (string part in metadata.Split(','))
            {
                string trimmed = part.Trim();
                if (trimmed.StartsWith("ahead ", StringComparison.Ordinal) &&
                    int.TryParse(trimmed.Substring(6), out int ahead))
                {
                    status.AheadBy = ahead;
                }
                else if (trimmed.StartsWith("behind ", StringComparison.Ordinal) &&
                    int.TryParse(trimmed.Substring(7), out int behind))
                {
                    status.BehindBy = behind;
                }
            }
        }

        private static string[] SplitLines(string output) =>
            output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
}
