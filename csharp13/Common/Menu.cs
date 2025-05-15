namespace DirectorySize;

public class MenuChoice
{
    public string Path { get; }
    public string Label { get; }

    public MenuChoice(string path, string label)
    {
        Path = path;
        Label = label;
    }

    public override string ToString() => Label;
}