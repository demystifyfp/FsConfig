# FsConfig 

FsConfig is a F# library for reading configuration data from environment variables and AppSettings with type safety

[![Nuget](https://img.shields.io/nuget/dt/FsConfig.svg)](https://www.nuget.org/packages/FsConfig)

[![Mono CI Build Status](https://img.shields.io/travis/demystifyfp/FsConfig/master.svg)](https://travis-ci.org/demystifyfp/FsConfig) 
[![.NET Build Status](https://img.shields.io/appveyor/ci/demystifyfp/fsconfig/master.svg)](https://ci.appveyor.com/project/demystifyfp/fsconfig)

## Why FsConfig?

To understand FsConfig, let's have a look at an use case from the [FsTweet](https://github.com/demystifyfp/FsTweet) application.

The FsTweet application follows [The Twelve-Factor App](https://12factor.net/config) guideline for managing the configuration data. During the application bootstrap, it retrieves its ten configuration parameters from their respective environment variables.

```fsharp
open System

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

Though the code snippet does the job, there are some shortcomings.

1. The code is verbose.
2. There is no error handling to deal with the absence of values or wrong values.
3. Explicit type casting

With the help of FsConfig, we can overcome these limitations by specifying the configuration data as a F# Record type.

```fsharp
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
  Environment : string

  [<CustomName("PORT")>]
  Port : uint16
  Stream : StreamConfig
}
```

And then read all the associated environment variables in a single function call with type safety and error handling!

```fsharp
let main argv =

  let config = 
    match EnvConfig.Get<Config>() with
    | Ok config -> config
    | Error error -> 
      match error with
      | NotFound envVarName -> 
        failwithf "Environment variable %s not found" envVarName
      | BadValue (envVarName, value) ->
        failwithf "Environment variable %s has invalid value" envVarName value
      | NotSupported msg -> 
        failwith msg
```

## Supported Data Types

FsConfig supports the following data types and leverages their respective `TryParse` function to do the type conversion.

* `Int16`, `Int32`, `Int64`, `UInt16`, `UInt32`, `UInt64`
* `Byte`, `SByte`
* `Single`, `Double`, `Decimal`
* `Char`, `String`
* `Bool`
* `TimeSpan`, `DateTimeOffset`, `DateTime`
* `Guid`
* `Enum`
* `list` of all the above types
* `option` of all the above types

### Option Type

FsConfig allows us to specify optional configuration parameters using the `option` type. In the previous example, if the configuration parameter `Port` is optional, we can define it like 

```diff
type Config = {
   ...
-  Port : uint16
+  Port : uint16 option
}
```

### Discriminated Union Type

FsConfig supports Discriminated Union Types that has cases alone. 

```diff
type Color =
| Red
| Green
| Blue 

type Config = {
  ConsoleColor : Color
}
```

> With this configuration declaration, FsConfig read the environment variable `CONSOLE_COLOR` and populates the `ConsoleColor` field of type `Color`.

> List of Discriminated Union Types also supported!

### List Type

FsConfig also supports `list` type, and it expects comma separated individual values. 

For example, to get mulitple ports, we can define the config as 

```fsharp
type Config = {
  Port : uint16 list
}
```

and then pass the value `8084,8085,8080` using the environment variable `PORT`.

The default separator for the list can be changed if needed using the `ListSeparator` attribute.

```fsharp
  [<Convention("MYENV")>]
  type CustomListSeparatorSampleConfig = {
    ProcessNames : string list
    [<ListSeparator(';')>]
    ProcessIds : uint16 list
    [<ListSeparator('|')>]
    PipedFlow : int list    
  }
  ```

> With this configuration declaration, FSConfig would be able to read the following entries from App.settings.
```xml
  <add key="MYENVProcessNames" value="conhost.exe,gitter.exe"/>
  <add key="MYENVProcessIds" value="4700;15680"/>
  <add key="MYENVPipedFlow" value="4700|15680|-1" />
```

A definition similar to the one shown below will allow parsing of standalone lists.
```fsharp
  type IntListUsingSemiColonsConfig = {
    [<ListSeparator(';')>]
    IntListUp : int list
  }
```

> E.g. an environment containing
```ini
INT_LIST_UP=42;43;44 
```

### Record Type

As shown in the initial example, FsConfig allows us to group similar configuration into a record type.

```fsharp
type AwsConfig = {
  AccessKeyId : string
  DefaultRegion : string
  SecretAccessKey : string
}

type Config = {
  Aws : AwsConfig
}
```

> With this configuration declaration, FsConfig read the environment variables `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, and `AWS_DEFAULT_REGION` and populates the `Aws` field of type `AwsConfig`.

## Environment Variable Name Convention & Customization

By default, FsConfig follows Underscores with uppercase convention, as in `UPPER_CASE`, for deriving the environment variable name. 

```fsharp
type Config = {
  ServerKey : string
}
```

> Using this configuration declaration, FsConfig read the environment variable `SERVER_KEY` and populates the `ServerKey` field


To specify a custom prefix in the environment variables, we can make use of the `Convention` attribute.

```fsharp
[<Convention("FSTWEET")>]
type Config = {
  ServerKey : string
}
```

> For this configuration declaration, FsConfig read the environment variable `FSTWEET_SERVER_KEY` and populates the `ServerKey` field.

We can also override the separator character `_` using the `Convention` attribute's optional field `Separator`

```fsharp
[<Convention("FSTWEET", Separator="-")>]
type Config = {
  ServerKey : string
}
```

> In this case, FsConfig derives the environment variable name as `FSTWEET-SERVER-KEY`.


If an environment variable name is not following a convention, we can override the environment variable name at the field level using the `CustomName` attribute.

```fsharp
type Config = {
  [<CustomName("MY_SERVER_KEY")>]
  ServerKey : string
}
```

> Here, FsConfig uses the environment variable name `MY_SERVER_KEY` to get the ServerKey.


We can also merely customise (or control) the environment variable name generation by passing an higher-order function while calling the `Get` function

```fsharp
open FsConfig

// Prefix -> string -> string
let lowerCaseConfigNameCanonicalizer (Prefix prefix) (name : string) = 
  let lowerCaseName = name.ToLowerInvariant()
  if String.IsNullOrEmpty prefix then 
    name.ToLowerInvariant()
  else
    sprintf "%s-%s" (prefix.ToLowerInvariant()) lowerCaseName


[<Convention("FSTWEET")>]
type Config = {
  ServerKey : string
}

let main argv =
  let config = 
    match EnvConfig.Get<Config> lowerCaseConfigNameCanonicalizer with
    | Ok config -> config
    | Error error -> failwithf "Error : %A" error
``` 

> FsConfig computes the environment variable name as `fstweet-server-key` in this scenario.

## Getting Individual Environment Variables

FsConfig also supports reading value directly by explicitly specifying the environment variable name

```fsharp
EnvConfig.Get<decimal> "MY_APP_INITIAL_BALANCE" // Result<decimal, ConfigParseError>
```

## App Config

FsConfig supports App Config for both DotNet Core and Non DotNet Core Applications. 

* [DotNet Core Applications](#dotnet-core-configuration) (Supported from V2.0.0 or above)

* [Non DotNet Core Applications](#appsettings-only-supported-in-v006-or-below) (Only Supported in V0.0.6 or below)

### DotNet Core Configuration (Supported from V2.0.0 or above)

FsConfig abstracts the configuration provider by depending on [IConfigurationRoot](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationroot).

```fsharp
let configurationRoot : IConfigurationRoot = // ...
let appConfig = new AppConfig(configurationRoot)
```

After creating an instance `appConfig` (of type `AppConfig` from FsConfig), you can use it to read the configuration values as below

```fsharp
// Reading Primitive
let result = 
  appConfig.Get<int> "processId" // Result<int, ConfigParseError>

// A Sample Record
type SampleConfig = {
  ProcessId : int
  ProcessName : string
}

// Reading a Record type
let result = 
  appConfig.Get<SampleConfig> () // Result<SampleConfig, ConfigParseError>

// A Sample Nested Record
type AwsConfig = {
  AccessKeyId : string
  DefaultRegion : string
  SecretAccessKey : string
}

type Config = {
  MagicNumber : int
  Aws : AwsConfig
}

// Reading a Nested Record type
let result = 
  appConfig.Get<Config> () // Result<Config, ConfigParseError>
```

Refer below for creating `configurationRoot` based on the file type and using FsConfig to read the values.  

#### JSON

```json
{
  "processId" : "123",
  "processName" : "FsConfig",
  "magicNumber" : 42,
  "aws" : {
    "accessKeyId" : "Id-123",
    "defaultRegion" : "us-east-1",
    "secretAccessKey" : "secret123"
  },
  "colors" : "Red,Green"
}
```

This JSON file can be read using 

```fsharp
// Requires NuGet package
// Microsoft.Extensions.Configuration.Json
let configurationRoot =  
  ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("settings.json").Build()

let appConfig = new AppConfig(configurationRoot)
let result = 
  appConfig.Get<Config> () // Result<Config, ConfigParseError>
```

#### XML

```xml
<Settings>
  <ProcessId>123</ProcessId>
  <ProcessName>FsConfig</ProcessName>
  <MagicNumber>42</MagicNumber>
  <Aws>
    <AccessKeyId>Id-123</AccessKeyId>
    <DefaultRegion>us-east-1</DefaultRegion>
    <SecretAccessKey>secret123</SecretAccessKey>
  </Aws>
  <Colors>Red,Green</Colors>
</Settings>
```

This XML file can be read using 

```fsharp
// Requires NuGet package
// Microsoft.Extensions.Configuration.Xml
let configurationRoot =  
  ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddXmlFile("settings.xml").Build()

let appConfig = new AppConfig(configurationRoot)
let result = 
  appConfig.Get<Config> () // Result<Config, ConfigParseError>
```

#### INI

```ini
ProcessId=123
ProcessName=FsConfig
MagicNumber=42
Colors=Red,Green

[Aws]
AccessKeyId=Id-123
DefaultRegion=us-east-1
SecretAccessKey=secret123
```

This INI file can be read using 

```fsharp
// Requires NuGet package
// Microsoft.Extensions.Configuration.Ini
let configurationRoot =  
  ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddIniFile("settings.ini").Build()

let appConfig = new AppConfig(configurationRoot)
let result = 
  appConfig.Get<Config> () // Result<Config, ConfigParseError>
```


### appSettings (Only Supported in V0.0.6 or below)

We can read the `appSettings` values using the `AppConfig` type instead of `EnvConfig` type. 

FsConfig uses the exact name of the field to derive the `appSettings` key name and doesn't use any separator by default.

```fsharp
type AwsConfig = {
  AccessKeyId : string
  DefaultRegion : string
  SecretAccessKey : string
}

type Config = {
  Port : uint16
  Aws : AwsConfig
}

let main argv =
  let config = 
    match AppConfig.Get<Config>() with
    | Ok config -> config
    | Error error -> failwithf "Error : %A" error
```

> The above code snippet looks for `appSettings` values with the name `Port`, `AwsAccessKeyId`, `AwsDefaultRegion`, `AwsSecretAccessKey` and populates the respective fields.


All the customisation that we have seen for `EnvConfig` is applicable for `AppConfig` as well.

## How FsConfig Works

If you are curious to know how FsConfig works and its internals then you might be interested in my blog post, [Generic Programming Made Easy](https://www.demystifyfp.com/fsharp/blog/generic-programming-made-easy/) that deep dives into the initial implementation of FsConfig.

## Feedback

> We all need people who will give us feedback. That's how we improve - Bill Gates.

Your suggestions/feedback are welcome!

## Acknowledgements

The idea of FsConfig is inspired by [Kelsey Hightower](https://twitter.com/kelseyhightower)'s golang library [envconfig](https://github.com/kelseyhightower/envconfig). 

FsConfig uses [Eirik Tsarpalis](https://twitter.com/eiriktsarpalis)'s [TypeShape](https://github.com/eiriktsarpalis/TypeShape) library for generic programming. 


## Maintainer(s)

- [@tamizhvendan](https://github.com/tamizhvendan)
