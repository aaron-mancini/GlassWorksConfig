Imports GlassWorks.Core.Models

Namespace Services

    ''' <summary>Exports a quote to shareable file formats.</summary>
    Public Interface IQuoteExporter

        ''' <summary>Writes the quote as CSV (opens straight into Excel).</summary>
        Sub ExportCsv(quote As Quote, filePath As String)

        ''' <summary>Writes the quote as a formatted plain-text document.</summary>
        Sub ExportText(quote As Quote, filePath As String)

    End Interface

End Namespace
