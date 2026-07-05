using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Automatically detects which .ink files are master files — i.e. not INCLUDEd by any other .ink
	/// file — and records that on each file's InkImporter (isMasterFile) so that masters compile and
	/// includes don't. This is the slim editor-side include graph that replaces InkLibrary's old
	/// master/include auto-detection; the ScriptedImporter itself can't see the whole project, so a
	/// project-wide pass is still needed to answer "is this file included by another?".
	///
	/// Note: on large projects this rescans every .ink file's INCLUDE list whenever any ink file
	/// changes. That matches the old InkLibrary rebuild cost; it could be made incremental later.
	/// </summary>
	class InkMasterFileDetector : AssetPostprocessor {
		static void OnPostprocessAllAssets (string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
			bool inkChanged = imported.Any(InkEditorUtils.IsInkFile)
				|| deleted.Any(InkEditorUtils.IsInkFile)
				|| moved.Any(InkEditorUtils.IsInkFile)
				|| movedFrom.Any(InkEditorUtils.IsInkFile);
			// Defer so we don't reimport while Unity is still inside the current import batch.
			if (inkChanged) EditorApplication.delayCall += RefreshMasterFlags;
		}

		static void RefreshMasterFlags () {
			var allInkPaths = AssetDatabase.FindAssets("glob:\"*.ink\"")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(p => !string.IsNullOrEmpty(p))
				.ToList();

			// The set of files that are INCLUDEd by some other file.
			var includedPaths = new HashSet<string>();
			foreach (var path in allInkPaths)
				foreach (var include in InkImporter.GetDirectIncludePaths(path))
					includedPaths.Add(include);

			// Update each importer's master flag; only reimport files whose status actually changed.
			var toReimport = new List<string>();
			foreach (var path in allInkPaths) {
				if (!(AssetImporter.GetAtPath(path) is InkImporter importer)) continue;
				bool shouldBeMaster = !includedPaths.Contains(path);
				if (importer.IsMasterFile != shouldBeMaster) {
					var so = new SerializedObject(importer);
					so.FindProperty("isMasterFile").boolValue = shouldBeMaster;
					so.ApplyModifiedPropertiesWithoutUndo();
					toReimport.Add(path);
				}
			}

			if (toReimport.Count == 0) return;
			AssetDatabase.StartAssetEditing();
			try {
				foreach (var path in toReimport)
					AssetImporter.GetAtPath(path).SaveAndReimport();
			} finally {
				AssetDatabase.StopAssetEditing();
			}
		}
	}
}
