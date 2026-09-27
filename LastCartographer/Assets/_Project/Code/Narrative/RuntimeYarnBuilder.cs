#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Yarn.Compiler;
using Yarn.Unity;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Compiles Yarn source text into a YarnProject at runtime. Used by tests and by tooling that
    /// wants to run a script without a .yarnproject asset. Mirrors the package importer's serialisation.
    /// </summary>
    public static class RuntimeYarnBuilder
    {
        public static YarnProject Build(string source, string fileName = "runtime.yarn")
        {
            var job = CompilationJob.CreateFromString(fileName, source);
            var result = Compiler.Compile(job);
            if (result.ContainsErrors)
            {
                var msg = string.Join("\n", result.Diagnostics
                    .Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));
                throw new InvalidOperationException("Yarn compile failed:\n" + msg);
            }

            byte[] bytes;
            using (var ms = new MemoryStream())
            using (var os = new Google.Protobuf.CodedOutputStream(ms))
            {
                result.Program.WriteTo(os);
                os.Flush();
                bytes = ms.ToArray();
            }

            var loc = ScriptableObject.CreateInstance<Localization>();
            loc.AddLocalizedStrings(result.StringTable
                .Where(kv => kv.Value.text != null)
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.text)));

            var project = ScriptableObject.CreateInstance<YarnProject>();
            project.compiledYarnProgram = bytes;
            project.baseLocalization = loc;
            project.localizationType = LocalizationType.YarnInternal;
            return project;
        }

        /// <summary>Several files: nodes are independent, so the sources are simply concatenated.</summary>
        public static YarnProject Build(IEnumerable<string> sources)
            => Build(string.Join("\n\n", sources));
    }
}
