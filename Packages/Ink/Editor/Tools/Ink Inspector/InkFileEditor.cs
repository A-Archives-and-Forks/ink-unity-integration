using System.Collections.Generic;
using System.Linq;
using Ink.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Inspector for the compiled InkFile asset produced by InkImporter. Shows whether the file is a
	/// master or include, its compiler diagnostics, its INCLUDE relationships, a play button, and the
	/// compiled JSON.
	/// </summary>
	[CustomEditor(typeof(InkFile))]
	public class InkFileEditor : Editor {
		bool showJson;

		public override void OnInspectorGUI () {
			var inkFile = (InkFile)target;
			var assetPath = AssetDatabase.GetAssetPath(inkFile);

			using (new EditorGUILayout.HorizontalScope()) {
				EditorGUILayout.LabelField(inkFile.isMaster ? "Master File" : "Include File", EditorStyles.boldLabel);
				if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(60)))
					AssetDatabase.OpenAsset(inkFile);
			}

			if (inkFile.isMaster && string.IsNullOrEmpty(inkFile.storyJson) && !inkFile.hasErrors)
				EditorGUILayout.HelpBox("Not yet compiled. If the file was just created or changed, it will compile shortly.", MessageType.Info);

			DrawLogs("Errors", inkFile.errors, MessageType.Error);
			DrawLogs("Warnings", inkFile.warnings, MessageType.Warning);
			DrawLogs("Todos", inkFile.todos, MessageType.Info);

			var includes = InkImporter.GetDirectIncludePaths(assetPath).ToList();
			if (includes.Count > 0) DrawFileList("Includes", includes);

			if (!inkFile.isMaster) {
				var includedBy = FindFilesIncluding(assetPath);
				if (includedBy.Count > 0) DrawFileList("Included By", includedBy);
			}

			using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(inkFile.storyJson))) {
				if (GUILayout.Button("Play in Ink Player"))
					InkPlayerWindow.Attach(new Story(inkFile.storyJson));
			}

			showJson = EditorGUILayout.Foldout(showJson, "Compiled JSON");
			if (showJson)
				EditorGUILayout.SelectableLabel(inkFile.storyJson, EditorStyles.textArea, GUILayout.MaxHeight(200));
		}

		static void DrawLogs (string label, List<InkCompilerLog> logs, MessageType type) {
			if (logs == null || logs.Count == 0) return;
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			foreach (var log in logs)
				EditorGUILayout.HelpBox($"{log.content} (at {log.relativeFilePath}:{log.lineNumber})", type);
		}

		static void DrawFileList (string label, List<string> paths) {
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			using (new EditorGUI.DisabledScope(true)) {
				foreach (var path in paths)
					EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<InkFile>(path), typeof(InkFile), false);
			}
		}

		// Scans all .ink files for ones that directly INCLUDE the given file. Used only while an include
		// file is selected, so the project-wide scan cost is acceptable.
		static List<string> FindFilesIncluding (string targetPath) {
			var result = new List<string>();
			foreach (var guid in AssetDatabase.FindAssets("glob:\"*.ink\"")) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (path == targetPath) continue;
				if (InkImporter.GetDirectIncludePaths(path).Contains(targetPath)) result.Add(path);
			}
			return result;
		}
	}
}
