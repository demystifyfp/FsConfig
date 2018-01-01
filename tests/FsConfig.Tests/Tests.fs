module FsConfig.Tests

open NUnit.Framework
open EnvConfig
open Swensen.Unquote.Assertions

[<Test>]
let ``parsePrimitive not found use case`` () =
  let result = parsePrimitive<int> "NOT_EXIST"
  let expected : EnvVarParseResult<int> = NotFound "NOT_EXIST" |> Error
  test <@ expected = result  @>

type SampleConfig = {
  ProcessId : int
  ProcessName : string
}

[<Test>]
let ``parseRecord not found use case`` () =
  let result = parseRecord<SampleConfig>()
  let expected = [NotFound "PROCESS_NAME"; NotFound "PROCESS_ID"] |> Error
  test <@ expected = result @>