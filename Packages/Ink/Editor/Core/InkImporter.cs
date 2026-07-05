using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using Ink;

namespace Ink.UnityIntegration {
    /// <summary>
    /// Compiles a .ink file on import and produces an InkFile asset holding the compiled JSON.
    /// Phase 1: every file is compiled standalone. Master/include handling arrives in Phase 2.
    /// </summary>
    [ScriptedImporter(1, "ink")]
    public class InkImporter : ScriptedImporter {
        public override void OnImportAsset (AssetImportContext ctx) {
            var inkFile = ScriptableObject.CreateInstance<InkFile>();
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
            } catch (System.Exception e) {
                ctx.LogImportError($"Ink compilation threw an exception for {fileName}: {e}");
            }

            ctx.AddObjectToAsset("InkFile", inkFile);
            ctx.SetMainObject(inkFile);
        }
    }
}
