namespace FsConfig

open FsConfig.Core
open Microsoft.Extensions.Configuration
open System

type AppConfig(configRoot: IConfigurationRoot) =

    let defaultFieldNameCanonicalizer: FieldNameCanonicalizer =
        fun prefix name ->
            let actualPrefix = findActualPrefix (Prefix "") (Separator "") prefix

            match actualPrefix with
            | "" -> name
            | x -> sprintf "%s:%s" x name

    let configReader: IConfigReader =
        { new IConfigReader with
            member __.GetValue name =
                let v = configRoot.[name]
                if v = null then None else Some v
        }

    member this.Get<'T when 'T :> IConvertible>(configName: string) =
        parse<'T> configReader defaultFieldNameCanonicalizer configName


    member this.Get<'T when 'T: not struct>() =
        parse<'T> configReader defaultFieldNameCanonicalizer ""

    member this.Get<'T when 'T: not struct>(fieldNameCanonicalizer: FieldNameCanonicalizer) =
        parse<'T> configReader fieldNameCanonicalizer ""
