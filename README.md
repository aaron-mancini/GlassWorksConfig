# GlassWorks Configurator

A parametric window / glass-unit configurator for window manufacturers and glass
fabricators, written in **VB.NET** on **.NET 8** with **WPF** and a strict
**MVVM** architecture. Configure a unit (frame type, size, glazing, tint, grids,
material, multi-lite mulled assemblies), watch it draw itself to scale with
dimension lines, get a live itemized price and a cut list, and build a quote
that persists to JSON and exports to CSV or text.

This is deliberately a *learning* codebase: it is commented far more generously
than production code would be, and every place VB.NET diverges from C# is
flagged inline with a `VB idiom:` comment. A digest of those idioms is in the
[cheat sheet](#vbnet-for-c-developers--a-cheat-sheet) below.

---

## Building and running

```bash
dotnet build                                    # whole solution
dotnet test                                     # 23 unit tests (run anywhere)
dotnet run --project src/GlassWorks.Configurator # launches the app (Windows only)
```

Notes:

- **Running the app requires Windows** — WPF has no cross-platform runtime.
- **Building works on any OS.** The app project sets
  `<EnableWindowsTargeting>true</EnableWindowsTargeting>`, so Linux/macOS build
  agents can compile it (the Windows targeting packs are restored from NuGet).
  One caveat for Linux: distro-packaged (source-built) .NET **8** SDKs omit the
  WindowsDesktop SDK component; use a Microsoft SDK build or a .NET 10 SDK
  (which includes it and can target net8.0). On Windows, any .NET 8+ SDK works.
- The core library and its tests target plain `net8.0` and build/run anywhere.

## Solution layout

```
GlassWorksConfigurator.sln
├── src/GlassWorks.Core/                 # net8.0 class library — NO WPF references
│   ├── Models/                          #   enums, LiteConfiguration, WindowUnitConfiguration,
│   │                                    #   FrameTypeCatalog (size limits), PriceBreakdown,
│   │                                    #   BomItem, Quote / QuoteLineItem
│   └── Services/                        #   IPricingService / PricingService
│                                        #   IBomService / BomService
│                                        #   IQuoteRepository / JsonQuoteRepository
│                                        #   IQuoteExporter / QuoteExporter (CSV + text)
├── src/GlassWorks.Configurator/         # net8.0-windows WPF app — presentation only
│   ├── Application.xaml(.vb)            #   composition root (service wiring, no StartupUri)
│   ├── Views/MainWindow.xaml(.vb)       #   the single window; code-behind is empty
│   ├── ViewModels/                      #   ViewModelBase, RelayCommand, MainViewModel,
│   │                                    #   LiteViewModel, QuoteLineItemViewModel, EnumCatalog
│   ├── Controls/WindowPreviewControl    #   custom UserControl: the live 2D drawing
│   ├── Converters/                      #   TintToBrushConverter, EnumDescriptionConverter
│   ├── Services/                        #   IDialogService / DialogService (file/confirm dialogs)
│   └── Themes/                          #   Colors.xaml, Styles.xaml (resource dictionaries)
└── tests/GlassWorks.Core.Tests/         # net8.0 xUnit tests, also VB.NET
```

The split matters: **everything that computes lives in `GlassWorks.Core`**,
which has no `PresentationFramework` reference and therefore runs (and is
unit-tested) on any OS. The WPF project contains only presentation:
XAML, ViewModels, converters, drawing code, and dialogs.

## How the MVVM pieces connect

```
                     Views (XAML)                    ViewModels                     Core
┌───────────────────────────────────────┐  ┌───────────────────────────┐  ┌──────────────────────┐
│ MainWindow.xaml                       │  │ MainViewModel             │  │                      │
│   ComboBox ──SelectedItem──────────────▶ │   FrameMaterial           │  │                      │
│   TextBox  ──Text (ValidatesOnData-   │  │   SelectedLite ───────────┼─▶│ LiteConfiguration    │
│              Errors, Delay=300)────────▶ │     LiteViewModel         │  │  (snapshot)          │
│   Buttons  ──Command───────────────────▶ │   AddLiteCommand, ...     │  │                      │
│                                       │  │        │ every edit       │  │                      │
│ WindowPreviewControl                  │  │        ▼                  │  │                      │
│   Unit="{Binding CurrentUnit}" ◀────────┼── CurrentUnit  = BuildSnapshot() ─ WindowUnit-       │
│   (DP change ⇒ redraw canvas)         │  │   Breakdown    = IPricingService.CalculatePrice() ──┤
│                                       │  │   BomItems     = IBomService.GenerateBom() ─────────┤
│ ItemsControl (price lines)  ◀───────────┼── Breakdown.Lines          │  │ PricingService       │
│ DataGrid     (cut list)     ◀───────────┼── BomItems                 │  │ BomService           │
│ ListView     (quote)        ◀───────────┼── QuoteItems ──persist───────▶│ JsonQuoteRepository  │
└───────────────────────────────────────┘  └───────────────────────────┘  └──────────────────────┘
```

The full loop for any edit:

1. A control pushes a value into `LiteViewModel` (or `MainViewModel`) through a
   two-way `{Binding}`. No event handler is involved.
2. The setter raises `PropertyChanged`. `MainViewModel` subscribes to every
   lite's `PropertyChanged` (hooked/unhooked in the `ObservableCollection`'s
   `CollectionChanged`, which itself is wired declaratively via VB's
   `WithEvents`/`Handles`).
3. `Recalculate()` builds an immutable `WindowUnitConfiguration` **snapshot**
   and assigns it to `CurrentUnit`, then calls the injected `IPricingService`
   and `IBomService`.
4. Assigning `CurrentUnit` fires the `Unit` dependency property on
   `WindowPreviewControl`, whose change callback redraws the canvas. The
   ViewModel never touches the canvas; the control never sees a ViewModel —
   its input is a plain model object.
5. Assigning `Breakdown`/`BomItems` refreshes the pricing panel and cut-list
   DataGrid through ordinary bindings.

Other connective tissue:

- **Commands.** Every button binds to a `RelayCommand` exposed by
  `MainViewModel`; `CanExecute` predicates disable buttons (e.g. *Add to
  quote* while validation errors exist, *Remove* when only one lite remains).
  The code-behind files contain no click handlers — `MainWindow.xaml.vb` is
  just `InitializeComponent()`.
- **Validation.** `LiteViewModel` implements `IDataErrorInfo`; width/height
  limits come from `FrameTypeCatalog` and depend on the frame type. Bindings
  opt in with `ValidatesOnDataErrors=True`; a shared `Validation.ErrorTemplate`
  (red outline + ⚠ + tooltip) plus an aggregate error `TextBlock` provide the
  visual feedback.
- **Converters.** `TintToBrushConverter` (enum → semi-transparent brush, used
  by both XAML and the drawing code) and `EnumDescriptionConverter` (enum →
  `Description` attribute text for every ComboBox).
- **Dependency injection.** `Application.xaml.vb` is the composition root: it
  news up the concrete services and hands them to `MainViewModel` as
  interfaces. There is no DI container (on purpose, to keep the sample small),
  but the constructor-injection shape means adding one later touches one file.
- **Persistence.** The quote auto-saves through `IQuoteRepository` after every
  mutation (add/remove/quantity edit) to
  `%APPDATA%\GlassWorks Configurator\current-quote.json` and reloads on
  startup. Export goes through `IDialogService` (so the ViewModel stays free
  of WPF dialog types) into `IQuoteExporter`.

## The pricing model (see `PricingService`)

| Component | Rule |
|---|---|
| Glass | billable area (min **6 sq ft**) × base rate: single **$8.50**, double **$12.75**, triple **$18.50** /sq ft |
| Tint adder | clear $0, low-E **$2.50**, bronze/gray **$1.90** /sq ft |
| Frame | glass cost × (type mult × material mult − 1): fixed 1.00, slider 1.18, awning 1.28, casement 1.32, double-hung 1.38 × vinyl 1.00, aluminum 1.12, fiberglass 1.40, wood 1.65 |
| Grids | colonial **$2.50/opening** (rows × cols); prairie **$16/lite** |
| Mulling | **$32.50 per joint** (n lites ⇒ n − 1 joints) |

All money is `Decimal`; each line rounds to cents and the total is the sum of
the rounded lines, so the breakdown always foots.

---

## VB.NET for C# developers — a cheat sheet

Everything below appears in this codebase; file references point at examples.

### Syntax basics

| C# | VB.NET | In this repo |
|---|---|---|
| `this` | `Me` | everywhere |
| `base.OnStartup(e)` | `MyBase.OnStartup(e)` | `Application.xaml.vb` |
| `null` | `Nothing` (also `default(T)`!) | everywhere |
| `x == null` / `x != null` | `x Is Nothing` / `x IsNot Nothing` | `RelayCommand.vb` |
| `a ?? b` | `If(a, b)` | `EnumHelper.vb` |
| `a ? b : c` | `If(a, b, c)` | `MainViewModel.ExportQuote` |
| `&&` / `\|\|` (short-circuit) | `AndAlso` / `OrElse` (`And`/`Or` do **not** short-circuit) | `RelayCommand.vb` |
| `var x = ...;` | `Dim x = ...` (needs `Option Infer On`) | everywhere |
| `//` comment, `/// <summary>` | `'` comment, `''' <summary>` | everywhere |
| `(decimal)x`, `(T)obj`, `obj as T` | `CDec(x)`/`CType(...)`, `DirectCast(...)`, `TryCast(...)` | `PricingService.vb`, `LiteConfiguration.Clone` |
| `x is T` | `TypeOf x Is T` | `TintToBrushConverter.vb` |
| `nameof(X)` | `NameOf(X)` | everywhere |
| `params T[] args` | `ParamArray args() As T` | `WindowPreviewControl.AddDashedPolyline` |
| `void M(out T x)` | `ByRef` parameter (no `out`; call site has no keyword) | `BomService.GetGlassPanelSize` |
| `string s = $"{a:0.##}"` | identical `$"..."` interpolation | everywhere |
| `'c'` char literal | `"c"c` | `QuoteExporter.vb` |
| `"\"quoted\""` | `""` doubling — VB strings have **no backslash escapes** | `QuoteExporter.CsvEscape` |
| `@class` (keyword escape) | `[Class]` square brackets | `[Enum]`, `[Error]` in `LiteViewModel.vb` |

### Types and members

| C# | VB.NET | In this repo |
|---|---|---|
| `class C : Base, IFoo` | `Class C` + `Inherits Base` + `Implements IFoo` lines | all ViewModels |
| interface members match by signature | **every member needs an explicit `Implements IFoo.Member` clause** (name may even differ) | `PricingService.vb` |
| `static` | `Shared` | `EnumCatalog.vb` |
| `static class` | `Module` (members import into scope) or `NotInheritable Class` + `Private Sub New` | `EnumHelper.vb` vs `EnumCatalog.vb` |
| `abstract` / `sealed` / `virtual` / `override` | `MustInherit` / `NotInheritable` / `Overridable` / `Overrides` | `ViewModelBase.vb` |
| `public T X { get; set; }` | `Public Property X As T` | models |
| `{ get; }` init in ctor | `Public ReadOnly Property X As T` | `MainViewModel` commands |
| `private set;` | `Private Set(...)` on the accessor | `MainViewModel.CurrentUnit` |
| `this[string name]` indexer | `Default Public ReadOnly Property Item(name As String)` | `LiteViewModel.Item` |
| static ctor `static C()` | `Shared Sub New()` | `JsonQuoteRepository.vb` |
| optional args `string s = ""` | `Optional s As String = ""` | `BomItem.vb` |
| named args `f(x: 1)` | `f(x:=1)` | `FrameTypeCatalog` |
| extension method (`this T x`) | `<Extension>` attribute, **must** live in a `Module` | `EnumHelper.GetDescription` |
| `new T { Prop = v }` | `New T With {.Prop = v}` (note the dot) | everywhere |
| `new List<T> { a, b }` | `New List(Of T) From {a, b}` | rate tables in `PricingService.vb` |
| generics `List<T>` | `List(Of T)` | everywhere |

### Control flow

| C# | VB.NET | In this repo |
|---|---|---|
| `for (int i = 0; i < n; i++)` | `For i = 0 To n - 1 ... Next` (inclusive bound) | `PricingService.vb` |
| `switch` | `Select Case` (no fall-through, no `break`) | `BomService.vb` |
| `catch (E e) when (cond)` | `Catch e As E When cond` | `JsonQuoteRepository.Load` |
| statement lambda `x => { ... }` | `Sub(x) ...` / multi-line `Sub(x) ... End Sub` | command wiring in `MainViewModel` |
| expression lambda `x => y` | `Function(x) y` | LINQ everywhere |
| method group `list.Select(M)` | `list.Select(AddressOf M)` | `LiteViewModel.[Error]` |

### Events — the biggest difference, and the most idiomatic VB

| C# | VB.NET | In this repo |
|---|---|---|
| `event PropertyChangedEventHandler PropertyChanged;` | `Public Event PropertyChanged As ... Implements ...` | `ViewModelBase.vb` |
| `PropertyChanged?.Invoke(this, e)` | `RaiseEvent PropertyChanged(Me, e)` — null-check is automatic | `ViewModelBase.vb` |
| `x.Event += Handler` / `-=` | `AddHandler x.Event, AddressOf Handler` / `RemoveHandler` | per-lite subscriptions in `MainViewModel` |
| *(no equivalent)* | **`WithEvents` field + `Handles` clause**: declarative subscription; reassigning the field rewires handlers automatically | `_lites`/`_quoteItems` in `MainViewModel`; `Handles Me.SizeChanged` in `WindowPreviewControl` |
| `event ... { add {} remove {} }` | `Custom Event` with `AddHandler`/`RemoveHandler`/`RaiseEvent` blocks | `RelayCommand.CanExecuteChanged` |

### WPF/XAML specifics in a VB project

- **`x:Class` is relative to the project's root namespace.** This project's
  root namespace is `GlassWorks.Configurator`, so the window declares
  `x:Class="Views.MainWindow"`, not the full name (a C# project would use the
  full name). `clr-namespace:` xmlns declarations still use the *full* CLR
  namespace.
- The application definition file is conventionally `Application.xaml` in VB
  (C# templates use `App.xaml`); the SDK picks it up by that name.
- `Option Strict On` (set in every `.vbproj`) is the setting that makes VB
  behave like C# — no implicit narrowing conversions, no late binding. Leave
  it on; the default (`Off`) allows silently late-bound member access.
- Watch out for **case-insensitivity**: `Dim directory = ...` shadows the
  `Directory` class (see `JsonQuoteRepository.Save`), and a local can collide
  with a property differing only by case.
- Watch out for **extension-method resolution**: `readOnlyList.Count(pred)`
  binds to the `Count` *property* and fails to compile — use
  `.Where(pred).Count()` (see `BomServiceTests.vb`).

## Things to try in the app

1. Change *Frame type* to **Slider** with the default 24″ width — the field
   turns red with a range message and *Add to quote* disables (min slider
   width is 36″).
2. Add two lites: **Casement + Fixed + Casement** — a classic mulled picture
   window. Watch the mull bars, per-lite dimension lines, and the mulling
   charge appear.
3. Set grid to **Colonial 3×2** on the center lite, tint to **Bronze**, and
   flip materials — the preview, breakdown, and cut list all track live.
4. Add a few configurations to the quote, edit quantities in the list, restart
   the app — the quote comes back (JSON in
   `%APPDATA%\GlassWorks Configurator\`), then export it to CSV.
