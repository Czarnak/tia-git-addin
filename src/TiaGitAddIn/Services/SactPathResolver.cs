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
            // 1. Registry
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(RegistryKey))
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
                        proc.WaitForExit();
                        if (proc.ExitCode == 0) return "node";
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
