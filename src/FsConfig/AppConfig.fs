namespace FsConfig

open FsConfig.Core
open System.Configuration

type AppConfig =
  static member private configReader : IConfigReader = {
    new IConfigReader with
      member __.GetValue name =
        let v = ConfigurationManager.AppSettings.Get name
        if v = null then None else Some v
  }

  static member private fieldNameCanonicalizer = sprintf "%s%s"


  static member Get<'T> (appSettingsName : string) = 
    parse<'T> AppConfig.configReader AppConfig.fieldNameCanonicalizer appSettingsName

  static member Get<'T when 'T : not struct> () =
    AppConfig.Get<'T> ""

  static member Get<'T when 'T : not struct> (fieldNameCanonicalizer : FieldNameCanonicalizer) =
    parse<'T> AppConfig.configReader fieldNameCanonicalizer "" 