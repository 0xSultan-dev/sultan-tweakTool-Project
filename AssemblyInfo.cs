using System.Reflection;
using System.Runtime.InteropServices;

// Assembly identity & version metadata.
// Compiling this alongside WinDebloater.cs makes csc emit a proper Win32 version resource,
// which also fixes the previously stale OriginalFilename baked into the binary.
[assembly: AssemblyTitle("Sultan's Ultimate Windows Optimizer")]
[assembly: AssemblyDescription("Lightweight, 100% reversible Windows performance & latency optimizer.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("0xSultan")]
[assembly: AssemblyProduct("Ultimate Tweak Tool")]
[assembly: AssemblyCopyright("Copyright (c) 2026 0xSultan")]
[assembly: AssemblyTrademark("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
