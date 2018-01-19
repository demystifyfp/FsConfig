namespace FsConfig.Tests

module Common =
  open FsConfig
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

  let lowerCaseConfigNameCanonicalizer _ (name : string) = 
    name.ToLowerInvariant()