// The roll-call's deliverables (AUD-02, docs/design/roll-call.md): every use rendered by the game's own synth as WAV,
// and the notes as MIDI, to docs/audio/rollcall/ for the composer to hear and replace.
//
//   -executeMethod OWSBG.Setup.RollCallExport.Render
using System.Collections.Generic;
using System.IO;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    public static class RollCallExport
    {
        public const string Folder = "docs/audio/rollcall";

        /// <summary>The Merrow's End walk: four of Saltmarrow's beats a bound (bounds-walk doc 1).</summary>
        public const float WalkBeat = 0.9f * 4f;

        public static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", Folder));

        [MenuItem("OWSBG/Render the Roll-Call")]
        public static void Render()
        {
            Directory.CreateDirectory(Root);
            var written = new List<string>();
            // Every use, with everyone in the chorus (the deliverable is the whole; the game sings whoever was met).
            var everyone = new WorldState();
            foreach (var (_, flag) in RollCallSong.ChorusRoster) everyone.Set(flag, true);
            foreach (var u in RollCallSong.Uses)
            {
                var voices = RollCallSong.VoicesFor(u, everyone);
                int names = RollCallSong.NamesFor(u, voices);
                float beat = AudioDirection.BeatOf(u.Tempo);
                var notes = RollCallSong.Notes(u.Form, names);
                written.Add(Write(u.Key, "rollcall-" + u.Id, beat, RollCallSong.Chorus(notes, RollCallSong.TonicHz(u.Key), beat, voices), notes, voices));
            }
            // The walk's pieces at Merrow's End: the call at the walk's beat, the answer at the region's.
            var dotha = RollCallSong.WalkChorus("merrows_end");
            var call = RollCallSong.Notes(RollCallSong.Form.WalkCall);
            written.Add(Write(Region.Saltmarrow, "rollcall-walk-call", WalkBeat, RollCallSong.Chorus(call, RollCallSong.TonicHz(Region.Saltmarrow), WalkBeat, dotha), call, dotha));
            var falter = RollCallSong.Notes(RollCallSong.Form.Name);
            written.Add(Write(Region.Saltmarrow, "rollcall-walk-falter", WalkBeat, RollCallSong.Chorus(falter, RollCallSong.TonicHz(Region.Saltmarrow), WalkBeat, dotha, 0), falter, dotha));
            var answer = RollCallSong.Notes(RollCallSong.Form.Answer);
            float coast = AudioDirection.BeatOf(Region.Saltmarrow);
            written.Add(Write(Region.Saltmarrow, "rollcall-walk-answer", coast, RollCallSong.Chorus(answer, RollCallSong.TonicHz(Region.Saltmarrow), coast, dotha), answer, dotha));
            Debug.Log("[OWSBG] RollCallExport: " + written.Count + " renders to " + Root + "\n" + string.Join("\n", written));
        }

        static string Write(Region key, string name, float beat, RollCallSong.Line line, IList<AudioDirection.Note> notes, IList<string> voices)
        {
            string file = RollCallSong.FileName(key, "voice", name, 60f / beat);
            File.WriteAllBytes(Path.Combine(Root, file), RollCallSong.Wav(line.Samples));
            var tracks = new List<RollCallSong.MidiTrack>();
            foreach (var v in voices)
            {
                var voice = RollCallSong.VoiceOf(v);
                tracks.Add(new RollCallSong.MidiTrack { Name = voice.Id, Program = voice.Whale ? 52 : 53, Transpose = voice.Transpose, Notes = notes });   // choir aahs / voice oohs
            }
            File.WriteAllBytes(Path.Combine(Root, Path.ChangeExtension(file, ".mid")), RollCallSong.Midi(beat, RollCallSong.TonicMidi(key), tracks));
            return file + " (" + line.Seconds.ToString("0.0") + " s, " + voices.Count + " voice" + (voices.Count == 1 ? "" : "s") + ")";
        }
    }
}
