namespace FsConfig

open FsConfig.Core
open System
open System.Text.RegularExpressions

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
  static member private envVarNameRegEx : Regex =
    Regex("([^A-Z]+|[A-Z][^A-Z]+|[A-Z]+)", RegexOptions.Compiled)

  static member private configNameCanonicalizer envConfigParams : IConfigNameCanonicalizer = {
    new IConfigNameCanonicalizer with
      member __.Canonicalize name =
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

  static member Get<'T> (envVarName : string) = 
    let canonicalizer = 
      EnvConfig.configNameCanonicalizer EnvConfig.defaultParams
    parse<'T> EnvConfig.configReader canonicalizer envVarName

  static member Get<'T when 'T : not struct> (envConfigParams : EnvConfigParams) =
    let canonicalizer = 
      EnvConfig.configNameCanonicalizer envConfigParams
    parse<'T> EnvConfig.configReader canonicalizer ""

  static member Get<'T when 'T : not struct> () =
    EnvConfig.Get<'T> EnvConfig.defaultParams

  static member Get<'T when 'T : not struct> (configNameCanonicalizer : IConfigNameCanonicalizer) =
    parse<'T> EnvConfig.configReader configNameCanonicalizer ""
  