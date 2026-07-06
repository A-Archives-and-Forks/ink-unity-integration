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
    /// Master files (files that aren't INCLUDEd by any other file) are compiled to a runnable story.
    /// Include files are imported as empty InkFile assets and compiled as part of their master(s), so
    /// they don't error on their own. Whether a file is a master is detected automatically by
    /// InkMasterFileDetector (which sets isMasterFile); the override lets you also compile an include.
    /// </summary>
    [ScriptedImporter(1, "ink")]
    public class InkImporter : ScriptedImporter {
        // Set automatically by InkMasterFileDetector: true when no other .ink file INCLUDEs this one.
        [SerializeField] bool isMasterFile = false;
        // Optional user override (matches the old "Should also be Master File" tickbox): compile this
        // file as a master even though it is INCLUDEd by another file.
        [SerializeField] bool compileAsMasterFileOverride = false;

        public bool IsMasterFile => isMasterFile;
        public bool CompileAsMasterFileOverride => compileAsMasterFileOverride;
        // A file is compiled if it's a detected master, or the user has forced it to compile.
        public bool ShouldCompile => isMasterFile || compileAsMasterFileOverride;

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
            foreach (var includePath in GetDirectIncludePaths(assetPath)) {
                if (!results.Contains(includePath)) results.Add(includePath);
                CollectIncludes(includePath, results, visited);
            }
        }

        /// <summary>
        /// Returns the project-relative paths of the files this .ink file directly INCLUDEs.
        /// Used both for dependency tracking and by InkMasterFileDetector to build the include graph.
        /// </summary>
        public static IEnumerable<string> GetDirectIncludePaths (string assetPath) {
            string text;
            try { text = File.ReadAllText(assetPath); }
            catch { yield break; }
            var dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            foreach (var include in InkIncludeParser.ParseIncludes(text)) {
                yield return NormalizeAssetPath(dir, include);
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
            inkFile.SetIsMaster(isMasterFile);

            if (ShouldCompile) {
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
                foreach (var includePath in GetDirectIncludePaths(ctx.assetPath)) {
                    ctx.DependsOnSourceAsset(includePath);
                }
            }

            ctx.AddObjectToAsset("InkFile", inkFile);
            ctx.SetMainObject(inkFile);
        }
    }
}
