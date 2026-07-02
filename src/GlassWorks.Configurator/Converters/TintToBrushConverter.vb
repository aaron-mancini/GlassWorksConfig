Imports System.Globalization
Imports System.Windows.Data
Imports System.Windows.Media
Imports GlassWorks.Core.Models

Namespace Converters

    ''' <summary>
    ''' Maps a GlassTint enum value to the semi-transparent brush used to paint
    ''' glass. Used two ways: as a XAML IValueConverter (the tint swatch next to
    ''' the tint picker) and directly from the preview control's drawing code
    ''' via the shared GetBrush method - one source of truth for tint colors.
    ''' </summary>
    Public Class TintToBrushConverter
        Implements IValueConverter

        ' Frozen brushes are immutable and thread-safe, and skip a lot of WPF
        ' change-tracking overhead - always freeze shared drawing resources.
        Private Shared ReadOnly TintBrushes As New Dictionary(Of GlassTint, Brush) From {
            {GlassTint.Clear, MakeFrozenBrush("#66D6EAF2")},
            {GlassTint.LowE, MakeFrozenBrush("#7FA7D8CE")},
            {GlassTint.Bronze, MakeFrozenBrush("#7FB08D57")},
            {GlassTint.Gray, MakeFrozenBrush("#7F8B9098")}
        }

        Private Shared ReadOnly FallbackBrush As Brush = MakeFrozenBrush("#66D6EAF2")

        Private Shared Function MakeFrozenBrush(hex As String) As Brush
            ' CType here performs the unboxing conversion from Object to Color
            ' (Option Strict On requires it to be explicit).
            Dim brush As New SolidColorBrush(CType(ColorConverter.ConvertFromString(hex), Color))
            brush.Freeze()
            Return brush
        End Function

        ''' <summary>Shared lookup so drawing code can reuse the same palette.</summary>
        Public Shared Function GetBrush(tint As GlassTint) As Brush
            ' VB idiom: there is no 'out var'. TryGetValue's out parameter is
            ' just a ByRef parameter to VB, so the receiving variable must be
            ' declared beforehand and is passed with no keyword at the call site.
            Dim brush As Brush = Nothing
            If TintBrushes.TryGetValue(tint, brush) Then Return brush
            Return FallbackBrush
        End Function

        Public Function Convert(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.Convert
            If TypeOf value Is GlassTint Then
                ' VB idiom: 'TypeOf x Is T' is the type test (C#: 'x is T');
                ' CType then converts - VB has no pattern-matching 'is T t'.
                Return GetBrush(CType(value, GlassTint))
            End If
            Return FallbackBrush
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                    culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException("TintToBrushConverter is one-way (display only).")
        End Function

    End Class

End Namespace
