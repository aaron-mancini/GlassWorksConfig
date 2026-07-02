Imports GlassWorks.Core.Models

Namespace Services

    ''' <summary>
    ''' Prices a configured window unit. Kept behind an interface so the
    ''' ViewModel depends on an abstraction (injectable, mockable in tests,
    ''' swappable for e.g. a region-specific price book later).
    ''' </summary>
    Public Interface IPricingService

        ''' <summary>Produces an itemized price breakdown for the unit.</summary>
        Function CalculatePrice(unit As WindowUnitConfiguration) As PriceBreakdown

    End Interface

End Namespace
