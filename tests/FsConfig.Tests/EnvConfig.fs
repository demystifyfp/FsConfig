namespace EnvConfig.Tests

open System
open NUnit.Framework
open FsConfig
open FsConfig.Tests.Common
open Swensen.Unquote.Assertions

module Common =
  
  let setEnvVar (key,value) =
    Environment.SetEnvironmentVariable(key,value, EnvironmentVariableTarget.Process)
  
  let getEnvVar key =
    Environment.GetEnvironmentVariable(key,EnvironmentVariableTarget.Process)

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
    let expected = NotFound "PROCESS_ID" |> Error
    test <@ expected = result @>


  [<Test>]
  let ``getRecord with custom prefix should return not found error`` () =
    let result = 
      EnvConfig.Get<CustomPrefixSampleConfig> ()
    let expected = NotFound "MYAPP_PROCESS_ID" |> Error
    test <@ expected = result @>

  [<Test>]
  let ``getRecord with custom separator should return not found error`` () =
    let result = 
      EnvConfig.Get<CustomSeparatorSampleConfig> ()
    let expected = NotFound "PROCESS-ID" |> Error
    test <@ expected = result @>


  [<Test>]
  let ``getRecord with custom Config Name Canonicalizer should return not found error`` () =
    let result = 
      EnvConfig.Get<SampleConfig> lowerCaseConfigNameCanonicalizer
    let expected = NotFound "processid" |> Error
    test <@ expected = result @>


module ``Given required environment variables exists`` =
  open Common

  [<TestFixture>]
  type ``with valid values`` () =
    let expectedRecord : SampleConfig = {ProcessId = 123; ProcessName = "fsconfig.exe"}

    let expectedCustomPrefixRecord : CustomPrefixSampleConfig = 
      {ProcessId = expectedRecord.ProcessId; ProcessName = expectedRecord.ProcessName}
    let expectedCustomSeparatorRecord : CustomSeparatorSampleConfig = 
      {ProcessId = expectedRecord.ProcessId; ProcessName = expectedRecord.ProcessName}
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
      test <@ EnvConfig.Get<CustomPrefixSampleConfig> () = Ok expectedCustomPrefixRecord @>

    [<Test>] 
    member __.``getRecord with custom separator should succeeds`` () =
      test <@ EnvConfig.Get<CustomSeparatorSampleConfig> () = Ok expectedCustomSeparatorRecord @>

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
  let ``get datetime offset, time span and Guid should succeed`` () =
    setEnvVar ("DATE_TIME_OFFSET", "5/01/2008 14:57:32.80 -07:00")
    setEnvVar ("TIME_SPAN", "99.23:59:59.9999999")
    setEnvVar ("GUID", "f36fd7ca-1005-4d72-af92-c62e63cccaaf")
    test <@ EnvConfig.Get<NonIConvertibleConfig> () = Ok expectedNonIConvertibleConfig @>

  [<Test>]
  let ``get Enum should succeed`` () =
    setEnvVar ("ENV_ENUM_STRING", "Red")
    setEnvVar ("ENV_ENUM_FLAGS", "Red, Blue")
    setEnvVar ("ENV_ENUM_INT", "0")
    let expectedFlagOutput = Color.Red ||| Color.Blue
    test <@ EnvConfig.Get<Color> "ENV_ENUM_STRING" = Ok Color.Red @>
    test <@ EnvConfig.Get<Color> "ENV_ENUM_INT" = Ok Color.Red @>
    test <@ EnvConfig.Get<Color> "ENV_ENUM_FLAGS" = Ok expectedFlagOutput @>


module ``Getting option type`` =
  open Common

  type OptionConfig = {
    IntOption : int option
  }

  [<Test>]
  let ``return none if environment variable not exists`` () =
    setEnvVar ("INT_OPTION", null)
    test <@ EnvConfig.Get<OptionConfig> () = Ok {IntOption = None} @>

  [<Test>]
  let ``return some of value if environment variable exists`` () =
    setEnvVar ("INT_OPTION", "42")
    test <@ EnvConfig.Get<OptionConfig> () = Ok {IntOption = Some 42} @>


  [<Test>]
  let ``return bad value error if environment variable exists with wrong format`` () =
    setEnvVar ("INT_OPTION", "foo")
    test <@ EnvConfig.Get<OptionConfig> () = Error (BadValue ("INT_OPTION", "foo")) @>

module ``Getting record with mutlitple option type fields`` =
  open Common

  type Config = {
    ProcessCount : int option
    Timeout : TimeSpan option
    AwsAccessKeyId : string option
  }

  [<Test>]
  let ``return record with corresponding option value`` () =
    setEnvVar ("PROCESS_COUNT", "42")
    setEnvVar ("AWS_ACCESS_KEY_ID", "ID-123")
    test <@ EnvConfig.Get<Config> () = Ok ({ProcessCount = Some 42; Timeout = None; AwsAccessKeyId = Some "ID-123"}) @>

module ``Getting list type`` =
  open Common

  type IntListConfig = {
    IntList : int list
  }

  [<Test>]
  let ``return empty list if environment variable not exists`` () =
    test <@ EnvConfig.Get<IntListConfig> () = Ok {IntList = []} @>

  [<Test>]
  let ``return singleton list if environment variable exist with one value`` () =
    setEnvVar ("INT_LIST", "42")
    test <@ EnvConfig.Get<IntListConfig> () = Ok {IntList = [42]} @>

  [<Test>]
  let ``return singleton list if environment variable exist with one value if custom list separator set`` () =
    setEnvVar ("INT_LIST_UP", "43")
    test <@ EnvConfig.Get<IntListUsingPipesConfig> () = Ok {IntListUp = [43]} @>


  [<Test>]
  let ``return list if environment variable exist with mutiple comma separated values`` () =
    setEnvVar ("INT_LIST", "42, 43,44")
    test <@ EnvConfig.Get<IntListConfig> () = Ok {IntList = [42;43;44]} @>

  [<Test>]
  let ``return list if environment variable exist with mutiple | separated values`` () =
    setEnvVar ("INT_LIST_UP", "42|43|44")
    test <@ EnvConfig.Get<IntListUsingPipesConfig> () = Ok {IntListUp = [42;43;44]} @>

module ``Getting record with record type`` =
  open Common

  [<Test>]
  let ``return record with corresponding record value`` () =
    setEnvVar ("MAGIC_NUMBER", "42")
    setEnvVar ("AWS_ACCESS_KEY_ID", "Id-123")
    setEnvVar ("AWS_SECRET_ACCESS_KEY", "secret123")
    setEnvVar ("AWS_DEFAULT_REGION", "us-east-1")
    let expected = 
      {
        Config.MagicNumber = 42
        Aws = 
        {
          AccessKeyId = "Id-123"
          DefaultRegion = "us-east-1"
          SecretAccessKey = "secret123"
        }
      } |> Ok
    test <@ EnvConfig.Get<Config> () = expected @>


module ``Getting record with custom prefix and record type`` =
  open Common

  [<Test>]
  let ``return record with corresponding record value`` () =
    setEnvVar ("MYAPP_MAGIC_NUMBER", "42")
    setEnvVar ("MYAPP_AWS_ACCESS_KEY_ID", "Id-123")
    setEnvVar ("MYAPP_AWS_SECRET_ACCESS_KEY", "secret123")
    setEnvVar ("MYAPP_AWS_DEFAULT_REGION", "us-east-1")
    let expected = 
      {
        ConfigWithCustomPrefix.MagicNumber = 42
        Aws = 
        {
          AccessKeyId = "Id-123"
          DefaultRegion = "us-east-1"
          SecretAccessKey = "secret123"
        }
      } |> Ok
    test <@ EnvConfig.Get<ConfigWithCustomPrefix> () = expected @>


 module ``Getting record with custom name properties`` =
  open Common

  type Config = {
    [<CustomName("magic-number")>]
    MagicNumber : int
  }

  [<Test>]
  let ``return record with corresponding value`` () =
    setEnvVar ("magic-number", "42")
    test <@ EnvConfig.Get<Config> () = Ok ({MagicNumber = 42}) @>