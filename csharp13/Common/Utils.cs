namespace DirectorySize.Common;

using System.Globalization;

public static class Utils
{
    public static readonly double MB = 1048576.0;

    public static string ToNumberFormat(long val) => string.Format("{0:#,0}", val);

    public static string ToMB(long val) => String.Create(CultureInfo.CurrentCulture, $"{Math.Round(val / MB, 2):#,0.00}"); //string.Format("{0:#,0.00}", Math.Round(val / MB, 2));

    public static string TrimPath(string path) =>
        Path.GetFileName(path) is var fileName && fileName.Length > 50
            ? fileName[..48] + "…"
            : fileName;

    public static string EscapeMarkup(string text) =>
        string.IsNullOrEmpty(text)
        ? text
        : text.Replace("[", "[[").Replace("]", "]]");
}