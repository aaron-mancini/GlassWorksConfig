Imports System.Windows
Imports Microsoft.Win32

Namespace Services

    ''' <summary>Production implementation of IDialogService using real WPF dialogs.</summary>
    Public Class DialogService
        Implements IDialogService

        Public Function ShowSaveFileDialog(title As String, filter As String,
                                           suggestedFileName As String) As String _
            Implements IDialogService.ShowSaveFileDialog

            Dim dialog As New SaveFileDialog With {
                .Title = title,
                .Filter = filter,
                .FileName = suggestedFileName,
                .AddExtension = True
            }

            ' ShowDialog returns Boolean? (Nullable). GetValueOrDefault treats
            ' both Nothing and False as "cancelled".
            If dialog.ShowDialog().GetValueOrDefault() Then
                Return dialog.FileName
            End If
            Return Nothing
        End Function

        Public Function Confirm(message As String, caption As String) As Boolean _
            Implements IDialogService.Confirm

            Return MessageBox.Show(message, caption, MessageBoxButton.YesNo,
                                   MessageBoxImage.Question) = MessageBoxResult.Yes
        End Function

    End Class

End Namespace
