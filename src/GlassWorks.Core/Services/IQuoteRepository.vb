Imports GlassWorks.Core.Models

Namespace Services

    ''' <summary>
    ''' Persistence boundary for the working quote. The app auto-saves the quote
    ''' after every change and loads it back on startup.
    ''' </summary>
    Public Interface IQuoteRepository

        ''' <summary>Loads the saved quote, or returns a fresh empty quote if none exists.</summary>
        Function Load() As Quote

        ''' <summary>Persists the quote (JSON on disk in the default implementation).</summary>
        Sub Save(quote As Quote)

    End Interface

End Namespace
