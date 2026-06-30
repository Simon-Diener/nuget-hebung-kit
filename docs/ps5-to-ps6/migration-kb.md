# PS5 → PS6 migration knowledge base

> Distilled from the product team's PS6 release notes ("6.0.0") and the official
> "Adjustments" migration checklist (valid up to PS5 5.4.0.392). The orchestrator
> and the `ps5-to-ps6-*` agents consult this before touching a project.
> **Core idea:** PS6 targets .NET 8 (PS5 targeted .NET Framework 4.7.1). Migrate
> projects to **SDK-style with `PackageReference`**, uninstall all previous
> packages/references, then install the PS6 package set. .NET 8 loads transitive
> dependencies automatically — do NOT re-add a package just because PS5 listed it;
> add back only what the build proves is missing AND that has a net8 build.

## Per-project target framework

| Project role | TargetFramework |
|---|---|
| Service | `net8.0` |
| RichClient | `net8.0-windows` |
| PublishingService | `net8.0` |
| Configuration | `net8.0` |

## Package rename map (old → new)

| Old | New |
|---|---|
| `LicenseTool` | `Noxum.PS5.SoftDevTools.LicenseTool` |
| `PS5WinClient` | `Noxum.PS5.Application.RichClient` |
| `Noxum.IDML.*` (writers/readers/etc.) | `Noxum.Publishing.*` (`*.Cmd` for command-line writers) |
| `Noxum.Publishing.HtmlWriter.IdmlToHtmlConverter` | `Noxum.Publishing.HtmlWriter.IpubToHtmlConverter` |
| `Noxum.Publishing.Core.IdmlExtension` | `Noxum.Publishing.Core.IpubExtension` |
| XML namespace `ipub:IdmlExtension` | `ipub:IpubExtension` |
| `Noxum.IDML.PdfWriter.exe` | `Noxum.Publishing.FoWriter.Cmd.dll` + AntennaHouse wrapper (see config transforms) |

## Required & optional packages per project type

**Service** (`net8.0`) — required: `Noxum.PS5.Service` 5.4.0+, `Noxum.PS5.Server` 5.4.0+,
`Noxum.PS5.Publishing.PS5Transformer` 5.4.0+, `Noxum.PS5.WindowsService.BinaryServiceModule` 5.4.0+,
`Noxum.PS5.Types` 2.0.0+, `Noxum.Publishing.Core` 2.1+, `Noxum.Publishing.HtmlWriter` 2.1+,
`Noxum.Publishing.ImageConverter` 1.4.1+, `Noxum.Publishing.Wrappers.*` 1.4+, `Noxum.Storage.Messaging.Mail` 1.1.0+.
Optional: `Noxum.PS5.Editors.ObjectDataExport.Server` 3.0.0+, `Noxum.PS5.Editors.ObjectDataImport.Server` 3.0.0+,
`Noxum.Editing.ImageLabelingEditor.PS5AuxProcessor` 2.0.0+, `Noxum.PS5.Extensions.ConditionalContent.Server` 2.0.0+,
`Noxum.PS5.Reports.*` 2.0.0+, `Noxum.PS5.Workflow.ActivityWorkflowManager` 2.0.0+,
`Noxum.PS5.Workflow.ActivityWorkflowConfiguration` 2.0.0+, project-specific server-side packages.

**RichClient** (`net8.0-windows`) — required: `Noxum.PS5.Application.RichClient` 5.4.0+, `Noxum.PS5.Client` 5.4.0+,
`Noxum.PS5.Win32` 5.4.0+, `Noxum.PS5.Types.DragDrop` 2.0.0+, `Noxum.PS5.Types.UITypeEditors` 2.0.0+,
`Noxum.Publishing.XamlWriter` 2.0.0+, `Noxum.Icons.Desktop.Controls` 1.1.2+, `Noxum.Localization.Controls` 1.1.0+,
`ActiproSoftware.Controls.WinForms.SyntaxEditor.Addons.XML` 23.1.3.
Optional: the `Noxum.PS5.Editors.*` client/editor set 2.0.0+/3.0.0+, `Noxum.Editing.*` editors,
`Noxum.PS5.Extensions.ConditionalContent.Client` 2.0.0+, `Noxum.PS5.Extensions.ContentChecker.Control` 2.0.0+,
project-specific client-side packages.

**PublishingService** (`net8.0`) — required: `Noxum.Services.Publishing.Service` 3.0.1+,
`Noxum.Publishing.ParameterExtractor` 2.0.0+, `Noxum.Compression.Zip` 2.0.0+, `Noxum.Publishing.Core` 2.1+,
`Noxum.Publishing.*Writer.Cmd` / `*Reader.Cmd` / `*ToStyle.Cmd` 2.1+, `Noxum.Publishing.XmlValidator.Cmd` 2.1+,
`Noxum.Publishing.XslTransformer.Cmd` 2.1+. Optional: `Noxum.Publishing.ObjectData.*` 2.0.1+.

**Configuration** (`net8.0`) — required: `Noxum.PS5.Configuration` 5.4.0+.
Optional: the `Noxum.PS5.Editors.*.Configuration(s)` set 2.0.0+/3.0.0+, `Noxum.Editing.*.Configuration(s)`,
`Noxum.Publishing.ObjectData.Schema` 2.0.1+.

## Config-file transforms

- **App settings:** replace `PS5Service.exe.config` → `appsettings.json`; `PS5WinClient.exe.config` → `PS5WinClient.dll.config`;
  `Noxum.PS5.Publishing.PS5Transformer.exe.config` → `appsettings.json` (ConnectionStrings only). PublishingService 3.0.1 needs its own `appsettings.json`.
  Note: for an "InProc" service the client's `.dll.config` is no longer used — the InProc service runs in its own process and needs its own `appsettings.json` (including logging switches).
- **GlobalDefinitions.config:** raise `globalDefinitions/@version` to `6.0.0`.
- **InfoProviderDefinitionList.config:** set `PS5.TopicTypeIcon` type to `Noxum.PS5.Controls.PSUIInfoProvider, Noxum.PS5.Controls`.
- **PublisherDefinitionList.config:** remove `server`, `tcpPort`, `endpoint` attributes (moved to appsettings.json).
- **XSLT:** remove `msxsl:script` blocks (call an extension function instead); replace external-URI `document()` with `nxps:ResolveConfiguration(...)`; replace `assembly=mscorlib` with `assembly=System.Private.CoreLib` (not needed in XAML/RESX).
- **Publishing framework references:** replace all `Noxum.IDML.*` type strings with `Noxum.Publishing.*`; `IdmlToHtmlConverter`→`IpubToHtmlConverter`; `IdmlExtension`→`IpubExtension`; `ipub:IdmlExtension`→`ipub:IpubExtension`.
- **PublishingService.config:** validate against schema; set `remotingtcpport="-1"`; replace `Noxum.Publishing.Wrappers.*.exe`→`.dll` and `Noxum.Publishing.ObjectData.*.exe`→`.dll` (also in ParameterExtractor `-executable` args); replace `Noxum.IDML.PdfWriter.exe` with `Noxum.Publishing.FoWriter.Cmd.dll` followed by a `Noxum.Publishing.Wrappers.AntennaHouse.dll` executable element; replace remaining `Noxum.IDML.*.exe` with the equivalent `Noxum.Publishing.*.Cmd.dll`.

## Source-code breaking changes

- **`Thread.Abort()` removed.** Threads can no longer be aborted in .NET 8. Convert to cooperative cancellation via `CancellationToken` for subclasses of:
  - `Noxum.PS5.Server.Jobs.ServerJob` (async server jobs) — honor cancellation, stop reliably within timeout.
  - `Noxum.PS5.Server.Data.PreparationModule` — same, in the `Execute` override.
  - `Noxum.PS5.Server.ServiceModules.ServiceModuleBase` and the `Noxum.PS5.WindowsService.BinaryServiceModule` base classes (`PreviewConverterBase`, `BinaryMetaCollectorBase`, `BinaryAuxiliaryProcessorBase`) — Task/CancellationToken lifecycle; finish within the stop timeout. Prefer modules that manage an external program's lifetime (processes can still be killed; threads cannot).
- **`msxsl:script` removed** in XSL transforms (see config transforms — move to extension functions).
- **Removed controls/types:** `Noxum.PS5.Controls.BinaryImportWPF`, `Noxum.PS5.Windows.Controls.BinaryImport`, `Noxum.PS5.Windows.Controls.WorkflowStatusInfo`, the `WpfConverter` preview converter.
- **Removed capabilities:** Windows Event Log support; multiple configurations ("Mandators"); multiple user sessions; custom `InProcServer_HttpServiceModuleType`.
