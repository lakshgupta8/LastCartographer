// Wren's sounds and the tells as files (AUD-03, docs/design/wren-sounds.md): every cue rendered by the game's own
// generators to docs/audio/sfx/ for the sound designer to hear and replace.
//
//   -executeMethod OWSBG.Setup.InkSoundsExport.Render
using System.Collections.Generic;
using System.IO;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    public static class InkSoundsExport
    {
        public const string Folder = "docs/audio/sfx";
        public static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", Folder));

        /// <summary>A cue's file: wren_sfx_<id>.wav, tell_sfx_<kind>.wav for a tell, enemy_sfx_<id>.wav for an enemy's (no region, no beat: one-shots).</summary>
        public static string FileName(InkSounds.Cue c) => InkSounds.FileName(c);

        [MenuItem("OWSBG/Render Wren's Sounds")]
        public static void Render()
        {
            Directory.CreateDirectory(Root);
            var written = new List<string>();
            foreach (var c in InkSounds.Cues)
            {
                var s = InkSounds.Render(c.Id);
                File.WriteAllBytes(Path.Combine(Root, FileName(c)), RollCallSong.Wav(s));
                written.Add(FileName(c) + " (" + (InkSounds.Seconds(s) * 1000f).ToString("0") + " ms)");
            }
            Debug.Log("[OWSBG] InkSoundsExport: " + written.Count + " renders to " + Root + "\n" + string.Join("\n", written));
        }
    }
}
