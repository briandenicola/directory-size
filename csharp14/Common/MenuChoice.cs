namespace DirectorySize.Common;

public class MenuChoice(string path, string label)
{
    public static readonly MenuChoice Up = new("up", Utils.EscapeMarkup("[[..] Up]"));
    public static readonly MenuChoice Exit = new("exit", Utils.EscapeMarkup("[Exit]"));

    public string Path { get; } = path;
    public string Label { get; } = label;

    public override string ToString() => Label;
}