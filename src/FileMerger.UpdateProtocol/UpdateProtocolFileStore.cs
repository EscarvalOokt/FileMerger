using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileMerger.UpdateProtocol;

public sealed class UpdateProtocolFileStore
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public Task WriteRequestAsync(
        string path,
        UpdateInstallationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return WriteAtomicAsync(path, request, cancellationToken);
    }

    public Task<UpdateInstallationRequest> ReadRequestAsync(string path, CancellationToken cancellationToken = default)
    {
        return ReadAsync<UpdateInstallationRequest>(path, cancellationToken);
    }

    public Task WriteReceiptAsync(
        string path,
        UpdateInstallationReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return WriteAtomicAsync(path, receipt, cancellationToken);
    }

    public Task<UpdateInstallationReceipt> ReadReceiptAsync(string path, CancellationToken cancellationToken = default)
    {
        return ReadAsync<UpdateInstallationReceipt>(path, cancellationToken);
    }

    public Task WriteAcknowledgementAsync(
        string path,
        UpdateRestartVerificationAcknowledgement acknowledgement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acknowledgement);
        return WriteAtomicAsync(path, acknowledgement, cancellationToken);
    }

    public Task<UpdateRestartVerificationAcknowledgement> ReadAcknowledgementAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return ReadAsync<UpdateRestartVerificationAcknowledgement>(path, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        T? value = await JsonSerializer.DeserializeAsync<T>(stream, _serializerOptions, cancellationToken);
        return value ?? throw new InvalidDataException($"Update protocol document '{path}' is empty or invalid.");
    }

    private static async Task WriteAtomicAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Update protocol document path has no parent directory.");

        Directory.CreateDirectory(directory);

        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (FileStream stream = new(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81920,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, value, _serializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // A failed atomic write must not hide the original failure.
            }
        }
    }
}