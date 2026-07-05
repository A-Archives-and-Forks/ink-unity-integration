using System.Collections.Generic;
using Ink.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Inspector for the compiled InkFile asset produced by InkImporter. Shows compiler
	/// diagnostics, lets you play the story in the Ink Player, and exposes the compiled JSON.
	/// (Include/master relationship browsing will return once InkLibrary is reworked in a later phase.)
	/// </summary>
	[CustomEditor(typeof(InkFile))]
	public class InkFileEditor : Editor {
		bool showJson;

		public override void OnInspectorGUI () {
			var inkFile = (InkFile)target;

			if (string.IsNullOrEmpty(inkFile.storyJson) && !inkFile.hasErrors) {
				EditorGUILayout.HelpBox(
					"This file hasn't been compiled to a story. If it's a standalone story, enable " +
					"\"Compile As Master File\" in the Import Settings above. Files that are only INCLUDEd " +
					"by others don't compile on their own.", MessageType.Info);
			}

			DrawLogs("Errors", inkFile.errors, MessageType.Error);
			DrawLogs("Warnings", inkFile.warnings, MessageType.Warning);
			DrawLogs("Todos", inkFile.todos, MessageType.Info);

			using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(inkFile.storyJson))) {
				if (GUILayout.Button("Play in Ink Player")) {
					InkPlayerWindow.Attach(new Story(inkFile.storyJson));
				}
			}

			showJson = EditorGUILayout.Foldout(showJson, "Compiled JSON");
			if (showJson) {
				EditorGUILayout.SelectableLabel(inkFile.storyJson, EditorStyles.textArea, GUILayout.MaxHeight(200));
			}
		}

		static void DrawLogs (string label, List<InkCompilerLog> logs, MessageType type) {
			if (logs == null || logs.Count == 0) return;
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			foreach (var log in logs) {
				EditorGUILayout.HelpBox($"{log.content} (at {log.relativeFilePath}:{log.lineNumber})", type);
			}
		}
	}
}
