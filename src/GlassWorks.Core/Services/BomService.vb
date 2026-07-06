Imports GlassWorks.Core.Models

Namespace Services

    ''' <summary>
    ''' Builds the cut list for a unit: frame extrusions, sash extrusions, glass
    ''' panel sizes (net of the frame reveal), spacer bar stock for insulated
    ''' units, grid kits, hardware per frame type, and mull kits.
    ''' The geometry is intentionally simplified shop math, but the shape of the
    ''' service (pure, injectable, testable) is the real-world pattern.
    ''' </summary>
    Public Class BomService
        Implements IBomService

        Public Function GenerateBom(unit As WindowUnitConfiguration) As IReadOnlyList(Of BomItem) _
            Implements IBomService.GenerateBom

            Dim items As New List(Of BomItem)
            If unit Is Nothing OrElse unit.Lites.Count = 0 Then Return items

            Dim material = unit.FrameMaterial.GetDescription()

            For i = 0 To unit.Lites.Count - 1
                Dim lite = unit.Lites(i)
                Dim spec = FrameTypeCatalog.GetSpec(lite.FrameType)
                Dim label = $"Lite {i + 1}"

                ' --- Frame extrusions: a rectangular frame is two horizontal
                '     pieces (head + sill) cut at width and two vertical pieces
                '     (jambs) cut at height.
                items.Add(New BomItem(BomCategory.Frame, $"{label} head/sill extrusion — {material}",
                                      2, "ea", FormatLength(lite.WidthInches)))
                items.Add(New BomItem(BomCategory.Frame, $"{label} jamb extrusion — {material}",
                                      2, "ea", FormatLength(lite.HeightInches)))

                ' --- Glass panels, net of the frame/sash reveal.
                Dim panelWidth, panelHeight As Double  ' VB idiom: one Dim can declare several variables of one type.
                Dim panelsPerLite As Integer
                GetGlassPanelSize(lite, spec, panelWidth, panelHeight, panelsPerLite)

                Dim panes = PaneCount(lite.GlassType)
                items.Add(New BomItem(BomCategory.Glass,
                    $"{label} glass pane — {lite.Tint.GetDescription()} ({lite.GlassType.GetDescription()})",
                    panelsPerLite * panes, "ea", FormatSize(panelWidth, panelHeight)))

                ' --- Sash extrusions for operable units (perimeter of each panel).
                If spec.SashCount > 0 Then
                    Dim sashPerimeter = 2 * (panelWidth + panelHeight)
                    items.Add(New BomItem(BomCategory.Sash, $"{label} sash extrusion — {material}",
                                          spec.SashCount, "ea", $"{FormatLength(sashPerimeter)} perimeter"))
                End If

                ' --- Spacer bar separates the panes of an insulated glass unit:
                '     one perimeter run per gap between panes, per panel.
                If panes > 1 Then
                    Dim spacerFeet = 2 * (panelWidth + panelHeight) / 12.0 * panelsPerLite * (panes - 1)
                    items.Add(New BomItem(BomCategory.Spacer, $"{label} warm-edge spacer bar",
                                          Math.Round(spacerFeet, 1), "lin ft"))
                End If

                ' --- Grid kits.
                Select Case lite.GridPattern
                    Case GridPattern.Colonial
                        items.Add(New BomItem(BomCategory.Grid,
                            $"{label} colonial grid kit {lite.GridRows}×{lite.GridColumns}",
                            panelsPerLite, "ea"))
                    Case GridPattern.Prairie
                        items.Add(New BomItem(BomCategory.Grid, $"{label} prairie grid kit",
                                              panelsPerLite, "ea"))
                End Select

                ' --- Screen.
                If lite.HasScreen And FrameTypeCatalog.GetSpec(lite.FrameType).SupportsScreen Then
                    items.Add(New BomItem(BomCategory.Screen, $"{label} screen mesh — {panelWidth}x{panelHeight}", Math.Round(panelWidth * panelHeight) / 144, "sq ft"))
                End If

                ' --- Hardware per frame type.
                AddHardware(items, label, lite.FrameType)
            Next

            ' --- Mulling: one structural mull kit per joint, cut at the shorter
            '     of the two adjacent lites.
            For j = 0 To unit.Lites.Count - 2
                Dim jointHeight = Math.Min(unit.Lites(j).HeightInches, unit.Lites(j + 1).HeightInches)
                items.Add(New BomItem(BomCategory.Mulling, $"Structural mull kit (joint {j + 1})",
                                      1, "ea", FormatLength(jointHeight)))
            Next

            Return items
        End Function

        ''' <summary>
        ''' Net glass size per frame type. Operable styles split the opening into
        ''' panels: a double-hung stacks two sashes (three horizontal frame
        ''' members: head, check rail, sill), a slider parks two panels side by
        ''' side. Fixed/casement/awning glaze a single panel.
        ''' </summary>
        ''' <remarks>
        ''' VB idiom: ByRef parameters are C#'s ref/out. VB does not distinguish
        ''' 'out' from 'ref', and call sites need no keyword (no 'out var' /
        ''' 'ref x' decoration) - which is why this deserves a loud comment.
        ''' </remarks>
        Private Shared Sub GetGlassPanelSize(lite As LiteConfiguration, spec As FrameTypeSpec,
                                             ByRef panelWidth As Double, ByRef panelHeight As Double,
                                             ByRef panelsPerLite As Integer)
            Dim d = spec.GlassDeductionPerSideInches
            Select Case lite.FrameType
                Case FrameType.DoubleHung
                    panelWidth = lite.WidthInches - 2 * d
                    panelHeight = (lite.HeightInches - 3 * d) / 2
                    panelsPerLite = 2
                Case FrameType.Slider
                    panelWidth = (lite.WidthInches - 3 * d) / 2
                    panelHeight = lite.HeightInches - 2 * d
                    panelsPerLite = 2
                Case Else
                    panelWidth = lite.WidthInches - 2 * d
                    panelHeight = lite.HeightInches - 2 * d
                    panelsPerLite = 1
            End Select
            panelWidth = Math.Max(panelWidth, 1)
            panelHeight = Math.Max(panelHeight, 1)
        End Sub

        Private Shared Function PaneCount(glassType As GlassType) As Integer
            Select Case glassType
                Case GlassType.SinglePane : Return 1
                Case GlassType.DoublePane : Return 2
                Case Else : Return 3
            End Select
            ' VB idiom: the colon above is a statement separator, letting a short
            ' Case share a line with its statement (use sparingly).
        End Function

        Private Shared Sub AddHardware(items As List(Of BomItem), label As String, frameType As FrameType)
            Select Case frameType
                Case FrameType.Casement
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} casement hinge set", 2, "ea"))
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} crank operator", 1, "ea"))
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} multi-point lock", 1, "ea"))
                Case FrameType.DoubleHung
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} sash balance", 2, "ea"))
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} cam lock + keeper", 2, "ea"))
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} tilt latch pair", 2, "ea"))
                Case FrameType.Slider
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} roller assembly", 2, "ea"))
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} sweep latch", 1, "ea"))
                Case FrameType.Awning
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} awning hinge set", 2, "ea"))
                    items.Add(New BomItem(BomCategory.Hardware, $"{label} push-out operator", 1, "ea"))
                    ' Fixed: no hardware.
            End Select
        End Sub

        Private Shared Function FormatLength(inches As Double) As String
            Return $"{inches:0.##} in"
        End Function

        Private Shared Function FormatSize(widthInches As Double, heightInches As Double) As String
            Return $"{widthInches:0.##} × {heightInches:0.##} in"
        End Function

    End Class

End Namespace
