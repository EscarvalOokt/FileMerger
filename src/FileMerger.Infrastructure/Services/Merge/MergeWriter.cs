using System.Text;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Infrastructure.Services.Merge;

public sealed class MergeWriter : IMergeWriter
{
    public void Write(MergeOutput output, OutputTarget target)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(target);

        string? directory = Path.GetDirectoryName(target.Path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var encoding = Encoding.GetEncoding(target.EncodingName);
        File.WriteAllText(target.Path, output.Content, encoding);
    }
}