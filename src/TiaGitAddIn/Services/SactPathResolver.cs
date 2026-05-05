using System;
using System.IO;
using Microsoft.Win32;

namespace TiaGitAddIn.Services
{
    public sealed class SactPathResolver : ISactPathResolver
    {
        private const string RegistryKey = @"SOFTWARE\Siemens\Automation\CompareTool";
        private const string DefaultPath = @"C:\Program Files\Siemens\Automation\SIMATIC Automation Compare Tool";

        public string? ResolveSiemensInstallPath()
        {
            // 1. Registry (64-bit view preferred for Siemens tools)
            try
            {
                using (var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(RegistryKey))
                {
                    var val = key?.GetValue("InstallDir") as string;
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
            // Check if node is in PATH
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("node", "-v")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = System.Diagnostics.Process.Start(psi))
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
