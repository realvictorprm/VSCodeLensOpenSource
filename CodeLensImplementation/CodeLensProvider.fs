// Copyright (c) 2017 Victor Peter Rouven Müller and Microsoft (Visual F#, MIT License)
// Licensed under the Apache
namespace rec CodeLens

open System
open System.Windows.Media
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Formatting
open System.ComponentModel.Composition
open System.Threading
open Microsoft.VisualStudio
open System.Windows
open System.Collections.Generic
open Microsoft.VisualStudio.Text.Tagging
open System.Collections.Concurrent
open System.Collections
open System.Globalization
open System.Windows.Controls
open Microsoft.VisualStudio.Utilities
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.TextManager.Interop
open System.Windows.Media.Animation

type SampleTagger (view, buffer) as self =
    inherit CodeLensGeneralTagger(view, buffer)

    let collectLinesWhichContain (snapshot:ITextSnapshot) word =
        snapshot.Lines
        |> Seq.filter(fun line -> line.GetText().Contains word)
        |> Seq.toArray

        
    do (
            (* Single code lens test
            buffer.Insert(0, "Foo") |> ignore
            let snapshot = buffer.CurrentSnapshot
            let trackingSpan = snapshot.CreateTrackingSpan(1, 3, SpanTrackingMode.EdgeInclusive)
            let uiElement = self.AddCodeLens trackingSpan

            let textBox = new TextBox(Text = "Hello", Width = 100., Height = 20.)

            let animation = 
                DoubleAnimation(
                    To = Nullable 1.,
                    Duration = (TimeSpan.FromMilliseconds 500. |> Duration.op_Implicit),
                    EasingFunction = QuadraticEase()
                    )
            let sb = Storyboard()
            sb.Children.Add animation
            textBox.Opacity <- 0.
            Storyboard.SetTarget(sb, textBox)
            Storyboard.SetTargetProperty(sb, PropertyPath Control.OpacityProperty)
            uiElement.IsVisibleChanged
            |> Event.filter (fun eventArgs -> eventArgs.NewValue :?> bool)
            |> Event.add (fun _ ->
                    sb.Begin()
                )
            self.AddUiElementToCodeLensOnce trackingSpan textBox 
            *)

            /// Responsive code lens test
            buffer.Changed.Add(fun event ->
                let snapshot = event.After
                let linesToTag = collectLinesWhichContain snapshot "TODO"
                let lineNumbersToTag = 
                    linesToTag 
                    |> Array.map(fun line -> line.LineNumber) 
                    |> HashSet

                let currentCodeLens = self.CurrentCodeLens

                let codeLensToContinue, codeLensToRemove = 
                    currentCodeLens
                    |> Seq.toArray
                    |> Array.partition(fun pair -> pair.Key |> lineNumbersToTag.Contains)
                
                let codeLensToAdd =
                    let continuedLineNumbers = 
                        codeLensToContinue 
                        |> Seq.map (fun pair -> pair.Key)
                        |> HashSet
                    lineNumbersToTag
                    |> Seq.toArray
                    |> Array.filter (continuedLineNumbers.Contains >> not)
                
                for codeLens in codeLensToRemove do
                    let trackingSpans = codeLens.Value |> Seq.toArray
                    for trackingSpan in trackingSpans do
                        self.RemoveCodeLens trackingSpan |> ignore

                for lineNumber in codeLensToAdd do
                    let line = snapshot.GetLineFromLineNumber lineNumber
                    let trackingSpan = snapshot.CreateTrackingSpan(Span(line.Start.Position, line.Length), SpanTrackingMode.EdgeExclusive)
                    let uiElement = self.AddCodeLens trackingSpan
                    
                    let textBox = new TextBlock(Text = "Hello", Width = 100., Height = 20.)

                    let animation = 
                        DoubleAnimation(
                            To = Nullable 1.,
                            Duration = (TimeSpan.FromMilliseconds 500. |> Duration.op_Implicit),
                            EasingFunction = QuadraticEase()
                            )
                    let sb = Storyboard()
                    sb.Children.Add animation
                    textBox.Opacity <- 0.
                    Storyboard.SetTarget(sb, textBox)
                    Storyboard.SetTargetProperty(sb, PropertyPath Control.OpacityProperty)
                    uiElement.IsVisibleChanged
                    |> Event.filter (fun eventArgs -> eventArgs.NewValue :?> bool)
                    |> Event.add (fun _ ->
                            sb.Begin()
                        )
                    self.AddUiElementToCodeLensOnce trackingSpan textBox
                    //uiElement.IsVisibleChanged
                    //|> Event.filter (fun eventArgs -> eventArgs.NewValue :?> bool)
                    //|> Event.add (fun _ ->
                    //    let textBox = new TextBox(Text = "Hello", Width = 100., Height = 20.)
                    //    self.AddUiElementToCodeLensOnce trackingSpan textBox
                    //)
            )
        )

[<Export(typeof<IViewTaggerProvider>)>]
[<TagType(typeof<CodeLensGeneralTag>)>]
[<ContentType("text")>]
[<TextViewRole(PredefinedTextViewRoles.Document)>]
type internal CodeLensProvider  
    [<ImportingConstructor>]
    () =

    [<Export(typeof<AdornmentLayerDefinition>); Name("CodeLens");
      Order(Before = PredefinedAdornmentLayers.Text);
      TextViewRole(PredefinedTextViewRoles.Document)>]
    member val CodeLensAdornmentLayerDefinition : AdornmentLayerDefinition = null with get, set

    interface IViewTaggerProvider with
        override __.CreateTagger(view, buffer) = 
            let wpfView =
                match view with
                | :? IVsUserData as userData ->
                    let res, v = userData.GetData (ref Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost)
                    
                    (v :?> IWpfTextViewHost).TextView
                | :? IWpfTextView as view -> view
                | _ -> failwith "error"
            
            box(SampleTagger(wpfView, buffer)) :?> _