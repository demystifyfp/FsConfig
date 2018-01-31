namespace FsConfig.Tests

module Common =
  open FsConfig
  open System
  type SampleConfig = {
    ProcessId : int
    ProcessName : string
  }

  [<Convention("MYAPP")>]
  type CustomPrefixSampleConfig = {
    ProcessId : int
    ProcessName : string
  }

  [<Convention("", Separator = "-")>]
  type CustomSeparatorSampleConfig = {
    ProcessId : int
    ProcessName : string
  }

  [<Convention("MYAPP", Separator = "__")>]
  type CustomPrefixAndSeparatorSampleConfig = {
    ProcessId : int
    ProcessName : string
  }

  type NonIConvertibleConfig = {
    DateTimeOffset : DateTimeOffset
    TimeSpan : TimeSpan
    Guid : Guid
  }

  let expectedNonIConvertibleConfig = {
    DateTimeOffset = DateTimeOffset.Parse "5/01/2008 14:57:32.80 -07:00"
    TimeSpan = TimeSpan.Parse "99.23:59:59.9999999"
    Guid = System.Guid.Parse("f36fd7ca-1005-4d72-af92-c62e63cccaaf")
  }

  let lowerCaseConfigNameCanonicalizer _ (name : string) = 
    name.ToLowerInvariant()

  [<Flags>]
  type Color =
  | Red = 0
  | Blue = 1
  | Green = 2


  type OptionConfig = {
    IntOption : int option
  }

  type AwsConfig = {
    AccessKeyId : string
    DefaultRegion : string
    SecretAccessKey : string
  }

  type Config = {
    MagicNumber : int
    Aws : AwsConfig
  }