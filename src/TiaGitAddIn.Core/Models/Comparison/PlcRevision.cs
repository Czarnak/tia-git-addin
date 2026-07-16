using System;
using System.Collections.Generic;
using System.IO;

namespace TiaGitAddIn.Models.Comparison
{
    public sealed class PlcRevisionSource
    {
        private PlcRevisionSource(PlcRevisionSourceKind kind, string? commitHash)
        {
            Kind = kind;
            CommitHash = commitHash;
        }

        public PlcRevisionSourceKind Kind { get; }
        public string? CommitHash { get; }
        public static PlcRevisionSource WorkingTree { get; } = new PlcRevisionSource(PlcRevisionSourceKind.WorkingTree, null);
        public static PlcRevisionSource Head { get; } = new PlcRevisionSource(PlcRevisionSourceKind.Head, "HEAD");
        public static PlcRevisionSource Commit(string hash) => new PlcRevisionSource(PlcRevisionSourceKind.Commit, hash);
        public static PlcRevisionSource ParentOfCommit(string hash) => new PlcRevisionSource(PlcRevisionSourceKind.ParentOfCommit, hash);
    }

    public sealed class PlcTextEncoding
    {
        private PlcTextEncoding(PlcTextEncodingKind kind, bool hasBom) { Kind = kind; HasBom = hasBom; }
        public PlcTextEncodingKind Kind { get; }
        public bool HasBom { get; }
        public static PlcTextEncoding None { get; } = new PlcTextEncoding(PlcTextEncodingKind.None, false);
        public static PlcTextEncoding Utf8WithoutBom { get; } = new PlcTextEncoding(PlcTextEncodingKind.Utf8, false);
        public static PlcTextEncoding Utf8WithBom { get; } = new PlcTextEncoding(PlcTextEncodingKind.Utf8, true);
        public static PlcTextEncoding Utf16LittleEndianWithBom { get; } = new PlcTextEncoding(PlcTextEncodingKind.Utf16LittleEndian, true);
        public static PlcTextEncoding Utf16BigEndianWithBom { get; } = new PlcTextEncoding(PlcTextEncodingKind.Utf16BigEndian, true);
    }

    public sealed class PlcRevision
    {
        private PlcRevision(PlcRevisionSide side, PlcRevisionSource source, string originalPath,
            IReadOnlyList<byte> bytes, PlcTextEncoding encoding, string? text, bool isMissing,
            PlcRevisionMissingReason missingReason, bool isBinary, string encodingLimitation)
        {
            Side = side; Source = source; OriginalPath = originalPath;
            OriginalSuffix = Path.GetExtension(originalPath); Bytes = bytes;
            Encoding = encoding; Text = text; IsMissing = isMissing;
            MissingReason = missingReason; IsBinary = isBinary; EncodingLimitation = encodingLimitation;
        }

        public PlcRevisionSide Side { get; }
        public PlcRevisionSource Source { get; }
        public string OriginalPath { get; }
        public string OriginalSuffix { get; }
        public IReadOnlyList<byte> Bytes { get; }
        public PlcTextEncoding Encoding { get; }
        public string? Text { get; }
        public bool IsMissing { get; }
        public PlcRevisionMissingReason MissingReason { get; }
        public bool IsBinary { get; }
        public string EncodingLimitation { get; }

        public static PlcRevision Present(PlcRevisionSide side, PlcRevisionSource source, string originalPath,
            IEnumerable<byte> bytes, PlcTextEncoding encoding, string? text, bool isBinary, string encodingLimitation)
            => new PlcRevision(side, source ?? throw new ArgumentNullException(nameof(source)), RequirePath(originalPath),
                ImmutableCopy.Of(bytes, nameof(bytes)), encoding ?? throw new ArgumentNullException(nameof(encoding)),
                text, false, PlcRevisionMissingReason.None, isBinary, encodingLimitation ?? string.Empty);

        public static PlcRevision Missing(PlcRevisionSide side, PlcRevisionSource source, string originalPath,
            PlcRevisionMissingReason reason)
            => new PlcRevision(side, source ?? throw new ArgumentNullException(nameof(source)), RequirePath(originalPath),
                Array.Empty<byte>(), PlcTextEncoding.None, null, true, reason, false, string.Empty);

        private static string RequirePath(string path) => string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Original path is required.", nameof(path)) : path;
    }
}
