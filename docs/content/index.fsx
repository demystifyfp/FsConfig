(*** do-not-eval-file ***)
(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
FsConfig
======================

FsConfig is a F# library for reading configuration data from environment variables and AppSettings with type safety

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The FsConfig library can be <a href="https://nuget.org/packages/FsConfig">installed from NuGet</a>:
      <pre>PM> Install-Package FsConfig</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Why FsConfig?
=============

To understand FsConfig, let's have a look at an use case from the [FsTweet](https://github.com/demystifyfp/FsTweet) application.

The FsTweet application follows [The Twelve-Factor App](https://12factor.net/config) guideline for managing the configuration data. During the application bootstrap, it retrieves its ten configuration parameters from their respective environment variables.

*)

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

  0

(**

Though the code snippet does the job, there are some shortcomings.

1. The code is verbose.
2. There is no error handling to deal with the absence of values or wrong values.
3. Explicit type casting

With the help of FsConfig, we can overcome these limitations by specifying the configuration data as a F# Record type.

*)

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

(**

And then read all the associated environment variables in a single function call with type safety and error handling!

*)

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

(**

Supported Data Types
=====================

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

Option Type
-----------

FsConfig allows us to specify optional configuration parameters using the `option` type. In the previous example, if the configuration parameter `Port` is optional, we can define it like 

```diff
type Config = {
   ...
-  Port : uint16
+  Port : uint16 option
}
```

Discriminated Union Type
-----------------

FsConfig supports Discriminated Union Types that has cases alone.


*)

type Color =
| Red
| Green
| Blue 

type Config = {
  ConsoleColor : Color
}

(**

> With this configuration declaration, FsConfig read the environment variable `CONSOLE_COLOR` and populates the `ConsoleColor` field of type `Color`.

> List of Discriminated Union Types also supported: see under _List Type_

Enumerations
-----------------

FsConfig can read enumerations using either their numeric values or their case names.

The `[<Flags>]` attribute is also supported, using only comma-separated values.

*)

[<Flags>]
type Status =
| Inactive = 0
| Receiving = 1
| Transmitting = 2

type Config = {
  Status : Status
}

(**

> This will result in the status `Receiving ||| Transmitting` 
```bash
STATUS=Receiving,Transmitting
```

> This will result in the status `Inactive` 
```bash
STATUS=0
```

Case sensitivity
-----------------

When parsing enumerations or discriminated unions, FsConfig will first look for an exact match (case sensitive), but if no matches are found, it will also accept a case-insensitive match.

*)

type LogLevel =
| Debug = 0
| Informative = 1
| Warning = 2
| Error = 3

type Config = {
  LogLevel : LogLevel
}

(**

> This is a valid configuration
```bash
LOG_LEVEL=warning
```

List Type
---------

FsConfig also supports `list` type, and it expects comma separated individual values. 

For example, to get mulitple ports, we can define the config as 

*)

type Config = {
  Port : uint16 list
}

(**

and then pass the value `8084,8085,8080` using the environment variable `PORT`.

The default separator for the list can be changed if needed using the `ListSeparator` attribute.

*)

[<Convention("MYENV")>]
type CustomListSeparatorSampleConfig = {
  ProcessNames : string list
  [<ListSeparator(';')>]
  ProcessIds : uint16 list
  [<ListSeparator('|')>]
  PipedFlow : int list    
}

(**

> With this configuration declaration, FSConfig would be able to read the following entries from App.settings.

```xml
  <add key="MYENVProcessNames" value="conhost.exe,gitter.exe"/>
  <add key="MYENVProcessIds" value="4700;15680"/>
  <add key="MYENVPipedFlow" value="4700|15680|-1" />
```

A definition similar to the one shown below will allow parsing of standalone lists.
*)

type IntListUsingSemiColonsConfig = {
  [<ListSeparator(';')>]
  IntListUp : int list
}

(**

> E.g. an environment variable containing
```bash
INT_LIST_UP=42;43;44 
```

Record Type
-----------

As shown in the initial example, FsConfig allows us to group similar configuration into a record type.

*)

type AwsConfig = {
  AccessKeyId : string
  DefaultRegion : string
  SecretAccessKey : string
}

type Config = {
  Aws : AwsConfig
}

(**

> With this configuration declaration, FsConfig read the environment variables `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, and `AWS_DEFAULT_REGION` and populates the `Aws` field of type `AwsConfig`.

Default Value
=============

If you'd like to use a default value in the absence of a field value, you can make use of the `DefaultValue` attribute.

*)

type Config = {
  [<DefaultValue("8080")>]
  HttpServerPort : int16
  [<DefaultValue("Server=localhost;Port=5432;Database=FsTweet;User Id=postgres;Password=test;")>]
  DbConnectionString: string
}


(**

Environment Variable Name Convention & Customization
=========================================================

By default, FsConfig follows Underscores with uppercase convention, as in `UPPER_CASE`, for deriving the environment variable name. 

*)

type Config = {
  ServerKey : string
}

(**

> Using this configuration declaration, FsConfig read the environment variable `SERVER_KEY` and populates the `ServerKey` field


To specify a custom prefix in the environment variables, we can make use of the `Convention` attribute.

*)

[<Convention("FSTWEET")>]
type Config = {
  ServerKey : string
}

(**


> For this configuration declaration, FsConfig read the environment variable `FSTWEET_SERVER_KEY` and populates the `ServerKey` field.

We can also override the separator character `_` using the `Convention` attribute's optional field `Separator`

*)

[<Convention("FSTWEET", Separator="-")>]
type Config = {
  ServerKey : string
}

(**


> In this case, FsConfig derives the environment variable name as `FSTWEET-SERVER-KEY`.


If an environment variable name is not following a convention, we can override the environment variable name at the field level using the `CustomName` attribute.

*)

type Config = {
  [<CustomName("MY_SERVER_KEY")>]
  ServerKey : string
}

(**
> Here, FsConfig uses the environment variable name `MY_SERVER_KEY` to get the ServerKey.


We can also merely customise (or control) the environment variable name generation by passing an higher-order function while calling the `Get` function
*)

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
  0

(**
 

> FsConfig computes the environment variable name as `fstweet-server-key` in this scenario.

## Getting Individual Environment Variables

FsConfig also supports reading value directly by explicitly specifying the environment variable name
*)

EnvConfig.Get<decimal> "MY_APP_INITIAL_BALANCE" // Result<decimal, ConfigParseError>

(**

## App Config

FsConfig supports App Config for both DotNet Core and Non DotNet Core Applications. 

* [DotNet Core Applications](#dotnet-core-configuration) (Supported from V2.0.0 or above)

* [Non DotNet Core Applications](#appsettings-only-supported-in-v006-or-below) (Only Supported in V0.0.6 or below)

### DotNet Core Configuration (Supported from V2.0.0 or above)

FsConfig abstracts the configuration provider by depending on [IConfigurationRoot](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationroot).
*)

let configurationRoot : IConfigurationRoot = "{...}"
let appConfig = new AppConfig(configurationRoot)

(**

After creating an instance `appConfig` (of type `AppConfig` from FsConfig), you can use it to read the configuration values as below

*)

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

(**

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

*)

// Requires NuGet package
// Microsoft.Extensions.Configuration.Json
let configurationRoot =  
  ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("settings.json").Build()

let appConfig = new AppConfig(configurationRoot)
let result = 
  appConfig.Get<Config> () // Result<Config, ConfigParseError>


(** 
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
*)

// Requires NuGet package
// Microsoft.Extensions.Configuration.Xml
let configurationRoot =  
  ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddXmlFile("settings.xml").Build()

let appConfig = new AppConfig(configurationRoot)
let result = 
  appConfig.Get<Config> () // Result<Config, ConfigParseError>


(** 
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
*)

// Requires NuGet package
// Microsoft.Extensions.Configuration.Ini
let configurationRoot =  
  ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddIniFile("settings.ini").Build()

let appConfig = new AppConfig(configurationRoot)
let result = 
  appConfig.Get<Config> () // Result<Config, ConfigParseError>

(** 

### appSettings (Only Supported in V0.0.6 or below)

We can read the `appSettings` values using the `AppConfig` type instead of `EnvConfig` type. 

FsConfig uses the exact name of the field to derive the `appSettings` key name and doesn't use any separator by default.

*)

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
  0
(**

> The above code snippet looks for `appSettings` values with the name `Port`, `AwsAccessKeyId`, `AwsDefaultRegion`, `AwsSecretAccessKey` and populates the respective fields.


All the customisation that we have seen for `EnvConfig` is applicable for `AppConfig` as well.

How FsConfig Works
==================

If you are curious to know how FsConfig works and its internals then you might be interested in my blog post, [Generic Programming Made Easy](https://www.demystifyfp.com/fsharp/blog/generic-programming-made-easy/) that deep dives into the initial implementation of FsConfig.


Acknowledgements
=================

The idea of FsConfig is inspired by [Kelsey Hightower](https://twitter.com/kelseyhightower)'s golang library [envconfig](https://github.com/kelseyhightower/envconfig). 

FsConfig uses [Eirik Tsarpalis](https://twitter.com/eiriktsarpalis)'s [TypeShape](https://github.com/eiriktsarpalis/TypeShape) library for generic programming. 


Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/FsConfig/tree/master/docs/content
  [gh]: https://github.com/fsprojects/FsConfig
  [issues]: https://github.com/fsprojects/FsConfig/issues
  [readme]: https://github.com/fsprojects/FsConfig/blob/master/README.md
  [license]: https://github.com/fsprojects/FsConfig/blob/master/LICENSE.txt
*)
