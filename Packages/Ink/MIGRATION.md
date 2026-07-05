# Migrating to Ink for Unity 2.0

Version 2.0 replaces the old compilation system (separate `.ink` source and generated
`.json` files, managed by `InkLibrary`/`InkCompiler`) with a Unity **ScriptedImporter**.
Each `.ink` file now imports directly into an **`InkFile`** asset that holds the compiled
story. There is no longer a separate `.json` file.

This is a breaking change. Here's how to upgrade an existing project.

## 1. Update Unity
The minimum supported version is now **Unity 2022.3 LTS**. Open your project in 2022.3+.

## 2. Mark your master files
A `.ink` file is only compiled into a runnable story when it is a *master* file.
Select each standalone story's `.ink` file in the Project window and, in the Inspector's
**Import Settings**, tick **Compile As Master File**, then click **Apply**.

Files that are only ever `INCLUDE`d by another file should be left unticked — they are
compiled as part of the master that includes them, and no longer produce their own errors.

> In 1.x, master files were auto-detected (any file not included by another). In 2.0 you
> designate them explicitly, which removes a whole class of ambiguity and cross-file scanning.

## 3. Update your code and references
Game code previously referenced the generated `.json` as a `TextAsset`:

```csharp
[SerializeField] TextAsset inkJsonAsset;
var story = new Story(inkJsonAsset.text);
```

Now reference the `.ink`'s imported `InkFile` directly:

```csharp
using Ink.UnityIntegration;

[SerializeField] InkFile inkFileAsset;
var story = new Story(inkFileAsset.storyJson);
```

Reassign the field in any scenes/prefabs (the old `TextAsset` reference won't carry over to
the new field type).

## 4. Delete the old generated `.json` files
The compiled JSON now lives inside the `InkFile` import artifact, so the committed `.json`
files next to your `.ink` files are no longer used. Delete them (and stop committing them).

## Notes
- Compiler errors/warnings/todos now show on the `.ink` file's import inspector and in the
  console. A build is blocked if any master file has compile errors.
- Editing an include file automatically reimports the master file(s) that include it,
  including nested includes.
- The "Rebuild Ink Library" menu is gone. Use **Assets ▸ Recompile All Ink Files (Async/Sync)**
  if you ever need to force a full reimport (the Sync variant is suitable for build scripts).
