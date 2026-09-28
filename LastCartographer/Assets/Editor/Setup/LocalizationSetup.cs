using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;
using Yarn.Compiler;
using Yarn.Unity;
using Yarn.Unity.Editor;

// Localisation upkeep (PRG-19). Run after writing dialogue or UI text:
//
//   -executeMethod OWSBG.Setup.LocalizationSetup.Refresh
//
// 1) gives every Yarn line without one a #line: id (Yarn Spinner's own tagger; ids are what translations key on),
// 2) writes the pseudo-locale's dialogue table (Dialogue/Localisation/en-XA.csv) from the lines as they are now,
// 3) harvests every Loc.T / Loc.F in Code/ into the English UI table translators work from (Settings/Localization/ui.en.csv).
// The edit-mode LocalizationTests fail when any of the three is out of date.
namespace OWSBG.Setup
{
    public static class LocalizationSetup
    {
        public const string DialogueDir = "Assets/_Project/Dialogue";
        public const string YarnProjectPath = DialogueDir + "/LastCartographer.yarnproject";
        public const string PseudoCsvPath = DialogueDir + "/Localisation/" + Loc.Pseudo + ".csv";
        public const string CodeDir = "Assets/_Project/Code";
        public const string UiSourcePath = "Assets/_Project/Settings/Localization/ui." + Loc.Base + ".csv";

        [MenuItem("OWSBG/Localisation/Refresh (tag lines, pseudo table, UI table)")]
        public static void Refresh()
        {
            int tagged = TagLines();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int lines = WritePseudoDialogue();
            int keys = WriteUiSource();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(YarnProjectPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[OWSBG] Localisation: " + tagged + " files tagged; " + lines + " lines in " + PseudoCsvPath + "; " + keys + " keys in " + UiSourcePath);
        }

        static List<string> YarnFiles() =>
            Directory.GetFiles(DialogueDir, "*.yarn", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToList();

        static CompilationResult CompileStrings()
        {
            var job = CompilationJob.CreateFromFiles(YarnFiles());
            job.CompilationType = CompilationJob.Type.StringsOnly;
            var result = Compiler.Compile(job);
            var errors = result.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error).ToList();
            if (errors.Count > 0) throw new System.InvalidOperationException("Yarn has errors: " + string.Join("; ", errors.Select(e => e.ToString())));
            return result;
        }

        /// <summary>Every line without a #line: tag gets one. Returns how many files changed.</summary>
        public static int TagLines()
        {
            var existing = new HashSet<string>(CompileStrings().StringTable.Where(kv => !kv.Value.isImplicitTag).Select(kv => kv.Key));
            int changed = 0;
            foreach (var path in YarnFiles())
            {
                var contents = File.ReadAllText(path);
                var tagged = Utility.TagLines(contents, existing);
                if (tagged.TagExceptions.Count > 0)
                    throw new System.InvalidOperationException(path + ": " + string.Join("; ", tagged.TagExceptions.Select(e => e.Message)));
                var modified = tagged.ModifiedSource;
                if (modified == null) continue;
                modified = System.Text.RegularExpressions.Regex.Replace(modified, @"(#line:[0-9a-f]+)[ \t]+(?=\r?\n|$)", "$1");   // the tagger leaves a space after the tag
                if (modified == contents) continue;
                File.WriteAllText(path, modified, new UTF8Encoding(false));
                changed++;
            }
            return changed;
        }

        /// <summary>The pseudo-locale's dialogue: every line, accented and bracketed, its speaker kept. Returns the line count.</summary>
        public static int WritePseudoDialogue()
        {
            var result = CompileStrings();
            var entries = result.StringTable
                .Select(kv => new StringTableEntry
                {
                    Language = Loc.Pseudo,
                    ID = kv.Key,
                    Text = Loc.Pseudoize(kv.Value.text, keepSpeaker: true),
                    File = Path.GetFileNameWithoutExtension(kv.Value.fileName),
                    Node = kv.Value.nodeName,
                    LineNumber = kv.Value.lineNumber.ToString(),
                    Lock = YarnImporter.GetHashString(kv.Value.text, 8),
                    Comment = "",
                })
                .OrderBy(e => e.File).ThenBy(e => int.Parse(e.LineNumber)).ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(PseudoCsvPath));
            File.WriteAllText(PseudoCsvPath, StringTableEntry.CreateCSV(entries), new UTF8Encoding(false));
            return entries.Count;
        }

        /// <summary>The English UI table, harvested from the code: key, text, where it is used. Returns the key count.</summary>
        public static int WriteUiSource()
        {
            var keys = new SortedDictionary<string, (string english, SortedSet<string> files)>(System.StringComparer.Ordinal);
            foreach (var path in Directory.GetFiles(CodeDir, "*.cs", SearchOption.AllDirectories).OrderBy(p => p))
            {
                var file = Path.GetFileName(path);
                foreach (var (key, english) in Loc.Harvest(File.ReadAllText(path)))
                {
                    if (keys.TryGetValue(key, out var had))
                    {
                        if (had.english != english) throw new System.InvalidOperationException("Loc key " + key + " has two texts: \"" + had.english + "\" and \"" + english + "\"");
                        had.files.Add(file);
                    }
                    else keys[key] = (english, new SortedSet<string> { file });
                }
            }
            var sb = new StringBuilder("key,text,comment\n");
            foreach (var kv in keys)
                sb.Append(Loc.CsvField(kv.Key)).Append(',').Append(Loc.CsvField(kv.Value.english)).Append(',').Append(Loc.CsvField(string.Join(" ", kv.Value.files))).Append('\n');
            Directory.CreateDirectory(Path.GetDirectoryName(UiSourcePath));
            File.WriteAllText(UiSourcePath, sb.ToString(), new UTF8Encoding(false));
            return keys.Count;
        }
    }
}
