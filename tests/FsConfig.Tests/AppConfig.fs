namespace AppConfig.Tests

open NUnit.Framework
open FsConfig
open System
open FsConfig.Tests.Common
open Swensen.Unquote.Assertions

module ``Given required app setting not exist`` =
  type ConfigNotFound = {
    AccessKey : string
  }

  [<Test>]
  let ``getPrimitive should return not found error`` () =
    let result = AppConfig.Get<int> "NotExist"
    let expected = NotFound "NotExist" |> Error
    test <@ expected = result  @>

  [<Test>]
  let ``getRecord should return not found error`` () =
    let result = AppConfig.Get<ConfigNotFound>()
    let expected = NotFound "AccessKey" |> Error
    test <@ expected = result @>


  [<Test>]
  let ``getRecord with custom Config Name Canonicalizer should return not found error`` () =
    let result = 
      EnvConfig.Get<ConfigNotFound> lowerCaseConfigNameCanonicalizer
    let expected = NotFound "accesskey" |> Error
    test <@ expected = result @>


module ``Given required app settings exists`` =

  [<TestFixture>]
  type ``with valid values`` () =
    let expectedRecord : SampleConfig = {ProcessId = 321; ProcessName = "appconfig.exe"}
    let expectedCustomPrefixRecord : CustomPrefixSampleConfig = 
      {ProcessId = expectedRecord.ProcessId; ProcessName = expectedRecord.ProcessName}
    let expectedCustomSeparatorRecord : CustomSeparatorSampleConfig = 
      {ProcessId = expectedRecord.ProcessId; ProcessName = expectedRecord.ProcessName}

    [<Test>] 
    member __.``getPrimitive should succeeds`` () =
      test <@ AppConfig.Get<int> "ProcessId" = Ok 321   @>

    [<Test>] 
    member __.``getRecord should succeeds`` () =
      test <@ AppConfig.Get<SampleConfig> () = Ok expectedRecord @>

    [<Test>] 
    member __.``getRecord with custom prefix should succeeds`` () =
      test <@ AppConfig.Get<CustomPrefixSampleConfig> () = Ok expectedCustomPrefixRecord @>

    [<Test>] 
    member __.``getRecord with custom separator should succeeds`` () =
      test <@ AppConfig.Get<CustomSeparatorSampleConfig> () = Ok expectedCustomSeparatorRecord @>

    [<Test>] 
    member __.``getRecord with custom Config Name Canonicalizer should succeeds`` () =
      test <@ AppConfig.Get<SampleConfig> lowerCaseConfigNameCanonicalizer = Ok expectedRecord @>

  
  [<Test>]
  let ``get bool should succeed`` () =
    test <@ AppConfig.Get<bool> "Bool1" = Ok true @>
    test <@ AppConfig.Get<bool> "Bool2" = Ok false @>


  [<Test>]
  let ``get int should succeed`` () =
    test <@ AppConfig.Get<int16> "INT16" = Ok 12s @>
    test <@ AppConfig.Get<int> "INT32" = Ok 567 @>
    test <@ AppConfig.Get<int64> "INT64" = Ok 1200000123232323L @>

  [<Test>]
  let ``get uint should succeed`` () =
    test <@ AppConfig.Get<uint16> "UINT16" = Ok 12us @>
    test <@ AppConfig.Get<uint32> "UINT32" = Ok 567u @>
    test <@ AppConfig.Get<uint64> "UINT64" = Ok 1200000123232323uL @>


  [<Test>]
  let ``get string should succeed`` () =
    test <@ AppConfig.Get<string> "STRING" = Ok "567" @>

  [<Test>]
  let ``get floating point numbers should succeed`` () =
    test <@ AppConfig.Get<float> "FLOAT" = Ok 3.14 @>
    test <@ AppConfig.Get<float32> "FLOAT32" = Ok 3.14F @>
    test <@ AppConfig.Get<single> "SINGLE" = Ok 3.14F @>
    test <@ AppConfig.Get<double> "DOUBLE" = Ok 6.28 @>
    test <@ AppConfig.Get<decimal> "DECIMAL" = Ok 123123.123493234342322m @>

  [<Test>]
  let ``get char should succeed`` () =
    test <@ AppConfig.Get<char> "CHAR" = Ok 'a' @>


  [<Test>]
  let ``get datetime should succeed`` () =
    let expected = DateTime.Parse "5/01/2008 14:57:32.80 -07:00"
    test <@ AppConfig.Get<DateTime> "DATE_TIME" = Ok expected @>

  [<Test>]
  let ``get datetime offset, time span and Guid should succeed`` () =
    test <@ AppConfig.Get<NonIConvertibleConfig> () = Ok expectedNonIConvertibleConfig @>

  [<Test>]
  let ``get Enum should succeed`` () =
    let expectedFlagOutput = Color.Red ||| Color.Blue
    test <@ AppConfig.Get<Color> "ENUM_STRING" = Ok Color.Red @>
    test <@ AppConfig.Get<Color> "ENUM_INT" = Ok Color.Red @>
    test <@ AppConfig.Get<Color> "ENUM_FLAGS" = Ok expectedFlagOutput @>


module ``Getting option type`` =

  type OptionConfigNotExist = {
    IntOptionNotExist : int option
  }
  type OptionConfigBadValue = {
    IntOptionBadValue : int option
  }

  [<Test>]
  let ``return none if environment variable not exists`` () =
    test <@ AppConfig.Get<OptionConfigNotExist> () = Ok {IntOptionNotExist = None} @>

  [<Test>]
  let ``return some of value if environment variable exists`` () =
    test <@ AppConfig.Get<OptionConfig> () = Ok {IntOption = Some 42} @>


  [<Test>]
  let ``return bad value error if environment variable exists with wrong format`` () =
    test <@ AppConfig.Get<OptionConfigBadValue> () = Error (BadValue ("IntOptionBadValue", "foo")) @>

module ``Getting record with record type`` =

  [<Test>]
  let ``return record with corresponding option value`` () =
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
    test <@ AppConfig.Get<Config> () = expected @>

module ``Getting record with custom name properties`` =
  type Config = {
    [<CustomName("magic-number")>]
    MagicNumber : int
  }

  [<Test>]
  let ``return record with corresponding value`` () =
    test <@ AppConfig.Get<Config> () = Ok ({MagicNumber = 42}) @>