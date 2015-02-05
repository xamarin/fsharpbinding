﻿namespace MonoDevelopTests
open System
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Core
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open FSharp.CompilerBinding
open MonoDevelop.Projects
open MonoDevelop.Ide.TypeSystem
open FsUnit
open MonoDevelop.Debugger

[<TestFixture>]
type DebuggerExpressionResolver() =
    inherit TestBase()
    let mutable doc = Unchecked.defaultof<Document>

    let content = """type TestOne() =
    member val PropertyOne = "42" with get, set
    member x.FunctionOne(parameter) = ()

let localOne = TestOne()
let localTwo = localOne.PropertyOne"""

    let createDoc (text:string)=
        let workbenchWindow = TestWorkbenchWindow()
        let viewContent = new TestViewContent()

        let project = Services.ProjectService.CreateDotNetProject ("F#")
        project.Name <- "test"
        project.FileName <- FilePath("test.fsproj")
        let projectConfig = project.AddNewConfiguration("Debug")

        TypeSystemService.LoadProject (project) |> ignore

        viewContent.Project <- project

        workbenchWindow.SetViewContent(viewContent)
        viewContent.ContentName <- "/users/a.fs"
        viewContent.GetTextEditorData().Document.MimeType <- "text/x-fsharp"
        let doc = Document(workbenchWindow)

        (viewContent :> IEditableTextBuffer).Text <- text
        (viewContent:> IEditableTextBuffer).CursorPosition <- 0

        let pfile = doc.Project.AddFile("/users/a.fs")

        let textEditorCompletion = new FSharpTextEditorCompletion()
        textEditorCompletion.Initialize(doc)
        viewContent.Contents.Add(textEditorCompletion)

        try doc.UpdateParseDocument() |> ignore
        with exn -> Diagnostics.Debug.WriteLine(exn.ToString())
        doc

    let getBasicOffset expr =
        let startOffset = content.IndexOf (expr, StringComparison.Ordinal)
        startOffset + (expr.Length / 2)

    let resolveExpression (doc:Document, content:string, offset:int) =
        let debugResolver =
            doc.GetContents<obj>()
            |> Seq.cast<IDebuggerExpressionResolver> 
            |> Seq.tryHead
        
        match debugResolver with
        | Some resolver -> 
            let result, startoffset = resolver.ResolveExpression(doc.Editor, doc,offset)
            result, startoffset
        | None -> failwith "No debug resolver found"


    [<TestFixtureSetUp>]
    override x.Setup() =
        base.Setup()
        doc <- createDoc(content)

    
    [<Test>]
    [<TestCase("localOne")>]
    [<TestCase("localTwo")>]
    member x.TestBasicLocalVariable(localVariable) =
        let basicOffset = getBasicOffset (localVariable)
        let expression, offset = resolveExpression (doc, content, basicOffset)
        System.Console.WriteLine(offset)
        expression |> should equal localVariable

   

