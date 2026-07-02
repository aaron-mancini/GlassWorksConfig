Imports System.Globalization
Imports System.IO
Imports System.Text
Imports GlassWorks.Core.Models

Namespace Services

    ''' <summary>
    ''' Renders a quote to CSV or aligned plain text. Uses the invariant culture
    ''' so exported files are deterministic regardless of the machine's locale.
    ''' </summary>
    Public Class QuoteExporter
        Implements IQuoteExporter

        Private Shared ReadOnly Invariant As CultureInfo = CultureInfo.InvariantCulture

        Public Sub ExportCsv(quote As Quote, filePath As String) Implements IQuoteExporter.ExportCsv
            Dim sb As New StringBuilder()
            sb.AppendLine("Line,Description,Quantity,Unit Price,Line Total")

            For i = 0 To quote.Items.Count - 1
                Dim item = quote.Items(i)
                sb.AppendLine(String.Join(","c,
                    (i + 1).ToString(Invariant),
                    CsvEscape(item.Description),
                    item.Quantity.ToString(Invariant),
                    item.UnitPrice.ToString("0.00", Invariant),
                    item.LineTotal.ToString("0.00", Invariant)))
            Next

            sb.AppendLine($",,,Grand Total,{quote.GrandTotal.ToString("0.00", Invariant)}")
            File.WriteAllText(filePath, sb.ToString())
        End Sub

        Public Sub ExportText(quote As Quote, filePath As String) Implements IQuoteExporter.ExportText
            Dim sb As New StringBuilder()
            sb.AppendLine("GLASSWORKS CONFIGURATOR — QUOTE")
            sb.AppendLine($"Quote #:  {quote.QuoteNumber}")
            sb.AppendLine($"Created:  {quote.CreatedUtc.ToString("yyyy-MM-dd HH:mm", Invariant)} UTC")
            sb.AppendLine(New String("="c, 78))
            ' VB idiom: "="c is a Char literal (C#: '='). Plain "=" would be a
            ' one-character String, which is a different type under Option Strict.

            For i = 0 To quote.Items.Count - 1
                Dim item = quote.Items(i)
                sb.AppendLine($"{i + 1,3}. {item.Description}")
                sb.AppendLine($"     {item.Quantity} × ${item.UnitPrice.ToString("N2", Invariant)}  =  ${item.LineTotal.ToString("N2", Invariant)}")
                sb.AppendLine()
            Next

            sb.AppendLine(New String("-"c, 78))
            sb.AppendLine($"{"GRAND TOTAL:",-20} ${quote.GrandTotal.ToString("N2", Invariant)}")
            File.WriteAllText(filePath, sb.ToString())
        End Sub

        ''' <summary>Minimal CSV quoting: wrap and double-up quotes when needed.</summary>
        Private Shared Function CsvEscape(value As String) As String
            If String.IsNullOrEmpty(value) Then Return ""
            ' VB idiom: inside a string literal, "" is an escaped double quote
            ' (C# uses \"). VB strings have no backslash escapes at all.
            If value.Contains(","c) OrElse value.Contains(""""c) OrElse value.Contains(vbLf) Then
                Return """" & value.Replace("""", """""") & """"
            End If
            Return value
        End Function

    End Class

End Namespace
