Namespace Models

    ''' <summary>
    ''' The full configured product: one frame material shared by the unit, and
    ''' one or more lites arranged left-to-right. Two or more lites make a
    ''' "mulled unit" (lites joined with structural mull bars).
    ''' </summary>
    Public Class WindowUnitConfiguration

        Public Property FrameMaterial As FrameMaterial = FrameMaterial.Vinyl

        ' VB idiom: 'As New List(...)' declares AND instantiates in one line.
        Public Property Lites As New List(Of LiteConfiguration)

        ''' <summary>Overall width: lites sit side by side, so widths add up.</summary>
        Public ReadOnly Property TotalWidthInches As Double
            Get
                ' VB idiom: lambdas are written Function(x) expr / Sub(x) stmt
                ' instead of C#'s x => expr. LINQ works identically.
                Return Lites.Sum(Function(l) l.WidthInches)
            End Get
        End Property

        ''' <summary>Overall height: the tallest lite governs.</summary>
        Public ReadOnly Property MaxHeightInches As Double
            Get
                Return If(Lites.Count = 0, 0, Lites.Max(Function(l) l.HeightInches))
            End Get
        End Property

        ''' <summary>Number of mull joints (bars joining adjacent lites).</summary>
        Public ReadOnly Property MullJointCount As Integer
            Get
                Return Math.Max(0, Lites.Count - 1)
            End Get
        End Property

        ''' <summary>Deep copy, used to freeze a quote line item's configuration.</summary>
        Public Function Clone() As WindowUnitConfiguration
            Dim copy As New WindowUnitConfiguration With {.FrameMaterial = FrameMaterial}
            ' VB idiom: 'With {.Prop = value}' is the object initializer syntax;
            ' note the leading dot on member names (C#: new T { Prop = value }).
            For Each lite In Lites
                copy.Lites.Add(lite.Clone())
            Next
            Return copy
        End Function

    End Class

End Namespace
