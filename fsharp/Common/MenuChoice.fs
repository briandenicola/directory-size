namespace DirectorySize.Common

open DirectorySize.Common

type MenuChoice(path: string, label: string) =
    static member Up = MenuChoice("up", Utils.escapeMarkup("[[..] Up]"))
    static member Exit = MenuChoice("exit", Utils.escapeMarkup("[Exit]"))

    member _.Path = path
    member _.Label = label

    override _.ToString() = label
