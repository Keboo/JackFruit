﻿namespace Generator

open System
open Generator.Language
open Generator.CSharpLanguageExtensions


type LanguageCSharp() =

    let staticOutput isStatic = 
        if isStatic then
            " static"
        else
            ""

    //let getParameters (parameters: Parameter list) = 
    //    let getDefault parameter =
    //        match parameter.Default with 
    //            | None -> ""
    //            | Some def -> " " + getExpression def

    //    let s = [ for param in parameters do
    //                $"{param.Type} {param.Name}{getDefault param}"]
    //    String.Join("", s)

    //let getArguments arguments = 
    //    let s = [ for arg in arguments do
    //                $"{getExpression arg}"]
    //    String.Join("", s)

    interface ILanguage with 

        member _.PrivateKeyword = "private"
        member _.PublicKeyword = "public"
        member _.InternalKeyword = "private"
        member _.StaticKeyword = "public"

        member _.Using using =
            let alias = 
                match using.Alias with 
                | Some a -> $"{a} = "
                | None -> ""
            [ $"using {alias}{using.Namespace};" ]

        member _.NamespaceOpen nspace = 
            [$"namespace {nspace.NamespaceName}"; "{"]
        member _.NamespaceClose _ = 
            ["}"]

        member _.ClassOpen cls =
            [$"{cls.Scope.Output}{staticOutput cls.IsStatic} class {cls.ClassName.Output}"; "{"]
        member _.ClassClose _ =
            ["}"]

        member _.MethodOpen(method: Method) =
            let returnType =
                match method.ReturnType with 
                | Some t -> t.Output
                | None -> ""
            [$"{method.Scope.Output}{staticOutput method.IsStatic} {returnType} {method.MethodName.Output}({OutputParameters method.Parameters})"; "{"]
        member _.MethodClose _ =
            ["}"]

        member _.AutoProperty(property: Property) =
            [$"{property.Scope.Output}{staticOutput property.IsStatic} {property.Type.Output} {property.PropertyName} {{get; set;}}"]
        member _.PropertyOpen(property: Property) =
            [$"{property.Scope.Output}{staticOutput property.IsStatic} {property.Type.Output} {property.PropertyName}"; "{"]
        member _.PropertyClose _ =
            ["}"]
        member _.GetOpen _ =
            [$"get"; "{"]
        member _.GetClose _ =
            ["}"]
        member _.SetOpen _ =
            [$"set"; "{"]
        member _.SetClose _ =
            ["}"]

        member _.IfOpen ifInfo =
            [$"if ({ifInfo.Condition.Output})"; "{"]
        member _.IfClose _ =
            ["}"]

        member _.ForEachOpen forEach =
            [$"foreach (var {forEach.LoopVar} in {forEach.LoopOver})"; "{"]
        member _.ForEachClose _ =
            ["}"]

        member _.Assignment assignment =
            [$"{assignment.Item} = {assignment.Value.Output};"]
        member _.AssignWithDeclare assign =
            let t = 
                match assign.TypeName with 
                | Some n -> n.Output
                | None -> "var"
            [$"{t} {assign.Variable} = {assign.Value.Output};"]
        member _.Return ret =
            [$"return {ret.Output};"]
        member _.SimpleCall simple =
            [$"{simple.Output};"]
        member _.Comment comment =
            [$"{comment.Output}"]
        member _.Pragma pragma =
            [$"{pragma.Output}"]


        member _.Invocation invocation =
            $"{invocation.Instance}.{invocation.MethodName}({OutputArguments invocation.Arguments})"
        member _.Comparison comparison =
            $"{comparison.Left.Output}.{comparison.Operator.Output} {comparison.Right.Output}"


