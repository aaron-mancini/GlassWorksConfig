Imports System.ComponentModel
Imports System.Reflection
Imports System.Runtime.CompilerServices

Namespace Models

    ''' <summary>
    ''' Reads the friendly display name from an enum value's Description attribute.
    ''' </summary>
    ''' <remarks>
    ''' VB idiom: a <c>Module</c> is VB's version of a C# <c>static class</c> -
    ''' all members are implicitly Shared (static) and the module's members can be
    ''' called without qualification from the importing file. Extension methods in
    ''' VB *must* live in a Module and be tagged with the
    ''' &lt;Extension&gt; attribute (C# instead uses the 'this' modifier on the
    ''' first parameter).
    ''' </remarks>
    Public Module EnumHelper

        ''' <summary>
        ''' Returns the Description attribute text for an enum value, or the raw
        ''' member name when no attribute is present.
        ''' </summary>
        <Extension>
        Public Function GetDescription(value As [Enum]) As String
            ' VB idiom: 'Enum' is a keyword, so to use the System.Enum *type* as an
            ' identifier it must be escaped in square brackets: [Enum].
            ' (C# has the equivalent @-prefix: @enum.)
            Dim field As FieldInfo = value.GetType().GetField(value.ToString())
            Dim attribute = field?.GetCustomAttribute(Of DescriptionAttribute)()

            ' VB idiom: If(a, b) is the null-coalescing operator (C#'s a ?? b).
            ' The same If(...) keyword with three arguments is the conditional
            ' operator (C#'s a ? b : c).
            Return If(attribute?.Description, value.ToString())
        End Function

    End Module

End Namespace
