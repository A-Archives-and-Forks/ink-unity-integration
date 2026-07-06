using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ink.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Inspector for the compiled InkFile asset produced by InkImporter. Shows the file's master/include
	/// status, compiler diagnostics, INCLUDE relationships (from the cached InkIncludeGraph), edit/compile
	/// dates, a source preview and actions — everything the old InkInspector showed, adapted to the importer.
	/// </summary>
	[CustomEditor(typeof(InkFile))]
	public class InkFileEditor : Editor {
		const int maxPreviewChars = 16000;
		bool showJson;
		bool showSource;

		public override void OnInspectorGUI () {
			var inkFile = (InkFile)target;
			var assetPath = AssetDatabase.GetAssetPath(inkFile);

			using (new EditorGUILayout.HorizontalScope()) {
				var label = inkFile.isMaster
					? new GUIContent("Master File", "A master file — compiled to a runnable story on import.")
					: new GUIContent("Include File", InkBrowserIcons.childIcon, "Included by another ink file; compiled as part of that master.");
				EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
				if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(60))) AssetDatabase.OpenAsset(inkFile);
			}

			var recursive = InkIncludeGraph.GetRecursiveIncludeErrorPaths(assetPath);
			if (recursive.Count > 0)
				EditorGUILayout.HelpBox("A recursive INCLUDE connection exists in this file's INCLUDE hierarchy:\n" +
					string.Join("\n", recursive.Select(p => "• " + p)), MessageType.Error);

			if (inkFile.hasUnhandledCompileErrors) {
				EditorGUILayout.HelpBox("The compiler failed unexpectedly. This may be a compiler bug — please report it.", MessageType.Error);
				using (new EditorGUILayout.HorizontalScope()) {
					if (GUILayout.Button("Report via GitHub")) Application.OpenURL("https://github.com/inkle/ink-unity-integration/issues");
					if (GUILayout.Button("Report via Email")) Application.OpenURL("mailto:info@inklestudios.com?subject=Ink%20compiler%20bug");
				}
			}

			DrawLogs(inkFile, "Errors", inkFile.errors, MessageType.Error);
			DrawLogs(inkFile, "Warnings", inkFile.warnings, MessageType.Warning);
			DrawLogs(inkFile, "To do", inkFile.todos, MessageType.Info);

			var includes = InkIncludeGraph.GetDirectIncludes(assetPath);
			if (includes.Count > 0) DrawFileList("Included Files", includes);
			if (!inkFile.isMaster) {
				var includedBy = InkIncludeGraph.GetIncludedBy(assetPath);
				if (includedBy.Count > 0) DrawFileList("Included By", includedBy);
			}

			DrawDates(inkFile, assetPath);

			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Reimport")) AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
				using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(inkFile.storyJson))) {
					if (GUILayout.Button("Play in Ink Player")) InkPlayerWindow.Attach(new Story(inkFile.storyJson));
				}
			}

			showJson = EditorGUILayout.Foldout(showJson, "Compiled JSON");
			if (showJson) EditorGUILayout.SelectableLabel(inkFile.storyJson, EditorStyles.textArea, GUILayout.MaxHeight(200));

			showSource = EditorGUILayout.Foldout(showSource, "Source");
			if (showSource) EditorGUILayout.SelectableLabel(ReadSource(assetPath), EditorStyles.textArea, GUILayout.MaxHeight(200));
		}

		static void DrawLogs (InkFile inkFile, string label, List<InkCompilerLog> logs, MessageType type) {
			if (logs == null || logs.Count == 0) return;
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			foreach (var log in logs) {
				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.HelpBox($"{log.content} (at {log.relativeFilePath}:{log.lineNumber})", type);
					if (GUILayout.Button("Open", GUILayout.Width(50), GUILayout.Height(38)))
						AssetDatabase.OpenAsset(inkFile, log.lineNumber);
				}
			}
		}

		static void DrawFileList (string label, IReadOnlyList<string> paths) {
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			using (new EditorGUI.DisabledScope(true)) {
				foreach (var path in paths)
					EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<InkFile>(path), typeof(InkFile), false);
			}
		}

		static void DrawDates (InkFile inkFile, string assetPath) {
			var sb = new StringBuilder();
			try { sb.Append("Last edit: ").Append(File.GetLastWriteTime(InkEditorUtils.UnityRelativeToAbsolutePath(assetPath))); }
			catch { /* file may be gone mid-edit */ }
			if (inkFile.compileDate.HasValue) sb.Append("\nLast compile: ").Append(inkFile.compileDate.Value);
			if (sb.Length > 0) EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
		}

		static string ReadSource (string assetPath) {
			try {
				var source = File.ReadAllText(InkEditorUtils.UnityRelativeToAbsolutePath(assetPath));
				return source.Length > maxPreviewChars ? source.Substring(0, maxPreviewChars) + "\n…" : source;
			} catch { return ""; }
		}
	}
}
