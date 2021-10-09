﻿module Dummy

open System
open Generator.Language


type LanguageCSharp() =

    let staticOutput isStatic = 
        if isStatic then
            " static"
        else
            ""

    let getExpression Expression =
        ""

    let getParameters (parameters: Parameter list) = 
        let getDefault parameter =
            match parameter.Default with 
                | None -> ""
                | Some def -> " " + getExpression def

        let s = [ for param in parameters do
                    $"{param.Type} {param.Name}{getDefault param}"]
        String.Join("", s)

    let getArguments arguments = 
        let s = [ for arg in arguments do
                    $"{getExpression arg}"]
        String.Join("", s)

    let getOperator op =
        match op with 
        | Equals -> "=="
        | NotEquals -> "!="
        | GreaterThan -> ">"
        | LessThan -> "<"
        | GreaterThanOrEqualTo -> ">="
        | LessThanOrEqualTo -> "<="

    interface ILanguage with 

        member __.PrivateKeyword = "private"
        member __.PublicKeyword = "public"
        member __.InternalKeyword = "private"
        member __.StaticKeyword = "public"

        member __.Using(using) =
            $"using {using.Namespace}"
        member __.NamespaceOpen(nspace) = 
            [$"namespace {nspace.Name}"; "{"]
        member __.NamespaceClose(_) = 
            ["}"]
        member __.ClassOpen(cls) =
            [$"{cls.Scope}{staticOutput cls.IsStatic} {cls.Name}"; "{"]
        member __.ClassClose(_) =
            ["}"]
        member __.MethodOpen(method: Method) =
            [$"{method.Scope}{staticOutput method.IsStatic} {method.ReturnType} {method.Name}({getParameters method.Parameters})"; "{"]
        member __.MethodClose(_) =
            ["}"]
        member __.PropertyOpen(property: Property) =
            [$"{property.Scope}{staticOutput property.IsStatic} {property.Type} {property.Name}"; "{"]
        member __.PropertyClose(_) =
            ["}"]
        member __.GetOpen(_) =
            [$"get"; "{"]
        member __.GetClose(_) =
            ["}"]
        member __.SetOpen(_) =
            [$"set"; "{"]
        member __.SetClose(_) =
            ["}"]
        member __.IfOpen(ifInfo) =
            [$"if ({ifInfo.Condition})"; "{"]
        member __.IfClose(_) =
            ["}"]
        member __.ForEachOpen(forEach) =
            [$"for ({forEach.LoopVar} in {forEach.LoopOver})"; "{"]
        member __.ForEachClose(_) =
            ["}"]

        member __.Assignment(assignment) =
            $"{assignment.Item}.= {assignment.Value};"

        member __.Invocation(invocation) =
            $"{invocation.Instance}.{invocation.MethodName}({getArguments invocation.Arguments})"
        member __.Comparison(comparison) =
            $"{getExpression comparison.Left}.{getOperator comparison.Operator} {getExpression comparison.Right}"


