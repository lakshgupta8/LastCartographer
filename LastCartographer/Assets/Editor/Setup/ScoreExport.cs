// The score's deliverables (AUD-04, docs/design/music.md): every theme's stems rendered by the game's own synth as
// WAV, a mix of them to listen to, the resolution, and each theme as MIDI, to docs/audio/music/ for the composer.
//
//   -executeMethod OWSBG.Setup.ScoreExport.Render
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    public static class ScoreExport
    {
        public const string Folder = "docs/audio/music";
        public static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", Folder));

        [MenuItem("OWSBG/Render the Score")]
        public static void Render()
        {
            Directory.CreateDirectory(Root);
            var written = new List<string>();
            foreach (var theme in Score.Themes.Concat(Score.SharedThemes))
            {
                var stems = Score.Render(theme);
                int len = 0;
                foreach (var kv in stems)
                {
                    string file = Score.FileName(theme, theme.Stem(kv.Key));
                    File.WriteAllBytes(Path.Combine(Root, file), RollCallSong.Wav(kv.Value));
                    written.Add(file + " (" + (kv.Value.Length / (float)Score.SampleRate).ToString("0.0") + " s)");
                    len = kv.Value.Length;
                }
                var mix = new float[len];
                foreach (var s in stems.Values) for (int i = 0; i < len; i++) mix[i] += s[i];
                string all = RollCallSong.FileName(theme.Region, "music", (theme.Boss != null || theme.Coda != Ending.None ? theme.Id + "-" : "") + "all-stems", 60f / theme.Beat);
                File.WriteAllBytes(Path.Combine(Root, all), RollCallSong.Wav(mix));
                written.Add(all);
                string midi = theme.Region.ToString().ToLowerInvariant() + "_music_" + theme.Id + "_" + Mathf.RoundToInt(60f / theme.Beat) + ".mid";
                File.WriteAllBytes(Path.Combine(Root, midi), Score.Midi(theme));
                written.Add(midi);
            }
            foreach (var region in new[] { Region.Saltmarrow, Region.Emberdown, Region.Verdance, Region.Halden, Region.Windreach, Region.Greyfold, Region.Blank })
            {
                var res = Score.Resolution(region);
                string resFile = RollCallSong.FileName(region, "music", "resolution", 60f / AudioDirection.BeatOf(region));
                File.WriteAllBytes(Path.Combine(Root, resFile), RollCallSong.Wav(res));
                written.Add(resFile);
            }
            Debug.Log("[OWSBG] ScoreExport: " + written.Count + " files to " + Root + "\n" + string.Join("\n", written));
        }
    }
}
