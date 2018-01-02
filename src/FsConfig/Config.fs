namespace FsConfig

type ConfigParseError =
| BadValue of (string * string)
| NotFound of string
| NotSupported of string

type ConfigParseResult<'T> = Result<'T, ConfigParseError>

type IConfigNameCanonicalizer = 
  abstract member Canonicalize: string -> string

module internal Core =

  open TypeShape
  open System

  type IConfigReader =
    abstract member GetValue : string -> string option

  [<NoEquality;NoComparison>]
  type ParseRecordConfig = {
    ConfigReader : IConfigReader
    ConfigNameCanonicalizer : IConfigNameCanonicalizer
  }

  let tryParseWith tryParseFunc (configReader : IConfigReader) name = 
    match configReader.GetValue name with
    | None -> NotFound name |> Error
    | Some value ->
      match tryParseFunc value with
      | true, v -> Ok v
      | _ -> BadValue (name, value) |> Error


  let parseInt = tryParseWith Int32.TryParse
  let parseBool = tryParseWith Boolean.TryParse
  let parseString = tryParseWith (fun s -> (true,s))

  let parsePrimitive<'T> (configReader : IConfigReader) (envVarName : string) =
    let wrap(p : IConfigReader -> string -> 'a) = 
      envVarName
      |> unbox<string -> ConfigParseResult<'T>> (p configReader)
      
    match shapeof<'T> with
    | Shape.Int32 -> wrap parseInt
    | Shape.String -> wrap parseString
    | Shape.Bool -> wrap parseBool
    | _ -> NotSupported "unknown target type" |> Error

  let private parseRecordField 
    (configReader : IConfigReader) (configNameCanonicalizer : IConfigNameCanonicalizer) (shape : IShapeWriteMember<'RecordType>) = 
    let configName = 
      configNameCanonicalizer.Canonicalize shape.Label
    shape.Accept {
      new IWriteMemberVisitor<'RecordType, 'RecordType -> ConfigParseResult<'RecordType>> with
        member __.Visit (shape : ShapeWriteMember<'RecordType, 'FieldType>) =
          match parsePrimitive<'FieldType> configReader configName with
            | Ok fieldValue -> fun record -> shape.Inject record fieldValue |> Ok
            | Error e -> fun _ -> Error e
      }

  let private foldParseRecordFieldResponse (configReader : IConfigReader) (configNameCanonicalizer : IConfigNameCanonicalizer) record parseRecordErrors field =
    match parseRecordField configReader configNameCanonicalizer field record with
    | Ok _ -> parseRecordErrors
    | Error e -> e :: parseRecordErrors
  
  
  let parseRecord<'T> (configReader : IConfigReader) (configNameCanonicalizer : IConfigNameCanonicalizer)  =
    match shapeof<'T> with
    | Shape.FSharpRecord (:? ShapeFSharpRecord<'T> as shape) ->
      let record = shape.CreateUninitialized()
      let parseRecordErrors =
        shape.Fields
        |> Seq.fold (foldParseRecordFieldResponse configReader configNameCanonicalizer record) []
      match List.isEmpty parseRecordErrors with 
      | true -> Ok record 
      |_  -> Error parseRecordErrors
    | _ -> failwith "not supported"