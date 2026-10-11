# Changelog

All notable changes to FormWind are documented in this file. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](CONTRIBUTING.md#versioning).

## [Unreleased]

## [0.1.0] - Unreleased

First public preview.

### Added

- **Utility classes for WinForms** through `TailwindProvider`: colors with opacity, typography and font weights, spacing,
  sizes, borders and corners, shadows and rings, gradients, cursors, visibility.
- **Variants:** `hover:`, `focus:`, `focus-visible:`, `active:`, `disabled:`, `checked:`, `invalid:`, `group-hover:`,
  `dark:`, responsive `sm:` … `2xl:`, `rtl:`/`ltr:` and `forced-colors:`, and their combinations.
- **Layout:** flex and grid on `TwPanel` (gap, justify, align, wrap, basis, grow/shrink, `col-span`, `row-span`, `grid-rows`),
  mirrored for right-to-left.
- **Themes:** JSON theme file (`tw.config.json`) with custom colors, components, font stacks, bundled fonts and icons;
  Tailwind v3 palette by default and the Tailwind v4 (OKLCH) palette with `"palette": "v4"`.
- **Controls:** `TwButton`, `TwPanel`, `TwTextBox`, `TwNumberBox`, `TwSelect`, `TwDatePicker`, `TwCalendar`, `TwCheckBox`,
  `TwRadioButton`, `TwSwitch`, `TwSlider`, `TwProgress`, `TwTabStrip`, `TwTabControl`, `TwDataList`, `TwListBox`,
  `TwCheckedListBox`, `TwTreeView`, `TwPager`, `TwField`, `TwDialog`, `TwToast`, `TwToolTip`, `TwIcon`, `TwPictureBox`.
- **Standard controls:** ToolStrip/menu renderer, DataGridView part prefixes, column classes, cell types, row rules,
  sorting, filtering, column chooser, virtual rows; framed `TextBox` and painted `DropDownList` `ComboBox`; dark title
  bars, Windows 11 backdrops, dark native scrollbars and tooltips.
- **Accessibility:** UI Automation trees for owner-drawn controls, WCAG contrast audit (`TwContrast`), Windows High
  Contrast support (`RespectHighContrast`), keyboard support throughout.
- **Localization:** `TwTexts` with English and Turkish texts, chosen from the UI culture.
- **Robustness:** styling errors are reported through `TailwindProvider.StyleError` instead of crashing the application.
- **Tooling:** FW001 and FW002 Roslyn analyzers, Designer smart tags and preview, Designer file converter
  (`tools/FormWind.Convert`), real-application harness (`tools/run-harness.ps1`), documentation site generator
  (`tools/FormWind.Docs`), `dotnet new formwind-app` template (`FormWind.Templates`).
- **Integrations:** Krypton Toolkit applier (`FormWind.Krypton`), compatibility with .NET 9+ `Application.SetColorMode`.
- Targets .NET Framework 4.7.2 and .NET 8; tested on .NET Framework, .NET 8, 9 and 10.

[Unreleased]: https://github.com/IsmailYucelOlmez/FormWind/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/IsmailYucelOlmez/FormWind/releases/tag/v0.1.0
