Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Shapes
Imports GlassWorks.Configurator.Converters
Imports GlassWorks.Core.Models

Namespace Controls

    ''' <summary>
    ''' Draws the configured window unit to scale on a Canvas: frames, sashes,
    ''' glass tint, grid patterns, operation symbols (the dashed lines glazing
    ''' shop drawings use), mull bars, and architectural dimension lines.
    '''
    ''' Redraw is fully binding-driven: MainViewModel publishes a fresh
    ''' WindowUnitConfiguration snapshot on every edit, the Unit dependency
    ''' property changes, and OnUnitChanged re-renders. No refresh button, no
    ''' events from the ViewModel.
    ''' </summary>
    Partial Public Class WindowPreviewControl
        Inherits UserControl

        ' ------------------------------------------------------------------
        ' Unit dependency property - the control's single input.
        ' VB idiom: dependency property registration looks just like C#; note
        ' AddressOf to pass the change callback method as a delegate.
        ' ------------------------------------------------------------------

        Public Shared ReadOnly UnitProperty As DependencyProperty =
            DependencyProperty.Register(NameOf(Unit), GetType(WindowUnitConfiguration),
                                        GetType(WindowPreviewControl),
                                        New PropertyMetadata(Nothing, AddressOf OnUnitChanged))

        ''' <summary>The configuration to draw. CLR wrapper over the dependency property.</summary>
        Public Property Unit As WindowUnitConfiguration
            Get
                Return DirectCast(GetValue(UnitProperty), WindowUnitConfiguration)
            End Get
            Set(value As WindowUnitConfiguration)
                SetValue(UnitProperty, value)
            End Set
        End Property

        Private Shared Sub OnUnitChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            DirectCast(d, WindowPreviewControl).Render()
        End Sub

        ' ------------------------------------------------------------------
        ' Drawing palette (frozen = immutable + cheap to share).
        ' ------------------------------------------------------------------

        Private Shared ReadOnly FrameFill As New Dictionary(Of FrameMaterial, Brush) From {
            {FrameMaterial.Vinyl, Frozen("#F5F5F2")},
            {FrameMaterial.Aluminum, Frozen("#C9CFD6")},
            {FrameMaterial.Wood, Frozen("#B98A4E")},
            {FrameMaterial.Fiberglass, Frozen("#DAD7CD")}
        }

        Private Shared ReadOnly FrameStroke As Brush = Frozen("#475569")
        Private Shared ReadOnly GlassStroke As Brush = Frozen("#7FA8B8C4")
        Private Shared ReadOnly MullFill As Brush = Frozen("#334155")
        Private Shared ReadOnly SymbolStroke As Brush = Frozen("#64748B")
        Private Shared ReadOnly DimStroke As Brush = Frozen("#94A3B8")
        Private Shared ReadOnly DimText As Brush = Frozen("#475569")
        Private Shared ReadOnly DimTextBackground As Brush = Frozen("#F2FFFFFF")

        Private Shared Function Frozen(hex As String) As Brush
            Dim brush As New SolidColorBrush(CType(ColorConverter.ConvertFromString(hex), Color))
            brush.Freeze()
            Return brush
        End Function

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' VB idiom: 'Handles Me.SizeChanged' declaratively subscribes this
        ''' method to the control's own event - the code-behind equivalent of
        ''' wiring SizeChanged="..." in XAML, with no delegate bookkeeping.
        ''' </summary>
        Private Sub OnControlSizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
            Render()
        End Sub

        ' ==================================================================
        ' Rendering
        ' ==================================================================

        Private Sub Render()
            DrawCanvas.Children.Clear()

            Dim unit = Me.Unit
            If unit Is Nothing OrElse unit.Lites.Count = 0 Then Return

            Dim canvasW = ActualWidth
            Dim canvasH = ActualHeight
            If canvasW < 80 OrElse canvasH < 80 Then Return ' too small to draw meaningfully

            ' Sanitize sizes so half-typed/invalid values can't break the math.
            Dim widths = unit.Lites.Select(Function(l) Math.Max(l.WidthInches, 6.0)).ToList()
            Dim heights = unit.Lites.Select(Function(l) Math.Max(l.HeightInches, 6.0)).ToList()
            Dim totalWidthIn = widths.Sum()
            Dim maxHeightIn = heights.Max()

            ' Reserve margins for the dimension lines (extra bottom row for
            ' per-lite widths on mulled units).
            Const MarginLeft = 64.0, MarginRight = 20.0, MarginTop = 20.0
            Dim marginBottom = If(unit.Lites.Count > 1, 84.0, 56.0)

            Dim scale = Math.Min((canvasW - MarginLeft - MarginRight) / totalWidthIn,
                                 (canvasH - MarginTop - marginBottom) / maxHeightIn)
            If scale <= 0 OrElse Double.IsInfinity(scale) Then Return

            Dim drawW = totalWidthIn * scale
            Dim drawH = maxHeightIn * scale
            Dim originX = MarginLeft + (canvasW - MarginLeft - MarginRight - drawW) / 2
            Dim originY = MarginTop + (canvasH - MarginTop - marginBottom - drawH) / 2

            ' --- Lites, left to right, bottom-aligned on a common sill line.
            Dim x = originX
            For i = 0 To unit.Lites.Count - 1
                Dim liteW = widths(i) * scale
                Dim liteH = heights(i) * scale
                Dim y = originY + drawH - liteH

                DrawLite(unit.Lites(i), unit.FrameMaterial, x, y, liteW, liteH, scale)

                ' Mull bar over the seam between this lite and the previous one.
                If i > 0 Then
                    Dim seamH = Math.Min(liteH, heights(i - 1) * scale)
                    AddRect(x - 2.5, originY + drawH - seamH, 5, seamH, MullFill, Nothing, 0)
                End If

                x += liteW
            Next

            ' --- Dimension lines.
            DrawHorizontalDimension(originX, originX + drawW, originY + drawH + 20, totalWidthIn)
            If unit.Lites.Count > 1 Then
                Dim dimX = originX
                For i = 0 To unit.Lites.Count - 1
                    Dim liteW = widths(i) * scale
                    DrawHorizontalDimension(dimX, dimX + liteW, originY + drawH + 48, widths(i))
                    dimX += liteW
                Next
            End If
            DrawVerticalDimension(originX - 22, originY, originY + drawH, maxHeightIn)
        End Sub

        ''' <summary>Draws one lite: outer frame, then style-specific sashes/symbols.</summary>
        Private Sub DrawLite(lite As LiteConfiguration, material As FrameMaterial,
                             x As Double, y As Double, w As Double, h As Double, scale As Double)

            Dim frameBrush = FrameFill(material)
            Dim tintBrush = TintToBrushConverter.GetBrush(lite.Tint)

            ' Outer frame (2.25" profile, but never thinner than 3px on screen).
            Dim frameT = Math.Max(2.25 * scale, 3.0)
            AddRect(x, y, w, h, frameBrush, FrameStroke, 1.2)

            Dim gx = x + frameT, gy = y + frameT
            Dim gw = w - 2 * frameT, gh = h - 2 * frameT
            If gw < 4 OrElse gh < 4 Then Return

            Select Case lite.FrameType
                Case FrameType.Fixed
                    ' Direct-glazed: glass fills the frame opening; no sash.
                    AddRect(gx, gy, gw, gh, tintBrush, GlassStroke, 1.0)
                    DrawGrid(lite, gx, gy, gw, gh, scale, frameBrush)

                Case FrameType.Casement
                    DrawSashPanel(lite, gx, gy, gw, gh, scale, frameBrush, tintBrush)
                    ' Operation symbol: dashed 'V' with the apex at the hinge
                    ' side (standard shop-drawing convention).
                    AddDashedPolyline(New Point(gx + gw, gy),
                                      New Point(gx, gy + gh / 2),
                                      New Point(gx + gw, gy + gh))

                Case FrameType.Awning
                    DrawSashPanel(lite, gx, gy, gw, gh, scale, frameBrush, tintBrush)
                    ' Awning: hinged at the top, so the apex points up.
                    AddDashedPolyline(New Point(gx, gy + gh),
                                      New Point(gx + gw / 2, gy),
                                      New Point(gx + gw, gy + gh))

                Case FrameType.DoubleHung
                    ' Two stacked sashes with a check rail between them.
                    Dim railH = Math.Max(1.5 * scale, 3.0)
                    Dim sashH = (gh - railH) / 2
                    If sashH > 4 Then
                        DrawSashPanel(lite, gx, gy, gw, sashH, scale, frameBrush, tintBrush)
                        AddRect(gx, gy + sashH, gw, railH, frameBrush, FrameStroke, 0.8)
                        DrawSashPanel(lite, gx, gy + sashH + railH, gw, sashH, scale, frameBrush, tintBrush)
                    End If

                Case FrameType.Slider
                    ' Two side-by-side panels with a vertical meeting rail.
                    Dim railW = Math.Max(1.5 * scale, 3.0)
                    Dim sashW = (gw - railW) / 2
                    If sashW > 4 Then
                        DrawSashPanel(lite, gx, gy, sashW, gh, scale, frameBrush, tintBrush)
                        AddRect(gx + sashW, gy, railW, gh, frameBrush, FrameStroke, 0.8)
                        DrawSashPanel(lite, gx + sashW + railW, gy, sashW, gh, scale, frameBrush, tintBrush)
                        ' Slide-direction arrow.
                        AddDashedPolyline(New Point(gx + sashW * 0.25, gy + gh / 2),
                                          New Point(gx + sashW * 0.75, gy + gh / 2))
                    End If
            End Select
        End Sub

        ''' <summary>A sash: thin frame-colored border with tinted glass and grids inside.</summary>
        Private Sub DrawSashPanel(lite As LiteConfiguration,
                                  x As Double, y As Double, w As Double, h As Double,
                                  scale As Double, frameBrush As Brush, tintBrush As Brush)
            Dim sashT = Math.Max(1.25 * scale, 2.0)
            AddRect(x, y, w, h, frameBrush, FrameStroke, 0.9)

            Dim ix = x + sashT, iy = y + sashT
            Dim iw = w - 2 * sashT, ih = h - 2 * sashT
            If iw < 3 OrElse ih < 3 Then Return

            AddRect(ix, iy, iw, ih, tintBrush, GlassStroke, 0.8)
            DrawGrid(lite, ix, iy, iw, ih, scale, frameBrush)
        End Sub

        ''' <summary>Colonial: even rows × columns. Prairie: lines inset 4" from each edge.</summary>
        Private Sub DrawGrid(lite As LiteConfiguration,
                             x As Double, y As Double, w As Double, h As Double,
                             scale As Double, muntinBrush As Brush)
            Dim muntinT = Math.Max(0.75 * scale, 1.5)

            Select Case lite.GridPattern
                Case GridPattern.Colonial
                    Dim rows = Math.Max(1, lite.GridRows)
                    Dim cols = Math.Max(1, lite.GridColumns)
                    For r = 1 To rows - 1
                        Dim ly = y + h * r / rows
                        AddRect(x, ly - muntinT / 2, w, muntinT, muntinBrush, Nothing, 0)
                    Next
                    For c = 1 To cols - 1
                        Dim lx = x + w * c / cols
                        AddRect(lx - muntinT / 2, y, muntinT, h, muntinBrush, Nothing, 0)
                    Next

                Case GridPattern.Prairie
                    Dim inset = 4 * scale ' the classic 4-inch prairie border
                    If w > 3 * inset AndAlso h > 3 * inset Then
                        AddRect(x + inset, y, muntinT, h, muntinBrush, Nothing, 0)
                        AddRect(x + w - inset - muntinT, y, muntinT, h, muntinBrush, Nothing, 0)
                        AddRect(x, y + inset, w, muntinT, muntinBrush, Nothing, 0)
                        AddRect(x, y + h - inset - muntinT, w, muntinT, muntinBrush, Nothing, 0)
                    End If
            End Select
        End Sub

        ' ------------------------------------------------------------------
        ' Dimension lines (architectural style: ticks + centered label).
        ' ------------------------------------------------------------------

        Private Sub DrawHorizontalDimension(x1 As Double, x2 As Double, y As Double, inches As Double)
            AddLine(x1, y, x2, y, DimStroke, 1.0)
            ' Extension guides up to the drawing, and 45° ticks at the ends.
            AddLine(x1, y - 6, x1, y + 4, DimStroke, 1.0)
            AddLine(x2, y - 6, x2, y + 4, DimStroke, 1.0)
            AddLine(x1 - 3, y + 3, x1 + 3, y - 3, DimStroke, 1.2)
            AddLine(x2 - 3, y + 3, x2 + 3, y - 3, DimStroke, 1.2)

            AddDimensionLabel(FormatInches(inches), (x1 + x2) / 2, y, rotated:=False)
        End Sub

        Private Sub DrawVerticalDimension(x As Double, y1 As Double, y2 As Double, inches As Double)
            AddLine(x, y1, x, y2, DimStroke, 1.0)
            AddLine(x - 4, y1, x + 6, y1, DimStroke, 1.0)
            AddLine(x - 4, y2, x + 6, y2, DimStroke, 1.0)
            AddLine(x - 3, y1 + 3, x + 3, y1 - 3, DimStroke, 1.2)
            AddLine(x - 3, y2 + 3, x + 3, y2 - 3, DimStroke, 1.2)

            AddDimensionLabel(FormatInches(inches), x, (y1 + y2) / 2, rotated:=True)
        End Sub

        Private Sub AddDimensionLabel(text As String, centerX As Double, centerY As Double, rotated As Boolean)
            Dim label As New TextBlock With {
                .Text = text,
                .FontSize = 11,
                .Foreground = DimText,
                .Background = DimTextBackground,
                .Padding = New Thickness(3, 1, 3, 1)
            }
            If rotated Then
                ' LayoutTransform (not RenderTransform) so DesiredSize is the
                ' *rotated* size and centering math below stays correct.
                label.LayoutTransform = New RotateTransform(-90)
            End If

            label.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
            Canvas.SetLeft(label, centerX - label.DesiredSize.Width / 2)
            Canvas.SetTop(label, centerY - label.DesiredSize.Height / 2)
            DrawCanvas.Children.Add(label)
        End Sub

        Private Shared Function FormatInches(inches As Double) As String
            ' 34.5 -> 34.5"  (ChrW(34) is the double-quote character; embedding
            ' it via "" escaping inside an interpolated string gets unreadable).
            Return inches.ToString("0.#") & ChrW(34)
        End Function

        ' ------------------------------------------------------------------
        ' Shape helpers
        ' ------------------------------------------------------------------

        Private Function AddRect(x As Double, y As Double, w As Double, h As Double,
                                 fill As Brush, stroke As Brush, strokeThickness As Double) As Rectangle
            Dim rect As New Rectangle With {
                .Width = Math.Max(w, 0),
                .Height = Math.Max(h, 0),
                .Fill = fill,
                .Stroke = stroke,
                .StrokeThickness = strokeThickness,
                .SnapsToDevicePixels = True
            }
            Canvas.SetLeft(rect, x)
            Canvas.SetTop(rect, y)
            DrawCanvas.Children.Add(rect)
            Return rect
        End Function

        Private Sub AddLine(x1 As Double, y1 As Double, x2 As Double, y2 As Double,
                            stroke As Brush, thickness As Double)
            DrawCanvas.Children.Add(New Line With {
                .X1 = x1, .Y1 = y1, .X2 = x2, .Y2 = y2,
                .Stroke = stroke,
                .StrokeThickness = thickness,
                .SnapsToDevicePixels = True
            })
        End Sub

        ''' <summary>Dashed polyline used for the operation symbols.</summary>
        ''' <remarks>VB idiom: ParamArray = C# 'params'; callers pass a comma list.</remarks>
        Private Sub AddDashedPolyline(ParamArray points() As Point)
            Dim polyline As New Polyline With {
                .Stroke = SymbolStroke,
                .StrokeThickness = 1.2,
                .StrokeDashArray = New DoubleCollection({4.0, 3.0})
            }
            For Each p In points
                polyline.Points.Add(p)
            Next
            DrawCanvas.Children.Add(polyline)
        End Sub

    End Class

End Namespace
