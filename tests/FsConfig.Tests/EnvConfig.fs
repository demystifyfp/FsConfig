module EnvConfig.Tests

open NUnit.Framework
open FsConfig
open Swensen.Unquote.Assertions

[<Test>]
let ``parsePrimitive not found use case`` () =
  let result = EnvConfig.Parse<int> "NOT_EXIST"
  let expected = NotFound "NOT_EXIST" |> Error
  test <@ expected = result  @>

type SampleConfig = {
  ProcessId : int
  ProcessName : string
}

[<Test>]
let ``parseRecord not found use case`` () =
  let result = EnvConfig.Parse<SampleConfig>()
  let expected = [NotFound "PROCESS_NAME"; NotFound "PROCESS_ID"] |> Error
  test <@ expected = result @>


[<Test>]
let ``parseRecord with custom prefix not found use case`` () =
  let result = 
    EnvConfig.Parse<SampleConfig> {EnvConfig.defaultParams with Prefix = "MYAPP"}
  let expected = [NotFound "MYAPP_PROCESS_NAME"; NotFound "MYAPP_PROCESS_ID"] |> Error
  test <@ expected = result @>