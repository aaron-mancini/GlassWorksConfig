Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Windows.Input
Imports GlassWorks.Configurator.Services
Imports GlassWorks.Core.Models
Imports GlassWorks.Core.Services

Namespace ViewModels

    ''' <summary>
    ''' The application's root ViewModel. Owns:
    '''   - the lites being configured (and which one is selected),
    '''   - the live snapshot (CurrentUnit) that drives the 2D preview,
    '''   - the live price breakdown and cut list,
    '''   - the quote (loaded at startup, auto-saved on every change).
    ''' All dependencies arrive as interfaces through the constructor
    ''' (constructor injection) - see Application.xaml.vb for the wiring.
    ''' </summary>
    Public Class MainViewModel
        Inherits ViewModelBase

        Private ReadOnly _pricing As IPricingService
        Private ReadOnly _bomService As IBomService
        Private ReadOnly _repository As IQuoteRepository
        Private ReadOnly _exporter As IQuoteExporter
        Private ReadOnly _dialogs As IDialogService

        ' VB idiom: 'WithEvents' - the flagship VB event feature. Declaring a
        ' field WithEvents lets methods bind to its events *declaratively* with
        ' a 'Handles' clause (see OnLitesCollectionChanged below). Assigning a
        ' new object to a WithEvents field automatically unhooks the old
        ' object's handlers and hooks the new one - plumbing C# forces you to
        ' write by hand with += / -=.
        Private WithEvents _lites As ObservableCollection(Of LiteViewModel)
        Private WithEvents _quoteItems As ObservableCollection(Of QuoteLineItemViewModel)

        ''' <summary>The persisted quote model; the item VMs wrap entries of its Items list.</summary>
        Private _quote As Quote

        Private _selectedLite As LiteViewModel
        Private _currentUnit As WindowUnitConfiguration
        Private _breakdown As PriceBreakdown = PriceBreakdown.Empty
        Private _bomItems As IReadOnlyList(Of BomItem) = Array.Empty(Of BomItem)()
        Private _frameMaterial As FrameMaterial = FrameMaterial.Vinyl
        Private _quoteQuantity As Integer = 1
        Private _statusMessage As String = "Ready."
        Private _isLoadingQuote As Boolean

        Public Sub New(pricing As IPricingService,
                       bomService As IBomService,
                       repository As IQuoteRepository,
                       exporter As IQuoteExporter,
                       dialogs As IDialogService)
            _pricing = pricing
            _bomService = bomService
            _repository = repository
            _exporter = exporter
            _dialogs = dialogs

            _lites = New ObservableCollection(Of LiteViewModel)()
            _quoteItems = New ObservableCollection(Of QuoteLineItemViewModel)()

            ' Commands: every button in the UI routes here - no click handlers
            ' exist anywhere in code-behind.
            AddLiteCommand = New RelayCommand(Sub(p) AddLite())
            RemoveLiteCommand = New RelayCommand(Sub(p) RemoveSelectedLite(),
                                                 Function(p) _lites.Count > 1 AndAlso SelectedLite IsNot Nothing)
            AddToQuoteCommand = New RelayCommand(Sub(p) AddCurrentUnitToQuote(),
                                                 Function(p) ConfigurationIsValid())
            RemoveQuoteItemCommand = New RelayCommand(Sub(p) RemoveQuoteItem(TryCast(p, QuoteLineItemViewModel)),
                                                      Function(p) TypeOf p Is QuoteLineItemViewModel)
            NewQuoteCommand = New RelayCommand(Sub(p) StartNewQuote(),
                                               Function(p) _quoteItems.Count > 0)
            ExportCsvCommand = New RelayCommand(Sub(p) ExportQuote(asCsv:=True),
                                                Function(p) _quoteItems.Count > 0)
            ExportTextCommand = New RelayCommand(Sub(p) ExportQuote(asCsv:=False),
                                                 Function(p) _quoteItems.Count > 0)

            LoadSavedQuote()

            ' Seed the designer with one default lite so the app opens alive.
            AddLite()
        End Sub

        ' ------------------------------------------------------------------
        ' Configuration state
        ' ------------------------------------------------------------------

        Public ReadOnly Property Lites As ObservableCollection(Of LiteViewModel)
            Get
                Return _lites
            End Get
        End Property

        Public Property SelectedLite As LiteViewModel
            Get
                Return _selectedLite
            End Get
            Set(value As LiteViewModel)
                SetProperty(_selectedLite, value)
            End Set
        End Property

        ''' <summary>One material for the whole unit (mulled lites share extrusion stock).</summary>
        Public Property FrameMaterial As FrameMaterial
            Get
                Return _frameMaterial
            End Get
            Set(value As FrameMaterial)
                If SetProperty(_frameMaterial, value) Then Recalculate()
            End Set
        End Property

        ''' <summary>
        ''' Immutable snapshot of the whole configuration, rebuilt on every
        ''' change. The preview control binds its Unit dependency property to
        ''' this; replacing the instance is what triggers a redraw - the
        ''' ViewModel never talks to the Canvas.
        ''' </summary>
        Public Property CurrentUnit As WindowUnitConfiguration
            Get
                Return _currentUnit
            End Get
            Private Set(value As WindowUnitConfiguration)
                ' VB idiom: asymmetric accessor visibility is spelled on the
                ' accessor itself ('Private Set'), like C#'s 'private set;'.
                SetProperty(_currentUnit, value)
            End Set
        End Property

        Public Property Breakdown As PriceBreakdown
            Get
                Return _breakdown
            End Get
            Private Set(value As PriceBreakdown)
                SetProperty(_breakdown, value)
            End Set
        End Property

        Public Property BomItems As IReadOnlyList(Of BomItem)
            Get
                Return _bomItems
            End Get
            Private Set(value As IReadOnlyList(Of BomItem))
                SetProperty(_bomItems, value)
            End Set
        End Property

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        ' ------------------------------------------------------------------
        ' Commands (assigned once in the constructor; ReadOnly auto-properties
        ' may be written in a constructor, like C# get-only properties).
        ' ------------------------------------------------------------------

        Public ReadOnly Property AddLiteCommand As ICommand
        Public ReadOnly Property RemoveLiteCommand As ICommand
        Public ReadOnly Property AddToQuoteCommand As ICommand
        Public ReadOnly Property RemoveQuoteItemCommand As ICommand
        Public ReadOnly Property NewQuoteCommand As ICommand
        Public ReadOnly Property ExportCsvCommand As ICommand
        Public ReadOnly Property ExportTextCommand As ICommand

        ' ------------------------------------------------------------------
        ' Lite management
        ' ------------------------------------------------------------------

        Private Sub AddLite()
            Dim lite As New LiteViewModel()
            _lites.Add(lite)
            SelectedLite = lite
        End Sub

        Private Sub RemoveSelectedLite()
            If SelectedLite Is Nothing OrElse _lites.Count <= 1 Then Return
            Dim index = _lites.IndexOf(SelectedLite)
            _lites.Remove(SelectedLite)
            ' Keep a sensible selection so the editor never goes blank.
            SelectedLite = _lites(Math.Min(index, _lites.Count - 1))
        End Sub

        ''' <summary>
        ''' VB idiom: the 'Handles' clause. Because _lites is declared
        ''' WithEvents, this method subscribes to its CollectionChanged event
        ''' by declaration - no AddHandler call anywhere. This fires when lites
        ''' are added/removed (not when their properties change; that is the
        ''' per-item subscription below).
        ''' </summary>
        Private Sub OnLitesCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs) _
            Handles _lites.CollectionChanged

            ' VB idiom: AddHandler/RemoveHandler statements are C#'s += / -= on
            ' events - the *imperative* counterpart to Handles, needed here
            ' because collection items come and go at runtime.
            If e.OldItems IsNot Nothing Then
                For Each lite As LiteViewModel In e.OldItems
                    RemoveHandler lite.PropertyChanged, AddressOf OnLitePropertyChanged
                Next
            End If
            If e.NewItems IsNot Nothing Then
                For Each lite As LiteViewModel In e.NewItems
                    AddHandler lite.PropertyChanged, AddressOf OnLitePropertyChanged
                Next
            End If

            RefreshLiteNames()
            Recalculate()
        End Sub

        ''' <summary>Any edit to any lite reprices and redraws the whole unit.</summary>
        Private Sub OnLitePropertyChanged(sender As Object, e As PropertyChangedEventArgs)
            ' DisplayName is set *by* RefreshLiteNames - reacting to it would
            ' cause an infinite loop. "Error" is a validation echo of another
            ' property change that already triggered a recalculation.
            If e.PropertyName = NameOf(LiteViewModel.DisplayName) OrElse e.PropertyName = "Error" Then Return

            If e.PropertyName = NameOf(LiteViewModel.FrameType) Then RefreshLiteNames()
            Recalculate()
        End Sub

        Private Sub RefreshLiteNames()
            For i = 0 To _lites.Count - 1
                _lites(i).DisplayName = $"Lite {i + 1} · {_lites(i).FrameType.GetDescription()}"
            Next
        End Sub

        ' ------------------------------------------------------------------
        ' The heartbeat: rebuild snapshot -> reprice -> re-BOM.
        ' ------------------------------------------------------------------

        Private Sub Recalculate()
            Dim snapshot = BuildSnapshot()
            CurrentUnit = snapshot                       ' triggers preview redraw
            Breakdown = _pricing.CalculatePrice(snapshot) ' triggers price panel refresh
            BomItems = _bomService.GenerateBom(snapshot)  ' triggers cut-list refresh
        End Sub

        Private Function BuildSnapshot() As WindowUnitConfiguration
            Dim unit As New WindowUnitConfiguration With {.FrameMaterial = _frameMaterial}
            For Each lite In _lites
                unit.Lites.Add(lite.ToConfiguration())
            Next
            Return unit
        End Function

        Private Function ConfigurationIsValid() As Boolean
            Return _lites.Count > 0 AndAlso _lites.All(Function(l) Not l.HasValidationErrors)
        End Function

        ' ------------------------------------------------------------------
        ' Quote management
        ' ------------------------------------------------------------------

        Public ReadOnly Property QuoteItems As ObservableCollection(Of QuoteLineItemViewModel)
            Get
                Return _quoteItems
            End Get
        End Property

        Public ReadOnly Property QuoteNumber As String
            Get
                Return If(_quote?.QuoteNumber, "")
            End Get
        End Property

        ''' <summary>Quantity to use when adding the current unit to the quote.</summary>
        Public Property QuoteQuantity As Integer
            Get
                Return _quoteQuantity
            End Get
            Set(value As Integer)
                If value < 1 Then value = 1
                SetProperty(_quoteQuantity, value)
            End Set
        End Property

        Public ReadOnly Property GrandTotal As Decimal
            Get
                Return _quoteItems.Sum(Function(i) i.LineTotal)
            End Get
        End Property

        Private Sub LoadSavedQuote()
            _isLoadingQuote = True
            Try
                _quote = _repository.Load()
                For Each item In _quote.Items
                    _quoteItems.Add(New QuoteLineItemViewModel(item))
                Next
                If _quote.Items.Count > 0 Then
                    StatusMessage = $"Loaded saved quote {_quote.QuoteNumber} ({_quote.Items.Count} item(s))."
                End If
            Finally
                _isLoadingQuote = False
            End Try
        End Sub

        Private Sub AddCurrentUnitToQuote()
            Dim snapshot = BuildSnapshot()
            Dim item As New QuoteLineItem With {
                .Description = BuildDescription(snapshot),
                .Quantity = QuoteQuantity,
                .UnitPrice = Breakdown.Total,
                .Unit = snapshot ' BuildSnapshot already deep-copies the VM state
            }
            _quote.Items.Add(item)
            _quoteItems.Add(New QuoteLineItemViewModel(item))
            QuoteQuantity = 1
            PersistQuote($"Added ""{item.Description}"" to the quote.")
        End Sub

        Private Sub RemoveQuoteItem(item As QuoteLineItemViewModel)
            If item Is Nothing Then Return
            _quote.Items.Remove(item.Model)
            _quoteItems.Remove(item)
            PersistQuote("Line item removed.")
        End Sub

        Private Sub StartNewQuote()
            If Not _dialogs.Confirm("Discard the current quote and start a new one?", "New quote") Then Return
            _quote = New Quote()
            _quoteItems.Clear()
            PersistQuote($"Started new quote {_quote.QuoteNumber}.")
            OnPropertyChanged(NameOf(QuoteNumber))
        End Sub

        ''' <summary>Same WithEvents/Handles pattern for the quote list.</summary>
        Private Sub OnQuoteItemsCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs) _
            Handles _quoteItems.CollectionChanged

            If e.OldItems IsNot Nothing Then
                For Each item As QuoteLineItemViewModel In e.OldItems
                    RemoveHandler item.PropertyChanged, AddressOf OnQuoteItemPropertyChanged
                Next
            End If
            If e.NewItems IsNot Nothing Then
                For Each item As QuoteLineItemViewModel In e.NewItems
                    AddHandler item.PropertyChanged, AddressOf OnQuoteItemPropertyChanged
                Next
            End If

            OnPropertyChanged(NameOf(GrandTotal))
        End Sub

        ''' <summary>Editing a quantity in the list updates totals and auto-saves.</summary>
        Private Sub OnQuoteItemPropertyChanged(sender As Object, e As PropertyChangedEventArgs)
            If e.PropertyName = NameOf(QuoteLineItemViewModel.Quantity) OrElse
               e.PropertyName = NameOf(QuoteLineItemViewModel.LineTotal) Then
                OnPropertyChanged(NameOf(GrandTotal))
                PersistQuote(Nothing)
            End If
        End Sub

        ''' <summary>Auto-saves the quote; optional status text on success.</summary>
        Private Sub PersistQuote(successStatus As String)
            If _isLoadingQuote Then Return
            Try
                _repository.Save(_quote)
                If successStatus IsNot Nothing Then StatusMessage = successStatus
            Catch ex As Exception
                StatusMessage = "Could not save quote: " & ex.Message
            End Try
        End Sub

        Private Sub ExportQuote(asCsv As Boolean)
            Dim filter = If(asCsv,
                            "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                            "Text files (*.txt)|*.txt|All files (*.*)|*.*")
            Dim suggestedName = _quote.QuoteNumber & If(asCsv, ".csv", ".txt")

            Dim filePath = _dialogs.ShowSaveFileDialog("Export quote", filter, suggestedName)
            If String.IsNullOrEmpty(filePath) Then Return ' user cancelled

            Try
                If asCsv Then
                    _exporter.ExportCsv(_quote, filePath)
                Else
                    _exporter.ExportText(_quote, filePath)
                End If
                StatusMessage = "Quote exported to " & filePath
            Catch ex As Exception
                StatusMessage = "Export failed: " & ex.Message
            End Try
        End Sub

        ''' <summary>Human-readable one-liner describing a configured unit.</summary>
        Private Function BuildDescription(unit As WindowUnitConfiguration) As String
            Dim material = unit.FrameMaterial.GetDescription()
            If unit.Lites.Count = 1 Then
                Dim lite = unit.Lites(0)
                Return $"{lite.FrameType.GetDescription()} {lite.WidthInches:0.#}×{lite.HeightInches:0.#} in, " &
                       $"{lite.GlassType.GetDescription()}, {lite.Tint.GetDescription()}, {material}"
            End If

            Dim parts = unit.Lites.Select(Function(l) l.FrameType.GetDescription())
            Return $"Mulled unit ({String.Join(" + ", parts)}), " &
                   $"{unit.TotalWidthInches:0.#}×{unit.MaxHeightInches:0.#} in overall, {material}"
        End Function

    End Class

End Namespace
