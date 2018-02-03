namespace FsConfig
open System
open System.Text.RegularExpressions

type ConfigParseError =
| BadValue of (string * string)
| NotFound of string
| NotSupported of string

type Prefix = Prefix of string
type Separator = Separator of string

type ConfigParseResult<'T> = Result<'T, ConfigParseError>

type FieldNameCanonicalizer = Prefix -> string -> string

[<AttributeUsage(AttributeTargets.Property, AllowMultiple = false)>]
type CustomNameAttribute(name : string) =
  inherit Attribute ()
  member __.Name = name

[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type ConventionAttribute(prefix : string) =
  inherit Attribute ()
  member val Prefix = prefix with get,set
  member val Separator = "" with get, set


module internal Core =

  open TypeShape.Core

  type IConfigReader =
    abstract member GetValue : string -> string option

  type TryParse<'a> = string -> bool * 'a

  let getPrefixAndSeparator<'T> defaultPrefix defaultSeparator =
    let conventionAttribute =
      typeof<'T>.GetCustomAttributes(typeof<ConventionAttribute>, true)
      |> Seq.tryHead
      |> Option.map (fun a -> a :?> ConventionAttribute)
    match conventionAttribute with
    | Some attr -> 
        let prefix = 
          if (isNull attr.Prefix) then defaultPrefix else Prefix attr.Prefix
        let separator = 
          if (String.IsNullOrEmpty(attr.Separator)) then 
            defaultSeparator 
          else Separator attr.Separator
        (prefix,separator)
    | None -> (defaultPrefix,defaultSeparator)

  let actualPrefix (Prefix customPrefix) (Separator separator) (Prefix prefix) =
    match (String.IsNullOrEmpty customPrefix, String.IsNullOrEmpty prefix) with
    | true, true -> ""
    | true, false -> sprintf "%s%s" prefix separator 
    | false, true -> sprintf "%s%s" customPrefix separator
    | false, false -> sprintf "%s%s%s%s" customPrefix separator prefix separator

  let private fieldNameRegex : Regex =
    Regex("([^A-Z]+|[A-Z][^A-Z]+|[A-Z]+)", RegexOptions.Compiled)

  let fieldNameSubstrings fieldName =
    fieldNameRegex.Matches fieldName
    |> Seq.cast
    |> Seq.map (fun (m : Match) -> m.Value)
    |> Seq.toArray

  let tryParseWith name value tryParseFunc  = 
    match tryParseFunc value with
    | true, v -> Ok v
    | _ -> BadValue (name, value) |> Error
      

  let getTryParseFunc<'T> targetTypeShape =
    let wrap(p : 'a) = Some (unbox<TryParse<'T>> p) 
    match targetTypeShape with
    | Shape.Byte -> wrap Byte.TryParse 
    | Shape.SByte -> wrap SByte.TryParse
    | Shape.Int16 -> wrap Int16.TryParse
    | Shape.Int32 -> wrap Int32.TryParse
    | Shape.Int64 -> wrap Int64.TryParse
    | Shape.UInt16 -> wrap UInt16.TryParse 
    | Shape.UInt32 -> wrap UInt32.TryParse 
    | Shape.UInt64 -> wrap UInt64.TryParse 
    | Shape.Single -> wrap Single.TryParse 
    | Shape.Double -> wrap Double.TryParse 
    | Shape.Decimal -> wrap Decimal.TryParse 
    | Shape.Char -> wrap Char.TryParse 
    | Shape.String -> wrap (fun (s : string) -> (true,s))
    | Shape.Bool -> wrap Boolean.TryParse
    | Shape.DateTimeOffset -> wrap DateTimeOffset.TryParse 
    | Shape.DateTime -> wrap DateTime.TryParse 
    | Shape.TimeSpan -> wrap TimeSpan.TryParse 
    | Shape.Char -> wrap Char.TryParse 
    | Shape.String -> wrap (fun (s : string) -> (true,s)) 
    | Shape.Guid -> wrap Guid.TryParse
    | _ -> None

  let parseFSharpOption<'T> name value (fsharpOption : IShapeFSharpOption) =
    let wrap (p : ConfigParseResult<'a>) =
      unbox<ConfigParseResult<'T>> p
    fsharpOption.Accept {
      new IFSharpOptionVisitor<ConfigParseResult<'T>> with
        member __.Visit<'t>() =
          match value with
          | None -> 
            let result : ConfigParseResult<'t option> = None |> Ok 
            wrap result
          | Some v ->
            match getTryParseFunc<'t> fsharpOption.Element with
            | Some tryParseFunc -> 
              tryParseWith name v tryParseFunc 
              |> Result.bind (Some >> Ok >> wrap) 
            | None -> NotSupported "unknown target type" |> Error 
    }

  let parseListReducer name tryParseFunc acc element = 
    acc
    |> Result.bind 
        (fun xs ->
          tryParseWith name element tryParseFunc
          |> Result.map (fun v -> v :: xs)
        )

  let parseFSharpList<'T> name value (fsharpList: IShapeFSharpList) =
    let wrap (p : ConfigParseResult<'a>) =
      unbox<ConfigParseResult<'T>> p
    fsharpList.Accept {
      new IFSharpListVisitor<ConfigParseResult<'T>> with
        member __.Visit<'t>() =
          match value with
          | None -> 
            let result : ConfigParseResult<'t list> = [] |> Ok 
            wrap result
          | Some (v : string) -> 
            match getTryParseFunc<'t> fsharpList.Element with
            | Some tryParseFunc -> 
              v.Split(',') 
              |> Array.map (fun s -> s.Trim())
              |> Array.filter (String.IsNullOrWhiteSpace >> not)
              |> Array.fold (parseListReducer name tryParseFunc) (Ok [])
              |> Result.bind (List.rev >> Ok >> wrap)
            | None -> NotSupported "unknown target type" |> Error 
    }

  let parseEnum<'T> name value (enumShape : IShapeEnum) : ConfigParseResult<'T> = 
    let wrap (p : ConfigParseResult<'a>) =
      unbox<ConfigParseResult<'T>> p
    enumShape.Accept {
      new IEnumVisitor<ConfigParseResult<'T>> with
        member __.Visit<'T, 'U when 'T : enum<'U>
                                    and 'T : struct
                                    and 'T :> ValueType
                                    and 'T : (new : unit -> 'T)> () = 
          tryParseWith name value (System.Enum.TryParse<'T>) |> wrap 
    }

  let rec parse<'T> (configReader : IConfigReader) (fieldNameCanonicalizer : FieldNameCanonicalizer) name =
    let value = configReader.GetValue name
    let targetTypeShape = shapeof<'T>
    match targetTypeShape with
    | Shape.FSharpRecord (:? ShapeFSharpRecord<'T> as shape) ->
      parseFSharpRecord configReader fieldNameCanonicalizer (Prefix name) shape
    | Shape.FSharpOption fsharpOption -> 
      parseFSharpOption<'T> name value fsharpOption
    | Shape.FSharpList fsharpList ->
      parseFSharpList<'T> name value fsharpList
    | Shape.Enum enumShape ->
      match value with
      | None -> NotFound name |> Error
      | Some v -> parseEnum<'T> name v enumShape
    | _ ->
      match getTryParseFunc<'T> targetTypeShape with
      | Some tryParseFunc -> 
        match value with
        | Some v -> tryParseWith name v tryParseFunc
        | None -> NotFound name |> Error
      | None -> NotSupported "unknown target type" |> Error
  and parseFSharpRecord (configReader : IConfigReader) (fieldNameCanonicalizer : FieldNameCanonicalizer) prefix shape =
    let record = shape.CreateUninitialized()
    shape.Fields
    |> Seq.fold 
      (fun acc field ->
        match acc with
        | Error x -> Error x 
        | Ok xs ->

          let customHeadAttribute =
            field.MemberInfo.GetCustomAttributes(typeof<CustomNameAttribute>, true)
            |> Seq.tryHead
            |> Option.map (fun a -> a :?> CustomNameAttribute)

          let configName = 
            match customHeadAttribute with
            | Some attr -> attr.Name
            | None -> fieldNameCanonicalizer prefix field.Label
          
          field.Accept {
            new IWriteMemberVisitor<'RecordType, ConfigParseResult<('RecordType -> 'RecordType) list>> with
              member __.Visit (shape : ShapeWriteMember<'RecordType, 'FieldType>) =
                match parse<'FieldType> configReader fieldNameCanonicalizer configName with
                | Ok fieldValue -> (fun record -> shape.Inject record fieldValue) :: xs |> Ok
                | Error e -> Error e
          }
       ) (Ok []) 
    |> Result.map (List.fold (fun acc f -> f acc) record)