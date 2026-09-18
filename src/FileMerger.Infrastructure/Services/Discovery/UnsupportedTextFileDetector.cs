using System.Security;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Infrastructure.Services.Discovery;

public sealed class UnsupportedTextFileDetector : IUnsupportedTextFileDetector
{
    public bool IsTextCandidate(string filePath, UnsupportedTextFallbackOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsEnabled)
            return false;

        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            if (!File.Exists(filePath))
                return false;

            FileInfo fileInfo = new(filePath);

            if (fileInfo.Length > options.MaxFileSizeBytes)
                return false;

            int probeLength = (int)Math.Min(fileInfo.Length, options.ProbeSizeBytes);
            if (probeLength == 0)
                return true;

            byte[] buffer = new byte[probeLength];

            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
                return true;

            int controlCharacterCount = 0;

            for (int i = 0; i < bytesRead; i++)
            {
                byte value = buffer[i];

                if (value == 0)
                    return false;

                if (IsControlCharacter(value))
                    controlCharacterCount++;
            }

            double controlCharacterRatio = controlCharacterCount / (double)bytesRead;

            return controlCharacterRatio <= options.MaxControlCharacterRatio;
        }
        catch (Exception ex) when (ex is IOException ||
                                   ex is UnauthorizedAccessException ||
                                   ex is NotSupportedException ||
                                   ex is SecurityException)
        {
            return false;
        }
    }

    private static bool IsControlCharacter(byte value)
    {
        return value < 0x20 && value != (byte)'\t' && value != (byte)'\n' && value != (byte)'\r';
    }
}