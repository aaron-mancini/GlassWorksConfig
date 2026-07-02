Imports System.Globalization
Imports System.Windows.Data
Imports GlassWorks.Core.Models

Namespace Converters

    ''' <summary>
    ''' Turns any enum value into its friendly Description-attribute text for
    ''' display (ComboBox items, chip labels). One-way by design: the controls
    ''' bind SelectedItem to the enum value itself, so nothing ever needs to be
    ''' converted back.
    ''' </summary>
    Public Class EnumDescriptionConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.Convert
            ' TryCast = C#'s 'as': Nothing when the cast fails (e.g. the
            ' placeholder value in an empty ComboBox).
            Dim enumValue = TryCast(value, [Enum])
            If enumValue Is Nothing Then Return If(value?.ToString(), String.Empty)
            Return enumValue.GetDescription()
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                    culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException("EnumDescriptionConverter is one-way (display only).")
        End Function

    End Class

End Namespace
