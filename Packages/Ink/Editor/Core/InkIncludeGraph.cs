using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Editor-side cache of the .ink INCLUDE graph, rebuilt whenever ink files change. Provides fast
	/// master detection, includes / included-by lookups and recursive-include detection — the caching
	/// role InkLibrary used to serve, but without the old compilation queue or per-file metadata objects.
	///
	/// It also records each file's master status on its InkImporter (isMasterFile) so masters compile
	/// and includes don't. A .ink file is a master unless another .ink file INCLUDEs it.
	/// </summary>
	[InitializeOnLoad]
	class InkIncludeGraph : AssetPostprocessor {
		// file path -> the paths it directly INCLUDEs (resolved; may contain paths that don't exist).
		static Dictionary<string, string[]> _directIncludes;
		// file path -> the files that directly INCLUDE it.
		static Dictionary<string, List<string>> _includedBy;

		static InkIncludeGraph () {
			// Populate the cache once the editor has loaded.
			EditorApplication.delayCall += EnsureBuilt;
		}

		static void OnPostprocessAllAssets (string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
			bool inkChanged = imported.Any(InkEditorUtils.IsInkFile)
				|| deleted.Any(InkEditorUtils.IsInkFile)
				|| moved.Any(InkEditorUtils.IsInkFile)
				|| movedFrom.Any(InkEditorUtils.IsInkFile);
			// Defer so we don't reimport while Unity is still inside the current import batch.
			if (inkChanged) EditorApplication.delayCall += Rebuild;
		}

		static void EnsureBuilt () {
			if (_directIncludes == null) Rebuild();
		}

		static void Rebuild () {
			var allInkPaths = AssetDatabase.FindAssets("glob:\"*.ink\"")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(p => !string.IsNullOrEmpty(p))
				.ToList();

			_directIncludes = new Dictionary<string, string[]>();
			_includedBy = new Dictionary<string, List<string>>();
			foreach (var path in allInkPaths) {
				var includes = InkImporter.GetDirectIncludePaths(path).ToArray();
				_directIncludes[path] = includes;
				foreach (var include in includes) {
					if (!_includedBy.TryGetValue(include, out var list)) {
						list = new List<string>();
						_includedBy[include] = list;
					}
					if (!list.Contains(path)) list.Add(path);
				}
			}

			// Record master status on each importer; reimport only files whose status changed.
			var toReimport = new List<string>();
			foreach (var path in allInkPaths) {
				if (!(AssetImporter.GetAtPath(path) is InkImporter importer)) continue;
				bool shouldBeMaster = IsMaster(path);
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

		// ---- Fast cached queries (used by the inspector) ----

		/// <summary>A file is a master unless another file INCLUDEs it.</summary>
		public static bool IsMaster (string assetPath) {
			EnsureBuilt();
			return !_includedBy.ContainsKey(assetPath);
		}

		public static IReadOnlyList<string> GetDirectIncludes (string assetPath) {
			EnsureBuilt();
			return _directIncludes.TryGetValue(assetPath, out var v) ? v : Array.Empty<string>();
		}

		public static IReadOnlyList<string> GetIncludedBy (string assetPath) {
			EnsureBuilt();
			return _includedBy.TryGetValue(assetPath, out var v) ? (IReadOnlyList<string>)v : Array.Empty<string>();
		}

		/// <summary>Returns include paths that form a recursive (circular) INCLUDE reachable from this file.</summary>
		public static List<string> GetRecursiveIncludeErrorPaths (string assetPath) {
			EnsureBuilt();
			var offending = new List<string>();
			Visit(assetPath, new HashSet<string>(), new HashSet<string>(), offending);
			return offending;
		}

		static void Visit (string path, HashSet<string> stack, HashSet<string> done, List<string> offending) {
			if (stack.Contains(path)) {
				if (!offending.Contains(path)) offending.Add(path);
				return;
			}
			if (done.Contains(path)) return;
			stack.Add(path);
			if (_directIncludes.TryGetValue(path, out var includes))
				foreach (var include in includes) Visit(include, stack, done, offending);
			stack.Remove(path);
			done.Add(path);
		}
	}
}
