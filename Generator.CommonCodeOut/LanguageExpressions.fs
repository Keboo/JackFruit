﻿module Generator.LanguageExpressions

open Generator
open Common
open Generator.Language
open System


type InvocationModel =
    { Instance: NamedItem // Named item for invoking static methods on generic types
      MethodName: NamedItem // For generic methods
      ShouldAwait: bool
      Arguments: IExpression list}
    interface IExpression
    static member Create instance methodName arguments =
        { Instance = instance // Named item for invoking static methods on generic types
          MethodName = methodName // For generic methods
          ShouldAwait = false
          Arguments = arguments }

type InstantiationModel =
    { TypeName: NamedItem
      Arguments: IExpression list}
    interface IExpression
    static member Create typeName arguments =
        { TypeName = typeName
          Arguments = arguments}

type ComparisonModel =
    { Left: IExpression
      Right: IExpression
      Operator: Operator}
    interface IExpression
    static member Create left right operator =
        { Left = left
          Right = right
          Operator = operator }
    interface ICompareExpression


type StringLiteralModel =
    { Value: string}
    interface IExpression
    static member Create value =
        { Value = value }

type OtherLiteralModel =
    { Value: string}
    interface IExpression
    static member Create value =
        { Value = value }

type BoolLiteralModel =
    { Value: bool}
    interface IExpression
    static member Create value =
        { Value = value }
    interface ICompareExpression

type SymbolModel =
    { Name: string}
    interface IExpression
    static member Create name =
        { Name = name }

// KAD-Chet: This seems pretty dumb, but I need something that implements the interface.
type NullModel =
    { Dummy: string} // not sure how to manage this
    interface IExpression
    static member Create() = { Dummy = "" }

module ExpressionHelpers =
    // KAD-Chet: Do you see another way to differentiate this from the statement Invoke? 
    //           I wonder which is more common in C#. If we had Invoke and Invocation, which 
    //           would be the statement and which the expression?
    let InvokeExpression
        (instance: NamedItem)
        (methodToCall: NamedItem)
        (args: IExpression list) =

        InvocationModel.Create instance methodToCall args

    let New
        (typeName: NamedItem)
        (args: IExpression list) =

        InstantiationModel.Create typeName  args

    let Compare
        (left: IExpression)
        (operator: Operator)
        (right: IExpression) =
        
        ComparisonModel.Create left right operator

    let Literal (value: obj) : IExpression=

        match value with 
        | :? string as s -> StringLiteralModel.Create s
        | :? bool as b -> BoolLiteralModel.Create b
        | _ -> OtherLiteralModel.Create (value.ToString())

    let Symbol
        (name: string) =

        SymbolModel.Create name

    let Null = NullModel.Create()

    let True = BoolLiteralModel.Create(true)

    let False = BoolLiteralModel.Create(true)
