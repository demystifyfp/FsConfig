namespace FsConfig

open FsConfig.Core
open System.Configuration
open System

type AppConfig =
  static member private configReader : IConfigReader = {
    new IConfigReader with
      member __.GetValue name =
        let v = ConfigurationManager.AppSettings.Get name
        if v = null then None else Some v
  }

  static member private defaultPrefix = Prefix ""
  static member private defaultSeparator = Separator ""

  static member private fieldNameCanonicalizer customPrefix (Separator separator) : FieldNameCanonicalizer = 
    fun prefix name -> 
      let actualPrefix =
        actualPrefix customPrefix (Separator separator) prefix
      String.Join(separator, (fieldNameSubstrings name))
      |> sprintf "%s%s" actualPrefix

  static member private defaultFieldNameCanonicalizer =
    AppConfig.fieldNameCanonicalizer AppConfig.defaultPrefix AppConfig.defaultSeparator


  static member Get<'T> (appSettingsName : string) = 
    parse<'T> AppConfig.configReader AppConfig.defaultFieldNameCanonicalizer appSettingsName

  static member Get<'T when 'T : not struct> () =
    let fieldNameCanonicalizer = 
      let (prefix, separator) = 
        getPrefixAndSeparator<'T> AppConfig.defaultPrefix AppConfig.defaultSeparator
      AppConfig.fieldNameCanonicalizer prefix separator
    parse<'T> AppConfig.configReader fieldNameCanonicalizer ""

  static member Get<'T when 'T : not struct> (fieldNameCanonicalizer : FieldNameCanonicalizer) =
    parse<'T> AppConfig.configReader fieldNameCanonicalizer "" 