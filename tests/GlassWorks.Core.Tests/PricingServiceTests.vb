Imports GlassWorks.Core.Models
Imports GlassWorks.Core.Services
Imports Xunit

''' <summary>
''' Unit tests for the pricing engine. Written in VB.NET with xUnit - note that
''' xUnit needs no VB-specific packages; <Fact> and Assert work exactly as in C#.
''' </summary>
Public Class PricingServiceTests

    ' The concrete service under test. Tests use the class directly; the app
    ' consumes it through IPricingService.
    Private ReadOnly _service As New PricingService()

    ''' <summary>Builds a one-lite unit with sensible defaults, overridable per test.</summary>
    Private Shared Function SingleLiteUnit(Optional frameType As FrameType = FrameType.Fixed,
                                           Optional widthInches As Double = 24,
                                           Optional heightInches As Double = 36,
                                           Optional glassType As GlassType = GlassType.SinglePane,
                                           Optional tint As GlassTint = GlassTint.Clear,
                                           Optional material As FrameMaterial = FrameMaterial.Vinyl,
                                           Optional gridPattern As GridPattern = GridPattern.None,
                                           Optional gridRows As Integer = 2,
                                           Optional gridColumns As Integer = 2,
                                           Optional hasScreen As Boolean = False) As WindowUnitConfiguration
        Dim unit As New WindowUnitConfiguration With {.FrameMaterial = material}
        unit.Lites.Add(New LiteConfiguration With {
            .FrameType = frameType,
            .WidthInches = widthInches,
            .HeightInches = heightInches,
            .GlassType = glassType,
            .Tint = tint,
            .GridPattern = gridPattern,
            .GridRows = gridRows,
            .GridColumns = gridColumns,
            .HasScreen = hasScreen
        })
        Return unit
    End Function

    <Fact>
    Public Sub FixedVinylClearSinglePane_ChargesBaseGlassRateOnly()
        ' 24×36 in = 6 sq ft; single pane clear = $8.50/sq ft; fixed+vinyl have
        ' multiplier 1.0 so there must be exactly one line: 6 × 8.50 = $51.00.
        Dim result = _service.CalculatePrice(SingleLiteUnit())

        Assert.Equal(51D, result.Total)
        Dim line = Assert.Single(result.Lines)
        Assert.Contains("glass", line.Description, StringComparison.OrdinalIgnoreCase)
    End Sub

    <Fact>
    Public Sub TinyLite_IsBilledAtMinimumArea()
        ' 12×12 in = 1 sq ft, below the 6 sq ft fabrication minimum -> priced
        ' identically to a 6 sq ft lite.
        Dim result = _service.CalculatePrice(SingleLiteUnit(widthInches:=12, heightInches:=12))

        Assert.Equal(51D, result.Total) ' 6 (min) × 8.50
    End Sub

    <Fact>
    Public Sub LowETint_AddsPerSquareFootAdder()
        ' Double pane 12.75 + low-E 2.50 = $15.25/sq ft × 6 sq ft = $91.50.
        Dim result = _service.CalculatePrice(
            SingleLiteUnit(glassType:=GlassType.DoublePane, tint:=GlassTint.LowE))

        Assert.Equal(91.5D, result.Total)
    End Sub

    <Fact>
    Public Sub FrameTypeAndMaterialMultipliers_CompoundOnGlassCost()
        ' Casement (×1.32) in wood (×1.65): glass = 6 × 8.50 = 51.00;
        ' frame upcharge = 51 × (1.32 × 1.65 − 1) = 51 × 1.178 = 60.078 -> 60.08.
        Dim result = _service.CalculatePrice(
            SingleLiteUnit(frameType:=FrameType.Casement, material:=FrameMaterial.Wood))

        Assert.Equal(2, result.Lines.Count)
        Assert.Equal(51D, result.Lines(0).Amount)
        Assert.Equal(60.08D, result.Lines(1).Amount)
        Assert.Equal(111.08D, result.Total)
    End Sub

    <Fact>
    Public Sub FixedVinyl_HasNoFrameUpchargeLine()
        ' Multiplier 1.0 × 1.0 must not emit a $0.00 noise line.
        Dim result = _service.CalculatePrice(SingleLiteUnit())

        Assert.DoesNotContain(result.Lines,
            Function(l) l.Description.Contains("frame", StringComparison.OrdinalIgnoreCase))
    End Sub

    <Fact>
    Public Sub ColonialGrid_ChargesPerOpening()
        ' 2 rows × 3 columns = 6 openings × $2.50 = $15.00 on top of glass.
        Dim result = _service.CalculatePrice(
            SingleLiteUnit(gridPattern:=GridPattern.Colonial, gridRows:=2, gridColumns:=3))

        Dim gridLine = result.Lines.Single(Function(l) l.Description.Contains("grid", StringComparison.OrdinalIgnoreCase))
        Assert.Equal(15D, gridLine.Amount)
        Assert.Equal(66D, result.Total) ' 51 glass + 15 grid
    End Sub

    <Fact>
    Public Sub PrairieGrid_ChargesFlatRatePerLite()
        Dim result = _service.CalculatePrice(SingleLiteUnit(gridPattern:=GridPattern.Prairie))

        Dim gridLine = result.Lines.Single(Function(l) l.Description.Contains("Prairie"))
        Assert.Equal(16D, gridLine.Amount)
    End Sub

    <Fact>
    Public Sub MulledUnit_ChargesPerJoint()
        ' Three lites -> two mull joints -> 2 × $32.50 = $65.00 mulling line.
        Dim unit = SingleLiteUnit()
        unit.Lites.Add(New LiteConfiguration With {.FrameType = FrameType.Fixed, .WidthInches = 24, .HeightInches = 36, .GlassType = GlassType.SinglePane, .Tint = GlassTint.Clear})
        unit.Lites.Add(New LiteConfiguration With {.FrameType = FrameType.Fixed, .WidthInches = 24, .HeightInches = 36, .GlassType = GlassType.SinglePane, .Tint = GlassTint.Clear})

        Dim result = _service.CalculatePrice(unit)

        Dim mullLine = result.Lines.Single(Function(l) l.Description.StartsWith("Mulling"))
        Assert.Equal(65D, mullLine.Amount)
        Assert.Equal(3 * 51D + 65D, result.Total)
    End Sub

    <Fact>
    Public Sub SingleLite_HasNoMullingCharge()
        Dim result = _service.CalculatePrice(SingleLiteUnit())

        Assert.DoesNotContain(result.Lines, Function(l) l.Description.StartsWith("Mulling"))
    End Sub

    <Fact>
    Public Sub Total_AlwaysEqualsSumOfLines()
        ' The breakdown must "foot": the displayed total is exactly the sum of
        ' the displayed (rounded) lines, for an awkwardly-sized configuration.
        Dim unit = SingleLiteUnit(frameType:=FrameType.DoubleHung,
                                  widthInches:=33.33, heightInches:=57.7,
                                  glassType:=GlassType.TriplePane, tint:=GlassTint.Bronze,
                                  material:=FrameMaterial.Fiberglass,
                                  gridPattern:=GridPattern.Colonial, gridRows:=3, gridColumns:=2)

        Dim result = _service.CalculatePrice(unit)

        Assert.Equal(result.Lines.Sum(Function(l) l.Amount), result.Total)
    End Sub

    <Fact>
    Public Sub EmptyUnit_ReturnsEmptyBreakdown()
        Dim result = _service.CalculatePrice(New WindowUnitConfiguration())

        Assert.Empty(result.Lines)
        Assert.Equal(0D, result.Total)
    End Sub

    <Fact>
    Public Sub FixedLite_CannotHaveScreenPrice()
        Dim unitWithScreen = SingleLiteUnit(frameType:=FrameType.Fixed,
                                  widthInches:=30, heightInches:=60,
                                  glassType:=GlassType.SinglePane, tint:=GlassTint.Clear,
                                  material:=FrameMaterial.Fiberglass, gridPattern:=GridPattern.None,
                                  hasScreen:=True)
        Dim unitWithoutScreen = SingleLiteUnit(frameType:=FrameType.Fixed,
                                  widthInches:=30, heightInches:=60,
                                  glassType:=GlassType.SinglePane, tint:=GlassTint.Clear,
                                  material:=FrameMaterial.Fiberglass, gridPattern:=GridPattern.None)
        Dim resultWith = _service.CalculatePrice(unitWithScreen)
        Dim resultWithout = _service.CalculatePrice(unitWithoutScreen)

        Assert.Equal(resultWith.Total, resultWithout.Total)
    End Sub

End Class
