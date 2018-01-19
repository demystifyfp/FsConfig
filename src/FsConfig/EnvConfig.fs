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

  static member private fieldNameCanonicalizer (Prefix customPrefix) (Separator separator) : FieldNameCanonicalizer = 
    fun (Prefix prefix) name -> 
      let actualPrefix =
        match (String.IsNullOrEmpty customPrefix, String.IsNullOrEmpty prefix) with
        | true, true -> ""
        | true, false -> sprintf "%s%s" prefix separator 
        | false, true -> sprintf "%s%s" customPrefix separator
        | false, false -> sprintf "%s%s%s%s" customPrefix separator prefix separator
      let subStrings =
        EnvConfig.envVarNameRegEx.Matches name
        |> Seq.cast
        |> Seq.map (fun (m : Match) -> m.Value.ToUpperInvariant())
        |> Seq.toArray
      String.Join(separator, subStrings)
      |> sprintf "%s%s" actualPrefix
  static member private defaultPrefix = Prefix ""
  static member private defaultSeparator = Separator "_"


  static member private defaultFieldNameCanonicalizer =
    EnvConfig.fieldNameCanonicalizer EnvConfig.defaultPrefix EnvConfig.defaultSeparator

  static member Get<'T> (envVarName : string) = 
    parse<'T> EnvConfig.configReader EnvConfig.defaultFieldNameCanonicalizer envVarName
  static member Get<'T when 'T : not struct> () =
    let conventionAttribute =
      typeof<'T>.GetCustomAttributes(typeof<ConventionAttribute>, true)
      |> Seq.tryHead
      |> Option.map (fun a -> a :?> ConventionAttribute)

    let fieldNameCanonicalizer = 
      match conventionAttribute with
      | Some attr -> 
          let prefix = 
            if (isNull attr.Prefix) then "" else attr.Prefix
            |> Prefix
          let separator = 
            if (String.IsNullOrEmpty(attr.Separator)) then 
              EnvConfig.defaultSeparator 
            else Separator attr.Separator
          EnvConfig.fieldNameCanonicalizer prefix separator
      | None -> EnvConfig.defaultFieldNameCanonicalizer

    parse<'T> EnvConfig.configReader fieldNameCanonicalizer ""

  static member Get<'T when 'T : not struct> (fieldNameCanonicalizer : FieldNameCanonicalizer) =
    parse<'T> EnvConfig.configReader fieldNameCanonicalizer ""
  