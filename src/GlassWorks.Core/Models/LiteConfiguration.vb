Namespace Models

    ''' <summary>
    ''' Plain model (POCO) describing one lite - a single framed glass opening.
    ''' A window unit is one or more lites mulled together side by side.
    ''' This class deliberately has no change notification: the ViewModel layer
    ''' wraps it and takes snapshots of it; this type is what gets priced,
    ''' turned into a BOM, drawn, and serialized to JSON.
    ''' </summary>
    Public Class LiteConfiguration

        Public Property FrameType As FrameType = FrameType.Casement
        Public Property WidthInches As Double = 24
        Public Property HeightInches As Double = 48
        Public Property GridPattern As GridPattern = GridPattern.None

        ''' <summary>Rows of grid openings when the pattern is Colonial.</summary>
        Public Property GridRows As Integer = 2

        ''' <summary>Columns of grid openings when the pattern is Colonial.</summary>
        Public Property GridColumns As Integer = 2

        Public Property GlassType As GlassType = GlassType.DoublePane
        Public Property Tint As GlassTint = GlassTint.Clear

        ''' <summary>Glass area of the rough opening in square feet.</summary>
        Public ReadOnly Property AreaSquareFeet As Double
            Get
                Return WidthInches * HeightInches / 144.0
            End Get
        End Property

        ''' <summary>Shallow copy - all properties are value types, so this is a full copy.</summary>
        Public Function Clone() As LiteConfiguration
            ' VB idiom: DirectCast is a compile-time-checked cast with no runtime
            ' conversion (C#'s (T)x for reference casts). CType also converts
            ' (e.g. numeric conversions); TryCast is C#'s 'as'.
            Return DirectCast(MemberwiseClone(), LiteConfiguration)
        End Function

    End Class

End Namespace
