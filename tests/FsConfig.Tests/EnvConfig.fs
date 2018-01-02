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

module ``Given required environment variables not exist`` =
  open Common
  [<Test>]
  let ``parsePrimitive should return not found error`` () =
    let result = EnvConfig.Parse<int> "NOT_EXIST"
    let expected = NotFound "NOT_EXIST" |> Error
    test <@ expected = result  @>

  [<Test>]
  let ``parseRecord should return not found error`` () =
    let result = EnvConfig.Parse<SampleConfig>()
    let expected = [NotFound "PROCESS_NAME"; NotFound "PROCESS_ID"] |> Error
    test <@ expected = result @>


  [<Test>]
  let ``parseRecord with custom prefix should return not found error`` () =
    let result = 
      EnvConfig.Parse<SampleConfig> defaultParamsWithCustomPrefix
    let expected = [NotFound "MYAPP_PROCESS_NAME"; NotFound "MYAPP_PROCESS_ID"] |> Error
    test <@ expected = result @>

  [<Test>]
  let ``parseRecord with custom separator should return not found error`` () =
    let result = 
      EnvConfig.Parse<SampleConfig> defaultParamsWithCustomSeparator
    let expected = [NotFound "PROCESS-NAME"; NotFound "PROCESS-ID"] |> Error
    test <@ expected = result @>


  [<Test>]
  let ``parseRecord with custom Config Name Canonicalizer should return not found error`` () =
    let result = 
      EnvConfig.Parse<SampleConfig> lowerCaseConfigNameCanonicalizer
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
      |> List.iter (fun (k,v) ->
        Environment.SetEnvironmentVariable(k,v, EnvironmentVariableTarget.Process)
      )
        
    [<TestFixtureTearDown>]
    member this.testFixtureTearDown () =
      this.envVars
      |> List.iter (fun (k,_) -> 
        Environment.SetEnvironmentVariable(k,null, EnvironmentVariableTarget.Process)
      )

    [<Test>] 
    member __.``parsePrimitive should succeeds`` () =
      test <@ EnvConfig.Parse<int> "PROCESS_ID" = Ok 123   @>

    [<Test>] 
    member __.``parseRecord should succeeds`` () =
      test <@ EnvConfig.Parse<SampleConfig> () = Ok expectedRecord @>

    [<Test>] 
    member __.``parseRecord with custom prefix should succeeds`` () =
      test <@ EnvConfig.Parse<SampleConfig> defaultParamsWithCustomPrefix = Ok expectedRecord @>

    [<Test>] 
    member __.``parseRecord with custom separator should succeeds`` () =
      test <@ EnvConfig.Parse<SampleConfig> defaultParamsWithCustomSeparator = Ok expectedRecord @>

    [<Test>] 
    member __.``parseRecord with custom Config Name Canonicalizer should succeeds`` () =
      test <@ EnvConfig.Parse<SampleConfig> lowerCaseConfigNameCanonicalizer = Ok expectedRecord @>