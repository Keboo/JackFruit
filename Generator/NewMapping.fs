﻿module Generator.NewMapping

open Microsoft.CodeAnalysis
open Generator.Models

let CommandDefFromMethod model (info: AppModelCommandInfo) subCommands =

    let members = 
        match info.Method with
             | Some method -> 
                 [ for parameter in method.Parameters do
                     let usage = UserParameter parameter
                     MemberDef(parameter.Name, parameter.Type.ToDisplayString(), usage, true)]
             | None -> [] 

    let id = 
        match info.InfoCommandId with
        | Some i -> i
        | None -> 
            match info.Method with 
            | Some m -> m.Name
            | None -> "<unknown>"

    let usage = 
        match info.Method with 
        | Some m -> UserMethod (m, model)
        | None -> Arbitrary

    let commandDef = CommandDef(id, info.Path, None, usage, members, subCommands)
    commandDef.AddToPocket "Method" info.Method 
    commandDef.AddToPocket "SemanticModel" model
    commandDef

let CommandDefsFrom<'T> semanticModel (appModel: AppModel<'T>) (items: 'T list)  =

    let rec depthFirstCreate item  =
        let subCommands = 
            [ for child in (appModel.Children item) do
                yield depthFirstCreate child ]
        let info = appModel.Info semanticModel item
        let commandDef = CommandDefFromMethod semanticModel info subCommands
        //RunTransformers commandDef appModel
        commandDef

    [ for item in items do
        depthFirstCreate item ]