module EnvConfig 

open TypeShape
open System
open System.Text.RegularExpressions

type EnvVarParseError =
| BadValue of (string * string)
| NotFound of string
| NotSupported of string

type EnvVarParseResult<'T> = Result<'T, EnvVarParseError>

// string -> string option
let getEnv name =
  let v = Environment.GetEnvironmentVariable name
  if v = null then None else Some v

// (string -> bool * 'a) -> name ->  EnvVarParseResult<'a>
let tryParseWith tryParseFunc name = 
  match getEnv name with
  | None -> NotFound name |> Error
  | Some value ->
    match tryParseFunc value with
    | true, v -> Ok v
    | _ -> BadValue (name, value) |> Error


let parseInt = tryParseWith Int32.TryParse
let parseBool = tryParseWith Boolean.TryParse
let parseString = tryParseWith (fun s -> (true,s))


let parsePrimitive<'T> (envVarName : string) =
  let wrap(p : string -> 'a) = 
    envVarName
    |> unbox<string -> EnvVarParseResult<'T>> p 
    
  match shapeof<'T> with
  | Shape.Int32 -> wrap parseInt
  | Shape.String -> wrap parseString
  | Shape.Bool -> wrap parseBool
  | _ -> NotSupported "unknown target type" |> Error



let envVarNameRegEx = 
  Regex("([^A-Z]+|[A-Z][^A-Z]+|[A-Z]+)", RegexOptions.Compiled)

let canonicalizeEnvVarName name =
  let subStrings =
    envVarNameRegEx.Matches name
    |> Seq.cast
    |> Seq.map (fun (m : Match) -> m.Value.ToUpperInvariant())
    |> Seq.toArray
  String.Join("_", subStrings)


let private parseRecordField (shape : IShapeWriteMember<'RecordType>) = 
  let envVarName = canonicalizeEnvVarName shape.Label
  shape.Accept {
    new IWriteMemberVisitor<'RecordType, 'RecordType -> EnvVarParseResult<'RecordType>> with
      member __.Visit (shape : ShapeWriteMember<'RecordType, 'FieldType>) =
        match parsePrimitive<'FieldType> envVarName with
        | Ok fieldValue -> fun record -> shape.Inject record fieldValue |> Ok
        | Error e -> fun _ -> Error e
    }


    
let private foldParseRecordFieldResponse record parseRecordErrors field =
  match parseRecordField field record with
  | Ok _ -> parseRecordErrors
  | Error e -> e :: parseRecordErrors
  
  
let parseRecord<'T> () =
  match shapeof<'T> with
  | Shape.FSharpRecord (:? ShapeFSharpRecord<'T> as shape) ->
    let record = shape.CreateUninitialized()
    let parseRecordErrors =
      shape.Fields
      |> Seq.fold (foldParseRecordFieldResponse record) []
    match List.isEmpty parseRecordErrors with 
    | true -> Ok record 
    |_  -> Error parseRecordErrors
  | _ -> failwith "not supported"
    
type Config = {
  ConnectionString : string
  Port : int
  EnableDebug : bool
  Environment : string
}

let setEnvVar (name,value) = 
  Environment.SetEnvironmentVariable(name,value)

let setEnvVars = List.iter setEnvVar

(*
[ ("PORT", "5432")
  ("CONNECTION_STRING", "Database=foobar;Password=foobaz")
  ("ENABLE_DEBUG", "true")
  ("Environment", "staging") ] |> setEnvVars

[ ("PORT", "5432"); ("ENABLE_DEBUG", "true")] 
|> setEnvVars

*)