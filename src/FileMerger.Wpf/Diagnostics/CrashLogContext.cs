using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FileMerger.Wpf.Diagnostics;

public sealed record CrashLogContext(
    DateTime OccurredAtUtc,
    string ApplicationName,
    string? ApplicationVersion,
    string? InformationalVersion,
    string RuntimeVersion,
    string OSVersion,
    string ProcessArchitecture,
    bool Is64BitProcess,
    string BaseDirectory,
    string CurrentDirectory,
    string ExceptionSource)
{
    public static CrashLogContext Create(
        string exceptionSource,
        DateTime? occurredAtUtc = null,
        Assembly? assembly = null)
    {
        if (string.IsNullOrWhiteSpace(exceptionSource))
            throw new ArgumentException("Exception source cannot be empty.", nameof(exceptionSource));

        Assembly resolvedAssembly =
            assembly
            ?? Assembly.GetEntryAssembly()
            ?? typeof(CrashLogContext).Assembly;

        AssemblyName assemblyName = resolvedAssembly.GetName();

        string? informationalVersion = resolvedAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        return new CrashLogContext(
            OccurredAtUtc: (occurredAtUtc ?? DateTime.UtcNow).ToUniversalTime(),
            ApplicationName: assemblyName.Name ?? "FileMerger",
            ApplicationVersion: assemblyName.Version?.ToString(),
            InformationalVersion: informationalVersion,
            RuntimeVersion: RuntimeInformation.FrameworkDescription,
            OSVersion: RuntimeInformation.OSDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            Is64BitProcess: Environment.Is64BitProcess,
            BaseDirectory: AppContext.BaseDirectory,
            CurrentDirectory: GetCurrentDirectorySafe(),
            ExceptionSource: exceptionSource);
    }

    private static string GetCurrentDirectorySafe()
    {
        try
        {
            return Directory.GetCurrentDirectory();
        }
        catch (Exception ex)
        {
            return $"Unavailable: {ex.GetType().FullName}: {ex.Message}";
        }
    }
}