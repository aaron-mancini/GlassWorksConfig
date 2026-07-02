Imports System.Windows
Imports GlassWorks.Configurator.Services
Imports GlassWorks.Configurator.ViewModels
Imports GlassWorks.Configurator.Views
Imports GlassWorks.Core.Services

''' <summary>
''' Application code-behind - the COMPOSITION ROOT. This is the one place where
''' concrete service implementations are chosen and wired together; everything
''' downstream (the MainViewModel) depends only on interfaces. There is no DI
''' container here to keep the learning surface small, but swapping this for
''' Microsoft.Extensions.DependencyInjection later would touch only this file.
''' </summary>
''' <remarks>
''' VB idiom: 'Partial' marks the other half of a class whose first half is
''' generated from XAML (same keyword as C#, but note VB spells inheritance
''' with an 'Inherits' *line* instead of ': Application').
''' </remarks>
Partial Public Class App
    Inherits Application

    ''' <summary>
    ''' VB idiom: 'Overrides' replaces C#'s 'override' modifier, and the base
    ''' call is 'MyBase.OnStartup(e)' - MyBase is C#'s 'base'.
    ''' </summary>
    Protected Overrides Sub OnStartup(e As StartupEventArgs)
        MyBase.OnStartup(e)

        ' Build the service graph: all business services come from the core
        ' library; IDialogService is the app-side abstraction over WPF dialogs
        ' so the ViewModel never touches UI types directly.
        Dim pricing As IPricingService = New PricingService()
        Dim bom As IBomService = New BomService()
        Dim repository As IQuoteRepository = New JsonQuoteRepository()
        Dim exporter As IQuoteExporter = New QuoteExporter()
        Dim dialogs As IDialogService = New DialogService()

        Dim viewModel As New MainViewModel(pricing, bom, repository, exporter, dialogs)

        ' View-first, ViewModel-injected: the window's DataContext is the only
        ' link between the XAML world and the ViewModel world.
        Dim window As New MainWindow With {.DataContext = viewModel}
        window.Show()
    End Sub

End Class
