using System.IO;
using System.Text;

namespace FileMerger.Wpf.Diagnostics;

public sealed class CrashLogWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly CrashLogPathPolicy _pathPolicy;
    private readonly CrashLogFormatter _formatter;

    public CrashLogWriter(
        CrashLogPathPolicy pathPolicy,
        CrashLogFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(pathPolicy);
        ArgumentNullException.ThrowIfNull(formatter);

        _pathPolicy = pathPolicy;
        _formatter = formatter;
    }

    public CrashLogWriteResult Write(Exception exception, CrashLogContext context)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            string directory = _pathPolicy.GetCrashLogDirectory();
            Directory.CreateDirectory(directory);

            string path = _pathPolicy.CreateCrashLogPath(context.OccurredAtUtc);
            string content = CrashLogFormatter.Format(exception, context);

            File.WriteAllText(path, content, Utf8WithoutBom);

            return CrashLogWriteResult.Success(path);
        }
        catch (Exception ex)
        {
            return CrashLogWriteResult.Failure(ex);
        }
    }
}