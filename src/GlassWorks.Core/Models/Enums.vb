Imports System.ComponentModel

' VB idiom: files in this project sit under the project's RootNamespace
' ("GlassWorks.Core", set in the .vbproj). A 'Namespace Models' block therefore
' produces the full name "GlassWorks.Core.Models". In C# you would write the
' fully-qualified namespace in every file; VB composes it for you.
Namespace Models

    ''' <summary>
    ''' Operating style of a single lite (one framed glass opening).
    ''' </summary>
    ''' <remarks>
    ''' VB idiom: XML doc comments use three apostrophes (''') instead of C#'s ///.
    ''' The &lt;Description&gt; attributes drive the friendly names shown in the UI
    ''' (see EnumHelper.GetDescription and the EnumDescriptionConverter in the app).
    ''' </remarks>
    Public Enum FrameType
        <Description("Fixed (picture)")> Fixed
        <Description("Casement")> Casement
        <Description("Double-hung")> DoubleHung
        <Description("Slider")> Slider
        <Description("Awning")> Awning
    End Enum

    ''' <summary>Decorative grid (muntin) pattern applied to the glass.</summary>
    Public Enum GridPattern
        <Description("None")> None
        <Description("Colonial (rows × columns)")> Colonial
        <Description("Prairie (perimeter)")> Prairie
    End Enum

    ''' <summary>
    ''' Glazing package. Named SinglePane/DoublePane/TriplePane rather than
    ''' Single/Double/Triple because Single and Double are VB *keywords*
    ''' (the Single/Double floating point types). They could be escaped with
    ''' square brackets ([Single]) but avoiding the clash is cleaner.
    ''' </summary>
    Public Enum GlassType
        <Description("Single pane")> SinglePane
        <Description("Double pane (IGU)")> DoublePane
        <Description("Triple pane (IGU)")> TriplePane
    End Enum

    ''' <summary>Glass tint / coating option.</summary>
    Public Enum GlassTint
        <Description("Clear")> Clear
        <Description("Low-E coating")> LowE
        <Description("Bronze tint")> Bronze
        <Description("Gray tint")> Gray
    End Enum

    ''' <summary>Frame extrusion material.</summary>
    Public Enum FrameMaterial
        <Description("Vinyl")> Vinyl
        <Description("Aluminum")> Aluminum
        <Description("Wood")> Wood
        <Description("Fiberglass")> Fiberglass
    End Enum

End Namespace
