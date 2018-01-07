namespace EnvConfig.Tests

open System
open NUnit.Framework
open FsConfig
open Swensen.Unquote.Assertions

type SampleConfig = {
  ProcessId : int
  ProcessName : string
}

module Common =
  let defaultParamsWithCustomPrefix = 
    {EnvConfig.defaultParams with Prefix = "MYAPP"}

  let defaultParamsWithCustomSeparator =
    {EnvConfig.defaultParams with Separator = "-"}

  let lowerCaseConfigNameCanonicalizer = {
        new IConfigNameCanonicalizer with
          member __.Canonicalize name = name.ToLowerInvariant()
      }
  let setEnvVar (key,value) =
    Environment.SetEnvironmentVariable(key,value, EnvironmentVariableTarget.Process)

module ``Given required environment variables not exist`` =
  open Common
  [<Test>]
  let ``getPrimitive should return not found error`` () =
    let result = EnvConfig.Get<int> "NOT_EXIST"
    let expected = NotFound "NOT_EXIST" |> Error
    test <@ expected = result  @>

  [<Test>]
  let ``getRecord should return not found error`` () =
    let result = EnvConfig.Get<SampleConfig>()
    let expected = [NotFound "PROCESS_NAME"; NotFound "PROCESS_ID"] |> Error
    test <@ expected = result @>


  [<Test>]
  let ``getRecord with custom prefix should return not found error`` () =
    let result = 
      EnvConfig.Get<SampleConfig> defaultParamsWithCustomPrefix
    let expected = [NotFound "MYAPP_PROCESS_NAME"; NotFound "MYAPP_PROCESS_ID"] |> Error
    test <@ expected = result @>

  [<Test>]
  let ``getRecord with custom separator should return not found error`` () =
    let result = 
      EnvConfig.Get<SampleConfig> defaultParamsWithCustomSeparator
    let expected = [NotFound "PROCESS-NAME"; NotFound "PROCESS-ID"] |> Error
    test <@ expected = result @>


  [<Test>]
  let ``getRecord with custom Config Name Canonicalizer should return not found error`` () =
    let result = 
      EnvConfig.Get<SampleConfig> lowerCaseConfigNameCanonicalizer
    let expected = [NotFound "processname"; NotFound "processid"] |> Error
    test <@ expected = result @>


module ``Given required environment variables exists`` =
  open Common

  [<TestFixture>]
  type ``with valid values`` () =
    let expectedRecord = {ProcessId = 123; ProcessName = "fsconfig.exe"}
    member __.envVars =
      [
        "PROCESS_ID", expectedRecord.ProcessId.ToString()
        "PROCESS_NAME", expectedRecord.ProcessName
        "MYAPP_PROCESS_ID", expectedRecord.ProcessId.ToString()
        "MYAPP_PROCESS_NAME", expectedRecord.ProcessName
        "PROCESS-ID",expectedRecord.ProcessId.ToString()
        "PROCESS-NAME", expectedRecord.ProcessName
        "processid",expectedRecord.ProcessId.ToString()
        "processname", expectedRecord.ProcessName
      ]
    [<TestFixtureSetUp>]
    member this.testFixtureSetUp () =
      this.envVars
      |> List.iter setEnvVar
        
    [<TestFixtureTearDown>]
    member this.testFixtureTearDown () =
      this.envVars
      |> List.iter (fun (k,_) -> setEnvVar(k,null))

    [<Test>] 
    member __.``getPrimitive should succeeds`` () =
      test <@ EnvConfig.Get<int> "PROCESS_ID" = Ok 123   @>

    [<Test>] 
    member __.``getRecord should succeeds`` () =
      test <@ EnvConfig.Get<SampleConfig> () = Ok expectedRecord @>

    [<Test>] 
    member __.``getRecord with custom prefix should succeeds`` () =
      test <@ EnvConfig.Get<SampleConfig> defaultParamsWithCustomPrefix = Ok expectedRecord @>

    [<Test>] 
    member __.``getRecord with custom separator should succeeds`` () =
      test <@ EnvConfig.Get<SampleConfig> defaultParamsWithCustomSeparator = Ok expectedRecord @>

    [<Test>] 
    member __.``getRecord with custom Config Name Canonicalizer should succeeds`` () =
      test <@ EnvConfig.Get<SampleConfig> lowerCaseConfigNameCanonicalizer = Ok expectedRecord @>


  [<Test>]
  let ``get bool should succeed`` () =
    setEnvVar ("ENV_BOOL1", "true")
    setEnvVar ("ENV_BOOL2", "false")
    test <@ EnvConfig.Get<bool> "ENV_BOOL1" = Ok true @>
    test <@ EnvConfig.Get<bool> "ENV_BOOL2" = Ok false @>

  [<Test>]
  let ``get int should succeed`` () =
    setEnvVar ("ENV_INT16", "12")
    setEnvVar ("ENV_INT32", "567")
    setEnvVar ("ENV_INT64", "1200000123232323")
    test <@ EnvConfig.Get<int16> "ENV_INT16" = Ok 12s @>
    test <@ EnvConfig.Get<int> "ENV_INT32" = Ok 567 @>
    test <@ EnvConfig.Get<int64> "ENV_INT64" = Ok 1200000123232323L @>

  [<Test>]
  let ``get uint should succeed`` () =
    setEnvVar ("ENV_UINT16", "12")
    setEnvVar ("ENV_UINT32", "567")
    setEnvVar ("ENV_UINT64", "1200000123232323")
    test <@ EnvConfig.Get<uint16> "ENV_UINT16" = Ok 12us @>
    test <@ EnvConfig.Get<uint32> "ENV_UINT32" = Ok 567u @>
    test <@ EnvConfig.Get<uint64> "ENV_UINT64" = Ok 1200000123232323uL @>

  [<Test>]
  let ``get string should succeed`` () =
    setEnvVar ("ENV_STRING", "567")
    test <@ EnvConfig.Get<string> "ENV_STRING" = Ok "567" @>

  [<Test>]
  let ``get byte should succeed`` () =
    setEnvVar ("ENV_BYTE", "86")
    setEnvVar ("ENV_SBYTE", "86")
    test <@ EnvConfig.Get<byte> "ENV_BYTE" = Ok 86uy @>
    test <@ EnvConfig.Get<sbyte> "ENV_SBYTE" = Ok 86y @>

  [<Test>]
  let ``get floating point numbers should succeed`` () =
    setEnvVar ("ENV_FLOAT", "3.14")
    setEnvVar ("ENV_FLOAT32", "3.14")
    setEnvVar ("ENV_SINGLE", "3.14")
    setEnvVar ("ENV_DOUBLE", "6.28")
    setEnvVar ("ENV_DECIMAL", "123123.123493234342322")
    test <@ EnvConfig.Get<float> "ENV_FLOAT" = Ok 3.14 @>
    test <@ EnvConfig.Get<float32> "ENV_FLOAT32" = Ok 3.14F @>
    test <@ EnvConfig.Get<single> "ENV_SINGLE" = Ok 3.14F @>
    test <@ EnvConfig.Get<double> "ENV_DOUBLE" = Ok 6.28 @>
    test <@ EnvConfig.Get<decimal> "ENV_DECIMAL" = Ok 123123.123493234342322m @>

  [<Test>]
  let ``get char should succeed`` () =
    setEnvVar ("ENV_CHAR", "a")
    test <@ EnvConfig.Get<char> "ENV_CHAR" = Ok 'a' @>


  [<Test>]
  let ``get datetime should succeed`` () =
    setEnvVar ("ENV_DATE_TIME", "5/01/2008 14:57:32.80 -07:00")
    let expected = DateTime.Parse "5/01/2008 14:57:32.80 -07:00"
    test <@ EnvConfig.Get<DateTime> "ENV_DATE_TIME" = Ok expected @>


  [<Test>]
  let ``get datetime offset should succeed`` () =
    setEnvVar ("ENV_DATE_TIME_OFFSET", "5/01/2008 14:57:32.80 -07:00")
    let expected = DateTimeOffset.Parse "5/01/2008 14:57:32.80 -07:00"
    test <@ EnvConfig.Get<DateTimeOffset> "ENV_DATE_TIME_OFFSET" = Ok expected @>
  
  [<Test>]
  let ``get time span should succeed`` () =
    setEnvVar ("ENV_TIME_SPAN", "99.23:59:59.9999999")
    let expected = TimeSpan.Parse "99.23:59:59.9999999"
    test <@ EnvConfig.Get<TimeSpan> "ENV_TIME_SPAN" = Ok expected @>


module ``Getting option type`` =
  open Common

  [<Test>]
  let ``return none if environment variable not exists`` () =
    test <@ EnvConfig.Get<int option> "ENV_INT_OPTION_NONE" = Ok None @>

  [<Test>]
  let ``return some of value if environment variable exists`` () =
    setEnvVar ("ENV_INT_OPTION_SOME", "42")
    test <@ EnvConfig.Get<int option> "ENV_INT_OPTION_SOME" = Ok (Some 42) @>


  [<Test>]
  let ``return bad value error if environment variable exists with wrong format`` () =
    setEnvVar ("ENV_INT_OPTION_BAD", "foo")
    test <@ EnvConfig.Get<int option> "ENV_INT_OPTION_BAD" = Error (BadValue ("ENV_INT_OPTION_BAD", "foo")) @>


module ``Getting record with option type`` =
  open Common

  type Config = {
    ProcessCount : int option
    Timeout : TimeSpan option
  }

  [<Test>]
  let ``return record with corresponding option value`` () =
    setEnvVar ("PROCESS_COUNT", "42")
    test <@ EnvConfig.Get<Config> () = Ok ({ProcessCount = Some 42; Timeout = None}) @>
