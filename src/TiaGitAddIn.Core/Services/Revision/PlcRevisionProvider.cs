using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Revision
{
    /// <summary>Immutable construction options for <see cref="PlcRevisionProvider"/>.</summary>
    public sealed class PlcRevisionProviderOptions
    {
        public PlcRevisionProviderOptions(int maximumBytes, string temporaryRoot)
        {
            if (maximumBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));

            MaximumBytes = maximumBytes;
            TemporaryRoot = string.IsNullOrWhiteSpace(temporaryRoot)
                ? throw new ArgumentException("Temporary root is required.", nameof(temporaryRoot))
                : temporaryRoot;
        }

        public int MaximumBytes { get; }
        public string TemporaryRoot { get; }

        public static PlcRevisionProviderOptions Default { get; } = new PlcRevisionProviderOptions(
            16_777_216, Path.Combine(Path.GetTempPath(), "TiaGitAddIn", "comparison"));
    }

    /// <summary>
    /// Loads revisions in strict order: validate → get size → reject over-limit before any content is
    /// read → read (bounded by the same limit) → verify the byte count matches the reported size → decode
    /// → build the immutable <see cref="PlcRevision"/> → hand it to a new <see cref="PlcRevisionLease"/>.
    /// No I/O or process exception is caught here: <see cref="RevisionLoadException"/> and
    /// <see cref="RevisionSizeLimitException"/> are allowed to reach the caller as hard failures.
    /// </summary>
    public sealed class PlcRevisionProvider : IPlcRevisionProvider
    {
        private readonly IGitBlobReader _blobReader;
        private readonly PlcRevisionProviderOptions _options;

        public PlcRevisionProvider(IGitBlobReader blobReader, PlcRevisionProviderOptions options)
        {
            _blobReader = blobReader ?? throw new ArgumentNullException(nameof(blobReader));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<PlcRevisionLease> LoadAsync(PlcRevisionSide side, PlcRevisionSource source,
            string repositoryRelativePath, CancellationToken cancellationToken)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(repositoryRelativePath))
            {
                throw new ArgumentException("Repository-relative path is required.", nameof(repositoryRelativePath));
            }

            cancellationToken.ThrowIfCancellationRequested();

            long size = await _blobReader.GetSizeAsync(source, repositoryRelativePath, cancellationToken)
                .ConfigureAwait(false);
            if (size > _options.MaximumBytes)
            {
                throw new RevisionSizeLimitException(
                    $"Revision size ({size} bytes) exceeds the {_options.MaximumBytes}-byte limit.");
            }

            System.Collections.Generic.IReadOnlyList<byte> content = await _blobReader
                .ReadAsync(source, repositoryRelativePath, _options.MaximumBytes, cancellationToken)
                .ConfigureAwait(false);

            if (content.Count != size)
            {
                throw new RevisionLoadException(
                    "The number of bytes read did not match the size git reported for the revision.");
            }

            byte[] bytes = content as byte[] ?? content.ToArray();
            DecodedRevision decoded = Decode(bytes);

            PlcRevision revision = PlcRevision.Present(side, source, repositoryRelativePath, bytes,
                decoded.Encoding, decoded.DecodedText, decoded.IsBinary, decoded.Limitation);

            return PlcRevisionLease.Create(revision, _options.TemporaryRoot);
        }

        public PlcRevisionLease Missing(PlcRevisionSide side, PlcRevisionSource source,
            string repositoryRelativePath, PlcRevisionMissingReason reason)
        {
            PlcRevision revision = PlcRevision.Missing(side, source, repositoryRelativePath, reason);
            return PlcRevisionLease.Create(revision, _options.TemporaryRoot);
        }

        private static DecodedRevision Decode(byte[] bytes)
        {
            if (StartsWith(bytes, 0xEF, 0xBB, 0xBF))
                return Text(new UTF8Encoding(false, true), bytes, 3, PlcTextEncoding.Utf8WithBom);
            if (StartsWith(bytes, 0xFF, 0xFE))
                return Text(new UnicodeEncoding(false, true, true), bytes, 2, PlcTextEncoding.Utf16LittleEndianWithBom);
            if (StartsWith(bytes, 0xFE, 0xFF))
                return Text(new UnicodeEncoding(true, true, true), bytes, 2, PlcTextEncoding.Utf16BigEndianWithBom);
            if (bytes.Any(value => value == 0))
                return DecodedRevision.Binary("NUL bytes were found without a supported Unicode BOM.");

            try
            {
                string text = new UTF8Encoding(false, true).GetString(bytes);
                return DecodedRevision.Text(text, PlcTextEncoding.Utf8WithoutBom);
            }
            catch (DecoderFallbackException)
            {
                return DecodedRevision.Binary("Content is not strict UTF-8 and has no supported Unicode BOM.");
            }
        }

        private static DecodedRevision Text(Encoding encoding, byte[] bytes, int bomLength, PlcTextEncoding encodingKind)
        {
            try
            {
                string text = encoding.GetString(bytes, bomLength, bytes.Length - bomLength);
                return DecodedRevision.Text(text, encodingKind);
            }
            catch (DecoderFallbackException)
            {
                return DecodedRevision.Binary(
                    $"Content declares a {encodingKind.Kind} byte-order mark but is not valid {encodingKind.Kind}.");
            }
        }

        private static bool StartsWith(byte[] bytes, params byte[] prefix)
        {
            if (bytes.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (bytes[i] != prefix[i]) return false;
            }

            return true;
        }

        private sealed class DecodedRevision
        {
            private DecodedRevision(string? text, PlcTextEncoding encoding, bool isBinary, string limitation)
            {
                DecodedText = text;
                Encoding = encoding;
                IsBinary = isBinary;
                Limitation = limitation;
            }

            public string? DecodedText { get; }
            public PlcTextEncoding Encoding { get; }
            public bool IsBinary { get; }
            public string Limitation { get; }

            public static DecodedRevision Text(string text, PlcTextEncoding encoding)
                => new DecodedRevision(text, encoding, false, string.Empty);

            public static DecodedRevision Binary(string limitation)
                => new DecodedRevision(null, PlcTextEncoding.None, true, limitation);
        }
    }
}
