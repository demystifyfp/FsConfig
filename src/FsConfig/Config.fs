namespace FsConfig
open System

type ConfigParseError =
| BadValue of (string * string)
| NotFound of string
| NotSupported of string

type ConfigParseResult<'T> = Result<'T, ConfigParseError>

type IConfigNameCanonicalizer = 
  abstract member Canonicalize: string -> string
  abstract member CanonicalizeWithPrefix: string -> string -> string

module internal Core =

  open TypeShape

  type IConfigReader =
    abstract member GetValue : string -> string option

  [<NoEquality;NoComparison>]
  type ParseRecordConfig = {
    ConfigReader : IConfigReader
    ConfigNameCanonicalizer : IConfigNameCanonicalizer
  }

  type TryParse<'a> = string -> bool * 'a

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
  let rec parse<'T> (configReader : IConfigReader) (configNameCanonicalizer : IConfigNameCanonicalizer) name =
    let value = configReader.GetValue name
    let targetTypeShape = shapeof<'T>
    match getTryParseFunc<'T> targetTypeShape with
    | Some tryParseFunc -> 
      match value with
      | Some v -> tryParseWith name v tryParseFunc
      | None -> NotFound name |> Error
    | None -> 
      match targetTypeShape with
      | Shape.FSharpOption fsharpOption -> 
        parseFSharpOption<'T> name value fsharpOption
      | Shape.FSharpList fsharpList ->
        parseFSharpList<'T> name value fsharpList
      | Shape.FSharpRecord (:? ShapeFSharpRecord<'T> as shape) ->
        parseFSharpRecord configReader configNameCanonicalizer name shape
      | _ -> NotSupported "unknown target type" |> Error
  and parseFSharpRecord (configReader : IConfigReader) (configNameCanonicalizer : IConfigNameCanonicalizer) name shape =
    let record = shape.CreateUninitialized()
    shape.Fields
    |> Seq.fold 
      (fun acc field ->
        match acc with
        | Error x -> Error x 
        | Ok xs ->
          let configName = configNameCanonicalizer.CanonicalizeWithPrefix name field.Label
          field.Accept {
            new IWriteMemberVisitor<'RecordType, ConfigParseResult<('RecordType -> 'RecordType) list>> with
              member __.Visit (shape : ShapeWriteMember<'RecordType, 'FieldType>) =
                match parse<'FieldType> configReader configNameCanonicalizer configName with
                | Ok fieldValue -> (fun record -> shape.Inject record fieldValue) :: xs |> Ok
                | Error e -> Error e
          }
       ) (Ok []) 
    |> Result.map (List.fold (fun acc f -> f acc) record)