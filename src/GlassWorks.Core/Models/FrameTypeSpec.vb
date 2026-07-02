Namespace Models

    ''' <summary>
    ''' Manufacturing constraints and geometry facts for one frame type:
    ''' allowable size range, how much the visible glass is reduced by the frame
    ''' and sash ("glass deduction"), and how many operable sashes it carries.
    ''' </summary>
    Public Class FrameTypeSpec

        ' VB idiom: auto-implemented properties. 'Public Property X As T' is the
        ' equivalent of C#'s 'public T X { get; set; }'. Read-only auto-properties
        ' use 'Public ReadOnly Property X As T' and may only be assigned in the
        ' constructor (like C# '{ get; }').
        Public ReadOnly Property FrameType As FrameType
        Public ReadOnly Property MinWidthInches As Double
        Public ReadOnly Property MaxWidthInches As Double
        Public ReadOnly Property MinHeightInches As Double
        Public ReadOnly Property MaxHeightInches As Double

        ''' <summary>
        ''' Inches removed from each side of the rough lite size to get the visible
        ''' glass size (frame profile + sash stile). Used by the BOM and preview.
        ''' </summary>
        Public ReadOnly Property GlassDeductionPerSideInches As Double

        ''' <summary>Number of operable sash panels (0 for fixed units).</summary>
        Public ReadOnly Property SashCount As Integer

        Public Sub New(frameType As FrameType,
                       minWidth As Double, maxWidth As Double,
                       minHeight As Double, maxHeight As Double,
                       glassDeductionPerSide As Double,
                       sashCount As Integer)
            ' VB idiom: 'Me' is C#'s 'this'. Assignments below disambiguate the
            ' property from the same-named parameter exactly as 'this.x = x' would.
            Me.FrameType = frameType
            Me.MinWidthInches = minWidth
            Me.MaxWidthInches = maxWidth
            Me.MinHeightInches = minHeight
            Me.MaxHeightInches = maxHeight
            Me.GlassDeductionPerSideInches = glassDeductionPerSide
            Me.SashCount = sashCount
        End Sub

        ''' <summary>Human-readable size range, used as a hint in the UI.</summary>
        Public ReadOnly Property SizeRangeText As String
            Get
                ' VB idiom: string interpolation uses $"..." exactly like C#.
                Return $"W {MinWidthInches:0}–{MaxWidthInches:0} in, H {MinHeightInches:0}–{MaxHeightInches:0} in"
            End Get
        End Property

    End Class

    ''' <summary>
    ''' Static catalog of the specs for every frame type the shop can build.
    ''' </summary>
    ''' <remarks>
    ''' VB idiom: a Module is a static class; everything in it is Shared.
    ''' </remarks>
    Public Module FrameTypeCatalog

        ' VB idiom: 'Dim x As New T From {...}' combines declaration, construction
        ' and a collection initializer. Dictionary initializers use nested braces
        ' {key, value} rather than C#'s {key, value} / [key] = value forms.
        Private ReadOnly Specs As New Dictionary(Of FrameType, FrameTypeSpec) From {
            {FrameType.Fixed, New FrameTypeSpec(FrameType.Fixed, 12, 120, 12, 96, glassDeductionPerSide:=1.75, sashCount:=0)},
            {FrameType.Casement, New FrameTypeSpec(FrameType.Casement, 14, 36, 24, 72, glassDeductionPerSide:=3.0, sashCount:=1)},
            {FrameType.DoubleHung, New FrameTypeSpec(FrameType.DoubleHung, 20, 48, 36, 84, glassDeductionPerSide:=3.25, sashCount:=2)},
            {FrameType.Slider, New FrameTypeSpec(FrameType.Slider, 36, 84, 24, 60, glassDeductionPerSide:=3.0, sashCount:=2)},
            {FrameType.Awning, New FrameTypeSpec(FrameType.Awning, 18, 48, 14, 36, glassDeductionPerSide:=3.0, sashCount:=1)}
        }
        ' VB idiom above: 'glassDeductionPerSide:=1.75' is a named argument -
        ' VB uses ':=' where C# uses ':' (glassDeductionPerSide: 1.75).

        ''' <summary>Looks up the spec for a frame type. Throws if unknown.</summary>
        Public Function GetSpec(frameType As FrameType) As FrameTypeSpec
            Return Specs(frameType)
        End Function

    End Module

End Namespace
