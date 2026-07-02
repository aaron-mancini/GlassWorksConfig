Imports GlassWorks.Core.Models

Namespace ViewModels

    ''' <summary>
    ''' Thin wrapper over a QuoteLineItem model: exposes it to binding and adds
    ''' change notification for the one editable field (Quantity), so line and
    ''' grand totals update live as the user edits quantities in the quote list.
    ''' </summary>
    Public Class QuoteLineItemViewModel
        Inherits ViewModelBase

        Private ReadOnly _model As QuoteLineItem

        Public Sub New(model As QuoteLineItem)
            If model Is Nothing Then Throw New ArgumentNullException(NameOf(model))
            _model = model
        End Sub

        ''' <summary>The underlying model - what actually gets persisted/exported.</summary>
        Public ReadOnly Property Model As QuoteLineItem
            Get
                Return _model
            End Get
        End Property

        Public ReadOnly Property Description As String
            Get
                Return _model.Description
            End Get
        End Property

        Public ReadOnly Property UnitPrice As Decimal
            Get
                Return _model.UnitPrice
            End Get
        End Property

        Public Property Quantity As Integer
            Get
                Return _model.Quantity
            End Get
            Set(value As Integer)
                ' Clamp instead of erroring: a quote line of zero units is nonsense.
                If value < 1 Then value = 1
                If _model.Quantity = value Then Return
                _model.Quantity = value
                OnPropertyChanged() ' CallerMemberName resolves to "Quantity"
                OnPropertyChanged(NameOf(LineTotal))
            End Set
        End Property

        Public ReadOnly Property LineTotal As Decimal
            Get
                Return _model.LineTotal
            End Get
        End Property

    End Class

End Namespace
