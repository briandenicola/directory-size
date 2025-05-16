namespace DirectorySize;

public static class Utils
{
    public static string EscapeMarkup(string text) =>
        string.IsNullOrEmpty(text)
        ? text
        : text.Replace("[", "[[").Replace("]", "]]");
}