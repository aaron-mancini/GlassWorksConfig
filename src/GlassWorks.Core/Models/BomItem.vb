Namespace Models

    ''' <summary>Grouping bucket for bill-of-materials rows.</summary>
    Public Enum BomCategory
        <ComponentModel.Description("Frame")> Frame
        <ComponentModel.Description("Sash")> Sash
        <ComponentModel.Description("Glass")> Glass
        <ComponentModel.Description("Spacer")> Spacer
        <ComponentModel.Description("Grid")> Grid
        <ComponentModel.Description("Hardware")> Hardware
        <ComponentModel.Description("Mulling")> Mulling
        <ComponentModel.Description("Screen")> Screen
    End Enum

    ''' <summary>
    ''' One row of the cut list / bill of materials: what to cut or pull from
    ''' stock, how many, and at what size.
    ''' </summary>
    Public Class BomItem

        Public ReadOnly Property Category As BomCategory
        Public ReadOnly Property Description As String

        ''' <summary>Quantity is Double because linear stock is measured in feet (e.g. 12.5 lin ft).</summary>
        Public ReadOnly Property Quantity As Double

        ''' <summary>Unit of measure for the quantity: "ea" or "lin ft".</summary>
        Public ReadOnly Property Unit As String

        ''' <summary>Cut dimensions, already formatted for display (may be empty for hardware).</summary>
        Public ReadOnly Property Dimensions As String

        Public Sub New(category As BomCategory, description As String, quantity As Double,
                       unit As String, Optional dimensions As String = "")
            ' VB idiom: 'Optional ... = ""' declares an optional parameter with a
            ' default, same as C#'s 'string dimensions = ""'.
            Me.Category = category
            Me.Description = description
            Me.Quantity = quantity
            Me.Unit = unit
            Me.Dimensions = dimensions
        End Sub

        ''' <summary>Friendly category name for display (from the Description attribute).</summary>
        Public ReadOnly Property CategoryName As String
            Get
                Return Category.GetDescription()
            End Get
        End Property

    End Class

End Namespace
