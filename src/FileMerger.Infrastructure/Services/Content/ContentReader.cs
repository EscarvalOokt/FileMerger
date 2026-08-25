using System.Security;
using System.Text;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Infrastructure.Services.Content;

public sealed class ContentReader : IContentReader
{
    private static readonly UTF8Encoding _strictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public ContentReadResult Read(InputFile file, InputReadOptions options)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            byte[] bytes = File.ReadAllBytes(file.FullPath);

            return options.EncodingMode switch
            {
                InputEncodingMode.Utf8 => ReadAsUtf8(bytes, file),
                InputEncodingMode.Specific => ReadAsSpecific(bytes, file, options),
                InputEncodingMode.Auto => ReadAuto(bytes, file, options),
                _ => ContentReadResult.Failure(CreateFailure(
                    file,
                    "Unsupported input encoding mode."))
            };
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is NotSupportedException ||
            ex is SecurityException)
        {
            return ContentReadResult.Failure(CreateFailure(
                file,
                ex.Message));
        }
    }

    private static ContentReadResult ReadAsUtf8(byte[] bytes, InputFile file)
    {
        try
        {
            string content = RemoveLeadingBom(_strictUtf8.GetString(bytes));
            return ContentReadResult.Success(content, _strictUtf8.WebName);
        }
        catch (DecoderFallbackException ex)
        {
            return ContentReadResult.Failure(CreateFailure(file, ex.Message));
        }
    }

    private static ContentReadResult ReadAsSpecific(byte[] bytes, InputFile file, InputReadOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PreferredEncodingName))
        {
            return ContentReadResult.Failure(CreateFailure(
                file,
                "Preferred input encoding name is not specified."));
        }

        Encoding encoding;
        try
        {
            encoding = Encoding.GetEncoding(options.PreferredEncodingName);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
        {
            return ContentReadResult.Failure(CreateFailure(
                file,
                $"Unknown input encoding '{options.PreferredEncodingName}': {ex.Message}"));
        }

        try
        {
            string content = RemoveLeadingBom(encoding.GetString(bytes));
            return ContentReadResult.Success(content, encoding.WebName);
        }
        catch (DecoderFallbackException ex)
        {
            return ContentReadResult.Failure(CreateFailure(file, ex.Message));
        }
    }

    private static ContentReadResult ReadAuto(byte[] bytes, InputFile file, InputReadOptions options)
    {
        if (TryDetectEncodingFromBom(bytes, out Encoding? bomEncoding))
        {
            try
            {
                ArgumentNullException.ThrowIfNull(bomEncoding);

                string content = RemoveLeadingBom(bomEncoding.GetString(bytes));
                return ContentReadResult.Success(content, bomEncoding.WebName);
            }
            catch (DecoderFallbackException ex)
            {
                return ContentReadResult.Failure(CreateFailure(file, ex.Message));
            }
        }

        try
        {
            string utf8Content = RemoveLeadingBom(_strictUtf8.GetString(bytes));
            return ContentReadResult.Success(utf8Content, _strictUtf8.WebName);
        }
        catch (DecoderFallbackException)
        {
        }

        Encoding fallbackEncoding;
        try
        {
            fallbackEncoding = ResolveFallbackEncoding(options.FallbackEncodingName);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
        {
            return ContentReadResult.Failure(CreateFailure(
                file,
                $"Unknown fallback input encoding '{options.FallbackEncodingName}': {ex.Message}"));
        }

        try
        {
            string fallbackContent = RemoveLeadingBom(fallbackEncoding.GetString(bytes));
            return ContentReadResult.Success(fallbackContent, fallbackEncoding.WebName);
        }
        catch (DecoderFallbackException ex)
        {
            return ContentReadResult.Failure(CreateFailure(file, ex.Message));
        }
    }

    private static Encoding ResolveFallbackEncoding(string? fallbackEncodingName)
    {
        if (string.IsNullOrWhiteSpace(fallbackEncodingName))
            return Encoding.Default;

        return Encoding.GetEncoding(fallbackEncodingName);
    }

    private static bool TryDetectEncodingFromBom(byte[] bytes, out Encoding? encoding)
    {
        encoding = null;

        if (bytes is [0xEF, 0xBB, 0xBF, ..])
        {
            encoding = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: true,
                throwOnInvalidBytes: true);
            return true;
        }

        if (bytes is [0xFF, 0xFE, ..])
        {
            encoding = new UnicodeEncoding(
                bigEndian: false,
                byteOrderMark: true,
                throwOnInvalidBytes: true);
            return true;
        }

        if (bytes is [0xFE, 0xFF, ..])
        {
            encoding = new UnicodeEncoding(
                bigEndian: true,
                byteOrderMark: true,
                throwOnInvalidBytes: true);
            return true;
        }

        if (bytes is [0xFF, 0xFE, 0x00, 0x00, ..])
        {
            encoding = new UTF32Encoding(
                bigEndian: false,
                byteOrderMark: true,
                throwOnInvalidCharacters: true);
            return true;
        }

        if (bytes is [0x00, 0x00, 0xFE, 0xFF, ..])
        {
            encoding = new UTF32Encoding(
                bigEndian: true,
                byteOrderMark: true,
                throwOnInvalidCharacters: true);
            return true;
        }

        return false;
    }

    private static string RemoveLeadingBom(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return content.Length > 0 && content[0] == '\uFEFF'
            ? content[1..]
            : content;
    }

    private static ValidationIssue CreateFailure(InputFile file, string details)
    {
        return new ValidationIssue(
            severity: ValidationSeverity.Warning,
            code: "file.read.failed",
            message: $"Failed to read '{file.RelativePath}': {details}");
    }
}