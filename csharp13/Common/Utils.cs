namespace DirectorySize;

public static class Utils
{
    public static readonly double MB = 1048576.0;
    public static string ToNumberFormat(long val) => string.Format("{0:#,0}", val);
    public static string ToMB(long val) => string.Format("{0:#,0.00}", Math.Round(val / MB, 2));
    public static string EscapeMarkup(string text) =>
        string.IsNullOrEmpty(text)
        ? text
        : text.Replace("[", "[[").Replace("]", "]]");
}