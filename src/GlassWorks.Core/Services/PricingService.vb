Imports GlassWorks.Core.Models

Namespace Services

    ''' <summary>
    ''' The pricing engine. Pure function of the configuration - no UI types,
    ''' no I/O - which is what makes it trivially unit-testable.
    '''
    ''' Pricing model:
    '''   glass cost   = billable area (sq ft, floor of 6) × (base rate per glass type + tint adder)
    '''   frame charge = glass cost × (frame-type multiplier × material multiplier − 1)
    '''   grid charge  = per-opening charge (colonial) or flat charge (prairie), per lite
    '''   mulling      = flat charge per mull joint on multi-lite units
    ''' All money is Decimal (never Double - binary floating point drifts on
    ''' currency); each line is rounded to cents and the total is the exact sum
    ''' of the rounded lines, so the panel always foots.
    ''' </summary>
    Public Class PricingService
        Implements IPricingService
        ' VB idiom: 'Implements IPricingService' at class level, and then *each
        ' member* repeats an 'Implements' clause naming the interface member it
        ' satisfies (see CalculatePrice below). Unlike C#'s implicit matching by
        ' signature, VB's interface wiring is always explicit - the method name
        ' does not even have to match the interface member name.

        ' --- Rate tables -----------------------------------------------------
        ' VB idiom: the 'D' suffix makes a Decimal literal (C# uses 'm').
        ' 'R' would be Double, 'F' Single, 'I' Integer, etc.

        ''' <summary>Small openings still cost a minimum amount to fabricate.</summary>
        Public Const MinimumBillableAreaSqFt As Double = 6.0

        Private Shared ReadOnly GlassBaseRatePerSqFt As New Dictionary(Of GlassType, Decimal) From {
            {GlassType.SinglePane, 8.5D},
            {GlassType.DoublePane, 12.75D},
            {GlassType.TriplePane, 18.5D}
        }

        Private Shared ReadOnly TintAdderPerSqFt As New Dictionary(Of GlassTint, Decimal) From {
            {GlassTint.Clear, 0D},
            {GlassTint.LowE, 2.5D},
            {GlassTint.Bronze, 1.9D},
            {GlassTint.Gray, 1.9D}
        }

        Private Shared ReadOnly FrameTypeMultiplier As New Dictionary(Of FrameType, Decimal) From {
            {FrameType.Fixed, 1.0D},
            {FrameType.Slider, 1.18D},
            {FrameType.Awning, 1.28D},
            {FrameType.Casement, 1.32D},
            {FrameType.DoubleHung, 1.38D}
        }

        Private Shared ReadOnly MaterialMultiplier As New Dictionary(Of FrameMaterial, Decimal) From {
            {FrameMaterial.Vinyl, 1.0D},
            {FrameMaterial.Aluminum, 1.12D},
            {FrameMaterial.Fiberglass, 1.4D},
            {FrameMaterial.Wood, 1.65D}
        }

        Public Const ColonialChargePerOpening As Decimal = 2.5D
        Public Const PrairieChargePerLite As Decimal = 16D
        Public Const MullChargePerJoint As Decimal = 32.5D

        ' --- Engine -----------------------------------------------------------

        Public Function CalculatePrice(unit As WindowUnitConfiguration) As PriceBreakdown _
            Implements IPricingService.CalculatePrice

            If unit Is Nothing OrElse unit.Lites.Count = 0 Then Return PriceBreakdown.Empty

            Dim lines As New List(Of PriceLine)

            ' VB idiom: classic For loop is 'For i = 0 To n - 1 ... Next'
            ' (inclusive upper bound, unlike C#'s 'i < n').
            For i = 0 To unit.Lites.Count - 1
                Dim lite = unit.Lites(i)
                Dim label = $"Lite {i + 1}"

                ' 1) Glass: area-based, with a fabrication minimum.
                Dim billableArea = Math.Max(lite.AreaSquareFeet, MinimumBillableAreaSqFt)
                Dim ratePerSqFt = GlassBaseRatePerSqFt(lite.GlassType) + TintAdderPerSqFt(lite.Tint)
                ' CDec(...) is VB's explicit conversion (like (decimal)x in C#);
                ' required here because Option Strict On forbids implicit
                ' Double -> Decimal narrowing.
                Dim glassCost = Math.Round(CDec(billableArea) * ratePerSqFt, 2)
                lines.Add(New PriceLine(
                    $"{label} glass — {lite.GlassType.GetDescription()}, {lite.Tint.GetDescription()} ({billableArea:0.##} sq ft billed)",
                    glassCost))

                ' 2) Frame: expressed as an upcharge over the glass cost so the
                '    breakdown stays itemized yet still sums to the exact total.
                Dim combinedMultiplier = FrameTypeMultiplier(lite.FrameType) * MaterialMultiplier(unit.FrameMaterial)
                If combinedMultiplier <> 1D Then
                    Dim frameCharge = Math.Round(glassCost * (combinedMultiplier - 1D), 2)
                    lines.Add(New PriceLine(
                        $"{label} frame — {lite.FrameType.GetDescription()}, {unit.FrameMaterial.GetDescription()}",
                        frameCharge))
                End If

                ' 3) Grids.
                ' VB idiom: Select Case is C#'s switch; no break needed, cases
                ' never fall through.
                Select Case lite.GridPattern
                    Case GridPattern.Colonial
                        Dim openings = Math.Max(1, lite.GridRows) * Math.Max(1, lite.GridColumns)
                        lines.Add(New PriceLine(
                            $"{label} grid — Colonial {lite.GridRows}×{lite.GridColumns} ({openings} openings)",
                            ColonialChargePerOpening * openings))
                    Case GridPattern.Prairie
                        lines.Add(New PriceLine($"{label} grid — Prairie", PrairieChargePerLite))
                End Select
            Next

            ' 4) Mulling: joining n lites needs n−1 structural mull joints.
            If unit.MullJointCount > 0 Then
                lines.Add(New PriceLine(
                    $"Mulling — {unit.MullJointCount} joint(s) × {MullChargePerJoint:0.00}",
                    MullChargePerJoint * unit.MullJointCount))
            End If

            Return New PriceBreakdown(lines)
        End Function

    End Class

End Namespace
