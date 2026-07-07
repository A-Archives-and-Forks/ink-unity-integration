# Dev Readme

This plugin is now designed to be imported as a UPM Package.
Our approach is to point [OpenUPM at a Git Repo](https://openupm.com/packages/com.inklestudios.ink-unity-integration/) with the assets in the Packages folder.
Demos are packaged up as separate .unitypackage files.

## Which Unity version to package from
Do the packaging from the **minimum supported Unity version** — the `unity` field in
`Packages/Ink/package.json` (currently **2022.3 LTS**) — not a newer editor.

- The **`.unitypackage`** (produced by "Prepare for publishing", then both attached to the GitHub
  release and uploaded to the Asset Store — it's the same file) ships serialized assets: the demo
  scene, prefabs, `InkSettings.asset`, icon import settings, and every `.meta`. Exporting from a
  newer editor bakes in newer `serializedVersion`s, so users on the minimum version get "serialized
  with a newer version of Unity" warnings or import oddly. Exporting from the floor guarantees the
  oldest supported editor reads everything cleanly. (Keep the minimum LTS installed just for packaging.)
- The **UPM / OpenUPM** branch is Unity-agnostic — it's a `git subtree` split of the `Packages`
  folder created by the tag-push action, so no editor runs. The version only matters for the
  exported `.unitypackage`.
- Before packaging, open a **clean** checkout in 2022.3 and check `git status`: if a newer editor
  re-serialized `.meta` files, discard that churn so the shipped package stays 2022.3-serialized.

## To update create a new release
- Update CHANGELOG.md with a list of changes (these will then autopopulate)
- Increase the version number in InkLibrary.cs
- Open the Ink Publishing Tools wizard with the 'Publishing/Show Helper Window' menu item
- Click 'Prepare for publishing'. This will run a bunch of automated tasks and produce a .UnityPackage which we'll need later. **Do this from the minimum supported Unity version — see "Which Unity version to package from" above.**
- You can click the 'Show Package' button to reveal the packages in Finder
- Commit any changes and push to Master, tagging with the version in the format (x.x.x)
- This causes an Action to trigger, which will create a UPM branch automatically. [Check it succeeded on OpenUPM](https://openupm.com/packages/com.inkle.ink-unity-integration/?subPage=pipelines)
- Click 'Draft GitHub Release'. This will take you to a page with most of the fields auto-populated. Drag the new package into the release and hit Publish.