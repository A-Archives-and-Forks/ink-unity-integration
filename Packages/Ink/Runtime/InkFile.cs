using System.Collections.Generic;
using UnityEngine;

namespace Ink.UnityIntegration {
    /// <summary>
    /// The imported representation of a .ink file. This is a runtime ScriptableObject
    /// (the main asset produced by InkImporter) holding the compiled story JSON plus any
    /// compiler logs. Reference this from game code and pass storyJson to a new Ink.Runtime.Story.
    /// </summary>
    public class InkFile : ScriptableObject {
        [SerializeField] string _storyJson;
        /// <summary>The compiled ink story as JSON. Pass this to new Ink.Runtime.Story(inkFile.storyJson).</summary>
        public string storyJson => _storyJson;

        // Compiler diagnostics captured during import.
        public List<InkCompilerLog> errors = new List<InkCompilerLog>();
        public List<InkCompilerLog> warnings = new List<InkCompilerLog>();
        public List<InkCompilerLog> todos = new List<InkCompilerLog>();

        public bool hasErrors => errors.Count > 0;
        public bool hasWarnings => warnings.Count > 0;
        public bool hasTodos => todos.Count > 0;

        /// <summary>
        /// Populated by InkImporter during import. Not intended to be called elsewhere.
        /// </summary>
        public void SetStoryJson (string storyJson) {
            _storyJson = storyJson;
        }

        public override string ToString () => $"[InkFile: name={name}]";
    }
}
