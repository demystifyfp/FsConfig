namespace FsConfig

open FsConfig.Core
open System

type EnvConfigParams = {
  Prefix : string
  Separator : string
}

type EnvConfig =
  static member private configReader : IConfigReader = {
    new IConfigReader with
      member __.GetValue name =
        let v = Environment.GetEnvironmentVariable name
        if v = null then None else Some v
  }
  

  static member private fieldNameCanonicalizer customPrefix (Separator separator) : FieldNameCanonicalizer = 
    fun prefix name -> 
      let actualPrefix =
        actualPrefix customPrefix (Separator separator) prefix
      let subStrings =
        fieldNameSubstrings name
        |> Array.map (fun v -> v.ToUpperInvariant())
      String.Join(separator, subStrings)
      |> sprintf "%s%s" actualPrefix
  static member private defaultPrefix = Prefix ""
  static member private defaultSeparator = Separator "_"


  static member private defaultFieldNameCanonicalizer =
    EnvConfig.fieldNameCanonicalizer EnvConfig.defaultPrefix EnvConfig.defaultSeparator

  static member Get<'T> (envVarName : string) = 
    parse<'T> EnvConfig.configReader EnvConfig.defaultFieldNameCanonicalizer envVarName
  static member Get<'T when 'T : not struct> () =
    let fieldNameCanonicalizer = 
      let (prefix, separator) = 
        getPrefixAndSeparator<'T> EnvConfig.defaultPrefix EnvConfig.defaultSeparator
      EnvConfig.fieldNameCanonicalizer prefix separator
    parse<'T> EnvConfig.configReader fieldNameCanonicalizer ""

  static member Get<'T when 'T : not struct> (fieldNameCanonicalizer : FieldNameCanonicalizer) =
    parse<'T> EnvConfig.configReader fieldNameCanonicalizer ""
  