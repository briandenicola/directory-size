namespace DirectorySize.Common

open System
open System.Globalization
open System.IO
open System.Text

module Utils =
    let private MB = 1048576.0
    let private MaxPathLength = 50
    let private TrimmedPathLength = 48

    let toNumberFormat (value: int64) =
        value.ToString("#,0", CultureInfo.CurrentCulture)

    let toMB (value: int64) =
        let rounded = Math.Round(float value / MB, 2)
        System.String.Format(CultureInfo.CurrentCulture, "{0:#,0.00}", rounded)

    let trimPath (path: ReadOnlySpan<char>) =
        let fileName = Path.GetFileName(path)
        if fileName.Length > MaxPathLength then
            let prefix = fileName.Slice(0, TrimmedPathLength).ToString()
            prefix + "…"
        else
            fileName.ToString()

    let escapeMarkup (text: string) =
        if String.IsNullOrEmpty(text) then
            text
        else
            let builder = StringBuilder(text.Length * 2)
            for ch in text do
                match ch with
                | '[' -> builder.Append('[').Append('[') |> ignore
                | ']' -> builder.Append(']').Append(']') |> ignore
                | _ -> builder.Append(ch) |> ignore
            builder.ToString()

    let toRelativeTime (value: DateTime) (now: DateTime) =
        if value = DateTime.MinValue then
            "-"
        elif value > now then
            "0s"
        else
            let delta = now - value
            if delta.TotalSeconds < 60.0 then
                sprintf "%ds" (int delta.TotalSeconds)
            elif delta.TotalMinutes < 60.0 then
                sprintf "%dm" (int delta.TotalMinutes)
            elif delta.TotalHours < 24.0 then
                sprintf "%dh" (int delta.TotalHours)
            elif delta.TotalDays < 7.0 then
                sprintf "%dd" (int delta.TotalDays)
            elif delta.TotalDays < 28.0 then
                sprintf "%dw" (int (delta.TotalDays / 7.0))
            elif delta.TotalDays < 365.0 then
                sprintf "%dmo" (int (delta.TotalDays / 30.0))
            else
                sprintf "%dy" (int (delta.TotalDays / 365.0))
