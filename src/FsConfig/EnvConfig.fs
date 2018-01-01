namespace FsConfig

open FsConfig.Core
open System
open System.Text.RegularExpressions

type EnvConfigParams = {
  Prefix : string
  Separator : string
}

type EnvConfig =
  static member private configValueGetter : ConfigValueGetter = {
    new ConfigValueGetter with
      member __.GetConfigValue name =
        let v = Environment.GetEnvironmentVariable name
        if v = null then None else Some v
  }
  static member private envVarNameRegEx : Regex =
    Regex("([^A-Z]+|[A-Z][^A-Z]+|[A-Z]+)", RegexOptions.Compiled)

  static member private configNameCanonicalizer envConfigParams : ConfigNameCanonicalizer = {
    new ConfigNameCanonicalizer with
      member __.CanonicalizeConfigName name =
        let actualPrefix =
          if String.IsNullOrEmpty envConfigParams.Prefix then "" 
          else sprintf "%s%s" envConfigParams.Prefix envConfigParams.Separator 
        let subStrings =
          EnvConfig.envVarNameRegEx.Matches name
          |> Seq.cast
          |> Seq.map (fun (m : Match) -> m.Value.ToUpperInvariant())
          |> Seq.toArray
        String.Join(envConfigParams.Separator, subStrings)
        |> sprintf "%s%s" actualPrefix
        
  }
  static member defaultParams : EnvConfigParams = {
    Prefix = ""
    Separator = "_"
  }

  static member Parse<'T when 'T : struct> (envVarName : string) = 
    parsePrimitive<'T> EnvConfig.configValueGetter envVarName

  static member Parse<'T when 'T : not struct> (envConfigParams : EnvConfigParams) =
    EnvConfig.configNameCanonicalizer envConfigParams
    |> parseRecord<'T> EnvConfig.configValueGetter 

  static member Parse<'T when 'T : not struct> () =
    EnvConfig.Parse<'T> EnvConfig.defaultParams

  static member Parse<'T when 'T : not struct> (configNameCanonicalizer : ConfigNameCanonicalizer) =
    parseRecord<'T> EnvConfig.configValueGetter configNameCanonicalizer
  