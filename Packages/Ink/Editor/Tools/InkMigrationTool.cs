using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// One-time helper for upgrading a project from ink-unity-integration 1.x to 2.0.
	///
	/// The 1.x pipeline generated a .json TextAsset next to each .ink file. In 2.0 the compiled story is
	/// stored inside each .ink file's imported InkFile, so those .json files are no longer used. This tool
	/// finds and deletes them. Reference rewiring can't be automated (a TextAsset field can't hold an
	/// InkFile), so it also reminds you to update your code to reference InkFile / inkFile.storyJson.
	/// </summary>
	public static class InkMigrationTool {
		const string codeReminder =
			"Also update your scripts: reference the .ink file's InkFile (not the old TextAsset) and use " +
			"new Story(inkFile.storyJson) instead of new Story(textAsset.text).";

		[MenuItem("Assets/Migrate Ink Project from 1.x", false, 205)]
		public static void Migrate () {
			var oldJson = AssetDatabase.FindAssets("t:TextAsset")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(p => p.EndsWith(".json") && IsCompiledInkJson(p))
				.ToList();

			if (oldJson.Count == 0) {
				EditorUtility.DisplayDialog("Migrate Ink Project from 1.x",
					"No leftover compiled ink .json files were found — nothing to delete.\n\n" + codeReminder, "OK");
				return;
			}

			if (!EditorUtility.DisplayDialog("Migrate Ink Project from 1.x",
				$"Found {oldJson.Count} compiled ink .json file(s) left over from ink 1.x. In 2.0 the compiled " +
				"story lives inside each .ink file's InkFile, so these are no longer used.\n\nDelete them?\n\n" + codeReminder,
				$"Delete {oldJson.Count} file(s)", "Cancel"))
				return;

			AssetDatabase.StartAssetEditing();
			try {
				foreach (var path in oldJson) AssetDatabase.DeleteAsset(path);
			} finally {
				AssetDatabase.StopAssetEditing();
			}
			Debug.Log($"Ink migration: deleted {oldJson.Count} old compiled .json file(s):\n{string.Join("\n", oldJson)}\n\n{codeReminder}");
		}

		// A compiled ink story JSON begins with an "inkVersion" field, so we only need to sniff the start.
		static bool IsCompiledInkJson (string jsonPath) {
			try {
				using (var reader = new StreamReader(InkEditorUtils.UnityRelativeToAbsolutePath(jsonPath))) {
					var head = new char[256];
					int read = reader.Read(head, 0, head.Length);
					return new string(head, 0, read).Contains("inkVersion");
				}
			} catch {
				return false;
			}
		}
	}
}
