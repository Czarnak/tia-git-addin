using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.SimaticMl;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    /// <summary>
    /// Locks the normalization matrix (<see cref="InterfaceSnapshotBuilder"/>) and the recursive matching
    /// algorithm (<see cref="InterfaceComparer"/>) declared in the Task 8 brief, plus the defensive cloning
    /// guarantee of <see cref="SactCompareResultCloner"/> that <see cref="LadPresentation"/> depends on.
    /// </summary>
    public class InterfaceComparerTests
    {
        // ------------------------------------------------------------------------------------------------
        // Step 1: normalization matrix theory (brief-provided cases plus additional field coverage).
        // ------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(InterfaceFieldKind.Datatype, "  Array[1..2] of Int  ", "Array[1..2] of Int", false)]
        [InlineData(InterfaceFieldKind.Datatype, "Array [1..2] of Int", "Array[1..2] of Int", true)]
        [InlineData(InterfaceFieldKind.StartValue, "  A\r\nB  ", "A\nB", false)]
        [InlineData(InterfaceFieldKind.StartValue, "A  B", "A B", true)]
        [InlineData(InterfaceFieldKind.DefaultValue, "  A\r\nB  ", "A\nB", false)]
        [InlineData(InterfaceFieldKind.DefaultValue, "X  Y", "X Y", true)]
        [InlineData(InterfaceFieldKind.Version, " 1.2 ", "1.2", false)]
        [InlineData(InterfaceFieldKind.Accessibility, " public ", "Public", false)]
        [InlineData(InterfaceFieldKind.Accessibility, "VendorA", "vendora", true)]
        [InlineData(InterfaceFieldKind.Accessibility, " readonly ", "ReadOnly", false)]
        [InlineData(InterfaceFieldKind.Retain, "true", "Retain", false)]
        [InlineData(InterfaceFieldKind.Retain, "false", "NonRetain", false)]
        [InlineData(InterfaceFieldKind.Retain, "", "true", true)]
        [InlineData(InterfaceFieldKind.Retain, "non-retain", "Retain", true)]
        public void FieldNormalizationMatchesDeclaredMatrix(
            InterfaceFieldKind field, string left, string right, bool expectedChange)
        {
            InterfaceSnapshot leftSnapshot = InterfaceFixture.OneMember(field, left);
            InterfaceSnapshot rightSnapshot = InterfaceFixture.OneMember(field, right);
            InterfaceMemberComparison member = SingleMember(new InterfaceComparer().Compare(leftSnapshot, rightSnapshot));
            Assert.Equal(expectedChange, member.FieldChanges.Any(change => change.Field == field));
            Assert.NotNull(member.Left);
            Assert.NotNull(member.Right);
        }

        [Theory]
        [InlineData(SemanticBoolean.Unspecified, SemanticBoolean.False)]
        [InlineData(SemanticBoolean.Unspecified, SemanticBoolean.True)]
        [InlineData(SemanticBoolean.False, SemanticBoolean.True)]
        public void RetainAndInformativeUseThreeDistinctStates(SemanticBoolean left, SemanticBoolean right)
        {
            InterfaceMemberComparison retain = CompareOne(Member("Coil", retain: left), Member("Coil", retain: right));
            InterfaceMemberComparison informative = CompareOne(Member("Coil", informative: left), Member("Coil", informative: right));
            Assert.Single(retain.FieldChanges, c => c.Field == InterfaceFieldKind.Retain);
            Assert.Single(informative.FieldChanges, c => c.Field == InterfaceFieldKind.Informative);
        }

        [Fact]
        public void CommentNormalization_TrimsTrailingSpacesAndTabsPerLineButPreservesLeadingAndInternal()
        {
            var member = new InterfaceMember
            {
                Name = "Motor",
                Datatype = "Bool",
                Comments = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    ["en-US"] = "  Line1  \t\r\n  Line2\t\nLine3   "
                })
            };
            var block = OneMemberBlock(member);

            InterfaceSnapshot snapshot = InterfaceSnapshotBuilder.Build(block);
            string comment = snapshot.Sections.Single(s => s.Name == "Input").Members.Single().Comments["en-US"];

            Assert.Equal("  Line1\n  Line2\nLine3", comment);
        }

        [Fact]
        public void SemanticAttributeNormalization_CanonicalizesBooleanFormsAndIgnoresNonWhitelisted()
        {
            var member = new InterfaceMember
            {
                Name = "Motor",
                Datatype = "Bool",
                AttributeList = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>
                {
                    ["ExternalAccessible"] = "1",
                    ["ExternalVisible"] = " FALSE ",
                    ["SetPoint"] = "custom-text",
                    ["UId"] = "12345",
                    ["Offset"] = "4"
                })
            };
            var block = OneMemberBlock(member);

            InterfaceSnapshot snapshot = InterfaceSnapshotBuilder.Build(block);
            IReadOnlyDictionary<string, string> attributes = snapshot.Sections.Single(s => s.Name == "Input").Members.Single().SemanticAttributes;

            Assert.Equal("true", attributes["ExternalAccessible"]);
            Assert.Equal("false", attributes["ExternalVisible"]);
            Assert.Equal("custom-text", attributes["SetPoint"]);
            Assert.False(attributes.ContainsKey("UId"));
            Assert.False(attributes.ContainsKey("Offset"));
            Assert.False(attributes.ContainsKey("ExternalWritable"));
        }

        [Fact]
        public void InformativeNormalization_NullableBooleanMapsToThreeStates()
        {
            Assert.Equal(SemanticBoolean.Unspecified, BuildInformative(null));
            Assert.Equal(SemanticBoolean.True, BuildInformative(true));
            Assert.Equal(SemanticBoolean.False, BuildInformative(false));

            static SemanticBoolean BuildInformative(bool? raw)
            {
                var member = new InterfaceMember { Name = "Motor", Datatype = "Bool", Informative = raw };
                InterfaceSnapshot snapshot = InterfaceSnapshotBuilder.Build(OneMemberBlock(member));
                return snapshot.Sections.Single(s => s.Name == "Input").Members.Single().Informative;
            }
        }

        [Fact]
        public void CanonicalSectionsIncludeAllSevenNamesInOrderEvenWhenSourceDeclaresFewer()
        {
            var block = new BlockDefinition
            {
                InterfaceSections = new[]
                {
                    new InterfaceSection { Name = "input", Members = Array.Empty<InterfaceMember>() },
                    new InterfaceSection { Name = "RETURN", Members = Array.Empty<InterfaceMember>() }
                }
            };

            InterfaceSnapshot snapshot = InterfaceSnapshotBuilder.Build(block);

            Assert.Equal(
                new[] { "Input", "Output", "InOut", "Static", "Temp", "Constant", "Return" },
                snapshot.Sections.Select(s => s.Name).ToArray());
            Assert.True(snapshot.Sections.Single(s => s.Name == "Input").IsPresent);
            Assert.True(snapshot.Sections.Single(s => s.Name == "Return").IsPresent);
            Assert.False(snapshot.Sections.Single(s => s.Name == "Output").IsPresent);
        }

        // ------------------------------------------------------------------------------------------------
        // Step 5: hierarchy, identity, ordering (brief-provided examples plus named scenarios).
        // ------------------------------------------------------------------------------------------------

        [Fact]
        public void OneSidedParentRetainsSubtreeWithoutDuplicateTopLevelChildren()
        {
            InterfaceMemberSnapshot child = Member("Child");
            InterfaceMemberSnapshot parent = Member("Parent", children: new[] { child });
            InterfacePresentation result = new InterfaceComparer().Compare(
                new InterfaceSnapshot(new[] { Section("Input", parent) }), new InterfaceSnapshot(Array.Empty<InterfaceSectionSnapshot>()));
            InterfaceMemberComparison only = Assert.Single(Assert.Single(result.Sections).Members);
            Assert.Equal(InterfaceChangeKind.Removed, only.ChangeKind);
            Assert.Single(only.Children);
            Assert.DoesNotContain(Assert.Single(result.Sections).Members, item => item.Left?.Name == "Child");
        }

        [Fact]
        public void MatchingUsesNfcOrdinalPathAndCaseRemainsDistinct()
        {
            InterfacePresentation canonical = CompareMembers(Member("Café"), Member("Café"));
            Assert.Equal(InterfaceChangeKind.Unchanged, SingleMember(canonical).ChangeKind);
            InterfacePresentation casing = CompareMembers(Member("Motor"), Member("motor"));
            Assert.Contains(casing.Sections.SelectMany(s => s.Members), m => m.ChangeKind == InterfaceChangeKind.Added);
            Assert.Contains(casing.Sections.SelectMany(s => s.Members), m => m.ChangeKind == InterfaceChangeKind.Removed);
        }

        [Fact]
        public void MergeOrderUsesRightDeclarationsThenLeftOnly()
        {
            InterfacePresentation result = CompareSectionNames(
                left: new[] { "A", "B", "D" }, right: new[] { "C", "A" });
            Assert.Equal(new[] { "C", "A", "B", "D" },
                Assert.Single(result.Sections).Members.Select(m => (m.Right ?? m.Left)!.Name));
        }

        [Fact]
        public void CommentFieldChanges_TrackPerLanguageAddRemoveAndChange()
        {
            InterfaceMemberComparison member = CompareOne(
                Member("Motor", comments: new Dictionary<string, string> { ["en-US"] = "Old", ["de-DE"] = "Stays" }),
                Member("Motor", comments: new Dictionary<string, string> { ["en-US"] = "New", ["fr-FR"] = "Nouveau" }));

            Assert.Contains(member.FieldChanges, c => c.Field == InterfaceFieldKind.Comment && c.Key == "en-US" && c.LeftValue == "Old" && c.RightValue == "New");
            Assert.Contains(member.FieldChanges, c => c.Field == InterfaceFieldKind.Comment && c.Key == "de-DE" && c.LeftValue == "Stays" && c.RightValue == null);
            Assert.Contains(member.FieldChanges, c => c.Field == InterfaceFieldKind.Comment && c.Key == "fr-FR" && c.LeftValue == null && c.RightValue == "Nouveau");
            Assert.Equal(InterfaceChangeKind.Modified, member.ChangeKind);
        }

        [Fact]
        public void EmptySectionPresentInOneRevisionOnly_ReportsSectionAddedOrRemoved()
        {
            var leftBlock = new BlockDefinition
            {
                InterfaceSections = new[] { new InterfaceSection { Name = "Constant", Members = Array.Empty<InterfaceMember>() } }
            };
            var rightBlock = new BlockDefinition { InterfaceSections = Array.Empty<InterfaceSection>() };

            InterfacePresentation presentation = new InterfaceComparer().Compare(
                InterfaceSnapshotBuilder.Build(leftBlock), InterfaceSnapshotBuilder.Build(rightBlock));

            InterfaceSectionComparison constant = presentation.Sections.Single(s => s.Left?.Name == "Constant" || s.Right?.Name == "Constant");
            Assert.Equal(InterfaceChangeKind.Removed, constant.ChangeKind);
            Assert.Empty(constant.Members);
        }

        [Fact]
        public void MemberMovedBetweenSections_ReportsIndependentRemovedAndAdded()
        {
            var leftBlock = new BlockDefinition
            {
                InterfaceSections = new[]
                {
                    new InterfaceSection { Name = "Static", Members = new[] { new InterfaceMember { Name = "X", Datatype = "Bool" } } }
                }
            };
            var rightBlock = new BlockDefinition
            {
                InterfaceSections = new[]
                {
                    new InterfaceSection { Name = "Temp", Members = new[] { new InterfaceMember { Name = "X", Datatype = "Bool" } } }
                }
            };

            InterfacePresentation presentation = new InterfaceComparer().Compare(
                InterfaceSnapshotBuilder.Build(leftBlock), InterfaceSnapshotBuilder.Build(rightBlock));

            InterfaceMemberComparison removedFromStatic = presentation.Sections.Single(s => s.Name == "Static").Members.Single();
            InterfaceMemberComparison addedToTemp = presentation.Sections.Single(s => s.Name == "Temp").Members.Single();
            Assert.Equal(InterfaceChangeKind.Removed, removedFromStatic.ChangeKind);
            Assert.Equal(InterfaceChangeKind.Added, addedToTemp.ChangeKind);
        }

        [Fact]
        public void VolatileMetadata_DoesNotProduceFieldChanges()
        {
            var left = new InterfaceMember
            {
                Name = "Motor",
                Datatype = "Bool",
                RawAttributes = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?> { ["UId"] = "1" }),
                AttributeList = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?> { ["ExportOrder"] = "1", ["Timestamp"] = "2020-01-01" })
            };
            var right = new InterfaceMember
            {
                Name = "Motor",
                Datatype = "Bool",
                RawAttributes = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?> { ["UId"] = "99" }),
                AttributeList = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?> { ["ExportOrder"] = "9", ["Timestamp"] = "2026-07-17" })
            };

            InterfacePresentation presentation = new InterfaceComparer().Compare(
                InterfaceSnapshotBuilder.Build(OneMemberBlock(left)), InterfaceSnapshotBuilder.Build(OneMemberBlock(right)));

            Assert.False(presentation.HasChanges);
        }

        [Fact]
        public void CompareDoesNotMutateInputSnapshotsAndPreservesReferenceIdentity()
        {
            InterfaceMemberSnapshot leftMember = Member("Motor", datatype: "Bool");
            InterfaceMemberSnapshot rightMember = Member("Motor", datatype: "Word");
            var leftSnapshot = new InterfaceSnapshot(new[] { Section("Input", leftMember) });
            var rightSnapshot = new InterfaceSnapshot(new[] { Section("Input", rightMember) });

            InterfacePresentation forward = new InterfaceComparer().Compare(leftSnapshot, rightSnapshot);
            InterfaceMemberComparison forwardMatch = SingleMember(forward);

            // The comparer hands back the exact snapshot instances it was given -- no re-copy, no mutation.
            Assert.Same(leftMember, forwardMatch.Left);
            Assert.Same(rightMember, forwardMatch.Right);
            Assert.Equal("Bool", leftSnapshot.Sections.Single().Members.Single().Datatype);
            Assert.Equal("Word", rightSnapshot.Sections.Single().Members.Single().Datatype);

            // Swapping the arguments produces an independent, correctly mirrored result: comparing the same
            // two snapshots twice (once each direction) must not leak state between the two result graphs.
            InterfacePresentation reverse = new InterfaceComparer().Compare(rightSnapshot, leftSnapshot);
            InterfaceMemberComparison reverseMatch = SingleMember(reverse);

            Assert.Same(rightMember, reverseMatch.Left);
            Assert.Same(leftMember, reverseMatch.Right);
            Assert.NotSame(forward, reverse);
            Assert.NotSame(forwardMatch.FieldChanges, reverseMatch.FieldChanges);
            Assert.Equal("Bool", leftSnapshot.Sections.Single().Members.Single().Datatype);
            Assert.Equal("Word", rightSnapshot.Sections.Single().Members.Single().Datatype);
        }

        [Fact]
        public void PureMemberReorderWithoutContentChange_AllMembersRemainUnchanged()
        {
            InterfacePresentation result = CompareSectionNames(left: new[] { "A", "B", "C" }, right: new[] { "C", "B", "A" });

            IReadOnlyList<InterfaceMemberComparison> members = Assert.Single(result.Sections).Members;
            Assert.Equal(new[] { "C", "B", "A" }, members.Select(m => m.Right!.Name));
            Assert.All(members, m => Assert.Equal(InterfaceChangeKind.Unchanged, m.ChangeKind));
            Assert.False(result.HasChanges);
        }

        [Fact]
        public void ByteDifferentButSemanticallyEqualInterfaces_ProduceNoChanges()
        {
            var leftBlock = new BlockDefinition
            {
                InterfaceSections = new[]
                {
                    new InterfaceSection
                    {
                        Name = "input",
                        Members = new[]
                        {
                            new InterfaceMember
                            {
                                Name = "Motor",
                                Datatype = "  Bool  ",
                                StartValue = "TRUE\r\n",
                                Accessibility = " public ",
                                Remanence = "retain"
                            }
                        }
                    }
                }
            };
            var rightBlock = new BlockDefinition
            {
                InterfaceSections = new[]
                {
                    new InterfaceSection
                    {
                        Name = "Input",
                        Members = new[]
                        {
                            new InterfaceMember
                            {
                                Name = "Motor",
                                Datatype = "Bool",
                                StartValue = "TRUE",
                                Accessibility = "Public",
                                Remanence = "true"
                            }
                        }
                    }
                }
            };

            InterfacePresentation presentation = new InterfaceComparer().Compare(
                InterfaceSnapshotBuilder.Build(leftBlock), InterfaceSnapshotBuilder.Build(rightBlock));

            Assert.False(presentation.HasChanges);
        }

        // ------------------------------------------------------------------------------------------------
        // Step 7: SactCompareResultCloner / LadPresentation defensive-copy guarantee.
        // ------------------------------------------------------------------------------------------------

        [Fact]
        public void CreateLegacyResult_MutatingReturnedCopy_DoesNotAffectPresentationOrLaterCopies()
        {
            SactCompareResult legacy = CreateLegacyResult();
            var interfacePresentation = new InterfacePresentation(Array.Empty<InterfaceSectionComparison>());
            var presentation = new LadPresentation(legacy, interfacePresentation);

            // Mutating the caller's original object after construction must not reach the presentation.
            legacy.Left = "MUTATED-BEFORE-CLONE-CHECK";
            legacy.Interface!.Members[0].Name = "MUTATED-BEFORE-CLONE-CHECK";

            SactCompareResult firstCopy = presentation.CreateLegacyResult();
            Assert.NotEqual("MUTATED-BEFORE-CLONE-CHECK", firstCopy.Left);
            Assert.NotEqual("MUTATED-BEFORE-CLONE-CHECK", firstCopy.Interface!.Members[0].Name);

            // Mutate two values on the returned copy itself.
            firstCopy.Left = "MUTATED-TOP-LEVEL";
            firstCopy.Interface!.Members[0].Name = "MUTATED-NESTED";
            firstCopy.Content!.Networks["0"].Body["1"].Name = "MUTATED-COMPONENT";

            SactCompareResult secondCopy = presentation.CreateLegacyResult();
            Assert.NotEqual("MUTATED-TOP-LEVEL", secondCopy.Left);
            Assert.NotEqual("MUTATED-NESTED", secondCopy.Interface!.Members[0].Name);
            Assert.NotEqual("MUTATED-COMPONENT", secondCopy.Content!.Networks["0"].Body["1"].Name);
            Assert.Equal("Original", secondCopy.Left);
        }

        [Fact]
        public void Clone_NullArgument_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SactCompareResultCloner.Clone(null!));
        }

        private static SactCompareResult CreateLegacyResult()
        {
            return new SactCompareResult
            {
                Left = "Original",
                Right = "Original",
                State = CompareState.Changed,
                Attributes = new Dictionary<string, object> { ["Note"] = "keep", ["Count"] = 3 },
                Interface = new SactInterfaceResult
                {
                    State = CompareState.Changed,
                    Sections = new Dictionary<string, object> { ["Input"] = new List<Dictionary<string, object>>() },
                    Members = new List<SactInterfaceMemberComparison>
                    {
                        new SactInterfaceMemberComparison
                        {
                            Section = "Input", Name = "Motor",
                            LeftDatatype = "Bool", RightDatatype = "Bool",
                            LeftStartValue = "", RightStartValue = "",
                            State = CompareState.Equal
                        }
                    }
                },
                Content = new SactContentResult
                {
                    State = CompareState.Changed,
                    Networks = new Dictionary<string, SactNetworkResult>
                    {
                        ["0"] = new SactNetworkResult
                        {
                            State = CompareState.Changed,
                            Title = "Network 1",
                            Number = new SactNumberPair { Left = 1, Right = 1 },
                            Body = new Dictionary<string, SactComponentData>
                            {
                                ["1"] = new SactComponentData
                                {
                                    Name = "Contact",
                                    UId = "1",
                                    State = CompareState.Equal,
                                    InvisiblePins = new List<string> { "eno" },
                                    InputParameters = new List<SactParameterData>
                                    {
                                        new SactParameterData { Name = "in", Section = "Input", Type = "Bool", Operand = "motor" }
                                    },
                                    OutputConnectors = new List<SactConnectorData>
                                    {
                                        new SactConnectorData { UId = "1_out", PinName = "out", PartnerUId = "2_in" }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private static BlockDefinition OneMemberBlock(InterfaceMember member) => new BlockDefinition
        {
            InterfaceSections = new[] { new InterfaceSection { Name = "Input", Members = new[] { member } } }
        };

        private static InterfaceMemberSnapshot Member(
            string name,
            string datatype = "Bool",
            SemanticBoolean retain = SemanticBoolean.Unspecified,
            string? defaultValue = null,
            string? startValue = null,
            IReadOnlyDictionary<string, string>? comments = null,
            string? accessibility = null,
            SemanticBoolean informative = SemanticBoolean.Unspecified,
            string? version = null,
            IReadOnlyDictionary<string, string>? semanticAttributes = null,
            IEnumerable<InterfaceMemberSnapshot>? children = null,
            string section = "Input")
        {
            string normalizedName = name.Normalize(System.Text.NormalizationForm.FormC);
            return new InterfaceMemberSnapshot(
                section, normalizedName, normalizedName, datatype, retain, defaultValue, startValue,
                comments ?? new Dictionary<string, string>(), accessibility, informative, version,
                semanticAttributes ?? new Dictionary<string, string>(), children ?? Array.Empty<InterfaceMemberSnapshot>());
        }

        private static InterfaceSectionSnapshot Section(string name, params InterfaceMemberSnapshot[] members)
            => new InterfaceSectionSnapshot(name, true, members);

        private static InterfaceMemberComparison SingleMember(InterfacePresentation presentation)
            => presentation.Sections.SelectMany(s => s.Members).Single();

        private static InterfaceMemberComparison CompareOne(InterfaceMemberSnapshot left, InterfaceMemberSnapshot right)
            => SingleMember(CompareMembers(left, right));

        private static InterfacePresentation CompareMembers(InterfaceMemberSnapshot left, InterfaceMemberSnapshot right)
            => new InterfaceComparer().Compare(
                new InterfaceSnapshot(new[] { Section("Input", left) }),
                new InterfaceSnapshot(new[] { Section("Input", right) }));

        private static InterfacePresentation CompareSectionNames(string[] left, string[] right)
            => new InterfaceComparer().Compare(
                new InterfaceSnapshot(new[] { Section("Input", left.Select(n => Member(n)).ToArray()) }),
                new InterfaceSnapshot(new[] { Section("Input", right.Select(n => Member(n)).ToArray()) }));

        private static class InterfaceFixture
        {
            public static InterfaceSnapshot OneMember(InterfaceFieldKind field, string rawValue)
            {
                var member = new InterfaceMember { Name = "Member", Datatype = "Bool" };
                switch (field)
                {
                    case InterfaceFieldKind.Datatype: member.Datatype = rawValue; break;
                    case InterfaceFieldKind.StartValue: member.StartValue = rawValue; break;
                    case InterfaceFieldKind.DefaultValue: member.DefaultValue = rawValue; break;
                    case InterfaceFieldKind.Version: member.Version = rawValue; break;
                    case InterfaceFieldKind.Accessibility: member.Accessibility = rawValue; break;
                    case InterfaceFieldKind.Retain: member.Remanence = rawValue; break;
                    default: throw new ArgumentOutOfRangeException(nameof(field), field, "Unsupported field for this fixture.");
                }

                return InterfaceSnapshotBuilder.Build(OneMemberBlock(member));
            }
        }
    }
}
