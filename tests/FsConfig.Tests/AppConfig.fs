namespace AppConfig.Tests

open NUnit.Framework
open FsConfig
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

    [<Test>] 
    member __.``getPrimitive should succeeds`` () =
      test <@ AppConfig.Get<int> "ProcessId" = Ok 321   @>

    [<Test>] 
    member __.``getRecord should succeeds`` () =
      test <@ AppConfig.Get<SampleConfig> () = Ok expectedRecord @>