# Structured SCL Diff Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a bounded, tolerant SCL lexer/parser/comparer and a focused WPF presentation that groups semantic changes by file, block, region, declaration section, statement, and comment while always retaining the raw-text alternative.

**Architecture:** The Siemens-free `TiaGitAddIn.Core` project owns immutable SCL tokens, syntax nodes, recovery spans, semantic changes, and `SclComparisonStrategy`. The strategy consumes the shared comparison-foundation contract and emits one `SclPresentation`; when no reliable block structure exists it requests the foundation's explicit text fallback. The `net48` WPF project maps that presentation once, selects `SclDiffView` through the shared resource dictionary, and never reclassifies the artifact.

**Tech Stack:** C# (`LangVersion=latest` without preview-only features), .NET Standard 2.0 Core, .NET Framework 4.8 WPF, xUnit 2.9.0 through VSTest, sanitized TIA Portal V21 fixtures, no SCL compiler or new runtime package.

## Global Constraints

- Complete `docs/superpowers/plans/2026-07-16-comparison-foundation-interface.md` first; this plan consumes its contracts and does not redefine its enums, coordinator, fallback, raw-text, diagnostics, mapper host, or STA helper.
- Keep `TiaGitAddIn.Core` on `netstandard2.0` with zero Siemens and WPF references; keep all WPF code in `TiaGitAddIn` on `net48`.
- Use the project-owned tolerant lexer and shallow parser only. Do not reference a Siemens compiler, generate PLC code, execute PLC code, or attempt full language validation.
- Use ordinal semantic identity for SCL block, region, section, and declaration names. Do not infer top-level block or region-label renames in the first implementation.
- Copy every input collection at construction and expose it as `IReadOnlyList<T>`; no parser, comparer, or mapper may mutate a token stream, syntax tree, previous comparison, or caller collection.
- Preserve byte/encoding decisions from the foundation revision provider. The SCL layer accepts decoded text only and never replacement-decodes bytes.
- Set exact SCL defaults to `MaxTokens = 200000` and `MaxNestingDepth = 128`; tests must exercise N and N+1 for each limit.
- Set exact structured-comparison defaults to `MaxSequenceItems = 10000` and `MaxWorkItems = 4000000`; preflight both limits before diff allocation and use bounded linear-memory matching only.
- Retain comments and exact source spans. Ignore formatting-only whitespace for statement comparison, but retain the original text in `ComparisonRawText`.
- A recoverable tree returns `Structured · Partial` with exact unparsed spans. No reliable block returns `Text · Fallback` with safe diagnostics. Cancellation returns no result through the shared coordinator.
- Highlight from the same `SclToken` instances used for comparison. Do not add a second UI lexer.
- Keep every added or modified focused C#/XAML file at or below 800 lines, methods below 50 lines where practical, and nesting no deeper than four levels.
- Use `Task`, not `async void`, outside genuine WPF event boundaries. UI-bound changes go through the captured dispatcher.
- Tests use deterministic source text, offsets, paths, hashes, and limits. They do not require TIA Portal at test time.
- Every implementation task follows RED → GREEN → refactor and ends with a conventional commit. Run commands from the repository root.

---

## Shared Contracts Consumed From the Foundation Plan

| Producer | Exact contract consumed here |
|---|---|
| `Models/Comparison/PlcComparisonEnums.cs` | `PlcArtifactKind.Scl`, `PlcComparisonMode.Structured`, `PlcSupportLevel`, `ComparisonPresentationKind.Scl`, `PlcDiagnosticSeverity`, `PlcRevisionSide` |
| `Models/Comparison/PlcComparisonContext.cs` | `PlcComparisonContext.Request` and `.RawText` |
| `Services/Comparison/IPlcComparisonStrategy.cs` | `IReadOnlyCollection<PlcArtifactKind> SupportedKinds` and `Task<PlcComparisonResult> CompareAsync(PlcComparisonContext context, CancellationToken cancellationToken)` |
| `Models/Comparison/ComparisonPresentation.cs` | abstract immutable `ComparisonPresentation`; `SclPresentation` fixes kind `ComparisonPresentationKind.Scl` |
| `Models/Comparison/PlcComparisonResult.cs` | complete result invariant and selectable `ComparisonRawText` |
| `Models/Comparison/PlcComparisonDiagnostic.cs` | safe stable diagnostic code/severity/message/`PlcSourceLocation` |
| `UI/Mapping/IComparisonPresentationMapper.cs` and `IComparisonPresentationViewModelFactory.cs` | the aggregate mapper plus the exact specialized-factory seam used by SCL |
| `UI/Views/Comparison/ComparisonTemplates.xaml` | the merged implicit-`DataTemplate` resource seam |
| `TiaGitAddIn.Tests/UI/WpfTestHost.cs` | `Run(Action<Dispatcher>)` and `RunAsync(Func<Dispatcher, Task>)` on a dedicated STA thread |

## File Map

### Core files to create

- `src/TiaGitAddIn.Core/Comparison/Scl/SclSourceSpan.cs` — immutable offset/length/line/column value.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclTokenKind.cs` — lexical categories used by parser, comparer, and highlighter.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclToken.cs` — immutable token text and span.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclDiagnostic.cs` — SCL-local stable diagnostic before safe shared mapping.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclParserLimits.cs` — exact token/nesting limits.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclLexResult.cs` — immutable token/diagnostic envelope.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclLexer.cs` — comment/string/identifier/operator-aware bounded lexer.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclSyntaxNodes.cs` — immutable document, block, declaration, region, statement, comment, and unparsed nodes.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclParseResult.cs` — immutable syntax/recovery envelope and reliability flag.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclParser.cs` — shallow block/declaration/body parser with named recovery boundaries.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclChanges.cs` — immutable hierarchical semantic change model.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclComparisonLimits.cs` — immutable sequence/work caps for semantic matching.
- `src/TiaGitAddIn.Core/Comparison/Scl/SclComparer.cs` — identity matching, token normalization, LCS grouping, and declaration rename inference.
- `src/TiaGitAddIn.Core/Models/Comparison/SclPresentation.cs` — concrete typed presentation containing both parse results and semantic comparison.
- `src/TiaGitAddIn.Core/Services/Comparison/SclComparisonStrategy.cs` — shared-strategy adapter and Full/Partial/Fallback decision.

### WPF files to create or modify

- `src/TiaGitAddIn/UI/ViewModels/Comparison/SclDiffViewModel.cs` — immutable-to-observable mapping, grouped source order, and shared token runs.
- `src/TiaGitAddIn/UI/Mapping/SclPresentationViewModelFactory.cs` — specialized factory selected by the aggregate foundation mapper.
- `src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml` — focused hierarchical structured view.
- `src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml.cs` — constructor-only code-behind.
- Modify `src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml` — add one implicit SCL template.
- Modify `src/TiaGitAddIn/UI/GitPanelLaunchService.cs` — register `SclComparisonStrategy` and `SclPresentationViewModelFactory` in the foundation composition lists.
- Modify `README.md` — replace generic SCL patch wording with the implemented structured/partial/fallback behavior.

### Test data and tests to create

- `src/TiaGitAddIn.Tests/Services/Comparison/SclLexerTests.cs`
- `src/TiaGitAddIn.Tests/Services/Comparison/SclParserTests.cs`
- `src/TiaGitAddIn.Tests/Services/Comparison/SclRecoveryTests.cs`
- `src/TiaGitAddIn.Tests/Services/Comparison/SclComparerTests.cs`
- `src/TiaGitAddIn.Tests/Services/Comparison/SclComparisonStrategyTests.cs`
- `src/TiaGitAddIn.Tests/UI/Comparison/SclDiffViewModelTests.cs`
- `src/TiaGitAddIn.Tests/UI/Comparison/SclDiffViewTests.cs`
- `src/TiaGitAddIn.Tests/Services/Comparison/SclFixtureCompatibilityTests.cs`
- `src/TiaGitAddIn.Tests/TestData/Scl/V21/GitAcceptanceScl.scl`
- `src/TiaGitAddIn.Tests/TestData/Scl/V21/manifest.json`
- `src/TiaGitAddIn.Tests/TestData/Scl/V21/New-Manifest.ps1`

### Task 1: Add the bounded SCL lexer and immutable token model

**Acceptance criteria:** AC-007, AC-055, AC-096, AC-107

**Files:**
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclSourceSpan.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclTokenKind.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclToken.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclDiagnostic.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclParserLimits.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclLexResult.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclLexer.cs`
- Test: `src/TiaGitAddIn.Tests/Services/Comparison/SclLexerTests.cs`

**Interfaces:**
- Produces: `SclLexer.Lex(string source, SclParserLimits limits, CancellationToken cancellationToken)` → immutable `SclLexResult`.
- Produces: `SclToken.Kind`, `.Text`, and `.Span`; later parser/comparer/UI tasks must reuse these exact objects.
- Consumes: no Siemens or WPF type.

- [ ] **Step 1: Write the failing lexer and limit tests**

```csharp
using System;
using System.Linq;
using System.Threading;
using TiaGitAddIn.Comparison.Scl;
using Xunit;

namespace TiaGitAddIn.Tests.Services.Comparison
{
    public sealed class SclLexerTests
    {
        [Fact]
        public void Lex_CommentsStringsQuotedIdentifiersAndOperators_KeepKindsAndSpansDistinct()
        {
            const string source =
                "FUNCTION_BLOCK \"Demo Block\"\r\n" +
                "VAR_INPUT\r\n" +
                "  Value : STRING := 'not // a comment and it''s escaped'; // real line comment\r\n" +
                "END_VAR\r\n" +
                "(* block comment with := and END_REGION *)\r\n" +
                "\"quoted value\" := Value <> 'x';\r\n" +
                "END_FUNCTION_BLOCK";

            SclLexResult result = SclLexer.Lex(source, SclParserLimits.Default, CancellationToken.None);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(2, result.Tokens.Count(token => token.Kind == SclTokenKind.Comment));
            Assert.Equal(2, result.Tokens.Count(token => token.Kind == SclTokenKind.StringLiteral));
            Assert.Equal(2, result.Tokens.Count(token => token.Kind == SclTokenKind.QuotedIdentifier));
            Assert.Contains(result.Tokens, token => token.Kind == SclTokenKind.Operator && token.Text == ":=");
            Assert.Contains(result.Tokens, token => token.Kind == SclTokenKind.Operator && token.Text == "<>");

            foreach (SclToken token in result.Tokens.Where(token => token.Kind != SclTokenKind.EndOfFile))
            {
                Assert.Equal(token.Text, source.Substring(token.Span.StartOffset, token.Span.Length));
            }
        }

        [Fact]
        public void Lex_NPlusOneTokens_StopsAtExactConfiguredBoundary()
        {
            var limits = new SclParserLimits(maxTokens: 3, maxNestingDepth: 8);

            SclLexResult atLimit = SclLexer.Lex("A B C", limits, CancellationToken.None);
            SclLexResult aboveLimit = SclLexer.Lex("A B C D", limits, CancellationToken.None);

            Assert.DoesNotContain(atLimit.Diagnostics, diagnostic => diagnostic.Code == "SCL1001");
            Assert.Equal(3, atLimit.Tokens.Count(token => token.Kind != SclTokenKind.EndOfFile));
            Assert.Contains(aboveLimit.Diagnostics, diagnostic => diagnostic.Code == "SCL1001");
            Assert.Equal(3, aboveLimit.Tokens.Count(token => token.Kind != SclTokenKind.EndOfFile));
        }

        [Fact]
        public void Lex_CallerCollectionMutation_CannotChangeResult()
        {
            SclLexResult result = SclLexer.Lex("A := 1;", SclParserLimits.Default, CancellationToken.None);
            SclToken first = result.Tokens[0];

            Assert.Throws<NotSupportedException>(() => ((System.Collections.IList)result.Tokens)[0] = null);
            Assert.Same(first, result.Tokens[0]);
        }
    }
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.Services.Comparison.SclLexerTests"
```

Expected: FAIL at compile time because `TiaGitAddIn.Comparison.Scl` and `SclLexer` do not exist.

- [ ] **Step 3: Add the immutable lexical contracts**

Create the exact public surface below; put each public type in the file named in the file list.

```csharp
namespace TiaGitAddIn.Comparison.Scl
{
    public sealed class SclSourceSpan
    {
        public SclSourceSpan(int startOffset, int length, int line, int column)
        {
            if (startOffset < 0) throw new System.ArgumentOutOfRangeException(nameof(startOffset));
            if (length < 0) throw new System.ArgumentOutOfRangeException(nameof(length));
            if (line < 1) throw new System.ArgumentOutOfRangeException(nameof(line));
            if (column < 1) throw new System.ArgumentOutOfRangeException(nameof(column));
            StartOffset = startOffset;
            Length = length;
            Line = line;
            Column = column;
        }

        public int StartOffset { get; }
        public int Length { get; }
        public int EndOffset => StartOffset + Length;
        public int Line { get; }
        public int Column { get; }
    }

    public enum SclTokenKind
    {
        Keyword,
        Identifier,
        QuotedIdentifier,
        StringLiteral,
        NumericLiteral,
        Operator,
        Semicolon,
        Colon,
        Comma,
        Dot,
        OpenParenthesis,
        CloseParenthesis,
        OpenBracket,
        CloseBracket,
        Comment,
        Unknown,
        EndOfFile
    }

    public sealed class SclToken
    {
        public SclToken(SclTokenKind kind, string text, SclSourceSpan span)
        {
            Kind = kind;
            Text = text ?? throw new System.ArgumentNullException(nameof(text));
            Span = span ?? throw new System.ArgumentNullException(nameof(span));
        }

        public SclTokenKind Kind { get; }
        public string Text { get; }
        public SclSourceSpan Span { get; }
    }

    public sealed class SclDiagnostic
    {
        public SclDiagnostic(string code, string message, SclSourceSpan span)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new System.ArgumentException("Diagnostic code is required.", nameof(code));
            if (string.IsNullOrWhiteSpace(message)) throw new System.ArgumentException("Diagnostic message is required.", nameof(message));
            Code = code;
            Message = message;
            Span = span ?? throw new System.ArgumentNullException(nameof(span));
        }

        public string Code { get; }
        public string Message { get; }
        public SclSourceSpan Span { get; }
    }

    public sealed class SclParserLimits
    {
        public static SclParserLimits Default { get; } = new SclParserLimits(200000, 128);

        public SclParserLimits(int maxTokens, int maxNestingDepth)
        {
            if (maxTokens < 1) throw new System.ArgumentOutOfRangeException(nameof(maxTokens));
            if (maxNestingDepth < 1) throw new System.ArgumentOutOfRangeException(nameof(maxNestingDepth));
            MaxTokens = maxTokens;
            MaxNestingDepth = maxNestingDepth;
        }

        public int MaxTokens { get; }
        public int MaxNestingDepth { get; }
    }
}
```

`SclLexResult` must copy both sequences:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Comparison.Scl
{
    public sealed class SclLexResult
    {
        public SclLexResult(IEnumerable<SclToken> tokens, IEnumerable<SclDiagnostic> diagnostics)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            Tokens = Array.AsReadOnly(tokens.ToArray());
            Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        }

        public IReadOnlyList<SclToken> Tokens { get; }
        public IReadOnlyList<SclDiagnostic> Diagnostics { get; }
        public bool LimitExceeded => Diagnostics.Any(item => item.Code == "SCL1001");
    }
}
```

- [ ] **Step 4: Implement the minimal bounded lexer**

Use one cursor, longest-operator-first matching, doubled-quote escaping, and token-count enforcement before adding a token. The complete loop and helpers are:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

namespace TiaGitAddIn.Comparison.Scl
{
    public sealed class SclLexer
    {
        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ORGANIZATION_BLOCK", "END_ORGANIZATION_BLOCK", "FUNCTION_BLOCK", "END_FUNCTION_BLOCK",
            "FUNCTION", "END_FUNCTION", "DATA_BLOCK", "END_DATA_BLOCK", "TYPE", "END_TYPE",
            "VAR_INPUT", "VAR_OUTPUT", "VAR_IN_OUT", "VAR", "VAR_TEMP", "VAR_CONSTANT", "END_VAR",
            "REGION", "END_REGION", "RETAIN", "CONSTANT", "BEGIN", "IF", "THEN", "ELSE", "END_IF"
        };

        private static readonly string[] TwoCharacterOperators =
        {
            ":=", "=>", "<=", ">=", "<>", "**", "+=", "-=", "*=", "/="
        };

        private readonly string source;
        private readonly SclParserLimits limits;
        private readonly CancellationToken cancellationToken;
        private readonly List<SclToken> tokens = new List<SclToken>();
        private readonly List<SclDiagnostic> diagnostics = new List<SclDiagnostic>();
        private int offset;
        private int line = 1;
        private int column = 1;
        private bool stopped;

        private SclLexer(string source, SclParserLimits limits, CancellationToken cancellationToken)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.limits = limits ?? throw new ArgumentNullException(nameof(limits));
            this.cancellationToken = cancellationToken;
        }

        public static SclLexResult Lex(string source, SclParserLimits limits, CancellationToken cancellationToken)
        {
            return new SclLexer(source, limits, cancellationToken).Run();
        }

        private SclLexResult Run()
        {
            while (offset < source.Length && !stopped)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (char.IsWhiteSpace(source[offset])) { Advance(); continue; }
                if (Matches("//")) { ReadLineComment(); continue; }
                if (Matches("(*")) { ReadDelimited(SclTokenKind.Comment, "(*", "*)", "SCL1002", "Unterminated block comment."); continue; }
                if (source[offset] == '\'') { ReadQuoted(SclTokenKind.StringLiteral, '\'', "SCL1003", "Unterminated string literal."); continue; }
                if (source[offset] == '\"') { ReadQuoted(SclTokenKind.QuotedIdentifier, '\"', "SCL1004", "Unterminated quoted identifier."); continue; }
                if (IsIdentifierStart(source[offset])) { ReadIdentifier(); continue; }
                if (char.IsDigit(source[offset])) { ReadNumber(); continue; }
                if (TryReadOperatorOrPunctuation()) continue;
                ReadSingle(SclTokenKind.Unknown);
            }

            tokens.Add(new SclToken(SclTokenKind.EndOfFile, string.Empty, new SclSourceSpan(offset, 0, line, column)));
            return new SclLexResult(tokens, diagnostics);
        }

        private void ReadLineComment()
        {
            int start = offset; int startLine = line; int startColumn = column;
            while (offset < source.Length && source[offset] != '\r' && source[offset] != '\n') Advance();
            AddToken(SclTokenKind.Comment, start, startLine, startColumn);
        }

        private void ReadDelimited(SclTokenKind kind, string open, string close, string code, string message)
        {
            int start = offset; int startLine = line; int startColumn = column;
            Advance(open.Length);
            while (offset < source.Length && !Matches(close)) Advance();
            if (Matches(close)) Advance(close.Length);
            else diagnostics.Add(new SclDiagnostic(code, message, new SclSourceSpan(start, offset - start, startLine, startColumn)));
            AddToken(kind, start, startLine, startColumn);
        }

        private void ReadQuoted(SclTokenKind kind, char quote, string code, string message)
        {
            int start = offset; int startLine = line; int startColumn = column;
            Advance();
            bool closed = false;
            while (offset < source.Length)
            {
                if (source[offset] != quote) { Advance(); continue; }
                if (offset + 1 < source.Length && source[offset + 1] == quote) { Advance(2); continue; }
                Advance(); closed = true; break;
            }
            if (!closed) diagnostics.Add(new SclDiagnostic(code, message, new SclSourceSpan(start, offset - start, startLine, startColumn)));
            AddToken(kind, start, startLine, startColumn);
        }

        private void ReadIdentifier()
        {
            int start = offset; int startLine = line; int startColumn = column;
            while (offset < source.Length && IsIdentifierPart(source[offset])) Advance();
            string text = source.Substring(start, offset - start);
            AddToken(Keywords.Contains(text) ? SclTokenKind.Keyword : SclTokenKind.Identifier, start, startLine, startColumn);
        }

        private void ReadNumber()
        {
            int start = offset; int startLine = line; int startColumn = column;
            while (offset < source.Length && (char.IsLetterOrDigit(source[offset]) || source[offset] == '_' || source[offset] == '#')) Advance();
            AddToken(SclTokenKind.NumericLiteral, start, startLine, startColumn);
        }

        private bool TryReadOperatorOrPunctuation()
        {
            foreach (string value in TwoCharacterOperators)
            {
                if (!Matches(value)) continue;
                int start = offset; int startLine = line; int startColumn = column;
                Advance(value.Length); AddToken(SclTokenKind.Operator, start, startLine, startColumn); return true;
            }

            SclTokenKind kind;
            switch (source[offset])
            {
                case ';': kind = SclTokenKind.Semicolon; break;
                case ':': kind = SclTokenKind.Colon; break;
                case ',': kind = SclTokenKind.Comma; break;
                case '.': kind = SclTokenKind.Dot; break;
                case '(': kind = SclTokenKind.OpenParenthesis; break;
                case ')': kind = SclTokenKind.CloseParenthesis; break;
                case '[': kind = SclTokenKind.OpenBracket; break;
                case ']': kind = SclTokenKind.CloseBracket; break;
                case '+': case '-': case '*': case '/': case '=': case '<': case '>': case '&': case '|':
                    kind = SclTokenKind.Operator; break;
                default: return false;
            }
            ReadSingle(kind); return true;
        }

        private void ReadSingle(SclTokenKind kind)
        {
            int start = offset; int startLine = line; int startColumn = column;
            Advance(); AddToken(kind, start, startLine, startColumn);
        }

        private void AddToken(SclTokenKind kind, int start, int startLine, int startColumn)
        {
            if (tokens.Count >= limits.MaxTokens)
            {
                diagnostics.Add(new SclDiagnostic("SCL1001", "SCL token limit exceeded.", new SclSourceSpan(start, 0, startLine, startColumn)));
                stopped = true;
                return;
            }
            tokens.Add(new SclToken(kind, source.Substring(start, offset - start), new SclSourceSpan(start, offset - start, startLine, startColumn)));
        }

        private bool Matches(string value)
        {
            return offset + value.Length <= source.Length && string.CompareOrdinal(source, offset, value, 0, value.Length) == 0;
        }

        private void Advance(int count = 1)
        {
            for (int index = 0; index < count && offset < source.Length; index++)
            {
                char current = source[offset++];
                if (current == '\r' && offset < source.Length && source[offset] == '\n')
                {
                    offset++; index++; line++; column = 1;
                }
                else if (current == '\r' || current == '\n') { line++; column = 1; }
                else { column++; }
            }
        }

        private static bool IsIdentifierStart(char value) => char.IsLetter(value) || value == '_' || value == '#';
        private static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value == '_' || value == '#';
    }
}
```

- [ ] **Step 5: Run lexer tests, then the existing Core parser regression tests**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclLexerTests|FullyQualifiedName~SimaticMlParserTests"
```

Expected: PASS; `SclLexerTests` and existing `SimaticMlParserTests` all succeed.

- [ ] **Step 6: Commit the lexical slice**

```powershell
git add src/TiaGitAddIn.Core/Comparison/Scl src/TiaGitAddIn.Tests/Services/Comparison/SclLexerTests.cs
git commit -m "feat: add bounded scl lexer"
```

### Task 2: Parse top-level blocks and declaration sections

**Acceptance criteria:** AC-007, AC-056, AC-057, AC-060, AC-107

**Files:**
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclSyntaxNodes.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclParseResult.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclParser.cs`
- Test: `src/TiaGitAddIn.Tests/Services/Comparison/SclParserTests.cs`

**Interfaces:**
- Consumes: `SclLexer.Lex(...)` and the same immutable `SclToken` instances.
- Produces: `SclParser.Parse(string, SclParserLimits, CancellationToken)` → `SclParseResult`.
- Produces: `SclDocumentSyntax.Blocks`; each block owns declaration sections and body nodes without mutating lexical input.

- [ ] **Step 1: Write failing block/declaration tests**

```csharp
using System.Linq;
using System.Threading;
using TiaGitAddIn.Comparison.Scl;
using Xunit;

namespace TiaGitAddIn.Tests.Services.Comparison
{
    public sealed class SclParserTests
    {
        [Theory]
        [InlineData("ORGANIZATION_BLOCK", "END_ORGANIZATION_BLOCK", SclBlockKind.OrganizationBlock)]
        [InlineData("FUNCTION_BLOCK", "END_FUNCTION_BLOCK", SclBlockKind.FunctionBlock)]
        [InlineData("FUNCTION", "END_FUNCTION", SclBlockKind.Function)]
        [InlineData("DATA_BLOCK", "END_DATA_BLOCK", SclBlockKind.DataBlock)]
        [InlineData("TYPE", "END_TYPE", SclBlockKind.Type)]
        public void Parse_SupportedBlockKind_ReturnsNameAndCompleteSpan(string start, string end, SclBlockKind expectedKind)
        {
            string source = start + " \"Demo\"\r\n" + end;

            SclParseResult result = SclParser.Parse(source, SclParserLimits.Default, CancellationToken.None);

            SclBlockSyntax block = Assert.Single(result.Document.Blocks);
            Assert.Equal(expectedKind, block.Kind);
            Assert.Equal("Demo", block.Name);
            Assert.Equal(source, source.Substring(block.Span.StartOffset, block.Span.Length));
            Assert.Equal(SclParseReliability.Full, result.Reliability);
        }

        [Fact]
        public void Parse_AllDeclarationSections_RetainsIdentifierDatatypeModifiersDefaultAndSpan()
        {
            const string source =
                "FUNCTION_BLOCK \"Demo\"\n" +
                "VAR_INPUT\nInputA : Bool := FALSE;\nEND_VAR\n" +
                "VAR_OUTPUT\nOutputA : DInt;\nEND_VAR\n" +
                "VAR_IN_OUT\nInOutA : Real;\nEND_VAR\n" +
                "VAR RETAIN\nStaticA : Array[1..2] OF Int := [1, 2];\nEND_VAR\n" +
                "VAR_TEMP\nTempA : Word;\nEND_VAR\n" +
                "VAR_CONSTANT\nConstantA : Int := 7;\nEND_VAR\n" +
                "END_FUNCTION_BLOCK";

            SclParseResult result = SclParser.Parse(source, SclParserLimits.Default, CancellationToken.None);
            SclBlockSyntax block = Assert.Single(result.Document.Blocks);

            Assert.Equal(6, block.DeclarationSections.Count);
            Assert.Equal(new[] { "InputA", "OutputA", "InOutA", "StaticA", "TempA", "ConstantA" },
                block.DeclarationSections.SelectMany(section => section.Declarations).Select(item => item.Identifier));
            SclDeclarationSyntax retained = block.DeclarationSections[3].Declarations.Single();
            Assert.Equal("Array [ 1 .. 2 ] OF Int", retained.DataType);
            Assert.Contains("RETAIN", retained.Modifiers);
            Assert.Equal("[ 1 , 2 ]", retained.DefaultValue);
            Assert.Equal("StaticA : Array[1..2] OF Int := [1, 2];",
                source.Substring(retained.Span.StartOffset, retained.Span.Length));
        }
    }
}
```

- [ ] **Step 2: Run the parser tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclParserTests"
```

Expected: FAIL at compile time because `SclParser`, `SclBlockSyntax`, and declaration types do not exist.

- [ ] **Step 3: Add the final immutable syntax shape**

Create the enums and immutable types below in `SclSyntaxNodes.cs`. Constructors must use the shown `Copy` helper for every sequence.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Comparison.Scl
{
    public enum SclBlockKind { OrganizationBlock, FunctionBlock, Function, DataBlock, Type }
    public enum SclDeclarationSectionKind { Input, Output, InOut, Static, Temp, Constant }
    public enum SclRecoveryBoundary { Semicolon, RegionBoundary, DeclarationTerminator, BlockTerminator }
    public enum SclParseReliability { None, Partial, Full }

    public sealed class SclDocumentSyntax
    {
        public SclDocumentSyntax(IEnumerable<SclBlockSyntax> blocks) => Blocks = SyntaxCopy.Of(blocks);
        public IReadOnlyList<SclBlockSyntax> Blocks { get; }
    }

    public sealed class SclBlockSyntax
    {
        public SclBlockSyntax(
            SclBlockKind kind,
            string name,
            SclSourceSpan span,
            IEnumerable<SclDeclarationSectionSyntax> declarationSections,
            IEnumerable<SclBodySyntax> body)
        {
            Kind = kind;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Span = span ?? throw new ArgumentNullException(nameof(span));
            DeclarationSections = SyntaxCopy.Of(declarationSections);
            Body = SyntaxCopy.Of(body);
        }

        public SclBlockKind Kind { get; }
        public string Name { get; }
        public SclSourceSpan Span { get; }
        public IReadOnlyList<SclDeclarationSectionSyntax> DeclarationSections { get; }
        public IReadOnlyList<SclBodySyntax> Body { get; }
    }

    public sealed class SclDeclarationSectionSyntax
    {
        public SclDeclarationSectionSyntax(
            SclDeclarationSectionKind kind,
            IEnumerable<string> modifiers,
            SclSourceSpan span,
            IEnumerable<SclDeclarationSyntax> declarations,
            IEnumerable<SclUnparsedSyntax> unparsedSpans)
        {
            Kind = kind;
            Modifiers = SyntaxCopy.Of(modifiers);
            Span = span ?? throw new ArgumentNullException(nameof(span));
            Declarations = SyntaxCopy.Of(declarations);
            UnparsedSpans = SyntaxCopy.Of(unparsedSpans);
        }

        public SclDeclarationSectionKind Kind { get; }
        public IReadOnlyList<string> Modifiers { get; }
        public SclSourceSpan Span { get; }
        public IReadOnlyList<SclDeclarationSyntax> Declarations { get; }
        public IReadOnlyList<SclUnparsedSyntax> UnparsedSpans { get; }
    }

    public sealed class SclDeclarationSyntax
    {
        public SclDeclarationSyntax(string identifier, string dataType, IEnumerable<string> modifiers, string? defaultValue, SclSourceSpan span)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            Modifiers = SyntaxCopy.Of(modifiers);
            DefaultValue = defaultValue;
            Span = span ?? throw new ArgumentNullException(nameof(span));
        }

        public string Identifier { get; }
        public string DataType { get; }
        public IReadOnlyList<string> Modifiers { get; }
        public string? DefaultValue { get; }
        public SclSourceSpan Span { get; }
    }

    public abstract class SclBodySyntax
    {
        protected SclBodySyntax(SclSourceSpan span) => Span = span ?? throw new ArgumentNullException(nameof(span));
        public SclSourceSpan Span { get; }
    }

    public sealed class SclRegionSyntax : SclBodySyntax
    {
        public SclRegionSyntax(string label, SclSourceSpan span, IEnumerable<SclBodySyntax> children) : base(span)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Children = SyntaxCopy.Of(children);
        }
        public string Label { get; }
        public IReadOnlyList<SclBodySyntax> Children { get; }
    }

    public sealed class SclStatementSyntax : SclBodySyntax
    {
        public SclStatementSyntax(SclSourceSpan span, IEnumerable<SclToken> tokens) : base(span) => Tokens = SyntaxCopy.Of(tokens);
        public IReadOnlyList<SclToken> Tokens { get; }
    }

    public sealed class SclCommentSyntax : SclBodySyntax
    {
        public SclCommentSyntax(SclToken token) : base(token?.Span ?? throw new ArgumentNullException(nameof(token))) => Token = token;
        public SclToken Token { get; }
    }

    public sealed class SclUnparsedSyntax : SclBodySyntax
    {
        public SclUnparsedSyntax(SclSourceSpan span, SclRecoveryBoundary boundary) : base(span) => Boundary = boundary;
        public SclRecoveryBoundary Boundary { get; }
    }

    internal static class SyntaxCopy
    {
        public static IReadOnlyList<T> Of<T>(IEnumerable<T> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            return Array.AsReadOnly(values.ToArray());
        }
    }
}
```

Create `SclParseResult.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Comparison.Scl
{
    public sealed class SclParseResult
    {
        public SclParseResult(SclLexResult lex, SclDocumentSyntax document, IEnumerable<SclDiagnostic> diagnostics, SclParseReliability reliability)
        {
            Lex = lex ?? throw new ArgumentNullException(nameof(lex));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
            Reliability = reliability;
            UnparsedSpans = Array.AsReadOnly(Document.Blocks
                .SelectMany(block => block.DeclarationSections.SelectMany(section => section.UnparsedSpans)
                    .Concat(Flatten(block.Body).OfType<SclUnparsedSyntax>()))
                .ToArray());
        }

        public SclLexResult Lex { get; }
        public SclDocumentSyntax Document { get; }
        public IReadOnlyList<SclDiagnostic> Diagnostics { get; }
        public SclParseReliability Reliability { get; }
        public IReadOnlyList<SclUnparsedSyntax> UnparsedSpans { get; }

        private static IEnumerable<SclBodySyntax> Flatten(IEnumerable<SclBodySyntax> body)
        {
            foreach (SclBodySyntax node in body)
            {
                yield return node;
                if (node is SclRegionSyntax region)
                    foreach (SclBodySyntax child in Flatten(region.Children)) yield return child;
            }
        }
    }
}
```

- [ ] **Step 4: Implement block and declaration parsing**

Implement `SclParser.Parse` with these exact maps and parsing rules:

```csharp
private static readonly IReadOnlyDictionary<string, SclBlockKind> BlockStarts =
    new Dictionary<string, SclBlockKind>(StringComparer.OrdinalIgnoreCase)
    {
        ["ORGANIZATION_BLOCK"] = SclBlockKind.OrganizationBlock,
        ["FUNCTION_BLOCK"] = SclBlockKind.FunctionBlock,
        ["FUNCTION"] = SclBlockKind.Function,
        ["DATA_BLOCK"] = SclBlockKind.DataBlock,
        ["TYPE"] = SclBlockKind.Type
    };

private static readonly IReadOnlyDictionary<SclBlockKind, string> BlockEnds =
    new Dictionary<SclBlockKind, string>
    {
        [SclBlockKind.OrganizationBlock] = "END_ORGANIZATION_BLOCK",
        [SclBlockKind.FunctionBlock] = "END_FUNCTION_BLOCK",
        [SclBlockKind.Function] = "END_FUNCTION",
        [SclBlockKind.DataBlock] = "END_DATA_BLOCK",
        [SclBlockKind.Type] = "END_TYPE"
    };

private static readonly IReadOnlyDictionary<string, SclDeclarationSectionKind> SectionStarts =
    new Dictionary<string, SclDeclarationSectionKind>(StringComparer.OrdinalIgnoreCase)
    {
        ["VAR_INPUT"] = SclDeclarationSectionKind.Input,
        ["VAR_OUTPUT"] = SclDeclarationSectionKind.Output,
        ["VAR_IN_OUT"] = SclDeclarationSectionKind.InOut,
        ["VAR"] = SclDeclarationSectionKind.Static,
        ["VAR_TEMP"] = SclDeclarationSectionKind.Temp,
        ["VAR_CONSTANT"] = SclDeclarationSectionKind.Constant
    };
```

The public entry and declaration segment parser must be:

```csharp
public static SclParseResult Parse(string source, SclParserLimits limits, CancellationToken cancellationToken)
{
    if (source == null) throw new ArgumentNullException(nameof(source));
    SclLexResult lex = SclLexer.Lex(source, limits, cancellationToken);
    var diagnostics = new List<SclDiagnostic>(lex.Diagnostics);
    var blocks = ParseBlocks(source, lex.Tokens, limits, diagnostics, cancellationToken);
    SclParseReliability reliability = blocks.Count == 0
        ? SclParseReliability.None
        : diagnostics.Count == 0 ? SclParseReliability.Full : SclParseReliability.Partial;
    return new SclParseResult(lex, new SclDocumentSyntax(blocks), diagnostics, reliability);
}

private static IReadOnlyList<SclDeclarationSyntax> ParseDeclarationSegment(
    IReadOnlyList<SclToken> segment,
    IReadOnlyList<string> sectionModifiers,
    List<SclDiagnostic> diagnostics)
{
    int colon = IndexOf(segment, SclTokenKind.Colon);
    if (colon <= 0)
    {
        diagnostics.Add(new SclDiagnostic("SCL2003", "Declaration is missing its datatype separator.", Span(segment)));
        return Array.Empty<SclDeclarationSyntax>();
    }

    int assignment = IndexOfText(segment, ":=");
    int typeEnd = assignment >= 0 ? assignment : segment.Count - 1;
    string dataType = JoinTokens(segment.Skip(colon + 1).Take(typeEnd - colon - 1));
    string? defaultValue = assignment >= 0
        ? JoinTokens(segment.Skip(assignment + 1).Take(segment.Count - assignment - 2))
        : null;
    var identifiers = segment.Take(colon)
        .Where(token => token.Kind == SclTokenKind.Identifier || token.Kind == SclTokenKind.QuotedIdentifier)
        .Select(token => Unquote(token.Text));
    return identifiers.Select(identifier => new SclDeclarationSyntax(
        identifier,
        dataType,
        sectionModifiers,
        defaultValue,
        Span(segment))).ToArray();
}
```

Add these complete Task 2 helpers. They scan only keyword tokens for structural boundaries, use a same-line identifier for the block name, and pass an empty recovery collection until Task 3 adds tolerant recovery:

```csharp
private static IReadOnlyList<SclBlockSyntax> ParseBlocks(
    string source,
    IReadOnlyList<SclToken> tokens,
    SclParserLimits limits,
    List<SclDiagnostic> diagnostics,
    CancellationToken cancellationToken)
{
    var blocks = new List<SclBlockSyntax>();
    int index = 0;
    while (index < tokens.Count && tokens[index].Kind != SclTokenKind.EndOfFile)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SclToken start = tokens[index];
        if (start.Kind != SclTokenKind.Keyword || !BlockStarts.TryGetValue(start.Text, out SclBlockKind kind))
        {
            index++;
            continue;
        }

        int nameIndex = FindBlockName(tokens, index + 1, start.Span.Line);
        string name = nameIndex >= 0 ? Unquote(tokens[nameIndex].Text) : string.Empty;
        if (nameIndex < 0)
            diagnostics.Add(new SclDiagnostic("SCL2002", "SCL block name is missing.", start.Span));

        int endIndex = FindKeyword(tokens, index + 1, BlockEnds[kind]);
        if (endIndex < 0)
        {
            diagnostics.Add(new SclDiagnostic("SCL2001", "SCL block terminator is missing.", start.Span));
            endIndex = LastContentTokenIndex(tokens);
        }
        if (endIndex < index) endIndex = index;

        int contentStart = nameIndex >= 0 ? nameIndex + 1 : index + 1;
        IReadOnlyList<SclDeclarationSectionSyntax> sections = ParseDeclarationSections(
            tokens, contentStart, endIndex, diagnostics, cancellationToken);
        blocks.Add(new SclBlockSyntax(
            kind,
            name,
            Span(tokens[index], tokens[endIndex]),
            sections,
            Array.Empty<SclBodySyntax>()));
        index = endIndex + 1;
    }
    return blocks;
}

private static IReadOnlyList<SclDeclarationSectionSyntax> ParseDeclarationSections(
    IReadOnlyList<SclToken> tokens,
    int startIndex,
    int endExclusive,
    List<SclDiagnostic> diagnostics,
    CancellationToken cancellationToken)
{
    var sections = new List<SclDeclarationSectionSyntax>();
    int index = startIndex;
    while (index < endExclusive)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SclToken start = tokens[index];
        if (start.Kind != SclTokenKind.Keyword || !SectionStarts.TryGetValue(start.Text, out SclDeclarationSectionKind kind))
        {
            index++;
            continue;
        }

        int endVar = FindKeyword(tokens, index + 1, "END_VAR", endExclusive);
        if (endVar < 0)
        {
            diagnostics.Add(new SclDiagnostic("SCL2004", "Declaration section terminator is missing.", start.Span));
            endVar = Math.Max(index, endExclusive - 1);
        }
        IReadOnlyList<string> modifiers = ReadSectionModifiers(tokens, index, endVar);
        IReadOnlyList<SclDeclarationSyntax> declarations = ParseDeclarations(
            tokens, FirstDeclarationIndex(tokens, index + 1, endVar), endVar, modifiers, diagnostics);
        sections.Add(new SclDeclarationSectionSyntax(
            kind,
            modifiers,
            Span(tokens[index], tokens[endVar]),
            declarations,
            Array.Empty<SclUnparsedSyntax>()));
        index = endVar + 1;
    }
    return sections;
}

private static IReadOnlyList<SclDeclarationSyntax> ParseDeclarations(
    IReadOnlyList<SclToken> tokens,
    int startIndex,
    int endExclusive,
    IReadOnlyList<string> modifiers,
    List<SclDiagnostic> diagnostics)
{
    var declarations = new List<SclDeclarationSyntax>();
    var segment = new List<SclToken>();
    for (int index = startIndex; index < endExclusive; index++)
    {
        SclToken token = tokens[index];
        if (token.Kind == SclTokenKind.Comment) continue;
        segment.Add(token);
        if (token.Kind != SclTokenKind.Semicolon) continue;
        declarations.AddRange(ParseDeclarationSegment(segment, modifiers, diagnostics));
        segment.Clear();
    }
    if (segment.Count > 0)
        diagnostics.Add(new SclDiagnostic("SCL2005", "Declaration is missing its semicolon.", Span(segment)));
    return declarations;
}

private static IReadOnlyList<string> ReadSectionModifiers(
    IReadOnlyList<SclToken> tokens,
    int sectionStart,
    int endExclusive)
{
    int line = tokens[sectionStart].Span.Line;
    return tokens.Skip(sectionStart + 1)
        .Take(endExclusive - sectionStart - 1)
        .TakeWhile(token => token.Span.Line == line)
        .Where(token => token.Kind == SclTokenKind.Keyword)
        .Select(token => token.Text)
        .ToArray();
}

private static int FirstDeclarationIndex(IReadOnlyList<SclToken> tokens, int startIndex, int endExclusive)
{
    int index = startIndex;
    while (index < endExclusive && tokens[index].Span.Line == tokens[startIndex - 1].Span.Line &&
           tokens[index].Kind == SclTokenKind.Keyword)
        index++;
    return index;
}

private static int FindBlockName(IReadOnlyList<SclToken> tokens, int startIndex, int headerLine)
{
    for (int index = startIndex; index < tokens.Count; index++)
    {
        SclToken token = tokens[index];
        if (token.Kind == SclTokenKind.EndOfFile || token.Span.Line != headerLine) return -1;
        if (token.Kind == SclTokenKind.Identifier || token.Kind == SclTokenKind.QuotedIdentifier) return index;
    }
    return -1;
}

private static int FindKeyword(
    IReadOnlyList<SclToken> tokens,
    int startIndex,
    string keyword,
    int endExclusive = int.MaxValue)
{
    int end = Math.Min(tokens.Count, endExclusive);
    for (int index = startIndex; index < end; index++)
        if (IsKeyword(tokens[index], keyword)) return index;
    return -1;
}

private static int LastContentTokenIndex(IReadOnlyList<SclToken> tokens)
{
    for (int index = tokens.Count - 1; index >= 0; index--)
        if (tokens[index].Kind != SclTokenKind.EndOfFile) return index;
    return -1;
}

private static int IndexOf(IReadOnlyList<SclToken> tokens, SclTokenKind kind)
{
    for (int index = 0; index < tokens.Count; index++)
        if (tokens[index].Kind == kind) return index;
    return -1;
}

private static int IndexOfText(IReadOnlyList<SclToken> tokens, string text)
{
    for (int index = 0; index < tokens.Count; index++)
        if (string.Equals(tokens[index].Text, text, StringComparison.Ordinal)) return index;
    return -1;
}

private static bool IsKeyword(SclToken token, string keyword) =>
    token.Kind == SclTokenKind.Keyword && string.Equals(token.Text, keyword, StringComparison.OrdinalIgnoreCase);

private static string JoinTokens(IEnumerable<SclToken> tokens) =>
    string.Join(" ", tokens.Select(token => token.Text)).Replace(". .", "..");

private static string Unquote(string value)
{
    if (value.Length >= 2 && ((value[0] == '\"' && value[value.Length - 1] == '\"') ||
                              (value[0] == '\'' && value[value.Length - 1] == '\'')))
        return value.Substring(1, value.Length - 2).Replace("\"\"", "\"").Replace("''", "'");
    return value;
}

private static SclSourceSpan Span(IReadOnlyList<SclToken> tokens) =>
    Span(tokens[0], tokens[tokens.Count - 1]);

private static SclSourceSpan Span(SclToken first, SclToken last) =>
    new SclSourceSpan(
        first.Span.StartOffset,
        last.Span.EndOffset - first.Span.StartOffset,
        first.Span.Line,
        first.Span.Column);
```

- [ ] **Step 5: Run parser, lexer, and immutability tests**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclParserTests|FullyQualifiedName~SclLexerTests|FullyQualifiedName~ComparisonContractTests"
```

Expected: PASS; all five block theories and the declaration-field test succeed, with no caller-mutation leak.

- [ ] **Step 6: Commit the structural parser slice**

```powershell
git add src/TiaGitAddIn.Core/Comparison/Scl/SclSyntaxNodes.cs src/TiaGitAddIn.Core/Comparison/Scl/SclParseResult.cs src/TiaGitAddIn.Core/Comparison/Scl/SclParser.cs src/TiaGitAddIn.Tests/Services/Comparison/SclParserTests.cs
git commit -m "feat: parse scl blocks and declarations"
```

### Task 3: Preserve regions, statements, comments, and exact recovery spans

**Acceptance criteria:** AC-058, AC-059, AC-066, AC-096, AC-107

**Files:**
- Modify: `src/TiaGitAddIn.Core/Comparison/Scl/SclParser.cs`
- Test: `src/TiaGitAddIn.Tests/Services/Comparison/SclRecoveryTests.cs`

**Interfaces:**
- Consumes: the final body-node types already created in Task 2.
- Produces: nested `SclRegionSyntax`, direct out-of-region nodes, `SclStatementSyntax`, `SclCommentSyntax`, and `SclUnparsedSyntax` with a named `SclRecoveryBoundary`.
- Preserves: every node references the original lexer tokens/spans.

- [ ] **Step 1: Write failing grouping and recovery tests**

```csharp
using System;
using System.Linq;
using System.Threading;
using TiaGitAddIn.Comparison.Scl;
using Xunit;

namespace TiaGitAddIn.Tests.Services.Comparison
{
    public sealed class SclRecoveryTests
    {
        [Fact]
        public void Parse_NestedRegionsCommentsAndOutOfRegionCode_PreservesSourceOrder()
        {
            const string source =
                "FUNCTION_BLOCK \"Demo\"\n" +
                "// outside\nOutside := 1;\n" +
                "REGION Outer\n(* leading *)\nREGION Inner\nInside := 2;\nEND_REGION\nEND_REGION\n" +
                "Tail := 3; // trailing\nEND_FUNCTION_BLOCK";

            SclParseResult result = SclParser.Parse(source, SclParserLimits.Default, CancellationToken.None);
            SclBlockSyntax block = Assert.Single(result.Document.Blocks);

            Assert.Collection(block.Body,
                node => Assert.IsType<SclCommentSyntax>(node),
                node => Assert.IsType<SclStatementSyntax>(node),
                node =>
                {
                    SclRegionSyntax outer = Assert.IsType<SclRegionSyntax>(node);
                    Assert.Equal("Outer", outer.Label);
                    Assert.Contains(outer.Children, child => child is SclRegionSyntax region && region.Label == "Inner");
                },
                node => Assert.IsType<SclStatementSyntax>(node),
                node => Assert.IsType<SclCommentSyntax>(node));
            Assert.Equal(SclParseReliability.Full, result.Reliability);
        }

        [Theory]
        [InlineData("Broken( ; Good := 1;", ";", SclRecoveryBoundary.Semicolon)]
        [InlineData("Broken( REGION Good\nGood := 1;\nEND_REGION", "REGION", SclRecoveryBoundary.RegionBoundary)]
        [InlineData("VAR_INPUT\nBroken(\nEND_VAR\nGood := 1;", "END_VAR", SclRecoveryBoundary.DeclarationTerminator)]
        [InlineData("Broken( END_FUNCTION_BLOCK\nFUNCTION_BLOCK \"GoodBlock\"\nGood := 1;\nEND_FUNCTION_BLOCK", "END_FUNCTION_BLOCK", SclRecoveryBoundary.BlockTerminator)]
        public void Parse_MalformedInput_RecoversOnlyAtApprovedBoundary(
            string body,
            string boundaryText,
            SclRecoveryBoundary expectedBoundary)
        {
            string source = "FUNCTION_BLOCK \"Demo\"\n" + body +
                (body.Contains("END_FUNCTION_BLOCK") ? string.Empty : "\nEND_FUNCTION_BLOCK");

            SclParseResult result = SclParser.Parse(source, SclParserLimits.Default, CancellationToken.None);
            SclUnparsedSyntax unparsed = Assert.Single(result.UnparsedSpans);

            Assert.Equal(expectedBoundary, unparsed.Boundary);
            Assert.Equal(source.IndexOf("Broken", StringComparison.Ordinal), unparsed.Span.StartOffset);
            Assert.Equal(source.IndexOf(boundaryText, unparsed.Span.StartOffset, StringComparison.Ordinal), unparsed.Span.EndOffset);
            Assert.Contains(result.Document.Blocks.SelectMany(block => block.Body).SelectMany(Flatten), node =>
                node is SclStatementSyntax statement && statement.Tokens.Any(token => token.Text == "Good"));
            Assert.Equal(SclParseReliability.Partial, result.Reliability);
        }

        [Fact]
        public void Parse_NestingNPlusOne_StopsAtConfiguredDepth()
        {
            const string atLimitSource = "FUNCTION_BLOCK \"Demo\"\nREGION A\nEND_REGION\nEND_FUNCTION_BLOCK";
            const string aboveLimitSource = "FUNCTION_BLOCK \"Demo\"\nREGION A\nREGION B\nEND_REGION\nEND_REGION\nEND_FUNCTION_BLOCK";
            var limits = new SclParserLimits(maxTokens: 100, maxNestingDepth: 1);

            SclParseResult atLimit = SclParser.Parse(atLimitSource, limits, CancellationToken.None);
            SclParseResult aboveLimit = SclParser.Parse(aboveLimitSource, limits, CancellationToken.None);

            Assert.DoesNotContain(atLimit.Diagnostics, diagnostic => diagnostic.Code == "SCL2008");
            Assert.Contains(aboveLimit.Diagnostics, diagnostic => diagnostic.Code == "SCL2008");
            Assert.Equal(SclParseReliability.Partial, aboveLimit.Reliability);
        }

        private static System.Collections.Generic.IEnumerable<SclBodySyntax> Flatten(SclBodySyntax node)
        {
            yield return node;
            if (node is SclRegionSyntax region)
                foreach (SclBodySyntax child in region.Children.SelectMany(Flatten)) yield return child;
        }
    }
}
```

- [ ] **Step 2: Run recovery tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclRecoveryTests"
```

Expected: FAIL because block bodies are still empty and no recovery span is produced.

- [ ] **Step 3: Add body grouping and bounded nesting**

Replace the empty body builder in `ParseBlocks` with `ParseBody`. Pass the matched block-terminator token's `Span.StartOffset` as `blockEndOffset`; this makes block-boundary recovery spans end exactly where `END_*` begins, including any whitespace between the final malformed token and the terminator. Use a stack of region builders; direct nodes stay at block level and become the UI's explicit `Ungrouped` bucket. The core loop must use this shape:

```csharp
private static IReadOnlyList<SclBodySyntax> ParseBody(
    IReadOnlyList<SclToken> tokens,
    SclParserLimits limits,
    List<SclDiagnostic> diagnostics,
    int blockEndOffset)
{
    var root = new List<SclBodySyntax>();
    var regions = new Stack<RegionBuilder>();
    var statement = new List<SclToken>();

    for (int index = 0; index < tokens.Count; index++)
    {
        SclToken token = tokens[index];
        if (IsKeyword(token, "REGION"))
        {
            FlushStatement(statement, Current(root, regions), diagnostics, SclRecoveryBoundary.RegionBoundary, token.Span.StartOffset);
            string label = ReadRegionLabel(tokens, ref index, diagnostics);
            if (regions.Count >= limits.MaxNestingDepth)
            {
                diagnostics.Add(new SclDiagnostic("SCL2008", "SCL nesting limit exceeded.", token.Span));
                int matchingEnd = FindMatchingRegionEnd(tokens, index, cancellationToken);
                SclToken last = matchingEnd >= 0 ? tokens[matchingEnd] : token;
                Current(root, regions).Add(new SclUnparsedSyntax(
                    Span(token, last), SclRecoveryBoundary.RegionBoundary));
                if (matchingEnd >= 0) index = matchingEnd;
                continue;
            }
            regions.Push(new RegionBuilder(label, token.Span));
            continue;
        }

        if (IsKeyword(token, "END_REGION"))
        {
            FlushStatement(statement, Current(root, regions), diagnostics, SclRecoveryBoundary.RegionBoundary, token.Span.StartOffset);
            CloseRegion(root, regions, token, diagnostics);
            continue;
        }

        if (token.Kind == SclTokenKind.Comment)
        {
            Current(root, regions).Add(new SclCommentSyntax(token));
            continue;
        }

        statement.Add(token);
        if (token.Kind == SclTokenKind.Semicolon)
            FlushStatement(statement, Current(root, regions), diagnostics, SclRecoveryBoundary.Semicolon, token.Span.StartOffset);
    }

    FlushStatement(statement, Current(root, regions), diagnostics, SclRecoveryBoundary.BlockTerminator, blockEndOffset);
    while (regions.Count > 0) CloseUnterminatedRegion(root, regions, diagnostics, blockEndOffset);
    return root;
}
```

Add these complete helpers used by `ParseBody`:

```csharp
private static List<SclBodySyntax> Current(
    List<SclBodySyntax> root,
    Stack<RegionBuilder> regions) =>
    regions.Count == 0 ? root : regions.Peek().Children;

private static void FlushStatement(
    List<SclToken> statement,
    List<SclBodySyntax> destination,
    List<SclDiagnostic> diagnostics,
    SclRecoveryBoundary boundary,
    int boundaryOffset)
{
    if (statement.Count == 0) return;
    bool terminated = statement[statement.Count - 1].Kind == SclTokenKind.Semicolon;
    if (terminated && IsBalanced(statement))
    {
        destination.Add(new SclStatementSyntax(Span(statement), statement));
    }
    else
    {
        SclSourceSpan recovery = SpanToBoundary(statement[0], boundaryOffset);
        destination.Add(new SclUnparsedSyntax(recovery, boundary));
        diagnostics.Add(new SclDiagnostic(
            "SCL2006",
            "SCL parser recovered at " + boundary + ".",
            recovery));
    }
    statement.Clear();
}

private static bool IsBalanced(IReadOnlyList<SclToken> tokens)
{
    int parentheses = 0;
    int brackets = 0;
    foreach (SclToken token in tokens)
    {
        if (token.Kind == SclTokenKind.OpenParenthesis) parentheses++;
        else if (token.Kind == SclTokenKind.CloseParenthesis) parentheses--;
        else if (token.Kind == SclTokenKind.OpenBracket) brackets++;
        else if (token.Kind == SclTokenKind.CloseBracket) brackets--;
        if (parentheses < 0 || brackets < 0) return false;
    }
    return parentheses == 0 && brackets == 0;
}

private static SclSourceSpan SpanToBoundary(SclToken first, int boundaryOffset)
{
    int end = Math.Max(first.Span.StartOffset, boundaryOffset);
    return new SclSourceSpan(
        first.Span.StartOffset,
        end - first.Span.StartOffset,
        first.Span.Line,
        first.Span.Column);
}

private static string ReadRegionLabel(
    IReadOnlyList<SclToken> tokens,
    ref int index,
    List<SclDiagnostic> diagnostics)
{
    int candidate = index + 1;
    if (candidate < tokens.Count &&
        (tokens[candidate].Kind == SclTokenKind.Identifier ||
         tokens[candidate].Kind == SclTokenKind.QuotedIdentifier ||
         tokens[candidate].Kind == SclTokenKind.StringLiteral))
    {
        index = candidate;
        return Unquote(tokens[candidate].Text);
    }
    diagnostics.Add(new SclDiagnostic("SCL2009", "REGION label is missing.", tokens[index].Span));
    return string.Empty;
}

private static void CloseRegion(
    List<SclBodySyntax> root,
    Stack<RegionBuilder> regions,
    SclToken endToken,
    List<SclDiagnostic> diagnostics)
{
    if (regions.Count == 0)
    {
        diagnostics.Add(new SclDiagnostic("SCL2007", "END_REGION has no matching REGION.", endToken.Span));
        root.Add(new SclUnparsedSyntax(endToken.Span, SclRecoveryBoundary.RegionBoundary));
        return;
    }
    RegionBuilder completed = regions.Pop();
    Current(root, regions).Add(completed.Build(endToken.Span.EndOffset));
}

private static void CloseUnterminatedRegion(
    List<SclBodySyntax> root,
    Stack<RegionBuilder> regions,
    List<SclDiagnostic> diagnostics,
    int blockEndOffset)
{
    RegionBuilder incomplete = regions.Pop();
    diagnostics.Add(new SclDiagnostic("SCL2007", "REGION terminator is missing.", incomplete.Start));
    Current(root, regions).Add(incomplete.Build(blockEndOffset));
}

private static int FindMatchingRegionEnd(
    IReadOnlyList<SclToken> tokens,
    int labelIndex,
    CancellationToken cancellationToken)
{
    int depth = 1;
    for (int index = labelIndex + 1; index < tokens.Count; index++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsKeyword(tokens[index], "REGION")) depth++;
        else if (IsKeyword(tokens[index], "END_REGION") && --depth == 0) return index;
    }
    return -1;
}

private sealed class RegionBuilder
{
    public RegionBuilder(string label, SclSourceSpan start)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Start = start ?? throw new ArgumentNullException(nameof(start));
        Children = new List<SclBodySyntax>();
    }

    public string Label { get; }
    public SclSourceSpan Start { get; }
    public List<SclBodySyntax> Children { get; }

    public SclRegionSyntax Build(int endOffset) =>
        new SclRegionSyntax(
            Label,
            new SclSourceSpan(
                Start.StartOffset,
                Math.Max(0, endOffset - Start.StartOffset),
                Start.Line,
                Start.Column),
            Children);
}
```

`RegionBuilder` never mutates an already-published syntax node: it is private construction state, and `SclRegionSyntax` defensively copies `Children` only when the region closes.

Replace Task 2's `ParseBlocks`, `ParseDeclarationSections`, and `ParseDeclarations` methods with these final recovery-aware methods:

```csharp
private static IReadOnlyList<SclBlockSyntax> ParseBlocks(
    string source,
    IReadOnlyList<SclToken> tokens,
    SclParserLimits limits,
    List<SclDiagnostic> diagnostics,
    CancellationToken cancellationToken)
{
    var blocks = new List<SclBlockSyntax>();
    int index = 0;
    while (index < tokens.Count && tokens[index].Kind != SclTokenKind.EndOfFile)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SclToken start = tokens[index];
        if (start.Kind != SclTokenKind.Keyword || !BlockStarts.TryGetValue(start.Text, out SclBlockKind kind))
        {
            index++;
            continue;
        }

        int nameIndex = FindBlockName(tokens, index + 1, start.Span.Line);
        string name = nameIndex >= 0 ? Unquote(tokens[nameIndex].Text) : string.Empty;
        if (nameIndex < 0)
            diagnostics.Add(new SclDiagnostic("SCL2002", "SCL block name is missing.", start.Span));

        int terminatorIndex = FindKeyword(tokens, index + 1, BlockEnds[kind]);
        bool hasTerminator = terminatorIndex >= 0;
        if (!hasTerminator)
        {
            diagnostics.Add(new SclDiagnostic("SCL2001", "SCL block terminator is missing.", start.Span));
            terminatorIndex = LastContentTokenIndex(tokens);
        }
        if (terminatorIndex < index) terminatorIndex = index;

        int contentEndExclusive = hasTerminator ? terminatorIndex : EndOfFileIndex(tokens);
        int blockEndOffset = hasTerminator ? tokens[terminatorIndex].Span.StartOffset : source.Length;
        int contentStart = nameIndex >= 0 ? nameIndex + 1 : index + 1;
        ParseBlockContent(
            tokens,
            contentStart,
            contentEndExclusive,
            nameIndex >= 0 ? tokens[nameIndex].Span.Line : start.Span.Line,
            blockEndOffset,
            limits,
            diagnostics,
            cancellationToken,
            out IReadOnlyList<SclDeclarationSectionSyntax> sections,
            out IReadOnlyList<SclBodySyntax> body);
        blocks.Add(new SclBlockSyntax(
            kind,
            name,
            Span(tokens[index], tokens[terminatorIndex]),
            sections,
            body));
        index = hasTerminator ? terminatorIndex + 1 : tokens.Count;
    }
    return blocks;
}

private static void ParseBlockContent(
    IReadOnlyList<SclToken> tokens,
    int startIndex,
    int endExclusive,
    int headerLine,
    int blockEndOffset,
    SclParserLimits limits,
    List<SclDiagnostic> diagnostics,
    CancellationToken cancellationToken,
    out IReadOnlyList<SclDeclarationSectionSyntax> sections,
    out IReadOnlyList<SclBodySyntax> body)
{
    var parsedSections = new List<SclDeclarationSectionSyntax>();
    var bodyTokens = new List<SclToken>();
    int index = startIndex;
    while (index < endExclusive && tokens[index].Span.Line == headerLine) index++;
    while (index < endExclusive)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsKeyword(tokens[index], "BEGIN"))
        {
            index++;
            continue;
        }
        if (tokens[index].Kind == SclTokenKind.Keyword && SectionStarts.ContainsKey(tokens[index].Text))
        {
            SectionParse section = ParseDeclarationSection(
                tokens, index, endExclusive, blockEndOffset, diagnostics, cancellationToken);
            parsedSections.Add(section.Section);
            index = section.NextIndex;
            continue;
        }
        bodyTokens.Add(tokens[index++]);
    }
    sections = parsedSections;
    body = ParseBody(bodyTokens, limits, diagnostics, blockEndOffset);
}

private static SectionParse ParseDeclarationSection(
    IReadOnlyList<SclToken> tokens,
    int sectionStart,
    int blockEndExclusive,
    int blockEndOffset,
    List<SclDiagnostic> diagnostics,
    CancellationToken cancellationToken)
{
    SclToken start = tokens[sectionStart];
    SclDeclarationSectionKind kind = SectionStarts[start.Text];
    int endVar = FindKeyword(tokens, sectionStart + 1, "END_VAR", blockEndExclusive);
    bool hasEndVar = endVar >= 0;
    int contentEnd = hasEndVar ? endVar : blockEndExclusive;
    if (!hasEndVar)
        diagnostics.Add(new SclDiagnostic("SCL2004", "Declaration section terminator is missing.", start.Span));

    IReadOnlyList<string> modifiers = ReadSectionModifiers(tokens, sectionStart, contentEnd);
    int declarationStart = FirstDeclarationIndex(tokens, sectionStart + 1, contentEnd);
    var declarations = new List<SclDeclarationSyntax>();
    var unparsed = new List<SclUnparsedSyntax>();
    var segment = new List<SclToken>();
    for (int index = declarationStart; index < contentEnd; index++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SclToken token = tokens[index];
        if (token.Kind == SclTokenKind.Comment) continue;
        segment.Add(token);
        if (token.Kind != SclTokenKind.Semicolon) continue;
        if (IsBalanced(segment)) declarations.AddRange(ParseDeclarationSegment(segment, modifiers, diagnostics));
        else AddDeclarationRecovery(segment, token.Span.StartOffset, SclRecoveryBoundary.Semicolon, unparsed, diagnostics);
        segment.Clear();
    }

    if (segment.Count > 0)
    {
        int boundaryOffset = hasEndVar ? tokens[endVar].Span.StartOffset : blockEndOffset;
        AddDeclarationRecovery(
            segment,
            boundaryOffset,
            SclRecoveryBoundary.DeclarationTerminator,
            unparsed,
            diagnostics);
    }

    SclToken last = hasEndVar
        ? tokens[endVar]
        : tokens[Math.Max(sectionStart, blockEndExclusive - 1)];
    var section = new SclDeclarationSectionSyntax(
        kind,
        modifiers,
        Span(start, last),
        declarations,
        unparsed);
    return new SectionParse(section, hasEndVar ? endVar + 1 : blockEndExclusive);
}

private static void AddDeclarationRecovery(
    IReadOnlyList<SclToken> segment,
    int boundaryOffset,
    SclRecoveryBoundary boundary,
    List<SclUnparsedSyntax> unparsed,
    List<SclDiagnostic> diagnostics)
{
    SclSourceSpan span = SpanToBoundary(segment[0], boundaryOffset);
    unparsed.Add(new SclUnparsedSyntax(span, boundary));
    diagnostics.Add(new SclDiagnostic(
        "SCL2006",
        "SCL parser recovered at " + boundary + ".",
        span));
}

private static int EndOfFileIndex(IReadOnlyList<SclToken> tokens)
{
    for (int index = 0; index < tokens.Count; index++)
        if (tokens[index].Kind == SclTokenKind.EndOfFile) return index;
    return tokens.Count;
}

private sealed class SectionParse
{
    public SectionParse(SclDeclarationSectionSyntax section, int nextIndex)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
        NextIndex = nextIndex;
    }
    public SclDeclarationSectionSyntax Section { get; }
    public int NextIndex { get; }
}
```

- [ ] **Step 4: Make recovery boundary selection explicit**

Add and use this boundary selector; do not skip over an earlier approved boundary:

```csharp
private static RecoveryPoint FindRecoveryPoint(IReadOnlyList<SclToken> tokens, int startIndex, string blockEnd)
{
    for (int index = startIndex; index < tokens.Count; index++)
    {
        SclToken token = tokens[index];
        if (token.Kind == SclTokenKind.Semicolon)
            return new RecoveryPoint(index, token.Span.StartOffset, SclRecoveryBoundary.Semicolon);
        if (IsKeyword(token, "REGION") || IsKeyword(token, "END_REGION"))
            return new RecoveryPoint(index, token.Span.StartOffset, SclRecoveryBoundary.RegionBoundary);
        if (IsKeyword(token, "END_VAR"))
            return new RecoveryPoint(index, token.Span.StartOffset, SclRecoveryBoundary.DeclarationTerminator);
        if (IsKeyword(token, blockEnd))
            return new RecoveryPoint(index, token.Span.StartOffset, SclRecoveryBoundary.BlockTerminator);
    }

    SclToken eof = tokens[tokens.Count - 1];
    return new RecoveryPoint(tokens.Count - 1, eof.Span.StartOffset, SclRecoveryBoundary.BlockTerminator);
}
```

Add the exact immutable helper next to the selector:

```csharp
private sealed class RecoveryPoint
{
    public RecoveryPoint(int tokenIndex, int endOffset, SclRecoveryBoundary boundary)
    {
        if (tokenIndex < 0) throw new ArgumentOutOfRangeException(nameof(tokenIndex));
        if (endOffset < 0) throw new ArgumentOutOfRangeException(nameof(endOffset));
        TokenIndex = tokenIndex;
        EndOffset = endOffset;
        Boundary = boundary;
    }

    public int TokenIndex { get; }
    public int EndOffset { get; }
    public SclRecoveryBoundary Boundary { get; }
}
```

Cancellation is checked at the start of each block, section, body, region-skip, and bounded comparison loop iteration.

- [ ] **Step 5: Run all SCL parser tests and the exact-limit theory**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclLexerTests|FullyQualifiedName~SclParserTests|FullyQualifiedName~SclRecoveryTests"
```

Expected: PASS; each malformed case produces one exact unparsed span, later valid content survives, and N+1 nesting produces `SCL2008` without exceeding depth.

- [ ] **Step 6: Commit tolerant recovery**

```powershell
git add src/TiaGitAddIn.Core/Comparison/Scl/SclParser.cs src/TiaGitAddIn.Tests/Services/Comparison/SclRecoveryTests.cs
git commit -m "feat: add tolerant scl parser recovery"
```

### Task 4: Compare blocks, declarations, statements, regions, and comments deterministically

**Acceptance criteria:** AC-007, AC-058, AC-060, AC-064, AC-065

**Files:**
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclChanges.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclComparisonLimits.cs`
- Create: `src/TiaGitAddIn.Core/Comparison/Scl/SclComparer.cs`
- Test: `src/TiaGitAddIn.Tests/Services/Comparison/SclComparerTests.cs`

**Interfaces:**
- Consumes: two immutable `SclParseResult` instances.
- Produces: `SclComparer.Compare(SclParseResult left, SclParseResult right, CancellationToken)` using `SclComparisonLimits.Default`, plus the injectable overload `Compare(SclParseResult left, SclParseResult right, SclComparisonLimits limits, CancellationToken cancellationToken)` → immutable hierarchical `SclComparison`.
- Bounds: returns `SclComparison.ComparisonLimitExceeded == true` before excessive allocation when either sequence or estimated comparison work is above its exact cap; Task 6 converts that outcome to shared text fallback with `SCL3002`.
- Produces: right-side source order followed by left-only nodes in left-side source order; serialization order never acts as identity.

- [ ] **Step 1: Write failing semantic comparison tests**

```csharp
using System.Linq;
using System.Threading;
using TiaGitAddIn.Comparison.Scl;
using Xunit;

namespace TiaGitAddIn.Tests.Services.Comparison
{
    public sealed class SclComparerTests
    {
        [Fact]
        public void Compare_ReorderedBlocksAndDeclarations_ProducesNoSemanticChange()
        {
            const string left =
                "FUNCTION_BLOCK \"A\"\nVAR_INPUT\nX : Bool;\nY : Int;\nEND_VAR\nEND_FUNCTION_BLOCK\n" +
                "FUNCTION_BLOCK \"B\"\nEND_FUNCTION_BLOCK";
            const string right =
                "FUNCTION_BLOCK \"B\"\nEND_FUNCTION_BLOCK\n" +
                "FUNCTION_BLOCK \"A\"\nVAR_INPUT\nY : Int;\nX : Bool;\nEND_VAR\nEND_FUNCTION_BLOCK";

            SclComparison comparison = Compare(left, right);

            Assert.False(comparison.HasChanges);
            Assert.Equal(new[] { "FunctionBlock:B", "FunctionBlock:A" }, comparison.Groups.Select(group => group.Key));
        }

        [Fact]
        public void Compare_FormattingOnlyStatementChange_ProducesNoStatementChange()
        {
            const string left = "FUNCTION_BLOCK \"A\"\nREGION R\nX:=X+1;\nEND_REGION\nEND_FUNCTION_BLOCK";
            const string right = "FUNCTION_BLOCK \"A\"\r\n  REGION R\r\n    X := X + 1 ;\r\n  END_REGION\r\nEND_FUNCTION_BLOCK";

            SclComparison comparison = Compare(left, right);

            Assert.DoesNotContain(comparison.Descendants(), node =>
                node.Category == SclChangeCategory.Statement && node.Kind != SclChangeKind.Unchanged);
        }

        [Fact]
        public void Compare_CommentEdit_ReportsOnlyCommentChange()
        {
            const string left = "FUNCTION_BLOCK \"A\"\n// before\nX := 1;\nEND_FUNCTION_BLOCK";
            const string right = "FUNCTION_BLOCK \"A\"\n// after\nX := 1;\nEND_FUNCTION_BLOCK";

            SclComparison comparison = Compare(left, right);
            SclChangeNode change = Assert.Single(comparison.Descendants().Where(node =>
                node.Children.Count == 0 && node.Kind != SclChangeKind.Unchanged));

            Assert.Equal(SclChangeCategory.Comment, change.Category);
            Assert.Equal(SclChangeKind.Modified, change.Kind);
            Assert.Equal("// before", change.LeftText);
            Assert.Equal("// after", change.RightText);
        }

        [Fact]
        public void Compare_SequenceAndWorkLimits_AcceptNAndRejectNPlusOneBeforeDiffAllocation()
        {
            SclParseResult two = Parse(
                "FUNCTION_BLOCK \"A\"\nX := 1;\nY := 2;\nEND_FUNCTION_BLOCK");
            SclParseResult three = Parse(
                "FUNCTION_BLOCK \"A\"\nX := 1;\nY := 2;\nZ := 3;\nEND_FUNCTION_BLOCK");

            SclComparison atSequenceLimit = SclComparer.Compare(
                two, two, new SclComparisonLimits(maxSequenceItems: 2, maxWorkItems: 8), CancellationToken.None);
            SclComparison aboveSequenceLimit = SclComparer.Compare(
                three, three, new SclComparisonLimits(maxSequenceItems: 2, maxWorkItems: 18), CancellationToken.None);
            SclComparison atWorkLimit = SclComparer.Compare(
                two, two, new SclComparisonLimits(maxSequenceItems: 2, maxWorkItems: 8), CancellationToken.None);
            SclComparison aboveWorkLimit = SclComparer.Compare(
                two, two, new SclComparisonLimits(maxSequenceItems: 2, maxWorkItems: 7), CancellationToken.None);

            Assert.False(atSequenceLimit.ComparisonLimitExceeded);
            Assert.True(aboveSequenceLimit.ComparisonLimitExceeded);
            Assert.False(atWorkLimit.ComparisonLimitExceeded);
            Assert.True(aboveWorkLimit.ComparisonLimitExceeded);
        }

        private static SclComparison Compare(string left, string right)
        {
            SclParseResult leftTree = Parse(left);
            SclParseResult rightTree = Parse(right);
            return SclComparer.Compare(leftTree, rightTree, CancellationToken.None);
        }

        private static SclParseResult Parse(string source) =>
            SclParser.Parse(source, SclParserLimits.Default, CancellationToken.None);
    }
}
```

- [ ] **Step 2: Run the comparer tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclComparerTests"
```

Expected: FAIL at compile time because `SclComparison`, `SclChangeNode`, and `SclComparer` do not exist.

- [ ] **Step 3: Add the immutable hierarchical change model**

Create `SclComparisonLimits.cs` first:

```csharp
using System;

namespace TiaGitAddIn.Comparison.Scl
{
    public sealed class SclComparisonLimits
    {
        public static SclComparisonLimits Default { get; } =
            new SclComparisonLimits(maxSequenceItems: 10000, maxWorkItems: 4000000);

        public SclComparisonLimits(int maxSequenceItems, long maxWorkItems)
        {
            if (maxSequenceItems < 1) throw new ArgumentOutOfRangeException(nameof(maxSequenceItems));
            if (maxWorkItems < 1) throw new ArgumentOutOfRangeException(nameof(maxWorkItems));
            MaxSequenceItems = maxSequenceItems;
            MaxWorkItems = maxWorkItems;
        }

        public int MaxSequenceItems { get; }
        public long MaxWorkItems { get; }
    }
}
```

Create `SclChanges.cs` with this exact surface and defensive copies:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Comparison.Scl
{
    public enum SclChangeKind { Unchanged, Added, Removed, Modified, Rename }
    public enum SclChangeCategory { File, Block, Region, DeclarationSection, Declaration, Statement, Comment, Ungrouped, Unparsed }

    public sealed class SclChangeNode
    {
        public SclChangeNode(
            SclChangeCategory category,
            SclChangeKind kind,
            string key,
            string? leftText,
            string? rightText,
            SclSourceSpan? leftSpan,
            SclSourceSpan? rightSpan,
            IEnumerable<SclChangeNode> children)
        {
            Category = category;
            Kind = kind;
            Key = key ?? throw new ArgumentNullException(nameof(key));
            LeftText = leftText;
            RightText = rightText;
            LeftSpan = leftSpan;
            RightSpan = rightSpan;
            Children = Array.AsReadOnly((children ?? throw new ArgumentNullException(nameof(children))).ToArray());
        }

        public SclChangeCategory Category { get; }
        public SclChangeKind Kind { get; }
        public string Key { get; }
        public string? LeftText { get; }
        public string? RightText { get; }
        public SclSourceSpan? LeftSpan { get; }
        public SclSourceSpan? RightSpan { get; }
        public IReadOnlyList<SclChangeNode> Children { get; }
    }

    public sealed class SclComparison
    {
        public SclComparison(IEnumerable<SclChangeNode> groups, bool comparisonLimitExceeded = false)
        {
            Groups = Array.AsReadOnly((groups ?? throw new ArgumentNullException(nameof(groups))).ToArray());
            ComparisonLimitExceeded = comparisonLimitExceeded;
        }

        public IReadOnlyList<SclChangeNode> Groups { get; }
        public bool ComparisonLimitExceeded { get; }
        public bool HasChanges => Descendants().Any(node => node.Children.Count == 0 && node.Kind != SclChangeKind.Unchanged);

        public static SclComparison LimitExceeded() =>
            new SclComparison(Array.Empty<SclChangeNode>(), comparisonLimitExceeded: true);

        public IEnumerable<SclChangeNode> Descendants()
        {
            foreach (SclChangeNode group in Groups)
                foreach (SclChangeNode node in Flatten(group)) yield return node;
        }

        private static IEnumerable<SclChangeNode> Flatten(SclChangeNode node)
        {
            yield return node;
            foreach (SclChangeNode child in node.Children)
                foreach (SclChangeNode descendant in Flatten(child)) yield return descendant;
        }
    }
}
```

- [ ] **Step 4: Implement semantic matching and token-sequence comparison**

Implement `SclComparer` as a stateless static class. Use these exact identity and normalization helpers:

```csharp
private static string BlockKey(SclBlockSyntax block) => block.Kind + ":" + block.Name;
private static string DeclarationKey(SclDeclarationSectionKind section, SclDeclarationSyntax declaration) =>
    section + ":" + declaration.Identifier;
private static string RegionKey(string parentPath, SclRegionSyntax region) => parentPath + "/Region:" + region.Label;

private static string NormalizeTokens(IEnumerable<SclToken> tokens)
{
    return string.Join("\u001f", tokens
        .Where(token => token.Kind != SclTokenKind.Comment && token.Kind != SclTokenKind.EndOfFile)
        .Select(token => token.Kind + "=" + token.Text));
}

private static string NormalizeComment(string value)
{
    string normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
    return string.Join("\n", normalized.Split('\n').Select(line => line.TrimEnd()));
}
```

Add the complete top-level and hierarchy implementation below. It emits right-side source order followed by left-only nodes, treats duplicate semantic keys as deterministic `SCL3001` unparsed leaves, and funnels every ordinal sequence through the shared budget:

```csharp
public static SclComparison Compare(
    SclParseResult left,
    SclParseResult right,
    CancellationToken cancellationToken) =>
    Compare(left, right, SclComparisonLimits.Default, cancellationToken);

public static SclComparison Compare(
    SclParseResult left,
    SclParseResult right,
    SclComparisonLimits limits,
    CancellationToken cancellationToken)
{
    if (left == null) throw new ArgumentNullException(nameof(left));
    if (right == null) throw new ArgumentNullException(nameof(right));
    if (limits == null) throw new ArgumentNullException(nameof(limits));
    var budget = new ComparisonBudget(limits);
    try
    {
        return new SclComparison(CompareBlocks(
            left.Document.Blocks, right.Document.Blocks, budget, cancellationToken));
    }
    catch (ComparisonLimitException)
    {
        return SclComparison.LimitExceeded();
    }
}

private static IReadOnlyList<SclChangeNode> CompareBlocks(
    IReadOnlyList<SclBlockSyntax> left,
    IReadOnlyList<SclBlockSyntax> right,
    ComparisonBudget budget,
    CancellationToken cancellationToken)
{
    budget.EnsureSequence(left.Count, right.Count);
    var leftBuckets = left.GroupBy(BlockKey, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    var rightBuckets = right.GroupBy(BlockKey, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    var consumedLeft = new HashSet<SclBlockSyntax>();
    var emittedKeys = new HashSet<string>(StringComparer.Ordinal);
    var nodes = new List<SclChangeNode>();
    foreach (SclBlockSyntax rightBlock in right)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string key = BlockKey(rightBlock);
        if (!emittedKeys.Add(key)) continue;
        SclBlockSyntax[] rightMatches = rightBuckets[key];
        leftBuckets.TryGetValue(key, out SclBlockSyntax[] leftMatches);
        leftMatches = leftMatches ?? Array.Empty<SclBlockSyntax>();
        foreach (SclBlockSyntax item in leftMatches) consumedLeft.Add(item);
        if (rightMatches.Length != 1 || leftMatches.Length > 1)
            nodes.Add(DuplicateNode(key, leftMatches, rightMatches));
        else if (leftMatches.Length == 0)
            nodes.Add(BlockNode(null, rightBlock, Array.Empty<SclChangeNode>()));
        else
            nodes.Add(CompareBlock(leftMatches[0], rightBlock, budget, cancellationToken));
    }
    foreach (SclBlockSyntax leftBlock in left.Where(item => !consumedLeft.Contains(item)))
        nodes.Add(BlockNode(leftBlock, null, Array.Empty<SclChangeNode>()));
    return nodes;
}

private static SclChangeNode CompareBlock(
    SclBlockSyntax left,
    SclBlockSyntax right,
    ComparisonBudget budget,
    CancellationToken cancellationToken)
{
    var children = new List<SclChangeNode>();
    children.AddRange(CompareSections(left, right, budget, cancellationToken));
    children.AddRange(CompareBody(left.Body, right.Body, BlockKey(right), budget, cancellationToken));
    return BlockNode(left, right, children);
}

private static IReadOnlyList<SclChangeNode> CompareSections(
    SclBlockSyntax left,
    SclBlockSyntax right,
    ComparisonBudget budget,
    CancellationToken cancellationToken)
{
    SclDeclarationSectionKind[] rightKinds = right.DeclarationSections.Select(item => item.Kind).Distinct().ToArray();
    SclDeclarationSectionKind[] leftKinds = left.DeclarationSections.Select(item => item.Kind).Distinct().ToArray();
    budget.EnsureSequence(leftKinds.Length, rightKinds.Length);
    var nodes = new List<SclChangeNode>();
    foreach (SclDeclarationSectionKind kind in rightKinds)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SclDeclarationSectionSyntax[] leftSections = left.DeclarationSections.Where(item => item.Kind == kind).ToArray();
        SclDeclarationSectionSyntax[] rightSections = right.DeclarationSections.Where(item => item.Kind == kind).ToArray();
        IReadOnlyList<SclChangeNode> declarations = CompareDeclarations(
            kind,
            leftSections.SelectMany(item => item.Declarations).ToArray(),
            rightSections.SelectMany(item => item.Declarations).ToArray(),
            budget,
            cancellationToken);
        nodes.Add(SectionNode(kind, leftSections, rightSections, declarations));
    }
    foreach (SclDeclarationSectionKind kind in leftKinds.Where(kind => !rightKinds.Contains(kind)))
    {
        SclDeclarationSectionSyntax[] sections = left.DeclarationSections.Where(item => item.Kind == kind).ToArray();
        nodes.Add(SectionNode(kind, sections, Array.Empty<SclDeclarationSectionSyntax>(),
            sections.SelectMany(item => item.Declarations).Select(item => DeclarationNode(kind, item, null)).ToArray()));
    }
    return nodes;
}

private static IReadOnlyList<SclChangeNode> CompareDeclarations(
    SclDeclarationSectionKind section,
    IReadOnlyList<SclDeclarationSyntax> left,
    IReadOnlyList<SclDeclarationSyntax> right,
    ComparisonBudget budget,
    CancellationToken cancellationToken)
{
    budget.EnsureSequence(left.Count, right.Count);
    var leftBuckets = left.GroupBy(item => DeclarationKey(section, item), StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    var consumed = new HashSet<SclDeclarationSyntax>();
    var emitted = new HashSet<string>(StringComparer.Ordinal);
    var nodes = new List<SclChangeNode>();
    foreach (SclDeclarationSyntax rightItem in right)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string key = DeclarationKey(section, rightItem);
        if (!emitted.Add(key)) continue;
        leftBuckets.TryGetValue(key, out SclDeclarationSyntax[] leftMatches);
        leftMatches = leftMatches ?? Array.Empty<SclDeclarationSyntax>();
        SclDeclarationSyntax[] rightMatches = right.Where(item => DeclarationKey(section, item) == key).ToArray();
        foreach (SclDeclarationSyntax item in leftMatches) consumed.Add(item);
        if (leftMatches.Length > 1 || rightMatches.Length > 1)
            nodes.Add(DuplicateNode(key, leftMatches.Select(item => item.Span), rightMatches.Select(item => item.Span)));
        else if (leftMatches.Length == 0)
            nodes.Add(DeclarationNode(section, null, rightItem));
        else
            nodes.Add(DeclarationNode(section, leftMatches[0], rightItem));
    }
    foreach (SclDeclarationSyntax leftItem in left.Where(item => !consumed.Contains(item)))
        nodes.Add(DeclarationNode(section, leftItem, null));
    return nodes;
}

private static IReadOnlyList<SclChangeNode> CompareBody(
    IReadOnlyList<SclBodySyntax> left,
    IReadOnlyList<SclBodySyntax> right,
    string parentPath,
    ComparisonBudget budget,
    CancellationToken cancellationToken)
{
    budget.EnsureSequence(left.Count, right.Count);
    var nodes = new List<SclChangeNode>();
    IReadOnlyList<SclBodySyntax> leftDirect = left.Where(item => !(item is SclRegionSyntax)).ToArray();
    IReadOnlyList<SclBodySyntax> rightDirect = right.Where(item => !(item is SclRegionSyntax)).ToArray();
    bool directEmitted = false;
    var consumedRegions = new HashSet<SclRegionSyntax>();
    foreach (SclBodySyntax rightNode in right)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!(rightNode is SclRegionSyntax rightRegion))
        {
            if (!directEmitted)
            {
                nodes.Add(CompareUngrouped(leftDirect, rightDirect, parentPath, budget, cancellationToken));
                directEmitted = true;
            }
            continue;
        }
        SclRegionSyntax[] matches = left.OfType<SclRegionSyntax>()
            .Where(item => string.Equals(item.Label, rightRegion.Label, StringComparison.Ordinal)).ToArray();
        foreach (SclRegionSyntax match in matches) consumedRegions.Add(match);
        nodes.Add(matches.Length == 1
            ? CompareRegion(matches[0], rightRegion, parentPath, budget, cancellationToken)
            : matches.Length == 0
                ? RegionNode(null, rightRegion, parentPath, Array.Empty<SclChangeNode>())
                : DuplicateNode(RegionKey(parentPath, rightRegion), matches.Select(item => item.Span), new[] { rightRegion.Span }));
    }
    if (!directEmitted && (leftDirect.Count > 0 || rightDirect.Count > 0))
        nodes.Add(CompareUngrouped(leftDirect, rightDirect, parentPath, budget, cancellationToken));
    foreach (SclRegionSyntax leftRegion in left.OfType<SclRegionSyntax>().Where(item => !consumedRegions.Contains(item)))
        nodes.Add(RegionNode(leftRegion, null, parentPath, Array.Empty<SclChangeNode>()));
    return nodes;
}

private static SclChangeNode CompareRegion(
    SclRegionSyntax left,
    SclRegionSyntax right,
    string parentPath,
    ComparisonBudget budget,
    CancellationToken cancellationToken)
{
    string path = RegionKey(parentPath, right);
    IReadOnlyList<SclChangeNode> children = CompareBody(left.Children, right.Children, path, budget, cancellationToken);
    return RegionNode(left, right, parentPath, children);
}

private static SclChangeNode CompareUngrouped(
    IReadOnlyList<SclBodySyntax> left,
    IReadOnlyList<SclBodySyntax> right,
    string parentPath,
    ComparisonBudget budget,
    CancellationToken cancellationToken)
{
    var children = new List<SclChangeNode>();
    children.AddRange(CompareSequence(
        left.OfType<SclStatementSyntax>().ToArray(), right.OfType<SclStatementSyntax>().ToArray(),
        item => NormalizeTokens(item.Tokens), item => TokenText(item.Tokens),
        item => item.Span, SclChangeCategory.Statement, budget, cancellationToken));
    children.AddRange(CompareSequence(
        left.OfType<SclCommentSyntax>().ToArray(), right.OfType<SclCommentSyntax>().ToArray(),
        item => NormalizeComment(item.Token.Text), item => item.Token.Text,
        item => item.Span, SclChangeCategory.Comment, budget, cancellationToken));
    children.AddRange(right.OfType<SclUnparsedSyntax>().Select(item => UnparsedNode(null, item)));
    children.AddRange(left.OfType<SclUnparsedSyntax>().Select(item => UnparsedNode(item, null)));
    SclChangeNode[] ordered = children.Where(item => item.RightSpan != null)
        .OrderBy(item => item.RightSpan!.StartOffset)
        .Concat(children.Where(item => item.RightSpan == null).OrderBy(item => item.LeftSpan!.StartOffset))
        .ToArray();
    return new SclChangeNode(
        SclChangeCategory.Ungrouped,
        ParentKind(ordered),
        parentPath + "/Ungrouped",
        null,
        null,
        MergeSpans(left.Select(item => item.Span)),
        MergeSpans(right.Select(item => item.Span)),
        ordered);
}
```

Create one `ComparisonBudget` per `Compare` invocation and pass it to every statement/comment sequence match. `Reserve` checks sequence size and the conservative Hirschberg work estimate before allocating score rows:

```csharp
private sealed class ComparisonBudget
{
    private readonly int maxSequenceItems;
    private long remainingWorkItems;

    public ComparisonBudget(SclComparisonLimits limits)
    {
        maxSequenceItems = limits.MaxSequenceItems;
        remainingWorkItems = limits.MaxWorkItems;
    }

    public bool Reserve(int leftCount, int rightCount)
    {
        if (leftCount > maxSequenceItems || rightCount > maxSequenceItems) return false;
        long product = (long)leftCount * rightCount;
        if (product > remainingWorkItems / 2L) return false;
        remainingWorkItems -= product * 2L;
        return true;
    }
}

private static bool TryLongestCommonSubsequence(
    IReadOnlyList<string> left,
    IReadOnlyList<string> right,
    ComparisonBudget budget,
    CancellationToken cancellationToken,
    out IReadOnlyList<Tuple<int, int>> pairs)
{
    if (!budget.Reserve(left.Count, right.Count))
    {
        pairs = Array.Empty<Tuple<int, int>>();
        return false;
    }

    var result = new List<Tuple<int, int>>();
    if (right.Count <= left.Count)
    {
        BuildLcs(left, 0, left.Count, right, 0, right.Count, result, cancellationToken);
        pairs = result;
        return true;
    }

    var swapped = new List<Tuple<int, int>>();
    BuildLcs(right, 0, right.Count, left, 0, left.Count, swapped, cancellationToken);
    pairs = swapped.Select(pair => Tuple.Create(pair.Item2, pair.Item1))
        .OrderBy(pair => pair.Item1)
        .ThenBy(pair => pair.Item2)
        .ToArray();
    return true;
}

private static void BuildLcs(
    IReadOnlyList<string> left,
    int leftStart,
    int leftCount,
    IReadOnlyList<string> right,
    int rightStart,
    int rightCount,
    List<Tuple<int, int>> pairs,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (leftCount == 0 || rightCount == 0) return;
    if (leftCount == 1)
    {
        for (int index = 0; index < rightCount; index++)
        {
            if (!string.Equals(left[leftStart], right[rightStart + index], StringComparison.Ordinal)) continue;
            pairs.Add(Tuple.Create(leftStart, rightStart + index));
            return;
        }
        return;
    }

    int leftHalf = leftCount / 2;
    int rightSplit = FindRightSplit(
        left, leftStart, leftHalf, leftCount - leftHalf,
        right, rightStart, rightCount, cancellationToken);
    BuildLcs(left, leftStart, leftHalf, right, rightStart, rightSplit, pairs, cancellationToken);
    BuildLcs(left, leftStart + leftHalf, leftCount - leftHalf,
        right, rightStart + rightSplit, rightCount - rightSplit, pairs, cancellationToken);
}

private static int FindRightSplit(
    IReadOnlyList<string> left,
    int leftStart,
    int leftCount,
    int rightLeftCount,
    IReadOnlyList<string> right,
    int rightStart,
    int rightCount,
    CancellationToken cancellationToken)
{
    int[] prefix = PrefixScores(left, leftStart, leftCount, right, rightStart, rightCount, cancellationToken);
    int[] suffix = SuffixScores(left, leftStart + leftCount, rightLeftCount, right, rightStart, rightCount, cancellationToken);
    int bestSplit = 0;
    int bestScore = -1;
    for (int split = 0; split <= rightCount; split++)
    {
        int score = prefix[split] + suffix[split];
        if (score > bestScore)
        {
            bestScore = score;
            bestSplit = split;
        }
    }
    return bestSplit;
}

private static int[] PrefixScores(
    IReadOnlyList<string> left, int leftStart, int leftCount,
    IReadOnlyList<string> right, int rightStart, int rightCount,
    CancellationToken cancellationToken)
{
    var previous = new int[rightCount + 1];
    var current = new int[rightCount + 1];
    for (int leftOffset = 0; leftOffset < leftCount; leftOffset++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        for (int rightOffset = 1; rightOffset <= rightCount; rightOffset++)
            current[rightOffset] = string.Equals(
                left[leftStart + leftOffset], right[rightStart + rightOffset - 1], StringComparison.Ordinal)
                ? previous[rightOffset - 1] + 1
                : Math.Max(previous[rightOffset], current[rightOffset - 1]);
        int[] swap = previous; previous = current; current = swap;
        Array.Clear(current, 0, current.Length);
    }
    return previous;
}

private static int[] SuffixScores(
    IReadOnlyList<string> left, int leftStart, int leftCount,
    IReadOnlyList<string> right, int rightStart, int rightCount,
    CancellationToken cancellationToken)
{
    var previous = new int[rightCount + 1];
    var current = new int[rightCount + 1];
    for (int leftOffset = leftCount - 1; leftOffset >= 0; leftOffset--)
    {
        cancellationToken.ThrowIfCancellationRequested();
        for (int rightOffset = rightCount - 1; rightOffset >= 0; rightOffset--)
            current[rightOffset] = string.Equals(
                left[leftStart + leftOffset], right[rightStart + rightOffset], StringComparison.Ordinal)
                ? previous[rightOffset + 1] + 1
                : Math.Max(previous[rightOffset], current[rightOffset + 1]);
        int[] swap = previous; previous = current; current = swap;
        Array.Clear(current, 0, current.Length);
    }
    return previous;
}
```

The top-level overload creates `ComparisonBudget(limits)`, catches no cancellation, and returns `SclComparison.LimitExceeded()` immediately when any `TryLongestCommonSubsequence` call returns false. Unmatched left/right statements become Removed/Added. Adjacent unmatched left/right runs of equal length become Modified pairs only when both occupy the same group and ordinal gap; this avoids global rename-like inference. Comments use the same local pairing but stay `SclChangeCategory.Comment`. Tie-breaking in `FindRightSplit` retains the lowest right split, and the one-item base case retains the first right match, so repeated statements remain deterministic.

- [ ] **Step 5: Run comparer and parser suites**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclComparerTests|FullyQualifiedName~SclParserTests|FullyQualifiedName~SclRecoveryTests"
```

Expected: PASS; reorder and whitespace cases have zero changed leaves, while the comment edit has exactly one comment leaf.

- [ ] **Step 6: Commit deterministic comparison**

```powershell
git add src/TiaGitAddIn.Core/Comparison/Scl/SclChanges.cs src/TiaGitAddIn.Core/Comparison/Scl/SclComparisonLimits.cs src/TiaGitAddIn.Core/Comparison/Scl/SclComparer.cs src/TiaGitAddIn.Tests/Services/Comparison/SclComparerTests.cs
git commit -m "feat: compare scl structure"
```

### Task 5: Infer only unique declaration renames

**Acceptance criteria:** AC-061, AC-062, AC-063

**Files:**
- Modify: `src/TiaGitAddIn.Core/Comparison/Scl/SclComparer.cs`
- Modify: `src/TiaGitAddIn.Tests/Services/Comparison/SclComparerTests.cs`

**Interfaces:**
- Consumes: unmatched Added/Removed declarations from Task 4.
- Produces: `SclChangeKind.Rename` only for a one-to-one fingerprint within the same declaration section.
- Excludes: block-name and region-label inference by construction.

- [ ] **Step 1: Add failing unique, ambiguous, block, and region rename tests**

```csharp
[Fact]
public void Compare_OneUniqueDeclarationFingerprint_ProducesOneRename()
{
    const string left = "FUNCTION_BLOCK \"A\"\nVAR_INPUT\nOldName : DInt := 7;\nEND_VAR\nEND_FUNCTION_BLOCK";
    const string right = "FUNCTION_BLOCK \"A\"\nVAR_INPUT\nNewName : DInt := 7;\nEND_VAR\nEND_FUNCTION_BLOCK";

    SclChangeNode rename = Assert.Single(Compare(left, right).Descendants().Where(node => node.Kind == SclChangeKind.Rename));

    Assert.Equal(SclChangeCategory.Declaration, rename.Category);
    Assert.Equal("OldName", rename.LeftText);
    Assert.Equal("NewName", rename.RightText);
    Assert.DoesNotContain(Compare(left, right).Descendants(), node =>
        node.Category == SclChangeCategory.Declaration && (node.Kind == SclChangeKind.Added || node.Kind == SclChangeKind.Removed));
}

[Fact]
public void Compare_AmbiguousDeclarationFingerprints_RemainAddedAndRemoved()
{
    const string left = "FUNCTION_BLOCK \"A\"\nVAR_INPUT\nOld1 : DInt;\nOld2 : DInt;\nEND_VAR\nEND_FUNCTION_BLOCK";
    const string right = "FUNCTION_BLOCK \"A\"\nVAR_INPUT\nNew1 : DInt;\nNew2 : DInt;\nEND_VAR\nEND_FUNCTION_BLOCK";

    SclComparison result = Compare(left, right);

    Assert.DoesNotContain(result.Descendants(), node => node.Kind == SclChangeKind.Rename);
    Assert.Equal(2, result.Descendants().Count(node => node.Category == SclChangeCategory.Declaration && node.Kind == SclChangeKind.Added));
    Assert.Equal(2, result.Descendants().Count(node => node.Category == SclChangeCategory.Declaration && node.Kind == SclChangeKind.Removed));
}

[Theory]
[InlineData("FUNCTION_BLOCK \"Old\"\nX := 1;\nEND_FUNCTION_BLOCK", "FUNCTION_BLOCK \"New\"\nX := 1;\nEND_FUNCTION_BLOCK", SclChangeCategory.Block)]
[InlineData("FUNCTION_BLOCK \"A\"\nREGION Old\nX := 1;\nEND_REGION\nEND_FUNCTION_BLOCK", "FUNCTION_BLOCK \"A\"\nREGION New\nX := 1;\nEND_REGION\nEND_FUNCTION_BLOCK", SclChangeCategory.Region)]
public void Compare_BlockOrRegionLabelChange_StaysAddedAndRemoved(string left, string right, SclChangeCategory category)
{
    SclComparison result = Compare(left, right);

    Assert.DoesNotContain(result.Descendants(), node => node.Kind == SclChangeKind.Rename);
    Assert.Contains(result.Descendants(), node => node.Category == category && node.Kind == SclChangeKind.Added);
    Assert.Contains(result.Descendants(), node => node.Category == category && node.Kind == SclChangeKind.Removed);
}
```

- [ ] **Step 2: Run the focused tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclComparerTests"
```

Expected: FAIL because `OldName`/`NewName` are separate Removed/Added leaves.

- [ ] **Step 3: Add the one-to-one declaration fingerprint pass**

Run this pass only inside a matched declaration section, after exact-name matching and before emitting unmatched nodes:

```csharp
private static IReadOnlyList<SclChangeNode> PairUniqueDeclarationRenames(
    SclDeclarationSectionKind section,
    IReadOnlyList<SclDeclarationSyntax> removed,
    IReadOnlyList<SclDeclarationSyntax> added,
    ISet<SclDeclarationSyntax> consumedRemoved,
    ISet<SclDeclarationSyntax> consumedAdded)
{
    var leftGroups = removed.GroupBy(item => RenameFingerprint(section, item), StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    var rightGroups = added.GroupBy(item => RenameFingerprint(section, item), StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    var renames = new List<SclChangeNode>();

    foreach (string fingerprint in leftGroups.Keys.OrderBy(value => value, StringComparer.Ordinal))
    {
        SclDeclarationSyntax[] leftMatches = leftGroups[fingerprint];
        if (leftMatches.Length != 1 || !rightGroups.TryGetValue(fingerprint, out SclDeclarationSyntax[] rightMatches) || rightMatches.Length != 1)
            continue;

        SclDeclarationSyntax left = leftMatches[0];
        SclDeclarationSyntax right = rightMatches[0];
        consumedRemoved.Add(left);
        consumedAdded.Add(right);
        renames.Add(new SclChangeNode(
            SclChangeCategory.Declaration,
            SclChangeKind.Rename,
            section + ":" + left.Identifier + "->" + right.Identifier,
            left.Identifier,
            right.Identifier,
            left.Span,
            right.Span,
            Array.Empty<SclChangeNode>()));
    }
    return renames;
}

private static string RenameFingerprint(SclDeclarationSectionKind section, SclDeclarationSyntax declaration)
{
    return section + "\u001f" + declaration.DataType + "\u001f" +
        string.Join("\u001e", declaration.Modifiers) + "\u001f" + (declaration.DefaultValue ?? string.Empty);
}
```

Use reference-equality sets scoped to one comparer invocation; do not annotate or mutate declarations. Emit renames in the corresponding right declaration position, then left-only removals. Do not call this function for blocks or regions.

- [ ] **Step 4: Run all SCL comparer tests twice to prove deterministic output**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclComparerTests"
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclComparerTests"
```

Expected: both runs PASS with identical test counts; unique declaration rename is one Rename, ambiguous candidates are two Added plus two Removed, and block/region changes contain no Rename.

- [ ] **Step 5: Commit rename inference**

```powershell
git add src/TiaGitAddIn.Core/Comparison/Scl/SclComparer.cs src/TiaGitAddIn.Tests/Services/Comparison/SclComparerTests.cs
git commit -m "feat: detect unique scl declaration renames"
```

### Task 6: Adapt SCL comparison to the shared result/fallback contract

**Acceptance criteria:** AC-008, AC-018, AC-022, AC-023, AC-066, AC-107, AC-117, AC-118

**Files:**
- Create: `src/TiaGitAddIn.Core/Models/Comparison/SclPresentation.cs`
- Create: `src/TiaGitAddIn.Core/Services/Comparison/SclComparisonStrategy.cs`
- Modify: `src/TiaGitAddIn/UI/GitPanelLaunchService.cs`
- Test: `src/TiaGitAddIn.Tests/Services/Comparison/SclComparisonStrategyTests.cs`

**Interfaces:**
- Consumes: `PlcComparisonContext.Request.Left/Right`, `PlcRevision.Text/IsMissing`, and `PlcComparisonResultFactory` from the foundation plan.
- Implements: `IPlcComparisonStrategy` for exactly `PlcArtifactKind.Scl`.
- Produces: `SclPresentation` for Structured Full/Partial; delegates unrecoverable source to `CreateTextFallback`; rethrows cancellation.

- [ ] **Step 1: Write failing Full/Partial/Fallback/addition/cancellation tests**

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Comparison.Text;
using TiaGitAddIn.Tests.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Services.Comparison
{
    public sealed class SclComparisonStrategyTests
    {
        [Fact]
        public async Task CompareAsync_ValidPair_ReturnsStructuredFullSclPresentationAndRawText()
        {
            PlcComparisonContext context = ComparisonTestData.Context(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Structured,
                "FUNCTION_BLOCK \"A\"\nX := 1;\nEND_FUNCTION_BLOCK",
                "FUNCTION_BLOCK \"A\"\nX := 2;\nEND_FUNCTION_BLOCK",
                "Program.scl");
            SclComparisonStrategy strategy = CreateStrategy();

            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcArtifactKind.Scl, result.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Structured, result.RequestedMode);
            Assert.Equal(PlcComparisonMode.Structured, result.ActualMode);
            Assert.Equal(PlcSupportLevel.Full, result.SupportLevel);
            Assert.IsType<SclPresentation>(result.Presentation);
            Assert.NotNull(result.RawText);
            Assert.Equal(context.Request.Left.Text, result.RawText!.LeftText);
        }

        [Fact]
        public async Task CompareAsync_RecoverableTree_ReturnsStructuredPartialWithExactDiagnostic()
        {
            PlcComparisonContext context = ComparisonTestData.Context(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Structured,
                "FUNCTION_BLOCK \"A\"\nBroken( ; Good := 1;\nEND_FUNCTION_BLOCK",
                "FUNCTION_BLOCK \"A\"\nGood := 2;\nEND_FUNCTION_BLOCK",
                "Program.scl");

            PlcComparisonResult result = await CreateStrategy()
                .CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcSupportLevel.Partial, result.SupportLevel);
            Assert.Equal(PlcComparisonMode.Structured, result.ActualMode);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SCL2006" && diagnostic.Location != null);
            Assert.False(string.IsNullOrWhiteSpace(result.Limitation));
            Assert.Single(((SclPresentation)result.Presentation).Left.UnparsedSpans);
        }

        [Fact]
        public async Task CompareAsync_NoReliableBlock_ReturnsTextFallback()
        {
            PlcComparisonContext context = ComparisonTestData.Context(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Structured,
                "Broken := ;",
                "Still broken(",
                "Program.scl");

            PlcComparisonResult result = await CreateStrategy()
                .CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcComparisonMode.Text, result.ActualMode);
            Assert.Equal(PlcSupportLevel.Fallback, result.SupportLevel);
            Assert.IsType<TextPresentation>(result.Presentation);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SCL4001");
        }

        [Fact]
        public async Task CompareAsync_MissingLeftSide_UsesEmptyTreeAndKeepsStructuredMode()
        {
            const string source = "FUNCTION_BLOCK \"Added\"\nX := 1;\nEND_FUNCTION_BLOCK";
            PlcRevision left = ComparisonTestData.MissingRevision(PlcRevisionSide.Left, "Added.scl");
            PlcRevision right = ComparisonTestData.TextRevision(PlcRevisionSide.Right, source, "Added.scl");
            var descriptor = new PlcArtifactDescriptor(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Structured,
                new[] { "content:scl-block" });
            var pair = new PlcArtifactPairDescriptor(
                null,
                descriptor,
                PlcArtifactKind.Scl,
                PlcComparisonMode.Structured,
                PlcPairChangeKind.Added,
                string.Empty);
            var context = new PlcComparisonContext(
                new PlcComparisonRequest(left, right, pair),
                new ComparisonRawText(null, source, isLeftMissing: true, isRightMissing: false));

            PlcComparisonResult result = await CreateStrategy()
                .CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcSupportLevel.Full, result.SupportLevel);
            var presentation = Assert.IsType<SclPresentation>(result.Presentation);
            Assert.Empty(presentation.Left.Document.Blocks);
            Assert.Single(presentation.Right.Document.Blocks);
        }

        [Fact]
        public async Task CompareAsync_CancelledToken_RethrowsCancellationWithoutFallback()
        {
            PlcComparisonContext context = ComparisonTestData.Context(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Structured,
                "FUNCTION_BLOCK \"A\"\nEND_FUNCTION_BLOCK",
                "FUNCTION_BLOCK \"A\"\nEND_FUNCTION_BLOCK",
                "Program.scl");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                CreateStrategy().CompareAsync(context, cancellation.Token));
        }

        [Fact]
        public async Task CompareAsync_ComparisonBudgetExceeded_ReturnsTextFallbackWithStableDiagnostic()
        {
            PlcComparisonContext context = ComparisonTestData.Context(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Structured,
                "FUNCTION_BLOCK \"A\"\nX := 1;\nY := 2;\nEND_FUNCTION_BLOCK",
                "FUNCTION_BLOCK \"A\"\nX := 2;\nY := 3;\nEND_FUNCTION_BLOCK",
                "Program.scl");
            var strategy = new SclComparisonStrategy(
                new PlcComparisonResultFactory(new LineTextComparer(TextComparisonLimits.Default)),
                SclParserLimits.Default,
                new SclComparisonLimits(maxSequenceItems: 1, maxWorkItems: 2));

            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcComparisonMode.Text, result.ActualMode);
            Assert.Equal(PlcSupportLevel.Fallback, result.SupportLevel);
            Assert.IsType<TextPresentation>(result.Presentation);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SCL3002");
        }

        private static SclComparisonStrategy CreateStrategy() =>
            new SclComparisonStrategy(
                new PlcComparisonResultFactory(new LineTextComparer(TextComparisonLimits.Default)));
    }
}
```

The tests consume the foundation's `src/TiaGitAddIn.Tests/Comparison/ComparisonTestData.cs`; do not introduce a second general revision/context factory.

- [ ] **Step 2: Run strategy tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclComparisonStrategyTests"
```

Expected: FAIL at compile time because `SclPresentation` and `SclComparisonStrategy` do not exist.

- [ ] **Step 3: Add the typed SCL presentation**

```csharp
using System;
using TiaGitAddIn.Comparison.Scl;

namespace TiaGitAddIn.Models.Comparison
{
    public sealed class SclPresentation : ComparisonPresentation
    {
        public SclPresentation(
            string leftPath,
            string rightPath,
            SclParseResult left,
            SclParseResult right,
            SclComparison comparison)
            : base(ComparisonPresentationKind.Scl)
        {
            LeftPath = leftPath ?? throw new ArgumentNullException(nameof(leftPath));
            RightPath = rightPath ?? throw new ArgumentNullException(nameof(rightPath));
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
            Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
        }

        public string LeftPath { get; }
        public string RightPath { get; }
        public SclParseResult Left { get; }
        public SclParseResult Right { get; }
        public SclComparison Comparison { get; }
    }
}
```

Missing sides use an immutable empty parse result created by `SclParser.Parse(string.Empty, ...)`; they are not treated as malformed because `PlcRevision.IsMissing` is authoritative.

- [ ] **Step 4: Implement strategy support, diagnostics, and fallback thresholds**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Comparison.Scl;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    public sealed class SclComparisonStrategy : IPlcComparisonStrategy
    {
        private static readonly IReadOnlyCollection<PlcArtifactKind> Kinds =
            Array.AsReadOnly(new[] { PlcArtifactKind.Scl });
        private readonly PlcComparisonResultFactory resultFactory;
        private readonly SclParserLimits limits;
        private readonly SclComparisonLimits comparisonLimits;

        public SclComparisonStrategy(
            PlcComparisonResultFactory resultFactory,
            SclParserLimits? limits = null,
            SclComparisonLimits? comparisonLimits = null)
        {
            this.resultFactory = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
            this.limits = limits ?? SclParserLimits.Default;
            this.comparisonLimits = comparisonLimits ?? SclComparisonLimits.Default;
        }

        public IReadOnlyCollection<PlcArtifactKind> SupportedKinds => Kinds;

        public Task<PlcComparisonResult> CompareAsync(PlcComparisonContext context, CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            cancellationToken.ThrowIfCancellationRequested();
            if (context.Request.Pair.ArtifactKind != PlcArtifactKind.Scl)
                throw new ArgumentException("SCL strategy requires an SCL artifact pair.", nameof(context));

            SclParseResult left = ParseSide(context.Request.Left, cancellationToken);
            SclParseResult right = ParseSide(context.Request.Right, cancellationToken);
            var diagnostics = MapDiagnostics(left, PlcRevisionSide.Left)
                .Concat(MapDiagnostics(right, PlcRevisionSide.Right)).ToArray();

            if (HasUnreliablePresentSide(context, left, right))
            {
                var all = diagnostics.Concat(new[]
                {
                    new PlcComparisonDiagnostic("SCL4001", PlcDiagnosticSeverity.Warning,
                        "SCL block structure could not be established; raw text is shown.")
                });
                return Task.FromResult(resultFactory.CreateTextFallback(
                    context,
                    "Structured SCL comparison is unavailable because block structure could not be established.",
                    all));
            }

            SclComparison comparison = SclComparer.Compare(left, right, comparisonLimits, cancellationToken);
            if (comparison.ComparisonLimitExceeded)
            {
                var all = diagnostics.Concat(new[]
                {
                    new PlcComparisonDiagnostic(
                        "SCL3002",
                        PlcDiagnosticSeverity.Warning,
                        "Structured SCL comparison limits were exceeded; raw text is shown.")
                });
                return Task.FromResult(resultFactory.CreateTextFallback(
                    context,
                    "Structured SCL comparison exceeded its bounded sequence or work limit.",
                    all));
            }
            bool partial = PresentResults(context, left, right).Any(item => item.Reliability == SclParseReliability.Partial);
            PlcSupportLevel support = partial ? PlcSupportLevel.Partial : PlcSupportLevel.Full;
            string limitation = partial
                ? "SCL was recovered at bounded parser boundaries; exact unparsed spans remain visible."
                : string.Empty;
            var presentation = new SclPresentation(
                context.Request.Left.OriginalPath,
                context.Request.Right.OriginalPath,
                left,
                right,
                comparison);
            return Task.FromResult(resultFactory.CreateSemantic(
                context,
                PlcComparisonMode.Structured,
                support,
                limitation,
                diagnostics,
                presentation));
        }

        private SclParseResult ParseSide(PlcRevision revision, CancellationToken cancellationToken)
        {
            if (revision.IsMissing) return SclParser.Parse(string.Empty, limits, cancellationToken);
            if (revision.Text == null) throw new InvalidOperationException("A classified SCL revision must contain decoded text.");
            return SclParser.Parse(revision.Text, limits, cancellationToken);
        }

        private static bool HasUnreliablePresentSide(PlcComparisonContext context, SclParseResult left, SclParseResult right)
        {
            return (!context.Request.Left.IsMissing && left.Reliability == SclParseReliability.None) ||
                   (!context.Request.Right.IsMissing && right.Reliability == SclParseReliability.None);
        }

        private static IEnumerable<SclParseResult> PresentResults(PlcComparisonContext context, SclParseResult left, SclParseResult right)
        {
            if (!context.Request.Left.IsMissing) yield return left;
            if (!context.Request.Right.IsMissing) yield return right;
        }

        private static IEnumerable<PlcComparisonDiagnostic> MapDiagnostics(SclParseResult result, PlcRevisionSide side)
        {
            return result.Diagnostics.Select(item => new PlcComparisonDiagnostic(
                item.Code,
                PlcDiagnosticSeverity.Warning,
                item.Message,
                new PlcSourceLocation(side, item.Span.Line, item.Span.Column, item.Span.StartOffset, item.Span.Length)));
        }
    }
}
```

Do not catch `OperationCanceledException`. If decoded text is unexpectedly absent, let the coordinator's hard-error boundary convert the thrown `InvalidOperationException`; do not label it a text fallback.

- [ ] **Step 5: Register the strategy through the foundation composition seam**

Modify only the strategy-list construction in `src/TiaGitAddIn/UI/GitPanelLaunchService.cs`:

```csharp
IReadOnlyList<IPlcComparisonStrategy> comparisonStrategies = new IPlcComparisonStrategy[]
{
    interfaceComparisonStrategy,
    ladComparisonStrategy,
    new SclComparisonStrategy(comparisonResultFactory)
};
```

Preserve the list's existing entries and order; append SCL before the foundation fallback is evaluated. No `DiffViewModel` classifier branch is allowed.

- [ ] **Step 6: Run strategy, coordinator-invariant, and cancellation tests**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclComparisonStrategyTests|FullyQualifiedName~PlcComparisonCoordinatorTests|FullyQualifiedName~ComparisonResultInvariantTests"
```

Expected: PASS; Full, Partial, Fallback, added-side, invariant, and cancellation cases all succeed.

- [ ] **Step 7: Commit the strategy slice**

```powershell
git add src/TiaGitAddIn.Core/Models/Comparison/SclPresentation.cs src/TiaGitAddIn.Core/Services/Comparison/SclComparisonStrategy.cs src/TiaGitAddIn/UI/GitPanelLaunchService.cs src/TiaGitAddIn.Tests/Services/Comparison/SclComparisonStrategyTests.cs
git commit -m "feat: route structured scl comparison"
```

### Task 7: Map SCL hierarchy and lexer tokens into focused ViewModels

**Acceptance criteria:** AC-007, AC-022, AC-023, AC-025, AC-027, AC-067

**Files:**
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/SclDiffViewModel.cs`
- Create: `src/TiaGitAddIn/UI/Mapping/SclPresentationViewModelFactory.cs`
- Modify: `src/TiaGitAddIn/UI/GitPanelLaunchService.cs`
- Test: `src/TiaGitAddIn.Tests/UI/Comparison/SclDiffViewModelTests.cs`

**Interfaces:**
- Consumes: `SclPresentation`, `ComparisonViewModelMetadata`, and `IComparisonPresentationViewModelFactory`.
- Produces: `SclDiffViewModel.Groups` in comparison/source order and `SclTokenRunViewModel` values copied from the domain token kind/span/text.
- Registers: one specialized factory in the aggregate mapper; it does not modify the aggregate mapper's selection algorithm.

- [ ] **Step 1: Write failing ViewModel mapping and token-identity tests**

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Comparison.Text;
using TiaGitAddIn.Tests.Comparison;
using TiaGitAddIn.UI.Mapping;
using TiaGitAddIn.UI.ViewModels.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.UI.Comparison
{
    public sealed class SclDiffViewModelTests
    {
        [Fact]
        public async Task Map_BlockRegionAndUngroupedContent_UsesSourceOrderAndComparisonTokens()
        {
            const string left =
                "FUNCTION_BLOCK \"Demo\"\nVAR_INPUT\nValue : DInt;\nEND_VAR\n" +
                "Outside := 1;\nREGION R\nInside := 1;\nEND_REGION\nEND_FUNCTION_BLOCK";
            const string right =
                "FUNCTION_BLOCK \"Demo\"\nVAR_INPUT\nValue : DInt;\nEND_VAR\n" +
                "Outside := 2;\nREGION R\nInside := 3;\nEND_REGION\nEND_FUNCTION_BLOCK";
            PlcComparisonContext context = ComparisonTestData.Context(
                PlcArtifactKind.Scl, PlcComparisonMode.Structured, left, right, "Program.scl");
            var strategy = new SclComparisonStrategy(
                new PlcComparisonResultFactory(new LineTextComparer(TextComparisonLimits.Default)));
            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);
            var factory = new SclPresentationViewModelFactory();

            var viewModel = Assert.IsType<SclDiffViewModel>(
                factory.Map(result, ComparisonViewModelMetadata.From(result)));

            Assert.Equal("Program.scl", viewModel.FileHeader);
            SclGroupViewModel block = Assert.Single(viewModel.Groups);
            Assert.Equal("FUNCTION_BLOCK Demo", block.Header);
            Assert.Equal(
                new[] { SclChangeCategory.DeclarationSection, SclChangeCategory.Ungrouped, SclChangeCategory.Region },
                block.Children.Select(child => child.Category));
            Assert.Equal("Ungrouped", block.Children[1].Header);
            Assert.Equal("REGION R", block.Children[2].Header);

            SclTokenRunViewModel run = block.Descendants()
                .SelectMany(group => group.RightRuns)
                .First(item => item.Text == "Inside");
            SclToken domainToken = ((SclPresentation)result.Presentation).Right.Lex.Tokens
                .First(item => item.Text == "Inside");
            Assert.Equal(domainToken.Kind, run.TokenKind);
            Assert.Equal(domainToken.Span.StartOffset, run.StartOffset);
            Assert.Equal(domainToken.Span.Length, run.Length);
        }

        [Fact]
        public async Task Map_FormattingOnlyChange_KeepsRawTextDifferenceWithoutSemanticLeaf()
        {
            const string left = "FUNCTION_BLOCK \"A\"\nX:=1;\nEND_FUNCTION_BLOCK";
            const string right = "FUNCTION_BLOCK \"A\"\r\n  X := 1 ;\r\nEND_FUNCTION_BLOCK";
            PlcComparisonContext context = ComparisonTestData.Context(
                PlcArtifactKind.Scl, PlcComparisonMode.Structured, left, right, "Program.scl");
            var strategy = new SclComparisonStrategy(
                new PlcComparisonResultFactory(new LineTextComparer(TextComparisonLimits.Default)));
            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);

            var viewModel = Assert.IsType<SclDiffViewModel>(new SclPresentationViewModelFactory()
                .Map(result, ComparisonViewModelMetadata.From(result)));

            Assert.DoesNotContain(viewModel.Groups.SelectMany(group => group.Descendants()), group => group.StatusText != "Unchanged");
            Assert.NotNull(viewModel.RawText);
            Assert.NotEqual(viewModel.RawText!.LeftText, viewModel.RawText.RightText);
        }
    }
}
```

- [ ] **Step 2: Run ViewModel tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclDiffViewModelTests"
```

Expected: FAIL at compile time because the SCL presentation factory and ViewModels do not exist.

- [ ] **Step 3: Add the specialized presentation factory**

```csharp
using System;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    public sealed class SclPresentationViewModelFactory : IComparisonPresentationViewModelFactory
    {
        public bool CanMap(ComparisonPresentation presentation) => presentation is SclPresentation;

        public ComparisonPresentationViewModel Map(PlcComparisonResult result, ComparisonViewModelMetadata metadata)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            if (!(result.Presentation is SclPresentation presentation))
                throw new ArgumentException("SCL factory requires SclPresentation.", nameof(result));
            return SclDiffViewModel.Create(presentation, metadata);
        }
    }
}
```

- [ ] **Step 4: Add the focused ViewModel hierarchy and token-run mapping**

Create `SclDiffViewModel.cs` with these public types in `TiaGitAddIn.UI.ViewModels.Comparison`:

```csharp
public sealed class SclDiffViewModel : ComparisonPresentationViewModel
{
    private SclDiffViewModel(
        string fileHeader,
        IEnumerable<SclGroupViewModel> groups,
        ComparisonViewModelMetadata metadata)
        : base(ComparisonPresentationKind.Scl, metadata)
    {
        FileHeader = fileHeader;
        Groups = Array.AsReadOnly(groups.ToArray());
    }

    public string FileHeader { get; }
    public IReadOnlyList<SclGroupViewModel> Groups { get; }

    public static SclDiffViewModel Create(SclPresentation presentation, ComparisonViewModelMetadata metadata)
    {
        if (presentation == null) throw new ArgumentNullException(nameof(presentation));
        string path = string.IsNullOrWhiteSpace(presentation.RightPath) ? presentation.LeftPath : presentation.RightPath;
        string header = System.IO.Path.GetFileName(path);
        var groups = presentation.Comparison.Groups.Select(node => MapGroup(node, presentation));
        return new SclDiffViewModel(header, groups, metadata);
    }

    private static SclGroupViewModel MapGroup(SclChangeNode node, SclPresentation presentation)
    {
        var children = node.Children.Select(child => MapGroup(child, presentation));
        return new SclGroupViewModel(
            HeaderFor(node),
            node.Category,
            StatusFor(node.Kind),
            TokensFor(presentation.Left, node.LeftSpan),
            TokensFor(presentation.Right, node.RightSpan),
            children);
    }

    private static IReadOnlyList<SclTokenRunViewModel> TokensFor(SclParseResult parse, SclSourceSpan? span)
    {
        if (span == null) return Array.Empty<SclTokenRunViewModel>();
        return parse.Lex.Tokens
            .Where(token => token.Kind != SclTokenKind.EndOfFile &&
                token.Span.StartOffset < span.EndOffset && token.Span.EndOffset > span.StartOffset)
            .Select(token => new SclTokenRunViewModel(token.Text, token.Kind, token.Span.StartOffset, token.Span.Length))
            .ToArray();
    }

    private static string HeaderFor(SclChangeNode node)
    {
        switch (node.Category)
        {
            case SclChangeCategory.Block: return BlockHeader(node.Key);
            case SclChangeCategory.Region: return "REGION " + LastPathValue(node.Key);
            case SclChangeCategory.DeclarationSection: return node.Key;
            case SclChangeCategory.Declaration: return "Declaration " + (node.RightText ?? node.LeftText ?? node.Key);
            case SclChangeCategory.Statement: return "Statement";
            case SclChangeCategory.Comment: return "Comment";
            case SclChangeCategory.Ungrouped: return "Ungrouped";
            case SclChangeCategory.Unparsed: return "Unparsed source";
            default: return node.Key;
        }
    }

    private static string StatusFor(SclChangeKind kind)
    {
        switch (kind)
        {
            case SclChangeKind.Added: return "Added";
            case SclChangeKind.Removed: return "Removed";
            case SclChangeKind.Modified: return "Modified";
            case SclChangeKind.Rename: return "Renamed";
            default: return "Unchanged";
        }
    }

    private static string LastPathValue(string key)
    {
        int separator = key.LastIndexOf("/Region:", StringComparison.Ordinal);
        return separator < 0 ? key : key.Substring(separator + 8);
    }

    private static string BlockHeader(string key)
    {
        int separator = key.IndexOf(':');
        string kind = separator < 0 ? key : key.Substring(0, separator);
        string name = separator < 0 ? string.Empty : key.Substring(separator + 1);
        string keyword;
        switch (kind)
        {
            case "OrganizationBlock": keyword = "ORGANIZATION_BLOCK"; break;
            case "FunctionBlock": keyword = "FUNCTION_BLOCK"; break;
            case "Function": keyword = "FUNCTION"; break;
            case "DataBlock": keyword = "DATA_BLOCK"; break;
            case "Type": keyword = "TYPE"; break;
            default: keyword = kind.ToUpperInvariant(); break;
        }
        return string.IsNullOrEmpty(name) ? keyword : keyword + " " + name;
    }
}

public sealed class SclGroupViewModel
{
    public SclGroupViewModel(
        string header,
        SclChangeCategory category,
        string statusText,
        IEnumerable<SclTokenRunViewModel> leftRuns,
        IEnumerable<SclTokenRunViewModel> rightRuns,
        IEnumerable<SclGroupViewModel> children)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Category = category;
        StatusText = statusText ?? throw new ArgumentNullException(nameof(statusText));
        LeftRuns = Array.AsReadOnly((leftRuns ?? throw new ArgumentNullException(nameof(leftRuns))).ToArray());
        RightRuns = Array.AsReadOnly((rightRuns ?? throw new ArgumentNullException(nameof(rightRuns))).ToArray());
        Children = Array.AsReadOnly((children ?? throw new ArgumentNullException(nameof(children))).ToArray());
    }

    public string Header { get; }
    public SclChangeCategory Category { get; }
    public string StatusText { get; }
    public IReadOnlyList<SclTokenRunViewModel> LeftRuns { get; }
    public IReadOnlyList<SclTokenRunViewModel> RightRuns { get; }
    public IReadOnlyList<SclGroupViewModel> Children { get; }

    public IEnumerable<SclGroupViewModel> Descendants()
    {
        foreach (SclGroupViewModel child in Children)
        {
            yield return child;
            foreach (SclGroupViewModel descendant in child.Descendants()) yield return descendant;
        }
    }
}

public sealed class SclTokenRunViewModel
{
    public SclTokenRunViewModel(string text, SclTokenKind tokenKind, int startOffset, int length)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        TokenKind = tokenKind;
        StartOffset = startOffset;
        Length = length;
    }
    public string Text { get; }
    public SclTokenKind TokenKind { get; }
    public int StartOffset { get; }
    public int Length { get; }
    public string AccessibleName => TokenKind + " " + Text;
}
```

Add the required `System`, `Collections.Generic`, `Linq`, SCL, and comparison-model `using` directives. Do not use `ObservableCollection`; the result is complete before mapping and must remain immutable.

- [ ] **Step 5: Register the specialized factory beside the strategy**

In `GitPanelLaunchService`, append `new SclPresentationViewModelFactory()` to the foundation's `IComparisonPresentationViewModelFactory[]` composition list. Keep `ComparisonPresentationMapper` unchanged so it continues to enforce exactly one matching factory.

- [ ] **Step 6: Run ViewModel, mapper uniqueness, and latest-selection tests**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclDiffViewModelTests|FullyQualifiedName~ComparisonPresentationMapperTests|FullyQualifiedName~DiffViewModelSelectionTests"
```

Expected: PASS; SCL maps once, group/token assertions succeed, and rapid selection still applies only the latest immutable result on the dispatcher.

- [ ] **Step 7: Commit the WPF mapping slice**

```powershell
git add src/TiaGitAddIn/UI/ViewModels/Comparison/SclDiffViewModel.cs src/TiaGitAddIn/UI/Mapping/SclPresentationViewModelFactory.cs src/TiaGitAddIn/UI/GitPanelLaunchService.cs src/TiaGitAddIn.Tests/UI/Comparison/SclDiffViewModelTests.cs
git commit -m "feat: map structured scl presentation"
```

### Task 8: Add the SCL DataTemplate, focused view, and STA runtime smoke tests

**Acceptance criteria:** AC-023, AC-028, AC-029, AC-030, AC-031, AC-032, AC-067

**Files:**
- Create: `src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml`
- Create: `src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml.cs`
- Modify: `src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml`
- Test: `src/TiaGitAddIn.Tests/UI/Comparison/SclDiffViewTests.cs`

**Interfaces:**
- Consumes: `SclDiffViewModel`, the foundation metadata/limitation/raw-text host, and `WpfTestHost`.
- Produces: one implicit WPF `DataTemplate` keyed by `SclDiffViewModel` and a constructor-only `SclDiffView`.
- Preserves: visible text labels in addition to syntax/status color; no business logic in code-behind.

- [ ] **Step 1: Write failing implicit-template and STA binding tests**

```csharp
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Comparison.Text;
using TiaGitAddIn.Tests.Comparison;
using TiaGitAddIn.UI.Mapping;
using TiaGitAddIn.UI.ViewModels.Comparison;
using TiaGitAddIn.UI.Views.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.UI.Comparison
{
    public sealed class SclDiffViewTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ImplicitTemplate_LoadsFullAndPartialSclViewOnStaWithoutBindingErrors(bool partial)
        {
            WpfTestHost.Run(dispatcher =>
            {
                PlcComparisonResult result = CreateResult(partial);
                var viewModel = Assert.IsType<SclDiffViewModel>(new SclPresentationViewModelFactory()
                    .Map(result, ComparisonViewModelMetadata.From(result)));
                var listener = new BindingErrorListener();
                PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                try
                {
                    var resources = new ResourceDictionary
                    {
                        Source = new Uri(
                            "/TiaGitAddIn;component/UI/Views/Comparison/ComparisonTemplates.xaml",
                            UriKind.Relative)
                    };
                    var key = new DataTemplateKey(typeof(SclDiffViewModel));
                    DataTemplate template = Assert.IsType<DataTemplate>(resources[key]);
                    var view = Assert.IsType<SclDiffView>(template.LoadContent());
                    view.DataContext = viewModel;
                    view.Measure(new Size(1200, 800));
                    view.Arrange(new Rect(0, 0, 1200, 800));
                    view.UpdateLayout();

                    Assert.Equal("Program.scl structured SCL comparison", AutomationProperties.GetName(view));
                    Assert.True(viewModel.HasRawText);
                    Assert.Equal(partial, viewModel.HasLimitation);
                    Assert.Empty(listener.Messages);
                }
                finally
                {
                    PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
                }
            });
        }

        [Fact]
        public void ViewModel_StatusAndTokenLabelsRemainAvailableWithoutColor()
        {
            WpfTestHost.Run(dispatcher =>
            {
                PlcComparisonResult result = CreateResult(partial: false);
                var viewModel = Assert.IsType<SclDiffViewModel>(new SclPresentationViewModelFactory()
                    .Map(result, ComparisonViewModelMetadata.From(result)));

                Assert.Contains(viewModel.Groups.SelectMany(group => group.Descendants()), group => group.StatusText == "Modified");
                Assert.All(viewModel.Groups.SelectMany(group => group.Descendants()).SelectMany(group => group.RightRuns),
                    run => Assert.False(string.IsNullOrWhiteSpace(run.AccessibleName)));
            });
        }

        private static PlcComparisonResult CreateResult(bool partial)
        {
            string left = partial
                ? "FUNCTION_BLOCK \"A\"\nBroken( ; X := 1;\nEND_FUNCTION_BLOCK"
                : "FUNCTION_BLOCK \"A\"\nX := 1;\nEND_FUNCTION_BLOCK";
            PlcComparisonContext context = ComparisonTestData.Context(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Structured,
                left,
                "FUNCTION_BLOCK \"A\"\nX := 2;\nEND_FUNCTION_BLOCK",
                "Program.scl");
            var strategy = new SclComparisonStrategy(
                new PlcComparisonResultFactory(new LineTextComparer(TextComparisonLimits.Default)));
            return strategy.CompareAsync(context, CancellationToken.None).GetAwaiter().GetResult();
        }

        private sealed class BindingErrorListener : TraceListener
        {
            private readonly System.Collections.Generic.List<string> messages = new System.Collections.Generic.List<string>();
            public System.Collections.Generic.IReadOnlyList<string> Messages => messages;
            public override void Write(string? message) { if (!string.IsNullOrWhiteSpace(message)) messages.Add(message!); }
            public override void WriteLine(string? message) { if (!string.IsNullOrWhiteSpace(message)) messages.Add(message!); }
        }
    }
}
```

- [ ] **Step 2: Run the STA tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclDiffViewTests"
```

Expected: FAIL at compile time because `SclDiffView` and its implicit template do not exist.

- [ ] **Step 3: Add the constructor-only view code-behind**

```csharp
using System.Windows.Controls;

namespace TiaGitAddIn.UI.Views.Comparison
{
    public partial class SclDiffView : UserControl
    {
        public SclDiffView()
        {
            InitializeComponent();
        }
    }
}
```

No event handler or parser/mapping logic belongs in this file.

- [ ] **Step 4: Add the focused hierarchical XAML**

Create `SclDiffView.xaml` with this complete structure. Keep brushes/styles local so the control is self-contained and theme values are not repeated inline.

```xml
<UserControl x:Class="TiaGitAddIn.UI.Views.Comparison.SclDiffView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:scl="clr-namespace:TiaGitAddIn.Comparison.Scl;assembly=TiaGitAddIn.Core"
             xmlns:vm="clr-namespace:TiaGitAddIn.UI.ViewModels.Comparison"
             AutomationProperties.Name="{Binding FileHeader, StringFormat={}{0} structured SCL comparison}">
    <UserControl.Resources>
        <SolidColorBrush x:Key="SclKeywordBrush" Color="#005A9E"/>
        <SolidColorBrush x:Key="SclIdentifierBrush" Color="#202020"/>
        <SolidColorBrush x:Key="SclLiteralBrush" Color="#A31515"/>
        <SolidColorBrush x:Key="SclCommentBrush" Color="#357A38"/>
        <SolidColorBrush x:Key="SclBorderBrush" Color="#D0D0D0"/>

        <Style x:Key="SclTokenStyle" TargetType="TextBlock">
            <Setter Property="FontFamily" Value="Consolas"/>
            <Setter Property="Foreground" Value="{StaticResource SclIdentifierBrush}"/>
            <Setter Property="Margin" Value="0,0,4,0"/>
            <Setter Property="AutomationProperties.Name" Value="{Binding AccessibleName}"/>
            <Style.Triggers>
                <DataTrigger Binding="{Binding TokenKind}" Value="{x:Static scl:SclTokenKind.Keyword}">
                    <Setter Property="Foreground" Value="{StaticResource SclKeywordBrush}"/>
                    <Setter Property="FontWeight" Value="SemiBold"/>
                </DataTrigger>
                <DataTrigger Binding="{Binding TokenKind}" Value="{x:Static scl:SclTokenKind.StringLiteral}">
                    <Setter Property="Foreground" Value="{StaticResource SclLiteralBrush}"/>
                </DataTrigger>
                <DataTrigger Binding="{Binding TokenKind}" Value="{x:Static scl:SclTokenKind.NumericLiteral}">
                    <Setter Property="Foreground" Value="{StaticResource SclLiteralBrush}"/>
                </DataTrigger>
                <DataTrigger Binding="{Binding TokenKind}" Value="{x:Static scl:SclTokenKind.Comment}">
                    <Setter Property="Foreground" Value="{StaticResource SclCommentBrush}"/>
                    <Setter Property="FontStyle" Value="Italic"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>

        <DataTemplate DataType="{x:Type vm:SclTokenRunViewModel}">
            <TextBlock Text="{Binding Text}" Style="{StaticResource SclTokenStyle}"/>
        </DataTemplate>

        <HierarchicalDataTemplate DataType="{x:Type vm:SclGroupViewModel}" ItemsSource="{Binding Children}">
            <Border BorderBrush="{StaticResource SclBorderBrush}" BorderThickness="0,0,0,1" Padding="4">
                <StackPanel>
                    <DockPanel>
                        <TextBlock Text="{Binding StatusText}"
                                   DockPanel.Dock="Right"
                                   FontWeight="SemiBold"
                                   Margin="12,0,0,0"
                                   AutomationProperties.Name="{Binding StatusText, StringFormat=Change status {0}}"/>
                        <TextBlock Text="{Binding Header}" FontWeight="SemiBold"/>
                    </DockPanel>
                    <Grid Margin="0,4,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="8"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <ItemsControl Grid.Column="0" ItemsSource="{Binding LeftRuns}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                        </ItemsControl>
                        <ItemsControl Grid.Column="2" ItemsSource="{Binding RightRuns}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                        </ItemsControl>
                    </Grid>
                </StackPanel>
            </Border>
        </HierarchicalDataTemplate>
    </UserControl.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <Border Grid.Row="0" BorderBrush="{StaticResource SclBorderBrush}" BorderThickness="0,0,0,1" Padding="8">
            <TextBlock Text="{Binding FileHeader}" FontSize="15" FontWeight="Bold"/>
        </Border>
        <TreeView Grid.Row="1"
                  ItemsSource="{Binding Groups}"
                  VirtualizingPanel.IsVirtualizing="True"
                  VirtualizingPanel.VirtualizationMode="Recycling"
                  AutomationProperties.Name="Structured SCL changes"/>
    </Grid>
</UserControl>
```

Status text is always rendered, so no change relies on color alone.

- [ ] **Step 5: Register the implicit template in the shared resource dictionary**

Add the namespace and template to `ComparisonTemplates.xaml` without changing its other templates:

```xml
xmlns:comparisonVm="clr-namespace:TiaGitAddIn.UI.ViewModels.Comparison"
xmlns:comparisonViews="clr-namespace:TiaGitAddIn.UI.Views.Comparison"

<DataTemplate DataType="{x:Type comparisonVm:SclDiffViewModel}">
    <comparisonViews:SclDiffView/>
</DataTemplate>
```

The foundation host continues to render the mode badge, inline limitation, safe expandable diagnostics, and selectable raw-text alternative around this typed view.

- [ ] **Step 6: Run STA view, all comparison-template, and XAML build tests**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclDiffViewTests|FullyQualifiedName~ComparisonTemplateTests|FullyQualifiedName~ComparisonPresentationHostTests"
```

Expected: PASS on a dedicated STA thread; implicit template resolves to `SclDiffView`, bindings produce no error, raw text remains available, and every foundation presentation template still resolves.

- [ ] **Step 7: Verify focused file sizes before commit**

Run:

```powershell
$paths = @(
  'src/TiaGitAddIn/UI/ViewModels/Comparison/SclDiffViewModel.cs',
  'src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml',
  'src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml.cs'
)
$oversized = $paths | Where-Object { (Get-Content -LiteralPath $_).Count -gt 800 }
if ($oversized) { throw "Focused SCL files exceed 800 lines: $($oversized -join ', ')" }
```

Expected: exit zero with no output.

- [ ] **Step 8: Commit the focused WPF view**

```powershell
git add src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml.cs src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml src/TiaGitAddIn.Tests/UI/Comparison/SclDiffViewTests.cs
git commit -m "feat: render structured scl diff"
```

### Task 9: Anchor behavior with a sanitized real V21 fixture and finish documentation

**Acceptance criteria:** AC-030, AC-096, AC-098, AC-100, AC-102, AC-107

**Files:**
- Create: `src/TiaGitAddIn.Tests/TestData/Scl/V21/GitAcceptanceScl.scl`
- Create: `src/TiaGitAddIn.Tests/TestData/Scl/V21/New-Manifest.ps1`
- Create: `src/TiaGitAddIn.Tests/TestData/Scl/V21/manifest.json` by running the committed generator
- Create: `src/TiaGitAddIn.Tests/Services/Comparison/SclFixtureCompatibilityTests.cs`
- Modify: `README.md:17-30`
- Modify: `README.md:63-68`

**Interfaces:**
- Produces: one fresh, synthetic-but-real TIA Portal V21 export plus provenance/hash metadata for AC-102.
- Consumes: `SclParser`, `SclComparer`, `SclComparisonStrategy`, and foundation diagnostic redaction.
- Does not consume: any customer project, author identity, address, credential, or machine-specific path.

- [ ] **Step 1: Write failing fixture provenance, sanitization, and compatibility tests**

```csharp
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;
using TiaGitAddIn.Comparison.Scl;
using Xunit;

namespace TiaGitAddIn.Tests.Services.Comparison
{
    public sealed class SclFixtureCompatibilityTests
    {
        private static readonly Regex[] Forbidden =
        {
            new Regex(@"(?i)https?://[^/\s]*@", RegexOptions.Compiled),
            new Regex(@"(?i)(password|passwd|token|secret)\s*[:=]", RegexOptions.Compiled),
            new Regex(@"(?i)[a-z]:\\users\\", RegexOptions.Compiled),
            new Regex(@"(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.Compiled),
            new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled)
        };

        [Fact]
        public void RealV21Fixture_ManifestHashAndProvenanceMatchBytes()
        {
            string directory = FixtureDirectory();
            string fixture = Path.Combine(directory, "GitAcceptanceScl.scl");
            JObject manifest = JObject.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
            JToken entry = Assert.Single(manifest["fixtures"]!);

            Assert.Equal("2100.0.121.1", (string?)entry["publicApiBuild"]);
            Assert.Equal("V21", (string?)entry["tiaPortalVersion"]);
            Assert.Equal("GitAcceptanceScl.scl", (string?)entry["path"]);
            Assert.Equal("SCL", (string?)entry["artifactKind"]);
            Assert.Contains((string?)entry["encoding"], new[] { "UTF-8", "UTF-16 LE", "UTF-16 BE" });
            Assert.Contains((string?)entry["bom"], new[] { "none", "utf8", "utf16-le", "utf16-be" });
            Assert.Equal("Full", (string?)entry["expectedSupportLevel"]);
            Assert.Equal(Hash(File.ReadAllBytes(fixture)), (string?)entry["sha256"]);
            Assert.NotEmpty((JArray)entry["sanitizationActions"]!);
        }

        [Fact]
        public void RealV21Fixture_ContainsNoForbiddenIdentityNetworkCredentialOrMachinePath()
        {
            string text = File.ReadAllText(Path.Combine(FixtureDirectory(), "GitAcceptanceScl.scl"));

            Assert.All(Forbidden, pattern => Assert.DoesNotMatch(pattern, text));
            Assert.Contains("GitAcceptanceScl", text);
            Assert.DoesNotContain("Author", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ModifiedBy", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RealV21Fixture_ParsesAndSelfComparesAsStructuredFull()
        {
            string text = File.ReadAllText(Path.Combine(FixtureDirectory(), "GitAcceptanceScl.scl"));

            SclParseResult parse = SclParser.Parse(text, SclParserLimits.Default, CancellationToken.None);
            SclComparison comparison = SclComparer.Compare(parse, parse, CancellationToken.None);

            Assert.Equal(SclParseReliability.Full, parse.Reliability);
            Assert.NotEmpty(parse.Document.Blocks);
            Assert.False(comparison.HasChanges);
        }

        private static string Hash(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2")));
        }

        private static string FixtureDirectory()
        {
            string current = Directory.GetCurrentDirectory();
            for (int depth = 0; depth < 7; depth++)
            {
                string candidate = Path.Combine(current, "src", "TiaGitAddIn.Tests", "TestData", "Scl", "V21");
                if (Directory.Exists(candidate)) return candidate;
                current = Directory.GetParent(current)?.FullName ?? current;
            }
            throw new DirectoryNotFoundException("SCL V21 fixture directory was not found.");
        }
    }
}
```

If the repository's net48 xUnit overload does not accept `StringComparison` in `Assert.DoesNotContain`, use `Assert.True(text.IndexOf("Author", StringComparison.OrdinalIgnoreCase) < 0)` and the same expression for `ModifiedBy`; do not weaken the case-insensitive assertion.

- [ ] **Step 2: Run the fixture test and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SclFixtureCompatibilityTests"
```

Expected: FAIL because the `TestData/Scl/V21` fixture and manifest do not exist.

- [ ] **Step 3: Create the source in a fresh V21 project and export it**

In a disposable TIA Portal V21 project named `TiaGitAcceptance`, create an external SCL source named `GitAcceptanceScl` with exactly this program, generate the block, then export that source through TIA Portal V21 into a temporary staging directory:

```scl
FUNCTION_BLOCK "GitAcceptanceScl"
VAR_INPUT
    Start : Bool := FALSE;
END_VAR
VAR
    Counter : DInt := 0;
END_VAR

REGION GitAcceptanceRegion
    IF #Start THEN
        #Counter := #Counter + 1;
    END_IF;
END_REGION
END_FUNCTION_BLOCK
```

Open the exported file as text. It must contain only the synthetic identifiers above and tool-generated syntax. If TIA writes author/project/device metadata, remove those fields without changing language tokens. Copy the reviewed bytes to `src/TiaGitAddIn.Tests/TestData/Scl/V21/GitAcceptanceScl.scl`. Record the actions as `fresh synthetic project`, `synthetic identifiers`, and `manual metadata review`; no customer export can substitute for this step.

- [ ] **Step 4: Add and run the deterministic manifest generator**

Create `New-Manifest.ps1`:

```powershell
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$directory = $PSScriptRoot
$fixturePath = Join-Path $directory 'GitAcceptanceScl.scl'
if (-not (Test-Path -LiteralPath $fixturePath -PathType Leaf)) {
    throw "Fixture missing: $fixturePath"
}

$bytes = [System.IO.File]::ReadAllBytes($fixturePath)
$bom = if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
    'utf8'
} elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
    'utf16-le'
} elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
    'utf16-be'
} else {
    'none'
}

$encoding = switch ($bom) {
    'utf16-le' { 'UTF-16 LE' }
    'utf16-be' { 'UTF-16 BE' }
    default {
        $strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
        $null = $strictUtf8.GetString($bytes)
        'UTF-8'
    }
}

$hash = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash.ToLowerInvariant()
$manifest = [ordered]@{
    schemaVersion = 1
    fixtures = @(
        [ordered]@{
            path = 'GitAcceptanceScl.scl'
            tiaPortalVersion = 'V21'
            publicApiBuild = '2100.0.121.1'
            artifactKind = 'SCL'
            encoding = $encoding
            bom = $bom
            sanitizationActions = @(
                'fresh synthetic TiaGitAcceptance project'
                'synthetic identifiers only'
                'manual identity, network, credential, and machine-path review'
            )
            sha256 = $hash
            expectedSupportLevel = 'Full'
        }
    )
}

$json = $manifest | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText(
    (Join-Path $directory 'manifest.json'),
    $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))
```

Run:

```powershell
pwsh -NoProfile -File src/TiaGitAddIn.Tests/TestData/Scl/V21/New-Manifest.ps1
```

Expected: `manifest.json` is created with the actual lowercase SHA-256 and detected BOM; rerunning produces byte-identical JSON while fixture bytes are unchanged.

- [ ] **Step 5: Run fixture and complete SCL suites**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~Scl"
```

Expected: PASS; the real V21 fixture is Full, provenance/hash/sanitization pass, and all synthetic lexical/parser/comparer/UI cases pass.

- [ ] **Step 6: Update the README feature and roadmap wording**

Add this feature section immediately after `### Visual LAD Diff`:

```markdown
### Structured SCL Diff

- **Semantic grouping**: SCL changes are grouped by file, block, declaration section, region, statement, comment, and explicit ungrouped source.
- **Shared syntax model**: highlighting uses the same bounded lexer tokens and source spans used by comparison.
- **Safe degradation**: recoverable syntax is shown as `Structured · Partial`; source without reliable block structure uses an explicit selectable `Text · Fallback` view.
```

Replace the SCL roadmap bullet with:

```markdown
   - Extend the structured SCL compatibility corpus when later TIA Portal releases change exported source syntax.
```

Do not claim compilation, execution, or complete IEC/SCL language validation.

- [ ] **Step 7: Commit the fixture and documentation slice**

```powershell
git add src/TiaGitAddIn.Tests/TestData/Scl/V21 src/TiaGitAddIn.Tests/Services/Comparison/SclFixtureCompatibilityTests.cs README.md
git commit -m "test: add v21 scl compatibility fixture"
```

## Dependency and Parallelization Map

1. Use this canonical integration order: VCI Task 1 -> foundation Tasks 1-10 -> FBD Tasks 1-9 -> SCL Tasks 1-9, rebased over the FBD shared-file commits -> VCI Tasks 2-4 -> foundation Task 11 -> VCI Tasks 5-8.
2. Foundation is the hard prerequisite for SCL feature work because it creates every shared result, fallback, composition, raw-text, and WPF host contract. FBD-owned and SCL-owned files may be developed on parallel branches after foundation, but their integration commits are serialized by item 1.
3. Tasks 1 through 8 in this plan execute in order; each consumes the previous task's exact public surface.
4. The fresh V21 fixture acquisition in Task 9 Step 3 can run in parallel with Tasks 1 through 8, but the manifest/compatibility commit waits for Task 6.
5. FBD and SCL both append to `src/TiaGitAddIn/UI/GitPanelLaunchService.cs` and `src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml`. Rebase the SCL integration commit over FBD and preserve both strategies, both specialized factories, and both implicit templates.
6. VCI owns `TiaGitAddIn.IntegrationTests`, Coverlet 6.0.4, `scripts/Invoke-TestGate.ps1`, CI workflows, and live evidence. Its additive `GitPanelLaunchService.cs` adapter-logging edit lands after foundation, FBD, and SCL composition and preserves every registered strategy/factory.
7. After all four plans land, run the VCI plan's reusable gate once over the integrated tree and publish no artifact until its live-TIA candidate gate passes.

## Final Verification Gate

- [ ] Restore and build the complete solution without Add-In packaging:

```powershell
dotnet restore TiaGitAddIn.sln
dotnet build TiaGitAddIn.sln -c Release --no-restore -p:EnableTiaAddInPackaging=false
```

Expected: both commands exit zero with no new compiler warning attributable to SCL files.

- [ ] Run focused SCL and full net48 tests with the xUnit v2/VSTest runner:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release --no-build -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~Scl"
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release --no-build -p:EnableTiaAddInPackaging=false -- RunConfiguration.DisableAppDomain=true
```

Expected: all focused and full tests PASS; no test depends on order, TIA installation, current time, network, or shared mutable state.

- [ ] After the VCI workflow plan is present, run its merged coverage gate:

```powershell
pwsh -NoProfile -File scripts/Invoke-TestGate.ps1 -Configuration Release
```

Expected: net48 JSON merges into the net8 run, `TestResults/Coverage/coverage.cobertura.xml` exists, and total production line coverage is at least 80.00%.

- [ ] Run security/boundary/static scans:

```powershell
$forbidden = rg -n -i "Siemens\.Automation\.CommonServices\.Compare|CompareEditorStarter|SactService|ISactService" src/TiaGitAddIn.Core/Comparison/Scl src/TiaGitAddIn.Core/Services/Comparison/SclComparisonStrategy.cs src/TiaGitAddIn/UI/ViewModels/Comparison/SclDiffViewModel.cs
if ($LASTEXITCODE -eq 0) { throw "Forbidden comparison/compiler/obsolete dependency found:`n$forbidden" }

$asyncVoid = rg -n "async\s+void" src/TiaGitAddIn.Core/Comparison/Scl src/TiaGitAddIn.Core/Services/Comparison/SclComparisonStrategy.cs src/TiaGitAddIn/UI/ViewModels/Comparison/SclDiffViewModel.cs
if ($LASTEXITCODE -eq 0) { throw "Unexpected async void found:`n$asyncVoid" }

$whitespace = git diff --check
if ($LASTEXITCODE -ne 0) { throw "Whitespace validation failed:`n$whitespace" }
git diff -- . ':!docs/superpowers/plans/*' |
  Select-String -Pattern '(?i)((api[_-]?key|password|passwd|secret|access[_-]?token)\s*[:=]\s*["''][^"'']+|BEGIN [A-Z ]*PRIVATE KEY)' |
  ForEach-Object { throw "Potential secret in diff: $_" }
```

Expected: every command exits zero; scans find no internal Siemens compare type, obsolete SACT dependency, SCL `async void`, credential-like value, or whitespace error.

- [ ] Enforce the repository file-size limit over every SCL production file:

```powershell
$sclFiles = Get-ChildItem src/TiaGitAddIn.Core/Comparison/Scl,src/TiaGitAddIn.Core/Services/Comparison/SclComparisonStrategy.cs,src/TiaGitAddIn.Core/Models/Comparison/SclPresentation.cs,src/TiaGitAddIn/UI/ViewModels/Comparison/SclDiffViewModel.cs,src/TiaGitAddIn/UI/Mapping/SclPresentationViewModelFactory.cs,src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml,src/TiaGitAddIn/UI/Views/Comparison/SclDiffView.xaml.cs -File
$oversized = $sclFiles | Where-Object { (Get-Content -LiteralPath $_.FullName).Count -gt 800 }
if ($oversized) { throw "SCL production files exceed 800 lines: $($oversized.FullName -join ', ')" }
```

Expected: exit zero with no output.

- [ ] Refresh the architecture graph after code changes:

```powershell
graphify update .
git rev-parse HEAD
Select-String -Path graphify-out/GRAPH_REPORT.md -Pattern (git rev-parse --short HEAD)
```

Expected: graph update exits zero and the report identifies the current source revision according to the repository's graphify version.

## Acceptance-Criteria Traceability

| Criterion | Planned evidence |
|---|---|
| AC-007 | Tasks 1-7 immutable constructors, read-only collections, and mutation tests |
| AC-008 | Task 6 one `SclPresentation` strategy result; Task 7 one specialized mapper factory |
| AC-018 | Task 6 unrecoverable text fallback with safe parser diagnostics |
| AC-022, AC-023 | Task 6 shared mode/support result and raw text; Tasks 7-8 metadata/host binding |
| AC-025, AC-027, AC-118 | Task 7 consumes foundation latest-generation/dispatcher/cancellation tests; Task 6 rethrows cancellation |
| AC-028 through AC-032 | Task 8 implicit template, limitation/diagnostic host, file-size check, and STA runtime binding tests |
| AC-055 | Task 1 lexer manifest covering comments, strings, quoted identifiers, escapes, operators, and spans |
| AC-056, AC-057 | Task 2 five block kinds and all declaration semantic fields |
| AC-058, AC-059 | Task 3 nested region/source-order grouping and four exact recovery boundaries |
| AC-060 | Task 4 semantic block/declaration identity independent of serialization order |
| AC-061 through AC-063 | Task 5 unique declaration rename only; ambiguous/block/region controls |
| AC-064, AC-065 | Task 4 normalized statement tokens and independent comment comparison |
| AC-066 | Task 6 explicit reliable Partial versus no-block Text Fallback threshold |
| AC-067 | Tasks 7-8 source-order groups and the same lexer token kind/span in highlighting |
| AC-096 | Tasks 1 and 3 exact token/nesting N and N+1 tests |
| AC-098 | Task 6 safe `PlcSourceLocation`; final redaction/secret scans consume foundation boundary |
| AC-100, AC-102 | Task 9 V21 build/encoding/BOM/sanitization/hash/support manifest and compatibility test |
| AC-107 | Tasks 1-6 project-owned tolerant parser only; final compiler/internal dependency scan |
| AC-117 | Task 6 uses the foundation factory/invariant suite for Full, Partial, and Fallback |

## Completion Commit Sequence

1. `feat: add bounded scl lexer`
2. `feat: parse scl blocks and declarations`
3. `feat: add tolerant scl parser recovery`
4. `feat: compare scl structure`
5. `feat: detect unique scl declaration renames`
6. `feat: route structured scl comparison`
7. `feat: map structured scl presentation`
8. `feat: render structured scl diff`
9. `test: add v21 scl compatibility fixture`

Do not squash these boundaries during implementation review; each commit is an independently testable RED/GREEN slice.
