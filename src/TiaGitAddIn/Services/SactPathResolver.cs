using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace TiaGitAddIn.Services
{
    public sealed class SactPathResolver(string? siemensOverride = null, string? nodeOverride = null) : ISactPathResolver
    {
        private const string RegistryKey = @"SOFTWARE\Siemens\Automation\CompareTool";
        private const string DefaultPath = @"C:\Program Files\Siemens\Automation\SIMATIC Automation Compare Tool";

        public string? ResolveSiemensInstallPath()
        {
            // 0. Override
            if (!string.IsNullOrWhiteSpace(siemensOverride) && Directory.Exists(siemensOverride))
                return siemensOverride;

            // 1. Registry (64-bit view preferred for Siemens tools)
            try
            {
                using (RegistryKey baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(RegistryKey))
                {
                    string? val = key?.GetValue("InstallDir") as string;
                    if (!string.IsNullOrEmpty(val) && Directory.Exists(val)) return val;
                }
            }
            catch
            {
                // Registry access might fail in some environments
            }

            // 2. Default Path
            if (Directory.Exists(DefaultPath)) return DefaultPath;

            return null;
        }

        public string? ResolveNodePath()
        {
            // 0. Override
            if (!string.IsNullOrWhiteSpace(nodeOverride))
                return nodeOverride;

            // Check if node is in PATH
            try
            {
                ProcessStartInfo psi = new("node", "-v")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process proc = System.Diagnostics.Process.Start(psi))
                {
                    if (proc != null)
                    {
                        // Wait with 2 second timeout to avoid hanging UI thread
                        if (proc.WaitForExit(2000))
                        {
                            if (proc.ExitCode == 0) return "node";
                        }
                        else
                        {
                            proc.Kill();
                        }
                    }
                }
            }
            catch
            {
                // Node might not be in PATH
            }
            return null;
        }
    }
}
