namespace DirectorySize;

public class MenuChoice
{
    public static readonly MenuChoice Up = new("up", Utils.EscapeMarkup("[[..] Up]"));
    public static readonly MenuChoice Exit = new("exit", Utils.EscapeMarkup("[Exit]"));

    public string Path { get; }
    public string Label { get; }

    public MenuChoice(string path, string label)
    {
        Path = path;
        Label = label;
    }

    public override string ToString() => Label;
}