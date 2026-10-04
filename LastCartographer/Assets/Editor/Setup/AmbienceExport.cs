// The ambience's deliverables (AUD-05, docs/design/ambience.md): every region's layers rendered by the game's own
// generators to docs/audio/ambience/ for the sound designer to hear and replace.
//
//   -executeMethod OWSBG.Setup.AmbienceExport.Render
using System.Collections.Generic;
using System.IO;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    public static class AmbienceExport
    {
        public const string Folder = "docs/audio/ambience";
        public static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", Folder));

        [MenuItem("OWSBG/Render the Ambience")]
        public static void Render()
        {
            Directory.CreateDirectory(Root);
            var written = new List<string>();
            foreach (var l in Ambience.Layers)
            {
                var s = Ambience.Render(l);
                File.WriteAllBytes(Path.Combine(Root, l.FileName), RollCallSong.Wav(s));
                written.Add(l.FileName + " (" + l.Seconds + " s, " + Ambience.RmsDb(s).ToString("0.0") + " dB RMS)");
            }
            Debug.Log("[OWSBG] AmbienceExport: " + written.Count + " renders to " + Root + "\n" + string.Join("\n", written));
        }
    }
}
