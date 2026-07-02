Imports GlassWorks.Core.Models

Namespace Services

    ''' <summary>
    ''' Generates the cut list / bill of materials for a configured unit.
    ''' Interface-first for the same reason as IPricingService: the ViewModel
    ''' takes the abstraction, tests exercise the concrete class directly.
    ''' </summary>
    Public Interface IBomService

        Function GenerateBom(unit As WindowUnitConfiguration) As IReadOnlyList(Of BomItem)

    End Interface

End Namespace
