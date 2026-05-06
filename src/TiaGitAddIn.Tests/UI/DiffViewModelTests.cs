using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models;
using TiaGitAddIn.UI.ViewModels;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public class DiffViewModelTests
    {
        [Theory]
        [InlineData("Project/Program blocks/Main.xml", true)]
        [InlineData("Project/Program blocks/SubFolder/Block.xml", true)]
        [InlineData("Project/Data blocks/GlobalDB.xml", true)]
        [InlineData("Project/LAD/Network1.xml", true)]
        [InlineData("Project/SomeFolder/NotAnArtifact.txt", false)]
        [InlineData("Project/SomeFolder/Generic.xml", true)] // Fixed: all .xml are now detected
        [InlineData("Project/PLC_1/Program blocks/MyBlock.xml", true)]
        public void DetectTiaArtifact_HeuristicCheck(string path, bool expected)
        {
            // Arrange
            var entry = new DiffEntry 
            { 
                FilePath = path,
                Hunks = new List<DiffHunk>()
            };

            // Act
            var vm = new DiffEntryViewModel(entry);

            // Assert
            Assert.Equal(expected, vm.IsTiaArtifact);
        }
    }
}
