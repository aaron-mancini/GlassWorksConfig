Imports GlassWorks.Core.Models

Namespace ViewModels

    ''' <summary>
    ''' Static lists of enum values for ComboBox ItemsSource bindings, consumed
    ''' from XAML via {x:Static vm:EnumCatalog.FrameTypes}. Centralizing them
    ''' here avoids one ObjectDataProvider per enum in every view.
    ''' </summary>
    Public NotInheritable Class EnumCatalog

        ' VB idiom: a Private Sub New() hides the constructor - combined with
        ' NotInheritable (sealed) this is the pre-Module way to make a static
        ' class. A Module would work too, but x:Static in XAML reads better
        ' against a class with Shared properties.
        Private Sub New()
        End Sub

        ' VB idiom: 'Shared' = C# 'static'. Auto-properties can be initialized
        ' inline, like C# '{ get; } = ...'.
        Public Shared ReadOnly Property FrameTypes As IReadOnlyList(Of FrameType) = GetAll(Of FrameType)()
        Public Shared ReadOnly Property GridPatterns As IReadOnlyList(Of GridPattern) = GetAll(Of GridPattern)()
        Public Shared ReadOnly Property GlassTypes As IReadOnlyList(Of GlassType) = GetAll(Of GlassType)()
        Public Shared ReadOnly Property GlassTints As IReadOnlyList(Of GlassTint) = GetAll(Of GlassTint)()
        Public Shared ReadOnly Property FrameMaterials As IReadOnlyList(Of FrameMaterial) = GetAll(Of FrameMaterial)()

        Private Shared Function GetAll(Of T As Structure)() As IReadOnlyList(Of T)
            Return DirectCast([Enum].GetValues(GetType(T)), T()).ToList()
        End Function

    End Class

End Namespace
