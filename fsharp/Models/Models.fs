namespace DirectorySize.Models

open System
open System.Collections.Generic

[<AllowNullLiteral>]
type DirectoryStatistics() =
    let mutable path = ""
    let mutable directorySize = 0L
    let mutable fileCount = 0L
    let mutable lastModified = DateTime.MinValue
    let subdirectories = HashSet<DirectoryStatistics>()

    new(path: string, directorySize: int64, fileCount: int64) as this =
        DirectoryStatistics() then
            this.Path <- path
            this.DirectorySize <- directorySize
            this.FileCount <- fileCount

    member _.Subdirectories = subdirectories

    member _.Path
        with get () = path
        and set value =
            if obj.ReferenceEquals(value, null) then raise (ArgumentNullException(nameof value))
            path <- value

    member _.DirectorySize
        with get () = directorySize
        and set value = directorySize <- value

    member _.FileCount
        with get () = fileCount
        and set value = fileCount <- value

    member _.LastModified
        with get () = lastModified
        and set value = lastModified <- value

[<Struct>]
type DirectoryErrorInfo =
    { Path: string
      ErrorDescription: string }
