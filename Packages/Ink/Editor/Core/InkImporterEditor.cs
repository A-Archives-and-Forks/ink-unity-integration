using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Ink.UnityIntegration {
    /// <summary>
    /// Inspector for the InkImporter import settings. Exposes the master-file toggle that decides
    /// whether a .ink file is compiled to JSON on its own, or only as part of the file(s) that INCLUDE it.
    /// </summary>
    [CustomEditor(typeof(InkImporter))]
    public class InkImporterEditor : ScriptedImporterEditor {
        public override void OnInspectorGUI () {
            serializedObject.Update();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("compileAsMasterFile"),
                new GUIContent("Compile As Master File",
                    "Master files are compiled to a runnable story on import. Leave this off for files " +
                    "that are only INCLUDEd by other files and can't compile standalone."));
            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
