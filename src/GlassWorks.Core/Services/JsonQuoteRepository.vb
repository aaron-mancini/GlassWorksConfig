Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GlassWorks.Core.Models

Namespace Services

    ''' <summary>
    ''' Stores the working quote as pretty-printed JSON under the user's
    ''' application-data folder (or a custom path, which the unit tests use).
    ''' </summary>
    Public Class JsonQuoteRepository
        Implements IQuoteRepository

        Private ReadOnly _filePath As String

        ' Shared serializer options: enums as their names ("Casement", not 2) so
        ' the JSON on disk stays readable and resilient to enum reordering.
        Private Shared ReadOnly SerializerOptions As New JsonSerializerOptions With {
            .WriteIndented = True
        }

        ' VB idiom: Shared Sub New() is the static constructor (C#: static ClassName()).
        Shared Sub New()
            SerializerOptions.Converters.Add(New JsonStringEnumConverter())
        End Sub

        ''' <param name="filePath">
        ''' Optional override of the storage location; defaults to
        ''' %APPDATA%\GlassWorks Configurator\current-quote.json.
        ''' </param>
        Public Sub New(Optional filePath As String = Nothing)
            _filePath = If(filePath,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                             "GlassWorks Configurator", "current-quote.json"))
        End Sub

        Public Function Load() As Quote Implements IQuoteRepository.Load
            Try
                If Not File.Exists(_filePath) Then Return New Quote()
                Dim json = File.ReadAllText(_filePath)
                ' If(x, fallback) = null-coalescing: a corrupt "null" file still
                ' yields a usable empty quote.
                Return If(JsonSerializer.Deserialize(Of Quote)(json, SerializerOptions), New Quote())
            Catch ex As Exception When TypeOf ex Is JsonException OrElse TypeOf ex Is IOException
                ' VB idiom: 'Catch ... When <condition>' is an exception filter
                ' (C# 'catch when'). A corrupt or unreadable file should not
                ' prevent the app from starting - fall back to an empty quote.
                Return New Quote()
            End Try
        End Function

        Public Sub Save(quote As Quote) Implements IQuoteRepository.Save
            Dim directory = Path.GetDirectoryName(_filePath)
            If Not String.IsNullOrEmpty(directory) Then
                IO.Directory.CreateDirectory(directory)
            End If
            File.WriteAllText(_filePath, JsonSerializer.Serialize(quote, SerializerOptions))
        End Sub

    End Class

End Namespace
