Imports System.Text.Json.Serialization

Namespace Models

    ''' <summary>
    ''' One saved line on a quote: a frozen window unit configuration plus
    ''' quantity and the unit price that was in effect when it was added.
    ''' Mutable with public setters so System.Text.Json can round-trip it.
    ''' </summary>
    Public Class QuoteLineItem

        Public Property Description As String = ""
        Public Property Quantity As Integer = 1
        Public Property UnitPrice As Decimal
        Public Property Unit As WindowUnitConfiguration

        ''' <summary>Computed, so it is always consistent; not serialized.</summary>
        <JsonIgnore>
        Public ReadOnly Property LineTotal As Decimal
            Get
                Return UnitPrice * Quantity
            End Get
        End Property

    End Class

    ''' <summary>A customer quote: a set of line items persisted to JSON.</summary>
    Public Class Quote

        Public Property QuoteNumber As String = $"Q-{Date.UtcNow:yyyyMMdd-HHmmss}"
        Public Property CreatedUtc As Date = Date.UtcNow

        ' VB idiom: 'Date' is VB's alias for System.DateTime (like 'Integer' for
        ' Int32 or 'String' for System.String). Either spelling works.
        Public Property Items As New List(Of QuoteLineItem)

        <JsonIgnore>
        Public ReadOnly Property GrandTotal As Decimal
            Get
                Return Items.Sum(Function(i) i.LineTotal)
            End Get
        End Property

    End Class

End Namespace
