using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Ink.UnityIntegration {
    /// <summary>
    /// Inspector for the InkImporter import settings. Master files (not INCLUDEd by another file) are
    /// detected automatically and compiled. For include files, exposes the optional "Compile As Master
    /// File" override — the equivalent of the old "Should also be Master File" tickbox.
    /// </summary>
    [CustomEditor(typeof(InkImporter))]
    public class InkImporterEditor : ScriptedImporterEditor {
        public override void OnInspectorGUI () {
            // Keep this section to import *settings* only (it owns Apply/Revert); all status and info is
            // shown on the imported InkFile below. Master files have no settings, so show a short note.
            var importer = (InkImporter)target;
            serializedObject.Update();

            if (importer.IsMasterFile) {
                EditorGUILayout.LabelField("Master file (detected automatically).", EditorStyles.miniLabel);
            } else {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("compileAsMasterFileOverride"),
                    new GUIContent("Compile As Master File",
                        "Also compile this file as a master, even though it's included by another file. " +
                        "(The equivalent of the old \"Should also be Master File\" option.)"));
            }

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
