﻿module Generator.OutputCode

open Language
open Models

let private OutputHeader (outputter: RoslynOut) =
    outputter.OutputComment(Comment "Copyright (c) .NET Foundation and contributors. All rights reserved.")

    outputter.OutputComment(
        Comment "Licensed under the MIT license. See LICENSE file in the project root for full license information."
    )

    outputter.BlankLine()

    outputter.OutputPragma(Pragma "warning disable")
    ()

let private CommonMembers pos commandDefs : Member list =
    let mutable i = pos
    // TODO: We need to get a distinct on the parameters as these are shared later
    [ for commandDef in commandDefs do
          let delegateParameter =
              Parameter.Create "Temp" (GenericNamedItem.Create "Temp")

          let parameters =
              [ Parameter.Create "command" (GenericNamedItem.Create "Command")
                delegateParameter
                match commandDef.Arg with
                | Some arg ->
                    Parameter.Create
                        arg.Name
                        { Name = "Argument"
                          GenericTypes =
                            [ { Name = arg.TypeName
                                GenericTypes = [] } ] }
                | None -> ()
                //for opt in options do

                ]
          // TODO: Figure out the scenario where there are multiple generic types
          Method
              { MethodName =
                  { Name = "SetHandler"
                    GenericTypes = [ GenericNamedItem.Create "T1" ] }
                ReturnType = None
                IsStatic = true
                IsExtension = true
                Scope = Public
                Parameters = parameters
                Statements = [] }

          Class
              { ClassName =
                  { Name = "GeneratedHandler"
                    GenericTypes = [] }
                IsStatic = false
                Scope = Private
                Members = [] } ]

let private TypeForSymbol symbolName typeName =
    { Name = symbolName 
      GenericTypes = [ { Name = typeName; GenericTypes = []} ]}
    


let private CommandCreateStatements (commandDef: CommandDef) : Statement list =
    let Argument (arg: ArgDef) =
        let name = $"{arg.ArgId}Argument"
        let args = [ StringLiteral arg.Name ] // Add other fields
        name, args
        
    let Option (option: OptionDef) =
        let name = $"{option.OptionId}Option"
        let args = [ StringLiteral option.Name ] // Add Other fields
        name, args

    let ArgumentsForCommand =
        [ StringLiteral commandDef.Name ] // Other fields

    // This needs to be done as we go because it's essential the order be the same
    let mutable handlerTypes = []
    let addHandlerType typeName =
        handlerTypes <- handlerTypes 
        |> List.insertAt 
            handlerTypes.Length { Name = typeName; GenericTypes = []}
 
    [
        AssignVar "command" (New "Command" ArgumentsForCommand)

        match commandDef.Arg with 
        | Some arg -> 
            let name, args = Argument arg
            AssignVar name (NewGeneric "Argument" arg.TypeName args)
            SimpleCall (Invoke "command" "AddSymbol" [(Symbol name)])
            addHandlerType arg.TypeName
            
        | None -> ()

        for option in commandDef.Options do
            let name, args = Option option
            AssignVar name (NewGeneric "Option" option.TypeName args)
            SimpleCall (Invoke "command" "AddSymbol" [(Symbol name)])
            addHandlerType option.TypeName
    ]
    

let private CommandMembers commandDefs : Member list = 
    [ for (commandDef: CommandDef) in commandDefs do
        // TODO: Change to path and camel
        Method
            { MethodName = GenericNamedItem.Create $"{commandDef.CommandId}Symbols"
              ReturnType = Some { Name = "Command"; GenericTypes=[] } 
              IsStatic = true
              IsExtension = false
              Scope = Public
              Parameters = []
              Statements = CommandCreateStatements commandDef } ]


let private OutputCommandCode commandDefs =
    { ClassName = GenericNamedItem.Create "GeneratedCommandHandlers"
      IsStatic = true
      Scope = Internal
      Members = CommandMembers commandDefs }


let NamespaceFrom commandDefs includeCommandCode =
    let commonCode =
        // TODO: There are some important attributes to add
        { ClassName = GenericNamedItem.Create "GeneratedCommandHandlers"
          IsStatic = true
          Scope = Internal
          Members = CommonMembers 1 commandDefs }

    { NamespaceName = "System.CommandLine"
      Usings =
        [ Using.Create "System.ComponentModel"
          Using.Create "System.CommandLine.Binding"
          Using.Create "System.Reflection"
          Using.Create "System.Threading.Tasks"
          Using.Create "System.CommandLine.Invocation;" ]
      Classes =
        [ if includeCommandCode then
              OutputCommandCode commandDefs
          commonCode ] }


let CodeFrom includeCommandCode (language: ILanguage) writer commandDefs =
    let outputter = RoslynOut(LanguageCSharp(), writer)

    let nspace =
        NamespaceFrom commandDefs includeCommandCode

    OutputHeader outputter
    outputter.Output nspace



    writer.Output
