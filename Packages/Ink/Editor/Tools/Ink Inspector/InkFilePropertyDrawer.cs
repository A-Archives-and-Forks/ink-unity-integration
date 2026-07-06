using UnityEditor;
using UnityEngine;

namespace Ink.UnityIntegration {
	/// <summary>
	/// Default drawer for InkFile reference fields. Draws the normal object field and, if the assigned
	/// InkFile has no compiled story (an include file, or one that failed to compile), shows a warning —
	/// so it's obvious when a slot has been given a file that can't be turned into a Story at runtime.
	/// </summary>
	[CustomPropertyDrawer(typeof(InkFile))]
	public class InkFilePropertyDrawer : PropertyDrawer {
		const float spacing = 2f;

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			EditorGUI.BeginProperty(position, label, property);

			var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			// Use ObjectField (not PropertyField) to avoid recursively invoking this drawer.
			property.objectReferenceValue = EditorGUI.ObjectField(
				fieldRect, label, property.objectReferenceValue, typeof(InkFile), false);

			var inkFile = property.objectReferenceValue as InkFile;
			if (inkFile != null && !inkFile.isCompiled) {
				var warnRect = new Rect(position.x, fieldRect.yMax + spacing, position.width,
					position.height - EditorGUIUtility.singleLineHeight - spacing);
				EditorGUI.HelpBox(warnRect, inkFile.isMaster
					? "This ink file hasn't compiled to a story (it may have errors)."
					: "This is an INCLUDE file with no story of its own. Assign a master ink file instead.",
					MessageType.Warning);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label) {
			var height = EditorGUIUtility.singleLineHeight;
			var inkFile = property.objectReferenceValue as InkFile;
			if (inkFile != null && !inkFile.isCompiled)
				height += spacing + EditorGUIUtility.singleLineHeight * 2f;
			return height;
		}
	}
}
