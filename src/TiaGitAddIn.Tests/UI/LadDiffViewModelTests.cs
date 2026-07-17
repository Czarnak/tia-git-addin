using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.ViewModels;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public class LadDiffViewModelTests
    {
        private class FakeGitFileExtractor : IGitFileExtractor
        {
            public string? LeftTempPathToReturn { get; set; } = "left.xml";
            public string RightTempPathToReturn { get; set; } = "right.xml";

            public Task<string> ExtractFileAsync(string? commitHash, string filePath, CancellationToken ct)
            {
                if (!string.IsNullOrEmpty(RightTempPathToReturn) && !File.Exists(RightTempPathToReturn))
                {
                    File.WriteAllText(RightTempPathToReturn, "fake right content");
                }
                return Task.FromResult(RightTempPathToReturn);
            }

            public Task<string?> ExtractParentFileAsync(string? commitHash, string filePath, CancellationToken ct)
            {
                if (!string.IsNullOrEmpty(LeftTempPathToReturn) && !File.Exists(LeftTempPathToReturn))
                {
                    File.WriteAllText(LeftTempPathToReturn, "fake left content");
                }
                return Task.FromResult(LeftTempPathToReturn);
            }
        }

        private class FakeSactService : ISactService
        {
            public bool IsAvailable { get; set; } = true;
            public SactCompareResult? ResultToReturn { get; set; }

            public Task<SactCompareResult?> CompareAsync(string? leftXmlPath, string? rightXmlPath, CancellationToken ct)
            {
                return Task.FromResult(ResultToReturn);
            }
        }

        [Fact]
        public async Task LoadLadDiffAsync_WithSuccessfulResult_PopulatesNetworks()
        {
            var extractor = new FakeGitFileExtractor();
            var sactService = new FakeSactService
            {
                ResultToReturn = new SactCompareResult
                {
                    State = CompareState.Changed,
                    Interface = new SactInterfaceResult
                    {
                        State = CompareState.Changed,
                        Members =
                        {
                            new SactInterfaceMemberComparison
                            {
                                Section = "Input",
                                Name = "Alarm",
                                LeftDatatype = "Bool",
                                RightDatatype = "Word",
                                State = CompareState.Changed
                            }
                        }
                    },
                    Content = new SactContentResult
                    {
                        Networks = new Dictionary<string, SactNetworkResult>
                        {
                            { "0", new SactNetworkResult { State = CompareState.Changed, Number = new SactNumberPair { Left = 1, Right = 1 } } }
                        }
                    }
                }
            };
            var dispatcher = ImmediateUiDispatcher.Instance;
            var viewModel = new LadDiffViewModel(extractor, sactService, new TiaGitAddIn.Logging.FileLogger(), dispatcher);

            await viewModel.LoadLadDiffAsync("commit1", "file.xml", CancellationToken.None);

            Assert.True(viewModel.IsLadDiffLoaded);
            Assert.Single(viewModel.Networks);
            Assert.Equal(8, viewModel.InterfaceRows.Count);
            Assert.Equal("Input", viewModel.InterfaceRows[0].DisplayName);
            Assert.Equal("Alarm", viewModel.InterfaceRows[1].DisplayName);
            Assert.Equal(CompareState.Changed, viewModel.Networks[0].DiffState);
            Assert.False(viewModel.IsBusy);
        }

        [Fact]
        public async Task LoadLadDiffAsync_InterfaceRows_KeepTiaSectionOrderWithEmptySections()
        {
            var extractor = new FakeGitFileExtractor();
            var sactService = new FakeSactService
            {
                ResultToReturn = new SactCompareResult
                {
                    State = CompareState.Equal,
                    Right = "deviceState",
                    Interface = new SactInterfaceResult
                    {
                        State = CompareState.Equal,
                        Members =
                        {
                            new SactInterfaceMemberComparison
                            {
                                Section = "Input",
                                Name = "Alarm",
                                RightDatatype = "Bool",
                                State = CompareState.Equal
                            },
                            new SactInterfaceMemberComparison
                            {
                                Section = "Input",
                                Name = "Warning",
                                RightDatatype = "Bool",
                                State = CompareState.Equal
                            },
                            new SactInterfaceMemberComparison
                            {
                                Section = "Input",
                                Name = "Running",
                                RightDatatype = "Bool",
                                State = CompareState.Equal
                            },
                            new SactInterfaceMemberComparison
                            {
                                Section = "InOut",
                                Name = "deviceIcon",
                                RightDatatype = "Int",
                                State = CompareState.Equal
                            },
                            new SactInterfaceMemberComparison
                            {
                                Section = "Return",
                                Name = "Ret_Val",
                                RightDatatype = "Void",
                                State = CompareState.Equal
                            }
                        }
                    },
                    Content = new SactContentResult()
                }
            };
            var viewModel = new LadDiffViewModel(extractor, sactService, new TiaGitAddIn.Logging.FileLogger(), ImmediateUiDispatcher.Instance);

            await viewModel.LoadLadDiffAsync("commit1", "file.xml", CancellationToken.None);

            Assert.Equal("deviceState", viewModel.InterfaceTitle);
            Assert.Equal(
                new[] { "Input", "Output", "InOut", "Static", "Temp", "Constant", "Return" },
                viewModel.InterfaceRows.Where(r => r.IsSectionHeader).Select(r => r.DisplayName).ToArray());
            Assert.Equal("Alarm", viewModel.InterfaceRows[1].DisplayName);
            Assert.Equal("Warning", viewModel.InterfaceRows[2].DisplayName);
            Assert.Equal("Running", viewModel.InterfaceRows[3].DisplayName);
            Assert.Equal("deviceIcon", viewModel.InterfaceRows[6].DisplayName);
            Assert.Equal("Ret_Val", viewModel.InterfaceRows[11].DisplayName);
        }

        [Fact]
        public async Task LoadLadDiffAsync_NullParentInitialCommit_SetsAllNetworksAsAdded()
        {
            var extractor = new FakeGitFileExtractor { LeftTempPathToReturn = null };
            var sactService = new FakeSactService
            {
                ResultToReturn = new SactCompareResult
                {
                    State = CompareState.MissingOnLeft,
                    Content = new SactContentResult
                    {
                        Networks = new Dictionary<string, SactNetworkResult>
                        {
                            { "0", new SactNetworkResult { State = CompareState.MissingOnLeft, Number = new SactNumberPair { Right = 1 } } }
                        }
                    }
                }
            };
            var dispatcher = ImmediateUiDispatcher.Instance;
            var viewModel = new LadDiffViewModel(extractor, sactService, new TiaGitAddIn.Logging.FileLogger(), dispatcher);

            await viewModel.LoadLadDiffAsync("commit1", "file.xml", CancellationToken.None);

            Assert.True(viewModel.IsLadDiffLoaded);
            Assert.Single(viewModel.Networks);
            Assert.Equal(CompareState.MissingOnLeft, viewModel.Networks[0].DiffState);
        }

        [Fact]
        public async Task LoadLadDiffAsync_WithSactFailure_SetsLadDiffError()
        {
            var extractor = new FakeGitFileExtractor();
            var sactService = new FakeSactService { ResultToReturn = null }; // Represents parse failure or timeout
            var dispatcher = ImmediateUiDispatcher.Instance;
            var viewModel = new LadDiffViewModel(extractor, sactService, new TiaGitAddIn.Logging.FileLogger(), dispatcher);

            await viewModel.LoadLadDiffAsync("commit1", "file.xml", CancellationToken.None);

            Assert.False(viewModel.IsLadDiffLoaded);
            Assert.NotEmpty(viewModel.LadDiffError);
            Assert.Empty(viewModel.Networks);
        }

        [Fact]
        public async Task LoadLadDiffAsync_SactNotAvailable_SetsAppropriateError()
        {
            var extractor = new FakeGitFileExtractor();
            var sactService = new FakeSactService { IsAvailable = false };
            var dispatcher = ImmediateUiDispatcher.Instance;
            var viewModel = new LadDiffViewModel(extractor, sactService, new TiaGitAddIn.Logging.FileLogger(), dispatcher);

            await viewModel.LoadLadDiffAsync("commit1", "file.xml", CancellationToken.None);

            Assert.False(viewModel.IsLadDiffLoaded);
            Assert.Contains("SACT not installed", viewModel.LadDiffError);
        }

        [Fact]
        public void Clear_EmptiesNetworksAndResetsErrorState()
        {
            var extractor = new FakeGitFileExtractor();
            var sactService = new FakeSactService();
            var dispatcher = ImmediateUiDispatcher.Instance;
            var viewModel = new LadDiffViewModel(extractor, sactService, new TiaGitAddIn.Logging.FileLogger(), dispatcher);

            // Force some state
            viewModel.LadDiffError = "Some error";
            viewModel.IsLadDiffLoaded = true;
            viewModel.InterfaceRows.Add(LadInterfaceRowViewModel.CreateMember(1, new SactInterfaceMemberComparison { Name = "Alarm" }));

            viewModel.Clear();

            Assert.Empty(viewModel.Networks);
            Assert.Empty(viewModel.InterfaceRows);
            Assert.False(viewModel.IsLadDiffLoaded);
            Assert.Empty(viewModel.LadDiffError);
        }
    }
}
