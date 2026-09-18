using System.Globalization;
using System.Text;

namespace FileMerger.Wpf.Diagnostics;

public static class CrashLogFormatter
{
    public static string Format(Exception exception, CrashLogContext context)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(context);

        StringBuilder builder = new();

        builder.AppendLine("FileMerger crash log");
        builder.AppendLine(
            $"Timestamp UTC: {context.OccurredAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Source: {context.ExceptionSource}");
        builder.AppendLine();

        builder.AppendLine("Application:");
        AppendLine(builder, "Name", context.ApplicationName);
        AppendLine(builder, "Version", context.ApplicationVersion);
        AppendLine(builder, "InformationalVersion", context.InformationalVersion);
        builder.AppendLine();

        builder.AppendLine("Runtime:");
        AppendLine(builder, ".NET", context.RuntimeVersion);
        AppendLine(builder, "OS", context.OSVersion);
        AppendLine(builder, "Architecture", context.ProcessArchitecture);
        AppendLine(builder, "Is64BitProcess", context.Is64BitProcess.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine();

        builder.AppendLine("Process:");
        AppendLine(builder, "BaseDirectory", context.BaseDirectory);
        AppendLine(builder, "CurrentDirectory", context.CurrentDirectory);
        builder.AppendLine();

        builder.AppendLine("Exception:");
        AppendException(builder, exception, depth: 0);

        return builder.ToString();
    }

    private static void AppendLine(StringBuilder builder, string name, string? value)
    {
        builder.Append("  ");
        builder.Append(name);
        builder.Append(": ");
        builder.AppendLine(string.IsNullOrWhiteSpace(value) ? "(none)" : value);
    }

    private static void AppendException(StringBuilder builder, Exception exception, int depth)
    {
        string indent = new(' ', depth * 2);

        builder.Append(indent);
        builder.Append('[');
        builder.Append(depth);
        builder.Append("] ");
        builder.AppendLine(exception.GetType().FullName);

        builder.Append(indent);
        builder.Append("Message: ");
        builder.AppendLine(exception.Message);

        builder.Append(indent);
        builder.AppendLine("Stack trace:");

        if (string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            builder.Append(indent);
            builder.AppendLine("(no stack trace)");
        }
        else
        {
            builder.AppendLine(exception.StackTrace);
        }

        if (exception is AggregateException { InnerExceptions.Count: > 0 } aggregateException)
        {
            for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
            {
                builder.AppendLine();
                builder.Append(indent);
                builder.Append("Aggregate inner exception #");
                builder.Append(i + 1);
                builder.AppendLine(":");

                AppendException(builder, aggregateException.InnerExceptions[i], depth + 1);
            }

            return;
        }

        if (exception.InnerException is null)
            return;

        builder.AppendLine();
        builder.Append(indent);
        builder.AppendLine("Inner exception:");

        AppendException(builder, exception.InnerException, depth + 1);
    }
}