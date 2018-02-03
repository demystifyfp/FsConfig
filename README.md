# FsConfig

FsConfig is a F# library for reading configuration data from environment variables and AppSettings with type safety. 

![Nuget](https://img.shields.io/nuget/dt/FsConfig.svg)(https://www.nuget.org/packages/FsConfig)

## Why FsConfig?

```fsharp
let main argv =
  let fsTweetConnString = 
   Environment.GetEnvironmentVariable  "FSTWEET_DB_CONN_STRING"

  let serverToken =
    Environment.GetEnvironmentVariable "FSTWEET_POSTMARK_SERVER_TOKEN"

  let senderEmailAddress =
    Environment.GetEnvironmentVariable "FSTWEET_SENDER_EMAIL_ADDRESS"

  let env = 
    Environment.GetEnvironmentVariable "FSTWEET_ENVIRONMENT"

  let streamConfig : GetStream.Config = {
      ApiKey = 
        Environment.GetEnvironmentVariable "FSTWEET_STREAM_KEY"
      ApiSecret = 
        Environment.GetEnvironmentVariable "FSTWEET_STREAM_SECRET"
      AppId = 
        Environment.GetEnvironmentVariable "FSTWEET_STREAM_APP_ID"
  }

  let serverKey = 
    Environment.GetEnvironmentVariable "FSTWEET_SERVER_KEY"

  let port = 
    Environment.GetEnvironmentVariable "PORT" |> uint16

  // ...
```

```fsharp
open FsConfig

type StreamConfig = {
  Key : string
  Secret : string
  AppId : string
}

[<Convention("FSTWEET")>]
type Config = {
  DbConnString : string
  PostmarkServerToken : string
  SenderEmailAddress : string
  ServerKey : string
  [<CustomName("PORT")>]
  Port : uint16
  Environment : string
  Stream : StreamConfig
}

let main argv =

  let config = 
    match EnvConfig.Get<Config>() with
    | Ok config -> config
    | Error error -> 
      match error with
      | NotFound envVarName -> 
        failwithf "Environment variable %s not found" envVarName
      | BadValue (envVarName, value) ->
        failwithf "Unable to parse the value %s of the Environment variable %s" value envVarName
      | NotSupported msg -> 
        failwithf "Unable to read : %s" msg

  // ...
```



## Build Status

Mono | .NET
---- | ----
[![Mono CI Build Status](https://img.shields.io/travis/demystifyfp/FsConfig/master.svg)](https://travis-ci.org/demystifyfp/FsConfig) | [![.NET Build Status](https://img.shields.io/appveyor/ci/demystifyfp/fsconfig/master.svg)](https://ci.appveyor.com/project/demystifyfp/fsconfig)

## Maintainer(s)

- [@tamizhvendan](https://github.com/tamizhvendan)
