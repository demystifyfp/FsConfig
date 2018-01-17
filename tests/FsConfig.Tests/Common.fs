namespace FsConfig.Tests

module Common =

  type SampleConfig = {
    ProcessId : int
    ProcessName : string
  }

  let lowerCaseConfigNameCanonicalizer _ (name : string) = 
    name.ToLowerInvariant()