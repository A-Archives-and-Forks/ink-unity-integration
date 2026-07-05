using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using Ink;

namespace Ink.UnityIntegration {
    /// <summary>
    /// Compiles a .ink file on import and produces an InkFile asset holding the compiled JSON.
    ///
    /// Only files flagged as master files are compiled. Include files (the default) are imported
    /// as empty InkFile assets and are compiled as part of the master(s) that INCLUDE them. This
    /// preserves the master-file workflow while avoiding errors from compiling includes standalone.
    /// </summary>
    [ScriptedImporter(1, "ink")]
    public class InkImporter : ScriptedImporter {
        [SerializeField]
        [Tooltip("Master files are compiled to JSON on import. Leave this off for files that are only " +
                 "INCLUDEd by other files and can't compile on their own (e.g. they rely on variables or " +
                 "functions defined elsewhere). Only master files produce a runnable story.")]
        bool compileAsMasterFile = false;

        public bool CompileAsMasterFile => compileAsMasterFile;

        /// <summary>
        /// Declares every INCLUDEd file (recursively) as a dependency, so Unity imports includes before
        /// their master and reimports a master whenever any nested include changes. Runs on the main
        /// thread before import, so reading files directly here is safe.
        /// </summary>
        public static string[] GatherDependenciesFromSourceFile (string assetPath) {
            var results = new List<string>();
            var visited = new HashSet<string>();
            CollectIncludes(assetPath, results, visited);
            return results.ToArray();
        }

        static void CollectIncludes (string assetPath, List<string> results, HashSet<string> visited) {
            if (!visited.Add(assetPath)) return;
            string text;
            try { text = File.ReadAllText(assetPath); }
            catch { return; }
            var dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            foreach (var include in InkIncludeParser.ParseIncludes(text)) {
                var includePath = NormalizeAssetPath(dir, include);
                if (!results.Contains(includePath)) results.Add(includePath);
                CollectIncludes(includePath, results, visited);
            }
        }

        // Resolves an INCLUDE path (relative to the including file's folder) into a project-relative
        // asset path, collapsing "." and ".." segments and normalising to forward slashes.
        static string NormalizeAssetPath (string dir, string include) {
            var combined = (string.IsNullOrEmpty(dir) ? include : dir + "/" + include).Replace('\\', '/');
            var parts = new List<string>();
            foreach (var part in combined.Split('/')) {
                if (part.Length == 0 || part == ".") continue;
                if (part == "..") { if (parts.Count > 0) parts.RemoveAt(parts.Count - 1); }
                else parts.Add(part);
            }
            return string.Join("/", parts);
        }

        public override void OnImportAsset (AssetImportContext ctx) {
            var inkFile = ScriptableObject.CreateInstance<InkFile>();

            if (compileAsMasterFile) {
                var absolutePath = InkEditorUtils.UnityRelativeToAbsolutePath(ctx.assetPath);
                var inputString = File.ReadAllText(ctx.assetPath);
                var fileName = Path.GetFileName(absolutePath);

                var compiler = new Compiler(inputString, new Compiler.Options {
                    countAllVisits = true,
                    fileHandler = new UnityInkFileHandler(Path.GetDirectoryName(absolutePath)),
                    errorHandler = (string message, ErrorType type) => {
                        if (InkCompilerLog.TryParse(message, out var log)) {
                            if (string.IsNullOrEmpty(log.relativeFilePath)) log.relativeFilePath = fileName;
                            var location = $"{log.relativeFilePath}:{log.lineNumber}";
                            switch (log.type) {
                                case ErrorType.Error:
                                    inkFile.errors.Add(log);
                                    ctx.LogImportError($"Ink Error for {fileName}: {log.content} (at {location})", inkFile);
                                    break;
                                case ErrorType.Warning:
                                    inkFile.warnings.Add(log);
                                    ctx.LogImportWarning($"Ink Warning for {fileName}: {log.content} (at {location})", inkFile);
                                    break;
                                case ErrorType.Author:
                                    inkFile.todos.Add(log);
                                    break;
                            }
                        } else {
                            ctx.LogImportWarning($"Couldn't parse ink compiler log: {message}");
                        }
                    }
                });

                try {
                    var compiledStory = compiler.Compile();
                    if (compiledStory != null) inkFile.SetStoryJson(compiledStory.ToJson());
                } catch (Exception e) {
                    ctx.LogImportError($"Ink compilation threw an exception for {fileName}: {e}");
                }

                // Belt-and-braces: also register direct includes (GatherDependenciesFromSourceFile
                // already declares the full recursive set for import ordering + reimport).
                var dir = Path.GetDirectoryName(ctx.assetPath)?.Replace('\\', '/');
                foreach (var include in InkIncludeParser.ParseIncludes(inputString)) {
                    ctx.DependsOnSourceAsset(NormalizeAssetPath(dir, include));
                }
            }

            ctx.AddObjectToAsset("InkFile", inkFile);
            ctx.SetMainObject(inkFile);
        }
    }
}
