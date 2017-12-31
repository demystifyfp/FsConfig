module FsConfig.Tests

open NUnit.Framework
open EnvConfig

[<Test>]
let ``parsePrimitive not found use case`` () =
  let result = parsePrimitive<int> "NOT_EXIST"
  let expected : EnvVarParseResult<int> = NotFound "NOT_EXIST" |> Error
  Assert.AreEqual(expected,result)

type SampleConfig = {
  ProcessId : int
  ProcessName : string
}

[<Test>]
let ``parseRecord not found use case`` () =
  let result = parseRecord<SampleConfig>()
  printfn "%A" result
  Assert.IsNotNull(result)