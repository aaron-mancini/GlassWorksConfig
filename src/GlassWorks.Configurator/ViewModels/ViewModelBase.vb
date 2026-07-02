Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Namespace ViewModels

    ''' <summary>
    ''' Base class for all ViewModels: implements INotifyPropertyChanged once so
    ''' derived classes get change notification with one line per setter.
    ''' </summary>
    Public MustInherit Class ViewModelBase
        Implements INotifyPropertyChanged
        ' VB idiom: 'MustInherit' = C# 'abstract' (for classes).
        ' Related keywords: NotInheritable = sealed, MustOverride = abstract
        ' member, Overridable = virtual, NotOverridable = sealed member.

        ''' <summary>
        ''' VB idiom: events are declared with 'Public Event ... Implements ...'.
        ''' Unlike C#, you never touch the delegate field directly; you raise the
        ''' event with the RaiseEvent statement, and VB handles the null check
        ''' for you (no '?.Invoke' dance).
        ''' </summary>
        Public Event PropertyChanged As PropertyChangedEventHandler _
            Implements INotifyPropertyChanged.PropertyChanged

        ''' <summary>
        ''' Raises PropertyChanged. &lt;CallerMemberName&gt; fills in the calling
        ''' property's name automatically, exactly as in C#.
        ''' </summary>
        Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub

        ''' <summary>
        ''' Standard MVVM setter helper: assigns the backing field and notifies
        ''' only when the value actually changed. Returns True when it did, so
        ''' setters can chain follow-up notifications.
        ''' </summary>
        ''' <remarks>
        ''' VB idiom: 'ByRef' passes the backing field by reference (C# 'ref').
        ''' At the call site VB needs no keyword - 'SetProperty(_width, value)'
        ''' silently passes _width by reference because the parameter says so.
        ''' </remarks>
        Protected Function SetProperty(Of T)(ByRef field As T, value As T,
                                             <CallerMemberName> Optional propertyName As String = Nothing) As Boolean
            If EqualityComparer(Of T).Default.Equals(field, value) Then Return False
            field = value
            OnPropertyChanged(propertyName)
            Return True
        End Function

    End Class

End Namespace
