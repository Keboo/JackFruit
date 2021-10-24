﻿module Generator.Tests.ArchetypeMappingTests


open Xunit
open FsUnit.Xunit
open FsUnit.CustomMatchers
open Jackfruit.ArchetypeMapping
open Generator.RoslynUtils
open Generator.GeneralUtils
open Jackfruit.Models
open Generator.Tests.UtilsForTests
open Microsoft.CodeAnalysis
open Generator.Tests.MapData
open Jackfruit.UtilsForTests


type ``When parsing archetypes``() =
    [<Fact>]
    member _.``Ancestors found for empty archetype``() =
        let actual = ParseArchetypeInfo "\"\"" None

        actual.Path |> should equal [""]

    [<Fact>]
    member _.``Ancestors found for simple archetype``() =
        let actual = ParseArchetypeInfo "\"first\"" None

        actual.Path |> should equal ["first"]

    [<Fact>]
    member _.``Ancestors found for multi-level archetype``() =
        let expectedCommands = [ "first"; "second"; "third"]
        let actual = ParseArchetypeInfo "\"first second third\"" None

        actual.Path |> should equal expectedCommands

    [<Fact>]
    member _.``ArgArchetype parsed from single element part`` () =
        let input = "<one>"
        let expected = 
            ArgArchetype 
                { Id = "one"
                  Name = "one"
                  Aliases = [] 
                  HiddenAliases = []}

        let actual = ArgArchetypeFrom input

        actual |> should equal expected

    [<Fact>]
    member _.``OptionArchetype parsed from single element part`` () =
        let input = "--one"
        let expected = 
           OptionArchetype
               { Id = "one"
                 Name = "one"
                 Aliases = []
                 HiddenAliases = [] }

        let actual = OptionArchetypeFrom input

        actual |> should equal expected

    [<Fact>]
    member _.``CommandArchetype parsed from single element part`` () =
        let input = "one"
        let expected = 
            CommandArchetype
                { Id = "one"
                  Name = "one"
                  Aliases = []
                  HiddenAliases = [] }

        let actual = CommandArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``ArgArchetype parsed from multi-element part`` () =
        let input = "<one|two>"
        let expected = 
            ArgArchetype 
                { Id = "one"
                  Name = "one"
                  Aliases = ["two"]
                  HiddenAliases = [] }

        let actual = ArgArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``OptionArchetype parsed from multi-element part`` () =
        let input = "--one|two"
        let expected = 
            OptionArchetype
                { Id = "one"
                  Name = "one"
                  Aliases = ["two"]
                  HiddenAliases = [] }

        let actual = OptionArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``CommandArchetype parsed from multi-element part`` () =
        let input = "one|two"
        let expected = 
            CommandArchetype
                { Id = "one"
                  Name = "one"
                  Aliases = ["two"]
                  HiddenAliases = [] }

        let actual = CommandArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``ArgArchetype parsed with hidden id`` () =
        let input = "<[one]|two>"
        let expected = 
            ArgArchetype 
                { Id = "one"
                  Name = "two"
                  Aliases = []
                  HiddenAliases = ["one"] }

        let actual = ArgArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``OptionArchetype parsed with hidden id`` () =
        let input = "--[one]|two"
        let expected = 
            OptionArchetype
                { Id = "one"
                  Name = "two"
                  Aliases = []
                  HiddenAliases = ["one"] }

        let actual = OptionArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``CommandArchetype parsed with hidden id`` () =
        let input = "[one]|two"
        let expected = 
            CommandArchetype
                { Id = "one"
                  Name = "two"
                  Aliases = []
                  HiddenAliases = ["one"] }

        let actual = CommandArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``ArgArchetype parsed with hidden alias`` () =
        let input = "<one|[two]>"
        let expected = 
            ArgArchetype 
                { Id = "one"
                  Name = "one"
                  Aliases = []
                  HiddenAliases = ["two"] }

        let actual = ArgArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``OptionArchetype parsed with hidden aliss`` () =
        let input = "--one|[two]"
        let expected = 
            OptionArchetype
                { Id = "one"
                  Name = "one"
                  Aliases = []
                  HiddenAliases = ["two"] }

        let actual = OptionArchetypeFrom input
        
        actual |> should equal expected

    [<Fact>]
    member _.``CommandArchetype parsed with hidden alias`` () =
        let input = "one|[two]"
        let expected = 
            CommandArchetype
                { Id = "one"
                  Name = "one"
                  Aliases = []
                  HiddenAliases = ["two"] }

        let actual = CommandArchetypeFrom input
        
        actual |> should equal expected


type ``When creating archetypeInfo from mapping``() =
    let CommandNamesFromSource source =
        let commandNames archInfoList =
            [ for archInfo in archInfoList do
                archInfo.Path |> List.last ]

        let result = 
            ModelFrom [(CSharpCode source); (CSharpCode HandlerSource)]
            |> Result.bind (InvocationsFromModel "MapInferred")
            |> Result.bind ArchetypeInfoListFrom 
            |> Result.map commandNames

        match result with 
        | Ok n -> n
        | Error err -> invalidOp $"Test failed with {err}"


    [<Fact>]
    member _.``None are found when there are none``() =
        let source = AddMapStatements false MapData.NoMapping.MapInferredStatements

        let actual = CommandNamesFromSource source

        actual |> should matchList MapData.NoMapping.CommandNames

    [<Fact>]
    member _.``One is found when there is one``() =
        let source = AddMapStatements false MapData.OneMapping.MapInferredStatements

        let actual = CommandNamesFromSource source

        actual |> should matchList MapData.OneMapping.CommandNames

    [<Fact>]
    member _.``Multiples are found when there are multiple``() =
        let source = AddMapStatements false MapData.ThreeMappings.MapInferredStatements

        let actual = CommandNamesFromSource source

        actual |> should matchList MapData.ThreeMappings.CommandNames


 //type ``When working with  handlers``() =

 //   [<Fact>]
 //   member _.``Handler name is found as method in separate class``() =
 //       let (archetypes, model) = archetypesAndModelFromSource MapData.OneMapping.MapInferredStatements
 //       let archetypeInfo = archetypes |> List.exactlyOne

 //       let actual = 
 //           match archetypeInfo.Handler with 
 //           | Some handler -> MethodFromHandler model handler
 //           | None -> invalidOp "Test failed because no handler found"

 //       actual |> should not' (be Null)

 //       actual.ToString() |> should haveSubstring "Handlers.A"



 //   [<Fact>]
 //   member _.``Tree is built with ArchetypeInfoTreeFrom``() =
 //       let source = AddMapStatements false MapData.ThreeMappings.MapInferredStatements
 //       let result = 
 //           SyntaxTreeResult (CSharpCode source)
 //           |> Result.bind (InvocationsFrom "MapInferred")
 //           |> Result.bind ArchetypeInfoListFrom
 //           |> Result.map ArchetypeInfoTreeFrom

 //       let actual = 
 //           match result with 
 //           | Ok tree -> tree
 //           | Error err -> invalidOp $"Failed to build tree {err}" // TODO: Work on error reporting

 //       actual[0].Data.Path |> should equal ["dotnet"]
 //       actual[0].Children[0].Data.Path |> should equal ["dotnet"; "add"]
 //       actual[0].Children[0].Children[0].Data.Path |> should equal ["dotnet";"add"; "package"]


 //   [<Fact>]
 //   member _.``CommandDef built from ArchetypeInfo with Handler``() =
 //       let (archetypes, model) = archetypesAndModelFromSource MapData.OneMapping.MapInferredStatements
 //       let archetypeInfo = archetypes |> List.exactlyOne

 //       let actual = 
 //           match archetypeInfo.Handler with 
 //           | Some handler -> MethodFromHandler model handler
 //           | None -> invalidOp "Test failed because no handler found"

 //       actual |> should not' (be Null)

 //       actual.ToString()
 //       |> should haveSubstring "Handlers.A"

    //[<Fact>]
    //member _.``Option and Argument types are updated on command``() =
    //    let (archetypes, model) = archetypesAndModelFromSource oneMapping
    //    let archetypeInfo = archetypes |> List.exactlyOne 
    //    let methodSymbolResult = MethodFromHandler model archetypeInfo.HandlerExpression

    //    let actual = 
    //        match methodSymbolResult with 
    //        | Some methodSymbol -> copyUpdateArchetypeInfoFromSymbol archetypeInfo methodSymbol
    //        | None -> invalidOp "Method symbol not found during arrange"

    //    actual.Archetype.Arg |> shouldBeNone
    //    actual.Archetype.Options |> should haveLength 1
    //    let firstOption = actual.Archetype.Options.[0]
    //    firstOption |> should equal {OptionName="one"; TypeName=Some "string"}
