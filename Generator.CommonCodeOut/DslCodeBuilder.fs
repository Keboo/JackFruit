﻿module DslCodeBuilder


open System
open DslKeywords
open Generator.Language
open Generator.LanguageStatements
open Generator.LanguageExpressions
open Common
open Generator.LanguageExpressions.ExpressionHelpers


[<AbstractClass>]
type BuilderBase<'T>() =
 
    abstract member EmptyItem: unit -> 'T
    abstract member InternalCombine: 'T -> 'T -> 'T

    member this.Yield (()) : 'T = this.EmptyItem()
    member this.Zero() : 'T = this.Yield(())
    member this.Combine (method: 'T, method2: 'T) : 'T=
        this.InternalCombine method method2
    member _.Delay(f: unit -> 'T) : 'T = f()
    member this.For(methods, f) :'T = 
        let methodList = Seq.toList methods
        match methodList with 
        | [] -> this.Zero()
        | [x] -> f(x)
        | head::tail ->
            let mutable headResult = f(head)
            for x in tail do 
                headResult <- this.Combine(headResult, f(x))
            headResult

            
[<AbstractClass>]
type BuilderBase2<'M, 'T>() =
             
    abstract member EmptyItem: unit -> 'M
    abstract member InternalCombine: 'M -> 'M -> 'M
            
    member this.Zero() : 'M = this.EmptyItem()
    member this.Combine (item: 'M, item2: 'M) : 'M=
        this.InternalCombine item item2
    member _.Delay(f) : 'M = f()
    member this.For(methods, f) :'M = 
        let methodList = Seq.toList methods
        match methodList with 
        | [] -> this.Zero()
        | [x] -> f(x)
        | head::tail ->
            let mutable headResult = f(head)
            for x in tail do 
                headResult <- this.Combine(headResult, f(x))
            headResult

[<AbstractClass>]
type StatementBuilderBase<'T when 'T :> IStatementContainer<'T>>() =
    inherit BuilderBase<'T>()

    member this.Yield (statement: IStatement) : 'T = 
        this.Zero().AddStatements [ statement ]

    // KAD-Chet: I do notunderstand how adding the statements here and in combine don't double them
    [<CustomOperation("Return", MaintainsVariableSpace = true)>]
    member _.addReturn (method: 'T) =
        method.AddStatements [ {ReturnModel.Expression = None} ] 

    [<CustomOperation("Return", MaintainsVariableSpace = true)>]
    member _.addReturn (method: 'T, expression: obj) =
        let expr: IExpression = 
            match expression with 
            | :? IExpression as x -> x
            | :? string as x -> StringLiteralModel.Create x
            | _ -> OtherLiteralModel.Create (expression.ToString())

        method.AddStatements [ {ReturnModel.Expression = Some expr} ] 

    [<CustomOperation("SimpleCall", MaintainsVariableSpace = true)>]
    member _.addSimpleCall (method: 'T, expression: obj) =
        let expr: IExpression = 
            match expression with 
            | :? IExpression as x -> x
            | :? string as x -> StringLiteralModel.Create x
            | _ -> OtherLiteralModel.Create (expression.ToString())

        method.AddStatements [ {SimpleCallModel.Expression = expr} ] 
 
    [<CustomOperation("Invoke", MaintainsVariableSpace = true)>]
    member _.addInvoke (method: 'T, instance: NamedItem, methodToCall: NamedItem, [<ParamArray>] args: IExpression[]) =
        let expr = InvokeExpression instance methodToCall (List.ofArray args)
        method.AddStatements [ {SimpleCallModel.Expression = expr} ] 

    [<CustomOperation("Invoke", MaintainsVariableSpace = true)>]
    member _.addInvoke (method: 'T, instance: string, methodToCall: string, [<ParamArray>] args: IExpression[]) =
        let expr = InvokeExpression instance methodToCall (List.ofArray args)
        method.AddStatements [ {SimpleCallModel.Expression = expr} ] 

    //[<CustomOperation("If", MaintainsVariableSpace = true)>]
    //member _.addIf (method: 'T, condition: ICompareExpression, statements: IStatement list) =
    //    method.AddStatements [ IfModel.Create condition statements ] 

type If(condition: ICompareExpression) =
    inherit StatementBuilderBase<IfModel>()

    override _.EmptyItem() : IfModel =  IfModel.Create condition []
    override _.InternalCombine if1 if2 : IfModel =
        if1.AddStatements if2.Statements

type ElseIf(condition: ICompareExpression) =
    inherit StatementBuilderBase<ElseIfModel>()

    override _.EmptyItem() : ElseIfModel =  ElseIfModel.Create condition []
    override _.InternalCombine if1 if2 : ElseIfModel =
        if1.AddStatements if2.Statements

type Else() =
    inherit StatementBuilderBase<ElseModel>()

    override _.EmptyItem() : ElseModel =  ElseModel.Create []
    override _.InternalCombine if1 if2 : ElseModel =
        if1.AddStatements if2.ElseStatements



type Namespace(name: string) =
    inherit BuilderBase2<NamespaceModel, ClassModel>()

    override _.EmptyItem() : NamespaceModel =  NamespaceModel.Create name
    override _.InternalCombine nspace nspace2 : NamespaceModel =
        { nspace with 
            Usings =  List.append nspace.Usings nspace2.Usings
            Classes = List.append nspace.Classes nspace2.Classes }  

    member this.Yield () : NamespaceModel = this.Zero()
    member this.Yield (_: unit) : NamespaceModel = this.Zero()
    member this.Yield (usingModel: UsingModel) : NamespaceModel = 
        this.Zero().AddUsings [usingModel]
    member this.Yield (classModel: ClassModel) : NamespaceModel = 
        this.Zero().AddClasses [classModel]
    member this.Return(classModel) = this.Zero().AddUsings classModel
  

type Class(name: string) =
    inherit BuilderBase<ClassModel>()

    let updateModifiers (cls: ClassModel) scope (modifiers: Modifier[]) =
        { cls with 
            Scope = scope; 
            Modifiers = List.ofArray modifiers
       }

    override _.EmptyItem() =  ClassModel.Create name
    override _.InternalCombine cls cls2 =
        { cls with Members =  List.append cls.Members cls2.Members }  
    member this.Yield (memberModel: IMember) : ClassModel = 
        { this.Zero() with Members = [ memberModel ] }

    //member _.Quote() = ()

    [<CustomOperation("Public", MaintainsVariableSpace = true)>]
    member _.publicWithModifiers (cls: ClassModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers cls Public modifiers

    [<CustomOperation("Private", MaintainsVariableSpace = true)>]
    member _.privateWithModifiers (cls: ClassModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers cls Private modifiers

    [<CustomOperation("Internal", MaintainsVariableSpace = true)>]
    member _.internalWithModifiers (cls: ClassModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers cls Internal modifiers

    [<CustomOperation("Protected", MaintainsVariableSpace = true)>]
    member _.protectedWithModifiers (cls: ClassModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers cls Protected modifiers

    // TODO: Passing a named item will be quite messy. This problemw will recur. Harder if we can't get overloads
    [<CustomOperation("InheritedFrom", MaintainsVariableSpace = true)>]
    member _.inheritedFrom (cls: ClassModel, inheritedFrom: NamedItem) =
        // Consider whether resetting this to None is a valid scenario
        { cls with InheritedFrom = Some inheritedFrom }

    // TODO: Passing a named item will be quite messy. This problemw will recur. Harder if we can't get overloads
    [<CustomOperation("ImplementedInterfaces", MaintainsVariableSpace = true)>]
    member _.interfaces (cls: ClassModel, [<ParamArray>]interfaces: NamedItem list) =
        { cls with ImplementedInterfaces = interfaces }

    [<CustomOperation("Generics", MaintainsVariableSpace = true)>]
    member _.generics (cls: ClassModel, generics: NamedItem list) =
        let currentName =
            match cls.ClassName with 
            | SimpleNamedItem n -> n
            | GenericNamedItem (n, _ ) -> n
        { cls with ClassName = NamedItem.Create currentName generics }

    [<CustomOperation("Members", MaintainsVariableSpace = true)>]
    member _.addMember (cls: ClassModel, memberModels: IMember list) =
        { cls with Members =  List.append cls.Members memberModels }
 

// KAD-Don: I have not been able to create a CE that supports an empty body. Is it possible?
//          I want to allow: 
//            Field(n, t) { }
//            Field(n, t) { Public Static }
type Field(name: string, typeName: NamedItem) =
    inherit BuilderBase<FieldModel>()

    let updateModifiers (field: FieldModel) scope (modifiers: Modifier[])  =
        { field with 
            Scope = scope
            Modifiers = List.ofArray modifiers }
        
    override _.EmptyItem() =  FieldModel.Create name typeName
    // KAD-Chet: This is goofy
    override _.InternalCombine cls cls2 = cls

    [<CustomOperation("Public", MaintainsVariableSpace = true)>]
    member _.publicWithModifiers (field: FieldModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers field Public modifiers

    [<CustomOperation("Private", MaintainsVariableSpace = true)>]
    member _.privateWithModifiers (field: FieldModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers field Private modifiers

    [<CustomOperation("Internal", MaintainsVariableSpace = true)>]
    member _.internalWithModifiers (field: FieldModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers field Internal modifiers

    [<CustomOperation("Protected", MaintainsVariableSpace = true)>]
    member _.protectedWithModifiers (field: FieldModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers field Protected modifiers


type Method(name: NamedItem, returnType: ReturnType) =
    inherit StatementBuilderBase<MethodModel>()

    let updateModifiers (method: MethodModel) scope (modifiers: Modifier[]) =
        { method with 
            Scope = scope
            Modifiers = List.ofArray modifiers }        
 
    override _.EmptyItem (): MethodModel =  MethodModel.Create name returnType
    override _.InternalCombine (method: MethodModel) (method2: MethodModel) =
        { method with Statements =  List.append method.Statements method2.Statements }

    [<CustomOperation("Public", MaintainsVariableSpace = true)>]
    member _.publicWithModifiers (method: MethodModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers method Public modifiers

    [<CustomOperation("Private", MaintainsVariableSpace = true)>]
    member _.privateWithModifiers (method: MethodModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers method Private modifiers

    [<CustomOperation("Internal", MaintainsVariableSpace = true)>]
    member _.internalWithModifiers (method: MethodModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers method Internal modifiers

    [<CustomOperation("Protected", MaintainsVariableSpace = true)>]
    member _.protectedWithModifiers (method: MethodModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers method Protected modifiers


type Property(name: string, typeName: NamedItem) =

    let updateModifiers (property: PropertyModel) scope (modifiers: Modifier[]) =
        { property with 
            Scope = scope
            Modifiers = List.ofArray modifiers }        
        
    member _.Yield (_) = PropertyModel.Create name typeName
    member this.Zero() = this.Yield()

    [<CustomOperation("Public", MaintainsVariableSpace = true)>]
    member _.modifiers (property: PropertyModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers property Public modifiers


    [<CustomOperation("Private", MaintainsVariableSpace = true)>]
    member _.privateWithModifiers (property: PropertyModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers property Public modifiers

    [<CustomOperation("Internal", MaintainsVariableSpace = true)>]
    member _.internalWithModifiers (property: PropertyModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers property Public modifiers

    [<CustomOperation("Protected", MaintainsVariableSpace = true)>]
    member _.protectedWithModifiers (property: PropertyModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers property Public modifiers


type Constructor(className: string) =
    let updateModifiers (ctor: ConstructorModel) scope (modifiers: Modifier[]) =
        { ctor with 
            Scope = scope
            Modifiers = List.ofArray modifiers }        
  
        
    member _.Yield (_) = ConstructorModel.Create className
    member this.Zero() = this.Yield()

    [<CustomOperation("Public", MaintainsVariableSpace = true)>]
    member _.publicWithModifiers (ctor: ConstructorModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers ctor Public modifiers

    [<CustomOperation("Private", MaintainsVariableSpace = true)>]
    member _.privateWithModifiers (ctor: ConstructorModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers ctor Public modifiers

    [<CustomOperation("Internal", MaintainsVariableSpace = true)>]
    member _.internalWithModifiers (ctor: ConstructorModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers ctor Public modifiers

    [<CustomOperation("Protected", MaintainsVariableSpace = true)>]
    member _.protectedWithModifiers (ctor: ConstructorModel, [<ParamArray>] modifiers: Modifier[]) =
        updateModifiers ctor Public modifiers



