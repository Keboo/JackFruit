﻿namespace Generator

open Generator.Models
open Microsoft.CodeAnalysis
open System.Xml.Linq
open System.Linq
open System

type ItemReturn<'T> =
| NewValue of Value: 'T
| UsePreviousValue

module AppModelCommandDefHelpers =

    let DescriptionFromLookup mapOption (commandDef: CommandDef) =
        let key = commandDef.PathString
        match mapOption with 
        | None -> UsePreviousValue
        | Some map -> 
            let value = Map.tryFind key map
            match value with 
            | Some value -> NewValue (Some value)
            | None -> UsePreviousValue

    
    let DescFromXmlComment (method: IMethodSymbol) =
        let xmlString = method.GetDocumentationCommentXml()
        if String.IsNullOrEmpty xmlString  then
            UsePreviousValue
        else
            let xml = XDocument.Parse(xmlString)
            let summaries = 
                [ for node in xml.Elements() do
                    if node.Name = XName.Get("summary") then
                        node.ToString() ]
            match summaries with 
            | [] -> UsePreviousValue
            | head::_ -> if String.IsNullOrEmpty head then UsePreviousValue else NewValue (Some head)


    let DescFromAttribute (roslynSymbol: ISymbol) =
        let attributes = 
            [ for attr in roslynSymbol.GetAttributes() do
                if attr.AttributeClass.Name = "DescriptionAttribute" then
                    let arg = attr.ConstructorArguments.First()
                    arg.Value ]
        match attributes with 
        | [] -> UsePreviousValue
        | head:: _ -> NewValue (Some (head.ToString()))


    // Do we care? If we only use description to create Symbols, we do not care. 
    let DescriptionFromSymbol commandDef =
        let descriptionFromSymbol(symbol) =
            UsePreviousValue

        match commandDef.CommandDefUsage with 
        | SetHandlerMethod (_, _, symbol) -> descriptionFromSymbol symbol
        | _ -> UsePreviousValue


    let DescriptionFromXmlComment commandDef =
        match commandDef.CommandDefUsage with 
        | UserMethod (method, _) -> DescFromXmlComment method
        | SetHandlerMethod (method, _, _) -> DescFromXmlComment method
        | _ -> UsePreviousValue

    let DescriptionFromAttribute commandDef =
        match commandDef.CommandDefUsage with 
        | UserMethod (method, _) -> DescFromAttribute method
        | SetHandlerMethod (method, _, _) -> DescFromAttribute method
        | _ -> UsePreviousValue