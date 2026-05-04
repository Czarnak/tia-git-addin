using System.Collections.Generic;
using TiaGitAddIn.Models;
using Xunit;

namespace TiaGitAddIn.Tests.Models
{
    public sealed class GitStatusTests
    {
        [Fact]
        public void HasConflictsReturnsTrueWhenAnyEntryIsConflicted()
        {
            GitStatus status = new GitStatus
            {
                Entries = new List<FileStatusEntry>
                {
                    new FileStatusEntry
                    {
                        FilePath = "Block.scl",
                        IndexStatus = FileStatus.Conflicted,
                        WorkTreeStatus = FileStatus.Modified
                    }
                }
            };

            Assert.True(status.HasConflicts);
        }

        [Fact]
        public void HasConflictsReturnsFalseForCleanStatus()
        {
            GitStatus status = new GitStatus
            {
                Entries = new List<FileStatusEntry>
                {
                    new FileStatusEntry
                    {
                        FilePath = "Block.scl",
                        IndexStatus = FileStatus.Unmodified,
                        WorkTreeStatus = FileStatus.Modified
                    }
                }
            };

            Assert.False(status.HasConflicts);
        }
    }
}
