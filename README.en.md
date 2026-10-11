# FormWind

[Türkçe](README.md) | **English**

A Tailwind-style utility-class styling system for WinForms controls. Drop a `TailwindProvider` on a form and every control gets a `Classes` field in the Properties window:

```
btnSave  →  Classes: "bg-blue-600 text-white px-4 py-2 rounded-md font-bold hover:bg-blue-700"
```

- Targets: .NET Framework 4.7.2+ and .NET 8+ (`net472`, `net8.0-windows`); tests also run on .NET 9 and 10
- The core package has no dependencies beyond .NET (only `System.Text.Json` on `net472`)
- State variants: `hover:`, `focus:`, `active:`, `disabled:`, `checked:`, `dark:` and combinations (`dark:hover:`)
- Flex and grid layout (`flex`, `gap-4`, `justify-between`, `flex-1`, `w-1/2`, `grid-cols-3`, `col-span-2`)
- Component classes (`btn-primary`, `card`) and a JSON theme file
- `TwButton`, `TwPanel`, `TwTextBox` with anti-aliased rounded corners
- Native scrollbars and tooltips also turn dark in dark mode
- WCAG contrast audit (`TwContrast`) and Windows High Contrast support
- Optional Krypton Toolkit integration (`FormWind.Krypton`)
- A harness for trying FormWind on real applications without touching their source (`tools/run-harness.ps1`)

| Light | Dark |
| --- | --- |
| ![Light](docs/showcase-light.png) | ![Dark](docs/showcase-dark.png) |

## Quick start

1. Add the package (see [Packaging](#packaging)) or a project reference to `src/FormWind`.
2. In `Program.Main`, load the theme before any form is opened (the default theme is used if the file does not exist):

   ```csharp
   TwTheme.Current = TwTheme.LoadIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tw.config.json"));
   ```

3. Drag `TailwindProvider` from the Toolbox onto the form. If it is missing: Toolbox → right-click → **Choose Items** → `FormWind.dll`.
4. Select controls and fill in `Classes on tailwindProvider1`. The Designer generates:

   ```csharp
   this.tailwindProvider1.SetClasses(this.btnSave, "btn-primary");
   ```

5. Dark mode: `tailwindProvider1.DarkMode = true;`

### Project template

The `FormWind.Templates` package creates an app with ready-made screens: sign-in (validated fields), list-detail (searchable `TwDataList` with a detail card) and settings; the light/dark switch is shared across forms.

```bash
dotnet new install FormWind.Templates
```

```bash
dotnet new formwind-app -n MyApp --Framework net8.0-windows --Palette v4
```

`--Framework`: `net8.0-windows` (default), `net9.0-windows`, `net10.0-windows`, `net472`; `--Palette`: `v3` (default), `v4`; `--FormWindVersion`: package version. Forms are built in code without the Designer and use `AutoScaleMode.Dpi`. Until the package is published, add the `artifacts/packages` folder as a package source; CI generates and builds the template for two targets on every run.

## Class reference

| Category | Pattern | Example | WinForms equivalent |
| --- | --- | --- | --- |
| Background | `bg-{color}-{shade}`, `bg-[#hex]` | `bg-blue-600`, `bg-white` | `BackColor` |
| Text color | `text-{color}-{shade}` | `text-gray-900` | `ForeColor` |
| Font size | `text-xs … text-4xl` | `text-sm` = 14 px | `Font` (px × 0.75 = pt) |
| Font style | `font-thin` … `font-black`, `font-sans`, `font-mono`, `italic`, `underline` | `font-semibold` | `Font` (see [Font weights](#font-weights)) |
| Padding | `p`, `px`, `py`, `pt`, `pr`, `pb`, `pl` + `-{n}` / `-px` / `-[12px]` | `px-4` = 16 px | `Padding` |
| Margin | `m`, `mx`, `my`, `mt`, `mr`, `mb`, `ml` + `-{n}` | `mt-2` = 8 px | `Margin` (in layout panels) |
| Size | `w-{n}`, `h-{n}` | `h-10` = 40 px | `Width`, `Height` |
| Border | `border`, `border-{0,2,4,8}`, `border-{color}-{shade}` | `border border-gray-300` | Painted |
| Corners | `rounded`, `rounded-{none,sm,md,lg,xl,2xl,3xl,full}`, `rounded-[10px]` | `rounded-md` = 6 px | `Region` or owner-draw |
| Alignment | `text-left`, `text-center`, `text-right` | | `TextAlign` |
| Other | `cursor-pointer`, `cursor-text`, `hidden`, `visible`, `truncate` | | `Cursor`, `Visible`, `AutoEllipsis` |

- The spacing unit is 4 px (configurable with `spacingUnit`). Numbers always use a dot, in every culture: `p-0.5`.
- Palette: the 22 colors × 11 shades (50–950) of Tailwind v3, plus `white`, `black`, `transparent`. The Tailwind v4 palette can be selected too (see [Theme file](#theme-file-twconfigjson)).
- Within the same variant the later class wins: `p-2 px-4` → 8 px vertical, 16 px horizontal.
- Unknown classes are reported through the `TailwindProvider.UnknownClasses` event (or the Debug output when nobody listens).
- In CSS, padding grows the box; in WinForms, on a fixed-size (`AutoSize` off) button or label it shrinks the content. On these controls padding is reduced just enough for the text to fit on one line (e.g. `py-2` shrinks when `btn` is applied to a 25 px tall button). It is recalculated when the control grows or its font or text changes, and returns to the full value when there is room.
- Padding fitting cannot shrink the font: if a fixed-size control gets a class that enlarges its font (e.g. `text-sm font-semibold` in `btn`), the text may not fit even with zero padding; do not give such controls a font class, or make the control larger.
- Removing `hidden` makes a control hidden by FormWind visible again.
- **MDI:** the `bg-*` color of an `IsMdiContainer` form is also given to the area behind the child windows (`MdiClient`); otherwise that area keeps the system gray and becomes a light gap in dark mode. A color the application assigned to the `MdiClient` is kept. The minimize/restore/close buttons that a maximized child adds to the menu are drawn in the strip's text color. Windows does not draw dark title bars on MDI children (classic frame), and the `MdiClient`'s sunken edge shows as a light line in dark mode; both are outside FormWind's control.
- **Boxes hosted in a strip:** `ToolStripTextBox` and `ToolStripComboBox` are not drawn by the strip's renderer and stayed white on a dark strip. They take the strip's text color and a background derived from the strip (white on a light strip, slightly lighter than the background on a dark one); the combo box's frame and arrow button are drawn with these colors too. Boxes in sub-menus that share the strip's renderer and boxes added later are included. Colors the application assigned to these boxes are kept.
- **Disabled native text boxes** (`TextBox`, `RichTextBox`, `MaskedTextBox`): Windows does not reliably honor the background color of a disabled text box (depending on when the color was assigned it may paint the system background), but it does honor the text color; in dark mode this produced light text on a light background. So while disabled, only the colors of `disabled:` variants are applied; otherwise Windows' own disabled look (gray text on a light background) remains. Controls FormWind draws itself (including `TwTextBox`) are not affected.
- **Disabled buttons:** Windows draws the text of a disabled flat button in a darkened shade of its background instead of `ForeColor`, which made the text invisible on a dark background. On buttons styled by FormWind the text of a disabled button is redrawn with the `disabled:text-*` color, or, without one, with the text color mixed halfway toward the background (`TwButton` follows the same rule). Native buttons with an image keep Windows' drawing. The default `btn-primary`, `btn-ghost` and `btn-danger` components define `disabled:` colors for light and dark mode; `disabled:` takes precedence over `dark:`, so give your own components `dark:disabled:` colors for dark mode as well.
- A class color is assigned explicitly even when it equals the inherited parent color; it does not change when the parent's color changes later.
- When classes are removed (`SetClasses(control, "")`), color, font, padding, margin, cursor, visibility, corners and the ToolStrip renderer return to their values from before the classes. Sizes (`w-*`, `h-*`) are not reverted, because the layout may have resized the control since.

### Colors assigned by application code

If application code changes a control's `BackColor` or `ForeColor` after classes have been applied (e.g. painting a status indicator green), FormWind leaves that color channel to the application: `hover:`, `dark:` and other variants do not override it. The other channel stays with FormWind.

```csharp
tw.SetClasses(lblStatus, "bg-white text-gray-700 dark:bg-gray-800 dark:text-gray-200");
lblStatus.BackColor = Color.LightGreen;   // stays green in dark mode too; the text color comes from the classes
tw.SetClasses(lblStatus, "bg-red-600 text-white");   // new classes: the background belongs to FormWind again
```

- Color changes inherited from the parent (e.g. the form switched to dark mode) do not count as application assignments.
- To give the channel back, assign different classes; `Refresh(control)` is not enough.
- If the application changes only the background and leaves the text color to the classes, dark mode may produce an unreadable pair (light text on a light background). The [contrast audit](#contrast-audit) reports this.

## Layout (flex and grid)

A **`TwPanel`** with the `flex` or `grid` class lays out its children itself; the children's `Location`, `Dock` and `Anchor` are ignored. Because the layout engine of the standard `Panel` and `Form` cannot be replaced, flex/grid only works on `TwPanel` (or a container implementing `ITwLayoutHost`).

```csharp
// Card: vertical stack, items fill the width
this.tailwindProvider1.SetClasses(this.pnlCard, "card flex flex-col gap-1");
// Button row: right-aligned, 8 px apart
this.tailwindProvider1.SetClasses(this.pnlActions, "flex justify-end gap-2");
// Spacer: takes the remaining space and pushes the button row to the bottom
this.tailwindProvider1.SetClasses(this.pnlSpacer, "flex-1");
```

| Container (TwPanel) | Item (child control) |
| --- | --- |
| `flex`, `grid`, `block` | `flex-1`, `flex-auto`, `flex-initial`, `flex-none` |
| `flex-row`, `flex-col`, `flex-wrap`, `flex-nowrap` | `grow`, `grow-0`, `shrink`, `shrink-0` |
| `gap-{n}`, `gap-x-{n}`, `gap-y-{n}` | `basis-{n}`, `basis-1/2`, `basis-full`, `basis-auto` |
| `justify-start/end/center/between/around/evenly` | `w-1/2`, `w-full`, `h-full` (fractions of the container) |
| `items-start/end/center/stretch` | `min-w-{n}`, `max-w-{n}`, `max-w-md` …, `min-h-{n}`, `max-h-{n}` |
| `grid-cols-{1–12}`, `grid-rows-{1–12}` | `self-start/end/center/stretch`, `col-span-{1–12}`, `col-span-full`, `row-span-{1–12}`, `row-span-full`, `m-*` |

Differences from CSS:
- An item's "natural" size is its `Size` in the Designer (the preferred size for AutoSize controls). Items without an explicit size do not shrink below their natural size.
- Item spacing only comes from `m-*` classes; the default 3 px WinForms `Margin` is not used.
- The default is `items-stretch`: items in a row stretch to the container's height, items in a column to its width.
- Grid items are placed in order with CSS auto-placement (sparse); cells covered by `row-span-*` are skipped. `grid-rows-N` splits the container height into N equal rows (`repeat(N, minmax(0, 1fr))`); `row-span-full` covers all of these explicit rows (a single row without `grid-rows`). Other rows take their height from the content; if an item spanning several rows does not fit, the missing height is shared equally by the rows it spans. There is no `col-start-*`, `row-start-*`, `grid-flow-*`, `margin: auto` or `order-*`.
- Items do not exceed the row's cross size (in CSS they may overflow; in WinForms the overflow would be clipped and lost). Shrunk labels show "…" with `truncate`.
- A `TwPanel`'s content starts inside its border (CSS box model); in an `AutoScroll` panel the width is taken from the visible area and room for the vertical scrollbar is reserved up front.
- A flex `TwPanel` with `AutoSize = true` sizes itself to its content (not yet for grid).
- Responsive variants (`sm:`, `md:` …): see [Animations and responsive variants](#animations-and-responsive-variants).

## Font weights

`font-thin` (100), `font-extralight` (200), `font-light` (300), `font-normal` (400), `font-medium` (500), `font-semibold` (600), `font-bold` (700), `font-extrabold` (800), `font-black` (900).

WinForms only knows regular and bold; intermediate weights are installed on Windows as **separate font families**. FormWind resolves a weight to these families using CSS's matching rule:

| Class | Used with Segoe UI |
| --- | --- |
| `font-thin`, `font-extralight`, `font-light` | Segoe UI Light |
| `font-normal`, `font-medium` | Segoe UI |
| `font-semibold` | Segoe UI Semibold |
| `font-bold` | Segoe UI + bold style |
| `font-extrabold`, `font-black` | Segoe UI Black |

For other fonts, weight families are defined in the theme file; families that are not installed are skipped and the closest installed weight is chosen:

```jsonc
"fontWeights": { "Inter": { "300": "Inter Light", "600": "Inter SemiBold" } }
```

## Icons

The icon font shipped with Windows is used: **Segoe Fluent Icons** on Windows 11, **Segoe MDL2 Assets** on Windows 10 (no icons on Windows 7/8).

- **`TwIcon`**: a standalone icon. The `IconName` property is picked from a drop-down in the Designer. Color comes from `text-*`, size from the font size: `text-xl` = 20×20 px. AutoSize is on by default.
- **`TwButton.IconName`**: an icon next to the text. `IconPosition` (Left/Right) and `IconSpacing` (default 8 px) can be set.

```csharp
this.icoCustomer.IconName = "user";
this.tailwindProvider1.SetClasses(this.icoCustomer, "text-xl text-blue-600 dark:text-blue-400");
this.btnSave.IconName = "save";
```

Built-in names: `arrow-down`, `arrow-left`, `arrow-right`, `arrow-up`, `attach`, `bell`, `calculator`, `calendar`, `camera`, `cart`, `check`, `chevron-down`, `chevron-left`, `chevron-right`, `chevron-up`, `clock`, `copy`, `cut`, `download`, `edit`, `error`, `eye`, `eye-off`, `file`, `filter`, `flag`, `folder`, `folder-open`, `fullscreen`, `globe`, `grid`, `heart`, `heart-filled`, `help`, `history`, `home`, `image`, `info`, `key`, `link`, `list`, `location`, `lock`, `mail`, `map-pin`, `menu`, `minus`, `more`, `paste`, `phone`, `pin`, `plus`, `power`, `print`, `redo`, `refresh`, `save`, `search`, `send`, `settings`, `share`, `shield`, `sort`, `star`, `star-filled`, `sync`, `tag`, `trash`, `undo`, `unlock`, `upload`, `user`, `users`, `warning`, `x`, `zoom-in`, `zoom-out`.

Any glyph of the font can also be used by code point (`IconName = "#E8A5"`). Extra names and a different icon font come from the theme file:

```jsonc
"iconFont": "Segoe Fluent Icons",
"icons": { "invoice": "E8A5" }
```

Glyphs are centered on their actual ink bounds rather than the font metrics (the line height of icon fonts does not match the glyph size).

### SVG icons

Single-color SVG sets such as Lucide, Heroicons or Tabler can be used too. An `icons` value can be a path to an `.svg` file (relative to the theme file) or inline `<svg>` text; in code, `TwTheme.SetSvgIcon(name, svg)`:

```jsonc
"icons": {
  "invoice": "E8A5",                         // icon font glyph
  "package": "Assets/icons/package.svg",     // file
  "dot": "<svg viewBox='0 0 24 24'><circle cx='12' cy='12' r='4'/></svg>"
}
```

An SVG icon overrides the glyph with the same name, so built-in icons (`check`, `chevron-down` …) can be replaced one by one across all controls (`TwButton`, `TwSelect`, calendar, grid). The color is always the text color where it is drawn (like `currentColor`). Supported: `path` (all commands, including arcs), `circle`, `ellipse`, `rect` (`rx`), `line`, `polyline`, `polygon`, `g`; `fill`, `stroke`, `stroke-width`, `stroke-linecap`, `stroke-linejoin`. Gradients, masks, text and CSS are not supported.

## Borders, shadows and focus rings

| Class | Effect |
| --- | --- |
| `border-t`, `border-b-2`, `border-x`, `border-y` | Per-side borders (drawn without corners) |
| `shadow-sm`, `shadow`, `shadow-md`, `shadow-lg`, `shadow-xl`, `shadow-2xl`, `shadow-none` | Tailwind v3 shadow layers (blurred) |
| `shadow-{color}`, `shadow-blue-500/50` | Colored shadow |
| `ring`, `ring-0/1/2/4/8`, `ring-{color}`, `ring-offset-2`, `ring-offset-{color}` | Ring; usually `focus:ring-2 focus:ring-blue-500/30` |
| `bg-black/50`, `text-white/75` … | Opacity: `/0–100` on every color class |

Shadows and rings are drawn on the control's **parent** (controls cannot paint outside their own bounds); this works on Form, Panel and TwPanel parents. That is why native controls such as `TextBox` can have a focus ring too.

## Gradients

`bg-gradient-to-{r,l,t,b,tr,tl,br,bl} from-{color} via-{color} to-{color}` — drawn on `TwPanel`, `TwButton` and the standard `Panel`. A missing end is the transparent version of the other end.

## Interaction

- **`transition`** (`transition-colors`, `duration-75` … `duration-1000`, `duration-[250ms]`): background and text color changes are animated (ease-out, 150 ms by default).
- **`hover:` / `active:`**: only controls with these variants are restyled when the mouse enters or leaves; for the others, mouse movement does not trigger styling. If classes are assigned while the cursor is already over the control (e.g. a button whose classes change on click), the hover state is applied immediately.
- **`group` / `group-hover:`**: hovering a container with the `group` class applies the `group-hover:` variants of the controls inside it. A container that reacts to hover counts as hovered even when the cursor enters directly over a child.
- **Placeholder**: `TwTextBox.PlaceholderText`; color `placeholder:text-gray-400`.
- **`focus-visible:`**: applied only when focus came from the keyboard (Tab, arrows), not when the control was clicked (`focus-visible:ring-2 focus-visible:ring-blue-500/40`). Owner-drawn controls (`TwButton`, `TwTabStrip`, `TwDataList`, `TwCalendar`, `TwSlider`) draw their focus rings with the same rule. The last input type is available as `TwFocusVisible.LastInputWasKeyboard`.

## CheckBox, Radio, Switch

`TwCheckBox`, `TwRadioButton` and `TwSwitch` are owner-drawn. The checked color is `accent-*`, the unchecked fill `fill-*`, the box border `border-*`, the corners `rounded-*`, the text `text-*`. The `TwSwitch` (44×24 px) knob slides in 150 ms.

## Menus and toolbars

A themed renderer for MenuStrip, ToolStrip, StatusStrip and ContextMenuStrip. Base classes apply to the strip, `menu:` to drop-down menus and `item:` to items:

```csharp
SetClasses(menuStrip1, "bg-white text-gray-700 border-b menu:bg-white menu:border-gray-200 item:hover:bg-gray-100 item:rounded-md divide-gray-200 dark:bg-gray-900 dark:text-gray-200");
```

- When `item:hover:`, the separator (`divide-*`) and the menu border (`menu:border-*`) are not given, the defaults are meant for a light strip. If the strip's background is dark, these colors are derived from the strip's own background and text colors, so the text does not disappear in the hover box on a dark strip.
- Drop-downs assigned to items by hand (`menuItem.DropDown = contextMenuStrip1`) do not inherit their owner's renderer in WinForms. FormWind gives these menus the strip's renderer (nested menus included). Menus with their own renderer or their own classes are left alone. A menu assigned to the strip later gets its colors the next time the strip is styled (e.g. on a theme change).
- The renderer is not reassigned unless the colors change (assigning it relayouts the strip). When classes are removed, the strip's original renderer is restored.

## Title bar

When a Form has classes and dark mode is on, the title bar turns dark automatically (Windows 10 20H1+). On Windows 11 it can be colored with `caption:bg-*`, `caption:text-*`, `caption:border-*`.

`backdrop-mica`, `backdrop-acrylic`, `backdrop-tabbed` (Mica Alt) and `backdrop-none` select the Windows 11 22H2+ system backdrop. The backdrop is visible in the title bar and window frame; it does not show inside the form because WinForms paints the client area opaque. If `caption:bg-*` is given, the caption color takes precedence over the backdrop.

## Dark mode: scrollbars and tooltips

With `DarkMode = true`, native parts that FormWind does not paint also switch to Windows' dark theme:

| Property (on by default) | Effect |
| --- | --- |
| `TailwindProvider.DarkScrollbars` | Scrollbars of Panel, TextBox, ListBox, TreeView, ListView and DataGridView |
| `TailwindProvider.DarkToolTips` | Native tooltip windows: `ToolTip` components, ToolStrip item tooltips, ListView/TreeView tooltips |

- Because tooltips belong to the application's own components, `DarkToolTips` is thread-wide: if at least one provider on a thread is in dark mode, that thread's tooltips are dark. Open tooltip windows update when the theme changes.
- Both use the undocumented `DarkMode_Explorer` theme used by Windows Explorer (Windows 10 1809+); it has no effect on unsupported systems and can be turned off if it causes problems.
- For a tooltip styled with classes, use `TwToolTip`.

## DataGridView

Parts of a DataGridView are styled with **part prefixes**, which can be combined with state prefixes (`dark:header:bg-gray-900`, `row:hover:bg-gray-50`). The built-in `table` component gives a light and dark table with a single class:

```csharp
this.tailwindProvider1.SetClasses(this.gridOrders, "table");
```

| Prefix | Target | Supported classes |
| --- | --- | --- |
| (none) | Control area and all cells | `bg-*`, `text-*`, font, `border`, `divide-x`, `divide-y`, `divide-{color}` (grid lines) |
| `cell:` | `DefaultCellStyle` | color, font, `px-*`/`py-*`, `text-left/center/right`, `whitespace-nowrap` |
| `alt:` | `AlternatingRowsDefaultCellStyle` | color, font |
| `selected:` | Selection colors | `bg-*`, `text-*` |
| `header:` | Column headers | color, font, padding, alignment, `h-*`, `border-0`, `whitespace-nowrap` |
| `row:` | Rows | `h-*` (row height), `row:hover:bg-*` / `row:hover:text-*` (row hover) |
| `row-header:` | Row headers | `hidden`, color |

- When header colors are given, `EnableHeadersVisualStyles` is turned off (the Windows theme ignores header colors).
- When classes are removed, the grid's original styles are restored.
- In dark mode the scrollbars switch to Windows' dark scrollbars (`TailwindProvider.DarkScrollbars`, on by default).
- Part prefixes are ignored on controls other than DataGridView.
- On grids using `VirtualMode` or `DataSource`, `row:h-*` is supplied through the `RowHeightInfoNeeded` event: rows are not unshared, and the scroll position and current cell are preserved (~12 ms for 17,000 rows; ~17 s when setting rows one by one). On unbound grids only rows whose height differs are updated.
- The grid background (`BackgroundColor`) does not support transparency; a transparent `bg-*` falls back to the original color (it still applies to cells).
- When rows are added, the grid is repainted only if the empty-table or loading view changes.
- **Self-drawing grids:** grids that paint their own cells (e.g. the Git Extensions commit list) ignore cell styles and may depend on structural properties. `divide-x` / `divide-y` change `CellBorderStyle` and `row:h-*` changes the row height; do not use these classes if such a grid assumes `CellBorderStyle.None` or its own row height. The Git Extensions commit list hangs while scrolling with a `SingleHorizontal` border; the same happens without FormWind.

### Column classes

Every column has a **ColumnClasses on tailwindProvider1** property in its Properties window (`SetColumnClasses` in code). Base classes apply to the column's cells and `header:` to its header; `w-32`, `min-w-20`, `flex-1` (Fill weight) and `hidden` affect the column itself. If no font size is given, the grid's font is kept.

```csharp
tw.SetColumnClasses(colTotal, "text-right font-semibold w-28 header:text-right");
tw.SetColumnClasses(colName, "flex-1 min-w-40");
```

### Cell types

| Column | Value | Settings |
| --- | --- | --- |
| `TwBadgeColumn` | Text; a colored badge per value | `Variants = "Alive: bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-100; Dead: bg-red-100 text-red-800; *: bg-gray-100 text-gray-700"` |
| `TwAvatarColumn` | `Image` (or the `Placeholder` color) | `AvatarSize`, `Radius` |
| `TwIconColumn` | Icon name (`check`, `#E710`) | Color is the cell's text color |
| `TwProgressColumn` | A number from 0 to `Maximum` | `ShowText`, `BarColor`, `TrackColor`, `DarkTrackColor` |
| `TwCheckColumn` | `bool` / `CheckState` | Rounded box, `AccentColor`, `BoxSize`; `ThreeState` shows the indeterminate state as a dash |
| `TwButtonColumn` | Text (or `Text` + `UseColumnTextForButtonValue`) | `Classes = "rounded-md bg-red-600 text-white hover:bg-red-700"`; clicks via `CellContentClick` |

Cell types learn the dark mode from the grid's provider; `dark:` variants need no extra setup.

### Row rules

```csharp
tw.SetRowClasses(gridOrders, row =>
    row.Cells["colStatus"].Value as string == "Cancelled" ? "text-gray-400 dark:text-gray-500" : null);
```

The rule returns classes for each row (`null` = no change); it is applied during `CellFormatting` and updates automatically when data changes. `SetRowClasses(grid, null)` removes the rule.

### Sorting, empty and loading states

On grids with classes, the sort arrow in the header is drawn with the theme icon (`arrow-up` / `arrow-down`) in the `accent-*` color. A grid bound to a `List<T>` or a plain `BindingList<T>` does not sort when its header is clicked; with `TwSortableBindingList<T>`, columns with `SortMode = Automatic` sort (strings with the current culture, including Turkish letter order; equal values keep their order):

```csharp
gridOrders.DataSource = new TwSortableBindingList<Order>(orders);
TwGrid.SetEmptyText(gridOrders, "No records");   // icon + text in the middle when there are no rows (TwGrid.SetEmptyIcon)
TwGrid.SetLoading(gridOrders, true);             // skeleton rows when empty, otherwise a faded overlay + "Loading…"
```

The TextBox/ComboBox opened while editing a cell is shown borderless in the cell's colors; its scrollbars turn dark in dark mode too.

### Filtering, column chooser and paging

```csharp
TwGrid.EnableFilterMenu(gridOrders);     // filter button in the header: the column's values as checkboxes
TwGrid.EnableColumnChooser(gridOrders);  // right-click on the header: show/hide columns
```

- The filter menu appears on grids whose data source is an `ITwFilterableList` (`TwSortableBindingList<T>`) and on columns with a `DataPropertyName`. A filtered column's button is drawn in the `accent-*` color. In code: `list.SetColumnFilter("City", new[] { "İzmir" })`, `ClearFilters()`.
- Sorts are stacked (in a list sorted by name and then by date, rows with the same date stay sorted by name); filtering keeps the sort order, and added or removed rows are reflected in the full list.
- The last visible column cannot be hidden from the column menu.

**Large tables (VirtualMode):** `TwVirtualRows<T>` puts the grid in VirtualMode and loads rows page by page as they come into view; the most recently used pages are cached, so with a million rows only the visible pages are in memory:

```csharp
var rows = new TwVirtualRows<Order>(gridOrders, (start, count) => db.Orders.OrderBy(o => o.Id).Skip(start).Take(count).ToList(), pageSize: 100);
rows.Reset(db.Orders.Count());              // again when the data or the sort order changes
```

- A cell's value is read from the property named by the column's `DataPropertyName` (or its name); change it with `rows.CellValue = (order, column) => ...`. `rows.GetItem(rowIndex)` returns the row's data.
- `loadPage` runs on the UI thread and should be fast (OFFSET/FETCH in the database). In VirtualMode the grid cannot sort by itself: sort in the query and call `Reset`. Adding rows by the user is turned off.

`TwPager` draws paging buttons (‹ 1 … 9 10 11 … 20 ›); you bind the data for the page:

```csharp
pager.TotalItems = orders.Count;
pager.PageSize = 20;
pager.PageChanged += (s, e) => grid.DataSource = orders.Skip(pager.FirstItemIndex).Take(pager.PageSize).ToList();
tw.SetClasses(pager, "text-gray-700 selected:bg-blue-600 selected:text-white item:hover:bg-gray-100");
```

### Variant order

Later overrides earlier: base → `dark:` → `checked:` → `focus:` → `focus-visible:` → `group-hover:` → `hover:` → `dark:hover:` → `active:` → `disabled:` → `invalid:` → `sm:` → `md:` → `lg:` → `xl:` → `2xl:` → `forced-colors:` (width variants come last, as in Tailwind).
`focus`, `hover` and `active` are not applied while a control is disabled.

## Theme file (`tw.config.json`)

```jsonc
{
  "fontFamily": "Segoe UI",
  "spacingUnit": 4,
  "colors": {
    "brand": { "500": "#2b579a", "600": "#234a85" },   // bg-brand-500
    "surface": "#f3f6fb"                                  // bg-surface
  },
  "components": {
    "btn-primary": "btn bg-brand-500 text-white hover:bg-brand-600"
  }
}
```

`fontFamily` can be a comma-separated stack (`"Inter, Segoe UI"`): the first installed or registered family is used. Fonts shipped with the application are registered without installation via `"fonts": ["Assets/fonts/Inter-Regular.ttf", "Assets/fonts/Inter-SemiBold.ttf"]` (paths are relative to the theme file; missing files are skipped); in code, `TwFonts.Register(path)`. Fonts are registered with both GDI+ (`PrivateFontCollection`) and GDI (`AddFontResourceEx`, process-private), so they also work in standard controls drawn with `TextRenderer`. Distribute the font license (e.g. SIL OFL) with your application.

**Tailwind v4 palette.** `"palette": "v4"` (in code `new TwTheme(TwPaletteVersion.V4)` or `theme.UsePalette(...)`) uses the v4 shades of the same 22 colors. v4 colors are defined in OKLCH (`TwPaletteV4`); those outside sRGB are approximated with CSS Color 4 gamut mapping, as browsers do, so `blue-500` becomes `#2b7fff` just like on the Tailwind site. Colors in the theme file can also be written as `oklch(62.3% 0.214 259.8)`; in code, `TwOklch.TryParse`. The default components pass the contrast audit with both palettes.

The file is added on top of the default palette and components. Default components: `btn`, `btn-primary`, `btn-ghost`, `btn-danger`, `card`, `input`, `table` (see `TwTheme.DefaultComponents`). The Designer does not read the theme file, so the design-time preview uses the defaults.

## Sample: Rickipedia

`samples/Rickipedia` is a character list page using data from [The Rick and Morty API](https://rickandmortyapi.com): search by name, status/species/gender filters, page size selection, dark mode and a details window on row click. The whole page consists of standard WinForms controls and `Tw*` controls styled with FormWind classes; the page components live in `tw.config.json`.

| Light | Dark |
| --- | --- |
| ![Rickipedia light](docs/rickipedia-light.png) | ![Rickipedia dark](docs/rickipedia-dark.png) |

```bash
samples/Rickipedia/bin/Debug/net472/Rickipedia.exe --snapshot page.png --dark --menu --details
```

## Controls

| Control | When to use |
| --- | --- |
| `TwPictureBox` | Image with smooth rounded corners (`rounded-lg`, `rounded-full` avatar); `object-cover` / `object-contain` / `object-fill`. |
| `TwSelect` | Stylable drop-down (instead of the native ComboBox): the box with base classes, the menu with `menu:` / `item:`. By default the width follows the selected item (`SizeToSelection`). Typing selects the item starting with those letters; `Searchable = true` adds a search box above the menu that filters items (ignoring case, accents and the ı/i difference). |
| `TwIcon` | Standalone icon (see [Icons](#icons)). |
| `TwCheckBox`, `TwRadioButton`, `TwSwitch` | See [CheckBox, Radio, Switch](#checkbox-radio-switch). |
| Standard controls | Color, text and spacing work on every control. Corners are clipped with `Region` (no anti-aliasing) and borders are drawn in the `Paint` event. On a `TextBox` (`TextBoxBase`) the native border is turned off; `border-*` and `rounded-*` are drawn on the parent as a frame 3 px outside the control, filled with the background color (the frame extends past the control's bounds; leave room in the layout). The closed box of a `ComboBox` in `DropDownList` mode (background, border, corners, text, arrow) is painted by FormWind; the drop-down list stays native. Editable `ComboBox`es and other native controls (`ListBox` …) get no corners or borders; use `TwSelect`, `TwListBox`. |
| `TwButton` | Button with smooth corners. `Image` is not drawn yet and the `Paint` event is not raised. |
| `TwPanel` | Panel (card) with smooth corners and a border. |
| `TwTextBox` | Wraps a borderless TextBox in a rounded panel; for the `input` style. |
| `TwNumberBox` | Number box (`TwTextBox` + ▲▼ on the right). `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`; arrow keys, mouse wheel and culture-aware parsing. |
| `TwDatePicker` | Date field + drop-down `TwCalendar`. The date can be typed (parsed with `Culture`/`Format`); F4 / Alt+↓ opens the calendar. The calendar is styled with `menu:bg-* menu:border-*`, `selected:bg-* selected:text-*`, `item:hover:bg-*`. |
| `TwCalendar` | Standalone month calendar: the culture's first day of week and day names, a ring on today, `MinDate`/`MaxDate`, keyboard (arrows, PageUp/PageDown, Home/End, Enter, Esc). |
| `TwTabStrip` | Tab strip: `Underline`, `Pills`, `Segmented`. `selected:text-* selected:border-*` / `selected:bg-*`, `item:hover:*`, `fill-*` (segmented background), `gap-*`. Only the strip; for pages use `TwTabControl`. |
| `TwTabControl` | Tab control with pages (instead of TabControl): `AddPage("General")` or add `TwTabPage`s; the selected page is shown. The strip is styled through `Strip` (`provider.SetClasses(tabs.Strip, "...")`); Ctrl+Tab / Ctrl+PageDown move between pages. |
| `TwSlider` | Slider (instead of TrackBar): `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`. Track `fill-*`, filled part `accent-*`, thumb `item:bg-*`. Keyboard, mouse wheel and dragging. |
| `TwProgress` | Progress bar (instead of ProgressBar): track `fill-*`, fill `accent-*`, corners `rounded-*`. `Indeterminate = true` draws a moving segment for work of unknown length. |
| `TwDataList` | Virtual list (see below). |
| `TwListBox` | Instead of ListBox: `Items` (+ `DisplayMember`), `SelectedItem(s)`, `FindString`; with TwDataList's virtual drawing, multi-selection and styles. |
| `TwCheckedListBox` | Instead of CheckedListBox: a rounded checkbox at the start of each row (`accent-*`), `CheckedIndices`/`CheckedItems`, `ItemCheck`; click the box or press Space. |
| `TwTreeView` | Virtual tree instead of TreeView: `Nodes`, nodes with icons, `SelectedNode`, `ExpandAll`; ←/→ expand/collapse, `HasLazyChildren` + `BeforeExpand` for lazy loading. Exposed to screen readers as an Outline. |
| `TwPager` | Paging (see [DataGridView](#datagridview)). |
| `TwField` | Label + editor + hint/error text; validation rules (see [Forms](#forms-validation-masks-and-dialogs)). |
| `TwToolTip` | Stylable tooltip: `Classes`; follows dark mode when `Provider` is set. |

### TwDataList

A virtual list that draws only the visible rows; the data is not stored in the list, rows are requested by index. Drawing time depends on the number of visible rows, even with a million rows.

```csharp
list.ItemCount = orders.Count;
list.ItemHeight = 52;                       // 96 DPI pixels
list.DrawItem += (s, e) =>
{
    var o = orders[e.Index];
    var c = e.ContentBounds;
    int left = e.DrawBadge(o.Status, c.Right, badgeBack, badgeFore);
    e.DrawText(o.Customer, new Rectangle(c.X, c.Y + 6, left - c.X - 12, 20), font: e.FontWithWeight(600));
    e.DrawText(o.Number, new Rectangle(c.X, c.Y + 26, left - c.X - 12, 20), e.MutedColor);
};
tw.SetClasses(list, "p-1 bg-white text-gray-900 item:hover:bg-gray-100 selected:bg-blue-50 selected:text-blue-900 "
                  + "row:border-gray-100 fill-gray-300 dark:bg-gray-800 dark:text-gray-100 dark:selected:bg-blue-950");
```

- Without `DrawItem`, `ItemText` (or the index) is drawn as a single line of text.
- The mouse wheel scrolls smoothly (`SmoothScrolling`); the scrollbar is owner-drawn, draggable and colored with `fill-*`.
- Keyboard: ↑/↓, PageUp/PageDown, Home/End; Enter and double-click raise `ItemActivate`.
- `item:rounded-*` row corners, `item:text-*` secondary text, `row:border-*` row separator, `ItemPadding` inner padding (`e.ContentBounds`).
- **Multi-selection:** `SelectionMode = MultiExtended`; Ctrl+click adds/removes, Shift+click and Shift+arrow select a range, Ctrl+A selects all (without storing indices, even with a million rows). `SelectedIndices`, `IsSelected`, `SetSelected`, `SelectionChanged`.
- **Groups:** `SetGroups(new[] { (0, "Today"), (12, "Yesterday") })`; the header of the group at the top sticks while scrolling. Headers are drawn with `header:bg-* header:text-*` or the `DrawGroupHeader` event.
- **Rows of different heights:** `ItemHeights = i => orders[i].HasNote ? 72 : 52` (96 DPI pixels); row offsets are computed once, call `InvalidateItemHeights()` when the heights change. Without it every row is `ItemHeight` tall and a million rows cost nothing extra; with it each row's height is asked for once.
- **Incremental loading:** `IsItemLoaded` draws unloaded rows as skeletons (with a gentle pulse) and `ItemsNeeded(first, last)` is raised with the visible range; call `Invalidate()` when the data arrives.

## Forms: validation, masks and dialogs

```csharp
var field = new TwField { LabelText = "National ID", Required = true, Validator = TwValidators.TcKimlikNo };
field.Controls.Add(new TwTextBox { Mask = TwMasks.TcKimlikNo });
tw.SetClasses(field, "field");                 // label:, hint:, error: parts
// On Save:
if (!tw.Validate(this)) return;                // shows the errors and focuses the first invalid field
```

- **`TwField`**: a label (" *" when required), a single editor and a hint or error text below it. Rules: `Required`, `Pattern` (regular expression), `Validator` (custom), completeness for masked fields. The label becomes the editor's `AccessibleName` and the error text its `AccessibleDescription`.
- **`tw.SetError(control, "message")`**: a manual error (e.g. from the server). While there is an error, the `invalid:` variant is applied; the `input` component includes a red border and ring. The error clears itself when the user changes the value. `HasErrors`, `ClearErrors()`, `ErrorsChanged`.
- **Built-in rules** (`TwValidators`): `TcKimlikNo` (Turkish national ID check digits), `Iban` (mod 97), `Email`, `TurkishMobile`, `MinLength(n)`.
- **Masks** (`TwTextBox.Mask`): `0` digit, `9` optional digit, `L` letter; built-in masks `TwMasks.Phone`, `Iban`, `TcKimlikNo`, `Date`, `PostalCode`. Only matching characters can be typed; pasting skips literal characters. `UnmaskedText`, `MaskCompleted`.

```csharp
if (TwDialog.Show(this, "Delete this order?", "Confirm delete", TwDialogKind.Warning, TwDialogButtons.YesNo, danger: true) == DialogResult.Yes) …
TwToast.Show(this, "Order saved", TwDialogKind.Success);
```

- **`TwDialog`** (instead of MessageBox): icon, title, message and right-aligned buttons; Enter is the default, Esc cancels/answers no. Follows the owner form's dark mode. Button texts are changed through `TwDialog.Texts`; `TwDialog.Create(...)` allows customization before showing.
- **`TwToast`**: a notification that stacks in the bottom-right corner of the owner form, does not steal focus, pauses while hovered and closes by itself (`DefaultDuration` 4 s). Announced to screen readers as an Alert.

## Accessibility

Owner-drawn controls are exposed with their items to screen readers and UI automation tools (Narrator, NVDA, FlaUI, Appium/WinAppDriver):

| Control | Role | Items |
| --- | --- | --- |
| `TwDataList` | List | Each row is a ListItem; name from `AccessibleItemText` (else `ItemText`, else "Item N"). Virtual: item objects are created on demand. |
| `TwTabStrip` | PageTabList | Each tab is a PageTab (with its selected state and default action) |
| `TwCalendar` | Table | "Previous month" / "Next month" buttons and days (name: "15 October 2026 Thursday"; days out of range are Unavailable) |
| `TwSelect` | ComboBox | The value is the selected item; the default action opens the menu |
| `TwSlider` / `TwProgress` | Slider / ProgressBar | Value |
| `TwTextBox`, `TwNumberBox`, `TwDatePicker` | (inner TextBox) | Name and description from the wrapper's `AccessibleName` / `AccessibleDescription`, else from the placeholder text |

Selection and focus changes are announced to screen readers. A control's `AccessibleRole` is used when set. Give fields without a visible label an `AccessibleName` (see `ControlsForm`). `TwTreeView` appears as Outline/OutlineItem (with expanded state and level), `TwPager` as ToolBar ("Page 3 of 20"), `TwToast` as Alert and `TwDialog` as Dialog.

UI Automation tests (`Category=UIAutomation`) verify the list, tabs, drop-down and calendar from outside, using the client that screen readers use. Because this client changes process-wide settings (text is drawn differently in later tests), these tests are skipped in the default run; run them separately:

```powershell
$env:FORMWIND_UIA = "1"; dotnet test tests/FormWind.Tests -f net472 --filter Category=UIAutomation
```

### High Contrast mode

When High Contrast is on in Windows (Settings → Accessibility → Contrast themes), FormWind uses system colors instead of class colors:

- Control backgrounds and text return to their original (system) values; selected and hovered parts (`selected:`, `item:hover:`, `row:hover:`) get the highlight color (`Highlight`).
- Borders and separators from classes are drawn in the text color; shadows, gradients and color transitions are not drawn. `TwButton`s without a border get a 1 px border.
- Spacing, font size, corners and layout do not change. If the mode changes while the application runs, styles are reapplied.
- Colors specific to this mode are given with the `forced-colors:` variant (as in Tailwind): `forced-colors:border-2`.
- To turn this off, set `TailwindProvider.RespectHighContrast = false`. Whether the mode is active can be read from `HighContrastActive`; code that draws by itself (e.g. badge colors in `DrawItem`) should check it.
- Grid cell types (`TwBadgeColumn`, `TwProgressColumn` …) keep their own colors.

### Contrast audit

`TwContrast` checks a form's current colors against WCAG 2 contrast ratios. Use it in tests or during development to verify the light and dark versions of a theme:

```csharp
tw.DarkMode = true;
foreach (var issue in TwContrast.Audit(this, tw))
    Debug.WriteLine(issue);   // TwButton 'btnSave' hover: 2.54:1 < 4.5:1 (#FFFFFF on #60A5FA)
```

| Checked | Section name |
| --- | --- |
| Controls with text (Label, button, TextBox, ComboBox, list, tree, GroupBox …) | `text` |
| The `hover:` variant in the classes (when a provider is given; no need to move the mouse) | `hover` |
| ToolStrips and menus: strip, item hover, drop-down menu, menu item hover | `strip`, `item:hover`, `menu`, `menu item:hover` |
| DataGridView cells, selected cells and headers | `cell`, `selected`, `header` |

- The threshold is 4.5:1 for normal text and 3:1 for large text (18 pt, or 14 pt bold) (WCAG AA). It can be changed with the `minimum` parameter.
- Transparent backgrounds are blended with the parent's background. Hidden controls are skipped.
- Disabled controls are outside WCAG's scope and are skipped by default. `includeDisabled: true` checks them too (part name `disabled text`); this catches disabled controls used to display content (e.g. a description box disabled so that no caret shows). For disabled native text boxes the background is the system background Windows paints. The harness runs the audit with this option. For disabled buttons the text color actually drawn is checked against 3:1: the aim is to catch invisible text, not to enforce contrast.
- For colors in the middle of a `transition`, the target color is checked; the audit can be called right after a theme change.
- `TwContrast.Ratio(foreground, background)` returns the ratio between two colors.
- `TwContrast.FindOwnerDrawn(root)` lists controls that paint their own colors (grids with `CellPainting` handlers or an overridden `OnPaint`, lists, trees, tabs and combo boxes with `OwnerDraw`/`DrawMode` on). The audit cannot know their colors on screen; check them by eye in dark mode.
- The default components (`btn-primary`, `btn-ghost`, `card`, `input`, `table`) pass this audit in light and dark mode (`ContrastTests`). `btn-primary` uses `blue-600`/`blue-700` in dark mode too; white text stays below the threshold on `blue-500` (3.7:1) and `blue-400` (2.5:1).

## Error handling

An error while applying styles does not crash the application: the exception is caught, the control keeps the style applied so far, and the error is reported through the `TailwindProvider.StyleError` event (or the Debug output when nobody listens). Exceptions thrown by a custom `IStyleApplier` or a `SetRowClasses` rule are reported this way; errors while painting shadows, borders and ComboBoxes do not stop painting either.

```csharp
tw.StyleError += (s, e) => logger.LogWarning(e.Exception, "Could not style {Target}: {Classes}", e.Target, e.Classes);
```

- A listener may rethrow the exception (e.g. to keep errors visible in tests).
- Critical exceptions such as out-of-memory are not caught.
- The failing control is retried on the next state change (hover, theme …).

## Right-to-left (RTL)

When a control's inherited `RightToLeft` is `Yes`:

- The `rtl:` and `ltr:` variants apply by direction (as in Tailwind): `pl-4 rtl:pl-0 rtl:pr-4`. Styles are reapplied when the direction changes.
- `TwPanel` flex and grid layouts are mirrored horizontally (like CSS `dir="rtl"`): the first item is on the right and `justify-start` aligns to the right. Margins swap with the mirror (`ml-*` acts as the start side).
- `TwButton` (the icon moves to the other side of the text, text alignment is mirrored), `TwCheckBox`, `TwRadioButton`, `TwSwitch` (indicator on the right, "on" position on the left) and `TwSelect` (arrow on the left) are mirrored.
- If the form has `RightToLeftLayout = true`, Windows mirrors the windows itself and FormWind does not mirror again.
- Not mirrored yet: `TwNumberBox`, `TwDatePicker`, `TwSlider`, `TwProgress`, `TwTabStrip` (tab order), `TwDataList`, `TwTreeView`, `TwCalendar`, the `DropDownList` ComboBox painting. There are no logical spacing classes such as `ps-*`/`pe-*`; use `rtl:` instead.

## .NET 9+ color mode (`Application.SetColorMode`)

On .NET 9 and later, `Application.SetColorMode(SystemColorMode.Dark)` darkens system colors and native controls. FormWind works together with it:

- Class colors take precedence over system colors; when classes are removed, the control returns to the dark system color.
- For `dark:` variants, bind `DarkMode` to the application's color mode: `tw.DarkMode = Application.IsDarkModeEnabled;`
- This behavior is verified on .NET 9 and 10 by `ColorModeTests` (run separately because it affects the whole process):

```powershell
$env:FORMWIND_COLORMODE = "1"; dotnet test tests/FormWind.Tests -f net10.0-windows --filter Category=ColorMode
```

## Krypton Toolkit

```csharp
FormWind.Krypton.KryptonApplier.Register();   // in Program.Main
```

For `KryptonButton`, `KryptonPanel`, `KryptonTextBox` and `KryptonLabel`, variants map to Krypton's state palettes: base → `StateCommon`, `hover:` → `StateTracking`, `active:` → `StatePressed`, `focus:` → `OverrideFocus`, `disabled:` → `StateDisabled`.

`KryptonDataGridView` is styled structurally (row height, grid lines, `row-header:hidden`, sort arrow) by FormWind's DataGridView applier. Because Krypton paints cells and headers with its own palette, the colors are also written to the palette: base classes and `cell:` → data cells, `header:` → column headers, `row-header:` → row headers (falling back to `header:`), `selected:` → selected cells, `bg-*` → empty area, `divide-*` → cell borders. To follow the same approach in your own applier, `TwAppliers.FindBuiltIn(control)` returns FormWind's built-in applier.

## DPI

Recommended setup: `AutoScaleMode = AutoScaleMode.Dpi` and `AutoScaleDimensions = new SizeF(96F, 96F)` on forms (for forms built in code, wrap the constructor in `SuspendLayout`/`ResumeLayout`), and run the application as `PerMonitorV2`. `samples/FormWind.Showcase/ControlsForm.cs` is set up this way; at 125% (120 DPI) the content scales and fits. Measurements:

| Setup | Result |
| --- | --- |
| DPI-unaware (the 4.7.2 default) | Windows scales the bitmap; the layout is correct, the text blurry. |
| PerMonitorV2 + `AutoScaleMode.Dpi` | Layout, fonts and FormWind spacing scale together. |
| PerMonitorV2 + `AutoScaleMode.Font` | Forms built in code and (on .NET Framework) Designer forms whose `Font` is assigned after `AutoScaleDimensions` did not scale; spacing and fonts grew, controls did not. |

- On 4.7.2, PerMonitorV2 is enabled with `System.Windows.Forms.ApplicationConfigurationSection` (`DpiAwareness`) in `app.config`. When editing this file by hand, do not remove the `bindingRedirect`s generated by the build; otherwise the `System.Text.Json` dependencies cannot be loaded and the application crashes at startup.
- On .NET Framework, WinForms applies its DPI setting when the first `Control` is created, while GDI+ reads the screen DPI once when it starts. If a `Font` is created before that (e.g. while loading the theme), GDI+ stays at 96 DPI and native controls (TextBox) draw their fonts too small. FormWind triggers WinForms' DPI initialization before touching GDI+; if your own code creates a `Font` in `Program.Main` before creating a form, call `new Control().Dispose()` first.
- PerMonitorV2 only takes effect when the application manifest declares Windows 10 compatibility (`supportedOS`); otherwise the process silently stays system-aware.
- **Moving between screens (150%, 200%):** when a form moves to a screen with a different DPI, FormWind reapplies the classes once after WinForms finishes scaling. On forms set up with `AutoScaleMode.Dpi`, fonts, spacing, owner-drawn controls and DataGridView (fonts and column widths) scale correctly; this was verified at 150% and 200% for `ControlsForm` and a grid form.
- **`AutoScaleMode.Font` and .NET Framework:** the Designer writes the form's `Font` after `AutoScaleDimensions`. With PerMonitorV2, .NET Framework WinForms skips scaling on that font assignment and updates `AutoScaleDimensions` as if it had scaled: controls keep their 96 DPI size while fonts (and FormWind spacing) grow with the screen DPI; text is clipped and grid columns stay narrow. This also happens without FormWind (the same form scales correctly when `Font` is assigned first) and does not happen on .NET 8. It cannot be fixed at run time: the overwritten value no longer tells at which DPI the form was designed. The **FW002** analyzer warns about these forms; the fix is `AutoScaleMode.Dpi` with `AutoScaleDimensions = new SizeF(96F, 96F)`.
- In Font-mode forms .NET Framework changes fonts without raising events when moving between screens; the inner box of `TwTextBox` kept a deleted font handle and drew its text small with the system font. `TwTextBox` now refreshes the inner font on the form's `DpiChanged` event (`Text_boxes_keep_a_valid_native_font_after_moving_to_200_percent`).
- Tests: the `Controls_*_at_150/200_percent` visual tests open the showcase in a separate PerMonitorV2 process and scale it with WM_DPICHANGED (`FormWind.Showcase.exe --snapshot x.png --controls --dpi 192`; only effective with the PerMonitorV2 configuration). The baseline name includes the process's system DPI.

Pixel values are scaled by the control's `DeviceDpi / 96`; values in classes are 96 DPI units (`p-2` is 10 px at 125%). On controls the application itself does not scale for DPI (e.g. UserControls whose AutoScale does not work), this gives different results than the original values.

- **The form's AutoScale:** `SetClasses` in `InitializeComponent` assigns the scaled value; the form's AutoScale (the final `ResumeLayout`) would then scale the same padding again (8 → 10 → 12 px at 125%). In nested UserControls with their own `AutoScaleMode` this scaling also happens after the handle is created. FormWind prevents this in two ways:
  - If the classes define padding or margin, FormWind's value is written back when it changes (`PaddingChanged`/`MarginChanged`).
  - The classes are reapplied once when the control's first handle is created (after scaling, before `Load`); size and font are corrected too.
- **Consequence:** as long as the classes manage padding/margin, run-time assignments to these properties by application code are undone (FW001 warns about them); to keep the application's value, remove padding/margin from the classes. Tracking stops when the classes are removed. Colors work the other way round: the application's assignment [is kept](#colors-assigned-by-application-code). No tracking happens in the Designer. When moving between screens, WinForms' scaling is undone for a moment and the DPI refresh then applies the new scale.
- Verification: compared with the original application at 125% on the converted Git Extensions forms; the remaining differences come from 1 px rounding and from controls the application never scales.
- Scaled pixels are rounded like WinForms does (.5 up).

If the application runs DPI-aware (e.g. `PerMonitorV2`) but the forms' AutoScale does not enlarge the layout, spacing grows while controls do not, and text does not fit. In that case fix AutoScale or set `TailwindProvider.ScaleWithDpi = false`. In DPI-unaware applications (the default for most 4.7.2 projects) Windows does the scaling and no extra setup is needed.

## Designer

- `TwTabControl`: "Add page" / "Remove selected page" commands and a smart tag (style selection); clicking a tab at design time switches the page; only `TwTabPage`s can be dropped onto the control itself, other controls are dropped onto the page. A control dropped from the Toolbox starts with two pages.
- The `TwTabControl` designer is built for .NET Framework and .NET 8; in the .NET 8 out-of-process Designer it runs in the server process (the commands are verified on both targets with `DesignSurface` tests).
- The "…" editor of the `Classes` field (`ClassesEditor`) only opens in the .NET Framework Designer. On .NET 8 the field is a drop-down that suggests the component names in the theme (`btn-primary`, `card` …) (`ClassesConverter`); a selected name replaces the field's value, and free text can be typed too.

### FW001 analyzer

The Roslyn analyzer shipped with the package warns when code assigns a property that classes manage:

```csharp
tw.SetClasses(btnSave, "btn-primary");
btnSave.BackColor = Color.Red;   // FW001: control 'btnSave' has classes; assigning 'BackColor' conflicts with them.
```

At run time a color assignment wins: FormWind leaves that color channel to the application (see [Colors assigned by application code](#colors-assigned-by-application-code)); the warning can be suppressed for deliberate assignments such as status colors. Padding and margin assignments, on the other hand, are undone and the class wins (see [DPI](#dpi)); here the warning points to a real conflict.

Only calls with constant class strings and properties the classes actually manage are checked (`bg-*` → BackColor, `text-{color}` → ForeColor, `text-{size}` → Font, `p-*` → Padding, `m-*` → Margin, `cursor-*` → Cursor; default components included). A plain `Font` assignment does not warn when only `font-sans` or `font-semibold` is present. To turn it off: `dotnet_diagnostic.FW001.severity = none` in `.editorconfig`.

### FW002 analyzer

In .NET Framework projects, warns when an `InitializeComponent` that uses `SetClasses` sets `AutoScaleMode.Font` and assigns the form's `Font` after `AutoScaleDimensions` (see [DPI](#dpi)): with PerMonitorV2 the form does not scale. The target framework is taken from `build_property.TargetFramework`, or else from the referenced WinForms version. If the application does not run PerMonitorV2, the warning can be turned off with `dotnet_diagnostic.FW002.severity = none` in `.editorconfig`.

## Animations and responsive variants

- The tab indicator (underline, pill, segment) slides to the new tab; list row hover backgrounds fade in; skeleton rows (list and grid) pulse gently. `TwMotion.Enabled` follows Windows' "Show animations" setting and can be turned off in code; there is no animation at design time or on invisible controls.
- `sm:`, `md:`, `lg:`, `xl:`, `2xl:` variants apply based on the form's client width (640 / 768 / 1024 / 1280 / 1536 px at 96 DPI; configurable via `TwBreakpoints`). When the form is resized and a threshold changes, only controls using these variants are restyled:

```csharp
tw.SetClasses(pnlForm, "flex flex-col gap-4 md:flex-row lg:gap-8");
tw.SetClasses(lblTitle, "text-lg lg:text-2xl");
```

## Project structure

```
src/FormWind/             Core: parser, theme, state engine, WinFormsApplier, Tw* controls, Designer editor
src/FormWind.Krypton/     KryptonApplier
src/FormWind.Analyzers/   FW001 Roslyn analyzer (included in the FormWind package)
tests/FormWind.Tests/     xUnit tests (net472, net8.0, net9.0, net10.0-windows)
samples/FormWind.Showcase/ Showcase application (net472)
samples/Rickipedia/       Sample page using the Rick and Morty API (net472)
tools/FormWind.Convert/   Command-line tool that converts Designer files to classes (net8.0-windows)
tools/FormWind.Harness/   Startup hook and harness for trying FormWind on real applications (net8.0-windows)
tools/FormWind.Docs/      Generates the class reference site with previews (net8.0-windows)
templates/                dotnet new template (formwind-app)
tools/run-harness.ps1     Runs a .NET WinForms application with the harness and prints a log summary
.github/workflows/ci.yml  Build, test, pack and upload of visual snapshots as artifacts
```

## Development

```bash
dotnet build FormWind.sln
```

```bash
dotnet test FormWind.sln
```

Performance and GDI tests (`Category=Performance`) are skipped in the default run. They take ~1 minute in total and do not run in parallel with other tests because they measure process-wide GDI counters. To run them and see the measurements, set the `FORMWIND_PERF=1` environment variable (PowerShell):

```powershell
$env:FORMWIND_PERF = "1"; dotnet test FormWind.sln --filter Category=Performance --logger "console;verbosity=detailed"
```

To run them from the Visual Studio Test Explorer, define the variable system-wide before opening Visual Studio.

### Public API

`src/FormWind/PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` list the library's public surface. The build warns when a public member is added that is not in the list, or a listed member is removed (RS0016/RS0017); new members go into the `Unshipped` file and move to `Shipped` when a version is released. Bulk add with the code fix:

```bash
dotnet format analyzers src/FormWind/FormWind.csproj --diagnostics RS0016 --severity warn
```

Only helpers used by the library itself (drawing, parser, internal appliers) are `internal`; extension points (`IStyleApplier`, `TwApplyContext`, `TwStyle`, `ITwChrome`) are public.

### Code coverage

Coverage is measured with the "Code Coverage" collector that ships with the test SDK (net472 and net8.0-windows; only `FormWind` and `FormWind.Analyzers`):

```powershell
dotnet test tests/FormWind.Tests -f net8.0-windows --settings tests/coverage.runsettings --collect "Code Coverage" --results-directory TestResults/net8.0-windows
```

```powershell
.\tools\check-coverage.ps1 -Results TestResults -Minimum 0.78
```

CI writes both targets' reports to the workflow summary as a table and fails if `FormWind` line coverage drops below 78% (currently ~81–83%). Coverlet is not used because it cannot resolve the WinForms framework assemblies.

### Invariant tests

`InvariantTests` applies a random sequence of operations to a random control (16 types: standard controls, ToolStrip, DataGridView, `Tw*` controls): assigning classes, dark mode, disabling, hover, changing the parent's color, application color assignments, recreating the handle. After every step it checks that the colors equal the resolved style (or the color the application assigned), and at the end that the original values return when the classes are removed.

The default run executes 300 scenarios. Scenarios are seeded; a failure message prints the seed and the operation sequence:

```powershell
$env:FORMWIND_INVARIANT_RUNS = "3000"; dotnet test tests/FormWind.Tests --filter FullyQualifiedName~InvariantTests
```

```powershell
$env:FORMWIND_INVARIANT_SEED = "162"; dotnet test tests/FormWind.Tests --filter FullyQualifiedName~InvariantTests
```

For every bug these tests found, `ApplierTests` also keeps an explicit regression test; seeds may not reproduce the same scenario if the generator changes.

### Trying real applications (harness)

`tools/run-harness.ps1` runs a .NET (Core) WinForms application with FormWind **without changing its source code**. `FormWind.Harness`, loaded via `DOTNET_STARTUP_HOOKS`, applies classes to every opened form by control type (`btn-ghost` on buttons, light/dark colors on text boxes, strip colors on ToolStrips …) and records the results:

```powershell
.\tools\run-harness.ps1 -Exe C:\apps\MyApp.exe -SelfTest -ExitAfter 60
```

| Parameter / environment variable | Effect |
| --- | --- |
| `-Dark` / `FORMWIND_DARK=1` | Starts in dark mode. **Ctrl+Alt+Shift+D** toggles the theme while running. |
| `-SelfTest` / `FORMWIND_SELFTEST=1` | Captures every new form in light and dark mode and audits its contrast both from the class colors and from the image. |
| `-ExitAfter 60` / `FORMWIND_EXIT_AFTER` | Ends the process after the given number of seconds (unattended runs). |
| `-Skip DataGridView,ToolStrip` / `FORMWIND_SKIP` | Does not style these types; to narrow a problem down to one control type. |
| `FORMWIND_DISABLE=1` | Turns the harness off; to compare the same scenario without FormWind. |
| `-Dump` / `FORMWIND_DUMP=1` | Applies no styles; writes the color, font and padding values of each form's controls to `dump-<form>.txt`. Run before and after the conversion tool and compare. |
| `FORMWIND_CLOSE=FormSettings` | Closes the named forms after processing them; to reach forms behind modal windows shown at startup without supervision. |
| `FORMWIND_WALK=Launch` | Clicks the buttons with this text on the first form one by one; processes and closes each form that opens and moves on; exits the process when done (e.g. every sample of a demo application). `FORMWIND_WALK_WAIT` is the wait per form (ms, default 4000), `FORMWIND_WALK_SKIP=33,40` the positions of buttons to skip. |
| `FORMWIND_OPEN=SettingsForm,AboutForm` | Finds these form types in the loaded assemblies and opens, processes and closes them one by one with their parameterless constructor; to reach forms opened from menus or the tray without supervision (reflection for testing). Forms that refuse to close are disposed. |
| `-Skip '*'` / `FORMWIND_SKIP=*` | Styles no controls but still measures; to compare the same scenario without FormWind (stalls, paint counts, application errors). |

Logs and images are written to `%TEMP%\FormWind-Harness\<process>-<time>\`:

| Line | Meaning |
| --- | --- |
| `FORM` | Form, number of styled controls and duration; `unstyled` lists unrecognized control types |
| `ADDED` | Controls added and styled later |
| `CONTRAST` | Pairs found by the contrast audit (light and dark mode) |
| `SCREEN` | Text below 3:1 in a self-test image: within the control's area the most frequent color counts as the background and the pixel with the highest contrast against it as the text. Catches what the class colors cannot show (disabled text drawn by Windows, controls painting with their own palette); the measurement is approximate because of font smoothing |
| `OWNERDRAW` | Controls that paint their own colors (`TwContrast.FindOwnerDrawn`): the contrast audit cannot measure them and the classes' cell/item colors may be ignored; check them by eye in dark mode |
| `UI STALL` | The UI thread froze for more than 250 ms |
| `PAINT STORM` | More than 100 paints per second for 3 seconds in a row: an invalidate-while-painting loop |
| `EXCEPTION`, `UNKNOWN` | An error while applying styles; an unknown class |

- Startup hooks only exist in .NET (Core) applications; they are not loaded in trimmed applications or when hooks are disabled. For .NET Framework applications, add `FormWindHarness.cs` from `FormWind.Harness` to the project and call `FormWindHarness.Install()` on the UI thread.
- The harness only descends into containers that carry content (forms, panels, GroupBox, tabs, SplitContainer, UserControl, ToolStrip containers). The inner parts of composite controls (e.g. the `TextBox` inside a `KryptonTextBox`, the help pane of a `PropertyGrid`, the edit box of a `NumericUpDown`) belong to the control itself and are not styled.
- Self-test images of top-level windows are taken with `PrintWindow`: the title bar, `RichTextBox` and MDI children look as on screen, and it works even when the window is covered by another one. MDI children (where `PrintWindow` also captures sibling windows on top) and failed captures fall back to `DrawToBitmap`, where the title bar does not look as Windows draws it and `RichTextBox` is drawn incorrectly.
- Forms are found with a timer, so this works whether the application runs a WinForms or a WPF message loop (e.g. Greenshot). Classes are applied after a form is shown, so it may appear unstyled for a moment at startup; colors the application assigned at that point count as original values.
- When you find a problem, first reproduce it without FormWind using `FORMWIND_DISABLE=1`; if it still happens, it belongs to the application. Then narrow it down to one control type with `-Skip`.
- Applications tried: Git Extensions (.NET 10, startup hook), Greenshot (.NET Framework 4.8, WPF loop), Greenshot 1.3 (.NET Framework 4.7.2, WinForms loop, harness as code), mRemoteNG (.NET 10, DockPanelSuite), the Krypton Toolkit samples (.NET 9: grid, input form, ribbon, docking), the ScottPlot WinForms demo (.NET 10, 45 demos, with `FORMWIND_WALK`), an MDI shell (.NET 8: merged menus, window list, maximized child, children opened later). The problems found became the behaviors and tests described in this README. In applications with their own theme engine (mRemoteNG) dark mode shows only partially, because colors assigned by the application are kept; this is expected.

### Visual regression tests

`tests/FormWind.Tests/Visual` renders the showcase forms (`MainForm`, `ControlsForm`) and the calendar in light and dark mode and compares them with the PNGs in `tests/FormWind.Tests/Baselines` (net472 only). A pixel counts as different when the sum of its RGB differences exceeds 48; if more than 0.2% of pixels differ the test fails and `*.actual.png` plus a red-marked `*.diff.png` are written to `VisualDiffs` in the output folder.

Because font rendering depends on the environment, baselines are kept per environment: `Baselines/win26200-96dpi-cleartype/` (Windows build, DPI, font smoothing). If there is no folder for the current environment, the tests are skipped. After intentional visual changes, regenerate the baselines and commit them after reviewing the PNG differences:

```powershell
$env:FORMWIND_UPDATE_BASELINES = "1"; dotnet test tests/FormWind.Tests -f net472 --filter Category=Visual; $env:FORMWIND_UPDATE_BASELINES = ""
```

**CI.** `.github/workflows/ci.yml` builds, tests on four targets (net472, .NET 8, 9, 10), checks coverage and packs on every push and PR. Because `FORMWIND_VISUAL_EXPORT` is set, the visual tests write their renderings to the `visual-snapshots` artifact; if there is no baseline for the CI environment yet, the tests only export and the workflow prints a warning. CI is pinned to the `windows-2022` image; if the image changes, the baseline folder name changes too. Adding the `win…` folder from the artifact under `tests/FormWind.Tests/Baselines/` and committing it enables comparison for that environment.

### Documentation site

`tools/FormWind.Docs` renders every example off-screen with FormWind itself (PNG; light and dark for components and controls) and generates a class reference with previews; the color palette (v3 and v4) and icon names are included too:

```bash
dotnet run --project tools/FormWind.Docs -c Release -- artifacts/docs-site
```

The examples are defined in `tools/FormWind.Docs/Catalog.cs`. `.github/workflows/docs.yml` generates the site on every push and uploads it as the `docs-site` artifact; to publish to GitHub Pages, set the Pages source to "GitHub Actions" in the repository settings and define the `DEPLOY_DOCS=true` repository variable.

### Conversion tool

Converts hard-coded style assignments in existing forms to classes:

```bash
dotnet run --project tools/FormWind.Convert -- src/MyApp --report conversion.md
```

```bash
dotnet run --project tools/FormWind.Convert -- src/MyApp --write
```

| Designer line | Class |
| --- | --- |
| `BackColor` / `ForeColor` | `bg-*` / `text-*`: the palette color, the closest palette color within 10 units (with a note), otherwise `bg-[#hex]` |
| `Font` | `text-sm` … `text-4xl` or `text-[13px]`; a `Semibold`/`Bold` family or `FontStyle.Bold` → `font-semibold`/`font-bold`; `italic`, `underline` |
| `Padding` | `p-*`, `px-*`/`py-*` or per side (4 px = 1 unit; `[7px]`) |
| `FlatAppearance` | `BorderSize` → `border-0`/`border`, `BorderColor` → `border-*`, `MouseOverBackColor` → `hover:bg-*`, `MouseDownBackColor` → `active:bg-*` |

- The default is a dry run: it prints what would change and the notes. `--write` updates the files in place; commit first. The files' BOM is preserved.
- Converted lines are removed and a `SetClasses` call is written instead; if the control already has a `SetClasses` call, the classes are appended to it. A `TailwindProvider` is added if the file has none, and it is added to the `components` container so it is disposed with the form. If the class has no `components` field, one is added; if `Dispose` does not call it, a note is added to the report.
- Code from Visual Studio's new Designer, which does not write `this.` (`buttonOk.BackColor = ...`), and controls generated as local variables (`GenerateMember = false`) are supported; the control is passed to `SetClasses` the way it is written in the source.
- Only **Control**s created in `InitializeComponent` are converted. ToolStrip items, columns and local cell styles (`dataGridViewCellStyle1`) cannot take `SetClasses`; their lines stay. The application's own types are resolved to a WinForms type by following the `class X : Y` declarations in the target folder and its parent folder (sibling projects) (e.g. a class deriving from `ToolStripComboBox` is not converted); types that cannot be resolved are converted with a note.
- The old Designer format (`this.` prefix, brace on the same line as the method, tab indentation) is supported too.
- **Visual Basic:** `.Designer.vb` files are converted too (a folder is searched for both; the language is detected from the file's content). The output uses VB syntax: `Me.TailwindProvider1.SetClasses(btnSave, "...")`, the field is `Friend WithEvents TailwindProvider1 As FormWind.TailwindProvider`; without fields it is added before `End Class`. Font sizes like `10.5!`, color components like `CType(CType(37, Byte), Integer)` and `CByte(37)`, and lines with or without the `Me.` prefix (new Designer) are recognized. The application's own types are resolved from `Class X` / `Inherits Y` declarations. On a form created with `dotnet new winforms -lang VB` and styled by hand, the converted project built without warnings or errors, and colors and fonts were identical in a `-Dump` comparison.
- System colors (`SystemColors.*`), the application's own color expressions (`AppColor.X.GetThemeColor()`), derived fonts (`new Font(Font, FontStyle.Bold)`) and font families other than the theme's are not converted; the line stays and is noted in the report.
- **Verified on a real project (Greenshot 1.3, .NET Framework 4.7.2):** the converted project built with Visual Studio's MSBuild without warnings or errors; colors and fonts were identical on 311 controls in 7 forms, padding differed by 1 px (DPI rounding).
- **Verified on a real project:** 360 classes were generated in 74 Designer files of Git Extensions; the result built without warnings or errors (in a project that treats warnings as errors and uses StyleCop). In values taken with the harness's `-Dump` mode before and after the conversion, colors and fonts of 270 controls were identical; the padding differences came from DPI behavior and revealed two library bugs (double scaling; see [DPI](#dpi)).
- `--config tw.config.json` uses the application's own palette; `--tolerance` changes the closest-color threshold.

Screenshot of the showcase form (`--controls` opens the controls form):

```bash
samples/FormWind.Showcase/bin/Debug/net472/FormWind.Showcase.exe --snapshot light.png
```

## Packaging

```bash
dotnet pack FormWind.sln -c Release
```

Packages are written to `artifacts/packages` (`FormWind`, `FormWind.Krypton` and `.snupkg` symbol packages). The `FormWind` package includes the FW001 analyzer under `analyzers/dotnet/cs`. Packages include SourceLink information, so the debugger can step into FormWind's source from GitHub. For local use, add this folder to Package Sources in Visual Studio.

## Known limitations and roadmap

- [ ] The Designer may also write the `BackColor`/`Font` values applied in the preview to `Designer.cs`. `SetClasses` overrides them at run time; use `DesignTimePreview = false` to keep the file clean.
- [ ] Quick component selection via smart tag (for standard controls; `TwTabControl` has a smart tag)
- [ ] Self-drawing controls (e.g. owner-drawn grids) ignore cell styles; they may stay light in dark mode
- [x] `row-span`, `grid-rows` for grid; list rows with varying heights (`ItemHeights`); a DataGridView VirtualMode helper (`TwVirtualRows<T>`)
- [ ] Shadow animations
- [ ] Porting `ClassesEditor` to the .NET 8+ out-of-process Designer with the Designer SDK
- [ ] Choosing a license (before publishing the package)
