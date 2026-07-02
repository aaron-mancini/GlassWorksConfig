Namespace Models

    ''' <summary>One itemized charge on the price breakdown.</summary>
    Public Class PriceLine

        Public ReadOnly Property Description As String
        Public ReadOnly Property Amount As Decimal

        Public Sub New(description As String, amount As Decimal)
            Me.Description = description
            Me.Amount = amount
        End Sub

    End Class

    ''' <summary>
    ''' The itemized result of pricing a window unit. Immutable snapshot -
    ''' the pricing service returns a fresh one on every recalculation and the
    ''' UI binds to it.
    ''' </summary>
    Public Class PriceBreakdown

        Public ReadOnly Property Lines As IReadOnlyList(Of PriceLine)

        ''' <summary>Sum of all line amounts (already rounded per line).</summary>
        Public ReadOnly Property Total As Decimal

        Public Sub New(lines As IEnumerable(Of PriceLine))
            Dim list = lines.ToList()
            Me.Lines = list
            Me.Total = list.Sum(Function(l) l.Amount)
        End Sub

        ''' <summary>An empty breakdown, handy as a null-object default.</summary>
        Public Shared ReadOnly Property Empty As New PriceBreakdown(Enumerable.Empty(Of PriceLine)())

    End Class

End Namespace
