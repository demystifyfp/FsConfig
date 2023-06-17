namespace FsConfig

open System
open System.Text.RegularExpressions
open System.ComponentModel

type ConfigParseError =
    | BadValue of (string * string)
    | NotFound of string
    | NotSupported of string

type Prefix = Prefix of string
type Separator = Separator of string
type SplitCharacter = SplitCharacter of char

type ConfigParseResult<'T> = Result<'T, ConfigParseError>

type FieldNameCanonicalizer = Prefix -> string -> string

[<AttributeUsage(AttributeTargets.Property, AllowMultiple = false)>]
type CustomNameAttribute(name: string) =
    inherit Attribute()
    member __.Name = name

[<AttributeUsage(AttributeTargets.Property, AllowMultiple = false)>]
type DefaultValueAttribute(value: string) =
    inherit Attribute()
    member __.Value = value

[<AttributeUsage(AttributeTargets.Property, AllowMultiple = false)>]
type ListSeparatorAttribute(splitCharacter: char) =
    inherit Attribute()
    member __.SplitCharacter = SplitCharacter splitCharacter

[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type ConventionAttribute(prefix: string) =
    inherit Attribute()
    member val Prefix = prefix with get, set
    member val Separator = "" with get, set


type IConfigReader =
    abstract member GetValue: string -> string option


module internal Core =

    open TypeShape.Core

    let notSupported name =
        sprintf """The target type of "%s" is currently not supported""" name
        |> NotSupported

    let defaultSplitCharacter = SplitCharacter ','

    type TryParse<'a> = string -> 'a option

    let getPrefixAndSeparator<'T> defaultPrefix defaultSeparator =
        let conventionAttribute =
            typeof<'T>.GetCustomAttributes (typeof<ConventionAttribute>, true)
            |> Seq.tryHead
            |> Option.map (fun a -> a :?> ConventionAttribute)

        match conventionAttribute with
        | Some attr ->
            let prefix =
                if (isNull attr.Prefix) then
                    defaultPrefix
                else
                    Prefix attr.Prefix

            let separator =
                if (String.IsNullOrEmpty(attr.Separator)) then
                    defaultSeparator
                else
                    Separator attr.Separator

            (prefix, separator)
        | None -> (defaultPrefix, defaultSeparator)

    let findActualPrefix (Prefix customPrefix) (Separator separator) (Prefix prefix) =
        match (String.IsNullOrEmpty customPrefix, String.IsNullOrEmpty prefix) with
        | true, true -> ""
        | true, false
        | false, false -> sprintf "%s%s" prefix separator
        | false, true -> sprintf "%s%s" customPrefix separator

    let private fieldNameRegex: Regex =
        Regex("([^A-Z]+|[A-Z][^A-Z]+|[A-Z]+)", RegexOptions.Compiled)

    let fieldNameSubstrings fieldName =
        fieldNameRegex.Matches fieldName
        |> Seq.cast
        |> Seq.map (fun (m: Match) -> m.Value)
        |> Seq.toArray

    let tryParseWith name value tryParseFunc =
        match tryParseFunc value with
        | Some v -> Ok v
        | _ ->
            BadValue(name, value)
            |> Error


    let tryParse tryParseFunc value =
        match tryParseFunc value with
        | true, v -> Some v
        | _ -> None

    let tryParseFSharpDU (shape: ShapeFSharpUnion<'T>) value =
        shape.UnionCases
        |> Seq.tryFind (fun c -> c.CaseInfo.Name = value)
        |> Option.orElseWith (fun () ->
            shape.UnionCases
            |> Seq.tryFind (fun c ->
                String.Equals(c.CaseInfo.Name, value, StringComparison.InvariantCultureIgnoreCase)
            )
        )
        |> Option.map (fun c -> c.CreateUninitialized())


    let tryParseEnum<'T> (enumShape: IShapeEnum) value =
        let wrap (p: Option<'a>) = unbox<Option<'T>> p

        enumShape.Accept
            { new IEnumVisitor<'T option> with
                member __.Visit<'Enum, 'U
                    when 'Enum: enum<'U>
                    and 'Enum: struct
                    and 'Enum :> ValueType
                    and 'Enum: (new: unit -> 'Enum)>
                    ()
                    =

                    // cannot use tryParse as the function has a different signature with the ignoreCase option added
                    let success, result =
                        System.Enum.TryParse<'Enum>(value = value, ignoreCase = false)

                    if success then
                        Some result
                    else

                        let caseInsensitiveSuccess, caseInsensitiveResult =
                            System.Enum.TryParse<'Enum>(value = value, ignoreCase = true)

                        if caseInsensitiveSuccess then
                            Some caseInsensitiveResult
                        else
                            None

                    |> wrap
            }

    let getTryParseFunc<'T> targetTypeShape =
        let wrap (p: 'a) = Some(unbox<TryParse<'T>> p)

        match targetTypeShape with
        | Shape.Byte -> wrap (tryParse Byte.TryParse)
        | Shape.SByte -> wrap (tryParse SByte.TryParse)
        | Shape.Int16 -> wrap (tryParse Int16.TryParse)
        | Shape.Int32 -> wrap (tryParse Int32.TryParse)
        | Shape.Int64 -> wrap (tryParse Int64.TryParse)
        | Shape.UInt16 -> wrap (tryParse UInt16.TryParse)
        | Shape.UInt32 -> wrap (tryParse UInt32.TryParse)
        | Shape.UInt64 -> wrap (tryParse UInt64.TryParse)
        | Shape.Single -> wrap (tryParse Single.TryParse)
        | Shape.Double -> wrap (tryParse Double.TryParse)
        | Shape.Decimal -> wrap (tryParse Decimal.TryParse)
        | Shape.Char -> wrap (tryParse Char.TryParse)
        | Shape.String -> wrap (tryParse (fun (s: string) -> (true, s)))
        | Shape.Bool -> wrap (tryParse Boolean.TryParse)
        | Shape.DateTimeOffset -> wrap (tryParse DateTimeOffset.TryParse)
        | Shape.DateTime -> wrap (tryParse DateTime.TryParse)
        | Shape.TimeSpan -> wrap (tryParse TimeSpan.TryParse)
        | Shape.Char -> wrap (tryParse Char.TryParse)
        | Shape.Guid -> wrap (tryParse Guid.TryParse)
        | Shape.Enum enumShape -> wrap (tryParseEnum<'T> enumShape)
        | Shape.FSharpUnion (:? ShapeFSharpUnion<'T> as shape) -> wrap (tryParseFSharpDU shape)
        | Shape.Uri ->
            wrap (fun s ->
                try
                    Some(Uri s)
                with _ ->
                    None
            )
        | _ -> None

    let parseFSharpOption<'T> name value (fsharpOption: IShapeFSharpOption) =
        let wrap (p: ConfigParseResult<'a>) = unbox<ConfigParseResult<'T>> p

        fsharpOption.Element.Accept
            { new ITypeVisitor<ConfigParseResult<'T>> with
                member __.Visit<'t>() =
                    match value with
                    | None ->
                        let result: ConfigParseResult<'t option> = None |> Ok
                        wrap result
                    | Some v ->
                        match getTryParseFunc<'t> fsharpOption.Element with
                        | Some tryParseFunc ->
                            match shapeof<'t> with
                            | Shape.String ->
                                if String.IsNullOrWhiteSpace v then
                                    let result: ConfigParseResult<'t option> = None |> Ok
                                    wrap result
                                else
                                    tryParseWith name v tryParseFunc
                                    |> Result.bind (
                                        Some
                                        >> Ok
                                        >> wrap
                                    )
                            | _ ->
                                tryParseWith name v tryParseFunc
                                |> Result.bind (
                                    Some
                                    >> Ok
                                    >> wrap
                                )
                        | None ->
                            notSupported name
                            |> Error
            }

    let parseListReducer name tryParseFunc acc element =
        acc
        |> Result.bind (fun xs ->
            tryParseWith name element tryParseFunc
            |> Result.map (fun v -> v :: xs)
        )

    let parseFSharpList<'T>
        name
        value
        (fsharpList: IShapeFSharpList)
        (SplitCharacter splitCharacter)
        =
        let wrap (p: ConfigParseResult<'a>) = unbox<ConfigParseResult<'T>> p

        fsharpList.Element.Accept
            { new ITypeVisitor<ConfigParseResult<'T>> with
                member __.Visit<'t>() =
                    match value with
                    | None ->
                        let result: ConfigParseResult<'t list> = [] |> Ok
                        wrap result
                    | Some (v: string) ->
                        match getTryParseFunc<'t> fsharpList.Element with
                        | Some tryParseFunc ->
                            v.Split(splitCharacter)
                            |> Array.map (fun s -> s.Trim())
                            |> Array.filter (
                                String.IsNullOrWhiteSpace
                                >> not
                            )
                            |> Array.fold (parseListReducer name tryParseFunc) (Ok [])
                            |> Result.bind (
                                List.rev
                                >> Ok
                                >> wrap
                            )
                        | None ->
                            notSupported name
                            |> Error
            }

    type FieldValueParseArgs = {
        Name: string
        ListSplitChar: SplitCharacter
        DefaultValue: string option
    }

    let rec parseInternal<'T>
        (configReader: IConfigReader)
        (fieldNameCanonicalizer: FieldNameCanonicalizer)
        args
        =
        let value =
            match configReader.GetValue args.Name with
            | Some v -> Some v
            | None -> args.DefaultValue

        let targetTypeShape = shapeof<'T>

        match targetTypeShape with
        | Shape.FSharpRecord (:? ShapeFSharpRecord<'T> as shape) ->
            parseFSharpRecord configReader fieldNameCanonicalizer (Prefix args.Name) shape
        | Shape.FSharpOption fsharpOption -> parseFSharpOption<'T> args.Name value fsharpOption
        | Shape.FSharpList fsharpList ->
            parseFSharpList<'T> args.Name value fsharpList args.ListSplitChar
        | _ ->
            match getTryParseFunc<'T> targetTypeShape with
            | Some tryParseFunc ->
                match value with
                | Some v -> tryParseWith args.Name v tryParseFunc
                | None ->
                    NotFound args.Name
                    |> Error
            | None ->
                notSupported args.Name
                |> Error

    and parseFSharpRecord
        (configReader: IConfigReader)
        (fieldNameCanonicalizer: FieldNameCanonicalizer)
        prefix
        shape
        =
        let record = shape.CreateUninitialized()

        shape.Fields
        |> Seq.fold
            (fun acc field ->
                match acc with
                | Error x -> Error x
                | Ok xs ->

                    let customNameAttribute =
                        field.MemberInfo.GetCustomAttributes(typeof<CustomNameAttribute>, true)
                        |> Seq.tryHead
                        |> Option.map (fun a -> a :?> CustomNameAttribute)

                    let configName =
                        match customNameAttribute with
                        | Some attr -> attr.Name
                        | None -> fieldNameCanonicalizer prefix field.Label

                    let splitCharacter =
                        field.MemberInfo.GetCustomAttributes(typeof<ListSeparatorAttribute>, true)
                        |> Seq.tryHead
                        |> Option.map (fun sc -> sc :?> ListSeparatorAttribute)
                        |> function
                            | None -> defaultSplitCharacter
                            | Some c -> c.SplitCharacter

                    let defaultValueAttribute =
                        field.MemberInfo.GetCustomAttributes(typeof<DefaultValueAttribute>, true)
                        |> Seq.tryHead
                        |> Option.map (fun a -> a :?> DefaultValueAttribute)
                        |> Option.map (fun attr -> attr.Value)

                    let args = {
                        Name = configName
                        ListSplitChar = splitCharacter
                        DefaultValue = defaultValueAttribute
                    }

                    field.Accept
                        { new IMemberVisitor<'T, ConfigParseResult<('T -> 'T) list>> with
                            member __.Visit(shape: ShapeMember<'T, 'FieldType>) =
                                match
                                    parseInternal<'FieldType>
                                        configReader
                                        fieldNameCanonicalizer
                                        args
                                with
                                | Ok fieldValue ->
                                    (fun record -> shape.Set record fieldValue)
                                    :: xs
                                    |> Ok
                                | Error e -> Error e
                        }
            )
            (Ok [])
        |> Result.map (List.fold (fun acc f -> f acc) record)

[<AutoOpen>]
module Config =
    open Core

    let parse<'T>
        (configReader: IConfigReader)
        (fieldNameCanonicalizer: FieldNameCanonicalizer)
        name
        =
        let args = {
            Name = name
            ListSplitChar = defaultSplitCharacter
            DefaultValue = None
        }

        parseInternal<'T>
            (configReader: IConfigReader)
            (fieldNameCanonicalizer: FieldNameCanonicalizer)
            args
