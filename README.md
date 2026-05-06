# TIA Git Add-In

TIA Portal V21 Add-In for working with Git from inside TIA Portal Version Control (VCI) workflows.

The add-in targets `.NET Framework 4.8` and packages as a native TIA Portal V21 `.addin` file. It provides a specialized Git panel integrated directly into the TIA Portal UI, focusing on the unique needs of PLC developers using VCI.

## Features & Status

### Native SimaticML Decoding (NEW)

- **Zero Dependencies**: Completely removed dependencies on Node.js and external Siemens ACTool scripting.
- **Native Parser**: Custom C# `XDocument`-based parser for SimaticML files.
- **Structural Comparison**: Logic and interface are compared directly in C#, allowing for deep semantic diffing of PLC blocks.

### Visual LAD Diff

- **Side-by-Side View**: Compare "OLD" (parent) and "NEW" (commit/working tree) versions horizontally.
- **Graphic Rendering**: Simplified geometric representation of Ladder Logic (Contacts, Coils, Boxes, Powerrails).
- **Branching Layout**: Custom layout engine that correctly handles multiple rungs and complex branching from the Powerrail.
- **Color Coding**: Semantic highlighting of changes (Yellow: Changed, Green: Added, Red: Removed).

### Git Integration

- **VCI-Aware**: Automatically detects Git repositories associated with TIA Portal VCI workspaces.
- **Core Operations**: View status, history, and commit changes without leaving TIA Portal.
- **Transparent Execution**: Uses your local `git.exe` for all operations.

## Prerequisites

- **TIA Portal V21** (required for the Add-In API and Publisher).
- **.NET SDK** compatible with the repo `global.json`.
- **Local Git** installation available in system PATH.

## Build & Installation

### Build

```powershell
dotnet restore TiaGitAddIn.sln
dotnet build TiaGitAddIn.sln --no-restore
```

The build process automatically packages the result into a `.addin` file using the Siemens Add-In Publisher:
`src/TiaGitAddIn/bin/Debug/net48/TiaGitAddIn.addin`

### Installation

1. Copy the `TiaGitAddIn.addin` file to your TIA Portal Add-Ins folder (typically `%TIA_INSTALL_DIR%\AddIns`).
2. Open TIA Portal V21.
3. Enable the Add-In in the "Add-ins" task card.
4. Right-click on a project or VCI workspace to find the "Git" menu items.

## Development Roadmap

1. **Language Support**
   - Extend the visual diff to support **FBD** (Function Block Diagram).
   - Implement structured text diffing for **SCL** with block-aware syntax highlighting.

2. **Advanced Comparison**
   - Deep interface comparison (detecting changes in Datatypes, Retain settings, and Comments).
   - Block metadata comparison (Attributes, Optimized Access, etc.).
   - Multi-block comparison within a single commit.

3. **UI/UX Polishing**
   - Improved orthogonal wire routing for cleaner visual diagrams.
   - Interactive zooming and panning in the visual diff viewer.
   - Integration with TIA Portal's theme and system colors.

4. **HMI & Configuration**
   - Research visual diffing for HMI Screens and Tag Tables.
   - Support for comparing Hardware Configuration artifacts.

## Project Structure

```text
src/
  TiaGitAddIn/
    Services/SimaticMl/  Native SimaticML parser and comparison engine
    Services/            Git process execution and repository discovery
    Models/              Core data models for Git, LAD, and Sact/SimaticML
    UI/                  WPF MVVM (ViewModels, Views, Converters)
    Entry/               TIA Portal Add-In entry points and menu registration
  TiaGitAddIn.Tests/     Unit and UI logic tests
```
