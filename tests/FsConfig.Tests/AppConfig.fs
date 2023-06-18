namespace AppConfig.Tests

open System
open NUnit.Framework
open FsConfig
open Swensen.Unquote.Assertions
open Microsoft.Extensions.Configuration
open System.IO
open FsConfig.Tests.Common

module ``App Config tests`` =
    type DuListConfig = {
        [<CustomName("colors")>]
        DuColors: DuColor list
    }

    [<TestFixture>]
    type ``JSON Configuration tests``() =

        let appConfig =
            let b = new ConfigurationBuilder()

            b
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("settings.json")
                .Build()
            |> AppConfig

        [<Test>]
        member __.``getPrimitive should succeeds``() =
            test <@ appConfig.Get<int> "processId" = Ok 123 @>

        [<Test>]
        member __.``getUri should succeed``() =
            test <@ appConfig.Get<ConfigWithUri>() = Ok { Uri = Uri("https://example.com") } @>

        [<Test>]
        member __.``getUri should return error with invalid URIs``() =
            test
                <@ appConfig.Get<ConfigWithInvalidUri>() = Error(BadValue("InvalidUri", "invalid")) @>

        [<Test>]
        member __.``getRecord should succeeds``() =
            test
                <@
                    appConfig.Get<SampleConfig>() = Ok
                                                        {
                                                            ProcessId = 123
                                                            ProcessName = "FsConfig"
                                                        }
                @>


        [<Test>]
        member __.``getNestedRecord should succeeds``() =
            let expected =
                {
                    Config.MagicNumber = 42
                    Aws =
                        {
                            AccessKeyId = "Id-123"
                            DefaultRegion = "us-east-1"
                            SecretAccessKey = "secret123"
                        }
                }
                |> Ok

            test <@ appConfig.Get<Config>() = expected @>

        [<Test>]
        member __.``getOptionalRecord should succeeds``() =
            let expected =
                {
                    listOfRecords =
                        [
                            {
                                optionalSubsectionNone = None
                                optionalSubsectionSome = Some({ apiKey = "abc-def"; value = 1 })

                            }
                        ]
                }
                |> Ok

            test <@ appConfig.Get<ConfigWithListOfRecords>() = expected @>

        [<Test>]
        member __.``get list of DU with custom name``() =
            test
                <@
                    appConfig.Get<DuListConfig>() = Ok
                                                        {
                                                            DuColors =
                                                                [
                                                                    Red
                                                                    Green
                                                                ]
                                                        }
                @>


    [<TestFixture>]
    type ``XML Configuration tests``() =

        let appConfig =
            let b = new ConfigurationBuilder()

            b
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddXmlFile("settings.xml")
                .Build()
            |> AppConfig

        [<Test>]
        member __.``getPrimitive should succeeds``() =
            test <@ appConfig.Get<int> "processId" = Ok 123 @>

        [<Test>]
        member __.``getUri should succeed``() =
            test <@ appConfig.Get<ConfigWithUri>() = Ok { Uri = Uri("https://example.com") } @>

        [<Test>]
        member __.``getUri should return error with invalid URIs``() =
            test
                <@ appConfig.Get<ConfigWithInvalidUri>() = Error(BadValue("InvalidUri", "invalid")) @>

        [<Test>]
        member __.``getRecord should succeeds``() =
            test
                <@
                    appConfig.Get<SampleConfig>() = Ok
                                                        {
                                                            ProcessId = 123
                                                            ProcessName = "FsConfig"
                                                        }
                @>

        [<Test>]
        member __.``get list of DU with custom name``() =
            test
                <@
                    appConfig.Get<DuListConfig>() = Ok
                                                        {
                                                            DuColors =
                                                                [
                                                                    Red
                                                                    Green
                                                                ]
                                                        }
                @>


        [<Test>]
        member __.``getNestedRecord should succeeds``() =
            let expected =
                {
                    Config.MagicNumber = 42
                    Aws =
                        {
                            AccessKeyId = "Id-123"
                            DefaultRegion = "us-east-1"
                            SecretAccessKey = "secret123"
                        }
                }
                |> Ok

            test <@ appConfig.Get<Config>() = expected @>

    [<TestFixture>]
    type ``Ini Configuration tests``() =

        let appConfig =
            let b = new ConfigurationBuilder()

            b
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddIniFile("settings.ini")
                .Build()
            |> AppConfig

        [<Test>]
        member __.``getPrimitive should succeeds``() =
            test <@ appConfig.Get<int> "processId" = Ok 123 @>

        [<Test>]
        member __.``getRecord should succeeds``() =
            test
                <@
                    appConfig.Get<SampleConfig>() = Ok
                                                        {
                                                            ProcessId = 123
                                                            ProcessName = "FsConfig"
                                                        }
                @>

        [<Test>]
        member __.``getUri should succeed``() =
            test <@ appConfig.Get<ConfigWithUri>() = Ok { Uri = Uri("https://example.com") } @>

        [<Test>]
        member __.``getUri should return error with invalid URIs``() =
            test
                <@ appConfig.Get<ConfigWithInvalidUri>() = Error(BadValue("InvalidUri", "invalid")) @>

        [<Test>]
        member __.``get list of DU with custom name``() =
            test
                <@
                    appConfig.Get<DuListConfig>() = Ok
                                                        {
                                                            DuColors =
                                                                [
                                                                    Red
                                                                    Green
                                                                ]
                                                        }
                @>


        [<Test>]
        member __.``getNestedRecord should succeeds``() =
            let expected =
                {
                    Config.MagicNumber = 42
                    Aws =
                        {
                            AccessKeyId = "Id-123"
                            DefaultRegion = "us-east-1"
                            SecretAccessKey = "secret123"
                        }
                }
                |> Ok

            test <@ appConfig.Get<Config>() = expected @>
