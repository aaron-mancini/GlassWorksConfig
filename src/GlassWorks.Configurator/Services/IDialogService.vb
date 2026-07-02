Namespace Services

    ''' <summary>
    ''' Abstraction over UI dialogs so the ViewModel never references WPF types
    ''' like SaveFileDialog or MessageBox directly. In a unit test this gets
    ''' replaced by a stub that returns canned answers.
    ''' </summary>
    Public Interface IDialogService

        ''' <summary>Shows a save-file dialog; returns the chosen path, or Nothing if cancelled.</summary>
        Function ShowSaveFileDialog(title As String, filter As String, suggestedFileName As String) As String

        ''' <summary>Yes/No confirmation; True when the user accepts.</summary>
        Function Confirm(message As String, caption As String) As Boolean

    End Interface

End Namespace
