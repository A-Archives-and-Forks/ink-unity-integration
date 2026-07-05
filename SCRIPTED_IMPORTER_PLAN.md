# Ink ScriptedImporter — Implementation Plan

Dev planning doc (repo root, **not** shipped in the `Packages/Ink` UPM package).

## Goal
Replace the postprocessor/queue compile pipeline with a `ScriptedImporter` so `.ink`
imports directly into an `InkFile` asset, eliminating the `.json` sidecar. Breaking
change (v2.0.0): users delete `.json` and repoint references to `InkFile`; an
auto-migration comes later.

## Principles
- Build on this branch off the modernized `master`. If abandoned, `master` keeps every
  standalone win — this is an isolated bet.
- Preserve the **user-facing model/behaviors** (master files, include handling,
  auto-compile, compile-summary logs, play-mode block). Replace the **machinery**.
- Each phase is a validatable milestone on the branch, compiled/tested in Unity as we go.

## Key design decisions
- **A. `InkFile` name clash.** The new imported asset (runtime `ScriptableObject`) takes
  the name `InkFile` (users reference it). The existing editor graph-node class
  `InkFile` is renamed `InkFileMetadata` (or folded into `InkLibrary`), which is fine
  since `InkLibrary` demotes to editor-only.
- **B. Master flag on the importer.** Serialized `bool compileAsMasterFile` (default
  **false**) on `InkImporter` replaces `InkSettings.includeFilesToCompileAsMasterFiles`.
  Per-file, deterministic, worker-safe; preserves the master-file workflow.
- **C. `InkLibrary` → editor-only convenience** (graph browsing, Player Window,
  inspector relationships). Not consulted during import.

## Why compilation stays "async" and keeps the play-mode block
`OnImportAsset` is synchronous per file, but Asset Pipeline V2 runs imports on background
worker processes → the editor stays responsive and files compile in parallel, with no
hand-rolled threading. The "don't enter play mode while compiling" guarantee is editor-side
and is retained, wired to "ink imports pending?". Compile-summary logs are reproduced via a
thin `OnPostprocessAllAssets`.

## Phases

### Phase 1 — Core importer + `InkFile` asset (the swap)
- Runtime asmdef + `Packages/Ink/Runtime/InkFile.cs` (`ScriptableObject`: `storyJson`,
  errors/warnings/todos). Move `InkCompilerLog` to runtime and **fix its `using
  UnityEditor;`** (latent player-build break).
- `InkImporter.cs` (`[ScriptedImporter(1,"ink")]`): compile via `new Compiler(text,{fileHandler=UnityInkFileHandler,...})`,
  store JSON+logs, `AddObjectToAsset`/`SetMainObject`, errors via `ctx.LogImportError`.
- Disable the old compile trigger in `InkPostProcessor`.
- Resolve decision A (rename old `InkFile` → `InkFileMetadata`).
- **Accept:** standalone `.ink` → `InkFile` with correct JSON; `new Story(inkFile.storyJson)` runs; no `.json`.

### Phase 2 — Includes & master files
- `InkImporter.GatherDependenciesFromSourceFile` (static, recursive INCLUDE parse) for
  nested reimport + import ordering; `ctx.DependsOnSourceAsset` for direct includes.
- `compileAsMasterFile` (default false); compile only masters; includes import without JSON.
- **Accept:** 2-level include chain recompiles on nested change; no standalone-include error spam; large include-heavy project imports cleanly.

### Phase 3 — Editor UX & retained behaviors
- Delete `DefaultAssetEditor`/`DefaultAssetInspector`; use `[CustomEditor(typeof(InkFile))]`
  and/or `ScriptedImporterEditor` (for the master toggle). Port inspector content.
- Compile-summary log via `OnPostprocessAllAssets` (gated by settings).
- Retain play-mode guard, rewired to pending imports.
- Assign icon directly to `InkFile`; drop `InkBrowserIcons.OnDrawProjectWindowItem`.

### Phase 4 — Reconcile retained systems
- `InkLibrary` → editor-only; rename node type; strip compile authority.
- `InkCompiler` → remove threaded queue/play-mode-delay/build-lock (Unity schedules now).
- `InkEditorUtils` → `ForceRecompileAllInkFiles(Async/Sync)` via
  `StartAssetEditing`/`ImportAsset`/`StopAssetEditing` (CI-callable).
- `InkSettings` → drop master/auto-compile lists now on the importer.
- `InkPreBuildValidationCheck` → validate via importer results.

### Phase 5 — Migration, docs, versioning
- Migration: old master lists → `compileAsMasterFile`; optional rewire `TextAsset`→`InkFile` + delete `.json`.
- Update demos to `InkFile`. Migration guide + CHANGELOG. **Version → 2.0.0**.

## Risks / open items
- `ScriptedImporterEditor` vs `CustomEditor(InkFile)` — master toggle lives on the importer.
- Import-worker determinism — nothing in `OnImportAsset` may touch main-thread/singleton state.
- Cold-rebuild import ordering via `GatherDependenciesFromSourceFile`.
- `.json`/GUID churn — migration must repoint references before deleting.

## First step
Phase 1 as a spike to de-risk the core (importer + `InkFile` asset + compile) before Phases 2–5.
