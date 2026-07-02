Imports GlassWorks.Core.Models
Imports GlassWorks.Core.Services
Imports Xunit

''' <summary>Tests for the cut list / bill of materials generator.</summary>
Public Class BomServiceTests

    Private ReadOnly _service As New BomService()

    Private Shared Function UnitWith(ParamArray lites() As LiteConfiguration) As WindowUnitConfiguration
        ' VB idiom: ParamArray = C#'s 'params'. Callers pass a comma list.
        Dim unit As New WindowUnitConfiguration()
        unit.Lites.AddRange(lites)
        Return unit
    End Function

    <Fact>
    Public Sub FixedLite_GlassIsReducedByFrameReveal()
        ' Fixed spec deducts 1.75 in per side: 24−3.5 × 36−3.5 = 20.5 × 32.5.
        Dim bom = _service.GenerateBom(UnitWith(New LiteConfiguration With {
            .FrameType = FrameType.Fixed, .WidthInches = 24, .HeightInches = 36,
            .GlassType = GlassType.SinglePane}))

        Dim glass = bom.Single(Function(i) i.Category = BomCategory.Glass)
        Assert.Equal("20.5 × 32.5 in", glass.Dimensions)
        Assert.Equal(1, glass.Quantity) ' one panel × one pane
    End Sub

    <Fact>
    Public Sub TriplePaneDoubleHung_YieldsSixPanes()
        ' Double-hung = 2 sash panels; triple pane = 3 panes each -> 6 pieces of glass.
        Dim bom = _service.GenerateBom(UnitWith(New LiteConfiguration With {
            .FrameType = FrameType.DoubleHung, .WidthInches = 30, .HeightInches = 60,
            .GlassType = GlassType.TriplePane}))

        Dim glass = bom.Single(Function(i) i.Category = BomCategory.Glass)
        Assert.Equal(6, glass.Quantity)
    End Sub

    <Fact>
    Public Sub SinglePane_HasNoSpacerBar()
        Dim bom = _service.GenerateBom(UnitWith(New LiteConfiguration With {
            .FrameType = FrameType.Fixed, .GlassType = GlassType.SinglePane}))

        Assert.DoesNotContain(bom, Function(i) i.Category = BomCategory.Spacer)
    End Sub

    <Fact>
    Public Sub DoublePane_HasSpacerBar()
        Dim bom = _service.GenerateBom(UnitWith(New LiteConfiguration With {
            .FrameType = FrameType.Fixed, .GlassType = GlassType.DoublePane}))

        Assert.Contains(bom, Function(i) i.Category = BomCategory.Spacer)
    End Sub

    <Fact>
    Public Sub Casement_GetsOperatorHardware_FixedGetsNone()
        Dim casementBom = _service.GenerateBom(UnitWith(New LiteConfiguration With {.FrameType = FrameType.Casement}))
        Dim fixedBom = _service.GenerateBom(UnitWith(New LiteConfiguration With {.FrameType = FrameType.Fixed}))

        Assert.Contains(casementBom, Function(i) i.Description.Contains("crank operator"))
        Assert.DoesNotContain(fixedBom, Function(i) i.Category = BomCategory.Hardware)
    End Sub

    <Fact>
    Public Sub MulledUnit_GetsOneMullKitPerJoint()
        Dim bom = _service.GenerateBom(UnitWith(
            New LiteConfiguration(), New LiteConfiguration(), New LiteConfiguration()))

        ' VB gotcha: bom.Count(predicate) will NOT compile here - VB binds the
        ' Count *property* of IReadOnlyList before considering the LINQ Count
        ' *extension method* (C# picks the extension overload). Filter first.
        Assert.Equal(2, bom.Where(Function(i) i.Category = BomCategory.Mulling).Count())
    End Sub

    <Fact>
    Public Sub EveryFrameType_HasACatalogSpec()
        ' Guards against adding an enum member without manufacturing data.
        For Each frameType As FrameType In [Enum].GetValues(GetType(FrameType))
            Dim spec = FrameTypeCatalog.GetSpec(frameType)
            Assert.True(spec.MinWidthInches < spec.MaxWidthInches)
            Assert.True(spec.MinHeightInches < spec.MaxHeightInches)
        Next
    End Sub

End Class
