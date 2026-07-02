Imports System.Windows.Input

Namespace ViewModels

    ''' <summary>
    ''' The classic MVVM command: wraps a delegate (and an optional can-execute
    ''' predicate) behind ICommand so XAML buttons bind to ViewModel behavior
    ''' with zero code-behind click handlers.
    ''' </summary>
    Public Class RelayCommand
        Implements ICommand

        Private ReadOnly _execute As Action(Of Object)
        Private ReadOnly _canExecute As Predicate(Of Object)

        ''' <param name="execute">What to do. VB lambdas: 'Sub(p) DoThing()'.</param>
        ''' <param name="canExecute">Optional gate; Nothing means always enabled.</param>
        Public Sub New(execute As Action(Of Object), Optional canExecute As Predicate(Of Object) = Nothing)
            ' VB idiom: 'Nothing' is C#'s 'null' (and also default(T) for value types).
            If execute Is Nothing Then Throw New ArgumentNullException(NameOf(execute))
            _execute = execute
            _canExecute = canExecute
        End Sub

        ''' <summary>
        ''' VB idiom worth studying: a 'Custom Event' lets you hand-implement the
        ''' add/remove/raise accessors of an event (C#'s 'event { add; remove; }').
        ''' Here subscriptions are forwarded to CommandManager.RequerySuggested,
        ''' so WPF automatically re-evaluates CanExecute after focus changes,
        ''' clicks, etc. - no manual RaiseCanExecuteChanged plumbing needed.
        ''' </summary>
        Public Custom Event CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged
            AddHandler(value As EventHandler)
                AddHandler CommandManager.RequerySuggested, value
            End AddHandler
            RemoveHandler(value As EventHandler)
                RemoveHandler CommandManager.RequerySuggested, value
            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)
                ' Raised by CommandManager; nothing to do locally.
            End RaiseEvent
        End Event

        Public Function CanExecute(parameter As Object) As Boolean Implements ICommand.CanExecute
            ' VB idiom: 'Is Nothing' for reference null checks; OrElse/AndAlso are
            ' the *short-circuiting* operators (bare Or/And always evaluate both sides).
            Return _canExecute Is Nothing OrElse _canExecute(parameter)
        End Function

        Public Sub Execute(parameter As Object) Implements ICommand.Execute
            _execute(parameter)
        End Sub

    End Class

End Namespace
