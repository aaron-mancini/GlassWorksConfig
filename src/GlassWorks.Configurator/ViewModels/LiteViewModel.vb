Imports System.ComponentModel
Imports GlassWorks.Core.Models

Namespace ViewModels

    ''' <summary>
    ''' Editable wrapper around one lite's configuration. This is where user
    ''' input lives: change notification for live preview/pricing, and
    ''' IDataErrorInfo validation for min/max sizes per frame type.
    ''' The plain model (LiteConfiguration) is produced on demand via
    ''' ToConfiguration() and handed to the services.
    ''' </summary>
    Public Class LiteViewModel
        Inherits ViewModelBase
        Implements IDataErrorInfo

        Private _displayName As String = "Lite"
        Private _frameType As FrameType = FrameType.Casement
        Private _widthInches As Double = 24
        Private _heightInches As Double = 48
        Private _gridPattern As GridPattern = GridPattern.None
        Private _gridRows As Integer = 2
        Private _gridColumns As Integer = 2
        Private _glassType As GlassType = GlassType.DoublePane
        Private _tint As GlassTint = GlassTint.Clear
        Private _hasScreen As Boolean = False

        ''' <summary>Position label ("Lite 2 · Casement"), maintained by MainViewModel.</summary>
        Public Property DisplayName As String
            Get
                Return _displayName
            End Get
            Set(value As String)
                SetProperty(_displayName, value)
            End Set
        End Property

        Public Property FrameType As FrameType
            Get
                Return _frameType
            End Get
            Set(value As FrameType)
                If SetProperty(_frameType, value) Then
                    ' The size limits depend on the frame type, so a type change
                    ' must re-run validation on width/height. Re-raising their
                    ' PropertyChanged makes WPF re-query IDataErrorInfo for them.
                    OnPropertyChanged(NameOf(WidthInches))
                    OnPropertyChanged(NameOf(HeightInches))
                    OnPropertyChanged(NameOf(SizeRangeHint))
                    NotifySummaryAndErrorChanged()
                End If
            End Set
        End Property

        Public Property WidthInches As Double
            Get
                Return _widthInches
            End Get
            Set(value As Double)
                If SetProperty(_widthInches, value) Then NotifySummaryAndErrorChanged()
            End Set
        End Property

        Public Property HeightInches As Double
            Get
                Return _heightInches
            End Get
            Set(value As Double)
                If SetProperty(_heightInches, value) Then NotifySummaryAndErrorChanged()
            End Set
        End Property

        Public Property GridPattern As GridPattern
            Get
                Return _gridPattern
            End Get
            Set(value As GridPattern)
                If SetProperty(_gridPattern, value) Then
                    OnPropertyChanged(NameOf(IsColonial))
                    NotifySummaryAndErrorChanged()
                End If
            End Set
        End Property

        Public Property GridRows As Integer
            Get
                Return _gridRows
            End Get
            Set(value As Integer)
                If SetProperty(_gridRows, value) Then NotifySummaryAndErrorChanged()
            End Set
        End Property

        Public Property GridColumns As Integer
            Get
                Return _gridColumns
            End Get
            Set(value As Integer)
                If SetProperty(_gridColumns, value) Then NotifySummaryAndErrorChanged()
            End Set
        End Property

        Public Property GlassType As GlassType
            Get
                Return _glassType
            End Get
            Set(value As GlassType)
                SetProperty(_glassType, value)
            End Set
        End Property

        Public Property Tint As GlassTint
            Get
                Return _tint
            End Get
            Set(value As GlassTint)
                SetProperty(_tint, value)
            End Set
        End Property

        Public Property HasScreen As Boolean
            Get
                Return _hasScreen
            End Get
            Set(value As Boolean)
                If SetProperty(_hasScreen, value) Then
                    OnPropertyChanged(NameOf(HasScreen))
                    NotifySummaryAndErrorChanged()
                End If
            End Set
        End Property

        ''' <summary>Drives visibility of the rows/columns editor in the view.</summary>
        Public ReadOnly Property IsColonial As Boolean
            Get
                Return _gridPattern = GridPattern.Colonial
            End Get
        End Property

        ''' <summary>"W 14–36 in, H 24–72 in" hint under the size fields.</summary>
        Public ReadOnly Property SizeRangeHint As String
            Get
                Return "Allowed: " & FrameTypeCatalog.GetSpec(_frameType).SizeRangeText
            End Get
        End Property

        ''' <summary>Short size text for the lite chip in the list.</summary>
        Public ReadOnly Property SummaryText As String
            Get
                Return $"{_widthInches:0.#} × {_heightInches:0.#} in"
            End Get
        End Property

        Private Sub NotifySummaryAndErrorChanged()
            OnPropertyChanged(NameOf(SummaryText))
            ' 'Error' must be re-raised so the aggregate message under the editor
            ' updates as the user types.
            OnPropertyChanged("Error")
        End Sub

        ' ------------------------------------------------------------------
        ' IDataErrorInfo - WPF's simplest validation hook. Bindings that set
        ' ValidatesOnDataErrors=True call Item(propertyName) after every source
        ' update; a non-empty return marks the control invalid (red adorner).
        ' ------------------------------------------------------------------

        ''' <summary>
        ''' VB idiom: this is a *parameterized property* implementing the
        ''' interface's indexer. 'Default' makes it the class's default property
        ''' (the closest VB gets to C#'s 'this[string]' indexer syntax).
        ''' </summary>
        Default Public ReadOnly Property Item(columnName As String) As String _
            Implements IDataErrorInfo.Item
            Get
                Return ValidateProperty(columnName)
            End Get
        End Property

        ''' <summary>
        ''' VB idiom: 'Error' is a reserved word in VB (the legacy 'Error'
        ''' statement), so the member name must be escaped with square brackets.
        ''' Bindings still reference it as plain "Error".
        ''' </summary>
        Public ReadOnly Property [Error] As String Implements IDataErrorInfo.Error
            Get
                Dim messages = ValidatedPropertyNames.
                    Select(AddressOf ValidateProperty).
                    Where(Function(m) Not String.IsNullOrEmpty(m))
                ' VB idiom: 'AddressOf' creates a delegate from a method group
                ' (C# just names the method). Required whenever you pass a
                ' method rather than a lambda.
                Return String.Join(Environment.NewLine, messages)
            End Get
        End Property

        Private Shared ReadOnly ValidatedPropertyNames As String() =
            {NameOf(WidthInches), NameOf(HeightInches), NameOf(GridRows), NameOf(GridColumns)}

        ''' <summary>True when any validated property is out of range.</summary>
        Public ReadOnly Property HasValidationErrors As Boolean
            Get
                Return ValidatedPropertyNames.Any(Function(name) Not String.IsNullOrEmpty(ValidateProperty(name)))
            End Get
        End Property

        Public ReadOnly Property CanHaveScreen As Boolean
            Get
                OnPropertyChanged(NameOf(FrameType))
                Return FrameTypeCatalog.GetSpec(FrameType).SupportsScreen
            End Get
        End Property

        Private Function ValidateProperty(columnName As String) As String
            Dim spec = FrameTypeCatalog.GetSpec(_frameType)
            Dim typeName = _frameType.GetDescription()

            Select Case columnName
                Case NameOf(WidthInches)
                    If _widthInches < spec.MinWidthInches OrElse _widthInches > spec.MaxWidthInches Then
                        Return $"Width must be {spec.MinWidthInches:0}–{spec.MaxWidthInches:0} in for a {typeName} unit."
                    End If

                Case NameOf(HeightInches)
                    If _heightInches < spec.MinHeightInches OrElse _heightInches > spec.MaxHeightInches Then
                        Return $"Height must be {spec.MinHeightInches:0}–{spec.MaxHeightInches:0} in for a {typeName} unit."
                    End If

                Case NameOf(GridRows)
                    If IsColonial AndAlso (_gridRows < 1 OrElse _gridRows > 8) Then
                        Return "Grid rows must be between 1 and 8."
                    End If

                Case NameOf(GridColumns)
                    If IsColonial AndAlso (_gridColumns < 1 OrElse _gridColumns > 8) Then
                        Return "Grid columns must be between 1 and 8."
                    End If
            End Select

            Return String.Empty ' Empty string / Nothing both mean "valid".
        End Function

        ''' <summary>Snapshot of the current state as a plain model for the services.</summary>
        Public Function ToConfiguration() As LiteConfiguration
            Return New LiteConfiguration With {
                .FrameType = _frameType,
                .WidthInches = _widthInches,
                .HeightInches = _heightInches,
                .GridPattern = _gridPattern,
                .GridRows = _gridRows,
                .GridColumns = _gridColumns,
                .GlassType = _glassType,
                .Tint = _tint,
                .HasScreen = _hasScreen
            }
        End Function

    End Class

End Namespace
