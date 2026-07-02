Imports System.IO
Imports GlassWorks.Core.Models
Imports GlassWorks.Core.Services
Imports Xunit

''' <summary>
''' Round-trip tests for JSON persistence and a smoke test for the exporters.
''' Implements IDisposable for cleanup - xUnit runs Dispose after each test
''' (the VB equivalent of a C# test class with a Dispose method).
''' </summary>
Public Class QuotePersistenceTests
    Implements IDisposable

    Private ReadOnly _tempDir As String =
        Path.Combine(Path.GetTempPath(), "glassworks-tests-" & Guid.NewGuid().ToString("N"))

    Public Sub New()
        Directory.CreateDirectory(_tempDir)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Directory.Delete(_tempDir, recursive:=True)
    End Sub

    Private Function MakeQuote() As Quote
        Dim unit As New WindowUnitConfiguration With {.FrameMaterial = FrameMaterial.Fiberglass}
        unit.Lites.Add(New LiteConfiguration With {
            .FrameType = FrameType.Casement, .WidthInches = 28, .HeightInches = 54,
            .GlassType = GlassType.TriplePane, .Tint = GlassTint.LowE,
            .GridPattern = GridPattern.Colonial, .GridRows = 3, .GridColumns = 2})

        Dim quote As New Quote()
        quote.Items.Add(New QuoteLineItem With {
            .Description = "Casement 28×54, triple, low-E",
            .Quantity = 4,
            .UnitPrice = 321.55D,
            .Unit = unit})
        Return quote
    End Function

    <Fact>
    Public Sub SaveThenLoad_RoundTripsTheQuote()
        Dim repoPath = Path.Combine(_tempDir, "quote.json")
        Dim repo As New JsonQuoteRepository(repoPath)
        Dim original = MakeQuote()

        repo.Save(original)
        Dim loaded = repo.Load()

        Dim item = Assert.Single(loaded.Items)
        Assert.Equal(4, item.Quantity)
        Assert.Equal(321.55D, item.UnitPrice)
        Assert.Equal(1286.2D, loaded.GrandTotal)
        ' The nested configuration must survive, including enums stored by name.
        Assert.Equal(FrameType.Casement, item.Unit.Lites.Single().FrameType)
        Assert.Equal(FrameMaterial.Fiberglass, item.Unit.FrameMaterial)
    End Sub

    <Fact>
    Public Sub Load_WithNoFile_ReturnsEmptyQuote()
        Dim repo As New JsonQuoteRepository(Path.Combine(_tempDir, "missing.json"))

        Dim quote = repo.Load()

        Assert.NotNull(quote)
        Assert.Empty(quote.Items)
    End Sub

    <Fact>
    Public Sub Load_WithCorruptFile_ReturnsEmptyQuote()
        Dim repoPath = Path.Combine(_tempDir, "corrupt.json")
        File.WriteAllText(repoPath, "{ not valid json !!")
        Dim repo As New JsonQuoteRepository(repoPath)

        Dim quote = repo.Load()

        Assert.Empty(quote.Items)
    End Sub

    <Fact>
    Public Sub CsvExport_QuotesFieldsAndFootsTotal()
        Dim csvPath = Path.Combine(_tempDir, "quote.csv")
        Dim quote = MakeQuote()
        quote.Items(0).Description = "Unit with, a comma"

        Call New QuoteExporter().ExportCsv(quote, csvPath)
        ' VB idiom: 'Call' lets you invoke a member on a freshly-constructed
        ' object in statement position (C# just writes new X().M()).

        Dim lines = File.ReadAllLines(csvPath)
        Assert.Contains("""Unit with, a comma""", lines(1))
        Assert.EndsWith("1286.20", lines.Last())
    End Sub

    <Fact>
    Public Sub TextExport_ContainsGrandTotal()
        Dim txtPath = Path.Combine(_tempDir, "quote.txt")

        Call New QuoteExporter().ExportText(MakeQuote(), txtPath)

        Assert.Contains("GRAND TOTAL:", File.ReadAllText(txtPath))
    End Sub

End Class
