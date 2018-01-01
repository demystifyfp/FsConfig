namespace FsConfig

type ConfigParseError =
| BadValue of (string * string)
| NotFound of string
| NotSupported of string

type ConfigParseResult<'T> = Result<'T, ConfigParseError>

type ConfigNameCanonicalizer = 
  abstract member CanonicalizeConfigName: string -> string

module internal Core =

  open TypeShape
  open System

  type ConfigValueGetter =
    abstract member GetConfigValue : string -> string option

  [<NoEquality;NoComparison>]
  type ParseRecordConfig = {
    ConfigValueGetter : ConfigValueGetter
    ConfigNameCanonicalizer : ConfigNameCanonicalizer
  }

  let tryParseWith tryParseFunc (configValueGetter : ConfigValueGetter) name = 
    match configValueGetter.GetConfigValue name with
    | None -> NotFound name |> Error
    | Some value ->
      match tryParseFunc value with
      | true, v -> Ok v
      | _ -> BadValue (name, value) |> Error


  let parseInt = tryParseWith Int32.TryParse
  let parseBool = tryParseWith Boolean.TryParse
  let parseString = tryParseWith (fun s -> (true,s))

  let parsePrimitive<'T> (configValueGetter : ConfigValueGetter) (envVarName : string) =
    let wrap(p : ConfigValueGetter -> string -> 'a) = 
      envVarName
      |> unbox<string -> ConfigParseResult<'T>> (p configValueGetter)
      
    match shapeof<'T> with
    | Shape.Int32 -> wrap parseInt
    | Shape.String -> wrap parseString
    | Shape.Bool -> wrap parseBool
    | _ -> NotSupported "unknown target type" |> Error

  let private parseRecordField 
    (configValueGetter : ConfigValueGetter) (configNameCanonicalizer : ConfigNameCanonicalizer) (shape : IShapeWriteMember<'RecordType>) = 
    let configName = 
      configNameCanonicalizer.CanonicalizeConfigName shape.Label
    shape.Accept {
      new IWriteMemberVisitor<'RecordType, 'RecordType -> ConfigParseResult<'RecordType>> with
        member __.Visit (shape : ShapeWriteMember<'RecordType, 'FieldType>) =
          match parsePrimitive<'FieldType> configValueGetter configName with
          | Ok fieldValue -> fun record -> shape.Inject record fieldValue |> Ok
          | Error e -> fun _ -> Error e
      }

  let private foldParseRecordFieldResponse (configValueGetter : ConfigValueGetter) (configNameCanonicalizer : ConfigNameCanonicalizer) record parseRecordErrors field =
    match parseRecordField configValueGetter configNameCanonicalizer field record with
    | Ok _ -> parseRecordErrors
    | Error e -> e :: parseRecordErrors
  
  
  let parseRecord<'T> (configValueGetter : ConfigValueGetter) (configNameCanonicalizer : ConfigNameCanonicalizer)  =
    match shapeof<'T> with
    | Shape.FSharpRecord (:? ShapeFSharpRecord<'T> as shape) ->
      let record = shape.CreateUninitialized()
      let parseRecordErrors =
        shape.Fields
        |> Seq.fold (foldParseRecordFieldResponse configValueGetter configNameCanonicalizer record) []
      match List.isEmpty parseRecordErrors with 
      | true -> Ok record 
      |_  -> Error parseRecordErrors
    | _ -> failwith "not supported"