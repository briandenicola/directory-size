namespace DirectorySize.Common;

using System.Globalization;

public static class Utils
{
    private const double MB = 1048576.0;
    private const int MaxPathLength = 50;
    private const int TrimmedPathLength = 48;

    public static string ToNumberFormat(long val) => 
        string.Format("{0:#,0}", val);

    public static string ToMB(long val) => 
        string.Create(CultureInfo.CurrentCulture, $"{Math.Round(val / MB, 2):#,0.00}");

    public static string TrimPath(ReadOnlySpan<char> path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Length > MaxPathLength
            ? $"{new string(fileName[..TrimmedPathLength])}…"
            : new string(fileName);
    }

    public static string EscapeMarkup(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        Span<char> buffer = stackalloc char[text.Length * 2];
        int pos = 0;

        foreach (var ch in text)
        {
            if (ch == '[')
            {
                if (pos + 1 < buffer.Length)
                {
                    buffer[pos++] = '[';
                    buffer[pos++] = '[';
                }
            }
            else if (ch == ']')
            {
                if (pos + 1 < buffer.Length)
                {
                    buffer[pos++] = ']';
                    buffer[pos++] = ']';
                }
            }
            else
            {
                if (pos < buffer.Length)
                    buffer[pos++] = ch;
            }
        }

        return new string(buffer[..pos]);
    }

    public static string ToRelativeTime(DateTime value, DateTime now)
    {
        if (value == DateTime.MinValue)
            return "-";

        if (value > now)
            return "0s";

        var delta = now - value;

        if (delta.TotalSeconds < 60)
            return $"{(int)delta.TotalSeconds}s";
        if (delta.TotalMinutes < 60)
            return $"{(int)delta.TotalMinutes}m";
        if (delta.TotalHours < 24)
            return $"{(int)delta.TotalHours}h";
        if (delta.TotalDays < 7)
            return $"{(int)delta.TotalDays}d";
        if (delta.TotalDays < 28)
            return $"{(int)(delta.TotalDays / 7)}w";
        if (delta.TotalDays < 365)
            return $"{(int)(delta.TotalDays / 30)}mo";

        return $"{(int)(delta.TotalDays / 365)}y";
    }
}
