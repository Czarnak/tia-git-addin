namespace TiaGitAddIn.Models.Comparison
{
    public enum PlcArtifactKind { Unknown, Lad, Fbd, Scl, Stl, Sfc, GenericXml, Text, Binary }

    public enum PlcComparisonMode { Visual, Structured, Text, Unsupported }

    public enum PlcSupportLevel { Full, Partial, Fallback, Unsupported }

    public enum ComparisonPresentationKind { Interface, LogicNetwork, Scl, Text, Unsupported, Error }

    public enum PlcDiagnosticSeverity { Info, Warning, Error }

    public enum PlcRevisionSide { Left, Right }

    public enum PlcPairChangeKind { Modified, Added, Removed }

    public enum PlcRevisionSourceKind { WorkingTree, Head, Commit, ParentOfCommit }

    public enum PlcRevisionMissingReason { None, Added, Deleted, NotPresentInRevision }

    public enum PlcTextEncodingKind { None, Utf8, Utf16LittleEndian, Utf16BigEndian }

    public enum TextDiffLineKind { Unchanged, Added, Removed, Omitted }
}
